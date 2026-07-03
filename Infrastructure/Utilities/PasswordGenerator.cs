using System.Security.Cryptography;

namespace Infrastructure.Utilities;

public static class PasswordGenerator
{
    private const string DefaultChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#%^&*";

    public static string Generate(int length = 18)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Password length must be greater than zero.");
        }

        return RandomNumberGenerator.GetString(DefaultChars, length);
    }
}
