# Feedback & Overlays

Alerts, toasts, dialogs, backdrops, skeletons, spinners. Namespaces:
`…Components.Feedback` (`MaviAlert`, `MaviBackdrop`, `MaviDialog`, `MaviSkeleton`),
`…Components.DataDisplay.MaviToastr` (`MaviToastr`), `…Components.DataDisplay`
(`MaviToastNotification`), `…Components.Spinner` (spinners).

Several of these are **imperatively driven** — hold an `@ref` and call methods.

---

## MaviAlert  (`MaviComponentBase`)

Inline alert banner with icon, title, body, and optional dismiss button. Hidden
until shown via a method (or after `Display`).

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Title` | `string?` | — | Bold heading (base `Title`). |
| `Text` | `string?` | — | Body text. |
| `Icon` | `string?` | — | Icon CSS class; defaults to a scheme-appropriate icon. |
| `Dismissible` | `bool` | `false` | Show the close (✕) button. |
| `OnDismiss` | `EventCallback<string>` | — | Fires with the alert `Id` on dismiss. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Success/Info/Warning/Alert pick default icons. |
| `ElementVariant` | `ElementVariant` | `Filled` | `Filled, Outlined, Text`(tonal). |

**Methods (via `@ref`):**
- `Task Display(string? title, string? text)`
- `Task Display(string? title, string? text, ElementVariant variant, ThemeColorScheme colorScheme)`
- `Task Hide()`

```razor
<MaviAlert @ref="_alert" ThemeColorScheme="ThemeColorScheme.Success" Dismissible="true" />
@code {
    private MaviAlert _alert = default!;
    async Task Saved() => await _alert.Display("Saved", "Your changes were saved.");
}
```

---

## MaviToastr  (`…Components.DataDisplay.MaviToastr`, `MaviComponentBase`)

Needs both `@using Maviray.Blazor.Components.Material.Components.DataDisplay.MaviToastr`
(the component) and `@using Maviray.Blazor.Components.Material.Components.DataDisplay`
(the `MaviToastNotification` record).

Renders a fixed-position stack of toast notifications from a list the parent
owns. It handles auto-dismiss timing and raises `OnClose`; **the parent removes
the item from its list**. Toasts are described by the immutable record
`MaviToastNotification`.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Notifications` *(required)* | `IReadOnlyList<MaviToastNotification>` | empty | The toasts to show (parent-owned). |
| `Position` | `ElementRelativePosition` | `CenterTop` | `CenterTop/CenterBottom/TopLeft/TopRight/BottomLeft/BottomRight`. |
| `AutoDismiss` | `bool` | `true` | Schedule dismissal for toasts with a `Duration`. |
| `NewestOnTop` | `bool` | `true` | Ordering. |
| `MaxVisible` | `int` | 5 | Max shown at once (0 = unlimited). |
| `OnClose` | `EventCallback<string>` | — | Fires with the toast `Id` on close/timeout — parent must remove it. |

`MaviToastNotification` (record):
`Id, Title, Message, ThemeColorScheme = Default, ElementVariant = Filled,
ElementSize = Regular, CreatedAt = default, TimeSpan? Duration = null,
Closable = true, ShowProgress = true`. A `Duration` enables the timed progress
bar and auto-dismiss.

```razor
<MaviToastr Notifications="_toasts" Position="ElementRelativePosition.TopRight"
            OnClose="RemoveToast" />
@code {
    private List<MaviToastNotification> _toasts = new();
    void Notify() => _toasts.Add(new(Guid.NewGuid().ToString(), "Done", "Saved.",
        ThemeColorScheme.Success, Duration: TimeSpan.FromSeconds(4)));
    void RemoveToast(string id) => _toasts.RemoveAll(t => t.Id == id);
}
```

---

## MaviDialog  (`MaviDialogBase` : `MaviComponentBase`)

Modal dialog with header (title + close), body (`ChildContent`), and a footer of
Confirm/Cancel buttons. Includes a built-in busy overlay (spinner) that shows
while a button handler runs. Configured through a `MaviDialogBaseParameters`
object and driven imperatively.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `MaviDialogBaseParameters` | `MaviDialogBaseParameters` | `new()` | All appearance/behavior config (below). |
| `ChildContent` | `RenderFragment?` | — | Dialog body. |
| `DialogButtonClick` | `EventCallback<DialogButtonClickEventArgs>` | — | Fires on any footer/close button with which button was clicked. |

**`MaviDialogBaseParameters`** (mutable config; has fluent `SetTitle`/`SetWidth`/
`SetBackgroundColor`/`SetModalCss` helpers and `Update(other)`):

