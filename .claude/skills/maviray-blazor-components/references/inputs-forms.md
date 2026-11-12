# Inputs & Forms

All components here live in `Maviray.Blazor.Components.Material.Components.Inputs`
(except `MaviSwitch`/`MaviToggle` → `…Components`). They share the form-input
base surface documented in `SKILL.md` (`Label`, `HelperText`, `DisplayHelperText`,
`Required`, `Disabled`, `Readonly`, `ThemeColorScheme`, `ElementSize`, `Width`,
`StartIcon`, `EndIcon`, `EndIconAlternative`, `OnEndIconClick`, `EndIconDisabled`,
`ButtonLabel`, `InferFromModelAttributes`, `Immediate`, `Class`, `Style`, `Title`,
`Id`), the `SetError`/`ClearError` methods, and `@bind-Value` + `EditForm`
validation. Only the **component-specific** additions are listed below.

**When the bound value updates.** Text, number, date and textarea inputs update
`@bind-Value` when the user commits the value: when they leave the field, or
press Enter in a single-line input. This matches Blazor's built-in inputs and
sends no server call per keystroke. Validation also runs once per commit. For
live updates (search-as-you-type, previews), set `Immediate="true"`; each
keystroke is then a server call in Blazor Server, still without validating.
`MaviTextArea` updates live while `ShowCharacterCount` is on. `MaviAutoComplete`
always searches as the user types. Dropdowns, multi-selects, checkboxes and
other click-driven inputs update on selection and ignore `Immediate`.

All render a Material floating-label outlined text field (label floats on focus /
when it has a value; helper text below; asterisk when required; alert-colored
border + message on validation error). `Small`/`Regular`/`Large` map to heights
40/48/56px.

---

## Text inputs

### MaviInputString  (`TValue = string`) · MaviInputStringNullable (`string?`)

Single-line text field.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `MaxLength` | `int` | 3000 (`FormatConstants.DEFAULT_MAX_STRING_INPUT_LIMIT`) | Max characters. Inferred from `[MaxLength]`/`[StringLength]`. |
| `StringInputType` | `StringInputType` | `Text` | `Text, Password, Email, Tel, Url, Search` → HTML input type. Inferred from `[DataType]`. |
| `TogglePasswordOnClick` | `bool` | `false` | With `Password`, the end icon toggles visibility (pair with `EndIcon`/`EndIconAlternative`). |

The `Nullable` variant maps empty/whitespace input to `null`.

```razor
<MaviInputString @bind-Value="_model.Email"
                 StringInputType="StringInputType.Email"
                 Label="Email" StartIcon="lni lni-envelope-1" Width="w-96" />

<MaviInputString @bind-Value="_model.Password"
                 StringInputType="StringInputType.Password"
                 TogglePasswordOnClick="true"
                 EndIcon="lni lni-eye" EndIconAlternative="lni lni-eye-crossed" />
```

### MaviTextArea  (`TValue = string`)

Multi-line text.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Rows` | `int` | 3 | Visible rows / initial height. |
| `MaxLength` | `int` | 3000 | Max characters (inferred from annotations). |
| `Resizable` | `bool` | `true` | Allow vertical resize (off for disabled/readonly). |
| `ShowCharacterCount` | `bool` | `false` | Show a `current/max` counter below the field. |

**Method:** `string GetCurrentText()` — read the current value via `@ref` without binding. It reflects typing only with `Immediate="true"` or `ShowCharacterCount="true"`; otherwise it holds the last committed text.

---

## Numeric inputs

Family sharing `MaviNumericInputBase<TValue>`. Each is a floating-label field
with numeric input mode. All add:

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Min` / `Max` | `TValue?` | — | Bounds (inferred from `[Range]`). |
| `Step` | `TValue` | `1` (`0.01` for decimal) | Increment. |
| `FormatCulture` | `CultureInfo?` | `CultureInfo.CurrentCulture` | Parse/format culture (decimal separator, grouping, `FormatMask` output). |
| `FormatMask` | `string?` | — | .NET numeric format (e.g. `"N2"`, `"C"`, `"F4"`, `"X"`). |
| `ShowFormattedWhileFocused` | `bool` | `false` | Show formatted value even while editing (default shows raw number when focused). |

| Component | `TValue` | Notes |
|-----------|----------|-------|
| `MaviInputInteger` | `int` | `Step` default 1. |
| `MaviInputIntegerNullable` | `int?` | Nullable variant. |
| `MaviInputLong` | `long` | |
| `MaviInputShort` | `short` | |
| `MaviInputFloat` | `float` | |
| `MaviInputDouble` | `double` | |
| `MaviInputDoubleNullable` | `double?` | |
| `MaviInputDecimal` | `decimal` | `Step` default `0.01`; adds `DecimalPlaces` (`int?`) for rounding/format. Validates against `Min`/`Max`. |

