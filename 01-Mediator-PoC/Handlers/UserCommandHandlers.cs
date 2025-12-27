using MediatorPoC.Commands;
using MediatorPoC.Models;
using MediatorPoC.Repositories;

namespace MediatorPoC.Handlers;

// Command Handler: Create User
public class CreateUserHandler(IUserRepository repository)
{
    public Guid Handle(CreateUserCommand command)
    {
        var user = new User(Guid.NewGuid(), command.Name, command.Email);
        repository.Add(user);
        Console.WriteLine($"   [Handler] User created: {user.Name}");
        return user.Id;
    }
}

// Command Handler: Update User
public class UpdateUserHandler(IUserRepository repository)
{
    public void Handle(UpdateUserCommand command)
    {
        var user = repository.GetById(command.Id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {command.Id} not found");
        }

        var updatedUser = user with { Name = command.Name, Email = command.Email };
        repository.Update(updatedUser);
        Console.WriteLine($"   [Handler] User updated: {updatedUser.Name}");
    }
}

// Command Handler: Send Notification (async)
public class SendUserNotificationHandler(IUserRepository repository)
{
    public async Task Handle(SendUserNotificationCommand command)
    {
        var user = repository.GetById(command.UserId);
        if (user != null)
        {
            // Simulate async work
            await Task.Delay(50);
            Console.WriteLine($"   [Handler] Notification sent to {user.Email}: {command.Message}");
        }
    }
}