| Property | Default | Purpose |
|----------|---------|---------|
| `Title` | — | Header title. |
| `ThemeColorScheme` | `Default` | Buttons/borders color. |
| `ZIndex` | `Forty` | Stacking. |
| `BackdropOpacity` | `Lighten` | Busy overlay opacity (`None/Lighten/Darken`). |
| `ComponentRelativePosition` | `Center` | On-screen placement. |
| `SpinnerSize` | `Regular` | Busy spinner size. |
| `CloseOnBackdropClick` | `false` | |
| `CloseOnUserAction` | `true` | Auto-close after Confirm/Cancel. |
| `DisplayConfirmButton` / `DisplayCancelButton` / `DisplayCloseButton` | `true` | Which buttons render. |
| `ConfirmButtonTitle` / `CancelButtonTitle` / `CloseButtonTitle` | `"Confirm"/"Cancel"/"Close"` | Labels. |
| `HideOverflow` | `false` | `overflow-hidden` on the box. |
| `Width` | `"w-full"` | Box width class. |
| `BackgroundColor` | `"bg-white"` | Box background class. |
| `DialogBoxCss` / `ContainerOverrideCss` | — | Extra/override classes. |

**Methods (via `@ref`):**
- `Task Display()` / `Display(string title)` / `Display(MaviDialogBaseParameters parameters)`
- `Task Display(Func<DialogButtonClick, Task> onButtonClick)` — and overloads
  taking a title or parameters — register a per-open callback invoked with the
  clicked button (`Confirm`/`Cancel`/`Close`); latest `Display` wins.
- `Task Hide()`
- `Task SetBusy()` / `Task SetIdle()` — toggle the in-dialog busy spinner.

```razor
<MaviDialog @ref="_dialog" ChildContent="@_body" DialogButtonClick="OnDialogButton" />
@code {
    private MaviDialog _dialog = default!;
    async Task Confirm() =>
        await _dialog.Display(
            new MaviDialogBaseParameters { ThemeColorScheme = ThemeColorScheme.Alert }
                .SetTitle("Delete item?"),
            async click =>
            {
                if (click == DialogButtonClick.Confirm) await DeleteAsync();
            });
}
```

---

## MaviBackdrop  (`MaviComponentBase`)

Overlay that centers content over its container — used for loading/busy states
and simple modals.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `ChildContent` | `RenderFragment?` | — | Centered content (e.g. a spinner). |
| `Opacity` | `BackdropOpacity` | `Darken` | `None` (no overlay), `Darken` (black/30), `Lighten` (white/70). |
| `CloseOnBackdropClick` | `bool` | `false` | Backdrop click hides it. |
| `OnBackdropClick` | `EventCallback` | — | Fires on backdrop click. |
| `CenterContent` | `bool` | `true` | Grid-center the content. |
| `ZIndex` | `ZIndex` | `Zero` | Stacking (`z-*`). |
| `Visible` | `bool` | `false` | Bind to show/hide declaratively. |
| `InitiallyActive` | `bool` | `false` | Start active on first render. |

`bool Active { get; }` and **methods** `Task Display()` / `Task Hide()` control
it imperatively. It positions `absolute inset-0`, so place it in a `relative`
parent to scope the overlay.

```razor
<div class="relative">
    <MaviBackdrop Visible="_loading" Opacity="BackdropOpacity.Lighten">
        <MaviSpinnerStarRotate ThemeColorScheme="ThemeColorScheme.Primary" />
    </MaviBackdrop>
    @* content *@
</div>
```

---

## MaviSkeleton

Loading placeholder (Material-style shimmer). Standalone (no base component).

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Variant` | `SkeletonVariant` | `Text` | `Text, Rectangular, Rounded, Circular`. |
| `Animation` | `SkeletonAnimation` | `Pulse` | `None, Pulse, Wave`. |
| `Width` / `Height` | `string?` | — | e.g. `"240px"`, `"100%"`, `"3rem"`. |
| `MinWidth` / `MinHeight` / `MaxWidth` / `MaxHeight` | `string?` | — | Constraints. |
| `Lines` | `int` | 1 | For `Text`: number of shimmer lines. |
| `LineHeight` / `LineGap` | `string` | `"0.9rem"` / `"0.5rem"` | Multiline metrics. |
| `LastLineWidth` | `string?` | `"60%"` | Last line width when `Lines > 1`. |
| `BorderRadius` | `string?` | — | Override default per variant. |
| `FullWidth` | `bool` | `false` | Quick `100%` width. |
| `Class` / `Style` | `string?` | — | |

```razor
<MaviSkeleton Variant="SkeletonVariant.Text" Lines="3" FullWidth="true" />
<MaviSkeleton Variant="SkeletonVariant.Circular" Width="48px" />
```

---

## Spinners  (`…Components.Spinner`, all inherit `MaviSpinnerBase`)

Six visual styles, identical API. Choose by look:

`MaviSpinner`, `MaviSpinnerRoller`, `MaviSpinnerGrid`, `MaviSpinnerEqualizer`,
`MaviSpinnerStar`, `MaviSpinnerStarRotate`.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Color. |
| `ElementSize` | `ElementSize` | `Regular` | Baseline scale: Small→0.5, Regular→0.75, Large→1.0. |
| `Scale` | `double` | 0 | Override scale in (−1..1]; `0` = use `ElementSize`. |

```razor
<MaviSpinnerStarRotate ThemeColorScheme="ThemeColorScheme.Primary"
                       ElementSize="ElementSize.Large" />
```
