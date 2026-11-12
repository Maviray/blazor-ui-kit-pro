using Maviray.Blazor.Components.Material.Constants;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Components.Docs;

/// <summary>Class strings shared by the documentation page, built from the theme's type scale and colour tokens.</summary>
internal static class DocStyles
{
    public static readonly string PageTitle = $"{Tailwind.Theme.Typography.DISPLAY_SMALL} mb-3";

    public static readonly string Lead = $"{Tailwind.Theme.Typography.BODY_LARGE} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_EIGHT_TEXT}";

    public static readonly string SectionTitle = $"{Tailwind.Theme.Typography.HEADLINE_SMALL} mb-2";

    public static readonly string SubTitle = $"{Tailwind.Theme.Typography.TITLE_MEDIUM} mt-8 mb-2";

    public static readonly string Body = $"{Tailwind.Theme.Typography.BODY_MEDIUM} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_EIGHT_TEXT}";

    public static readonly string Small = $"{Tailwind.Theme.Typography.BODY_SMALL} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_EIGHT_TEXT}";

    public static readonly string Label = $"{Tailwind.Theme.Typography.LABEL_LARGE} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_NINE_TEXT}";

    public const string CodeBlock =
        "block rounded-md border border-(--theme-default-three) bg-(--theme-default-one) p-4 pr-12 overflow-x-auto " +
        "font-mono text-xs leading-relaxed whitespace-pre text-(--theme-default-ten)";

    /// <summary>The frame around a live grid.</summary>
    public const string Demo = "rounded-lg border border-(--theme-default-three) bg-white p-4 overflow-x-auto";
}
