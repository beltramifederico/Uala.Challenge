using MongoDB.Driver;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Infrastructure.DAL.Contexts;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Tweet> Tweets => _database.GetCollection<Tweet>("Tweets");
    public IMongoDatabase Database => _database;
}
