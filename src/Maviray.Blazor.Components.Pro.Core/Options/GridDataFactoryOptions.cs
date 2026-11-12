namespace Maviray.Blazor.Components.Pro.Core.Options;

public sealed class GridDataFactoryOptions
{
    /// <summary>
    /// When <see langword="true"/>, a fetch runs only if at least one access policy allows it; a fetch every policy
    /// finds not applicable (or with no policies registered) is denied.
    /// </summary>
    public bool RequireExplicitAllow { get; set; }

    /// <summary>The largest page the factory loads; larger requested page sizes are reduced to it.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// When <see langword="true"/> (the default), a fetch that carries <c>MaviGridQuery.KnownTotalRows</c> (a page,
    /// page size or sort change with the same filters) skips the count. The factory counts again when the loaded page
    /// doesn't hold the number of rows that total predicts.
    /// </summary>
    /// <remarks>
    /// Rows added or removed elsewhere can leave the total stale until the filters change or the grid refreshes, and so
    /// can a policy's row filter that changes between fetches: call <c>RefreshAsync</c> when a user's access changes.
    /// </remarks>
    public bool ReuseTotalRows { get; set; } = true;
}
