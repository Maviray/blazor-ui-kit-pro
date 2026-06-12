using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces;

public interface IMaviIconButton : IMaviButton
{
    ElementColor IconColor { get; set; }
    ElementSize IconSize { get; set; }
}