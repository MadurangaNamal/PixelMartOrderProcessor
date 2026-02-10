using EmailWorker;
using Microsoft.EntityFrameworkCore;
using Shared.Configuration;
using Shared.Data;
using Shared.HealthChecks;
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
builder.Services.AddSingleton<WorkerHealthCheck>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RabbitMqHealthCheck>("rabbitmq")
    .AddCheck<WorkerHealthCheck>("worker");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
