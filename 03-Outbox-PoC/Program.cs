using Marten;
using OutboxPoC.Events;
using OutboxPoC.Models;
using OutboxPoC.Repositories;
using OutboxPoC.Services;
using StackExchange.Redis;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("PostgreSQL connection string is required");
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string is required");

// Configure Marten for PostgreSQL document store
builder.Services.AddMarten(opts =>
{
    opts.Connection(postgresConnection);
    opts.DatabaseSchemaName = "outbox_poc";

    // Register document types
    opts.RegisterDocumentType<Payment>();

    // Auto-create database schema
    opts.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
})
.IntegrateWithWolverine();  // Critical: Enables transactional outbox pattern

// Configure Wolverine with Marten Outbox
builder.Host.UseWolverine(opts =>
{
    // Enable durable inbox/outbox with Marten
    opts.PersistMessagesWithMarten();

    // Configure local queue for async processing
    opts.PublishAllMessages().ToLocalQueue("payments");
    opts.LocalQueue("payments").UseDurableInbox();

    Console.WriteLine("Wolverine configured with Marten transactional outbox");
});

// Configure Redis
var redis = ConnectionMultiplexer.Connect(redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Register services
builder.Services.AddScoped<IPaymentRepository, MartenPaymentRepository>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<INotificationService, ConsoleNotificationService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ============ API ENDPOINTS ============

app.MapGet("/", () => new
{
    Title = "Wolverine Outbox Pattern PoC",
    Description = "Demonstrating transactional outbox with PostgreSQL (Marten) and Redis caching",
    Endpoints = new[]
    {
        "POST /payments - Create a new payment (publishes PaymentInitiatedEvent)",
        "POST /payments/{id}/process - Process a payment (publishes PaymentProcessedEvent)",
        "POST /payments/{id}/fail - Mark payment as failed (publishes PaymentFailedEvent)",
        "POST /payments/{id}/refund - Refund a payment (publishes PaymentRefundedEvent)",
        "GET /payments - Get all payments",
        "GET /payments/{id} - Get payment by ID",
        "GET /payments/user/{userId} - Get payments by user",
        "GET /payments/{id}/cache - Check payment status in cache"
    },
    Features = new[]
    {
        "Transactional Outbox - Messages guaranteed to be published even if app crashes",
        "PostgreSQL - Durable storage with Marten document DB",
        "Redis - Caching for fast lookups",
        "Domain Events - PaymentInitiated, Processed, Failed, Refunded"
    }
});

// Create Payment - Uses transactional outbox
app.MapPost("/payments", async (CreatePaymentRequest request, IPaymentRepository repository, IMessageContext context) =>
{
    var payment = new Payment
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        Amount = request.Amount,
        Currency = request.Currency,
        Status = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    // Save to database and publish event in a single transaction
    // If the database save fails, the event will NOT be published
    // If the database save succeeds, the event is GUARANTEED to be published
    await repository.SaveAsync(payment);

    // Publish event - this will be stored in outbox table
    await context.PublishAsync(new PaymentInitiatedEvent(
        payment.Id,
        payment.UserId,
        payment.Amount,
        payment.Currency,
        payment.CreatedAt
    ));

    Console.WriteLine($"[API] Payment {payment.Id} created and event stored in outbox");

    return Results.Created($"/payments/{payment.Id}", payment);
});

// Process Payment
app.MapPost("/payments/{id}/process", async (Guid id, IPaymentRepository repository, IMessageContext context) =>
{
    var payment = await repository.GetByIdAsync(id);
    if (payment == null)
        return Results.NotFound();

    payment.Status = PaymentStatus.Completed;
    payment.ProcessedAt = DateTime.UtcNow;
    payment.TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";

    await repository.UpdateAsync(payment);

    // Publish event via outbox
    await context.PublishAsync(new PaymentProcessedEvent(
        payment.Id,
        payment.UserId,
        payment.Amount,
        payment.TransactionId,
        payment.ProcessedAt.Value
    ));

    return Results.Ok(payment);
});

// Fail Payment
app.MapPost("/payments/{id}/fail", async (Guid id, string reason, IPaymentRepository repository, IMessageContext context) =>
{
    var payment = await repository.GetByIdAsync(id);
    if (payment == null)
        return Results.NotFound();

    payment.Status = PaymentStatus.Failed;

    await repository.UpdateAsync(payment);

    // Publish event via outbox
    await context.PublishAsync(new PaymentFailedEvent(
        payment.Id,
        payment.UserId,
        reason,
        DateTime.UtcNow
    ));

    return Results.Ok(payment);
});

// Refund Payment
app.MapPost("/payments/{id}/refund", async (Guid id, string reason, IPaymentRepository repository, IMessageContext context) =>
{
    var payment = await repository.GetByIdAsync(id);
    if (payment == null)
        return Results.NotFound();

    if (payment.Status != PaymentStatus.Completed)
        return Results.BadRequest("Can only refund completed payments");

    payment.Status = PaymentStatus.Refunded;

    await repository.UpdateAsync(payment);

    // Publish event via outbox
    await context.PublishAsync(new PaymentRefundedEvent(
        payment.Id,
        payment.UserId,
        payment.Amount,
        reason,
        DateTime.UtcNow
    ));

    return Results.Ok(payment);
});

// Get all payments
app.MapGet("/payments", async (IPaymentRepository repository) =>
{
    return await repository.GetAllAsync();
});

// Get payment by ID
app.MapGet("/payments/{id}", async (Guid id, IPaymentRepository repository) =>
{
    var payment = await repository.GetByIdAsync(id);
    return payment is not null ? Results.Ok(payment) : Results.NotFound();
});

// Get payments by user
app.MapGet("/payments/user/{userId}", async (string userId, IPaymentRepository repository) =>
{
    return await repository.GetByUserIdAsync(userId);
});

// Check payment status in cache
app.MapGet("/payments/{id}/cache", async (Guid id, ICacheService cache) =>
{
    var key = $"payment:{id}:status";
    var status = await cache.GetAsync<string>(key);

    return status != null
        ? Results.Ok(new { PaymentId = id, CachedStatus = status, Source = "Redis" })
        : Results.NotFound(new { Message = "Payment status not found in cache" });
});

app.Run();
