using System.Collections.Concurrent;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>Cached checks of whether a dot-path names public members of <typeparamref name="TDataItem"/>.</summary>
internal static class GridDataItemPaths<TDataItem>
{
    private static readonly ConcurrentDictionary<string, bool> Known = new(StringComparer.Ordinal);

    public static bool Exists(string path) => Known.GetOrAdd(path, static p => Walk(p));

    private static bool Walk(string path)
    {
        var type = typeof(TDataItem);

        foreach (var segment in path.Split('.'))
        {
            MemberInfo? member = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance)
                ?? (MemberInfo?)type.GetField(segment, BindingFlags.Public | BindingFlags.Instance);

            switch (member)
            {
                case PropertyInfo property:
                    type = property.PropertyType;
                    break;
                case FieldInfo field:
                    type = field.FieldType;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
