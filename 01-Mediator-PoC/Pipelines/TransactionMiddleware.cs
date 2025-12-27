using Microsoft.Extensions.Logging;

namespace MediatorPoC.Pipelines;

/// <summary>
/// Transaction middleware example (pattern for EF Core SaveChanges)
/// This demonstrates how you would implement transaction boundaries
/// without actually using EF Core to keep the PoC simple
/// </summary>
public class TransactionMiddleware(ILogger<TransactionMiddleware> logger)
{
    /// <summary>
    /// Executed BEFORE the handler - Begin transaction
    /// In a real app with EF Core, you would:
    /// - Inject IDbContextFactory or DbContext
    /// - Call await dbContext.Database.BeginTransactionAsync()
    /// </summary>
    public void Before(object message)
    {
        logger.LogInformation("[Pipeline:Transaction] >> Begin transaction");
    }

    /// <summary>
    /// Executed AFTER the handler - Commit and SaveChanges
    /// In a real app with EF Core, you would:
    /// - Call await dbContext.SaveChangesAsync()
    /// - Call await transaction.CommitAsync()
    /// </summary>
    public void After(object message)
    {
        logger.LogInformation("[Pipeline:Transaction] << Commit transaction (with SaveChanges)");
    }

    /// <summary>
    /// Executed in case of exception - Rollback transaction
    /// </summary>
    public void OnException(Exception ex, object message)
    {
        logger.LogWarning("[Pipeline:Transaction] !! Rollback transaction due to error");
    }
}
