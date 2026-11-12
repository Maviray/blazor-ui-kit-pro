---
name: material-css-styling
description: >-
  Use when styling, laying out, or building UI in a .NET/Blazor app that
  consumes the Maviray.Blazor.Components.Material library (Mavi* components) —
  writing Tailwind classes, choosing colors/typography/spacing/elevation,
  picking or extending Mavi* components, or implementing a design. Keeps work on
  Tailwind 4 theme tokens and Material Design patterns instead of raw hex or
  stock palette colors. Self-contained; the library ships as a NuGet package.
---

# Material CSS Styling (Maviray Blazor — consumer)

## Overview

This app's UI is built on **Maviray.Blazor.Components.Material** (referenced as a
NuGet package). It styles with **Tailwind 4** driven by a **Material Design**
token system: colors, type, and elevation are CSS variables the library defines;
`Mavi*` components consume them through typed constants.

**Core principle:** Never invent a value the theme already defines. Reach for a
component first, then Tailwind utilities bound to theme tokens — never hardcoded
hex or stock Tailwind palette colors (`blue-500`, `gray-200`, …).

Because the library is a NuGet package, its C# source (`Tailwind.cs` constants,
component internals) is **not readable on disk** — rely on the tables below and
IDE IntelliSense. If a local checkout of `blazor-ui-kit` is available as an added
directory (see Pointers), read it for exhaustive detail.

## When to Use

- Building or laying out any screen, `.razor` component, or fragment
- Choosing a color, font size, spacing, border, shadow, or state style
- Deciding whether to reuse a `Mavi*` component or write custom markup
- Extending an existing component's appearance

## The Styling Decision Order

Work top-down. Only drop a level when the current one can't express it.

1. **Use an existing `Mavi*` component** if one fits (`MaviButton`, `MaviCard`,
   `MaviChip`, `MaviAlert`, `MaviBadge`, inputs, tabs, menus, …). Configure it
   with its enum parameters; don't rebuild it.
2. **No component fits → compose Tailwind 4 utilities** in the markup.
3. **For any themeable property (color, typography, elevation, z-index) → use a
   theme token** — a `Tailwind.Theme.*` constant in C#, or the `(--theme-*)` var
   syntax in a class string. Never a raw hex or stock palette color.
4. **Shape it with Material Design patterns** — type scale, elevation shadows,
   variant × color × size structure, hover/focus/active states.

**Violating the letter of this order ("just this one `#1976D2`") violates its
spirit — the point is runtime-themeable, consistent UI.**

## The Theme System

The library defines these as CSS variables (a Tailwind 4 `@theme` block) that are
loaded at runtime with the library's stylesheet. Reference them; don't redefine.

**Colors — 12 roles × 10 shades** (120 variables), named
`--theme-{role}-{one..ten}`, `one` = lightest tint, `ten` = darkest.

| Group | Roles |
|-------|-------|
| Brand / action | `primary` (blue), `secondary` (teal), `tertiary` (deep purple), `accent` (amber), `highlight` (pink) |
| Status | `success` (green), `info` (light blue), `warning` (orange), `alert` (red) |
| Neutral | `default` (gray), `light`, `dark` |

**Shade-pairing conventions** (for legible contrast):

| Usage | Shade |
|-------|-------|
| Solid/filled surface bg | six–eight, with white / `light-one` text |
| Tonal/subtle bg (badges, hover, nav) | one–two, with `nine`/`ten` text |
| Body/label text on light bg | eight–ten |
| Borders (outlined variants) | five–seven |
| Hover on solid surface | one step darker; on tonal surface, one/two bg |

Exception: pale fills (`warning`, `light`) take dark text (`dark-nine`/`ten`).

