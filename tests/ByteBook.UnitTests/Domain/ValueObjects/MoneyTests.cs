using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_WithValidAmountAndCurrency_ShouldCreateSuccessfully()
    {
        // Arrange
        var amount = 10.50m;
        var currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.Equal(amount, money.Amount);
        Assert.Equal(currency, money.Currency);
    }

    [Fact]
    public void Money_WithDefaultCurrency_ShouldUseUSD()
    {
        // Arrange
        var amount = 10.50m;

        // Act
        var money = new Money(amount);

        // Assert
        Assert.Equal(amount, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Money_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(-10.50m));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Money_WithEmptyOrWhitespaceCurrency_ShouldThrowArgumentException(string invalidCurrency)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(10.50m, invalidCurrency));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("1234")]
    public void Money_WithInvalidCurrencyLength_ShouldThrowArgumentException(string invalidCurrency)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(10.50m, invalidCurrency));
    }

    [Fact]
    public void Money_ShouldNormalizeCurrencyToUpperCase()
    {
        // Arrange
        var currency = "usd";

        // Act
        var money = new Money(10.50m, currency);

        // Assert
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Money_ShouldRoundAmountToTwoDecimalPlaces()
    {
        // Arrange
        var amount = 10.555m;

        // Act
        var money = new Money(amount);

        // Assert
        Assert.Equal(10.56m, money.Amount);
    }

    [Fact]
    public void Money_Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(5.25m, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        Assert.Equal(15.75m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Money_Add_WithDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(5.25m, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1.Add(money2));
    }

    [Fact]
    public void Money_Subtract_WithSameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(5.25m, "USD");

        // Act
        var result = money1.Subtract(money2);

        // Assert
        Assert.Equal(5.25m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Money_Subtract_WithDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(5.25m, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1.Subtract(money2));
    }

    [Fact]
    public void Money_Multiply_ShouldReturnProduct()
    {
        // Arrange
        var money = new Money(10.50m, "USD");
        var factor = 2m;

        // Act
        var result = money.Multiply(factor);

        // Assert
        Assert.Equal(21.00m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Money_Divide_ShouldReturnQuotient()
    {
        // Arrange
        var money = new Money(10.50m, "USD");
        var divisor = 2m;

        // Act
        var result = money.Divide(divisor);

        // Assert
        Assert.Equal(5.25m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Money_Divide_ByZero_ShouldThrowArgumentException()
    {
        // Arrange
        var money = new Money(10.50m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => money.Divide(0));
    }

    [Fact]
    public void Money_IsZero_WithZeroAmount_ShouldReturnTrue()
    {
        // Arrange
        var money = new Money(0m, "USD");

        // Act & Assert
        Assert.True(money.IsZero);
    }

    [Fact]
    public void Money_IsZero_WithNonZeroAmount_ShouldReturnFalse()
    {
        // Arrange
        var money = new Money(10.50m, "USD");

        // Act & Assert
        Assert.False(money.IsZero);
    }

    [Fact]
    public void Money_Zero_ShouldCreateZeroMoney()
    {
        // Act
        var money = Money.Zero("EUR");

        // Assert
        Assert.Equal(0m, money.Amount);
        Assert.Equal("EUR", money.Currency);
        Assert.True(money.IsZero);
    }

    [Fact]
    public void Money_OperatorOverloads_ShouldWork()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(5.25m, "USD");

        // Act & Assert
        var sum = money1 + money2;
        Assert.Equal(15.75m, sum.Amount);

        var difference = money1 - money2;
        Assert.Equal(5.25m, difference.Amount);

        var product = money1 * 2;
        Assert.Equal(21.00m, product.Amount);

        var quotient = money1 / 2;
        Assert.Equal(5.25m, quotient.Amount);
    }

    [Fact]
    public void Money_EqualityComparison_ShouldWork()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(10.50m, "USD");
        var money3 = new Money(5.25m, "USD");
        var money4 = new Money(10.50m, "EUR");

        // Act & Assert
        Assert.Equal(money1, money2);
        Assert.NotEqual(money1, money3);
        Assert.NotEqual(money1, money4);
    }
}