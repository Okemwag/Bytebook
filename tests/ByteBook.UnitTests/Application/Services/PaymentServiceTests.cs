using AutoMapper;
using ByteBook.Application.DTOs.Payments;
using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ByteBook.UnitTests.Application.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IReadingRepository> _readingRepositoryMock;
    private readonly Mock<IPaymentProcessor> _paymentProcessorMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _readingRepositoryMock = new Mock<IReadingRepository>();
        _paymentProcessorMock = new Mock<IPaymentProcessor>();
        _mapperMock = new Mock<IMapper>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        // Setup configuration
        _configurationMock.Setup(x => x["Payment:PlatformCommissionRate"]).Returns("0.15");
        _paymentProcessorMock.Setup(x => x.ProviderName).Returns("Stripe");

        var processors = new List<IPaymentProcessor> { _paymentProcessorMock.Object };

        _paymentService = new PaymentService(
            _paymentRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _userRepositoryMock.Object,
            _readingRepositoryMock.Object,
            processors,
            _mapperMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var request = new PaymentRequestDto
        {
            BookId = bookId,
            PaymentType = "PerPage",
            Provider = "Stripe",
            PagesRead = 5,
            PaymentMethodId = "pm_test_123"
        };

        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        var book = new Book("Test Book", "Test Description", 2, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 100);
        book.SetPricing(new Money(0.50m), null);
        book.Publish();

        var payment = new Payment(userId, bookId, new Money(2.50m), PaymentType.PerPage, PaymentProvider.Stripe);
        
        var processorResult = new PaymentResultDto
        {
            IsSuccess = true,
            PaymentId = "pi_test_123",
            ExternalTransactionId = "pi_test_123",
            Amount = 2.50m,
            Currency = "USD",
            Status = "completed"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _paymentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync(payment);

        _paymentProcessorMock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentProcessorRequest>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<PaymentResultDto>.Success(processorResult));

        _paymentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsSuccess);
        Assert.Equal("pi_test_123", result.Value.PaymentId);

        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);
        _paymentProcessorMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentProcessorRequest>(), default), Times.Once);
        _paymentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Once);
    }  
  [Fact]
    public async Task ProcessPaymentAsync_WithNonExistentUser_ShouldReturnValidationFailure()
    {
        // Arrange
        var userId = 999;
        var request = new PaymentRequestDto
        {
            BookId = 1,
            PaymentType = "PerPage",
            Provider = "Stripe",
            PagesRead = 5
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithNonExistentBook_ShouldReturnValidationFailure()
    {
        // Arrange
        var userId = 1;
        var bookId = 999;
        var request = new PaymentRequestDto
        {
            BookId = bookId,
            PaymentType = "PerPage",
            Provider = "Stripe",
            PagesRead = 5
        };

        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithUnavailableProcessor_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var request = new PaymentRequestDto
        {
            BookId = bookId,
            PaymentType = "PerPage",
            Provider = "PayPal", // Different from mock processor
            PagesRead = 5
        };

        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        var book = new Book("Test Book", "Test Description", 2, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 100);
        book.SetPricing(new Money(0.50m), null);
        book.Publish();

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _paymentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync(new Payment(userId, bookId, new Money(2.50m), PaymentType.PerPage, PaymentProvider.PayPal));

        _paymentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not available", result.ErrorMessage ?? "");

        _paymentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Once); // Should mark as failed
    }

    [Fact]
    public async Task CalculateChargesAsync_WithPerPagePricing_ShouldReturnCorrectAmount()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var pagesRead = 10;

        var book = new Book("Test Book", "Test Description", 2, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 100);
        book.SetPricing(new Money(0.25m), null);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _paymentService.CalculateChargesAsync(userId, bookId, pagesRead);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2.50m, result.Value); // 10 pages * $0.25
    }

    [Fact]
    public async Task CalculateChargesAsync_WithAuthorAccess_ShouldReturnZero()
    {
        // Arrange
        var authorId = 2;
        var bookId = 1;
        var pagesRead = 10;

        var book = new Book("Test Book", "Test Description", authorId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 100);
        book.SetPricing(new Money(0.25m), null);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _paymentService.CalculateChargesAsync(authorId, bookId, pagesRead);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value); // Author doesn't pay for their own book
    }

    [Fact]
    public async Task ProcessRefundAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var userId = 1;
        var paymentId = 1;
        var request = new RefundRequestDto
        {
            PaymentId = paymentId,
            Amount = 1.00m,
            Reason = "Customer request"
        };

        var payment = new Payment(userId, 1, new Money(2.50m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("pi_test_123");
        payment.MarkAsCompleted();

        var book = new Book("Test Book", "Test Description", userId, "Technology");

        var refundResult = new RefundResultDto
        {
            IsSuccess = true,
            RefundId = "re_test_123",
            Amount = 1.00m,
            Currency = "USD",
            Status = "succeeded"
        };

        _paymentRepositoryMock.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(payment.BookId))
            .ReturnsAsync(book);

        _paymentProcessorMock.Setup(x => x.ProcessRefundAsync(It.IsAny<PaymentProcessorRefundRequest>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<RefundResultDto>.Success(refundResult));

        _paymentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.ProcessRefundAsync(request, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsSuccess);
        Assert.Equal("re_test_123", result.Value.RefundId);

        _paymentProcessorMock.Verify(x => x.ProcessRefundAsync(It.IsAny<PaymentProcessorRefundRequest>(), default), Times.Once);
        _paymentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRefundAsync_WithNonRefundablePayment_ShouldReturnValidationFailure()
    {
        // Arrange
        var userId = 1;
        var paymentId = 1;
        var request = new RefundRequestDto
        {
            PaymentId = paymentId,
            Reason = "Customer request"
        };

        var payment = new Payment(userId, 1, new Money(2.50m), PaymentType.PerPage, PaymentProvider.Stripe);
        // Payment is still pending, not refundable

        _paymentRepositoryMock.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act
        var result = await _paymentService.ProcessRefundAsync(request, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not refundable", result.ErrorMessage ?? "");

        _paymentProcessorMock.Verify(x => x.ProcessRefundAsync(It.IsAny<PaymentProcessorRefundRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WithValidPayment_ShouldReturnSuccess()
    {
        // Arrange
        var userId = 1;
        var paymentId = 1;

        var payment = new Payment(userId, 1, new Money(2.50m), PaymentType.PerPage, PaymentProvider.Stripe);
        var book = new Book("Test Book", "Test Description", 2, "Technology");
        var paymentDto = new PaymentDto
        {
            Id = paymentId,
            UserId = userId,
            BookId = 1,
            Amount = 2.50m,
            Currency = "USD"
        };

        _paymentRepositoryMock.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(payment.BookId))
            .ReturnsAsync(book);

        _mapperMock.Setup(x => x.Map<PaymentDto>(payment))
            .Returns(paymentDto);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(paymentId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(paymentId, result.Value.Id);
        Assert.Equal("Test Book", result.Value.BookTitle);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WithUnauthorizedUser_ShouldReturnValidationFailure()
    {
        // Arrange
        var userId = 1;
        var unauthorizedUserId = 2;
        var paymentId = 1;

        var payment = new Payment(userId, 1, new Money(2.50m), PaymentType.PerPage, PaymentProvider.Stripe);
        var book = new Book("Test Book", "Test Description", 3, "Technology"); // Different author

        _paymentRepositoryMock.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(payment.BookId))
            .ReturnsAsync(book);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(paymentId, unauthorizedUserId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("permission", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task HandleWebhookAsync_WithValidWebhook_ShouldReturnSuccess()
    {
        // Arrange
        var provider = "Stripe";
        var payload = "{}";
        var signature = "test_signature";

        var webhookValidation = new WebhookValidationResult
        {
            IsValid = true,
            EventType = "payment.completed",
            ExternalTransactionId = "pi_test_123",
            Data = new Dictionary<string, object>()
        };

        var payment = new Payment(1, 1, new Money(2.50m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("pi_test_123");

        _paymentProcessorMock.Setup(x => x.ValidateWebhookAsync(payload, signature, default))
            .ReturnsAsync(ByteBook.Application.Common.Result<WebhookValidationResult>.Success(webhookValidation));

        _paymentRepositoryMock.Setup(x => x.GetByExternalTransactionIdAsync("pi_test_123"))
            .ReturnsAsync(payment);

        _paymentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.HandleWebhookAsync(provider, payload, signature);

        // Assert
        Assert.True(result.IsSuccess);

        _paymentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Once);
    }
}