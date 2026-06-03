# Architecture Document
## CivitAI Model Repository Viewer
### Version 1.0 | .NET 10 | Cross-Platform

---

## 1. System Context

The Viewer is a standalone application that operates **entirely offline** against a local SQLite database and a local directory tree of ZIP archives produced by **FilescannerCLI**. It optionally shells out to FilescannerCLI to trigger new scans.

```
┌──────────────────────────────────────────────────────────────────┐
│                        User / Operator                           │
└────────────┬──────────────────────────────────┬─────────────────┘
			 │ uses                             │ uses
			 ▼                                 ▼
┌────────────────────────┐       ┌─────────────────────────────┐
│   CivitAI Viewer App   │       │     FilescannerCLI.exe      │
│  (AvaloniaUI / WASM)   │──────▶│  (existing CLI scanner)     │
└────────┬───────────────┘ shell │  produces ZIP archives      │
		 │                 out   └─────────────────────────────┘
		 │ reads                            │ produces
		 ▼                                 ▼
┌────────────────┐            ┌──────────────────────────────┐
│  SQLite DB     │◀── import ─│  Directory tree of ZIPs      │
│  (cai.db)      │            │  ~/cai-repo/**/*.zip         │
└────────────────┘            └──────────────────────────────┘
```

---

## 2. Solution Structure

```
CaiViewer/                          (Solution root)
│
├── CaiViewer.Core/                 .NET 10 class library – no UI dependency
│   ├── Domain/                     Entity classes (Model, ModelVersion, …)
│   ├── Database/                   SQLite access (Dapper + raw SQL / EF Core)
│   │   ├── CaiDbContext.cs
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Import/                     ZIP directory scanner & DB importer
│   │   ├── ZipScanner.cs           Recursive directory walker
│   │   ├── ZipParser.cs            Reads model_meta.json / modelversion_meta.json
│   │   ├── ImportService.cs        Deduplication + ZipLocation alias logic
│   │   └── ContentHasher.cs        SHA-256 fingerprint of JSON content
│   ├── Scanner/                    FilescannerCLI process wrapper
│   │   ├── ScannerService.cs
│   │   └── ScannerOptions.cs       Mirrors SPEC_OPERATING_MODES
│   └── Search/                     FTS5 query builder
│       └── SearchService.cs
│
├── CaiViewer.Frontend.Desktop/              AvaloniaUI desktop project (Windows + Linux)
│   ├── App.axaml
│   ├── Views/
│   ├── ViewModels/                 MVVM (CommunityToolkit.Mvvm)
│   └── Program.cs                  Avalonia desktop entry point
│
├── CaiViewer.Frontend.Browser/              AvaloniaUI WASM browser project
│   └── Program.cs                  Avalonia browser entry point
│
└── CaiViewer.Tests/                xUnit tests for Core
```

---

## 3. Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Presentation Layer (CaiViewer.Desktop / CaiViewer.Browser)     │
│  AvaloniaUI XAML Views + MVVM ViewModels                        │
│  Tabs: [Browse] [Import] [Scanner] [Settings]                   │
└───────────────────────────┬─────────────────────────────────────┘
							│ calls
┌───────────────────────────▼─────────────────────────────────────┐
│  Application / Service Layer (CaiViewer.Core)                   │
│  SearchService | ImportService | ScannerService                 │
└──────┬──────────────────────────────────────┬───────────────────┘
	   │ queries                              │ reads ZIPs
┌──────▼──────────────┐          ┌────────────▼──────────────────┐
│  Database Layer     │          │  File System Layer            │
│  SQLite via Dapper  │          │  ZipScanner + ZipParser       │
│  FTS5               │          │  System.IO.Compression /      │
│  cai.db             │          │  .NET 10 ZIP extensions       │
└─────────────────────┘          └───────────────────────────────┘
```

---

## 4. Key Components

### 4.1 ZipScanner (Import Pipeline)

Responsible for recursively walking a root directory and importing ZIP archives into the database.
Always ask for the BaseDirectory to use as the root and keep zip locations always relative to the root, not to the drive
```
ZipScanner.ScanAsync(rootPath)
  └─ foreach *.zip (recursive)
	   └─ ZipParser.Parse(zipPath)
			├─ Extracts model_meta.json      → RootModel DTO
			├─ Extracts modelversion_meta.json → RootModelVersion DTO
			└─ Returns ParseResult { ModelDto, VersionDto, ImageCount, ContentHash }
  └─ ImportService.ImportAsync(ParseResult, zipPath)
	   ├─ ContentHasher.Compute(modelJson + versionJson)
	   ├─ Check DB: ModelVersion with same id exists?
	   │    ├─ NO  → Insert Model + ModelVersion + Files + Images + TrainedWords
	   │    │        Insert ZipLocation(model_version_id, zipPath, hash, isPrimary=true)
	   │    └─ YES → ContentHash changed?
	   │              ├─ YES → Insert new ModelVersion row (new snapshot),
	   │              │        link ZipLocation to new snapshot
	   │              └─ NO  → Add ZipLocation alias only (if path not yet registered)
	   │                        (Skip all other writes)
