namespace OutboxPoC.Services;

public interface INotificationService
{
    Task SendPaymentNotificationAsync(string userId, string message);
    Task SendEmailAsync(string userId, string subject, string body);
}

public class ConsoleNotificationService : INotificationService
{
    public Task SendPaymentNotificationAsync(string userId, string message)
    {
        Console.WriteLine($"[Notification] User: {userId} | {message}");
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string userId, string subject, string body)
    {
        Console.WriteLine($"[Email] To: {userId} | Subject: {subject} | Body: {body}");
        return Task.CompletedTask;
    }
}
