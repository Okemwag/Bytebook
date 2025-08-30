using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Infrastructure.Events;

namespace ByteBook.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Reading> Readings { get; set; }
    public DbSet<Referral> Referrals { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher domainEventDispatcher) 
        : base(options) 
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps before saving
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = DateTime.UtcNow;
            }
            entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }

        // Collect domain events before saving
        var domainEvents = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Save changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        if (_domainEventDispatcher != null && domainEvents.Any())
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        // Clear domain events from entities
        foreach (var entity in ChangeTracker.Entries<BaseEntity>().Select(e => e.Entity))
        {
            entity.ClearDomainEvents();
        }

        return result;
    }
}
