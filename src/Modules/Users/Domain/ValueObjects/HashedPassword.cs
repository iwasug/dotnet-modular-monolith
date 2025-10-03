using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Users.Domain.ValueObjects;

/// <summary>
/// Value object representing a hashed password
/// </summary>
public sealed class HashedPassword : ValueObject
{
    public string Value { get; }

    public HashedPassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(value));
        }

        if (value.Length < 10) // BCrypt hashes are typically much longer
        {
            throw new ArgumentException("Invalid hashed password format", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a HashedPassword from a string value
    /// </summary>
    public static HashedPassword From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(HashedPassword hashedPassword) => hashedPassword.Value;
    public static implicit operator HashedPassword(string value) => new(value);
}