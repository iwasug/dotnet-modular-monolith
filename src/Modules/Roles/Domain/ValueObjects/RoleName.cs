using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Roles.Domain.ValueObjects;

/// <summary>
/// Value object representing a role name
/// </summary>
public sealed class RoleName : ValueObject
{
    public string Value { get; }

    public RoleName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(value));
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("Role name cannot exceed 100 characters", nameof(value));
        }

        Value = value.Trim();
    }

    /// <summary>
    /// Creates a RoleName from a string value
    /// </summary>
    public static RoleName From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant(); // Case-insensitive comparison
    }

    public override string ToString() => Value;

    public static implicit operator string(RoleName roleName) => roleName.Value;
    public static implicit operator RoleName(string value) => new(value);
}