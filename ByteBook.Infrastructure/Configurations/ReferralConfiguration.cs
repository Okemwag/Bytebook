using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Infrastructure.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals");
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(r => r.ReferrerId)
            .IsRequired();
            
        builder.Property(r => r.ReferredUserId);
        
        builder.Property(r => r.ReferralCode)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ReferralStatus.Pending);
            
        builder.Property(r => r.ConvertedAt);
        
        // Configure Money value object for CommissionEarned
        builder.OwnsOne(r => r.CommissionEarned, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CommissionEarnedAmount")
                .HasColumnType("decimal(10,2)");
                
            money.Property(m => m.Currency)
                .HasColumnName("CommissionEarnedCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });
        
        builder.Property(r => r.CommissionRate)
            .IsRequired()
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(0.10m);
            
        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(r => r.TriggerPaymentId);
        
        // Configure Money value object for TriggerAmount
        builder.OwnsOne(r => r.TriggerAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TriggerAmount")
                .HasColumnType("decimal(10,2)");
                
            money.Property(m => m.Currency)
                .HasColumnName("TriggerCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });
        
        builder.Property(r => r.CommissionPaidAt);
        
        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReferrerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReferredUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(r => r.TriggerPaymentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Indexes
        builder.HasIndex(r => r.ReferrerId)
            .HasDatabaseName("IX_Referrals_ReferrerId");
            
        builder.HasIndex(r => r.ReferredUserId)
            .HasDatabaseName("IX_Referrals_ReferredUserId");
            
        builder.HasIndex(r => r.ReferralCode)
            .IsUnique()
            .HasDatabaseName("IX_Referrals_ReferralCode");
            
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Referrals_Status");
            
        builder.HasIndex(r => r.Type)
            .HasDatabaseName("IX_Referrals_Type");
            
        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Referrals_IsActive");
            
        builder.HasIndex(r => r.ConvertedAt)
            .HasDatabaseName("IX_Referrals_ConvertedAt");
            
        builder.HasIndex(r => r.TriggerPaymentId)
            .HasDatabaseName("IX_Referrals_TriggerPaymentId");
            
        // Ignore domain events for EF
        builder.Ignore(r => r.DomainEvents);
    }
}