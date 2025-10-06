using FluentAssertions;
using ModularMonolith.Shared.Services;

namespace ModularMonolith.UnitTests.Services;

public class SimplePasswordHashingServiceTests
{
    private readonly SimplePasswordHashingService _service;

    public SimplePasswordHashingServiceTests()
    {
        _service = new SimplePasswordHashingService();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        var password = "MySecurePassword123";

        var hashedPassword = _service.HashPassword(password);

        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_SamePassword_ShouldReturnSameHash()
    {
        var password = "MySecurePassword123";

        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_ShouldReturnDifferentHashes()
    {
        var password1 = "Password123";
        var password2 = "DifferentPassword456";

        var hash1 = _service.HashPassword(password1);
        var hash2 = _service.HashPassword(password2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        var password = "MySecurePassword123";
        var hashedPassword = _service.HashPassword(password);

        var result = _service.VerifyPassword(password, hashedPassword);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ShouldReturnFalse()
    {
        var password = "MySecurePassword123";
        var wrongPassword = "WrongPassword456";
        var hashedPassword = _service.HashPassword(password);

        var result = _service.VerifyPassword(wrongPassword, hashedPassword);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("password")]
    [InlineData("P@ssw0rd!")]
    [InlineData("12345678")]
    [InlineData("a very long password with spaces and special characters !@#$%")]
    public void HashPassword_VariousPasswords_ShouldHashSuccessfully(string password)
    {
        var hashedPassword = _service.HashPassword(password);

        hashedPassword.Should().NotBeNullOrEmpty();
        _service.VerifyPassword(password, hashedPassword).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_EmptyString_ShouldNotThrow()
    {
        var act = () => _service.HashPassword(string.Empty);

        act.Should().NotThrow();
    }
}
