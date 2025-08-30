using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
            
        // Configure Email value object
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(255);
                
            email.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
        });
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();
            
        // Configure UserProfile value object as JSON
        builder.OwnsOne(u => u.Profile, profile =>
        {
            profile.Property(p => p.Bio)
                .HasColumnName("ProfileBio")
                .HasMaxLength(1000);
                
            profile.Property(p => p.Website)
                .HasColumnName("ProfileWebsite")
                .HasMaxLength(500);
                
            profile.Property(p => p.TwitterHandle)
                .HasColumnName("ProfileTwitterHandle")
                .HasMaxLength(100);
                
            profile.Property(p => p.LinkedInProfile)
                .HasColumnName("ProfileLinkedInProfile")
                .HasMaxLength(500);
                
            profile.Property(p => p.AvatarUrl)
                .HasColumnName("ProfileAvatarUrl")
                .HasMaxLength(1000);
                
            profile.Property(p => p.Location)
                .HasColumnName("ProfileLocation")
                .HasMaxLength(200);
                
            profile.Property(p => p.Interests)
                .HasColumnName("ProfileInterests")
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        });
        
        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(u => u.EmailVerifiedAt);
        
        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(255);
            
        builder.Property(u => u.ResetPasswordToken)
            .HasMaxLength(255);
            
        builder.Property(u => u.ResetPasswordTokenExpiry);
        
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(u => u.LastLoginAt);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
            
        // Indexes
        builder.HasIndex(u => u.EmailVerificationToken)
            .HasDatabaseName("IX_Users_EmailVerificationToken");
            
        builder.HasIndex(u => u.ResetPasswordToken)
            .HasDatabaseName("IX_Users_ResetPasswordToken");
            
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");
            
        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");
            
        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("IX_Users_LastLoginAt");
            
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");
            
        // Ignore domain events for EF
        builder.Ignore(u => u.DomainEvents);
    }
}