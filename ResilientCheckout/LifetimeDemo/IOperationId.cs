namespace ResilientCheckout.Api.LifetimeDemo
{
    // Shared contract: each "marker" is an empty interface that inherits from this one,
    // just so the SAME concrete class can be registered under three different lifetimes
    // in the DI container (you can't register the same interface three times with
    // different lifetimes -- hence three interfaces).
    public interface IOperationId
    {
        Guid OperationId { get; }
    }

    public interface IOperationSingleton : IOperationId { }

    public interface IOperationScoped : IOperationId { }

    public interface IOperationTransient : IOperationId { }
}
