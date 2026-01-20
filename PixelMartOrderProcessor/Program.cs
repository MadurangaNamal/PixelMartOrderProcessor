using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Configuration;
using Shared.Data;
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

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory()
    {
        HostName = builder.Configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMq:Host is not configured"),
        Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? throw new InvalidOperationException("RabbitMq:Port is not configured")),
        UserName = builder.Configuration["RabbitMq:Username"] ?? throw new InvalidOperationException("RabbitMq:Username is not configured"),
        Password = builder.Configuration["RabbitMq:Password"] ?? throw new InvalidOperationException("RabbitMq:Password is not configured")
    };

    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder => policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

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
app.MapControllers();

await app.RunAsync();
