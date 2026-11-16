using MeterFlow.Domain;

namespace MeterFlow.Domain.Tests;

public class EanTests
{
    private const string Valid = "123456789012345675";

    [Theory]
    [InlineData(Valid)]
    [InlineData("000000000000000000")]
    [InlineData("999999999999999995")]
    public void Parse_accepts_a_code_with_a_correct_check_digit(string value)
    {
        Ean ean = Ean.Parse(value);

        Assert.Equal(value, ean.Value);
    }

    [Theory]
    [InlineData("123456789012345670")]   // wrong check digit
    [InlineData("12345678901234567")]    // 17 digits
    [InlineData("1234567890123456750")]  // 19 digits
    [InlineData("12345678901234567X")]   // letter
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_codes_are_rejected(string? value)
    {
        Assert.False(Ean.TryParse(value, out Ean? ean));
        Assert.Null(ean);
        Assert.Throws<FormatException>(() => Ean.Parse(value!));
    }

    [Fact]
    public void TryParse_accepts_a_valid_code()
    {
        Assert.True(Ean.TryParse(Valid, out Ean? ean));
        Assert.Equal(Valid, ean.Value);
    }

    [Fact]
    public void ToString_returns_the_digits()
    {
        Assert.Equal(Valid, Ean.Parse(Valid).ToString());
    }

    [Fact]
    public void Two_eans_with_the_same_digits_are_equal()
    {
        Assert.Equal(Ean.Parse(Valid), Ean.Parse(Valid));
    }

    [Fact]
    public void Two_eans_with_different_digits_are_not_equal()
    {
        Assert.NotEqual(Ean.Parse(Valid), Ean.Parse("000000000000000000"));
    }
}
