using Maviray.Blazor.Components.Core.Models.Tables;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

public class MaviGridRow<TKey> where TKey : IEquatable<TKey>
{
    public TKey? Key { get; set; }
    public string Guid { get; init; } = System.Guid.NewGuid().ToString();
    public bool Selected { get; set; }
    public bool Active { get; set; }

    public bool ContextMenuVisible { get; set; }
    public string ContextMenuId => $"context-menu-{Guid}";

    public List<MaviGridCell<TKey>> Cells { get; set; } = [];
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public List<IMaviRowContextMenuItem<TKey>> ContextActions { get; set; } = [];
}