using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Linq.Expressions;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Infrastructure.DAL.Contexts;

namespace Uala.Challenge.Infrastructure.Repositories;

public class TweetRepository : ITweetRepository
{
    private readonly MongoDbContext _context;

    public TweetRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Tweet?> GetByIdAsync(object id)
    {
        if (id is not Guid tweetId) throw new ArgumentException("Invalid id type");
        var filter = Builders<Tweet>.Filter.Eq(t => t.Id, tweetId);
        return await _context.Tweets.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Tweet>> GetAllAsync()
    {
        return await _context.Tweets.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> FindAsync(Expression<Func<Tweet, bool>> predicate)
    {
        return await _context.Tweets.Find(predicate).ToListAsync();
    }

    public async Task AddAsync(Tweet entity)
    {
        await _context.Tweets.InsertOneAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<Tweet> entities)
    {
        await _context.Tweets.InsertManyAsync(entities);
    }

    public async Task UpdateAsync(Tweet entity)
    {
        var filter = Builders<Tweet>.Filter.Eq(t => t.Id, entity.Id);
        await _context.Tweets.ReplaceOneAsync(filter, entity);
    }

    public async Task DeleteAsync(Tweet entity)
    {
        var filter = Builders<Tweet>.Filter.Eq(t => t.Id, entity.Id);
        await _context.Tweets.DeleteOneAsync(filter);
    }

    public async Task DeleteRangeAsync(IEnumerable<Tweet> entities)
    {
        var filter = Builders<Tweet>.Filter.In(t => t.Id, entities.Select(e => e.Id));
        await _context.Tweets.DeleteManyAsync(filter);
    }

    public async Task<Tuple<IEnumerable<Tweet>, int>> GetTimelineAsync(IEnumerable<Guid> followingIds, int pageNumber, int pageSize)
    {
        var followingIdsList = followingIds.ToList();

        var pipeline = new BsonDocument[]
        {
            // Match tweets from followed users
            new BsonDocument("$match", new BsonDocument("UserId", new BsonDocument("$in", new BsonArray(followingIdsList.Select(id => id.ToString()))))),

            // Sort by creation date (newest first)
            new BsonDocument("$sort", new BsonDocument("CreatedAt", -1)),
            
            // Facet to get both count and paginated results in one query
            new BsonDocument("$facet", new BsonDocument
            {
                ["totalCount"] = new BsonArray { new BsonDocument("$count", "count") },
                ["data"] = new BsonArray 
                {
                    new BsonDocument("$skip", (pageNumber - 1) * pageSize),
                    new BsonDocument("$limit", pageSize)
                }
            })
        };

        var aggregationResult = await _context.Tweets
            .Aggregate<BsonDocument>(pipeline)
            .FirstOrDefaultAsync();

        // Extract total count
        var totalCountArray = aggregationResult["totalCount"].AsBsonArray;
        var totalCount = totalCountArray.Count > 0 ? totalCountArray[0]["count"].AsInt32 : 0;

        // Extract tweets data
        var tweetsData = aggregationResult["data"].AsBsonArray;
        var tweets = tweetsData.Select(doc => BsonSerializer.Deserialize<Tweet>(doc.AsBsonDocument)).ToList();
        return new Tuple<IEnumerable<Tweet>, int>(tweets, totalCount);
    }

    public async Task<IEnumerable<Tweet>> GetUserTweetsAsync(Guid userId)
    {
        var filter = Builders<Tweet>.Filter.Eq(t => t.UserId, userId);
        var sort = Builders<Tweet>.Sort.Descending(t => t.CreatedAt);

        return await _context.Tweets
            .Find(filter)
            .Sort(sort)
            .ToListAsync();
    }
}
