# Buttons & Actions

Namespace `Maviray.Blazor.Components.Material.Components.Inputs`. Buttons derive
from `MaviButtonBase` → `MaviAttributesComponent`, so they carry `Id`, `Class`,
`Style`, `Title`, and splat unmatched HTML attributes via `AdditionalAttributes`.

---

## MaviButton

Standard button. Renders `Title` as its visible label, supports start/end icons,
and shows a spinner while an async `OnClick` runs.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Title` | `string?` | — | Button text (rendered inside a `<span>`). |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Color. |
| `ElementVariant` | `ElementVariant` | `Filled` | `Filled, Outlined, Text`. |
| `ElementSize` | `ElementSize` | `Regular` | `Small, Regular, Large`. |
| `TextTransform` | `TextTransform` | `UpperCase`* | Casing (`Normal, UpperCase, LowerCase, Capitalize`). |
| `ButtonRole` | `ButtonRole` | `Button` | `Button, Submit, Reset` → HTML `type`/`role`. |
| `Disabled` | `bool` | `false` | |
| `StartIcon` / `EndIcon` | `string?` | — | Icon CSS classes flanking the label. |
| `ShowSpinner` | `bool` | `false` | Show a spinner while the async `OnClick` runs. |
| `SpinnerText` | `string?` | — | Text shown beside the spinner. |
| `Width` | `string` | `"min-w-16"` | Width utility class. |
| `OnClick` | `EventCallback<MouseClickEventArgs>` | — | Click handler; the button self-disables (loading) while awaiting to prevent double-submit. |

\* Defaults to uppercase per Material; the enum default is `Normal` but the
render maps unset → uppercase.

```razor
<MaviButton Title="Save"
            ThemeColorScheme="ThemeColorScheme.Primary"
            ElementVariant="ElementVariant.Filled"
            ShowSpinner="true" SpinnerText="Saving…"
            OnClick="SaveAsync" />

<MaviButton Title="Submit" ButtonRole="ButtonRole.Submit"
            ThemeColorScheme="ThemeColorScheme.Primary" />
```

Use `ButtonRole.Submit` inside an `EditForm` to trigger `OnValidSubmit`.

---

## MaviButtonIcon

Circular icon-only button.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Icon` | `string?` | — | Icon CSS class (e.g. `"lni lni-xmark"`). |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Color. |
| `ElementVariant` | `ElementVariant` | `Filled` | `Filled, Outlined, Text`. |
| `ElementSize` | `ElementSize` | `Regular` | Small (w-8), Regular (w-10), Large (w-14). |
| `Disabled` | `bool` | `false` | |
| `ButtonRole` | `ButtonRole` | `Button` | |
| `OnClick` | `EventCallback<MouseClickEventArgs>` | — | Click handler. |

```razor
<MaviButtonIcon Icon="lni lni-trash-3"
                ElementVariant="ElementVariant.Text"
                ThemeColorScheme="ThemeColorScheme.Alert"
                OnClick="DeleteAsync" Title="Delete" />
```

---

## MaviButtonGroup

A segmented row of buttons, either data-driven (`Buttons`) or `ChildContent`.
Inherits `MaviAttributesComponent`.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Buttons` | `List<ButtonModel>?` | — | Data-driven buttons (id, title, start/end icon, disabled). |
| `ChildContent` | `RenderFragment?` | — | Alternative to `Buttons`. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Shared color. |
| `ElementVariant` | `ElementVariant` | `Filled` | `Filled, Outlined, Text`; controls joined-corner styling. |
| `ElementSize` | `ElementSize` | `Regular` | |
| `TextTransform` | `TextTransform` | `Normal` | |
| `Disabled` | `bool` | `false` | Disables the whole group. |
| `OnClick` | `EventCallback<MouseGroupClickEventArgs>` | — | Fires with the group id + clicked button id. |

`ButtonModel` (`…Core.Models.Buttons`): `Id`, `Title`, `StartIcon`, `EndIcon`,
`Disabled`.

```razor
<MaviButtonGroup ElementVariant="ElementVariant.Outlined"
                 Buttons="_viewModes"
                 OnClick="OnViewModeClick" />
```

---

## MaviLink

A themed navigation link — inherits Blazor's `NavLink` (so active-route matching
works) and styles it like a button/link.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Href` | `string` | `""` | Target URL. |
| `Label` | `string?` | — | Link text. |
| `LeftIcon` / `RightIcon` | `RenderFragment?` | — | Icon slots. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Color. |
| `ElementVariant` | `ElementVariant` | `Text` (default branch) | `Filled` = button-like; `Outlined` = underlined link; `Text` = plain colored link. |
| `ElementSize` | `ElementSize` | `Regular` | |
| `Badge` | `string?` | — | `data-badge` value (badge decoration). |
| `Width` | `string` | `"min-w-16"` | |
| `Disabled` | `bool` | `false` | Prevents navigation. |
| `OnClick` | `EventCallback<MouseClickEventArgs>` | — | Click handler. |
| `Title`, `Class`, `Style`, `Id` | | | As base. |

```razor
<MaviLink Href="/reports" Label="Reports"
          ElementVariant="ElementVariant.Text"
          ThemeColorScheme="ThemeColorScheme.Primary">
    <LeftIcon><i class="lni lni-bar-chart-4"></i></LeftIcon>
</MaviLink>
```
