using MediatorPoC.Models;
using MediatorPoC.Queries;
using MediatorPoC.Repositories;

namespace MediatorPoC.Handlers;

// Query Handler: Get User by ID
public class GetUserByIdHandler(IUserRepository repository)
{
    public UserDto? Handle(GetUserByIdQuery query)
    {
        var user = repository.GetById(query.Id);
        if (user == null) return null;

        Console.WriteLine($"   [Handler] Fetching user: {user.Name}");
        return new UserDto(user.Id, user.Name, user.Email);
    }
}

// Query Handler: Get All Users
public class GetAllUsersHandler(IUserRepository repository)
{
    public IEnumerable<UserDto> Handle(GetAllUsersQuery query)
    {
        var users = repository.GetAll();
        Console.WriteLine($"   [Handler] Fetching {users.Count()} users");
        return users.Select(u => new UserDto(u.Id, u.Name, u.Email));
    }
}
