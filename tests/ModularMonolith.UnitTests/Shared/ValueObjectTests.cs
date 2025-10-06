using FluentAssertions;
using ModularMonolith.Shared.Domain;

namespace ModularMonolith.UnitTests.Shared;

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ShouldNotBeEqual()
    {
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("456 Oak Ave", "Boston", "02101");

        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_OnePropertyDifferent_ShouldNotBeEqual()
    {
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10002");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_Null_ShouldNotBeEqual()
    {
        var address = new Address("123 Main St", "New York", "10001");
        Address? nullAddress = null;

        address.Should().NotBe(nullAddress);
        (address == nullAddress).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_ShouldNotBeEqual()
    {
        var address = new Address("123 Main St", "New York", "10001");
        var notAddress = "Not an address";

        address.Equals(notAddress).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldHaveSameHashCode()
    {
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ShouldHaveDifferentHashCode()
    {
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("456 Oak Ave", "Boston", "02101");

        address1.GetHashCode().Should().NotBe(address2.GetHashCode());
    }

    [Fact]
    public void Equals_WithNullProperties_ShouldHandleCorrectly()
    {
        var money1 = new Money(100, null);
        var money2 = new Money(100, null);
        var money3 = new Money(100, "USD");

        money1.Should().Be(money2);
        money1.Should().NotBe(money3);
    }

    private class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string ZipCode { get; }

        public Address(string street, string city, string zipCode)
        {
            Street = street;
            City = city;
            ZipCode = zipCode;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return ZipCode;
        }
    }

    private class Money : ValueObject
    {
        public decimal Amount { get; }
        public string? Currency { get; }

        public Money(decimal amount, string? currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }
}
