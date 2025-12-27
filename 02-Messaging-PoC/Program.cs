using MessagingPoC.Events;
using MessagingPoC.Models;
using MessagingPoC.Repositories;
using MessagingPoC.Services;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add Wolverine with messaging configuration
builder.Host.UseWolverine(opts =>
{
    // Configure based on environment variable or appsettings
    var useRabbitMQ = builder.Configuration.GetValue<bool>("UseRabbitMQ", false);
    var rabbitMqUrl = builder.Configuration.GetValue<string>("RabbitMQ:Url") ?? "amqp://localhost";

    if (useRabbitMQ)
    {
        Console.WriteLine($"Configuring RabbitMQ transport at {rabbitMqUrl}");

        // Configure RabbitMQ transport
        opts.UseRabbitMq(rabbit =>
        {
            rabbit.HostName = new Uri(rabbitMqUrl).Host;
            rabbit.Port = new Uri(rabbitMqUrl).Port;
        })
        .AutoProvision()  // Automatically create queues and exchanges
        .AutoPurgeOnStartup();  // Clear queues on startup (for PoC only)

        // Publish domain events to RabbitMQ
        opts.PublishMessage<OrderCreatedEvent>()
            .ToRabbitExchange("orders")
            .UseDurableOutbox();

        opts.PublishMessage<OrderShippedEvent>()
            .ToRabbitExchange("orders")
            .UseDurableOutbox();

        opts.PublishMessage<OrderCancelledEvent>()
            .ToRabbitExchange("orders")
            .UseDurableOutbox();

        // Listen to queues
        opts.ListenToRabbitQueue("order-events")
            .ProcessInline();
    }
    else
    {
        Console.WriteLine("Using in-memory transport");
        // Default in-memory transport for local development
        // Messages are handled immediately in-process
    }
});

// Add services to the container
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
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
    Title = "Wolverine Messaging/Domain Events PoC",
    Endpoints = new[]
    {
        "POST /orders - Create a new order",
        "POST /orders/{id}/ship - Ship an order",
        "POST /orders/{id}/cancel - Cancel an order",
        "GET /orders - Get all orders",
        "GET /orders/{id} - Get order by ID"
    },
    TransportMode = builder.Configuration.GetValue<bool>("UseRabbitMQ", false) ? "RabbitMQ" : "In-Memory"
});

// Create Order - Publishes OrderCreatedEvent
app.MapPost("/orders", async (CreateOrderRequest request, IMessageBus bus, IOrderRepository repository) =>
{
    var order = new Order
    {
        Id = Guid.NewGuid(),
        CustomerName = request.CustomerName,
        Items = request.Items,
        TotalAmount = request.TotalAmount,
        Status = OrderStatus.Created,
        CreatedAt = DateTime.UtcNow
    };

    repository.Add(order);

    // Publish domain event using Wolverine
    var orderEvent = new OrderCreatedEvent(
        order.Id,
        order.CustomerName,
        order.Items.Count,
        order.TotalAmount,
        order.CreatedAt
    );

    await bus.PublishAsync(orderEvent);

    return Results.Created($"/orders/{order.Id}", order);
});

// Ship Order - Publishes OrderShippedEvent
app.MapPost("/orders/{id}/ship", async (Guid id, IMessageBus bus, IOrderRepository repository) =>
{
    var order = repository.GetById(id);
    if (order == null)
        return Results.NotFound();

    order.Status = OrderStatus.Shipped;
    order.ShippedAt = DateTime.UtcNow;
    repository.Update(order);

    // Publish domain event
    await bus.PublishAsync(new OrderShippedEvent(order.Id, order.CustomerName, order.ShippedAt.Value));

    return Results.Ok(order);
});

// Cancel Order - Publishes OrderCancelledEvent
app.MapPost("/orders/{id}/cancel", async (Guid id, IMessageBus bus, IOrderRepository repository) =>
{
    var order = repository.GetById(id);
    if (order == null)
        return Results.NotFound();

    order.Status = OrderStatus.Cancelled;
    repository.Update(order);

    // Publish domain event
    await bus.PublishAsync(new OrderCancelledEvent(order.Id, order.CustomerName, "Customer request"));

    return Results.Ok(order);
});

// Get all orders
app.MapGet("/orders", (IOrderRepository repository) =>
{
    return repository.GetAll();
});

// Get order by ID
app.MapGet("/orders/{id}", (Guid id, IOrderRepository repository) =>
{
    var order = repository.GetById(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.Run();
