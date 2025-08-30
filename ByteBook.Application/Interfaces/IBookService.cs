using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Books;

namespace ByteBook.Application.Interfaces;

public interface IBookService
{
    Task<Result<BookDto>> CreateBookAsync(CreateBookDto dto, int authorId, CancellationToken cancellationToken = default);
    Task<Result<BookDto>> UpdateBookAsync(int bookId, UpdateBookDto dto, int authorId, CancellationToken cancellationToken = default);
    Task<Result<BookDto>> GetBookByIdAsync(int bookId, int? userId = null, CancellationToken cancellationToken = default);
    Task<Result<BookSearchResultDto>> SearchBooksAsync(BookSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<Result<List<BookListDto>>> GetBooksByAuthorAsync(int authorId, CancellationToken cancellationToken = default);
    Task<Result<BookDto>> PublishBookAsync(int bookId, int authorId, PublishBookDto? publishDto = null, CancellationToken cancellationToken = default);
    Task<Result> UnpublishBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
    Task<Result> RestoreBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
    Task<Result<PageContentDto>> GetPageContentAsync(int bookId, int pageNumber, int userId, CancellationToken cancellationToken = default);
    Task<Result<BookAnalyticsDto>> GetBookAnalyticsAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
    Task<Result> DeleteBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default);
}

public interface IContentProcessingService
{
    Task<Result<ProcessedContentDto>> ProcessContentAsync(string content, CancellationToken cancellationToken = default);
    Task<Result<ProcessedContentDto>> ProcessPdfAsync(Stream pdfStream, string fileName, CancellationToken cancellationToken = default);
    Task<Result<PlagiarismCheckDto>> CheckPlagiarismAsync(string content, CancellationToken cancellationToken = default);
    Task<Result<string>> FormatContentAsync(string content, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Result<Stream>> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<Result> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<Result<string>> GeneratePresignedUrlAsync(string fileUrl, TimeSpan expiry, CancellationToken cancellationToken = default);
}

public class ProcessedContentDto
{
    public string Content { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public string? ContentUrl { get; set; }
    public List<string> ExtractedImages { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PlagiarismCheckDto
{
    public bool IsPlagiarized { get; set; }
    public decimal SimilarityPercentage { get; set; }
    public List<PlagiarismMatch> Matches { get; set; } = new();
    public string? ReportUrl { get; set; }
}

public class PlagiarismMatch
{
    public string Source { get; set; } = string.Empty;
    public string MatchedText { get; set; } = string.Empty;
    public decimal SimilarityScore { get; set; }
    public string? SourceUrl { get; set; }
}