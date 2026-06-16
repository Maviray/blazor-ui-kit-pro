namespace Maviray.Blazor.Components.Pro.Grid.Sample.Services;

/// <summary>
/// Demo implementation with a fixed scope. The seeder creates three scopes (Alpha=1, Bravo=2,
/// Gamma=3) and three auth areas (North=1, South=2, East=3); this user can see scopes Alpha and
/// Bravo within the North auth area only. Of the 27 seeded scoped items, the grid will therefore
/// show the 6 that match (2 scopes x 1 area x 3 items) without any per-page filter being set.
/// </summary>
public class DemoUserScopeContext : IUserScopeContext
{
    public IReadOnlyCollection<int> AllowedScopeIds { get; } = [1, 2];
    public IReadOnlyCollection<int> AllowedAuthAreaIds { get; } = [1];
}
