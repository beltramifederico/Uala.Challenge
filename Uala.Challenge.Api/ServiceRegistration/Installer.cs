using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Infrastructure.DAL.Contexts;
using Uala.Challenge.Infrastructure.Repositories;
using Uala.Challenge.Infrastructure.DAL.Configurations;
using Uala.Challenge.Infrastructure.Services;
using Uala.Challenge.Domain.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Uala.Challenge.Api.Middleware;

namespace Uala.Challenge.Api.ServiceRegistration
{
    public static class Installer
    {
        public static void AddRepositories(this IServiceCollection service)
        {
            service.AddScoped<IUserRepository, UserRepository>();
            service.AddScoped<ITweetRepository, TweetRepository>();
        }

        public static void AddDatabase(this IServiceCollection service, IConfiguration config)
        {
            MongoDbConfigurator.RegisterMappings(); 

            service.AddDbContext<PostgresDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("PostgresConnection")));

            service.AddSingleton(new MongoDbContext(
                config.GetConnectionString("MongoConnection") ?? throw new InvalidOperationException("MongoDB connection string is missing."),
                config["MongoDbName"] ?? "UalaChallenge"
            ));
        }

        public static void AddCaching(this IServiceCollection service, IConfiguration config)
        {
            service.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = config.GetConnectionString("RedisConnection") ?? "localhost:6379";
                options.InstanceName = "UalaChallenge";
            });
            service.AddScoped<ICacheService, RedisCacheService>();
        }

        public static void AddMediatr(this IServiceCollection service)
        {
            service.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                Assembly.Load("Uala.Challenge.Application")));
        }

        public static void AddValidation(this IServiceCollection service)
        {
            service.AddFluentValidationAutoValidation();
            service.AddValidatorsFromAssembly(Assembly.Load("Uala.Challenge.Application"));
        }

        public static void ConfigSeriLog(this ILoggingBuilder logger, IConfiguration config)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            logger.ClearProviders();
            logger.AddSerilog(Log.Logger);
        }

        public static void AddSerilog(this IServiceCollection service)
        {
            service.AddSingleton(Log.Logger);
        }
        public static void AddUserMiddleware(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }

        public async static void MigrateDatabase(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<PostgresDbContext>();
                    await dbContext.Database.MigrateAsync();

                    var mongoContext = services.GetRequiredService<MongoDbContext>();
                    await MongoIndexConfiguration.ConfigureIndexesAsync(mongoContext.Database);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                    throw;
                }
            }
        }

    }
}
