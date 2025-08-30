using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetByVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}