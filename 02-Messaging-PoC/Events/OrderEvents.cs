namespace MessagingPoC.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    int ItemCount,
    decimal TotalAmount,
    DateTime CreatedAt
);

public record OrderShippedEvent(
    Guid OrderId,
    string CustomerName,
    DateTime ShippedAt
);

public record OrderCancelledEvent(
    Guid OrderId,
    string CustomerName,
    string Reason
);
