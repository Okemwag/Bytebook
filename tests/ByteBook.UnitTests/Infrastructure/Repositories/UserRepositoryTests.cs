using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;
using ByteBook.Infrastructure.Persistence;
using ByteBook.Infrastructure.Repositories;
using Xunit;

namespace ByteBook.UnitTests.Infrastructure.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var email = new Email("test@example.com");
        var user = new User("John", "Doe", email, "hashedPassword");
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(email.Value);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email.Value, result.Email.Value);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var email = new Email("test@example.com");
        var user = new User("John", "Doe", email, "hashedPassword");
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync(email.Value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetByVerificationTokenAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var email = new Email("test@example.com");
        var user = new User("John", "Doe", email, "hashedPassword");
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByVerificationTokenAsync(user.EmailVerificationToken!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByRoleAsync_WithValidRole_ReturnsUsersWithRole()
    {
        // Arrange
        var email1 = new Email("author1@example.com");
        var email2 = new Email("author2@example.com");
        var email3 = new Email("reader@example.com");
        
        var author1 = new User("Author", "One", email1, "hashedPassword", UserRole.Author);
        var author2 = new User("Author", "Two", email2, "hashedPassword", UserRole.Author);
        var reader = new User("Reader", "User", email3, "hashedPassword", UserRole.Reader);

        await _repository.AddAsync(author1);
        await _repository.AddAsync(author2);
        await _repository.AddAsync(reader);
        await _context.SaveChangesAsync();

        // Act
        var authors = await _repository.GetByRoleAsync(UserRole.Author);

        // Assert
        Assert.Equal(2, authors.Count());
        Assert.All(authors, u => Assert.Equal(UserRole.Author, u.Role));
    }

    [Fact]
    public async Task SearchUsersAsync_WithSearchTerm_ReturnsMatchingUsers()
    {
        // Arrange
        var email1 = new Email("john.doe@example.com");
        var email2 = new Email("jane.smith@example.com");
        var email3 = new Email("bob.johnson@example.com");
        
        var user1 = new User("John", "Doe", email1, "hashedPassword");
        var user2 = new User("Jane", "Smith", email2, "hashedPassword");
        var user3 = new User("Bob", "Johnson", email3, "hashedPassword");

        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);
        await _repository.AddAsync(user3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.SearchUsersAsync("john");

        // Assert
        Assert.Equal(2, results.Count()); // John Doe and Bob Johnson
        Assert.Contains(results, u => u.FirstName == "John");
        Assert.Contains(results, u => u.LastName == "Johnson");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}