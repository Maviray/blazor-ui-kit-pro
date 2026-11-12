using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>An access policy's answer for one fetch.</summary>
public sealed class GridAccessDecision<TDataItem> where TDataItem : class
{
    private static readonly IReadOnlySet<string> NoColumns = new HashSet<string>(StringComparer.Ordinal);
    private static readonly GridAccessDecision<TDataItem> NotApplicableDecision = new(GridAccessOutcome.NotApplicable, null, null, NoColumns);
    private static readonly GridAccessDecision<TDataItem> AllowAllDecision = new(GridAccessOutcome.Allowed, null, null, NoColumns);
    private static readonly GridAccessDecision<TDataItem> DenyDecision = new(GridAccessOutcome.Denied, null, null, NoColumns);

    private GridAccessDecision(
        GridAccessOutcome outcome, Expression<Func<TDataItem, bool>>? rowFilter, string? deniedMessage, IReadOnlySet<string> hiddenColumns)
    {
        Outcome = outcome;
        RowFilter = rowFilter;
        DeniedMessage = deniedMessage;
        HiddenColumns = hiddenColumns;
    }

    public GridAccessOutcome Outcome { get; }

    /// <summary>For <see cref="GridAccessOutcome.Allowed"/>: the rows the user may see, or <see langword="null"/> for all rows.</summary>
    public Expression<Func<TDataItem, bool>>? RowFilter { get; }

    /// <summary>For <see cref="GridAccessOutcome.Denied"/>: optional text shown to the user.</summary>
    public string? DeniedMessage { get; }

    /// <summary>
    /// Keys of the columns the user may not see (model property names, or <c>Navigation.Property</c> for nested
    /// columns). The page leaves them out entirely, and sorting and filtering on them are ignored. The hidden columns
    /// of every policy are combined.
    /// </summary>
    public IReadOnlySet<string> HiddenColumns { get; }

    public static GridAccessDecision<TDataItem> NotApplicable() => NotApplicableDecision;

    public static GridAccessDecision<TDataItem> Allow(Expression<Func<TDataItem, bool>>? rowFilter = null) =>
        rowFilter is null ? AllowAllDecision : new GridAccessDecision<TDataItem>(GridAccessOutcome.Allowed, rowFilter, null, NoColumns);

    public static GridAccessDecision<TDataItem> Deny(string? message = null) =>
        message is null ? DenyDecision : new GridAccessDecision<TDataItem>(GridAccessOutcome.Denied, null, message, NoColumns);

    /// <summary>
    /// The same decision, also hiding <paramref name="columnKeys"/>. On <see cref="NotApplicable"/> it hides columns
    /// without allowing any rows, for a policy that only governs columns.
    /// </summary>
    /// <exception cref="InvalidOperationException">The decision is a denial, which shows no columns' data at all.</exception>
    public GridAccessDecision<TDataItem> WithHiddenColumns(params IEnumerable<string> columnKeys)
    {
        ArgumentNullException.ThrowIfNull(columnKeys);

        if (Outcome == GridAccessOutcome.Denied)
        {
            throw new InvalidOperationException("A denied fetch shows no rows, so there are no columns to hide.");
        }

        var hidden = new HashSet<string>(HiddenColumns, StringComparer.Ordinal);
        hidden.UnionWith(columnKeys);

        return new GridAccessDecision<TDataItem>(Outcome, RowFilter, DeniedMessage, hidden);
    }

    /// <summary>The same outcome, message and hidden columns with another row filter; for rebinding to a subtype.</summary>
    internal GridAccessDecision<TOther> Rebind<TOther>(Expression<Func<TOther, bool>>? rowFilter) where TOther : class =>
        Outcome switch
        {
            GridAccessOutcome.Allowed when rowFilter is null && HiddenColumns.Count == 0 => GridAccessDecision<TOther>.Allow(),
            GridAccessOutcome.Denied => GridAccessDecision<TOther>.Deny(DeniedMessage),
            GridAccessOutcome.NotApplicable when HiddenColumns.Count == 0 => GridAccessDecision<TOther>.NotApplicable(),
            _ => new GridAccessDecision<TOther>(Outcome, rowFilter, null, HiddenColumns)
        };
}