**Other tokens:** fonts `--font-ui` (Lato, body/UI), `--font-heading`
(Montserrat, titles), `--font-display` (Oswald, hero), `--font-serif` (Roboto
Serif); `--font-sans`, the fallback stack for text without one of those fonts
(the library pins Tailwind 4.1's system stack); elevation `--mui-shadows-2/4/8`;
custom breakpoints `3xl`–`7xl`. The font files themselves aren't bundled: the
host page loads them (see the `maviray-blazor-components` setup).

**Tailwind 4 var syntax** — reference a theme var as an arbitrary value; add
state variants as normal prefixes:

```
bg-(--theme-primary-eight)      text-(--theme-alert-nine)
border-(--theme-primary-six)    ring-(--theme-primary-seven)
hover:bg-(--theme-primary-one)  active:text-(--theme-primary-nine)
font-ui  font-heading           (font tokens work as utilities)
```

## The Tailwind Constants (`Tailwind.Theme.*`)

In C# (`.razor` / code-behind), **prefer a `Tailwind.Theme.*` constant over a raw
class string** for themeable properties. Namespace:
`Maviray.Blazor.Components.Material.Constants`. Construct names by this pattern
(confirm exact spelling with IntelliSense):

| Namespace | Naming pattern → value |
|-----------|------------------------|
| `Theme.Colors.{Background,Border,Text,Outline,Ring,Divide}` | `THEME_{ROLE}_{SHADE}_{BG\|BORDER\|TEXT\|OUTLINE\|RING\|DIVIDE}` — e.g. `THEME_PRIMARY_SIX_BG` → `bg-(--theme-primary-six)` |
| …`.{Hover,Focus,Active}` (nested) | append state, e.g. `THEME_PRIMARY_SIX_BG_HOVER` → `hover:bg-(--theme-primary-six)` |
| `Theme.Typography.*` | Material type scale (below) |
| `Theme.Button` `.NavLink` `.Badge` `.Dialog` `.ZIndex` | pre-composed recipes / z-index steps |

`{SHADE}` is the word form: `ONE`…`TEN`. `{ROLE}` is `DEFAULT`, `PRIMARY`,
`SECONDARY`, `TERTIARY`, `ACCENT`, `HIGHLIGHT`, `SUCCESS`, `INFO`, `WARNING`,
`ALERT`, `LIGHT`, `DARK`.

**Constants vs. raw utilities:** constants for color, typography, state. Layout/
spacing utilities (`flex`, `gap-2`, `p-4`, `rounded-sm`, `w-96`) stay plain — no
constants exist for them, by design.

## Typography — Material Type Scale

Use `Tailwind.Theme.Typography.*` for semantic text instead of ad-hoc `text-*`;
each token bundles size + weight + tracking + leading + the right font.

| Tier | Tokens | Font |
|------|--------|------|
| Display | `DISPLAY_LARGE/MEDIUM/SMALL` | display |
| Headline | `HEADLINE_LARGE/MEDIUM/SMALL` | heading |
| Title | `TITLE_LARGE/MEDIUM/SMALL` | heading |
| Body | `BODY_LARGE/MEDIUM/SMALL` | ui |
| Label (buttons, chips) | `LABEL_LARGE/MEDIUM/SMALL` (+`LABEL_XL`) | ui |

## Component Usage Patterns

Most `Mavi*` components take some subset of these enums (namespace
`Maviray.Blazor.Components.Core.Enums`):

- **`ThemeColorScheme`**: Default, Primary, Secondary, Success, Alert, Warning,
  Info, Dark, Light. (Subset of the CSS roles — no Tertiary/Accent/Highlight.)
- **`ElementVariant`**: Filled, Outlined, Text.
- **`ElementSize`**: Regular, Large, Small.
- **`TextTransform`**: Normal, UpperCase, LowerCase, Capitalize (buttons default
  to UpperCase, per Material).

**Extension seam:** `MaviComponentBase` gives every component `Id`, `Class`,
`Style`, `Title`. Pass extra utilities via **`Class`** — components append it to
their computed classes, so you extend rather than fight the base. (A few
components, e.g. `MaviChip`, expose `CssClass` instead — check the component's
parameters / IntelliSense.)

```razor
@* Configure via enums; extend via Class — don't hand-roll a button *@
<MaviButton Title="Save"
            ThemeColorScheme="ThemeColorScheme.Primary"
            ElementVariant="ElementVariant.Filled"
            ElementSize="ElementSize.Regular"
            Class="w-full mt-4" />
```

## Building Custom Markup (no component fits)

Compose in this order so the result matches the library:

1. **Structure/layout** — `inline-flex items-center gap-2 rounded-sm …`
2. **Color** via theme vars — `bg-(--theme-primary-eight) text-(--theme-default-one)`
3. **States** — `hover:bg-(--theme-primary-nine) focus-visible:ring-2
   focus-visible:ring-(--theme-primary-seven) active:bg-(--theme-primary-four)`
4. **Typography** — a Material label/body token
5. **Elevation** — `shadow-lg` / a `--mui-shadows-*` var for raised surfaces

Include `focus-visible` rings for accessibility. If the markup is reusable in
your app, promote it to your own component driven by the enums above, mapping
each `ThemeColorScheme` to constants with a `switch`.

## Setup in a Consuming App (Tailwind 4 + NuGet RCL)

- **Load the library stylesheet** in your host page, served from the RCL at
  `_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css`
  (the full `<head>` wiring, fonts and icon font included, is in the
  `maviray-blazor-components` setup). It defines all 120 `--theme-*` vars and
  ships the `Mavi*` component classes **and every class a `Tailwind.Theme.*`
  constant produces** — so constants work without any Tailwind build of your own.
- **Raw class strings you write** (`gap-4`, `bg-(--theme-primary-eight)` typed in
  your markup) need **your own Tailwind 4 build** over *your* app's markup, using
  Tailwind 4.3 or later (the library CSS is built with 4.3.5). A source CSS:
  ```css
  @import "tailwindcss";

  @theme {
      /* Match the library's fallback font stack (Tailwind 4.3 changed the default). */
      --font-sans: ui-sans-serif, system-ui, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol", "Noto Color Emoji";
      /* Only if you use 3xl:–7xl: variants. */
      --breakpoint-3xl: 112rem;
      --breakpoint-4xl: 128rem;
      --breakpoint-5xl: 144rem;
      --breakpoint-6xl: 160rem;
      --breakpoint-7xl: 176rem;
  }
  ```
  Compile with the Tailwind CLI (e.g. `npx @tailwindcss/cli -i in.css -o
  out.min.css --minify`) and link the output after the library stylesheet.
- The `(--theme-*)` vars resolve **at runtime** from the loaded library CSS, so
  your `bg-(--theme-primary-eight)` classes work without redefining the palette
  in your build.

## Do / Don't

| Do | Don't |
|----|-------|
| `bg-(--theme-primary-eight)` / `THEME_PRIMARY_EIGHT_BG` | `bg-blue-700`, `bg-[#1976D2]`, inline `style="color:#…"` |
| Material type tokens (`BODY_MEDIUM`, `TITLE_LARGE`) | ad-hoc `text-sm font-semibold` piles for headings/body |
| Reuse/configure a `Mavi*` component; extend via `Class` | rebuild an existing component from scratch |
| Neutral roles (`default`/`light`/`dark`) for surfaces & text | stock `gray-*`/`slate-*` for themed surfaces |
| `shadow-lg` or `--mui-shadows-*` for elevation | bespoke `box-shadow` values |
| Layout utilities (`flex`, `gap-*`, `p-*`) as plain classes | inventing constants for pure spacing/layout |

## Pointers

- **Installed version:** check the app's `.csproj`
  `<PackageReference Include="Maviray.Blazor.Components.Material" Version="…" />`.
- **Exhaustive constant/token/component lookups:** if a local checkout of the
  `blazor-ui-kit` repo is available as an added directory, the sources of truth
  are `src/Maviray.Blazor.Components.Material/wwwroot/css/maviray.material.theme.css`
  (tokens), `src/Maviray.Blazor.Components.Material/Constants/Tailwind.cs`
  (constants), `.../Components/**`, `src/Maviray.Blazor.Components.Core/Enums/**`,
  and `samples/**/Pages/**` (living examples). ⚠️ The checkout may be a different
  version than the installed package — verify against the `<PackageReference>`.
- **Otherwise:** rely on the tables above and IDE IntelliSense over the
  referenced assembly.
