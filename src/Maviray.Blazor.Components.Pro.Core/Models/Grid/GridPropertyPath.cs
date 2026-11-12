namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// Where a column sorts and filters on the data item: a dot-path, and the value a <see langword="null"/> at that
/// path counts as.
/// </summary>
internal sealed record GridPropertyPath(string DataItemPath, object? NullDefault);
