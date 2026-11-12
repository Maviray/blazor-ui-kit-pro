namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Renders a column graphically (as a badge) instead of plain text. Each cell's colour comes from the column's
/// entry in <see cref="Models.Grid.GridFetchOptions{TDataItem,TModel}.CellThemes"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridAdvancedDisplayAttribute : Attribute;
