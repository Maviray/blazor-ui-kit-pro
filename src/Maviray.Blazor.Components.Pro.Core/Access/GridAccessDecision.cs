using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>An access policy's answer for one fetch.</summary>
public sealed class GridAccessDecision<TDataItem> where TDataItem : class
{
    private static readonly GridAccessDecision<TDataItem> NotApplicableDecision = new(GridAccessOutcome.NotApplicable, null, null);
    private static readonly GridAccessDecision<TDataItem> AllowAllDecision = new(GridAccessOutcome.Allowed, null, null);
    private static readonly GridAccessDecision<TDataItem> DenyDecision = new(GridAccessOutcome.Denied, null, null);

    private GridAccessDecision(GridAccessOutcome outcome, Expression<Func<TDataItem, bool>>? rowFilter, string? deniedMessage)
    {
        Outcome = outcome;
        RowFilter = rowFilter;
        DeniedMessage = deniedMessage;
    }

    public GridAccessOutcome Outcome { get; }

    /// <summary>For <see cref="GridAccessOutcome.Allowed"/>: the rows the user may see, or <see langword="null"/> for all rows.</summary>
    public Expression<Func<TDataItem, bool>>? RowFilter { get; }

    /// <summary>For <see cref="GridAccessOutcome.Denied"/>: optional text shown to the user.</summary>
    public string? DeniedMessage { get; }

    public static GridAccessDecision<TDataItem> NotApplicable() => NotApplicableDecision;

    public static GridAccessDecision<TDataItem> Allow(Expression<Func<TDataItem, bool>>? rowFilter = null) =>
        rowFilter is null ? AllowAllDecision : new GridAccessDecision<TDataItem>(GridAccessOutcome.Allowed, rowFilter, null);

    public static GridAccessDecision<TDataItem> Deny(string? message = null) =>
        message is null ? DenyDecision : new GridAccessDecision<TDataItem>(GridAccessOutcome.Denied, null, message);
}
