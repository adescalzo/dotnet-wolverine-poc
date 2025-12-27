using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MediatorPoC.Pipelines;

/// <summary>
/// Wolverine Middleware for logging using ILogger
/// Wolverine automatically recognizes Before/After/Finally methods
/// This is the native Wolverine way to add cross-cutting concerns
/// </summary>
public class LoggingMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly Stopwatch _stopwatch = new();

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executed BEFORE the handler (Wolverine convention)
    /// </summary>
    public void Before(object message)
    {
        var messageType = message.GetType().Name;
        _logger.LogInformation("[Pipeline:Logging] >> Starting: {MessageType}", messageType);
        _stopwatch.Restart();
    }

    /// <summary>
    /// Executed AFTER the handler completes successfully (Wolverine convention)
    /// </summary>
    public void After(object message)
    {
        _stopwatch.Stop();
        var messageType = message.GetType().Name;
        _logger.LogInformation("[Pipeline:Logging] << Completed: {MessageType} in {ElapsedMs}ms",
            messageType, _stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Executed in case of exception (Wolverine convention)
    /// </summary>
    public void OnException(Exception ex, object message)
    {
        _stopwatch.Stop();
        var messageType = message.GetType().Name;
        _logger.LogError(ex, "[Pipeline:Logging] !! Failed: {MessageType} after {ElapsedMs}ms",
            messageType, _stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Always executed, similar to finally block (Wolverine convention)
    /// </summary>
    public void Finally()
    {
        // Cleanup logic here if needed
        // This runs regardless of success or failure
    }
}

/// <summary>
/// Transaction middleware example (pattern for EF Core SaveChanges)
/// This demonstrates how you would implement transaction boundaries
/// without actually using EF Core to keep the PoC simple
/// </summary>
public class TransactionMiddleware
{
    private readonly ILogger<TransactionMiddleware> _logger;

    public TransactionMiddleware(ILogger<TransactionMiddleware> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executed BEFORE the handler - Begin transaction
    /// In a real app with EF Core, you would:
    /// - Inject IDbContextFactory or DbContext
    /// - Call await dbContext.Database.BeginTransactionAsync()
    /// </summary>
    public void Before(object message)
    {
        _logger.LogInformation("[Pipeline:Transaction] >> Begin transaction");
    }

    /// <summary>
    /// Executed AFTER the handler - Commit and SaveChanges
    /// In a real app with EF Core, you would:
    /// - Call await dbContext.SaveChangesAsync()
    /// - Call await transaction.CommitAsync()
    /// </summary>
    public void After(object message)
    {
        _logger.LogInformation("[Pipeline:Transaction] << Commit transaction (with SaveChanges)");
    }

    /// <summary>
    /// Executed in case of exception - Rollback transaction
    /// </summary>
    public void OnException(Exception ex, object message)
    {
        _logger.LogWarning("[Pipeline:Transaction] !! Rollback transaction due to error");
    }
}
