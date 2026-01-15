using InventoryWorker;
using Microsoft.EntityFrameworkCore;
using Shared.Configuration;
using Shared.Data;
using Shared.Helpers;
using Shared.Repositories;

var builder = Host.CreateApplicationBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

var connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<PixelMartOrderProcessorDbContext>(options =>
options.UseNpgsql(
    connectionString,
    b => b.MigrationsAssembly("PixelMartOrderProcessor")));

builder.Services.AddScoped<IPixelMartOrderProcessorRepository, PixelMartOrderProcessorRepository>();
builder.Services.AddSingleton<RabbitMqConnectionManager>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
