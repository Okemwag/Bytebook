using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Infrastructure.Configurations;

public class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("Readings");
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(r => r.UserId)
            .IsRequired();
            
        builder.Property(r => r.BookId)
            .IsRequired();
            
        builder.Property(r => r.StartTime)
            .IsRequired();
            
        builder.Property(r => r.EndTime);
        
        builder.Property(r => r.PagesRead)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(r => r.TimeSpentMinutes)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(r => r.LastPageRead)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(r => r.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ReadingStatus.Active);
            
        // Configure Money value object for ChargedAmount
        builder.OwnsOne(r => r.ChargedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ChargedAmount")
                .HasColumnType("decimal(10,2)");
                
            money.Property(m => m.Currency)
                .HasColumnName("ChargedCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });
        
        builder.Property(r => r.ChargeType)
            .HasConversion<int>();
            
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Indexes
        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("IX_Readings_UserId");
            
        builder.HasIndex(r => r.BookId)
            .HasDatabaseName("IX_Readings_BookId");
            
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Readings_Status");
            
        builder.HasIndex(r => r.IsCompleted)
            .HasDatabaseName("IX_Readings_IsCompleted");
            
        builder.HasIndex(r => r.StartTime)
            .HasDatabaseName("IX_Readings_StartTime");
            
        builder.HasIndex(r => r.EndTime)
            .HasDatabaseName("IX_Readings_EndTime");
            
        builder.HasIndex(r => new { r.UserId, r.BookId })
            .HasDatabaseName("IX_Readings_UserId_BookId");
            
        builder.HasIndex(r => new { r.UserId, r.StartTime })
            .HasDatabaseName("IX_Readings_UserId_StartTime");
            
        // Ignore domain events for EF
        builder.Ignore(r => r.DomainEvents);
    }
}