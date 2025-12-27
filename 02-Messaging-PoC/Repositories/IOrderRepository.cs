using MessagingPoC.Models;

namespace MessagingPoC.Repositories;

public interface IOrderRepository
{
    void Add(Order order);
    Order? GetById(Guid id);
    IEnumerable<Order> GetAll();
    void Update(Order order);
}

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public void Add(Order order)
    {
        _orders[order.Id] = order;
    }

    public Order? GetById(Guid id)
    {
        _orders.TryGetValue(id, out var order);
        return order;
    }

    public IEnumerable<Order> GetAll()
    {
        return _orders.Values;
    }

    public void Update(Order order)
    {
        _orders[order.Id] = order;
    }
}
