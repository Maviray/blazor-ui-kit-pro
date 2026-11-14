using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>A grid column, built from a model property. Immutable and shared by every page of the same grid.</summary>
public sealed class MaviGridColumn
{
    /// <summary>The model property name, or <c>Navigation.Property</c> for a nested column.</summary>
    public required string Key { get; init; }

    public required string Title { get; init; }

    /// <summary>Position in <see cref="MaviGridCollection{TKey}.Columns"/> and in each row's <see cref="MaviGridRow{TKey}.Cells"/>.</summary>
    public int Index { get; init; }

    public int Sequence { get; init; }

    public MaviGridPropertyType PropertyType { get; init; }

    public MaviGridColumnCellBehavior CellBehavior { get; init; }

    /// <summary>Cells render as badges coloured by their <see cref="MaviGridCell.ThemeColorScheme"/>.</summary>
    public bool GraphicDisplay { get; init; }

    /// <summary>
    /// The column's share of the grid's free width relative to other stretching columns, from
    /// <see cref="Attributes.Grid.GridStretchAttribute"/>; 0 for a column that keeps its width.
    /// </summary>
    public int StretchWeight { get; init; }

    /// <summary>The column resolves to a data item path, so the grid may sort by it.</summary>
    public bool Sortable { get; init; }

    /// <summary>The column resolves to a data item path, so the grid may filter by it.</summary>
    public bool Filterable { get; init; }

    /// <summary>Text of a <see langword="true"/> cell in a boolean column.</summary>
    public string TrueText { get; init; } = bool.TrueString;

    /// <summary>Text of a <see langword="false"/> cell in a boolean column.</summary>
    public string FalseText { get; init; } = bool.FalseString;

    /// <summary>The members of an enum column in declaration order, with the text shown for each.</summary>
    public IReadOnlyList<MaviGridEnumOption> EnumOptions { get; init; } = [];

    /// <summary>The same column at another position, when other columns are left out of the page.</summary>
    internal MaviGridColumn WithIndex(int index) => new()
    {
        Key = Key,
        Title = Title,
        Index = index,
        Sequence = Sequence,
        PropertyType = PropertyType,
        CellBehavior = CellBehavior,
        GraphicDisplay = GraphicDisplay,
        StretchWeight = StretchWeight,
        Sortable = Sortable,
        Filterable = Filterable,
        TrueText = TrueText,
        FalseText = FalseText,
        EnumOptions = EnumOptions
    };
}

/// <summary>An enum member: its <see cref="Name"/> (used in filters) and the <see cref="Text"/> shown in cells.</summary>
public readonly record struct MaviGridEnumOption(string Name, string Text);
