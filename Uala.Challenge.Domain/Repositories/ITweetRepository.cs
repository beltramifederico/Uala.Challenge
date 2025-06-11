using Uala.Challenge.Domain.Common;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Domain.Repositories;

public interface ITweetRepository : IRepository<Tweet>
{
    Task<Tuple<IEnumerable<Tweet>, int>> GetTimelineAsync(IEnumerable<Guid> followingIds, int pageNumber, int pageSize);
    Task<IEnumerable<Tweet>> GetUserTweetsAsync(Guid userId);
}
