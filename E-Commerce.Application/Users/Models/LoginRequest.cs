namespace E_Commerce.Application.Users.Models;

public sealed class LoginRequest
{
    public string EmailOrUsername { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

