using Microsoft.EntityFrameworkCore;
using ResilientCheckout.Application.Abstractions;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Payments;
using ResilientCheckout.Infraestructure.Idempotency;
using ResilientCheckout.Infraestructure.Outbox;
using ResilientCheckout.Infraestructure.Payments;
using ResilientCheckout.Infraestructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IPaymentProvider, StripeFakeProvider>();
//builder.Services.AddScoped<IPaymentProvider, PaypalFakeProvider>();

builder.Services.AddScoped<IIdempotencyStore, EFIdempotencyStore>();
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();
builder.Services.AddScoped<IOutboxWritter, EFOutboxWritter>();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "Hello World!");

app.Run();
