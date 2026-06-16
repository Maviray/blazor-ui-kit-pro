namespace Maviray.Blazor.Components.Pro.Grid.Sample.Services;

/// <summary>
/// Represents the data-access scope of the current user. In a real application this would be derived
/// from the authenticated principal (claims, roles, a permissions service, etc.). Here it is a fixed
/// demo value so the effect of the global grid filter is easy to observe.
/// </summary>
public interface IUserScopeContext
{
    IReadOnlyCollection<int> AllowedScopeIds { get; }
    IReadOnlyCollection<int> AllowedAuthAreaIds { get; }
}
