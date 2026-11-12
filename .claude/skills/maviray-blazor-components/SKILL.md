---
name: maviray-blazor-components
description: >-
  Use when building or changing UI in a .NET/Blazor app that consumes the
  Maviray.Blazor.Components.Material library (the Mavi* components) — whenever
  you need a form field, button, dialog, data table, tabs, menu, accordion,
  card, panel, alert, toast, spinner, badge, chip, icon, dropdown, multi-select,
  autocomplete, date picker, checkbox/switch/toggle/radio, slider, rich-text /
  WYSIWYG / HTML editor, or any interactive UI element. Consult this BEFORE
  writing custom HTML or hand-rolled
  Blazor markup for a UI element: this library is the standard solution and
  almost always already ships the component, fully themed and accessible. Covers
  every component's parameters, exposed methods, data models, events, and usage
  patterns. Pair with `material-css-styling` for theming/Tailwind decisions.
---

# Maviray Blazor Material Components

## Overview

`Maviray.Blazor.Components.Material` is a Blazor component library (Mavi*
components) providing themed, accessible, Material-Design UI building blocks. It
targets .NET 10 (`net10.0`) and ships as a NuGet package with its own CSS + JS
assets; it brings `Maviray.Blazor.Components.Core` (base classes, enums, models)
and HtmlSanitizer with it.

In a consuming project the library's source is **not on disk**: rely on this
skill and IntelliSense. The installed version is in the app's `.csproj`
(`<PackageReference Include="Maviray.Blazor.Components.Material" Version="…" />`).

**Core principle for consuming projects:** reach for a `Mavi*` component first.
Almost every common UI need — inputs, buttons, dialogs, tables, menus, tabs,
cards, alerts, toasts, spinners — already exists here, wired for theming,
validation, and accessibility. Do **not** hand-roll `<input>`, `<button>`,
`<table>`, `<dialog>`, dropdown, or modal markup when a component covers it.
Configure the component through its strongly-typed parameters instead.

This skill documents the components' **API surface** (parameters, methods,
events, data models). For color/typography/spacing/Tailwind decisions, use the
companion **`material-css-styling`** skill.

## When to use

- Adding or editing any screen, form, list, dialog, or interactive element
- Choosing which `Mavi*` component fits a need ("is there a component for X?")
- Configuring a component's parameters, wiring its events, or calling its methods
- Feeding data into the table, menu, autocomplete, dropdown, or toast components
- Reviewing whether existing custom markup should be replaced by a component

## "I need X" → component quick lookup

| Need | Component(s) | Reference |
|------|--------------|-----------|
| Text field | `MaviInputString` / `MaviInputStringNullable` | inputs-forms |
| Number field | `MaviInputInteger`, `…Long`, `…Short`, `…Float`, `…Double`, `…Decimal` (+ `Nullable` variants) | inputs-forms |
| Multi-line text | `MaviTextArea` | inputs-forms |
| Checkbox / on-off toggle | `MaviCheckbox`, `MaviSwitch`, `MaviToggle` | inputs-forms |
| Single choice from a set | `MaviRadioGroup` + `MaviRadioButton`, or `MaviDropdown` | inputs-forms |
| Multiple choice | `MaviMultiSelect` | inputs-forms |
| Type-ahead / async search | `MaviAutoComplete` | inputs-forms |
| Date / time picker | `MaviInputDate` / `MaviInputDateNullable` (calendar: `MaviDatePicker`) | inputs-forms |
| Range slider | `MaviSlider` | inputs-forms |
| Manual field error message | `MaviValidationMessage` | inputs-forms |
| Button / icon button / segmented group | `MaviButton`, `MaviButtonIcon`, `MaviButtonGroup` | buttons-actions |
| Navigation link (styled `NavLink`) | `MaviLink` | buttons-actions |
| Status badge / chip / tag | `MaviBadge`, `MaviChip` | data-display |
| SVG icon | `MaviIconMini`, `MaviIconSolid`, `MaviIconOnline` | data-display |
| Inline alert banner | `MaviAlert` | feedback-overlays |
| Toast notifications | `MaviToastr` (+ `MaviToastNotification`) | feedback-overlays |
| Modal dialog / confirm | `MaviDialog` | feedback-overlays |
| Loading overlay | `MaviBackdrop` + a spinner | feedback-overlays |
| Loading placeholder | `MaviSkeleton` | feedback-overlays |
| Loading spinner | `MaviSpinner*` (6 styles) | feedback-overlays |
| Card / titled panel | `MaviCard`, `MaviPanel` | layout-navigation |
| Expandable sections | `MaviAccordion` + `MaviAccordionItem` | layout-navigation |
| Tabs | `MaviTabNavBar` + `MaviTab` + `MaviTabContent` | layout-navigation |
| Sidebar / vertical menu | `MaviMenuVertical` | layout-navigation |
| Dropdown action menu | `MaviDropDownMenu` | layout-navigation |
| Top app bar | `TopNavBar` | layout-navigation |
| Root error boundary | `MaviErrorHandler` | layout-navigation |
| Data grid (sort/filter/page/context menus) | `MaviInMemoryTable`, `MaviCssGridTable` | tables |
| Rich text / WYSIWYG / HTML editor | `WysiwygEditor` | editors |

