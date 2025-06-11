using Microsoft.EntityFrameworkCore;
using Serilog;
using Uala.Challenge.Api.ServiceRegistration;
using Uala.Challenge.Infrastructure.DAL.Contexts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddMediatr();
builder.Services.AddValidation();
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddKafka();

builder.Logging.ConfigSeriLog(builder.Configuration);
builder.Services.AddSerilog();
builder.Host.UseSerilog();

var app = builder.Build();

// Only migrate database if not in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MigrateDatabase();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add exception handling middleware
app.AddUserMiddleware();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
