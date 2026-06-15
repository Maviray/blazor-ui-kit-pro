using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;

public class MaviRowContextMenuItem<TKey> : IMaviRowContextMenuItem<TKey> where TKey : IEquatable<TKey>
{
    public TKey? Key { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public ElementVariant ElementVariant { get; set; }
    public ThemeColorScheme ThemeColorScheme { get; set; }
    public ElementSize ElementSize { get; set; }
}