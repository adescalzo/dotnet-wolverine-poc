# PoC 2: Messaging & Domain Events with Wolverine

## Overview

This PoC demonstrates event-driven architecture with Wolverine, supporting both in-memory and RabbitMQ transports.

## What are Domain Events?

Domain events represent something significant that happened in your domain. They're published when state changes and handled by one or more subscribers.

## Features Demonstrated

- **Domain Events** - OrderCreated, OrderShipped, OrderCancelled
- **Event Handlers** - Multiple handlers can subscribe to the same event
- **Transport Abstraction** - Switch between in-memory and RabbitMQ without code changes
- **REST API** - HTTP endpoints trigger domain events
- **Pub/Sub Pattern** - One publisher, multiple subscribers

## Project Structure

```
Events/
  └── OrderEvents.cs           # Event definitions
Handlers/
  └── OrderEventHandlers.cs    # Event handler implementations
Models/
  └── Order.cs                 # Order model, status enum, DTOs
Repositories/
  └── IOrderRepository.cs      # In-memory order storage
Services/
  └── INotificationService.cs  # Notification service
```

## Configuration

Edit `appsettings.json`:

```json
{
  "UseRabbitMQ": false,  // Set to true for RabbitMQ transport
  "RabbitMQ": {
    "Url": "amqp://guest:guest@localhost:5673"
  }
}
```

## Running the PoC

### In-Memory Mode (Default)

```bash
dotnet run
```

No external dependencies required! Perfect for development and testing.

### RabbitMQ Mode

1. Start RabbitMQ:
   ```bash
   cd ../..
   docker-compose up -d rabbitmq
   ```

2. Update `appsettings.json`: Set `"UseRabbitMQ": true`

3. Run the application:
   ```bash
   dotnet run
   ```

4. Access RabbitMQ Management UI: http://localhost:15673
   - Username: guest
   - Password: guest

## API Endpoints

The API will start on `http://localhost:5000`

### Create an Order

```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "items": ["Laptop", "Mouse", "Keyboard"],
    "totalAmount": 1299.99
  }'
```

**What Happens:**
1. Order is saved to repository
2. `OrderCreatedEvent` is published
3. `OrderCreatedHandler` executes asynchronously
4. Notification is sent to customer
5. Status is cached

### Ship an Order

```bash
curl -X POST http://localhost:5000/orders/{orderId}/ship
```

**What Happens:**
1. Order status updated to "Shipped"
2. `OrderShippedEvent` is published
3. Shipping notification sent
4. Could trigger: tracking number generation, carrier API call, etc.

### Cancel an Order

```bash
curl -X POST http://localhost:5000/orders/{orderId}/cancel
```

**What Happens:**
1. Order status updated to "Cancelled"
2. `OrderCancelledEvent` is published
3. Cancellation notification sent
4. Could trigger: refund processing, inventory release, etc.

### Get All Orders

```bash
curl http://localhost:5000/orders
```

### Get Order by ID

```bash
curl http://localhost:5000/orders/{orderId}
```

## Testing with HTTP Files

Use the included `test-api.http` file with VS Code REST Client extension:

1. Install VS Code extension: "REST Client"
2. Open `test-api.http`
3. Click "Send Request" above each request

## In-Memory vs RabbitMQ

### In-Memory Transport

**Pros:**
- No external dependencies
- Fast for development and testing
- Simple configuration
- Immediate execution

**Cons:**
- Not durable (messages lost if app restarts)
- Single process only
- No distribution across multiple instances

**Use for:**
- Local development
- Unit/integration testing
- Simple applications
- Proof of concepts

### RabbitMQ Transport

**Pros:**
- Durable message storage
- Distributed across multiple instances
- Message persistence survives restarts
- Production-ready scalability

**Cons:**
- Requires RabbitMQ server
- More complex setup
- Slight latency overhead

**Use for:**
- Production environments
- Microservices architecture
- High availability requirements
- Cross-service communication

## Event Flow Example

```
1. HTTP POST /orders
   ↓
2. Order saved to repository
   ↓
3. OrderCreatedEvent published via Wolverine
   ↓
4. Event routed to:
   - In-Memory: Local queue
   - RabbitMQ: "orders" exchange
   ↓
5. OrderCreatedHandler.Handle() executed
   ↓
6. Side effects:
   - Notification sent
   - Could trigger: inventory, payment, etc.
```

## Key Concepts

### Publishing Events

```csharp
await bus.PublishAsync(new OrderCreatedEvent(...));
```

Wolverine routes the event to all registered handlers.

### Handling Events

```csharp
public class OrderCreatedHandler(INotificationService notifications)
{
    public async Task Handle(OrderCreatedEvent @event)
    {
        // Side effects here
        await notifications.SendAsync(...);
    }
}
```

Multiple handlers can process the same event!

### Configuration

```csharp
builder.Host.UseWolverine(opts =>
{
    if (useRabbitMQ)
    {
        opts.PublishMessage<OrderCreatedEvent>()
            .ToRabbitExchange("orders");
    }
});
```

## Use Cases

- **Event-Driven Architecture** - Loosely coupled components
- **Microservices** - Cross-service communication
- **CQRS** - Separate commands from events
- **Integration** - Connect with external systems
- **Audit Logging** - Track all state changes

## Comparison with Other Frameworks

| Feature | Wolverine | MediatR | MassTransit |
|---------|-----------|---------|-------------|
| In-process | ✅ | ✅ | ❌ |
| Distributed | ✅ | ❌ | ✅ |
| Transports | Multiple | N/A | Multiple |
| Outbox | Built-in | Manual | Built-in |
| Performance | High | High | Medium |
| Learning Curve | Medium | Low | High |

## Next Steps

1. Add more event types (OrderDelivered, OrderReturned)
2. Implement saga patterns for complex workflows
3. Add error handling and retries
4. Implement dead letter queues
5. Add monitoring and observability
6. Scale horizontally with multiple instances
