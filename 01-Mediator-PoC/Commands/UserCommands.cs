namespace MediatorPoC.Commands;

public record CreateUserCommand(string Name, string Email);

public record UpdateUserCommand(Guid Id, string Name, string Email);

public record SendUserNotificationCommand(Guid UserId, string Message);
