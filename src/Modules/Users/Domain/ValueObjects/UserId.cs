using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Users.Domain.ValueObjects;

/// <summary>
/// Value object representing a user identifier
/// </summary>
public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(value));
        }
        
        Value = value;
    }

    /// <summary>
    /// Creates a new UserId with UUID v7 for better performance and ordering
    /// </summary>
    public static UserId New() => new(Guid.CreateVersion7());

    /// <summary>
    /// Creates a UserId from a Guid value
    /// </summary>
    public static UserId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserId userId) => userId.Value;
    public static implicit operator UserId(Guid value) => new(value);
}