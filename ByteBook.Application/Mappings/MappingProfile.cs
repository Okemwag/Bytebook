using AutoMapper;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.DTOs.Books;
using ByteBook.Application.DTOs.Payments;
using ByteBook.Application.DTOs.Reading;
using ByteBook.Application.DTOs.Users;
using ByteBook.Domain.Entities;

namespace ByteBook.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateUserMappings();
        CreateBookMappings();
        CreatePaymentMappings();
        CreateReadingMappings();
    }

    private void CreateUserMappings()
    {
        // User entity mappings - simplified for current domain structure
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => MapUserProfile(src.Profile)));

        // Simple DTO mappings that don't require complex domain logic
        CreateMap<RegisterUserDto, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "Reader"))
            .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => MapRegisterDtoToProfile(src)));
    }

    private void CreateBookMappings()
    {
        // Book entity mappings - simplified for current domain structure
        CreateMap<Book, BookDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Book, BookListDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.Rating, opt => opt.Ignore()) // Will be calculated by service
            .ForMember(dest => dest.ReadCount, opt => opt.Ignore()); // Will be calculated by service

        CreateMap<CreateBookDto, Book>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ContentUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore())
            .ForMember(dest => dest.IsPublished, opt => opt.Ignore())
            .ForMember(dest => dest.PublishedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }

    private void CreatePaymentMappings()
    {
        // Payment entity mappings - simplified for current domain structure
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.BookTitle, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Details, opt => opt.Ignore()); // Will be mapped separately

        CreateMap<PaymentRequestDto, Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Amount, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ProcessedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }

    private void CreateReadingMappings()
    {
        // Reading entity mappings - simplified for current domain structure
        CreateMap<Reading, ReadingSessionDto>()
            .ForMember(dest => dest.BookTitle, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.EstimatedCharge, opt => opt.Ignore()) // Will be calculated by service
            .ForMember(dest => dest.ChargeType, opt => opt.Ignore()); // Will be set by service

        CreateMap<StartReadingSessionDto, Reading>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.EndTime, opt => opt.Ignore())
            .ForMember(dest => dest.PagesRead, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.TimeSpentMinutes, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.LastPageRead, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }

    // Helper methods for simple mappings
    private UserProfileDto? MapUserProfile(object profile)
    {
        // Simplified mapping - will be enhanced when UserProfile value object is implemented
        return new UserProfileDto();
    }

    private UserProfileDto MapRegisterDtoToProfile(RegisterUserDto dto)
    {
        return new UserProfileDto
        {
            Bio = dto.Bio,
            Website = dto.Website,
            SocialLinks = dto.SocialLinks
        };
    }
}