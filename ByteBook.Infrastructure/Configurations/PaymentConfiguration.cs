using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(p => p.UserId)
            .IsRequired();
            
        builder.Property(p => p.BookId)
            .IsRequired();
            
        // Configure Money value object for Amount
        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
                
            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsRequired();
        });
        
        builder.Property(p => p.PaymentType)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PaymentStatus.Pending);
            
        builder.Property(p => p.Provider)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(p => p.ExternalTransactionId)
            .HasMaxLength(255);
            
        builder.Property(p => p.ProcessedAt);
        
        builder.Property(p => p.FailureReason)
            .HasMaxLength(1000);
            
        // Configure Money value object for RefundedAmount
        builder.OwnsOne(p => p.RefundedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("RefundedAmount")
                .HasColumnType("decimal(10,2)");
                
            money.Property(m => m.Currency)
                .HasColumnName("RefundedCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });
        
        builder.Property(p => p.RefundedAt);
        
        builder.Property(p => p.ReadingSessionId);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(p => p.BookId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne<Reading>()
            .WithMany()
            .HasForeignKey(p => p.ReadingSessionId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Indexes
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Payments_UserId");
            
        builder.HasIndex(p => p.BookId)
            .HasDatabaseName("IX_Payments_BookId");
            
        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payments_Status");
            
        builder.HasIndex(p => p.Provider)
            .HasDatabaseName("IX_Payments_Provider");
            
        builder.HasIndex(p => p.ExternalTransactionId)
            .HasDatabaseName("IX_Payments_ExternalTransactionId");
            
        builder.HasIndex(p => p.ReadingSessionId)
            .HasDatabaseName("IX_Payments_ReadingSessionId");
            
        builder.HasIndex(p => p.ProcessedAt)
            .HasDatabaseName("IX_Payments_ProcessedAt");
            
        builder.HasIndex(p => new { p.UserId, p.BookId })
            .HasDatabaseName("IX_Payments_UserId_BookId");
            
        builder.HasIndex(p => new { p.Status, p.ProcessedAt })
            .HasDatabaseName("IX_Payments_Status_ProcessedAt");
            
        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");
            
        // Ignore domain events for EF
        builder.Ignore(p => p.DomainEvents);
    }
}