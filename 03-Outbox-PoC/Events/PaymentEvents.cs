namespace OutboxPoC.Events;

// Event published when a payment is initiated
public record PaymentInitiatedEvent(
    Guid PaymentId,
    string UserId,
    decimal Amount,
    string Currency,
    DateTime InitiatedAt
);

// Event published when payment processing is complete
public record PaymentProcessedEvent(
    Guid PaymentId,
    string UserId,
    decimal Amount,
    string TransactionId,
    DateTime ProcessedAt
);

// Event published when payment fails
public record PaymentFailedEvent(
    Guid PaymentId,
    string UserId,
    string Reason,
    DateTime FailedAt
);

// Event published when payment is refunded
public record PaymentRefundedEvent(
    Guid PaymentId,
    string UserId,
    decimal Amount,
    string Reason,
    DateTime RefundedAt
);