```

### 4.2 SearchService

Wraps SQLite FTS5 queries. Builds parameterized SQL from `SearchQuery` DTO covering:
- Full-text MATCH across FTS5 virtual tables
- Structured filters (type, base_model, nsfw_level, tags, creator, date range, has_zip)
- Sort column mapping
- Pagination (LIMIT / OFFSET)

### 4.3 ScannerService

Wraps `FilescannerCLI.exe` as a child process:
- Builds argument strings from `ScannerOptions` (see §7)
- Streams stdout/stderr to the UI via `IProgress<string>` / `Channel<string>`
- Supports cancellation via `CancellationToken` → kills process
- Returns `ScanResult { ExitCode, LogLines }`

### 4.4 Image Loading

Images are always sourced from the local ZIP first:

```
ImageLoader.LoadAsync(ZipLocation, localFilename)
  ├─ Open ZipArchive (System.IO.Compression, read-only)
  ├─ Find entry by name pattern: *_<index>_<id>.<ext>
  ├─ Decode to Avalonia Bitmap (via stream)
  └─ Cache in LRU MemoryCache (configurable max entries)
```

Fallback: URL fetch (only if `allowNetworkFallback = true` in settings).

---

## 5. UI Structure (AvaloniaUI)

### Main Window Tabs

```
┌──────────────────────────────────────────────────────┐
│  [📷 Browse]  [📥 Import]  [⚡ Scanner]  [⚙ Settings] │
└──────────────────────────────────────────────────────┘
```

#### Tab: Browse
Full implementation per `SPEC_VIEWER.md`:
- Top toolbar: Search bar, Filter dropdown, Sort dropdown, Refresh, Settings shortcut
- Left panel: Result list / grid (virtualized `ItemsRepeater`)
- Right panel: Model detail with version tabs, file table, image gallery
- Bottom status bar

#### Tab: Import
Directory scanner UI:
- Root path input + folder picker button
- "Scan Directory" button → progress bar + live log output
- Summary after completion: N new, N updated, N aliases added, N skipped
- Option toggles: recursive, follow symlinks, dry-run mode

#### Tab: Scanner
FilescannerCLI wrapper UI (see §7 for full specification).

#### Tab: Settings
Per `SPEC_VIEWER.md` settings table.

---

## 6. Cross-Platform Strategy

| Platform | Project | Entry Point | Notes |
|---|---|---|---|
| Windows | `CaiViewer.Desktop` | `Program.cs` (AppBuilder.UsePlatformDetect) | Win32 file pickers, Process.Start Explorer |
| Linux | `CaiViewer.Desktop` | same binary | xdg-open for file manager, GTK/X11/Wayland via Avalonia |
| Browser | `CaiViewer.Browser` | Avalonia WASM host | File system access via browser File API / virtual FS; no shell-out to FilescannerCLI; read-only mode |

**Browser limitations** (documented in Settings UI):
- Import tab disabled (no direct file system access)
- Scanner tab disabled (cannot launch child processes)
- DB loaded via file upload dialog or pre-bundled path
- Images loaded from ZIP via browser File API

---

## 7. Scanner Tab Specification

The Scanner tab provides a GUI front-end for all `SPEC_OPERATING_MODES.md` modes.

### Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  Scanner Mode:  ◉ File/Hash Scan (-S/-A)  ○ URL/ID Scan (-U)  │
│                 ○ Inspect SafeTensor (-I)                       │
├─────────────────────────────────────────────────────────────────┤
│  INPUT                                                          │
│  ┌────────────────────────────────────────────────┐ [Browse]   │
│  │  <path to .safetensors / InXxxList.txt / hash> │            │
│  └────────────────────────────────────────────────┘            │
│  Input type: ◉ Single file  ○ InfileList.txt                   │
│              ○ InAutoV2List.txt  ○ InUrlList.txt                │
├─────────────────────────────────────────────────────────────────┤
│  OPTIONS                                                        │
│  [✓] Create ZIP (-z)          [✓] All version metadata (-a)    │
│  [ ] File output (-f)         [ ] Write image URL lists (-i)   │
│  [ ] History output (-h)      [ ] Update mode (-u)             │
│  [ ] Debug output (-d)        [ ] Fast fail (-N)               │
│  ─── Download options ──────────────────────────────────────── │
│  [ ] Primary tensor if missing (-p)                            │
│  [ ] All versions primary tensor (overwrite) (-t)              │
│  [ ] All versions primary tensor (if missing) (-c)             │
│  [ ] All files all versions (overwrite) (-E)                   │
│  [ ] All files all versions (if missing) (-C)                  │
├─────────────────────────────────────────────────────────────────┤
│  PARAMETERS                                                     │
│  Central CivitAI Path (CCD:): [________________] [Browse]      │
│  Max download size MB (DLL:): [1600            ]               │
│  API Key (APIKEY:):           [________________]               │
│  Work Directory (WORKDIR:):   [________________] [Browse]      │
├─────────────────────────────────────────────────────────────────┤
│  GENERATED COMMAND (read-only, copyable)                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ FilescannerCLI.exe "..." -Szah CCD:... DLL:1600         │   │
│  └─────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│  [▶ Run]  [⏹ Stop]  [Clear Log]                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  > Assuming Central CivitAI Path: C:\...                │   │
│  │  > Processing file 1/42: model.safetensors              │   │
│  │  > ZIP written: model_v1_123456.zip                     │   │
│  │  …                                                      │   │
│  └─────────────────────────────────────────────────────────┘   │
│  [Progress bar                                         12/42]  │
└─────────────────────────────────────────────────────────────────┘
```