## Setup (consuming project)

1. **Register services** — in `Program.cs` (in a Blazor Web App with
   WebAssembly interactivity, in both the server and the client project):
   ```csharp
   using Maviray.Blazor.Components.Core.Extensions;
   using Maviray.Blazor.Components.Core.Services;

   builder.Services.AddMaviComponents();
   // or: builder.Services.AddMaviComponents(o => o.EnableLifecycleLogging = true);

   // Only if you use MaviMenuVertical (it injects this; AddMaviComponents doesn't register it):
   builder.Services.AddScoped<ICircuitStateService, CircuitStateService>();
   ```
   `AddMaviComponents` is **required**: every component injects
   `IMaviComponentOptions`, and without it rendering throws
   `InvalidOperationException: Cannot provide a value for property 'ComponentOptions'`.
   `MaviMenuVertical` throws the same way without an `ICircuitStateService`.
   To build app-wide menu items, subclass `CircuitStateService` and override
   `GetMenuItems()`; `ReportClick(buttonId)` raises `ButtonClicked` (see the
   menu section of `references/layout-navigation.md`).

   **Logging:** with `EnableLifecycleLogging = true`, components write lifecycle
   traces at **Debug** level, under categories that start with `Maviray`. Show
   them with `"Logging": { "LogLevel": { "Maviray": "Debug" } }`. Warnings and
   errors (failed data fetches, invalid format masks, …) are logged regardless.
   The `ComponentLogLevel` and `EnablePerformanceTracking` options currently have
   no effect on components.

2. **Host page `<head>`** (`App.razor` / `index.html`): the library stylesheet,
   the scoped-CSS bundle (styles of `WysiwygEditor`, `MaviToastr`, `MaviSlider`,
   `MaviSkeleton`, `MaviSpinnerEqualizer`), the outside-click script, plus the
   fonts and icon font the theme expects:
   ```html
   <link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
   <link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/Maviray.Blazor.Components.Material.bundle.scp.css" />
   <script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>
   ```
   - The `.bundle.scp.css` link can be skipped when the page already links the
     app's own `<AppName>.styles.css`, which imports it.
   - `maviJsInterop.js` is a plain script (not a module): `MaviDropDownMenu`,
     `WysiwygEditor` and the tables need it to close on outside clicks. Other
     components lazy-load their own ES modules from
     `_content/Maviray.Blazor.Components.Material/js/` (`MaviDropdown`/
     `MaviMultiSelect`, `MaviInputDate*`, `WysiwygEditor`); nothing to add.
   - **Fonts are not bundled.** The theme uses Lato (UI text), Montserrat
     (headings), Oswald (display) and Roboto Serif; load them (e.g. Google Fonts)
     or text falls back to system fonts.
   - **Icons:** `StartIcon`/`EndIcon`/`Icon` parameters take icon-font classes,
     and component defaults use LineIcons 5 (`lni lni-chevron-down`,
     `lni lni-calendar-days`, `lni lni-xmark`, …). Load the LineIcons stylesheet.

   `maviray.material.min.css` is the compiled Tailwind 4 build of the theme +
   component styles; see `material-css-styling` for the token system and for
   building your own app's Tailwind CSS.

