using Azure.Messaging.ServiceBus;
using ResilientCheckout.Domain.Notifications;
using ResilientCheckout.Notifications;
using ResilientCheckout.Notifications.Channels;

var builder = Host.CreateApplicationBuilder(args);

// Same reasoning as in the Api: the ServiceBusClient keeps the AMQP connection alive and is
// meant to live for the whole life of the process -> Singleton.
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus")
        ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is missing.");
    return new ServiceBusClient(connectionString);
});

// The three channels are registered under the SAME interface -- so NotificationDispatcher
// can request IEnumerable<INotificationChannel> and receive all three.
builder.Services.AddSingleton<INotificationChannel, EmailChannel>();
builder.Services.AddSingleton<INotificationChannel, SmsChannel>();
builder.Services.AddSingleton<INotificationChannel, PushChannel>();

builder.Services.AddSingleton<NotificationDispatcher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