The generated command preview updates live as options change.  
After a successful scan, an "Import results" button triggers the Import pipeline on the output directory.

---

## 8. Data Flow: Import Pipeline

```
User clicks "Scan Directory" in Import tab
		│
		▼
ZipScanner.ScanAsync(rootPath, recursive=true)
		│
		├─ Discover all *.zip files
		│
		▼
foreach zip: ZipParser.ParseAsync(zip)
		│
		├─ Read *.cai.model.<id>.json         → ModelDto
		├─ Read *.cai.model.<id>.v.<vid>.json → ModelVersionDto
		├─ List image entries                 → string[]
		└─ Compute ContentHash(modelJson + versionJson)
		│
		▼
ImportService.ImportAsync(ParseResult, zipPath)
		│
		├─ [ModelVersion not in DB] → full insert + primary ZipLocation
		├─ [Same version, same hash] → insert ZipLocation alias only
		└─ [Same version, changed hash] → insert new ModelVersion snapshot
										  + ZipLocation for new snapshot
```

---

## 9. Technology Stack Summary

| Concern | Choice | Rationale |
|---|---|---|
| UI Framework | AvaloniaUI 11 | Cross-platform XAML; WASM support; see ADR-001 |
| Target Framework | .NET 10 | LTS; latest ZIP APIs; see ADR-002 |
| MVVM | CommunityToolkit.Mvvm | Source generators, minimal boilerplate |
| Database | SQLite via Microsoft.Data.Sqlite | Embedded, zero-config, FTS5 built-in |
| ORM / Data access | Dapper + raw SQL | Full control over FTS5 queries |
| ZIP reading | System.IO.Compression (.NET 10) | Built-in, no extra deps; see ADR-002 |
| Image decode | Avalonia built-in Bitmap | Sufficient for JPEG/PNG/WEBP |
| DI Container | Microsoft.Extensions.DependencyInjection | Standard .NET pattern |
| Logging | Microsoft.Extensions.Logging + Serilog | File + console sink |
| Testing | xUnit + Moq + SQLite in-memory | Standard |

---

## 10. Deployment

### Desktop (Windows / Linux)
- Single-file self-contained publish: `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`
- Linux: `dotnet publish -r linux-x64 --self-contained`
- No installer required; portable executable

### Browser
- `dotnet publish` targeting `browser-wasm`
- Static files served from any HTTP server or GitHub Pages
- DB file loaded via `<input type="file">` or bundled as WASM asset
