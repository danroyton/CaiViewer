# Viewer Application Specification
## FilescannerCLI – CivitAI Model Repository Viewer

---

## Purpose

The Viewer is a standalone desktop application that:
- Reads the SQLite database populated by FilescannerCLI
- Provides fast, multi-criteria search across all stored models and versions
- Displays model and version metadata in a structured layout
- Renders preview images stored in local ZIP archives
- Allows export and re-scan actions

---

## Technology Recommendation

| Concern | Recommendation |
|---|---|
| Platform | Windows desktop (.NET 8) |
| UI Framework | WPF or WinUI 3 (XAML-based, supports image rendering and virtualized lists) |
| Database access | Microsoft.Data.Sqlite + Dapper or EF Core |
| Image display | `System.Windows.Controls.Image` / MagickImage for thumbnails |
| ZIP reading | `System.IO.Compression.ZipFile` |
| Full-text search | SQLite FTS5 |

Alternative: Blazor Hybrid (MAUI) if cross-platform is required.

---

## Application Layout

```
┌──────────────────────────────────────────────────────────────────────────┐
│  [Search Bar]         [Filter Panel ▼]        [Sort ▼]   [Refresh] [⚙]  │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────┐  ┌───────────────────────────────────┐│
│  │                                │  │  Model Detail Panel               ││
│  │  Results List / Grid           │  │                                   ││
│  │  (thumbnail + title + type     │  │  [Model Name]  [Type badge]       ││
│  │   + base model + version count)│  │  Creator: …   Tags: …            ││
│  │                                │  │  Description (scrollable HTML)    ││
│  │  ···                           │  │  ─────────────────────────────    ││
│  │  ···                           │  │  Version Selector (tab / combo)   ││
│  │  ···                           │  │  ─────────────────────────────    ││
│  │                                │  │  Version Detail                   ││
│  │                                │  │  Base Model / Trigger Words       ││
│  │                                │  │  Stats / Hashes / Files           ││
│  │                                │  │  ─────────────────────────────    ││
│  │                                │  │  Image Gallery (scrollable)       ││
│  │                                │  │  [img] [img] [img] [img] …       ││
│  └────────────────────────────────┘  └───────────────────────────────────┘│
│  [Status bar: N models found | DB path | last import]                    │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Search & Filter Panel

### Full-Text Search Bar

- Single text input field
- Searches across: `Model.name`, `Model.description`, `ModelVersion.name`, `ModelVersion.description`, `ModelVersionTrainedWord.word`, `ModelVersionImage.prompt`
- Uses SQLite FTS5 MATCH syntax internally
- Debounced (300 ms) live search as user types

---

### Filter Panel

Collapsible side panel or dropdown with the following filter groups:

#### Model Type Filter
Multi-select checkbox list populated from distinct `Model.type` values:
- `Checkpoint`
- `LORA`
- `TextualInversion`
- `Hypernetwork`
- `Controlnet`
- `Poses`
- *(others as present in DB)*

#### Base Model Filter
Multi-select list from distinct `ModelVersion.base_model`:
- `SD 1.5`
- `SDXL 1.0`
- `Flux.1 D`
- `Flux.1 S`
- *(others as present in DB)*

#### NSFW Level Filter
Range slider or segmented control:
- `SFW only` (nsfw_level = 0)
- `Allow mild` (nsfw_level ≤ 4)
- `All content`

#### Tag Filter
- Autocomplete text input to add tags
- Selected tags shown as chips; results must match ALL selected tags (AND) or ANY (toggle)

#### Creator Filter
- Autocomplete text input from `Creator.username`

#### Status Filter
- `Published` / `Draft` / `All`

#### Date Range Filter
- Published between `[from date]` and `[to date]` (applied to `ModelVersion.published_at`)

#### Has Local ZIP
- Toggle: show only versions with a local ZIP (`ModelVersion.zip_path IS NOT NULL`)

#### Availability Filter
- `Public` / `EarlyAccess` / `All`

---

## Sort Options

| Label | Sort column |
|---|---|
| Name A–Z | `Model.name ASC` |
| Name Z–A | `Model.name DESC` |
| Newest first | `ModelVersion.published_at DESC` |
| Oldest first | `ModelVersion.published_at ASC` |
| Most downloaded | `ModelVersion.stat_download_count DESC` |
| Highest rated | `ModelVersion.stat_rating DESC` |
| NSFW level ↑ | `ModelVersion.nsfw_level ASC` |

---

## Results List / Grid

### List Mode
Each row shows:
- Thumbnail (first image from `ModelVersionImage`, loaded from local ZIP or URL fallback)
- Model name + version name
- Type badge (coloured pill: Checkpoint / LORA / …)
- Base model label
- Creator username
- Version count
- NSFW level indicator
- Local ZIP present indicator (✓ / ✗)
- Star rating

### Grid Mode
- Thumbnail-dominant card layout (similar to CivitAI website)
- Model name, type, base model and rating overlay

Virtualized scrolling (only render visible rows/cards).

---

## Model Detail Panel

Displayed when a result is selected.

### Header Section
- Model name (large)
- Type badge
- Creator name (clickable → filter by creator)
- NSFW level badge
- Tags (clickable chips → add to tag filter)
- License flags (icons: commercial use, derivatives, credit)

### Description Section
- Scrollable rendered HTML or plain-text area
- "Show full description" expand button

### Version Selector
- Tab strip or dropdown listing all `ModelVersion` records for this model
- Each tab shows: version name + published date + base model
- Selecting a tab updates the version detail section

### Version Detail Section
| Field | Display |
|---|---|
| Version name | Heading |
| Base model | Label |
| Status / Availability | Badge |
| Published at | Formatted date |
| Trigger words | Comma-separated, each copyable on click |
| Description | Scrollable |
| AIR identifier | Monospace label |
| Stats | Download count, thumbs up, rating |
| Files table | name, size, type, fp, format, primary flag, hash (AutoV2/V3), scan results, download button |
| Local ZIP path | Clickable → open in Explorer |

### Image Gallery Section
- Horizontal or wrapping grid of thumbnails
- Images loaded from local ZIP (`images\` directory) if available, else from URL
- Click to open lightbox / full-size view
- Each image tooltip or overlay shows: NSFW level, dimensions, prompt (if available)
- NSFW images blurred by default; click to reveal (respects NSFW level filter setting)

---

## Lightbox / Full Image View

- Full-screen overlay
- Arrow navigation between images in the same version
- Shows: image URL, dimensions, NSFW level, generation prompt (if available)
- "Copy prompt" button
- "Open original URL" button
- Close with Escape or click outside

---

## Actions

| Action | Trigger | Description |
|---|---|---|
| Open ZIP in Explorer | Button in version detail | Opens the folder containing the ZIP |
| Copy trigger words | Click on trigger word chip | Copies the word to clipboard |
| Copy AutoV2 hash | Button in files table | Copies hash to clipboard |
| Copy model ID | Button in header | Copies CivitAI model ID |
| Copy version ID | Button in version detail | Copies CivitAI model-version ID |
| Open on CivitAI | Button | Opens `https://civitai.com/models/{model_id}?modelVersionId={version_id}` in browser |
| Filter by creator | Click creator name | Adds creator to filter panel |
| Filter by tag | Click tag chip | Adds tag to filter panel |
| Refresh DB | Toolbar button | Reloads all data from SQLite (does not re-scan files) |

