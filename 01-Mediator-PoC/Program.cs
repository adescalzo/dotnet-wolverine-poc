using MediatorPoC.Commands;
using MediatorPoC.Models;
using MediatorPoC.Queries;
using MediatorPoC.Repositories;
using MediatorPoC.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

// Build and configure the host
var builder = Host.CreateApplicationBuilder(args);

// Add Wolverine with mediator functionality
builder.Services.AddWolverine(opts =>
{
    // Wolverine will automatically discover handlers in this assembly
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    // Configure pipelines using policies
    // Policies determine which middleware applies to which handlers
    opts.Policies.Add<LoggingPolicy>();        // Apply logging to ALL handlers
    opts.Policies.Add<TransactionPolicy>();    // Apply transactions to Commands only
});

// Register application services
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var host = builder.Build();

// Start the host
await host.StartAsync();

Console.WriteLine("=== Wolverine Mediator Pattern with Pipelines PoC ===\n");

// Get the message bus (Wolverine's mediator)
var bus = host.Services.GetRequiredService<IMessageBus>();

try
{
    // Example 1: Create a new user (Command)
    Console.WriteLine("1. Creating a new user...");
    var createCommand = new CreateUserCommand("John Doe", "john.doe@example.com");
    var userId = await bus.InvokeAsync<Guid>(createCommand);
    Console.WriteLine($"   User created with ID: {userId}\n");

    // Example 2: Query the user (Query)
    Console.WriteLine("2. Querying user by ID...");
    var getUserQuery = new GetUserByIdQuery(userId);
    var user = await bus.InvokeAsync<UserDto?>(getUserQuery);
    if (user != null)
    {
        Console.WriteLine($"   Found: {user.Name} ({user.Email})\n");
    }

    // Example 3: Create more users
    Console.WriteLine("3. Creating additional users...");
    await bus.InvokeAsync<Guid>(new CreateUserCommand("Jane Smith", "jane.smith@example.com"));
    await bus.InvokeAsync<Guid>(new CreateUserCommand("Bob Johnson", "bob.johnson@example.com"));
    Console.WriteLine("   Users created\n");

    // Example 4: Get all users (Query)
    Console.WriteLine("4. Getting all users...");
    var getAllQuery = new GetAllUsersQuery();
    var users = await bus.InvokeAsync<IEnumerable<UserDto>>(getAllQuery);
    foreach (var u in users)
    {
        Console.WriteLine($"   - {u.Name} ({u.Email})");
    }
    Console.WriteLine();

    // Example 5: Update user (Command with validation)
    Console.WriteLine("5. Updating user...");
    var updateCommand = new UpdateUserCommand(userId, "John Doe Updated", "john.updated@example.com");
    await bus.InvokeAsync(updateCommand);
    Console.WriteLine("   User updated\n");

    // Example 6: Verify update
    Console.WriteLine("6. Verifying update...");
    var updatedUser = await bus.InvokeAsync<UserDto?>(new GetUserByIdQuery(userId));
    if (updatedUser != null)
    {
        Console.WriteLine($"   Updated: {updatedUser.Name} ({updatedUser.Email})\n");
    }

    // Example 7: Fire and forget command (async processing)
    Console.WriteLine("7. Sending notification (fire and forget)...");
    await bus.SendAsync(new SendUserNotificationCommand(userId, "Welcome to the system!"));
    Console.WriteLine("   Notification sent asynchronously\n");

    // Give async processing a moment
    await Task.Delay(100);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.WriteLine("=== PoC Completed ===");

await host.StopAsync();
