using MongoDB.Driver;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Infrastructure.DAL.Configurations
{
    public static class MongoIndexConfiguration
    {
        public static async Task ConfigureIndexesAsync(IMongoDatabase database)
        {
            await ConfigureTweetIndexesAsync(database);
            await ConfigureTimelineIndexesAsync(database);
        }

        private static async Task ConfigureTweetIndexesAsync(IMongoDatabase database)
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

        private static async Task ConfigureTimelineIndexesAsync(IMongoDatabase database)
        {
            var timelineCollection = database.GetCollection<Timeline>("timelines");
            
            // Índice principal para consultas de timeline por usuario
            var userTimelineIndexKeys = Builders<Timeline>.IndexKeys
                .Ascending(t => t.UserId)
                .Descending(t => t.CreatedAt);
            
            var userTimelineIndexOptions = new CreateIndexOptions
            {
                Name = "idx_timeline_userid_createdat",
                Background = true 
            };
            
            await timelineCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Timeline>(userTimelineIndexKeys, userTimelineIndexOptions));

            // Índice para búsquedas por TweetId
            var tweetIdIndexKeys = Builders<Timeline>.IndexKeys.Ascending(t => t.TweetId);
            var tweetIdIndexOptions = new CreateIndexOptions
            {
                Name = "idx_timeline_tweetid",
                Background = true
            };
            
            await timelineCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Timeline>(tweetIdIndexKeys, tweetIdIndexOptions));

            // Índice para búsquedas por AuthorId
            var authorIdIndexKeys = Builders<Timeline>.IndexKeys.Ascending(t => t.AuthorId);
            var authorIdIndexOptions = new CreateIndexOptions
            {
                Name = "idx_timeline_authorid",
                Background = true
            };
            
            await timelineCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Timeline>(authorIdIndexKeys, authorIdIndexOptions));
        }
    }
}
