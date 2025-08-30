using AutoMapper;
using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Books;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ByteBook.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly IContentProcessingService _contentProcessingService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IContentProtectionService _contentProtectionService;
    private readonly IMapper _mapper;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IBookRepository bookRepository,
        IUserRepository userRepository,
        IContentProcessingService contentProcessingService,
        IFileStorageService fileStorageService,
        IContentProtectionService contentProtectionService,
        IMapper mapper,
        ILogger<BookService> logger)
    {
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _contentProcessingService = contentProcessingService;
        _fileStorageService = fileStorageService;
        _contentProtectionService = contentProtectionService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BookDto>> CreateBookAsync(CreateBookDto dto, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify author exists
            var author = await _userRepository.GetByIdAsync(authorId);
            if (author == null)
            {
                return Result<BookDto>.ValidationFailure("Author", "Author not found");
            }

            // Create book entity
            var book = new Book(dto.Title, dto.Description, authorId, dto.Category);

            // Set pricing if provided
            if (dto.PricePerPage.HasValue || dto.PricePerHour.HasValue)
            {
                var pricePerPage = dto.PricePerPage.HasValue ? new Money(dto.PricePerPage.Value) : null;
                var pricePerHour = dto.PricePerHour.HasValue ? new Money(dto.PricePerHour.Value) : null;
                book.SetPricing(pricePerPage, pricePerHour);
            }

            // Process content
            ProcessedContentDto? processedContent = null;
            if (dto.ContentFile != null)
            {
                // Process PDF file
                using var stream = dto.ContentFile.OpenReadStream();
                var contentResult = await _contentProcessingService.ProcessPdfAsync(stream, dto.ContentFile.FileName, cancellationToken);
                if (contentResult.IsFailure)
                {
                    return Result<BookDto>.Failure($"Content processing failed: {contentResult.ErrorMessage}");
                }
                processedContent = contentResult.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                // Process text content
                var contentResult = await _contentProcessingService.ProcessContentAsync(dto.Content, cancellationToken);
                if (contentResult.IsFailure)
                {
                    return Result<BookDto>.Failure($"Content processing failed: {contentResult.ErrorMessage}");
                }
                processedContent = contentResult.Value;
            }

            // Update book with processed content
            if (processedContent != null)
            {
                book.UpdateContent(book.Title, book.Description, processedContent.ContentUrl, processedContent.TotalPages);
            }

            // Handle cover image upload
            if (dto.CoverImage != null)
            {
                using var coverStream = dto.CoverImage.OpenReadStream();
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    coverStream, 
                    $"covers/{Guid.NewGuid()}_{dto.CoverImage.FileName}", 
                    dto.CoverImage.ContentType, 
                    cancellationToken);
                
                if (uploadResult.IsSuccess)
                {
                    book.SetCoverImage(uploadResult.Value!);
                }
            }

            // Save book
            var savedBook = await _bookRepository.AddAsync(book);

            // Map to DTO
            var bookDto = _mapper.Map<BookDto>(savedBook);

            _logger.LogInformation("Book created successfully: {BookId} by author {AuthorId}", savedBook.Id, authorId);

            return Result<BookDto>.Success(bookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book for author {AuthorId}", authorId);
            return Result<BookDto>.Failure("An error occurred while creating the book");
        }
    }

    public async Task<Result<BookDto>> UpdateBookAsync(int bookId, UpdateBookDto dto, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result<BookDto>.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result<BookDto>.ValidationFailure("Authorization", "You can only update your own books");
            }

            // Update basic properties
            var title = dto.Title ?? book.Title;
            var description = dto.Description ?? book.Description;
            var category = dto.Category ?? book.Category;

            // Update pricing if provided
            if (dto.PricePerPage.HasValue || dto.PricePerHour.HasValue)
            {
                var pricePerPage = dto.PricePerPage.HasValue ? new Money(dto.PricePerPage.Value) : book.PricePerPage;
                var pricePerHour = dto.PricePerHour.HasValue ? new Money(dto.PricePerHour.Value) : book.PricePerHour;
                book.SetPricing(pricePerPage, pricePerHour);
            }

            // Process new content if provided
            ProcessedContentDto? processedContent = null;
            if (dto.ContentFile != null)
            {
                using var stream = dto.ContentFile.OpenReadStream();
                var contentResult = await _contentProcessingService.ProcessPdfAsync(stream, dto.ContentFile.FileName, cancellationToken);
                if (contentResult.IsFailure)
                {
                    return Result<BookDto>.Failure($"Content processing failed: {contentResult.ErrorMessage}");
                }
                processedContent = contentResult.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                var contentResult = await _contentProcessingService.ProcessContentAsync(dto.Content, cancellationToken);
                if (contentResult.IsFailure)
                {
                    return Result<BookDto>.Failure($"Content processing failed: {contentResult.ErrorMessage}");
                }
                processedContent = contentResult.Value;
            }

            // Update content
            var contentUrl = processedContent?.ContentUrl ?? book.ContentUrl;
            var totalPages = processedContent?.TotalPages ?? book.TotalPages;
            book.UpdateContent(title, description, contentUrl, totalPages);

            // Handle cover image update
            if (dto.CoverImage != null)
            {
                using var coverStream = dto.CoverImage.OpenReadStream();
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    coverStream, 
                    $"covers/{Guid.NewGuid()}_{dto.CoverImage.FileName}", 
                    dto.CoverImage.ContentType, 
                    cancellationToken);
                
                if (uploadResult.IsSuccess)
                {
                    // Delete old cover image if exists
                    if (!string.IsNullOrEmpty(book.CoverImageUrl))
                    {
                        await _fileStorageService.DeleteFileAsync(book.CoverImageUrl, cancellationToken);
                    }
                    
                    book.SetCoverImage(uploadResult.Value!);
                }
            }

            await _bookRepository.UpdateAsync(book);

            var bookDto = _mapper.Map<BookDto>(book);

            _logger.LogInformation("Book updated successfully: {BookId}", bookId);

            return Result<BookDto>.Success(bookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book {BookId}", bookId);
            return Result<BookDto>.Failure("An error occurred while updating the book");
        }
    }

    public async Task<Result<BookDto>> GetBookByIdAsync(int bookId, int? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result<BookDto>.ValidationFailure("Book", "Book not found");
            }

            // Check access permissions
            if (userId.HasValue && !book.CanBeAccessedBy(userId.Value))
            {
                return Result<BookDto>.ValidationFailure("Access", "You don't have permission to access this book");
            }

            var bookDto = _mapper.Map<BookDto>(book);

            return Result<BookDto>.Success(bookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving book {BookId}", bookId);
            return Result<BookDto>.Failure("An error occurred while retrieving the book");
        }
    }

    public async Task<Result<BookSearchResultDto>> SearchBooksAsync(BookSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var books = await _bookRepository.SearchAsync(searchDto);
            var totalCount = await _bookRepository.GetSearchCountAsync(searchDto);

            var bookListDtos = _mapper.Map<List<BookListDto>>(books);

            var result = new BookSearchResultDto
            {
                Books = bookListDtos,
                TotalCount = totalCount,
                Page = searchDto.Page,
                PageSize = searchDto.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize),
                HasNextPage = searchDto.Page * searchDto.PageSize < totalCount,
                HasPreviousPage = searchDto.Page > 1
            };

            return Result<BookSearchResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books with query: {Query}", searchDto.Query);
            return Result<BookSearchResultDto>.Failure("An error occurred while searching books");
        }
    }

    public async Task<Result<List<BookListDto>>> GetBooksByAuthorAsync(int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var books = await _bookRepository.GetByAuthorAsync(authorId);
            var bookListDtos = _mapper.Map<List<BookListDto>>(books);

            return Result<List<BookListDto>>.Success(bookListDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving books for author {AuthorId}", authorId);
            return Result<List<BookListDto>>.Failure("An error occurred while retrieving author's books");
        }
    }

    public async Task<Result<BookDto>> PublishBookAsync(int bookId, int authorId, PublishBookDto? publishDto = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result<BookDto>.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result<BookDto>.ValidationFailure("Authorization", "You can only publish your own books");
            }

            // Update pricing if provided
            if (publishDto != null && (publishDto.PricePerPage.HasValue || publishDto.PricePerHour.HasValue))
            {
                var pricePerPage = publishDto.PricePerPage.HasValue ? new Money(publishDto.PricePerPage.Value) : book.PricePerPage;
                var pricePerHour = publishDto.PricePerHour.HasValue ? new Money(publishDto.PricePerHour.Value) : book.PricePerHour;
                book.SetPricing(pricePerPage, pricePerHour);
            }

            // Perform plagiarism check before publishing
            if (!string.IsNullOrEmpty(book.ContentUrl))
            {
                var contentStream = await _fileStorageService.DownloadFileAsync(book.ContentUrl, cancellationToken);
                if (contentStream.IsSuccess)
                {
                    using var reader = new StreamReader(contentStream.Value!);
                    var content = await reader.ReadToEndAsync();
                    
                    var plagiarismResult = await _contentProcessingService.CheckPlagiarismAsync(content, cancellationToken);
                    if (plagiarismResult.IsSuccess && plagiarismResult.Value!.IsPlagiarized)
                    {
                        return Result<BookDto>.ValidationFailure("Content", 
                            $"Content failed plagiarism check with {plagiarismResult.Value.SimilarityPercentage:F1}% similarity");
                    }
                }
            }

            book.Publish();
            await _bookRepository.UpdateAsync(book);

            var bookDto = _mapper.Map<BookDto>(book);

            _logger.LogInformation("Book published successfully: {BookId}", bookId);

            return Result<BookDto>.Success(bookDto);
        }
        catch (InvalidOperationException ex)
        {
            return Result<BookDto>.ValidationFailure("Publish", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing book {BookId}", bookId);
            return Result<BookDto>.Failure("An error occurred while publishing the book");
        }
    }

    public async Task<Result> UnpublishBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result.ValidationFailure("Authorization", "You can only unpublish your own books");
            }

            book.Unpublish();
            await _bookRepository.UpdateAsync(book);

            _logger.LogInformation("Book unpublished successfully: {BookId}", bookId);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.ValidationFailure("Unpublish", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing book {BookId}", bookId);
            return Result.Failure("An error occurred while unpublishing the book");
        }
    }

    public async Task<Result> ArchiveBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result.ValidationFailure("Authorization", "You can only archive your own books");
            }

            book.Archive();
            await _bookRepository.UpdateAsync(book);

            _logger.LogInformation("Book archived successfully: {BookId}", bookId);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.ValidationFailure("Archive", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving book {BookId}", bookId);
            return Result.Failure("An error occurred while archiving the book");
        }
    }

    public async Task<Result> RestoreBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result.ValidationFailure("Authorization", "You can only restore your own books");
            }

            book.Restore();
            await _bookRepository.UpdateAsync(book);

            _logger.LogInformation("Book restored successfully: {BookId}", bookId);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.ValidationFailure("Restore", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring book {BookId}", bookId);
            return Result.Failure("An error occurred while restoring the book");
        }
    }

    public async Task<Result<PageContentDto>> GetPageContentAsync(int bookId, int pageNumber, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result<PageContentDto>.ValidationFailure("Book", "Book not found");
            }

            if (!book.CanBeAccessedBy(userId))
            {
                return Result<PageContentDto>.ValidationFailure("Access", "You don't have permission to access this book");
            }

            if (pageNumber < 1 || pageNumber > book.TotalPages)
            {
                return Result<PageContentDto>.ValidationFailure("Page", "Invalid page number");
            }

            // Validate content access through content protection service
            var accessValidation = await _contentProtectionService.ValidateContentAccessAsync(userId, bookId, pageNumber);
            if (!accessValidation)
            {
                return Result<PageContentDto>.ValidationFailure("Access", "Content access denied");
            }

            // Get page content (this would typically involve extracting specific page from stored content)
            var pageContent = await GetPageContentFromStorage(book.ContentUrl!, pageNumber);
            
            // Apply watermarking
            var watermarkedContent = await _contentProtectionService.ApplyWatermarkAsync(pageContent, userId);

            // Calculate charge if applicable
            decimal? chargeAmount = null;
            string? chargeType = null;

            if (book.PricePerPage != null)
            {
                chargeAmount = book.PricePerPage.Amount;
                chargeType = "per-page";
            }
            else if (book.PricePerHour != null)
            {
                // For per-hour pricing, we'd need to track reading time
                chargeAmount = book.PricePerHour.Amount;
                chargeType = "per-hour";
            }

            var result = new PageContentDto
            {
                BookId = bookId,
                PageNumber = pageNumber,
                Content = pageContent,
                WatermarkedContent = watermarkedContent,
                HasNextPage = pageNumber < book.TotalPages,
                HasPreviousPage = pageNumber > 1,
                TotalPages = book.TotalPages,
                ChargeAmount = chargeAmount,
                ChargeType = chargeType
            };

            return Result<PageContentDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving page content for book {BookId}, page {PageNumber}", bookId, pageNumber);
            return Result<PageContentDto>.Failure("An error occurred while retrieving page content");
        }
    }

    public async Task<Result<BookAnalyticsDto>> GetBookAnalyticsAsync(int bookId, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result<BookAnalyticsDto>.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result<BookAnalyticsDto>.ValidationFailure("Authorization", "You can only view analytics for your own books");
            }

            // Get analytics data from repository
            var analytics = await _bookRepository.GetBookAnalyticsAsync(bookId);

            return Result<BookAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for book {BookId}", bookId);
            return Result<BookAnalyticsDto>.Failure("An error occurred while retrieving book analytics");
        }
    }

    public async Task<Result> DeleteBookAsync(int bookId, int authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result.ValidationFailure("Book", "Book not found");
            }

            if (book.AuthorId != authorId)
            {
                return Result.ValidationFailure("Authorization", "You can only delete your own books");
            }

            // Delete associated files
            if (!string.IsNullOrEmpty(book.ContentUrl))
            {
                await _fileStorageService.DeleteFileAsync(book.ContentUrl, cancellationToken);
            }

            if (!string.IsNullOrEmpty(book.CoverImageUrl))
            {
                await _fileStorageService.DeleteFileAsync(book.CoverImageUrl, cancellationToken);
            }

            await _bookRepository.DeleteAsync(bookId);

            _logger.LogInformation("Book deleted successfully: {BookId}", bookId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book {BookId}", bookId);
            return Result.Failure("An error occurred while deleting the book");
        }
    }

    private async Task<string> GetPageContentFromStorage(string contentUrl, int pageNumber)
    {
        // This is a simplified implementation
        // In a real system, you would extract the specific page content from the stored file
        // For now, we'll return a placeholder
        await Task.Delay(10); // Simulate async operation
        return $"Content for page {pageNumber} from {contentUrl}";
    }
}