namespace Contracts.Enums;

[Flags]
public enum AdminPermission : long
{
    None = 0,
    CreateUsers = 1L << 1,
    EditUsers = 1L << 2,
    DeleteUsers = 1L << 3,
    ResetTraffic = 1L << 4,
    ChangeXraySettings = 1L << 5,
    ViewLogs = 1L << 6,
    ManageAdmins = 1L << 7,
    SuperAdmin = 1L << 8
}
