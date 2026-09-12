using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using ResilientCheckout.Application.Abstractions;
using ResilientCheckout.Application.Checkout;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Application.Messaging;
using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Payments;
using ResilientCheckout.Infraestructure.Idempotency;
using ResilientCheckout.Infraestructure.Messaging;
using ResilientCheckout.Infraestructure.Outbox;
using ResilientCheckout.Infraestructure.Payments;
using ResilientCheckout.Infraestructure.Persistence;
using ResilientCheckout.Infraestructure.Resilience;
using ResilientCheckout.Api.LifetimeDemo;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// The two concrete fakes are registered under their own type (not as IPaymentProvider),
// so the factory below can request either one without circular resolution. Which
// one gets wrapped by the decorator is decided by PaymentProviderSelection at
// runtime -- that's what makes IPaymentProvider a genuine Strategy,
// not just an interface with a single implementation.
builder.Services.AddScoped<StripeFakeProvider>();
builder.Services.AddScoped<PaypalFakeProvider>();
builder.Services.AddSingleton<PaymentProviderSelection>();

// The Polly pipeline must be a Singleton: the circuit breaker keeps its state
// (Closed/Open/Half-Open and the failure count) INSIDE the pipeline. If a new one were
// created per request, the circuit could never open.
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PaymentResilience");
    return ResiliencePolicies.CreatePaymentProviderPipeline(logger);
});

// IPaymentProvider resolves to the decorator, which wraps the ACTIVE fake (per
// PaymentProviderSelection) with the same Polly pipeline -- resilience doesn't know
// or care which concrete provider is behind it.
builder.Services.AddScoped<IPaymentProvider>(sp =>
{
    var selection = sp.GetRequiredService<PaymentProviderSelection>();
    IPaymentProvider innerProvider = selection.Current switch
    {
        PaymentProviderKind.Paypal => sp.GetRequiredService<PaypalFakeProvider>(),
        _ => sp.GetRequiredService<StripeFakeProvider>()
    };

    return new ResilientPaymentProvider(
        innerProvider,
        sp.GetRequiredService<ResiliencePipeline<PaymentResult>>());
});

builder.Services.AddScoped<IIdempotencyStore, EFIdempotencyStore>();
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();
builder.Services.AddScoped<IOutboxWritter, EFOutboxWritter>();
builder.Services.AddSingleton<PaymentSimulationOptions>();

// Application handler: gathers all the business orchestration for the charge. Scoped
// because everything it receives in its constructor (IPaymentProvider, IIdempotencyStore,
// IOutboxWritter, IUnitOfWork) is also Scoped -- it wouldn't make sense for it to live
// longer than its own dependencies.
builder.Services.AddScoped<IChargeOrderHandler, ChargeOrderHandler>();

// ServiceBusClient is safe and efficient to share for the whole life of the app
// (it keeps the underlying AMQP connection) — hence Singleton, same as the Polly pipeline.
builder.Services.AddSingleton(sp =>
{
    var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus")
        ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is missing.");
    return new ServiceBusClient(serviceBusConnectionString);
});
builder.Services.AddSingleton<IEventPublisher, ServiceBusPublisher>();

// The relay runs in the background for the whole life of the app, checking for pending
// OutboxMessages and publishing them to the Topic. Internally it opens its own scope per
// cycle so it can use AppDbContext (Scoped) without violating its own Singleton lifetime.
builder.Services.AddHostedService<ServiceBusOutboxRelay>();

// LifetimeDemo: the SAME concrete class (OperationService) registered under three
// different lifetimes, so they can be seen and compared via LifetimeDemoController.
builder.Services.AddSingleton<IOperationSingleton, OperationService>();
builder.Services.AddScoped<IOperationScoped, OperationService>();
builder.Services.AddTransient<IOperationTransient, OperationService>();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "Hello World!");

app.Run();
