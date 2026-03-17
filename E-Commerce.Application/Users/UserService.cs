using E_Commerce.Application.Shared;
using E_Commerce.Application.Users.Models;
using E_Commerce.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Application.Users;

public sealed class UserService(
    IApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService) : IUserService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly ITokenService _tokenService = tokenService;

    public async Task<UserResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim();
        var normalizedUsername = request.Username.Trim();

        var exists = await _dbContext.Users
            .AnyAsync(u => u.Email == normalizedEmail || u.Username == normalizedUsername, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A user with the same email or username already exists.");
        }

        var user = new User(
            Guid.NewGuid(),
            request.FirstName,
            request.LastName,
            request.Username,
            request.Email,
            passwordHash: string.Empty);

        var hashedPassword = _passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hashedPassword);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToUserResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identifier = request.EmailOrUsername.Trim();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                u => u.Email == identifier || u.Username == identifier,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return _tokenService.GenerateToken(user);
    }

    public async Task<UserResponse?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var newEmail = request.Email.Trim();
        var newUsername = request.Username.Trim();

        var emailChanged = !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase);
        var usernameChanged = !string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase);

        if (emailChanged || usernameChanged)
        {
            var exists = await _dbContext.Users.AnyAsync(
                u => u.Id != userId &&
                     (u.Email == newEmail || u.Username == newUsername),
                cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("Email or user name is already in use by another user.");
            }
        }

        user.SetFirstName(request.FirstName);
        user.SetLastName(request.LastName);
        user.SetUsername(request.Username);
        user.SetEmail(request.Email);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var hashedPassword = _passwordHasher.HashPassword(user, request.Password);
            user.SetPasswordHash(hashedPassword);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToUserResponse(user);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : MapToUserResponse(user);
    }

    private static UserResponse MapToUserResponse(User user) =>
        new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email
        };
}

