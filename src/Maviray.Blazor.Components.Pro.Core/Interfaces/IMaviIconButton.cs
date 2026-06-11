using Maviray.Blazor.Components.Pro.Core.Enums;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces;

public interface IMaviIconButton : IMaviButton
{
    MaviIconColor IconColor { get; set; }
    MaviIconSize IconSize { get; set; }
}