3. **Render mode** — the components need an interactive render mode (they use
   events and JS interop); under static SSR they render but don't respond. Set
   it app-wide (e.g. `<Routes @rendermode="InteractiveWebAssembly" />` and the
   same on `<HeadOutlet>`) or per page.

4. **Trimming** — the libraries are marked trimmable, so a Blazor WebAssembly
   Release publish trims them. They're trim-analyzer clean; nothing to
   configure. After upgrading, click through a Release publish once.

5. **`@using` namespaces** — components live under
   `Maviray.Blazor.Components.Material.Components.*` (see below). Add the
   namespaces you use to the consuming project's `_Imports.razor`, e.g.:
   ```razor
   @using Maviray.Blazor.Components.Material.Components.Inputs
   @using Maviray.Blazor.Components.Material.Components.Feedback
   @using Maviray.Blazor.Components.Core.Enums
   @using Maviray.Blazor.Components.Core.Models
   ```

## Namespaces

| Area | Namespace |
|------|-----------|
| Text/number/select/date inputs, buttons, link, slider | `Maviray.Blazor.Components.Material.Components.Inputs` |
| `MaviSwitch`, `MaviToggle` | `Maviray.Blazor.Components.Material.Components` |
| `MaviBadge`, icons | `…Components.DataDisplay`, `…Components.DataDisplay.Icons` |
| `MaviChip` | `…Components.Display` |
| `MaviAlert`, `MaviBackdrop`, `MaviDialog`, `MaviSkeleton` | `…Components.Feedback` |
| `MaviToastr` | `…Components.DataDisplay.MaviToastr` |
| `MaviToastNotification` | `…Components.DataDisplay` |
| `MaviErrorHandler` | `…Components.ErrorHandlers` |
| `MaviAccordion`, `MaviCard`, `MaviPanel` | `…Components.Surfaces` |
| Tabs | `…Components.Tabs` |
| Menus | `…Components.Menus` |
| `TopNavBar` | `…Components.NavBars` |
| Spinners | `…Components.Spinner` |
| Tables | `…Components.Tables` |
| `WysiwygEditor` | `…Components.Editors.Wysiwyg` |
| Enums, models, event args, attributes | `Maviray.Blazor.Components.Core.*` |
| `AddMaviComponents`, `ICircuitStateService` / `CircuitStateService` | `Maviray.Blazor.Components.Core.Extensions`, `…Core.Services` |

## Shared conventions (read once — they apply to most components)

### Base parameters every component has

All components derive from **`MaviComponentBase`**, which provides:

| Parameter | Type | Notes |
|-----------|------|-------|
| `Id` | `string?` | Auto-assigned GUID if unset; rendered as the element `id`. |
| `Class` | `string?` | Extra CSS classes appended to the component's computed classes — the extension seam. |
| `Style` | `string?` | Inline style. |
| `Title` | `string?` | Tooltip / accessible title (some components also render it as visible text — see `MaviButton`). |

Components deriving from **`MaviAttributesComponent`** (buttons, button group)
additionally capture unmatched attributes into `AdditionalAttributes`
(`@attributes` splat) so you can pass arbitrary HTML attributes.

### Form-input base surface

