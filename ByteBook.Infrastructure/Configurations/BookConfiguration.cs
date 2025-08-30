using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Infrastructure.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(b => b.Description)
            .IsRequired()
            .HasColumnType("text");
            
        builder.Property(b => b.AuthorId)
            .IsRequired();
            
        builder.Property(b => b.Category)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(BookStatus.Draft);
            
        builder.Property(b => b.ContentUrl)
            .HasMaxLength(1000);
            
        builder.Property(b => b.CoverImageUrl)
            .HasMaxLength(1000);
            
        builder.Property(b => b.TotalPages)
            .IsRequired()
            .HasDefaultValue(0);
            
        // Configure Money value objects
        builder.OwnsOne(b => b.PricePerPage, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PricePerPageAmount")
                .HasColumnType("decimal(10,4)");
                
            money.Property(m => m.Currency)
                .HasColumnName("PricePerPageCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });
        
        builder.OwnsOne(b => b.PricePerHour, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PricePerHourAmount")
                .HasColumnType("decimal(10,2)");
                
            money.Property(m => m.Currency)
                .HasColumnName("PricePerHourCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });
        
        builder.Property(b => b.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(b => b.PublishedAt);
        
        builder.Property(b => b.Tags)
            .HasMaxLength(1000);
            
        builder.Property(b => b.AverageRating)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0);
            
        builder.Property(b => b.ReviewCount)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        builder.Property(b => b.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Indexes
        builder.HasIndex(b => b.AuthorId)
            .HasDatabaseName("IX_Books_AuthorId");
            
        builder.HasIndex(b => b.Category)
            .HasDatabaseName("IX_Books_Category");
            
        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Books_Status");
            
        builder.HasIndex(b => b.IsPublished)
            .HasDatabaseName("IX_Books_IsPublished");
            
        builder.HasIndex(b => b.IsActive)
            .HasDatabaseName("IX_Books_IsActive");
            
        builder.HasIndex(b => b.PublishedAt)
            .HasDatabaseName("IX_Books_PublishedAt");
            
        builder.HasIndex(b => new { b.Title, b.AuthorId })
            .HasDatabaseName("IX_Books_Title_AuthorId");
            
        builder.HasIndex(b => b.AverageRating)
            .HasDatabaseName("IX_Books_AverageRating");
            
        builder.HasIndex(b => b.ReviewCount)
            .HasDatabaseName("IX_Books_ReviewCount");
            
        builder.HasIndex(b => b.CreatedAt)
            .HasDatabaseName("IX_Books_CreatedAt");
            
        // Ignore domain events for EF
        builder.Ignore(b => b.DomainEvents);
    }
}