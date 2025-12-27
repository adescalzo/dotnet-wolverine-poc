using MessagingPoC.Events;
using MessagingPoC.Services;

namespace MessagingPoC.Handlers;

// Handler for OrderCreatedEvent
public class OrderCreatedHandler(INotificationService notificationService)
{
    public async Task Handle(OrderCreatedEvent @event)
    {
        Console.WriteLine($"[Event Handler] Order created: {@event.OrderId} for {@event.CustomerName}");

        // Send notification to customer
        await notificationService.SendAsync(
            @event.CustomerName,
            $"Your order #{@event.OrderId.ToString()[..8]} has been created with {@event.ItemCount} items. Total: ${@event.TotalAmount}"
        );

        // Could trigger other actions: inventory reservation, payment processing, etc.
    }
}

// Handler for OrderShippedEvent
public class OrderShippedHandler(INotificationService notificationService)
{
    public async Task Handle(OrderShippedEvent @event)
    {
        Console.WriteLine($"[Event Handler] Order shipped: {@event.OrderId} for {@event.CustomerName}");

        await notificationService.SendAsync(
            @event.CustomerName,
            $"Great news! Your order #{@event.OrderId.ToString()[..8]} has been shipped!"
        );
    }
}

// Handler for OrderCancelledEvent
public class OrderCancelledHandler(INotificationService notificationService)
{
    public async Task Handle(OrderCancelledEvent @event)
    {
        Console.WriteLine($"[Event Handler] Order cancelled: {@event.OrderId} - Reason: {@event.Reason}");

        await notificationService.SendAsync(
            @event.CustomerName,
            $"Your order #{@event.OrderId.ToString()[..8]} has been cancelled. Reason: {@event.Reason}"
        );

        // Could trigger: refund processing, inventory release, etc.
    }
}
