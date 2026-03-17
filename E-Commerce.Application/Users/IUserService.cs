using E_Commerce.Application.Users.Models;

namespace E_Commerce.Application.Users;

public interface IUserService
{
    Task<UserResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

