using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Configuration;
using Shared.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var dbPassword = builder.Configuration["DB_PASSWORD"]
    ?? throw new InvalidOperationException("Database password 'DB_PASSWORD' not found in configuration.");

var connectionString = rawConnectionString.Replace("{DB_PASSWORD}", dbPassword);

builder.Services.AddDbContext<PixelMartOrderProcessorDbContext>(options =>
options.UseNpgsql(
    connectionString,
    b => b.MigrationsAssembly("PixelMartOrderProcessor")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RabbitMqConnectionManager>();

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

builder.Services.AddHostedService<PaymentWorker.Worker>();

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
