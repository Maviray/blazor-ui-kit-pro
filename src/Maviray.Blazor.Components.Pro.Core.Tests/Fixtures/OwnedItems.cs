namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public interface IOwnedItem
{
    int OwnerId { get; }
}

public class OwnedItem : IOwnedItem
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
}

public class OwnedBase
{
    public int OwnerId { get; set; }
}

public class DerivedOwnedItem : OwnedBase, IOwnedItem
{
    public int Id { get; set; }
}

public class ExplicitOwnedItem : IOwnedItem
{
    public int Id { get; set; }
    public int Owner { get; set; }
    int IOwnedItem.OwnerId => Owner;
}