**Field type.** `MaviInputInteger`, `…IntegerNullable`, `…Long` and `…Short` render
a native number field (`type="number"`) unless `FormatMask` needs text; the
browser shows and sends that value culture-invariant (`-12`, no grouping), so a
negative number displays in every culture. `MaviInputFloat`, `…Double`,
`…DoubleNullable` and `…Decimal` are text fields that parse and format with
`FormatCulture` (e.g. `1 234,5` in `sv-SE`).

```razor
<MaviInputDecimal @bind-Value="_model.Price"
                  Label="Price" FormatMask="C" DecimalPlaces="2"
                  Min="0" Width="w-64" />
```

---

## Boolean inputs

### MaviCheckbox  (`TValue = bool`)

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Indeterminate` | `bool` | `false` | Show the indeterminate dash state. |
| `CheckBoxStatusChange` | `EventCallback<ValueChangedCallbackParameters<bool>>` | — | Fires on change with `{ElementId, Value}` (in addition to `@bind-Value`). |

### MaviSwitch · MaviToggle  (`TValue = bool`, namespace `…Components`)

iOS-style switch (`MaviSwitch` has a hover halo; `MaviToggle` is the plainer
variant). Both support keyboard (Space/Enter), `Label`, `Required`, `HelperText`,
validation.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `SwitchStatusChange` / `ToggleStatusChange` | `EventCallback<ValueChangedCallbackParameters<bool>>` | Change callback with source id. |

### MaviRadioButton  (`TValue = bool`)

Standalone boolean radio with `Label`. Change callback:
`RadioStatusChange : EventCallback<ValueChangedCallbackParameters<bool>>`.

---

## Radio group (single choice from a set) — generic `TValue`

Compose `MaviRadioGroup` (cascades context) with `MaviRadioGroupItem` children.
`MaviRadioGroupItem` inherits size/scheme/disabled/value from the group, or can
run standalone.

**`MaviRadioGroup<TValue>`** (`MaviComponentBase`, not a form input):

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Value` / `@bind-Value` | `TValue?` | — | Selected value. |
| `ValueChanged` | `EventCallback<ValueChangedCallbackParameters<TValue?>>` | — | Selection change with source id. |
| `Disabled`, `Readonly` | `bool` | | Group state (cascaded). |
| `ElementSize` | `ElementSize` | `Regular` | Cascaded. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Cascaded. |
| `Width`, `HelperText`, `DisplayHelperText` | | | Layout/helper. |
| `ChildContent` | `RenderFragment?` | | The `MaviRadioGroupItem`s. |

**`MaviRadioGroupItem<TValue>`**: `Label`, `OptionValue` (the item's value),
plus standalone `Value`/`ValueChanged`/`Disabled`/`ElementSize`/`ThemeColorScheme`
when used outside a group.

```razor
<MaviRadioGroup @bind-Value="_model.Plan" ThemeColorScheme="ThemeColorScheme.Primary">
    <MaviRadioGroupItem OptionValue="@("free")" Label="Free" />
    <MaviRadioGroupItem OptionValue="@("pro")"  Label="Pro" />
</MaviRadioGroup>
```

---

## Selection inputs

### MaviDropdown  (`@typeparam TItem, TValue`, `TValue = @bind-Value` type)

Custom single-select dropdown (closes on outside click via JS interop). Enum
`TValue` auto-populates `Items` from the enum's values (and honors `[Display]`
names).

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Items` | `IEnumerable<TItem>?` | — | Options (auto-filled for enums). |
| `ValueSelector` | `Func<TItem, TValue>?` | — | Extract value from an item. |
| `TextSelector` | `Func<TItem, string>?` | — | Extract display text (else `ToString()` / enum `[Display]`). |
| `PlaceholderText` | `string` | `"Select an option..."` | Empty-state text. |
| `ShowPlaceholderOption` | `bool` | `true` | Render a placeholder row (unless `Required`). |
| `DropdownIcon` | `string` | `"lni lni-chevron-down"` | Arrow icon. |
| `MaxDropdownHeight` | `string` | `"max-h-60"` | Options list max height; longer lists scroll with a thin gray scrollbar. |
| `OnSelectionChanged` | `EventCallback<ValueChangedCallbackParameters<TValue>>` | — | Selection change with source id. |

### MaviMultiSelect  (`@typeparam TItem, TValue`; `@bind-Value` is `List<TValue>`)

Multi-select with chips + Select-All/Clear-All. Same `Items`/`ValueSelector`/
`TextSelector`/`DropdownIcon`/`MaxDropdownHeight` as dropdown, plus:

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `ShowSelectAllOption` | `bool` | `true` | Show Select-All/Clear-All actions. |
| `MaxVisibleChips` | `int` | 3 | Chips shown before overflow (0 = unlimited). |
| `OnSelectionChanged` | `EventCallback<ValueChangedCallbackParameters<List<TValue>>>` | — | Selection change. |

```razor
<MaviDropdown TItem="Country" TValue="int"
              @bind-Value="_model.CountryId"
              Items="_countries"
              ValueSelector="c => c.Id" TextSelector="c => c.Name"
              Label="Country" />

