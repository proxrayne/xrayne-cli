using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Utilities;

public static class IdentityPasswordHasher
{
    private static readonly object User = new();
    private static readonly PasswordHasher<object> passwordHasher = new();

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return passwordHasher.HashPassword(User, password);
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var result = passwordHasher.VerifyHashedPassword(User, passwordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
