using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Contracts.Values;
using Infrastructure.Utilities;
using Data;
using Data.Contracts;
using Data.Entities;

namespace Cli.Commands.Admin;

public sealed class AdminCreateCommand : Command
{
    public AdminCreateCommand(IServiceProvider serviceProvider)
        : base("create", "Create an administrator account")
    {
        SetAction(async (_, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            return await ExecuteAsync(
                scope.ServiceProvider,
                cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var adminAccounts = serviceProvider.GetRequiredService<IAdminAccountRepository>();
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<AdminCreateCommand>>();

        try
        {
            var input = ReadAdminInput(console);

            await serviceProvider.MigrateDatabaseAsync(cancellationToken);

            var exists = await adminAccounts.ExistsAsync(input.Username, cancellationToken);
            if (exists)
            {
                console.Error($"Admin account '{input.Username}' already exists.");

                return 1;
            }

            var permissions = AdminPermissionNames.ParseMany(input.PermissionsValue);
            var account = new AdminAccount
            {
                Username = input.Username,
                PasswordHash = IdentityPasswordHasher.HashPassword(input.Password),
                Permissions = permissions
            };

            await adminAccounts.AddAsync(account, cancellationToken);

            logger.LogInformation("Admin account {Username} created.", input.Username);
            console.Header("Administrator account created");
            console.Value("Username", input.Username);
            console.Value("Password", input.Password);
            console.Value("Permissions", input.PermissionsValue);
            console.Success($"admin account '{input.Username}' created.");

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create admin account.");
            console.Error(exception.Message);

            return 1;
        }
    }

    private static AdminInput ReadAdminInput(ICliConsole console)
    {
        console.Header("Create administrator account");

        var username = ReadRequiredValue("Username: ");
        var password = ReadPasswordWithConfirmation();
        var permissions = ReadOptionalValue(
            "Permissions [super_admin]: ",
            "super_admin");

        return new AdminInput(username, password, permissions);
    }

    private static string ReadRequiredValue(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.WriteLine("Value is required.");
        }
    }

    private static string ReadOptionalValue(
        string prompt,
        string defaultValue)
    {
        Console.Write(prompt);
        var value = Console.ReadLine()?.Trim();

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static string ReadPasswordWithConfirmation()
    {
        while (true)
        {
            var password = ReadSecret("Password (empty to generate): ");
            if (string.IsNullOrWhiteSpace(password))
            {
                var generatedPassword = PasswordGenerator.Generate();
                Console.WriteLine("Password will be generated automatically.");

                return generatedPassword;
            }

            var confirmation = ReadSecret("Confirm password: ");
            if (string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                return password;
            }

            Console.WriteLine("Passwords do not match.");
        }
    }

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(chars.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count == 0)
                {
                    continue;
                }

                chars.RemoveAt(chars.Count - 1);
                Console.Write("\b \b");
                continue;
            }

            if (char.IsControl(key.KeyChar))
            {
                continue;
            }

            chars.Add(key.KeyChar);
            Console.Write('*');
        }
    }

    private sealed record AdminInput(
        string Username,
        string Password,
        string PermissionsValue);
}
