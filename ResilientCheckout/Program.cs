using ResilientCheckout.Domain.Payments;
using ResilientCheckout.Infraestructure.Payments;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IPaymentProvider, StripeFakeProvider>();
//builder.Services.AddScoped<IPaymentProvider, PaypalFakeProvider>();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "Hello World!");

app.Run();
