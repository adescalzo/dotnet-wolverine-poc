# PoC 3: Transactional Outbox Pattern with Wolverine

## Overview

This PoC demonstrates the transactional outbox pattern using Wolverine with PostgreSQL (Marten) for guaranteed message delivery and Redis for caching.

## What is the Outbox Pattern?

The Outbox pattern ensures that database changes and message publishing happen atomically. Messages are stored in an "outbox" table within the same database transaction as the business data, then published asynchronously.

### The Problem It Solves

Without the outbox pattern:
```
1. Save payment to database ✅
2. App crashes 💥
3. Event never published ❌
4. Inconsistent state!
```

With the outbox pattern:
```
1. Save payment to database ✅
2. Save event to outbox table ✅
3. Commit transaction ✅
4. App crashes 💥
5. App restarts ✅
6. Outbox processor publishes event ✅
7. Consistent state!
```

## Features Demonstrated

- **Transactional Outbox** - Database and messages in single transaction
- **PostgreSQL with Marten** - Document database for payments
- **Redis Caching** - Fast payment status lookups
- **Event Processing** - Guaranteed at-least-once delivery
- **Domain Events** - PaymentInitiated, Processed, Failed, Refunded

## Project Structure

```
Events/
  └── PaymentEvents.cs         # Payment domain events
Handlers/
  └── PaymentEventHandlers.cs  # Event handlers with side effects
Models/
  └── Payment.cs               # Payment model, status enum, DTOs
Repositories/
  └── IPaymentRepository.cs    # Marten-based repository
Services/
  ├── ICacheService.cs         # Redis cache service
  └── INotificationService.cs  # Notification service
```

## Prerequisites

- Docker & Docker Compose
- .NET 10 SDK

## Running the PoC

### 1. Start Infrastructure

```bash
# From the root directory
docker-compose up -d postgres redis

# Verify services are running
docker-compose ps
```

Expected output:
```
wolverine-postgres    Up    5433->5432/tcp
wolverine-redis       Up    6380->6379/tcp
```

### 2. Run the Application

```bash
cd 03-Outbox-PoC/OutboxPoC
dotnet run
```

The application will:
- Connect to PostgreSQL
- Create database schema automatically
- Create outbox tables
- Start processing outbox messages
- Start the API on http://localhost:5000

## API Endpoints

### Create a Payment

```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user123",
    "amount": 150.00,
    "currency": "USD"
  }'
```

**Response:**
```json
{
  "id": "<payment-guid>",
  "userId": "user123",
  "amount": 150.00,
  "currency": "USD",
  "status": "Pending",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**What Happens:**
1. Payment saved to PostgreSQL
2. `PaymentInitiatedEvent` stored in outbox table (same transaction)
3. Transaction committed
4. Outbox processor publishes event
5. `PaymentInitiatedHandler` executes
6. Notification sent
7. Status cached in Redis

### Process a Payment

```bash
curl -X POST http://localhost:5000/payments/{id}/process
```

**What Happens:**
1. Payment updated to "Completed"
2. Transaction ID generated
3. `PaymentProcessedEvent` stored in outbox
4. Event published and handled
5. Success email sent
6. Redis cache updated

### Fail a Payment

```bash
curl -X POST "http://localhost:5000/payments/{id}/fail?reason=Insufficient%20funds"
```

### Refund a Payment

```bash
curl -X POST "http://localhost:5000/payments/{id}/refund?reason=Customer%20request"
```

### Get All Payments

```bash
curl http://localhost:5000/payments
```

### Get Payment by ID

```bash
curl http://localhost:5000/payments/{id}
```

### Get Payments by User

```bash
curl http://localhost:5000/payments/user/user123
```

### Check Cached Status

```bash
curl http://localhost:5000/payments/{id}/cache
```

**Response:**
```json
{
  "paymentId": "<guid>",
  "cachedStatus": "completed",
  "source": "Redis"
}
```

## Testing with HTTP Files

Use the included `test-api.http` file with VS Code REST Client extension.

## How It Works

### 1. Marten Configuration

```csharp
builder.Services.AddMarten(opts =>
{
    opts.Connection(postgresConnection);
    opts.DatabaseSchemaName = "outbox_poc";
})
.IntegrateWithWolverine();  // ← Critical for outbox!
```

The `.IntegrateWithWolverine()` call:
- Creates outbox tables
- Intercepts `IMessageContext.PublishAsync()`
- Stores messages in outbox
- Manages background processing

### 2. Publishing Events

```csharp
await repository.SaveAsync(payment);  // Database save

await context.PublishAsync(new PaymentInitiatedEvent(...));  // Outbox save

