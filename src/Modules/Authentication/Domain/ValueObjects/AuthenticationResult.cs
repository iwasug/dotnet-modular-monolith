using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Authentication.Domain.ValueObjects;

/// <summary>
/// Value object representing the result of an authentication operation
/// </summary>
public sealed class AuthenticationResult : ValueObject
{
    public bool IsSuccess { get; }
    public TokenResult? TokenResult { get; }
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }

    private AuthenticationResult(bool isSuccess, TokenResult? tokenResult, string? errorMessage, string? errorCode)
    {
        IsSuccess = isSuccess;
        TokenResult = tokenResult;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a successful authentication result with tokens
    /// </summary>
    public static AuthenticationResult Success(TokenResult tokenResult)
    {
        if (tokenResult == null)
        {
            throw new ArgumentNullException(nameof(tokenResult));
        }

        return new AuthenticationResult(true, tokenResult, null, null);
    }

    /// <summary>
    /// Creates a failed authentication result with error details
    /// </summary>
    public static AuthenticationResult Failure(string errorCode, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Error code cannot be null or empty", nameof(errorCode));
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));
        }

        return new AuthenticationResult(false, null, errorMessage, errorCode);
    }

    /// <summary>
    /// Creates a failed authentication result for invalid credentials
    /// </summary>
    public static AuthenticationResult InvalidCredentials()
    {
        return Failure("INVALID_CREDENTIALS", "Invalid email or password");
    }

    /// <summary>
    /// Creates a failed authentication result for inactive user
    /// </summary>
    public static AuthenticationResult UserInactive()
    {
        return Failure("USER_INACTIVE", "User account is inactive");
    }

    /// <summary>
    /// Creates a failed authentication result for invalid refresh token
    /// </summary>
    public static AuthenticationResult InvalidRefreshToken()
    {
        return Failure("INVALID_REFRESH_TOKEN", "Invalid or expired refresh token");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsSuccess;
        yield return TokenResult;
        yield return ErrorMessage;
        yield return ErrorCode;
    }

    public override string ToString()
    {
        return IsSuccess 
            ? "Authentication successful" 
            : $"Authentication failed: {ErrorCode} - {ErrorMessage}";
    }
}