namespace E_Commerce.Application.Users.Models;

public sealed class AuthResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}

