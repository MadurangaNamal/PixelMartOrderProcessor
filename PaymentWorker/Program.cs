using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentWorker;
using Shared.Configuration;
using Shared.Constants;
using Shared.Data;
using Shared.HealthChecks;
using Shared.Helpers;
using Shared.Repositories;

var builder = Host.CreateApplicationBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

var connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<PixelMartOrderProcessorDbContext>(options =>
options.UseNpgsql(
    connectionString,
    b => b.MigrationsAssembly(AppConstants.MigrationsAssembly)));

builder.Services.AddScoped<IPixelMartOrderProcessorRepository, PixelMartOrderProcessorRepository>();
builder.Services.AddSingleton<RabbitMqConnectionManager>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<WorkerHealthCheck>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(AppConstants.HealthChecks.Database)
    .AddCheck<RabbitMqHealthCheck>(AppConstants.HealthChecks.RabbitMq)
    .AddCheck<WorkerHealthCheck>(AppConstants.HealthChecks.Worker);

builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(5);
    options.Period = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IHealthCheckPublisher, DatabaseHealthCheckPublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
