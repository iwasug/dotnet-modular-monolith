using ModularMonolith.Shared.Domain;
using System.Text.RegularExpressions;

namespace ModularMonolith.Users.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(value));
        }

        if (value.Length > 255)
        {
            throw new ArgumentException("Email cannot exceed 255 characters", nameof(value));
        }

        var normalizedEmail = value.Trim().ToLowerInvariant();
        
        if (!EmailRegex.IsMatch(normalizedEmail))
        {
            throw new ArgumentException("Invalid email format", nameof(value));
        }

        Value = normalizedEmail;
    }

    /// <summary>
    /// Creates an Email from a string value
    /// </summary>
    public static Email From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string value) => new(value);
}