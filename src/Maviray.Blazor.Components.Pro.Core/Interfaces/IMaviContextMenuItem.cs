namespace Maviray.Blazor.Components.Pro.Core.Interfaces;

public interface IMaviContextMenuItem
{
    string? Key { get; set; }
    string? Title { get; set; }
    string? Css { get; set; }
    public bool? Disabled { get; set; }
}