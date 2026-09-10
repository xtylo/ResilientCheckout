using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using ResilientCheckout.Application.Abstractions;
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
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// El fake provider concreto se registra bajo su propio tipo (no como IPaymentProvider),
// para que el decorator de abajo pueda pedirlo sin caer en una resolución circular.
builder.Services.AddScoped<StripeFakeProvider>();
//builder.Services.AddScoped<PaypalFakeProvider>();

// El pipeline de Polly debe ser Singleton: el circuit breaker guarda su estado
// (Closed/Open/Half-Open y el conteo de fallas) DENTRO del pipeline. Si se creara
// uno nuevo por request, el circuito jamás podría abrirse.
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PaymentResilience");
    return ResiliencePolicies.CreatePaymentProviderPipeline(logger);
});

// IPaymentProvider ahora resuelve al decorator, que envuelve al fake provider con el pipeline.
builder.Services.AddScoped<IPaymentProvider>(sp => new ResilientPaymentProvider(
    sp.GetRequiredService<StripeFakeProvider>(),
    sp.GetRequiredService<ResiliencePipeline<PaymentResult>>()));

builder.Services.AddScoped<IIdempotencyStore, EFIdempotencyStore>();
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();
builder.Services.AddScoped<IOutboxWritter, EFOutboxWritter>();
builder.Services.AddSingleton<PaymentSimulationOptions>();

// ServiceBusClient es seguro y eficiente de compartir durante toda la vida de la app
// (mantiene la conexión AMQP subyacente) — por eso Singleton, igual que el pipeline de Polly.
builder.Services.AddSingleton(sp =>
{
    var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:ServiceBus.");
    return new ServiceBusClient(serviceBusConnectionString);
});
builder.Services.AddSingleton<IEventPublisher, ServiceBusPublisher>();

// El relay corre en background durante toda la vida de la app, revisando OutboxMessages
// pendientes y publicándolos al Topic. Internamente abre su propio scope por ciclo
// para poder usar AppDbContext (Scoped) sin violar su propio lifetime de Singleton.
builder.Services.AddHostedService<ServiceBusOutboxRelay>();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "Hello World!");

app.Run();
