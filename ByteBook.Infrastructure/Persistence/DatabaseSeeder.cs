using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Apply any pending migrations
            if ((await _context.Database.GetPendingMigrationsAsync()).Any())
            {
                _logger.LogInformation("Applying pending migrations...");
                await _context.Database.MigrateAsync();
            }

            // Seed data if not already present
            if (!await _context.Users.AnyAsync())
            {
                await SeedUsersAsync();
            }

            if (!await _context.Books.AnyAsync())
            {
                await SeedBooksAsync();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedUsersAsync()
    {
        _logger.LogInformation("Seeding users...");

        var users = new List<User>
        {
            // Admin user
            new User("System", "Administrator", new Email("admin@bytebook.com"), 
                BCrypt.Net.BCrypt.HashPassword("Admin123!"), UserRole.Admin),

            // Sample authors
            new User("John", "Doe", new Email("john.doe@example.com"), 
                BCrypt.Net.BCrypt.HashPassword("Author123!"), UserRole.Author),
            
            new User("Jane", "Smith", new Email("jane.smith@example.com"), 
                BCrypt.Net.BCrypt.HashPassword("Author123!"), UserRole.Author),
            
            new User("Michael", "Johnson", new Email("michael.johnson@example.com"), 
                BCrypt.Net.BCrypt.HashPassword("Author123!"), UserRole.Author),

            // Sample readers
            new User("Alice", "Wilson", new Email("alice.wilson@example.com"), 
                BCrypt.Net.BCrypt.HashPassword("Reader123!"), UserRole.Reader),
            
            new User("Bob", "Brown", new Email("bob.brown@example.com"), 
                BCrypt.Net.BCrypt.HashPassword("Reader123!"), UserRole.Reader),
            
            new User("Carol", "Davis", new Email("carol.davis@example.com"), 
                BCrypt.Net.BCrypt.HashPassword("Reader123!"), UserRole.Reader)
        };

        // Verify emails for seeded users
        foreach (var user in users)
        {
            user.VerifyEmail(user.EmailVerificationToken!);
        }

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {users.Count} users");
    }

    private async Task SeedBooksAsync()
    {
        _logger.LogInformation("Seeding books...");

        // Get authors for book creation
        var authors = await _context.Users
            .Where(u => u.Role == UserRole.Author)
            .ToListAsync();

        if (!authors.Any())
        {
            _logger.LogWarning("No authors found for book seeding");
            return;
        }

        var books = new List<Book>();

        // Technology books
        var techBooks = new[]
        {
            new { Title = "Introduction to Machine Learning", Description = "A comprehensive guide to machine learning fundamentals and applications.", Category = "Technology", Author = authors[0] },
            new { Title = "Web Development with React", Description = "Learn modern web development using React and related technologies.", Category = "Technology", Author = authors[1] },
            new { Title = "Database Design Principles", Description = "Master the art of designing efficient and scalable databases.", Category = "Technology", Author = authors[2] },
            new { Title = "Cloud Computing Essentials", Description = "Understanding cloud platforms and deployment strategies.", Category = "Technology", Author = authors[0] }
        };

        foreach (var bookData in techBooks)
        {
            var book = new Book(bookData.Title, bookData.Description, bookData.Author.Id, bookData.Category);
            book.UpdateContent(bookData.Title, bookData.Description, $"content/{Guid.NewGuid()}.pdf", Random.Shared.Next(50, 300));
            book.SetPricing(new Money(Random.Shared.Next(5, 25) / 100m), new Money(Random.Shared.Next(500, 2000) / 100m));
            book.SetTags("programming,technology,tutorial,guide");
            book.Publish();
            books.Add(book);
        }

        // Business books
        var businessBooks = new[]
        {
            new { Title = "Startup Fundamentals", Description = "Essential knowledge for launching and growing a successful startup.", Category = "Business", Author = authors[1] },
            new { Title = "Digital Marketing Strategies", Description = "Effective marketing techniques for the digital age.", Category = "Business", Author = authors[2] },
            new { Title = "Financial Planning for Entrepreneurs", Description = "Managing finances and investments for business owners.", Category = "Business", Author = authors[0] }
        };

        foreach (var bookData in businessBooks)
        {
            var book = new Book(bookData.Title, bookData.Description, bookData.Author.Id, bookData.Category);
            book.UpdateContent(bookData.Title, bookData.Description, $"content/{Guid.NewGuid()}.pdf", Random.Shared.Next(40, 200));
            book.SetPricing(new Money(Random.Shared.Next(8, 30) / 100m), new Money(Random.Shared.Next(800, 2500) / 100m));
            book.SetTags("business,entrepreneurship,finance,marketing");
            book.Publish();
            books.Add(book);
        }

        // Science books
        var scienceBooks = new[]
        {
            new { Title = "Climate Change and Environmental Science", Description = "Understanding our planet's changing climate and environmental challenges.", Category = "Science", Author = authors[2] },
            new { Title = "Introduction to Quantum Physics", Description = "Exploring the fascinating world of quantum mechanics.", Category = "Science", Author = authors[1] },
            new { Title = "Biotechnology and Genetic Engineering", Description = "Modern advances in biotechnology and their applications.", Category = "Science", Author = authors[0] }
        };

        foreach (var bookData in scienceBooks)
        {
            var book = new Book(bookData.Title, bookData.Description, bookData.Author.Id, bookData.Category);
            book.UpdateContent(bookData.Title, bookData.Description, $"content/{Guid.NewGuid()}.pdf", Random.Shared.Next(60, 250));
            book.SetPricing(new Money(Random.Shared.Next(6, 20) / 100m), new Money(Random.Shared.Next(600, 1800) / 100m));
            book.SetTags("science,research,education,academic");
            book.Publish();
            books.Add(book);
        }

        await _context.Books.AddRangeAsync(books);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {books.Count} books");
    }
}