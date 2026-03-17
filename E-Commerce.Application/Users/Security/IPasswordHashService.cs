namespace E_Commerce.Application.Users.Security;

public interface IPasswordHashService
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

