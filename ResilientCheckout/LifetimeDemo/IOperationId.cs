namespace ResilientCheckout.Api.LifetimeDemo
{
    // Contrato compartido: cada "marca" es una interfaz vacía que hereda de este,
    // solo para poder registrar la MISMA clase concreta bajo tres lifetimes distintos
    // en el contenedor de DI (no se puede registrar la misma interfaz tres veces con
    // lifetimes distintos -- por eso tres interfaces).
    public interface IOperationId
    {
        Guid OperationId { get; }
    }

    public interface IOperationSingleton : IOperationId { }

    public interface IOperationScoped : IOperationId { }

    public interface IOperationTransient : IOperationId { }
}
