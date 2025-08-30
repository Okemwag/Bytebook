using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.DTOs.Users;

namespace ByteBook.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserDto>> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateUserProfileAsync(int userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default);
    Task<Result<UserStatsDto>> GetUserStatsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> ActivateUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<List<UserDto>>> SearchUsersAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}