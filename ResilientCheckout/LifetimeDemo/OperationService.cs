namespace ResilientCheckout.Api.LifetimeDemo
{
    // Puramente demostrativo -- no es un servicio de negocio, por eso vive junto al
    // controller y no en Domain/Application/Infraestructura (mismo criterio que
    // PaymentSimulationOptions). Cada instancia se marca con un Guid al nacer; comparar
    // esos Guids entre dos resoluciones es la única forma de "ver" un lifetime en acción.
    public class OperationService : IOperationSingleton, IOperationScoped, IOperationTransient
    {
        public Guid OperationId { get; } = Guid.NewGuid();
    }
}
