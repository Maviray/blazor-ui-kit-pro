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
}
