using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;
using ByteBook.Infrastructure.Persistence;
using ByteBook.Infrastructure.Repositories;
using Xunit;

namespace ByteBook.UnitTests.Infrastructure.Repositories;

public class BookRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BookRepository _repository;
    private readonly UserRepository _userRepository;

    public BookRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new BookRepository(_context);
        _userRepository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByAuthorAsync_WithValidAuthorId_ReturnsAuthorBooks()
    {
        // Arrange
        var author = await CreateTestAuthor();
        var book1 = new Book("Book 1", "Description 1", author.Id, "Technology");
        var book2 = new Book("Book 2", "Description 2", author.Id, "Science");
        
        await _repository.AddAsync(book1);
        await _repository.AddAsync(book2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAuthorAsync(author.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal(author.Id, b.AuthorId));
    }

    [Fact]
    public async Task GetPublishedBooksAsync_ReturnsOnlyPublishedBooks()
    {
        // Arrange
        var author = await CreateTestAuthor();
        var publishedBook = new Book("Published Book", "Description", author.Id, "Technology");
        var draftBook = new Book("Draft Book", "Description", author.Id, "Technology");
        
        publishedBook.SetPricing(new Money(0.10m), null);
        publishedBook.UpdateContent("Published Book", "Description", "content-url", 100);
        publishedBook.Publish();
        
        await _repository.AddAsync(publishedBook);
        await _repository.AddAsync(draftBook);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPublishedBooksAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result.First().IsPublished);
    }

    [Fact]
    public async Task GetByCategoryAsync_WithValidCategory_ReturnsBooksInCategory()
    {
        // Arrange
        var author = await CreateTestAuthor();
        var techBook = new Book("Tech Book", "Description", author.Id, "Technology");
        var scienceBook = new Book("Science Book", "Description", author.Id, "Science");
        
        // Publish both books
        foreach (var book in new[] { techBook, scienceBook })
        {
            book.SetPricing(new Money(0.10m), null);
            book.UpdateContent(book.Title, "Description", "content-url", 100);
            book.Publish();
        }
        
        await _repository.AddAsync(techBook);
        await _repository.AddAsync(scienceBook);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCategoryAsync("Technology");

        // Assert
        Assert.Single(result);
        Assert.Equal("Technology", result.First().Category);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_ReturnsMatchingBooks()
    {
        // Arrange
        var author = await CreateTestAuthor();
        var book1 = new Book("JavaScript Guide", "Learn JavaScript programming", author.Id, "Technology");
        var book2 = new Book("Python Basics", "Introduction to Python", author.Id, "Technology");
        var book3 = new Book("Cooking Tips", "How to cook better", author.Id, "Lifestyle");
        
        // Publish all books
        foreach (var book in new[] { book1, book2, book3 })
        {
            book.SetPricing(new Money(0.10m), null);
            book.UpdateContent(book.Title, book.Description, "content-url", 100);
            book.Publish();
        }
        
        await _repository.AddAsync(book1);
        await _repository.AddAsync(book2);
        await _repository.AddAsync(book3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("JavaScript");

        // Assert
        Assert.Single(result);
        Assert.Contains("JavaScript", result.First().Title);
    }

    [Fact]
    public async Task TitleExistsForAuthorAsync_WithExistingTitle_ReturnsTrue()
    {
        // Arrange
        var author = await CreateTestAuthor();
        var book = new Book("Existing Title", "Description", author.Id, "Technology");
        
        await _repository.AddAsync(book);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.TitleExistsForAuthorAsync("Existing Title", author.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TitleExistsForAuthorAsync_WithNonExistentTitle_ReturnsFalse()
    {
        // Arrange
        var author = await CreateTestAuthor();

        // Act
        var result = await _repository.TitleExistsForAuthorAsync("Non-existent Title", author.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        var author = await CreateTestAuthor();
        var book1 = new Book("Book 1", "Description", author.Id, "Technology");
        var book2 = new Book("Book 2", "Description", author.Id, "Science");
        var book3 = new Book("Book 3", "Description", author.Id, "Technology"); // Duplicate category
        
        // Publish all books
        foreach (var book in new[] { book1, book2, book3 })
        {
            book.SetPricing(new Money(0.10m), null);
            book.UpdateContent(book.Title, book.Description, "content-url", 100);
            book.Publish();
        }
        
        await _repository.AddAsync(book1);
        await _repository.AddAsync(book2);
        await _repository.AddAsync(book3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCategoriesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains("Technology", result);
        Assert.Contains("Science", result);
    }

    private async Task<User> CreateTestAuthor()
    {
        var email = new Email($"author{Guid.NewGuid()}@example.com");
        var author = new User("Test", "Author", email, "hashedPassword", UserRole.Author);
        await _userRepository.AddAsync(author);
        await _context.SaveChangesAsync();
        return author;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}