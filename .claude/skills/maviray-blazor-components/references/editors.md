# Rich Text Editor

Namespace `Maviray.Blazor.Components.Material.Components.Editors.Wysiwyg`.

## WysiwygEditor  (`MaviComponentBase`, `IAsyncDisposable`)

A rich-text (WYSIWYG) HTML editor: a `contenteditable` surface plus a formatting
toolbar. The editing behavior is backed by a JS module
(`_content/Maviray.Blazor.Components.Material/js/maviWysiwygJsInterop.js`, lazy-
loaded on first render) that runs the rich-text commands and returns the updated
HTML; the component also uses the shared outside-click listener
(`maviJsInterop.js`) to close its toolbar popovers. Ensure both scripts are
available in the host.

> Note: unlike most components this one is **not** prefixed `Mavi*` — the tag is
> `<WysiwygEditor>`. It carries the base `Id`, `Class`, `Style`, `Title`.

### What the toolbar offers

- **Block style** (popover): Normal (`p`), Heading 1–6, Quote (`blockquote`),
  Code (`pre`).
- **Inline formatting**: Bold, Italic, Underline, Strikethrough, Clear formatting.
- **Lists**: unordered, ordered.
- **Paragraph** (popover): align left/center/right, justify, indent, outdent.
- **Font family** (popover) and **font size** (popover).
- **Color** (popover): text color + highlight, from the palette.
- **Link** (popover): text + URL → inserts an anchor.
- **Image**: file picker; the chosen image is embedded **inline as a base64
  `data:` URL** (no upload endpoint), subject to `MaxImageBytes`.
- **Table**: grid picker (up to 8×8) inserts a table.
- Optional **Refresh**, **Save**, **Code view** (edit raw HTML), and
  **Fullscreen** buttons.

The toolbar is hidden when `ReadOnly` is true.

### Parameters

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Content` / `@bind-Content` | `string?` | — | The editor HTML. Two-way bindable; the model is pushed back on **blur** and after each toolbar command (not on every keystroke). |
| `ContentChanged` | `EventCallback<string?>` | — | Change callback (enables `@bind-Content`). |
| `Placeholder` | `string?` | — | Placeholder shown when empty. |
| `ReadOnly` | `bool` | `false` | Non-editable, toolbar hidden (content still displays). |
| `Disabled` | `bool` | `false` | Dimmed + non-interactive (`pointer-events-none`). |
| `MinHeight` | `string` | `"240px"` | Min height of the editing surface / code view. |
| `ShowSaveButton` | `bool` | `false` | Show the Save toolbar button. |
| `OnSave` | `EventCallback<string>` | — | Fires with the current HTML when Save is clicked. |
| `ShowRefreshButton` | `bool` | `false` | Show the Refresh toolbar button. |
| `RefreshContentProvider` | `Func<Task<string?>>?` | — | Supplies HTML when Refresh is clicked (or `RefreshAsync()` is called); result replaces the content. |
| `ShowCodeViewButton` | `bool` | `true` | Allow toggling a raw-HTML `<textarea>` code view. |
| `ShowFullscreenButton` | `bool` | `true` | Allow toggling fullscreen. |
| `FontFamilies` | `IEnumerable<string>?` | `WysiwygDefaults.Fonts` | Font list for the font popover. |
| `FontSizes` | `IEnumerable<int>?` | `WysiwygDefaults.Sizes` | Sizes (px) for the size popover. |
| `ColorPalette` | `IEnumerable<string>?` | `WysiwygDefaults.Colors` | Colors for text/highlight popovers. |
| `MaxImageBytes` | `long` | `5 * 1024 * 1024` (5 MB) | Max inserted-image size; larger files are silently skipped. |
| `Id`, `Class`, `Style`, `Title` | | | As base (`Class` extends the root; `Title` becomes the surface aria-label). |

**Defaults** (`WysiwygDefaults`, same namespace):
- Fonts: Arial, Helvetica, Georgia, Tahoma, Times New Roman, Verdana, Courier New.
- Sizes (px): 10, 12, 14, 16, 18, 24, 32 (16 is the base).
- Colors: 12 swatches (`#000000`, `#495057`, `#e03131`, `#d6336c`, `#ae3ec9`,
  `#7048e8`, `#1971c2`, `#0c8599`, `#2f9e44`, `#f08c00`, `#e8590c`, `#ffffff`).

Block styles are `WysiwygBlockStyle.All` (Normal, Heading 1–6, Quote, Code).

### Public methods (via `@ref`)

| Method | Purpose |
|--------|---------|
| `Task<string> GetContentAsync()` | Current HTML of the surface, sanitized. |
| `Task<string> GetTextAsync()` | Current plain text (no markup). |
| `Task SetContentAsync(string? html)` | Sanitize the HTML, replace the content and raise `ContentChanged`. |
| `Task ClearAsync()` | Clear all content. |
| `Task FocusAsync()` | Focus the editing surface. |
| `Task RefreshAsync()` | Pull HTML from `RefreshContentProvider` and set it (host-callable even without the toolbar button). |

### Usage

```razor
@using Maviray.Blazor.Components.Material.Components.Editors.Wysiwyg

<WysiwygEditor @bind-Content="_html"
               Placeholder="Write something…"
               MinHeight="320px"
               ShowSaveButton="true" OnSave="SaveAsync" />

@code {
    private string? _html = "<p>Hello <strong>world</strong></p>";
    async Task SaveAsync(string html) => await _repo.SaveAsync(html);
}
```

Read/write imperatively via `@ref` when you don't want to bind:

```razor
<WysiwygEditor @ref="_editor" ShowRefreshButton="true"
               RefreshContentProvider="LoadDraftAsync" />

@code {
    private WysiwygEditor _editor = default!;
    Task<string?> LoadDraftAsync() => _repo.GetDraftHtmlAsync();
    async Task Publish() => await _repo.PublishAsync(await _editor.GetContentAsync());
}
```

### Gotchas

- **Content updates on blur / toolbar command, not per keystroke.** If you need
  the very latest text mid-edit, call `GetContentAsync()`/`GetTextAsync()` rather
  than reading the bound field.
- **Images are inlined as base64 `data:` URLs** — no server upload. This bloats
  the stored HTML; keep `MaxImageBytes` conservative, or post-process the HTML to
  externalize images if size matters.
- **Requires the JS assets** (`maviWysiwygJsInterop.js` + `maviJsInterop.js`).
  Without JS runtime the surface renders but editing/toolbar commands are inert.
- **HTML is sanitized in both directions** with
  [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer): `Content`,
  `SetContentAsync` and refresh/code-view HTML are cleaned before they reach the
  page, and `ContentChanged`, `OnSave` and `GetContentAsync` only return cleaned
  HTML. Scripts, event handlers, `javascript:` URLs, iframes and form controls
  are removed; everything the toolbar produces (fonts, colors, links incl.
  `mailto:`/`tel:`, tables, `data:` images) is kept.
- **Change the policy** by registering your own sanitizer, e.g. to allow video
  embeds:
  ```csharp
  var sanitizer = new HtmlSanitizer();   // using Ganss.Xss;
  sanitizer.AllowedTags.Add("iframe");
  builder.Services.AddSingleton<IHtmlSanitizer>(sanitizer);
  ```
- **Still sanitize on the server** whatever HTML your API accepts. In
  WebAssembly apps the editor runs in the user's browser, so a request can bypass
  it; the editor's cleaning protects the people viewing content, not your API.
