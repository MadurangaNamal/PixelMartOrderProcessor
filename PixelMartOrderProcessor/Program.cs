using Microsoft.EntityFrameworkCore;
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

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
