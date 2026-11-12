namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>What a grid fetch is about to read.</summary>
/// <param name="DataItemType">The entity type the grid reads.</param>
/// <param name="ModelType">The grid model type the rows are built from.</param>
/// <param name="AccessTag">The grid's <c>GridFetchOptions.AccessTag</c>, if any.</param>
public sealed record GridAccessRequest(Type DataItemType, Type ModelType, string? AccessTag);
