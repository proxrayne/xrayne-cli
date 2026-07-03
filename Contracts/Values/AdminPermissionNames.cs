using Contracts.Enums;

namespace Contracts.Values;

public static class AdminPermissionNames
{
    public const string CreateUsers = "create_users";
    public const string EditUsers = "edit_users";
    public const string DeleteUsers = "delete_users";
    public const string ResetTraffic = "reset_traffic";
    public const string ChangeXraySettings = "change_xray_settings";
    public const string ViewLogs = "view_logs";
    public const string ManageAdmins = "manage_admins";
    public const string SuperAdmin = "super_admin";

    private static readonly IReadOnlyDictionary<string, AdminPermission> NameToPermission =
        new Dictionary<string, AdminPermission>(StringComparer.OrdinalIgnoreCase)
        {
            [CreateUsers] = AdminPermission.CreateUsers,
            [EditUsers] = AdminPermission.EditUsers,
            [DeleteUsers] = AdminPermission.DeleteUsers,
            [ResetTraffic] = AdminPermission.ResetTraffic,
            [ChangeXraySettings] = AdminPermission.ChangeXraySettings,
            [ViewLogs] = AdminPermission.ViewLogs,
            [ManageAdmins] = AdminPermission.ManageAdmins,
            [SuperAdmin] = AdminPermission.SuperAdmin
        };

    public static AdminPermission ParseMany(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AdminPermission.None;
        }

        var permissions = AdminPermission.None;
        var names = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var name in names)
        {
            if (!NameToPermission.TryGetValue(name, out var permission))
            {
                throw new ArgumentException($"Unknown permission: {name}", nameof(value));
            }

            permissions |= permission;
        }

        return permissions;
    }

    public static IReadOnlyCollection<string> ToNames(AdminPermission permissions)
    {
        if (permissions.HasFlag(AdminPermission.SuperAdmin))
        {
            return [SuperAdmin];
        }

        return NameToPermission
            .Where(item =>
                item.Value != AdminPermission.SuperAdmin &&
                permissions.HasFlag(item.Value))
            .Select(item => item.Key)
            .ToArray();
    }
}
