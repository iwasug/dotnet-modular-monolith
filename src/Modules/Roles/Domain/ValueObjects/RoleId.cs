using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Roles.Domain.ValueObjects;

/// <summary>
/// Value object representing a role identifier
/// </summary>
public sealed class RoleId : ValueObject
{
    public Guid Value { get; }

    public RoleId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Role ID cannot be empty", nameof(value));
        }
        
        Value = value;
    }

    /// <summary>
    /// Creates a new RoleId with UUID v7 for better performance and ordering
    /// </summary>
    public static RoleId New() => new(Guid.CreateVersion7());

    /// <summary>
    /// Creates a RoleId from a Guid value
    /// </summary>
    public static RoleId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(RoleId roleId) => roleId.Value;
    public static implicit operator RoleId(Guid value) => new(value);
}