Form inputs derive from `MaviInputBase<TValue>` (which extends Blazor's
`InputBase<TValue>`, so `@bind-Value` and `EditForm`/`EditContext` validation
work) → `MaviMaterialInputBase<TValue>`. Common parameters shared by every text,
numeric, select, and date input:

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Value` / `@bind-Value` | `TValue` | — | Two-way bound value (standard Blazor input binding). |
| `Label` | `string` | `""` | Floating label. Auto-inferred from `[Display]`/`[DisplayName]` or the property name when unset. |
| `HelperText` | `string?` | — | Hint text below the field. |
| `DisplayHelperText` | `bool` | `true` | Suppress helper text without clearing it. |
| `Required` | `bool` | `false` | Shows the asterisk; auto-inferred from `[Required]`. |
| `Disabled` | `bool` | `false` | Disables and clears validation for the field. |
| `Readonly` | `bool` | `false` | Read-only, non-editable. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Primary` | Accent color (label/border/icon). |
| `ElementSize` | `ElementSize` | `Regular` | `Small` (h-10), `Regular` (h-12), `Large` (h-14). |
| `Width` | `string` | `"w-auto"` | Width utility class (e.g. `"w-96"`, `"w-full"`). |
| `StartIcon` | `string?` | — | Leading icon CSS class (e.g. a LineIcons `"lni lni-user"`). |
| `EndIcon` | `string?` | — | Trailing clickable icon. |
| `EndIconAlternative` | `string?` | — | Icon swapped in on end-icon click (toggle icons). |
| `OnEndIconClick` | `EventCallback<MouseClickEventArgs>` | — | Fires on end-icon click; shows a spinner while awaiting. |
| `EndIconDisabled` | `bool` | `false` | Disable the end-icon button only. |
| `ButtonLabel` | `string` | `"Action"` | Aria-label for the end-icon button. |
| `InferFromModelAttributes` | `bool` | `true` | Infer Label/Required/type-specific attrs from the bound property's data annotations. Set `false` to disable. |
| `Immediate` | `bool` | `false` | Text/number/date/textarea inputs: update `@bind-Value` on every keystroke instead of when the user commits (leaves the field / presses Enter). Each keystroke is a server call in Blazor Server. |
| `Class`, `Style`, `Title`, `Id` | — | As base. |

**Attribute inference** (when `InferFromModelAttributes` is true) reads the bound
property's annotations: `[Display]`/`[DisplayName]` → `Label`, `[Required]` →
`Required`, plus per-type inference (`[MaxLength]`/`[StringLength]` →
`MaxLength`; `[DataType]` → `StringInputType`/`DateInputType`; `[Range]` →
`Min`/`Max`).

**Programmatic validation** (on any Material input, via `@ref`):
- `void SetError(string message)` — mark the field invalid with the standard
  red/alert visual state and register the message on the `EditContext` (surfaces
  in a `ValidationSummary`). Auto-clears on the next value change.
- `void ClearError()` — clear a programmatic error explicitly.

Use these inside an `OnValidSubmit` handler when a check can't be expressed as a
data annotation.

### Shared enums (`Maviray.Blazor.Components.Core.Enums`)

| Enum | Values |
|------|--------|
| `ThemeColorScheme` | `Default, Primary, Secondary, Success, Alert, Warning, Info, Dark, Light` |
| `ElementVariant` | `Filled, Outlined, Text` |
| `ElementSize` | `Regular, Large, Small` |
| `TextTransform` | `Normal, UpperCase, LowerCase, Capitalize` |
| `ButtonRole` | `Button, Submit, Reset` (maps to `type=`) |
| `IconSize` | `ExtraSmall, Small, Medium, Standard, Large, ExtraLarge, Giant` |
| `ElementColor` | 140 named CSS colors (`Red`, `DodgerBlue`, `Transparent`, …) — used by icons |
| `StringInputType` | `Text, Password, Email, Tel, Url, Search` |
| `DateInputType` | `Date, DateTime, Time, Month, Week` |
| `SkeletonVariant` | `Text, Rectangular, Rounded, Circular` |
| `SkeletonAnimation` | `None, Pulse, Wave` |
| `SortOrder` | `Ascending, Descending` |
| `BackdropOpacity` | `None, Lighten, Darken` |
| `DialogButtonClick` | `Confirm, Cancel, Close` |
| `ZIndex` | `Zero, Five, Ten, Twenty, Thirty, Forty, Fifty, Sixty, Seventy, Eighty, Ninety, OneHundred, TwoHundred … Thousand` |
| `ScreenSizeBreakpoint` | `Small … SevenExtraLarge` (Tailwind `sm`…`7xl`) |
| `ElementRelativePosition` | `CenterTop, CenterBottom, TopLeft, TopRight, BottomLeft, BottomRight` (toast position) |
| `ComponentRelativePosition` | above + `Center` (dialog position) |
| `ElementHorizontalAlignment` | `Left, Center, Right` |
| `HorizontalPosition` | `Center, Left, Right` (table cell alignment) |
| `TableColumnDataType` | `String, Boolean, Integer, Decimal, Double, Enum, DateTime, Other` |
| `TableRowContextMenuDisplayStyle` | `DropDown, Icons` |

