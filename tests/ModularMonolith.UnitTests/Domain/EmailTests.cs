using FluentAssertions;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.UnitTests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("test123@test-domain.com")]
    public void From_ValidEmail_ShouldCreateEmailInstance(string emailValue)
    {
        var email = Email.From(emailValue);

        email.Should().NotBeNull();
        email.Value.Should().Be(emailValue.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhiteSpaceEmail_ShouldThrowArgumentException(string emailValue)
    {
        var act = () => new Email(emailValue);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email cannot be null or empty*");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user name@example.com")]
    [InlineData("user@domain")]
    public void Constructor_InvalidEmailFormat_ShouldThrowArgumentException(string emailValue)
    {
        var act = () => new Email(emailValue);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void Constructor_EmailExceeds255Characters_ShouldThrowArgumentException()
    {
        var longEmail = new string('a', 250) + "@test.com";

        var act = () => new Email(longEmail);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email cannot exceed 255 characters*");
    }

    [Fact]
    public void Constructor_ShouldNormalizeEmail()
    {
        var email = new Email("  Test@EXAMPLE.COM  ");

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Equals_SameEmailValue_ShouldBeEqual()
    {
        var email1 = Email.From("test@example.com");
        var email2 = Email.From("test@example.com");

        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentEmailValue_ShouldNotBeEqual()
    {
        var email1 = Email.From("test1@example.com");
        var email2 = Email.From("test2@example.com");

        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameEmailValue_ShouldHaveSameHashCode()
    {
        var email1 = Email.From("test@example.com");
        var email2 = Email.From("test@example.com");

        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        var email = Email.From("test@example.com");

        email.ToString().Should().Be("test@example.com");
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldCreateEmail()
    {
        Email email = "test@example.com";

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        var email = Email.From("test@example.com");
        string emailString = email;

        emailString.Should().Be("test@example.com");
    }
}