// Both happen in the same transaction!
```

### 3. Background Processing

Wolverine's outbox processor:
1. Polls the outbox table
2. Publishes pending messages
3. Marks them as processed
4. Handles failures with retries

### 4. Event Handling

```csharp
public class PaymentInitiatedHandler(
    INotificationService notifications,
    ICacheService cache)
{
    public async Task Handle(PaymentInitiatedEvent @event)
    {
        await notifications.SendAsync(...);
        await cache.SetAsync(...);
    }
}
```

## Database Schema

Wolverine creates these tables automatically:

```sql
outbox_poc.wolverine_outgoing_envelopes  -- Outbox messages
outbox_poc.wolverine_incoming_envelopes  -- Inbox for deduplication
outbox_poc.wolverine_dead_letters        -- Failed messages
outbox_poc.mt_doc_payment                -- Payment documents
```

### Viewing Outbox Messages

```bash
# Connect to PostgreSQL
psql -h localhost -p 5433 -U wolverine -d wolverine_db

# View outbox schema
\dt outbox_poc.*

# See pending messages
SELECT id, message_type, status, attempts
FROM outbox_poc.wolverine_outgoing_envelopes;

# See processed payments
SELECT data->>'Id' as id,
       data->>'UserId' as user_id,
       data->>'Amount' as amount,
       data->>'Status' as status
FROM outbox_poc.mt_doc_payment;
```

## Redis Cache

Event handlers cache payment status for fast lookups:

```bash
# Connect to Redis
redis-cli -p 6380

# View cached statuses
KEYS payment:*

# Get specific status
GET payment:<payment-id>:status
```

## Guaranteed Delivery Example

### Scenario: App Crashes During Processing

```
1. POST /payments (create payment)
2. Payment saved to DB ✅
3. Event saved to outbox ✅
4. Transaction committed ✅
5. App crashes before publishing 💥
6. App restarts ✅
7. Outbox processor finds unpublished event ✅
8. Event published ✅
9. Handler executes ✅
10. Message marked as processed ✅
```

**Result:** Zero message loss!

## Key Concepts

### Transactional Boundary

```csharp
// ✅ CORRECT - Both in transaction
await using var session = _store.LightweightSession();
session.Store(payment);
await session.SaveChangesAsync();  // Commits payment + outbox

// ❌ WRONG - Outside transaction
await repository.SaveAsync(payment);
await bus.PublishAsync(event);  // Not atomic!
```

### Durable Inbox

Prevents duplicate processing:
```
1. Event processed ✅
2. Event ID stored in inbox table ✅
3. Same event received again (retry)
4. ID already in inbox
5. Event skipped (no duplicate processing)
```

### At-Least-Once Delivery

- Messages guaranteed to be delivered
- May be delivered more than once
- Handlers should be idempotent

## Use Cases

- **Payment Processing** - Financial transactions
- **Order Management** - E-commerce workflows
- **Account Operations** - Banking systems
- **Inventory Management** - Stock updates with notifications
- **Any Critical Operation** - Where consistency matters

## Comparison: With vs Without Outbox

| Scenario | Without Outbox | With Outbox |
|----------|---------------|-------------|
| DB saved, event published | ✅ | ✅ |
| DB saved, app crashes | ❌ Lost event | ✅ Event published on restart |
| DB fails, event published | ❌ Orphan event | ✅ Transaction rolled back |
| Network failure | ❌ Lost event | ✅ Retried automatically |
| Duplicate events | ❌ Possible | ✅ Prevented by inbox |

## Performance Considerations

- **Polling Interval**: Wolverine polls every 5 seconds by default
- **Batch Size**: Processes multiple messages per poll
- **Indexing**: Outbox table indexed for fast queries
- **Cleanup**: Old messages can be archived/deleted

## Troubleshooting

### Events Not Processing

```bash
# Check outbox table
psql -h localhost -p 5433 -U wolverine -d wolverine_db
SELECT * FROM outbox_poc.wolverine_outgoing_envelopes
WHERE status = 'pending';
```

### Check Logs

```bash
dotnet run | grep Wolverine
```

### Dead Letters

```bash
# View failed messages
SELECT * FROM outbox_poc.wolverine_dead_letters;
```

## Next Steps

1. Implement custom retry policies
2. Add message versioning
3. Implement saga patterns for complex workflows
4. Add monitoring and alerting
5. Implement message archival
6. Scale with multiple processor instances
7. Add OpenTelemetry tracing

## Resources

- [Wolverine Persistence](https://wolverinefx.net/guide/durability/)
- [Marten Documentation](https://martendb.io/)
- [Outbox Pattern Explained](https://microservices.io/patterns/data/transactional-outbox.html)
- [Transactional Messaging](https://www.enterpriseintegrationpatterns.com/patterns/messaging/TransactionalClient.html)
