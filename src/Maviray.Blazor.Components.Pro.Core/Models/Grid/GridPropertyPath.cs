namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// A resolved <typeparamref name="TDataItem"/>-side sort/filter target for a <c>TModel</c> property:
/// the dot-path to walk, and an optional value a <see langword="null"/> at that path should be
/// coalesced to before comparing/ordering. See <see cref="GridPropertyPathMap{TDataItem,TModel}"/>.
/// </summary>
internal sealed record GridPropertyPath(string DataItemPath, object? NullDefault);
