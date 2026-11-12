# Data Display: Badge, Chip, Icons

---

## MaviBadge  (`…Components.DataDisplay`, `MaviComponentBase`)

A small icon with an optional superscript text marker (e.g. a notification dot /
count over an icon).

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Icon` | `string?` | — | Main icon CSS class. |
| `Text` | `string?` | — | Small marker text rendered top-right. |
| `ElementSize` | `ElementSize` | `Regular` | Icon/marker size. |
| `OnClick` | `EventCallback<MouseClickEventArgs>` | — | Click handler (whole badge). |
| `Class`, `Style`, `Title`, `Id` | | | As base. |

```razor
<MaviBadge Icon="lni lni-bell-1" Text="3" OnClick="OpenNotifications" />
```

---

## MaviChip  (`…Components.Display`, `MaviComponentBase`)

A compact rounded tag/label with optional start/end icons.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Text` | `string` | `""` | Chip label. |
| `StartIcon` / `EndIcon` | `string?` | — | Icon CSS classes. |
| `ElementSize` | `ElementSize` | `Regular` | Small/Regular/Large. |
| `ElementVariant` | `ElementVariant` | `Filled` | `Filled` or `Outlined` (Text renders like unset). |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Color. |
| `CssClass` | `string?` | — | **Extra classes** — note this component uses `CssClass`, not `Class`. |

```razor
<MaviChip Text="Active" ThemeColorScheme="ThemeColorScheme.Success"
          StartIcon="lni lni-check-circle-1" />
```

> Gotcha: `MaviChip` exposes `CssClass` for extra utilities (the base `Class`
> parameter exists but isn't applied to the chip markup).

---

## Icons: MaviIconMini · MaviIconSolid · MaviIconOnline  (`…Components.DataDisplay.Icons`)

Three inline-SVG icon components, differing only in the icon set / stroke style
they render. Each takes an icon-type enum, a color, and a size, and exposes a
click callback. Paths come from bundled `*Paths` classes — no external icon font
needed for these.

| Component | Type enum (parameter) | Enum members | Render style |
|-----------|----------------------|--------------|--------------|
| `MaviIconMini` | `IconMiniType IconMiniType` | ~285 | filled, 20×20 viewBox |
| `MaviIconSolid` | `IconSolidType IconSolidType` | ~285 | filled, 24×24 viewBox |
| `MaviIconOnline` | `IconOnlineType IconOnlineType` | ~285 | outline/stroke, 24×24 |

Common parameters (all three):

| Parameter | Type | Purpose |
|-----------|------|---------|
| `IconColor` | `ElementColor` | Named CSS color (`ElementColor.DodgerBlue`, `…Red`, `…Transparent`, …). Applied as SVG `fill` (or `stroke` for `MaviIconOnline`). |
| `IconSize` | `IconSize` | `ExtraSmall`(w-4)…`Standard`(w-10, default)…`Giant`(w-16). |
| `OnClick` | `EventCallback<string>` | Fires with the icon's `Id`. |
| `Class`, `Style`, `Title`, `Id` | | As base (icons include `cursor-pointer`). |

```razor
<MaviIconMini IconMiniType="IconMiniType.ChevronDownMini"
              IconColor="ElementColor.DimGrey"
              IconSize="IconSize.Small" />

<MaviIconOnline IconOnlineType="IconOnlineType.Bell"
                IconColor="ElementColor.SlateBlue"
                OnClick="@(id => ...)" />
```

> To browse the full icon lists, use IntelliSense on the `IconMiniType`,
> `IconSolidType` and `IconOnlineType` enums (`Maviray.Blazor.Components.Core.Enums`).
> Every enum value renders its icon. For icon **fonts** (LineIcons `lni …`,
> `material-symbols:…`) used by other components' `StartIcon`/`EndIcon` string
> parameters, pass the icon's CSS class string directly, and load that icon
> font's stylesheet in the host page.
