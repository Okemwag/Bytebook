using AutoMapper;
using ByteBook.Application.DTOs.Books;
using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ByteBook.UnitTests.Application.Services;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IContentProcessingService> _contentProcessingServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IContentProtectionService> _contentProtectionServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<BookService>> _loggerMock;
    private readonly BookService _bookService;

    public BookServiceTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _contentProcessingServiceMock = new Mock<IContentProcessingService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _contentProtectionServiceMock = new Mock<IContentProtectionService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<BookService>>();

        _bookService = new BookService(
            _bookRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contentProcessingServiceMock.Object,
            _fileStorageServiceMock.Object,
            _contentProtectionServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateBookAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var authorId = 1;
        var createBookDto = new CreateBookDto
        {
            Title = "Test Book",
            Description = "Test Description",
            Category = "Technology",
            PricePerPage = 0.50m,
            Content = "Test content for the book"
        };

        var author = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        var processedContent = new ProcessedContentDto
        {
            Content = "Processed test content",
            TotalPages = 10,
            ContentUrl = "https://example.com/content.txt"
        };

        var bookDto = new BookDto
        {
            Id = 1,
            Title = "Test Book",
            Description = "Test Description",
            AuthorId = authorId,
            Category = "Technology"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(authorId))
            .ReturnsAsync(author);

        _contentProcessingServiceMock.Setup(x => x.ProcessContentAsync(createBookDto.Content, default))
            .ReturnsAsync(ByteBook.Application.Common.Result<ProcessedContentDto>.Success(processedContent));

        _bookRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Book>()))
            .ReturnsAsync((Book book) => book);

        _mapperMock.Setup(x => x.Map<BookDto>(It.IsAny<Book>()))
            .Returns(bookDto);

        // Act
        var result = await _bookService.CreateBookAsync(createBookDto, authorId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Book", result.Value.Title);

        _userRepositoryMock.Verify(x => x.GetByIdAsync(authorId), Times.Once);
        _contentProcessingServiceMock.Verify(x => x.ProcessContentAsync(createBookDto.Content, default), Times.Once);
        _bookRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Book>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookAsync_WithNonExistentAuthor_ShouldReturnValidationFailure()
    {
        // Arrange
        var authorId = 999;
        var createBookDto = new CreateBookDto
        {
            Title = "Test Book",
            Description = "Test Description",
            Category = "Technology",
            PricePerPage = 0.50m,
            Content = "Test content"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(authorId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _bookService.CreateBookAsync(createBookDto, authorId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByIdAsync(authorId), Times.Once);
        _bookRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookAsync_WithContentProcessingFailure_ShouldReturnFailure()
    {
        // Arrange
        var authorId = 1;
        var createBookDto = new CreateBookDto
        {
            Title = "Test Book",
            Description = "Test Description",
            Category = "Technology",
            PricePerPage = 0.50m,
            Content = "Test content"
        };

        var author = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(authorId))
            .ReturnsAsync(author);

        _contentProcessingServiceMock.Setup(x => x.ProcessContentAsync(createBookDto.Content, default))
            .ReturnsAsync(ByteBook.Application.Common.Result<ProcessedContentDto>.Failure("Processing failed"));

        // Act
        var result = await _bookService.CreateBookAsync(createBookDto, authorId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Content processing failed", result.ErrorMessage ?? "");

        _bookRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBookAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var bookId = 1;
        var authorId = 1;
        var updateBookDto = new UpdateBookDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            PricePerPage = 0.75m
        };

        var book = new Book("Original Title", "Original Description", authorId, "Technology");
        var bookDto = new BookDto
        {
            Id = bookId,
            Title = "Updated Title",
            Description = "Updated Description",
            AuthorId = authorId
        };

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _bookRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Book>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(x => x.Map<BookDto>(It.IsAny<Book>()))
            .Returns(bookDto);

        // Act
        var result = await _bookService.UpdateBookAsync(bookId, updateBookDto, authorId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Title", result.Value.Title);

        _bookRepositoryMock.Verify(x => x.GetByIdAsync(bookId), Times.Once);
        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBookAsync_WithNonExistentBook_ShouldReturnValidationFailure()
    {
        // Arrange
        var bookId = 999;
        var authorId = 1;
        var updateBookDto = new UpdateBookDto
        {
            Title = "Updated Title"
        };

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _bookService.UpdateBookAsync(bookId, updateBookDto, authorId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _bookRepositoryMock.Verify(x => x.GetByIdAsync(bookId), Times.Once);
        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBookAsync_WithUnauthorizedUser_ShouldReturnValidationFailure()
    {
        // Arrange
        var bookId = 1;
        var authorId = 1;
        var unauthorizedUserId = 2;
        var updateBookDto = new UpdateBookDto
        {
            Title = "Updated Title"
        };

        var book = new Book("Original Title", "Original Description", authorId, "Technology");

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _bookService.UpdateBookAsync(bookId, updateBookDto, unauthorizedUserId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("your own books", result.ErrorMessage ?? "");

        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithValidId_ShouldReturnSuccess()
    {
        // Arrange
        var bookId = 1;
        var userId = 1;
        var book = new Book("Test Book", "Test Description", userId, "Technology");
        var bookDto = new BookDto
        {
            Id = bookId,
            Title = "Test Book",
            AuthorId = userId
        };

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _mapperMock.Setup(x => x.Map<BookDto>(book))
            .Returns(bookDto);

        // Act
        var result = await _bookService.GetBookByIdAsync(bookId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Book", result.Value.Title);

        _bookRepositoryMock.Verify(x => x.GetByIdAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithNonExistentBook_ShouldReturnValidationFailure()
    {
        // Arrange
        var bookId = 999;

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _bookService.GetBookByIdAsync(bookId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _bookRepositoryMock.Verify(x => x.GetByIdAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task PublishBookAsync_WithValidBook_ShouldReturnSuccess()
    {
        // Arrange
        var bookId = 1;
        var authorId = 1;
        var book = new Book("Test Book", "Test Description", authorId, "Technology");
        
        // Set up book with content and pricing
        book.UpdateContent("Test Book", "Test Description", "https://example.com/content.txt", 10);
        book.SetPricing(new Money(0.50m), null);

        var bookDto = new BookDto
        {
            Id = bookId,
            Title = "Test Book",
            IsPublished = true
        };

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _fileStorageServiceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<Stream>.Success(new MemoryStream()));

        _contentProcessingServiceMock.Setup(x => x.CheckPlagiarismAsync(It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<PlagiarismCheckDto>.Success(new PlagiarismCheckDto { IsPlagiarized = false }));

        _bookRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Book>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(x => x.Map<BookDto>(It.IsAny<Book>()))
            .Returns(bookDto);

        // Act
        var result = await _bookService.PublishBookAsync(bookId, authorId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsPublished);

        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>()), Times.Once);
    }

    [Fact]
    public async Task PublishBookAsync_WithPlagiarizedContent_ShouldReturnValidationFailure()
    {
        // Arrange
        var bookId = 1;
        var authorId = 1;
        var book = new Book("Test Book", "Test Description", authorId, "Technology");
        
        book.UpdateContent("Test Book", "Test Description", "https://example.com/content.txt", 10);
        book.SetPricing(new Money(0.50m), null);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _fileStorageServiceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<Stream>.Success(new MemoryStream()));

        _contentProcessingServiceMock.Setup(x => x.CheckPlagiarismAsync(It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<PlagiarismCheckDto>.Success(new PlagiarismCheckDto 
            { 
                IsPlagiarized = true, 
                SimilarityPercentage = 85.5m 
            }));

        // Act
        var result = await _bookService.PublishBookAsync(bookId, authorId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("plagiarism check", result.ErrorMessage ?? "");
        Assert.Contains("85.5%", result.ErrorMessage ?? "");

        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task GetPageContentAsync_WithValidAccess_ShouldReturnSuccess()
    {
        // Arrange
        var bookId = 1;
        var pageNumber = 1;
        var userId = 1;
        var book = new Book("Test Book", "Test Description", userId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "https://example.com/content.txt", 10);
        book.SetPricing(new Money(0.50m), null);
        book.Publish();

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _contentProtectionServiceMock.Setup(x => x.ValidateContentAccessAsync(userId, bookId, pageNumber))
            .ReturnsAsync(true);

        _contentProtectionServiceMock.Setup(x => x.ApplyWatermarkAsync(It.IsAny<string>(), userId))
            .ReturnsAsync("Watermarked content");

        // Act
        var result = await _bookService.GetPageContentAsync(bookId, pageNumber, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(bookId, result.Value.BookId);
        Assert.Equal(pageNumber, result.Value.PageNumber);
        Assert.Equal("Watermarked content", result.Value.WatermarkedContent);

        _contentProtectionServiceMock.Verify(x => x.ValidateContentAccessAsync(userId, bookId, pageNumber), Times.Once);
        _contentProtectionServiceMock.Verify(x => x.ApplyWatermarkAsync(It.IsAny<string>(), userId), Times.Once);
    }

    [Fact]
    public async Task GetPageContentAsync_WithInvalidAccess_ShouldReturnValidationFailure()
    {
        // Arrange
        var bookId = 1;
        var pageNumber = 1;
        var userId = 2;
        var authorId = 1;
        var book = new Book("Test Book", "Test Description", authorId, "Technology");

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _contentProtectionServiceMock.Setup(x => x.ValidateContentAccessAsync(userId, bookId, pageNumber))
            .ReturnsAsync(false);

        // Act
        var result = await _bookService.GetPageContentAsync(bookId, pageNumber, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("access denied", result.ErrorMessage ?? "");

        _contentProtectionServiceMock.Verify(x => x.ValidateContentAccessAsync(userId, bookId, pageNumber), Times.Once);
        _contentProtectionServiceMock.Verify(x => x.ApplyWatermarkAsync(It.IsAny<string>(), userId), Times.Never);
    }

    [Fact]
    public async Task DeleteBookAsync_WithValidAuthor_ShouldReturnSuccess()
    {
        // Arrange
        var bookId = 1;
        var authorId = 1;
        var book = new Book("Test Book", "Test Description", authorId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "https://example.com/content.txt", 10);
        book.SetCoverImage("https://example.com/cover.jpg");

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _fileStorageServiceMock.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result.Success());

        _bookRepositoryMock.Setup(x => x.DeleteAsync(bookId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _bookService.DeleteBookAsync(bookId, authorId);

        // Assert
        Assert.True(result.IsSuccess);

        _fileStorageServiceMock.Verify(x => x.DeleteFileAsync("https://example.com/content.txt", default), Times.Once);
        _fileStorageServiceMock.Verify(x => x.DeleteFileAsync("https://example.com/cover.jpg", default), Times.Once);
        _bookRepositoryMock.Verify(x => x.DeleteAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task DeleteBookAsync_WithUnauthorizedUser_ShouldReturnValidationFailure()
    {
        // Arrange
        var bookId = 1;
        var authorId = 1;
        var unauthorizedUserId = 2;
        var book = new Book("Test Book", "Test Description", authorId, "Technology");

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _bookService.DeleteBookAsync(bookId, unauthorizedUserId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("your own books", result.ErrorMessage ?? "");

        _bookRepositoryMock.Verify(x => x.DeleteAsync(bookId), Times.Never);
    }
}