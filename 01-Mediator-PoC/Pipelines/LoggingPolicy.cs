using Lamar;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.Runtime.Handlers;
using MediatorPoC.Commands;

namespace MediatorPoC.Pipelines;

/// <summary>
/// Wolverine Handler Policy to apply middleware
/// This is how you configure which middleware applies to which handlers
/// </summary>
public class LoggingPolicy : IHandlerPolicy
{
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        // Apply logging middleware to ALL handlers
        foreach (var chain in chains)
        {
            // Add logging as the outermost middleware (wraps everything)
            chain.Middleware.Add(new MiddlewarePolicy(typeof(LoggingMiddleware)));
        }
    }
}

/// <summary>
/// Policy to apply transaction middleware only to Commands
/// This demonstrates selective middleware application
/// </summary>
public class TransactionPolicy : IHandlerPolicy
{
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        foreach (var chain in chains)
        {
            // Only apply transaction middleware to commands (not queries)
            // In a real app, you might check for a marker interface like ICommand
            if (IsCommand(chain.MessageType))
            {
                chain.Middleware.Add(new MiddlewarePolicy(typeof(TransactionMiddleware)));
            }
        }
    }

    private static bool IsCommand(Type messageType)
    {
        // Check if it's a command based on namespace or type name
        return messageType.Namespace?.Contains("Commands") == true ||
               messageType.Name.EndsWith("Command");
    }
}
