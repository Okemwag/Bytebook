using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Books;

namespace ByteBook.Application.Interfaces;

public interface IBookService
{
    Task<Result<BookDto>> CreateBookAsync(int authorId, CreateBookDto dto, CancellationToken cancellationToken = default);
    Task<Result<BookDto>> UpdateBookAsync(int bookId, int authorId, UpdateBookDto dto, CancellationToken cancellationToken = default);
    Task<Result<BookDto>> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default);
    Task<Result<BookSearchResultDto>> SearchBooksAsync(BookSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<Result<List<BookListDto>>> GetBooksByAuthorAsync(int authorId, CancellationToken cancellationToken = default);
    Task<Result> PublishBookAsync(int bookId, int authorId, PublishBookDto dto, CancellationToken cancellationToken = default);
    Task<Result> UnpublishBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
    Task<Result> DeleteBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
    Task<Result<PageContentDto>> GetPageContentAsync(int bookId, int pageNumber, int userId, CancellationToken cancellationToken = default);
    Task<Result<BookAnalyticsDto>> GetBookAnalyticsAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
}

public interface IContentProtectionService
{
    Task<Result<string>> ApplyWatermarkAsync(string content, int userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateContentAccessAsync(int userId, int bookId, int pageNumber, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateSecureContentUrlAsync(int bookId, int pageNumber, int userId, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<Result> LogSuspiciousActivityAsync(int userId, string activity, string details, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Result<Stream>> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<Result> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<Result<string>> GeneratePresignedUrlAsync(string fileUrl, TimeSpan expiry, CancellationToken cancellationToken = default);
}