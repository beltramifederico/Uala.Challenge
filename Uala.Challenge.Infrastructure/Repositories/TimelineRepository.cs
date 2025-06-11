using MongoDB.Driver;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Infrastructure.Repositories;

public class TimelineRepository : ITimelineRepository
{
    private readonly IMongoCollection<Timeline> _timelineCollection;

    public TimelineRepository(IMongoDatabase mongoDatabase)
    {
        _timelineCollection = mongoDatabase.GetCollection<Timeline>("timelines");
    }

    public async Task CreateTimelineEntriesAsync(IEnumerable<Timeline> timelineEntries)
    {
        if (timelineEntries.Any())
        {
            await _timelineCollection.InsertManyAsync(timelineEntries);
        }
    }

    public async Task<(IEnumerable<Timeline> timelines, long totalCount)> GetTimelineAsync(Guid userId, int pageNumber, int pageSize)
    {
        var filter = Builders<Timeline>.Filter.Eq(t => t.UserId, userId);
        
        var totalCount = await _timelineCollection.CountDocumentsAsync(filter);
        
        var timelines = await _timelineCollection
            .Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (timelines, totalCount);
    }
}
