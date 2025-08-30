using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.Value == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await GetByEmailAsync(email.Value, cancellationToken);
    }

    public async Task<User?> GetByVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);
    }

    public async Task<User?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.ResetPasswordToken == token && 
                                     u.ResetPasswordTokenExpiry > DateTime.UtcNow, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Role == role)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email.Value == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await EmailExistsAsync(email.Value, cancellationToken);
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLowerInvariant();
            query = query.Where(u => 
                u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                u.LastName.ToLower().Contains(lowerSearchTerm) ||
                u.Email.Value.Contains(lowerSearchTerm));
        }

        return await query
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}