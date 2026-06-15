using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Core.Enums.Grid;

public enum MaviGridFilterCriteria
{
    [Display(Name = "None")]
    None,

    [Display(Name = "Equals")]
    Equals,

    [Display(Name = "Not Equals")]
    NotEquals,

    [Display(Name = "Contains")]
    Contains,

    [Display(Name = "Excludes")]
    Excludes,

    [Display(Name = "Less Than")]
    LessThan,

    [Display(Name = "Less Than Or Equal")]
    LessThanOrEqual,

    [Display(Name = "Greater Than")]
    GreaterThan,

    [Display(Name = "Greater Than Or Equal")]
    GreaterThanOrEqual,

    [Display(Name = "True/Selected")]
    IsTrue,

    [Display(Name = "False/Unselected")]
    IsFalse,

    [Display(Name = "Before")]
    Before,

    [Display(Name = "After")]
    After
}