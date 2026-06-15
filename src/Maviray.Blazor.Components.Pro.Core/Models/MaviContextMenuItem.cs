using Maviray.Blazor.Components.Pro.Core.Interfaces;

namespace Maviray.Blazor.Components.Pro.Core.Models;

public class MaviContextMenuItem : IMaviContextMenuItem
{
    public string? Key { get; set; }
    public string? Title { get; set; }
    public string? Css { get; set; }
    public bool? Disabled { get; set; }
}