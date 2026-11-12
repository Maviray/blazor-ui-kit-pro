namespace Maviray.Blazor.Components.Pro.Core.Access;

public enum GridAccessOutcome
{
    /// <summary>The policy has no rule for this request.</summary>
    NotApplicable,

    /// <summary>The fetch may run, optionally limited by a row filter.</summary>
    Allowed,

    /// <summary>The fetch must not run.</summary>
    Denied
}