> The component `ThemeColorScheme` enum covers a **subset** of the CSS theme
> roles — it has no `Tertiary`/`Accent`/`Highlight`. See `material-css-styling`.

### Common event payloads (`Maviray.Blazor.Components.Core`)

- `ValueChangedCallbackParameters<T>` — `{ string? ElementId; T? Value; }`.
  Many components expose a secondary `…StatusChange`/`…ValueChanged`/
  `OnSelectionChanged` callback of this type carrying the source `Id`.
- `MouseClickEventArgs` — extends `MouseEventArgs` with `ButtonId`. Emitted by
  `OnClick` on buttons, links, badges, etc.
- `MouseGroupClickEventArgs` — adds the clicked child button id (button group).
- `TabClickEventArgs`, `DialogButtonClickEventArgs`, `ButtonClickEventArgs` —
  used by tabs, dialog, and the circuit-state service respectively.

## Reference files (read the one for the component you're using)

Each file lists the components in that area with their **specific** parameters,
public methods, events, data models, and a usage snippet. The shared surface
above is not repeated there.

- **`references/inputs-forms.md`** — text, numeric, textarea, checkbox, switch,
  toggle, radio, dropdown, multi-select, autocomplete, date, slider, validation
  message.
- **`references/buttons-actions.md`** — `MaviButton`, `MaviButtonIcon`,
  `MaviButtonGroup`, `MaviLink`.
- **`references/data-display.md`** — `MaviBadge`, `MaviChip`, icon components.
- **`references/feedback-overlays.md`** — `MaviAlert`, `MaviToastr`,
  `MaviDialog`, `MaviBackdrop`, `MaviSkeleton`, spinners.
- **`references/layout-navigation.md`** — surfaces (accordion, card, panel),
  tabs, menus, top nav bar, error handler.
- **`references/tables.md`** — `MaviInMemoryTable`, `MaviCssGridTable`, and the
  table data model (`ITableDataItem`, `[TableColumn]`, `TableDataCollection`,
  `TableParameters`, context menus, events).
- **`references/editors.md`** — `WysiwygEditor` (rich-text / HTML editor):
  toolbar, parameters, `@bind-Content`, public methods, and gotchas.

## Component usage pattern (the shape of almost every component)

```razor
@* Configure with strongly-typed enum parameters; extend look via Class *@
<MaviButton Title="Save"
            ThemeColorScheme="ThemeColorScheme.Primary"
            ElementVariant="ElementVariant.Filled"
            ElementSize="ElementSize.Regular"
            OnClick="HandleSaveAsync"
            Class="w-full mt-4" />

@* Inputs bind with @bind-Value and live in an EditForm for validation *@
<EditForm Model="_model" OnValidSubmit="SubmitAsync">
    <DataAnnotationsValidator />
    <MaviInputString @bind-Value="_model.Name" Width="w-96" />
    <MaviInputInteger @bind-Value="_model.Age" Min="0" Max="120" />
    <MaviButton ButtonRole="ButtonRole.Submit" Title="Submit"
                ThemeColorScheme="ThemeColorScheme.Primary" />
</EditForm>
```

**Rules of thumb**
- Configure via enum parameters; never rebuild a component's internals.
- Extend appearance via `Class` (appended to computed classes), not by forking.
- Bind data inputs with `@bind-Value`; place them in an `EditForm` for
  validation and let attribute inference fill Label/Required.
- Hold an `@ref` to call imperative methods (`Display`/`Hide` on dialogs,
  backdrops, alerts; `SetError`/`ClearError` on inputs; `Refresh` on tables;
  `Reset`/`SetSelection` on autocomplete). Toasts are driven by the list you
  pass to `MaviToastr`, not by methods.
- Exhaustive lookups: rely on the reference files and IntelliSense over the
  installed package. If a local checkout of the `blazor-ui-kit` repo is
  available as an added directory, components live in
  `src/Maviray.Blazor.Components.Material/Components/**`, enums/models in
  `src/Maviray.Blazor.Components.Core/**`, living examples in
  `samples/Maviray.Blazor.Components.Samples.Material.Client/Pages/**`; the
  checkout may be a different version than the installed package.
