# PoC 1: Mediator Pattern with Wolverine

## Overview

This PoC demonstrates Wolverine's mediator pattern for in-process messaging using CQRS (Command Query Responsibility Segregation).

## What is the Mediator Pattern?

The Mediator pattern decouples request senders from request handlers by introducing a mediator object. In Wolverine, the `IMessageBus` acts as the mediator, routing commands and queries to their respective handlers.

## Features Demonstrated

- **Commands** - Write operations that modify state
- **Queries** - Read operations that return data
- **Synchronous Handlers** - Immediate response handlers
- **Asynchronous Handlers** - Background processing handlers
- **Dependency Injection** - Automatic handler resolution

## Project Structure

```
Commands/
  └── UserCommands.cs          # CreateUser, UpdateUser, SendNotification
Queries/
  └── UserQueries.cs           # GetUserById, GetAllUsers
Handlers/
  ├── UserCommandHandlers.cs   # Command handlers
  └── UserQueryHandlers.cs     # Query handlers
Models/
  └── User.cs                  # User model and DTO
Repositories/
  └── IUserRepository.cs       # In-memory repository
```

## Running the PoC

```bash
dotnet run
```

## Expected Output

```
=== Wolverine Mediator Pattern PoC ===

1. Creating a new user...
   [Handler] User created: John Doe
   User created with ID: <guid>

2. Querying user by ID...
   [Handler] Fetching user: John Doe
   Found: John Doe (john.doe@example.com)

3. Creating additional users...
   [Handler] User created: Jane Smith
   [Handler] User created: Bob Johnson
   Users created

4. Getting all users...
   [Handler] Fetching 3 users
   - John Doe (john.doe@example.com)
   - Jane Smith (jane.smith@example.com)
   - Bob Johnson (bob.johnson@example.com)

5. Updating user...
   [Handler] User updated: John Doe Updated
   User updated

6. Verifying update...
   [Handler] Fetching user: John Doe Updated
   Updated: John Doe Updated (john.updated@example.com)

7. Sending notification (fire and forget)...
   Notification sent asynchronously
   [Handler] Notification sent to john.updated@example.com: Welcome to the system!

=== PoC Completed ===
```

## Key Concepts

### IMessageBus Methods

- **InvokeAsync<TResponse>(message)** - Request/response pattern, waits for result
- **InvokeAsync(message)** - Fire command without result
- **SendAsync(message)** - Fire and forget, asynchronous processing

### Handler Conventions

Wolverine discovers handlers automatically. A handler class needs:
- A public `Handle` method
- Parameter matching the message type
- Optional return type for queries

```csharp
public class CreateUserHandler(IUserRepository repository)
{
    public Guid Handle(CreateUserCommand command)
    {
        // Implementation
        return userId;
    }
}
```

## Use Cases

- **In-process messaging** - Within a single application
- **CQRS implementation** - Separate read and write operations
- **Clean Architecture** - Decouple controllers from business logic
- **Testability** - Easy to mock and unit test handlers

## Next Steps

1. Add validation with `IValidator<T>`
2. Implement middleware/interceptors
3. Add error handling with `IExecutionContext`
4. Explore cascading messages (one handler triggering another message)
5. Add logging and metrics
