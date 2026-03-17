using E_Commerce.Application.Users.Models;
using E_Commerce.Domain.Users;

namespace E_Commerce.Application.Users;

public interface ITokenService
{
    AuthResponse GenerateToken(User user);
}

