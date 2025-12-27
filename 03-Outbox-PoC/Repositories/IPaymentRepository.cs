using Marten;
using OutboxPoC.Models;

namespace OutboxPoC.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<IEnumerable<Payment>> GetByUserIdAsync(string userId);
    Task SaveAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}

public class MartenPaymentRepository(IDocumentStore store) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        await using var session = store.QuerySession();
        return await session.LoadAsync<Payment>(id);
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        await using var session = store.QuerySession();
        return await session.Query<Payment>().ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(string userId)
    {
        await using var session = store.QuerySession();
        return await session.Query<Payment>()
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task SaveAsync(Payment payment)
    {
        await using var session = store.LightweightSession();
        session.Store(payment);
        await session.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        await using var session = store.LightweightSession();
        session.Update(payment);
        await session.SaveChangesAsync();
    }
}
