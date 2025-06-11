using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Infrastructure.DAL.Configurations
{
    public static class MongoDbConfigurator
    {
        private static bool _mapsRegistered = false;
        private static readonly object _lock = new object();

        public static void RegisterMappings()
        {
            if (_mapsRegistered) return;

            lock (_lock)
            {
                if (_mapsRegistered) return;

                ConfigureTweetMap();
                ConfigureTimelineMap();

                _mapsRegistered = true;
            }
        }

        private static void ConfigureTweetMap()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Tweet)))
            {
                BsonClassMap.RegisterClassMap<Tweet>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(t => t.Id)
                      .SetIdGenerator(GuidGenerator.Instance)
                      .SetSerializer(new GuidSerializer(BsonType.String));

                    cm.MapProperty(t => t.UserId).SetSerializer(new GuidSerializer(BsonType.String));
                });
            }
        }

        private static void ConfigureTimelineMap()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Timeline)))
            {
                BsonClassMap.RegisterClassMap<Timeline>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(t => t.Id)
                      .SetIdGenerator(GuidGenerator.Instance)
                      .SetSerializer(new GuidSerializer(BsonType.String));
                    cm.MapProperty(t => t.UserId).SetSerializer(new GuidSerializer(BsonType.String));
                    cm.MapProperty(t => t.TweetId).SetSerializer(new GuidSerializer(BsonType.String));
                    cm.MapProperty(t => t.AuthorId).SetSerializer(new GuidSerializer(BsonType.String));
                });
            }
        }
    }
}
