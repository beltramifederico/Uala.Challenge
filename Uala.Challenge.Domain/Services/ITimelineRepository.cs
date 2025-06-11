using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Application.Interfaces;

public interface ITimelineRepository
{
    Task CreateTimelineEntriesAsync(IEnumerable<Timeline> timelineEntries);
    Task<(IEnumerable<Timeline> timelines, long totalCount)> GetTimelineAsync(Guid userId, int pageNumber, int pageSize);
}
