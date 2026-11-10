using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;

public sealed class MaviRowContextMenuItem<TKey> : IMaviRowContextMenuItem<TKey> where TKey : IEquatable<TKey>
{
    public TKey? Key { get; init; }
    public string? Title { get; init; }
    public string? Icon { get; init; }
    public bool Disabled { get; init; }
    public ElementVariant ElementVariant { get; init; }
    public ThemeColorScheme ThemeColorScheme { get; init; }
    public ElementSize ElementSize { get; init; }
}
