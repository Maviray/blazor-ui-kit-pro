using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

public static class EnumExtensions
{
    public static string GetEnumDisplayName(this Enum enumValue)
    {
        var displayAttribute = enumValue
            .GetType()
            .GetField(enumValue.ToString())
            ?.GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? enumValue.ToString();
    }
}