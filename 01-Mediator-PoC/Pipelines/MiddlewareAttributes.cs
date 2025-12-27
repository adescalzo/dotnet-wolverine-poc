using Wolverine;

namespace MediatorPoC.Pipelines;

/// <summary>
/// Wolverine attribute to apply logging middleware to handlers
/// Can be applied to individual handler classes
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoggedAttribute : ModifyHandlerChainAttribute
{
    public override void Modify(IChain chain, GenerationRules rules)
    {
        // Add logging middleware to the handler chain
        chain.Middleware.Add(new LoggingMiddleware(null!));
    }
}

/// <summary>
/// Wolverine attribute to apply transaction middleware to handlers
/// Can be applied to command handler classes
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TransactionalAttribute : ModifyHandlerChainAttribute
{
    public override void Modify(IChain chain, GenerationRules rules)
    {
        // Add transaction middleware to the handler chain
        chain.Middleware.Add(new TransactionMiddleware(null!));
    }
}
