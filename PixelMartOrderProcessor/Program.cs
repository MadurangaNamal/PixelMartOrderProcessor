using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Shared.Configuration;
using Shared.Constants;
using Shared.Data;
using Shared.HealthChecks;
using Shared.Helpers;
using Shared.Repositories;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

// Retrieve the connection string from DatabaseConfiguration
var connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<PixelMartOrderProcessorDbContext>(options =>
options.UseNpgsql(
    connectionString,
    b => b.MigrationsAssembly(AppConstants.MigrationsAssembly)));

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
        HostName = builder.Configuration[AppConstants.RabbitMq.Host]!,
        Port = int.Parse(builder.Configuration[AppConstants.RabbitMq.Port]!),
        UserName = builder.Configuration[AppConstants.RabbitMq.Username]!,
        Password = builder.Configuration[AppConstants.RabbitMq.Password]!
    };
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        AppConstants.HealthChecks.Database,
        failureStatus: HealthStatus.Unhealthy,
        tags: [AppConstants.HealthChecks.Tags.Db, AppConstants.HealthChecks.Tags.Sql, AppConstants.HealthChecks.Tags.Postgres])
    .AddCheck<RabbitMqHealthCheck>(
        AppConstants.HealthChecks.RabbitMqCustom,
        failureStatus: HealthStatus.Unhealthy,
        tags: [AppConstants.HealthChecks.Tags.Messaging, AppConstants.HealthChecks.Tags.RabbitMq])
    .AddNpgSql(
        connectionString,
        name: AppConstants.HealthChecks.PostgresConnection,
        tags: [AppConstants.HealthChecks.Tags.Db, AppConstants.HealthChecks.Tags.Postgres])
     .AddTypeActivatedCheck<RemoteWorkerHealthCheck>(
        AppConstants.HealthChecks.PaymentWorker,
        failureStatus: HealthStatus.Unhealthy,
        tags: [AppConstants.HealthChecks.Tags.Worker, AppConstants.HealthChecks.Tags.Remote],
        args: [AppConstants.Workers.Payment, TimeSpan.FromSeconds(30)])

    .AddTypeActivatedCheck<RemoteWorkerHealthCheck>(
        AppConstants.HealthChecks.InventoryWorker,
        failureStatus: HealthStatus.Unhealthy,
        tags: [AppConstants.HealthChecks.Tags.Worker, AppConstants.HealthChecks.Tags.Remote],
        args: [AppConstants.Workers.Inventory, TimeSpan.FromSeconds(30)])

    .AddTypeActivatedCheck<RemoteWorkerHealthCheck>(
        AppConstants.HealthChecks.EmailWorker,
        failureStatus: HealthStatus.Unhealthy,
        tags: [AppConstants.HealthChecks.Tags.Worker, AppConstants.HealthChecks.Tags.Remote],
        args: [AppConstants.Workers.Email, TimeSpan.FromSeconds(30)]);

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(10);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.AddHealthCheckEndpoint(AppConstants.ApplicationName, AppConstants.HealthChecks.Paths.Health);
})
.AddInMemoryStorage();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppConstants.Cors.AllowAll,
        policyBuilder => policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});
builder.Services.AddPixelMartTelemetry("OrderApi", builder.Configuration);

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

app.UseCors(AppConstants.Cors.AllowAll);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks(AppConstants.HealthChecks.Paths.Health, new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks(AppConstants.HealthChecks.Paths.Ready, new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(AppConstants.HealthChecks.Tags.Ready),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks(AppConstants.HealthChecks.Paths.Live, new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecksUI(options =>
{
    options.UIPath = AppConstants.HealthChecks.Paths.Ui;
    options.ApiPath = AppConstants.HealthChecks.Paths.UiApi;
});

app.MapControllers();

await app.RunAsync();
