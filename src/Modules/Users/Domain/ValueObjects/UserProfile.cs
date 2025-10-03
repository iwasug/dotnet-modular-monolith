using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Users.Domain.ValueObjects;

/// <summary>
/// Value object representing user profile information
/// </summary>
public sealed class UserProfile : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    public UserProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be null or empty", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be null or empty", nameof(lastName));
        }

        if (firstName.Length > 100)
        {
            throw new ArgumentException("First name cannot exceed 100 characters", nameof(firstName));
        }

        if (lastName.Length > 100)
        {
            throw new ArgumentException("Last name cannot exceed 100 characters", nameof(lastName));
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    /// <summary>
    /// Creates a UserProfile from first and last name
    /// </summary>
    public static UserProfile Create(string firstName, string lastName) => new(firstName, lastName);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString() => FullName;
}