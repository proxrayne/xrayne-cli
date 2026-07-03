using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Contracts.Utilities;

public static class DynamicValueComparer
{
    public static bool AreEqual(object? left, object? right)
    {
        return AreEqual(left, right, new HashSet<ReferencePair>());
    }

    private static bool AreEqual(object? left, object? right, HashSet<ReferencePair> visited)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        left = NormalizeJsonValue(left);
        right = NormalizeJsonValue(right);

        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left is string leftString && right is string rightString)
        {
            return string.Equals(leftString, rightString, StringComparison.Ordinal);
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            return NumericValuesEqual(left, right);
        }

        if (IsSimpleValue(left) || IsSimpleValue(right))
        {
            return left.Equals(right);
        }

        if (!TrackReferencePair(left, right, visited))
        {
            return true;
        }

        if (TryAsDictionary(left, out var leftDictionary)
            && TryAsDictionary(right, out var rightDictionary))
        {
            return DictionariesEqual(leftDictionary, rightDictionary, visited);
        }

        if (left is IEnumerable leftEnumerable
            && right is IEnumerable rightEnumerable
            && left is not string
            && right is not string)
        {
            return EnumerablesEqual(leftEnumerable, rightEnumerable, visited);
        }

        if (left.GetType() == right.GetType())
        {
            return ObjectsEqual(left, right, visited);
        }

        return false;
    }

    private static object? NormalizeJsonValue(object? value)
    {
        return value switch
        {
            JsonElement element => NormalizeJsonElement(element),
            JsonNode node => NormalizeJsonNode(node),
            _ => value
        };
    }

    private static object? NormalizeJsonNode(JsonNode node)
    {
        return NormalizeJsonElement(node.Deserialize<JsonElement>());
    }

    private static object? NormalizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var integer)
                ? integer
                : element.GetDecimal(),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => NormalizeJsonElement(item))
                .ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => NormalizeJsonElement(property.Value),
                    StringComparer.Ordinal),
            _ => element.GetRawText()
        };
    }

    private static bool DictionariesEqual(
        IReadOnlyDictionary<string, object?> left,
        IReadOnlyDictionary<string, object?> right,
        HashSet<ReferencePair> visited)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, leftValue) in left)
        {
            if (!right.TryGetValue(key, out var rightValue))
            {
                return false;
            }

            if (!AreEqual(leftValue, rightValue, visited))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EnumerablesEqual(
        IEnumerable left,
        IEnumerable right,
        HashSet<ReferencePair> visited)
    {
        var leftEnumerator = left.GetEnumerator();
        var rightEnumerator = right.GetEnumerator();

        while (true)
        {
            var hasLeft = leftEnumerator.MoveNext();
            var hasRight = rightEnumerator.MoveNext();

            if (hasLeft != hasRight)
            {
                return false;
            }

            if (!hasLeft)
            {
                return true;
            }

            if (!AreEqual(leftEnumerator.Current, rightEnumerator.Current, visited))
            {
                return false;
            }
        }
    }

    private static bool ObjectsEqual(object left, object right, HashSet<ReferencePair> visited)
    {
        var properties = left.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            if (!AreEqual(property.GetValue(left), property.GetValue(right), visited))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryAsDictionary(
        object value,
        out IReadOnlyDictionary<string, object?> dictionary)
    {
        if (value is IReadOnlyDictionary<string, object?> typedDictionary)
        {
            dictionary = typedDictionary;
            return true;
        }

        if (value is IDictionary rawDictionary)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in rawDictionary)
            {
                if (entry.Key is not string key)
                {
                    dictionary = new Dictionary<string, object?>();
                    return false;
                }

                result[key] = entry.Value;
            }

            dictionary = result;
            return true;
        }

        dictionary = new Dictionary<string, object?>();
        return false;
    }

    private static bool IsSimpleValue(object value)
    {
        var type = value.GetType();

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }

    private static bool IsNumeric(object value)
    {
        return value is byte
            or sbyte
            or short
            or ushort
            or int
            or uint
            or long
            or ulong
            or float
            or double
            or decimal;
    }

    private static bool NumericValuesEqual(object left, object right)
    {
        if (left is double leftDouble && double.IsNaN(leftDouble))
        {
            return right is double rightDouble && double.IsNaN(rightDouble);
        }

        if (left is float leftFloat && float.IsNaN(leftFloat))
        {
            return right is float rightFloat && float.IsNaN(rightFloat);
        }

        try
        {
            var leftDecimal = Convert.ToDecimal(left, CultureInfo.InvariantCulture);
            var rightDecimal = Convert.ToDecimal(right, CultureInfo.InvariantCulture);

            return leftDecimal == rightDecimal;
        }
        catch (OverflowException)
        {
            return left.Equals(right);
        }
    }

    private static bool TrackReferencePair(object left, object right, HashSet<ReferencePair> visited)
    {
        if (left.GetType().IsValueType || right.GetType().IsValueType)
        {
            return true;
        }

        return visited.Add(new ReferencePair(left, right));
    }

    private readonly record struct ReferencePair(object Left, object Right);
}
