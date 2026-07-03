using System.Text;
using System.Text.Json;
using Contracts.Models;

namespace Contracts.Utilities;

public static class CursorPagination
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaxLimit);
    }

    public static string CreateCursor(DateTimeOffset createdAt, object id)
    {
        var payload = JsonSerializer.Serialize(new CursorPosition(createdAt, id.ToString()!), JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);

        return Convert.ToBase64String(bytes);
    }

    public static CursorPosition? TryReadCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var payload = Encoding.UTF8.GetString(bytes);

            return JsonSerializer.Deserialize<CursorPosition>(payload, JsonOptions);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