---

## Settings Screen

| Setting | Type | Description |
|---|---|---|
| Database path | File picker | Path to the SQLite `.db` file |
| Central CivitAI repo path | Folder picker | Root path for ZIP lookup (`cai-repo`) |
| Default NSFW filter | Dropdown | Remembered across sessions |
| Thumbnail size | Slider (S/M/L) | Grid card size |
| Image placeholder | Toggle | Show blurhash placeholder while loading |
| FilescannerCLI path | File picker | Path to `FilescannerCLI.exe` (for re-scan action, future) |

---

## Performance Requirements

| Metric | Target |
|---|---|
| Initial load (10 000 models) | < 2 s |
| Search results (FTS query) | < 300 ms |
| Thumbnail render (visible rows) | < 100 ms per image |
| ZIP image extraction | < 500 ms per image |
| Memory footprint | < 256 MB at idle |

---

## Error Handling

| Situation | Behaviour |
|---|---|
| ZIP file missing / moved | Show placeholder icon; tooltip: "ZIP not found: `<path>`" |
| Image not in ZIP | Show placeholder; fall back to URL fetch if online |
| DB not found at startup | Show setup wizard to select/create DB |
| DB schema mismatch | Show migration prompt or open read-only |
| Network unavailable (URL fallback) | Silent fallback to placeholder; no crash |
