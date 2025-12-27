using OutboxPoC.Events;
using OutboxPoC.Services;

namespace OutboxPoC.Handlers;

// Handler for PaymentInitiatedEvent
public class PaymentInitiatedHandler(
    INotificationService notificationService,
    ICacheService cacheService)
{
    public async Task Handle(PaymentInitiatedEvent @event)
    {
        Console.WriteLine($"[Event Handler] Payment initiated: {@event.PaymentId} for user {@event.UserId}");

        // Send notification
        await notificationService.SendPaymentNotificationAsync(
            @event.UserId,
            $"Payment of {@event.Amount} {@event.Currency} initiated"
        );

        // Cache the payment status
        await cacheService.SetAsync(
            $"payment:{@event.PaymentId}:status",
            "initiated",
            TimeSpan.FromHours(1)
        );

        // Simulate integration: Could trigger external payment gateway here
        Console.WriteLine($"[Integration] Forwarding payment {@event.PaymentId} to payment gateway...");
    }
}

// Handler for PaymentProcessedEvent
public class PaymentProcessedHandler(
    INotificationService notificationService,
    ICacheService cacheService)
{
    public async Task Handle(PaymentProcessedEvent @event)
    {
        Console.WriteLine($"[Event Handler] Payment processed: {@event.PaymentId} with transaction {@event.TransactionId}");

        // Send success notification
        await notificationService.SendEmailAsync(
            @event.UserId,
            "Payment Successful",
            $"Your payment of {@event.Amount} has been processed. Transaction ID: {@event.TransactionId}"
        );

        // Update cache
        await cacheService.SetAsync(
            $"payment:{@event.PaymentId}:status",
            "completed",
            TimeSpan.FromHours(24)
        );

        // Could trigger: Receipt generation, loyalty points, etc.
        Console.WriteLine($"[Integration] Generating receipt for payment {@event.PaymentId}...");
    }
}

// Handler for PaymentFailedEvent
public class PaymentFailedHandler(
    INotificationService notificationService,
    ICacheService cacheService)
{
    public async Task Handle(PaymentFailedEvent @event)
    {
        Console.WriteLine($"[Event Handler] Payment failed: {@event.PaymentId} - Reason: {@event.Reason}");

        // Send failure notification
        await notificationService.SendEmailAsync(
            @event.UserId,
            "Payment Failed",
            $"Your payment failed. Reason: {@event.Reason}. Please try again or contact support."
        );

        // Update cache
        await cacheService.SetAsync(
            $"payment:{@event.PaymentId}:status",
            "failed",
            TimeSpan.FromHours(24)
        );

        // Could trigger: Retry logic, fraud detection, customer support ticket
    }
}

// Handler for PaymentRefundedEvent
public class PaymentRefundedHandler(
    INotificationService notificationService,
    ICacheService cacheService)
{
    public async Task Handle(PaymentRefundedEvent @event)
    {
        Console.WriteLine($"[Event Handler] Payment refunded: {@event.PaymentId} - Amount: {@event.Amount}");

        // Send refund notification
        await notificationService.SendEmailAsync(
            @event.UserId,
            "Payment Refunded",
            $"Your payment of {@event.Amount} has been refunded. Reason: {@event.Reason}"
        );

        // Update cache
        await cacheService.SetAsync(
            $"payment:{@event.PaymentId}:status",
            "refunded",
            TimeSpan.FromHours(24)
        );

        // Could trigger: Account credit, refund processing
    }
}
