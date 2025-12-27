using MediatorPoC.Models;

namespace MediatorPoC.Repositories;

public interface IUserRepository
{
    void Add(User user);
    User? GetById(Guid id);
    IEnumerable<User> GetAll();
    void Update(User user);
}

public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public void Add(User user)
    {
        _users[user.Id] = user;
    }

    public User? GetById(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return user;
    }

    public IEnumerable<User> GetAll()
    {
        return _users.Values;
    }

    public void Update(User user)
    {
        _users[user.Id] = user;
    }
}
