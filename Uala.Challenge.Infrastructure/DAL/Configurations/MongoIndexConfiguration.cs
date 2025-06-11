using MongoDB.Driver;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Infrastructure.DAL.Configurations
{
    public static class MongoIndexConfiguration
    {
        public static async Task ConfigureIndexesAsync(IMongoDatabase database)
        {
            var tweetsCollection = database.GetCollection<Tweet>("Tweets");
            
            var timelineIndexKeys = Builders<Tweet>.IndexKeys
                .Ascending(t => t.UserId)
                .Descending(t => t.CreatedAt);
            
            var timelineIndexOptions = new CreateIndexOptions
            {
                Name = "idx_timeline_userid_createdat",
                Background = true 
            };
            
            await tweetsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Tweet>(timelineIndexKeys, timelineIndexOptions));

            var createdAtIndexKeys = Builders<Tweet>.IndexKeys.Descending(t => t.CreatedAt);
            var createdAtIndexOptions = new CreateIndexOptions
            {
                Name = "idx_createdat_desc",
                Background = true
            };
            
            await tweetsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Tweet>(createdAtIndexKeys, createdAtIndexOptions));
        }
    }
}
