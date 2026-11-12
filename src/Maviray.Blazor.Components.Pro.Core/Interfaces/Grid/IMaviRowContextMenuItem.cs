using Maviray.Blazor.Components.Core.Enums;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>An action shown on a grid row, as an icon button or a drop-down entry.</summary>
public interface IMaviRowContextMenuItem<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>Key of the row the action belongs to.</summary>
    TKey? Key { get; }

    string? Title { get; }

    string? Icon { get; }

    bool Disabled { get; }

    ElementVariant ElementVariant { get; }

    ThemeColorScheme ThemeColorScheme { get; }

    ElementSize ElementSize { get; }
}
