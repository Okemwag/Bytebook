namespace ByteBook.Application.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string tokenId, DateTime expiry, CancellationToken cancellationToken = default);
    Task<bool> IsTokenBlacklistedAsync(string tokenId, CancellationToken cancellationToken = default);
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}