<MaviMultiSelect TItem="Role" TValue="Role"
                 @bind-Value="_model.Roles" Items="_allRoles"
                 Label="Roles" />
```

### MaviAutoComplete  (`@typeparam TItem`; `@bind-Value` is `TItem`)

Async type-ahead with debounce, keyboard navigation, loading state, and a
templated dropdown.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `OnSearch` *(required)* | `Func<string, Task<IEnumerable<TItem>>>` | — | Fetch matches for the typed text. |
| `ItemTextSelector` | `Func<TItem, string>?` | — | Display text for an item. |
| `ItemTemplate` | `RenderFragment<TItem>?` | — | Custom option rendering. |
| `OnValueSelected` | `EventCallback<TItem>` | — | Fires when an option is chosen. |
| `OnReSet` | `EventCallback` | — | Fires when the input is cleared. |
| `MinSearchLength` | `int` | 1 | Chars before searching. |
| `NoResultsText` | `string` | `"No results found"` | Empty-results message. |
| `DebounceDelay` | `int` | 300 | Debounce (ms). |
| `MaxLength` | `int` | 3000 | Input max length. |
| `ClearOnSelect` | `bool` | `false` | Clear the box after selecting. |
| `MaxDisplayItems` | `int` | 0 | Cap options shown (0 = all). |

**Methods (via `@ref`):**
- `string GetCurrentText()` — the raw typed text (for "create new" flows).
- `TItem? GetSelectedItem()` — the item actually selected (null if free text).
- `void SetSelection(TItem? item, string? displayText = null)` — imperatively set
  selection/display (edit scenarios loaded after render).
- `void Reset()` — clear selection, text, and bound value (cascading resets).

Default end icon is search; becomes a clear (✕) icon when there is text.

---

## Date & time

### MaviInputDate (`DateTime`) · MaviInputDateNullable (`DateTime?`)

Floating-label date field that by default opens a custom calendar picker.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `DateInputType` | `DateInputType` | `Date` | `Date, DateTime, Time, Month, Week`. Inferred from `[DataType]`. |
| `Min` / `Max` | `DateTime?` | — | Selectable range (inferred from `[Range]`). |
| `DisplayFormat` | `string?` | culture default | e.g. `"dd/MM/yyyy"`, `"yyyy-MM-dd"`. |
| `FormatCulture` | `CultureInfo?` | `CultureInfo.CurrentCulture` | Parse/format culture of the typed and displayed date. |
| `UseCalendarPicker` | `bool` | `true` | Use the custom calendar; `false` = plain text input with parsing. |

Parses many common date formats; shows a calendar icon that toggles the picker.

### MaviDatePicker  (standalone calendar; `MaviComponentBase`)

The calendar popup used by the date inputs; usable on its own.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `Value` / `@bind-Value` | `DateTime?` | Selected date. |
| `Min` / `Max` | `DateTime?` | Selectable range (year list spans ±100y otherwise). |
| `Culture` | `CultureInfo?` | Month/day names, first day of week. Defaults to `CultureInfo.CurrentUICulture` (the UI language). |
| `IsOpen` / `@bind-IsOpen` | `bool` | Open state. |

Renders month/year selectors, Today/Clear/OK actions.

---

## MaviSlider  (`MaviComponentBase`)

Range slider (track + progress + thumb), themed by color.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Value` / `@bind-Value` | `double` | 0 | Current value. |
| `Min` / `Max` | `double` | 0 / 100 | Range. |
| `Step` | `double` | 1 | Increment. |
| `Size` | `ElementSize` | `Regular` | Track/thumb size. |
| `Color` | `ThemeColorScheme` | `Primary` | Track/thumb color. |
| `Disabled` | `bool` | `false` | |
| `ValueChanged` | `EventCallback<double>` | — | Standard bind callback. |
| `SliderValueChanged` | `EventCallback<ValueChangedCallbackParameters<double>>` | — | Change with source id. |

> Note: `MaviSlider` uses `Size`/`Color` (not `ElementSize`/`ThemeColorScheme`)
> and is **not** an `EditForm` input — it binds via `@bind-Value` only.

---

## MaviValidationMessage  (`MaviComponentBase`)

A standalone, imperatively-driven alert-colored message line (independent of the
`EditContext`), for ad-hoc field errors.

**Methods (via `@ref`):** `void Show(string message)`, `void Hide()`.
Renders nothing until `Show` is called.
