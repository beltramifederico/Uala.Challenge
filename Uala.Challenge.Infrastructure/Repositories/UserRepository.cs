using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Infrastructure.DAL.Contexts;

namespace Uala.Challenge.Infrastructure.Repositories;

public class UserRepository(PostgresDbContext context) : IUserRepository
{
    private readonly PostgresDbContext _context = context;

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User> Get(Guid id, bool include = false, bool traking = false)
    {
        var query = _context.Users.AsQueryable();
        query = include ? query.Include(u => u.Following) : query;
        query = traking ? query.AsTracking() : query.AsNoTracking();
       return await query
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<User>> GetFollowersAsync(Guid userId)
    {
        return await _context.Users
            .Where(u => u.Following.Any(f => f.Id == userId))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<Guid> userIds)
    {
        return await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(object id)
    {
        if (id is Guid guidId)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == guidId);
        }
        return null;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(User entity)
    {
        await _context.Users.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User entity)
    {
        _context.Users.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
