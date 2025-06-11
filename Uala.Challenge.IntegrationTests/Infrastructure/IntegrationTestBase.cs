using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.MongoDb;
using Testcontainers.Redis;
using Testcontainers.Kafka;
using Uala.Challenge.Infrastructure.Services;
using MongoDB.Driver;
using Uala.Challenge.Domain.Services;
using Uala.Challenge.Infrastructure.DAL.Contexts;
using Uala.Challenge.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Uala.Challenge.IntegrationTests.Infrastructure;

public class IntegrationTestBase
{
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    
    private PostgreSqlContainer _postgresContainer = null!;
    private MongoDbContainer _mongoContainer = null!;
    private RedisContainer _redisContainer = null!;
    private KafkaContainer _kafkaContainer = null!;

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        // Start test containers
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("uala_test")
            .WithUsername("test")
            .WithPassword("test123")
            .Build();

        _mongoContainer = new MongoDbBuilder()
            .WithUsername("test")
            .WithPassword("test123")
            .Build();

        _redisContainer = new RedisBuilder()
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .Build();

        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _mongoContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _kafkaContainer.StartAsync()
        );

        // Create test application factory
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add test configuration
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:PostgresConnection"] = _postgresContainer.GetConnectionString(),
                        ["ConnectionStrings:MongoConnection"] = _mongoContainer.GetConnectionString(),
                        ["ConnectionStrings:RedisConnection"] = _redisContainer.GetConnectionString(),
                        ["ConnectionStrings:Kafka"] = _kafkaContainer.GetBootstrapAddress(),
                        ["MongoDbName"] = "uala_test"
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Remove existing database contexts
                    services.RemoveAll(typeof(DbContextOptions<PostgresDbContext>));
                    services.RemoveAll(typeof(PostgresDbContext));
                    services.RemoveAll(typeof(IMongoDatabase));
                    services.RemoveAll(typeof(ICacheService));
                    services.RemoveAll(typeof(IKafkaProducer));

                    // Add test database contexts
                    services.AddDbContext<PostgresDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    // Add test MongoDB
                    services.AddSingleton<IMongoClient>(provider =>
                        new MongoClient(_mongoContainer.GetConnectionString()));
                    
                    services.AddSingleton<IMongoDatabase>(provider =>
                    {
                        var client = provider.GetRequiredService<IMongoClient>();
                        return client.GetDatabase("uala_test");
                    });

                    // Add test Redis cache
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = _redisContainer.GetConnectionString();
                    });
                    
                    services.AddSingleton<ICacheService, RedisCacheService>();

                    // Add test Kafka
                    services.AddSingleton<IKafkaProducer, KafkaProducer>();
                    services.AddHostedService<TweetCreatedConsumer>();
                });
            });

        Client = Factory.CreateClient();

        // Initialize databases
        await InitializeDatabasesAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Clean databases before each test
        await CleanDatabasesAsync();
    }

    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        
        if (_postgresContainer != null)
            await _postgresContainer.DisposeAsync();
        if (_mongoContainer != null)
            await _mongoContainer.DisposeAsync();
        if (_redisContainer != null)
            await _redisContainer.DisposeAsync();
        if (_kafkaContainer != null)
            await _kafkaContainer.DisposeAsync();
    }

    private async Task InitializeDatabasesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        
        // Initialize PostgreSQL with migrations
        var postgresContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        await postgresContext.Database.MigrateAsync();
        
        // MongoDB collections are created automatically when first document is inserted
    }

    private async Task CleanDatabasesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        
        // Clean PostgreSQL database
        var postgresContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        await postgresContext.Database.EnsureDeletedAsync();
        await postgresContext.Database.MigrateAsync();
        
        // Clean MongoDB database
        var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await mongoDatabase.Client.DropDatabaseAsync(mongoDatabase.DatabaseNamespace.DatabaseName);
    }

    protected async Task<T> GetService<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
