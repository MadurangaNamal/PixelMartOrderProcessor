using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Shared.Configuration;
using Shared.Data;
using Shared.HealthChecks;
using Shared.Helpers;
using Shared.Repositories;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

var connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<PixelMartOrderProcessorDbContext>(options =>
options.UseNpgsql(
    connectionString,
    b => b.MigrationsAssembly("PixelMartOrderProcessor")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPixelMartOrderProcessorRepository, PixelMartOrderProcessorRepository>();
builder.Services.AddSingleton<RabbitMqConnectionManager>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    return new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMq:Host"]!,
        Port = int.Parse(builder.Configuration["RabbitMq:Port"]!),
        UserName = builder.Configuration["RabbitMq:Username"]!,
        Password = builder.Configuration["RabbitMq:Password"]!
    };
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "sql", "postgres"])
    .AddCheck<RabbitMqHealthCheck>(
        "rabbitmq-custom",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["messaging", "rabbitmq"])
    .AddNpgSql(
        connectionString,
        name: "postgres-connection",
        tags: ["db", "postgres"])
     .AddTypeActivatedCheck<RemoteWorkerHealthCheck>(
        "payment-worker",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["worker", "remote"],
        args: ["PaymentWorker", TimeSpan.FromSeconds(30)])

    .AddTypeActivatedCheck<RemoteWorkerHealthCheck>(
        "inventory-worker",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["worker", "remote"],
        args: ["InventoryWorker", TimeSpan.FromSeconds(30)])

    .AddTypeActivatedCheck<RemoteWorkerHealthCheck>(
        "email-worker",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["worker", "remote"],
        args: ["EmailWorker", TimeSpan.FromSeconds(30)]);

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(15);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.AddHealthCheckEndpoint("PixelMartOrderProcessor", "/health");
})
.AddInMemoryStorage();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder => policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PixelMartOrderProcessorDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

app.MapControllers();

await app.RunAsync();
