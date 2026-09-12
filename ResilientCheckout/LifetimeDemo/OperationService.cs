namespace ResilientCheckout.Api.LifetimeDemo
{
    // Purely demonstrative -- not a business service, which is why it lives alongside
    // the controller and not in Domain/Application/Infraestructura (same reasoning as
    // PaymentSimulationOptions). Each instance is tagged with a Guid at birth; comparing
    // those Guids between two resolutions is the only way to "see" a lifetime in action.
    public class OperationService : IOperationSingleton, IOperationScoped, IOperationTransient
    {
        public Guid OperationId { get; } = Guid.NewGuid();
    }
}
