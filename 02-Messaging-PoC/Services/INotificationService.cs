namespace MessagingPoC.Services;

public interface INotificationService
{
    Task SendAsync(string recipient, string message);
}

public class ConsoleNotificationService : INotificationService
{
    public Task SendAsync(string recipient, string message)
    {
        Console.WriteLine($"[Notification] To: {recipient} | Message: {message}");
        return Task.CompletedTask;
    }
}
