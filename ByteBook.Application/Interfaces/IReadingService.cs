using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Reading;

namespace ByteBook.Application.Interfaces;

public interface IReadingService
{
    Task<Result<ReadingSessionDto>> StartReadingSessionAsync(int userId, StartReadingSessionDto dto, CancellationToken cancellationToken = default);
    Task<Result<ReadingSessionDto>> UpdateReadingProgressAsync(int userId, UpdateReadingProgressDto dto, CancellationToken cancellationToken = default);
    Task<Result<ReadingSessionDto>> EndReadingSessionAsync(int userId, EndReadingSessionDto dto, CancellationToken cancellationToken = default);
    Task<Result<ReadingSessionDto?>> GetActiveSessionAsync(int userId, int bookId, CancellationToken cancellationToken = default);
    Task<Result<ReadingHistoryDto>> GetReadingHistoryAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<List<ReadingSessionDto>>> GetBookReadingSessionsAsync(int userId, int bookId, CancellationToken cancellationToken = default);
    Task<Result> PauseReadingSessionAsync(int userId, int sessionId, CancellationToken cancellationToken = default);
    Task<Result> ResumeReadingSessionAsync(int userId, int sessionId, CancellationToken cancellationToken = default);
}