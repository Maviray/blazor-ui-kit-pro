namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

public class MaviGridRow<TKey> where TKey : IEquatable<TKey>
{
    public TKey? Key { get; set; }
    public string Guid { get; init; } = System.Guid.NewGuid().ToString();
    public bool Selected { get; set; }
    public bool Active { get; set; }

    public List<MaviGridCell<TKey>> Cells { get; set; } = [];
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}