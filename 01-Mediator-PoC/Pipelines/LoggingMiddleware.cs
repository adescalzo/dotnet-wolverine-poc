using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MediatorPoC.Pipelines;

/// <summary>
/// Wolverine Middleware for logging using ILogger
/// Wolverine automatically recognizes Before/After/Finally methods
/// This is the native Wolverine way to add cross-cutting concerns
/// </summary>
public class LoggingMiddleware(ILogger<LoggingMiddleware> logger)
{
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Executed BEFORE the handler (Wolverine convention)
    /// </summary>
    public void Before(object message)
    {
        var messageType = message.GetType().Name;
        logger.LogInformation("[Pipeline:Logging] >> Starting: {MessageType}", messageType);
        _stopwatch.Restart();
    }

    /// <summary>
    /// Executed AFTER the handler completes successfully (Wolverine convention)
    /// </summary>
    public void After(object message)
    {
        _stopwatch.Stop();
        var messageType = message.GetType().Name;
        logger.LogInformation("[Pipeline:Logging] << Completed: {MessageType} in {ElapsedMs}ms",
            messageType, _stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Executed in case of exception (Wolverine convention)
    /// </summary>
    public void OnException(Exception ex, object message)
    {
        _stopwatch.Stop();
        var messageType = message.GetType().Name;
        logger.LogError(ex, "[Pipeline:Logging] !! Failed: {MessageType} after {ElapsedMs}ms",
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
