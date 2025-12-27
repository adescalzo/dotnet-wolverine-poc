# Wolverine C# PoC - Detailed Documentation

## Table of Contents

1. [Introduction](#introduction)
2. [Technology Stack](#technology-stack)
3. [Prerequisites](#prerequisites)
4. [Project Structure](#project-structure)
5. [PoC 1: Mediator Pattern](#poc-1-mediator-pattern)
6. [PoC 2: Messaging & Domain Events](#poc-2-messaging--domain-events)
7. [PoC 3: Outbox Pattern](#poc-3-outbox-pattern)
8. [Infrastructure Setup](#infrastructure-setup)
9. [Running the Projects](#running-the-projects)
10. [Key Learnings](#key-learnings)

## Introduction

This repository contains three Proof of Concept (PoC) projects demonstrating Wolverine's capabilities as a modern .NET framework for building maintainable and scalable applications. Wolverine provides:

- **Mediator Pattern** - In-process messaging for decoupled components
- **Messaging Infrastructure** - Pub/Sub with multiple transports (in-memory, RabbitMQ)
- **Outbox Pattern** - Guaranteed message delivery with transactional consistency

## Technology Stack

- **.NET 10** - Latest .NET version with C# 13
- **Wolverine 5.9.2** - Application framework for .NET
- **PostgreSQL 17** - Relational database for persistence and outbox
- **Marten 8.16.4** - Document database built on PostgreSQL
- **RabbitMQ 3** - Message broker for distributed messaging
- **Redis 7** - In-memory cache and distributed locking
- **ASP.NET Core** - Web framework for API endpoints

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker & Docker Compose](https://www.docker.com/get-started) (for infrastructure services)
- Terminal/Command Line
- Code editor (VS Code, Visual Studio, Rider, etc.)

## Project Structure

```
wolverine/
├── 01-Mediator-PoC/              # Mediator pattern demonstration
│   └── MediatorPoC/
│       ├── Commands/              # Command definitions
│       ├── Queries/               # Query definitions
│       ├── Handlers/              # Command and Query handlers
│       ├── Models/                # Domain models
│       └── Repositories/          # Data access layer
│
├── 02-Messaging-PoC/             # Messaging and domain events
│   └── MessagingPoC/
│       ├── Events/                # Domain event definitions
│       ├── Handlers/              # Event handlers
│       ├── Models/                # Domain models (Order, etc.)
│       ├── Repositories/          # Data repositories
│       └── Services/              # Application services
│
├── 03-Outbox-PoC/                # Outbox pattern with PostgreSQL
│   └── OutboxPoC/
│       ├── Events/                # Payment-related events
│       ├── Handlers/              # Event handlers
│       ├── Models/                # Payment models
│       ├── Repositories/          # Marten-based repositories
│       └── Services/              # Cache and notification services
│
└── docker-compose.yml            # Infrastructure services
```

## PoC 1: Mediator Pattern

### Overview

Demonstrates Wolverine's mediator pattern for in-process messaging using CQRS (Command Query Responsibility Segregation).

### Features

- **Commands**: CreateUser, UpdateUser, SendUserNotification
- **Queries**: GetUserById, GetAllUsers
- **Handlers**: Automatic discovery and dependency injection
- **Synchronous & Asynchronous**: Both sync and async handler patterns

### File Structure

```
Commands/
  └── UserCommands.cs          # Command definitions
Queries/
  └── UserQueries.cs           # Query definitions
Handlers/
  ├── UserCommandHandlers.cs   # Command handlers
  └── UserQueryHandlers.cs     # Query handlers
Models/
  └── User.cs                  # User model and DTO
Repositories/
  └── IUserRepository.cs       # Repository interface and implementation
```

### Running

```bash
cd 01-Mediator-PoC/
dotnet run
```

### Expected Output

The console application will:
1. Create a user and return the ID
2. Query the user by ID
3. Create additional users
4. Get all users
5. Update a user
6. Verify the update
7. Send an async notification

### Key Concepts

- **IMessageBus.InvokeAsync()** - Synchronous request/response
- **IMessageBus.SendAsync()** - Fire and forget async messaging
- **Handler Discovery** - Wolverine automatically finds and registers handlers
- **Dependency Injection** - Handlers receive dependencies via constructor

## PoC 2: Messaging & Domain Events

### Overview

Demonstrates event-driven architecture with domain events using both in-memory and RabbitMQ transports.

### Features

- **Domain Events**: OrderCreated, OrderShipped, OrderCancelled
- **Event Handlers**: Automatic event processing with side effects
- **Multiple Transports**: In-memory (default) and RabbitMQ (configurable)
- **REST API**: HTTP endpoints to trigger domain events
- **Pub/Sub Pattern**: One event, multiple handlers

### File Structure

```
Events/
  └── OrderEvents.cs           # Domain event definitions
Handlers/
  └── OrderEventHandlers.cs    # Event handler implementations
Models/
  └── Order.cs                 # Order model and DTOs
Repositories/
  └── IOrderRepository.cs      # Order repository
Services/
  └── INotificationService.cs  # Notification service
```

### Configuration

Edit `appsettings.json` to switch between transports:

```json
{
  "UseRabbitMQ": false,          // true for RabbitMQ, false for in-memory
  "RabbitMQ": {
    "Url": "amqp://guest:guest@localhost:5673"
  }
}
```

### Running

#### In-Memory Mode (No dependencies)

```bash
cd 02-Messaging-PoC/MessagingPoC
dotnet run
```

#### RabbitMQ Mode (Requires Docker)

```bash
# Start RabbitMQ
docker-compose up -d rabbitmq

# Update appsettings.json: "UseRabbitMQ": true
cd 02-Messaging-PoC/MessagingPoC
dotnet run
```

### API Endpoints

```bash
# Create an order
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName": "John Doe", "items": ["Item1", "Item2"], "totalAmount": 99.99}'

# Ship an order
curl -X POST http://localhost:5000/orders/{id}/ship

# Cancel an order
curl -X POST http://localhost:5000/orders/{id}/cancel

# Get all orders
curl http://localhost:5000/orders
```

### Key Concepts

- **Domain Events** - Events representing state changes
- **Event Handlers** - Side effect execution (notifications, integrations)
- **Transport Abstraction** - Same code, different transports
- **Durable Outbox** - Guaranteed event delivery with RabbitMQ

## PoC 3: Outbox Pattern

### Overview

Demonstrates the transactional outbox pattern for guaranteed message delivery using PostgreSQL (Marten) and Redis for caching.

### Features

- **Transactional Outbox** - Database and event publishing in single transaction
- **PostgreSQL with Marten** - Document database for payment storage
- **Redis Caching** - Fast status lookups
- **Event Processing** - Guaranteed at-least-once delivery
- **Domain Events**: PaymentInitiated, PaymentProcessed, PaymentFailed, PaymentRefunded

### File Structure

```
Events/
  └── PaymentEvents.cs         # Payment domain events
Handlers/
  └── PaymentEventHandlers.cs  # Payment event handlers
Models/
  └── Payment.cs               # Payment model and DTOs
Repositories/
  └── IPaymentRepository.cs    # Marten-based repository
Services/
  ├── ICacheService.cs         # Redis cache service
  └── INotificationService.cs  # Notification service
```

### Running

#### 1. Start Infrastructure

```bash
# Start PostgreSQL and Redis
docker-compose up -d postgres redis

# Verify services are running
docker-compose ps
```

#### 2. Run the Application

```bash
cd 03-Outbox-PoC/OutboxPoC
dotnet run
```

The application will automatically:
- Create the database schema
- Create outbox tables
- Start processing outbox messages

### API Endpoints

```bash
# Create a payment (triggers PaymentInitiatedEvent)
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{"userId": "user123", "amount": 100.00, "currency": "USD"}'

# Process a payment (triggers PaymentProcessedEvent)
curl -X POST http://localhost:5000/payments/{id}/process

# Fail a payment (triggers PaymentFailedEvent)
curl -X POST "http://localhost:5000/payments/{id}/fail?reason=Insufficient+funds"

# Refund a payment (triggers PaymentRefundedEvent)
curl -X POST "http://localhost:5000/payments/{id}/refund?reason=Customer+request"

# Get all payments
curl http://localhost:5000/payments

# Check cached payment status
curl http://localhost:5000/payments/{id}/cache
```

### How the Outbox Pattern Works

1. **Transaction Starts**: When creating a payment, a database transaction begins
2. **Entity Saved**: Payment is saved to the database
3. **Event Stored**: Event is stored in the outbox table (same transaction)
4. **Transaction Commits**: Both payment and event are committed atomically
5. **Background Processing**: Wolverine reads from outbox and publishes events
6. **Event Handled**: Event handlers execute (notifications, cache updates, etc.)
7. **Guaranteed Delivery**: If app crashes, events are still published on restart

### Key Concepts

- **Transactional Outbox** - Messages stored in DB, published asynchronously
- **IMessageContext.PublishAsync()** - Publishes events to outbox
- **.IntegrateWithWolverine()** - Marten integration for outbox
- **Durable Inbox** - Deduplication of message processing
- **At-Least-Once Delivery** - Messages guaranteed to be processed

## Infrastructure Setup

### Docker Compose Services

The `docker-compose.yml` file provides all required infrastructure:

```yaml
Services:
  - RabbitMQ:     localhost:5673 (AMQP), localhost:15673 (Management UI)
  - PostgreSQL:   localhost:5433 (Database: wolverine_db)
  - Redis:        localhost:6380
```

Note: Custom ports are used to avoid conflicts with local services.

### Starting Services

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d postgres

# View logs
docker-compose logs -f rabbitmq

# Stop all services
docker-compose down

# Remove volumes (clean slate)
docker-compose down -v
```

### Accessing Services

**RabbitMQ Management UI**
- URL: http://localhost:15673
- Username: guest
- Password: guest

**PostgreSQL**
```bash
# Using psql
psql -h localhost -p 5433 -U wolverine -d wolverine_db
# Password: wolverine_pass

# View outbox tables
\dt outbox_poc.*
```

**Redis**
```bash
# Using redis-cli
redis-cli -p 6380

# View all keys
KEYS *

# Get payment status
GET payment:{payment-id}:status
```

## Running the Projects

### Quick Start (All PoCs)

```bash
# 1. Clone and navigate
cd wolverine

# 2. Start infrastructure
docker-compose up -d

# 3. Run PoC 1 (Mediator)
dotnet run --project 01-Mediator-PoC/MediatorPoC/MediatorPoC.csproj

# 4. Run PoC 2 (Messaging) - separate terminal
dotnet run --project 02-Messaging-PoC/MessagingPoC/MessagingPoC.csproj

# 5. Run PoC 3 (Outbox) - separate terminal
dotnet run --project 03-Outbox-PoC/OutboxPoC/OutboxPoC.csproj
```

### Testing the APIs

PoC 2 and 3 expose REST APIs. You can test them using:

1. **curl** (shown in examples above)
2. **Postman/Insomnia** - Import the endpoints
3. **Swagger UI** - Available at http://localhost:5000/swagger (when running)
4. **HTTP files** - Use VS Code REST Client extension

## Key Learnings

### When to Use Each Pattern

**Mediator Pattern (PoC 1)**
- In-process messaging within a single application
- CQRS implementation
- Decoupling handlers from controllers
- Request/response communication

**Messaging & Domain Events (PoC 2)**
- Event-driven architecture
- Loosely coupled components
- Scalable microservices
- Cross-service communication

**Outbox Pattern (PoC 3)**
- Distributed transactions
- Guaranteed message delivery
- Payment processing, financial transactions
- Critical business operations requiring consistency

### Wolverine Advantages

1. **Convention-based** - Minimal configuration, automatic discovery
2. **Flexible** - Multiple transports (in-memory, RabbitMQ, PostgreSQL, etc.)
3. **Reliable** - Built-in support for outbox/inbox patterns
4. **Performant** - Runtime code generation for zero reflection
5. **Integrated** - Works seamlessly with ASP.NET Core, Marten, etc.

### Best Practices

1. **Use Primary Constructors** - C# 12+ feature for cleaner code
2. **Organize by Feature** - Group related files (events, handlers, models)
3. **Leverage DI** - Let Wolverine handle handler instantiation
4. **Test with In-Memory** - Develop locally without external dependencies
5. **Deploy with Durable** - Use PostgreSQL/RabbitMQ in production

### Common Pitfalls

1. **Not Starting Host** - Wolverine requires `IHost.StartAsync()`
2. **Missing Integration** - Remember `.IntegrateWithWolverine()` for Marten
3. **Transaction Boundaries** - Understand when outbox messages are committed
4. **Message Ordering** - RabbitMQ doesn't guarantee order across queues
5. **Handler Exceptions** - Implement proper error handling and retries

## Additional Resources

- [Wolverine Documentation](https://wolverinefx.net/)
- [Wolverine Tutorials](https://wolverinefx.net/tutorials/)
- [Marten Documentation](https://martendb.io/)
- [Outbox Pattern Explained](https://microservices.io/patterns/data/transactional-outbox.html)

## Conclusion

These three PoCs demonstrate Wolverine's capabilities across different scenarios:

1. **Mediator** - Clean in-process messaging
2. **Messaging** - Scalable event-driven architecture
3. **Outbox** - Reliable distributed systems

Wolverine provides a unified programming model for all three patterns, making it an excellent choice for building maintainable and scalable .NET applications.

---

**Next Steps:**
1. Explore additional Wolverine features (sagas, scheduled jobs)
2. Implement custom error handling and retry policies
3. Add monitoring and observability (OpenTelemetry)
4. Scale out with multiple instances
5. Integrate with real external systems
