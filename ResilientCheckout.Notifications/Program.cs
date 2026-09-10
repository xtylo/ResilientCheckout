using Azure.Messaging.ServiceBus;
using ResilientCheckout.Domain.Notifications;
using ResilientCheckout.Notifications;
using ResilientCheckout.Notifications.Channels;

var builder = Host.CreateApplicationBuilder(args);

// Mismo argumento que en la Api: el ServiceBusClient mantiene la conexión AMQP y está
// pensado para vivir toda la vida del proceso -> Singleton.
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:ServiceBus.");
    return new ServiceBusClient(connectionString);
});

// Los tres canales se registran bajo la MISMA interfaz -- así NotificationDispatcher
// puede pedir IEnumerable<INotificationChannel> y recibir los tres.
builder.Services.AddSingleton<INotificationChannel, EmailChannel>();
builder.Services.AddSingleton<INotificationChannel, SmsChannel>();
builder.Services.AddSingleton<INotificationChannel, PushChannel>();

builder.Services.AddSingleton<NotificationDispatcher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
