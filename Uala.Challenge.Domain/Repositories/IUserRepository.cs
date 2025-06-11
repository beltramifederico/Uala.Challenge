using Uala.Challenge.Domain.Common;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User> Get(Guid id, bool include = false, bool traking = false);
    Task<IEnumerable<User>> GetFollowersAsync(Guid userId);
    Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<Guid> userIds);
}
