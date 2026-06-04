# CaiViewer

A **cross-platform offline viewer** for CivitAI model archives produced by **FilescannerCLI**.  
Built with **.NET 10**, **AvaloniaUI 11**, and **SQLite/FTS5**.

---

## Overview

CaiViewer reads a local SQLite database (`cai.db`) that is populated from a directory tree of ZIP archives created by FilescannerCLI.  
It provides fast full-text search, structured filtering, a detailed model viewer, an import pipeline, and a GUI front-end for running FilescannerCLI itself.

```
User ──► CaiViewer ──► cai.db (SQLite)
				   ──► ~/cai-repo/**/*.zip
				   ──► FilescannerCLI.exe (optional, Scanner tab)
```

---

## Solution Structure

| Project | Description |
|---|---|
| `CaiViewer.Core` | Business logic: database access, import pipeline, search, scanner |
| `CaiViewer.Frontend` | Shared AvaloniaUI views + MVVM view-models |
| `CaiViewer.Frontend.Desktop` | Windows / Linux desktop entry point |
| `CaiViewer.Frontend.Browser` | WebAssembly browser entry point |
| `CaiViewer.Tests` | xUnit unit tests for Core |
| `CaiViewer.Docs` | Architecture and specification documents |

---

## Features

### 📷 Browse Tab
- Full-text search across model names, descriptions, version names, trigger words, and image prompts (SQLite FTS5)
- Filters: model type, base model, NSFW level, tags (AND/OR), creator, date range, ZIP present, availability
- Sort by name, date, downloads, rating, NSFW level
- Paginated results list
- Detail panel with version tabs, file table, trigger words, hashes, stats
- Image gallery loaded directly from local ZIP archives

### 📥 Import Tab
- Recursively scan a directory tree of ZIP archives
- Deduplication and snapshot versioning (no data loss on re-import)
- Live progress log + summary (inserted / updated / aliases / skipped / failed)

### ⚡ Scanner Tab
Full GUI front-end for **FilescannerCLI** based on `SPEC_OPERATING_MODES`:

| Section | Controls |
|---|---|
| Mode | File/Hash Scan (`-S/-A`), URL/ID Scan (`-U`), Inspect SafeTensor (`-I`) |
| Input | File/list path picker + input-type selector |
| Options | All flag switches (`-z -a -f -i -h -u -d -N -p -t -c -E -C`) |
| Parameters | `CCD:` path, `DLL:` size, `APIKEY:`, `WORKDIR:` |
| Preview | Live command-line preview, copyable to clipboard |
| Run | Streaming log output, cancellation, exit-code display |

### ⚙ Settings Tab
- Database path, CivitAI repo root, FilescannerCLI executable path
- Default NSFW level, thumbnail size, blurhash placeholder, network fallback toggle

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A local copy of **FilescannerCLI.exe** (for the Scanner tab)
- A `cai.db` SQLite database, or use the Import tab to create one

### Build

```powershell
git clone https://github.com/danroyton/CaiViewer
cd CaiViewer
dotnet build CaiViewer.slnx
```

### Run (Desktop)

```powershell
dotnet run --project CaiViewer.Frontend/CaiViewer.Frontend.Desktop
```

### First-time Setup

1. Open the **Settings** tab.
2. Set the **Database path** (e.g. `%USERPROFILE%\cai-repo\cai.db`).
3. Set the **Central CivitAI Repository Path** – the root folder containing all ZIP archives.
4. Optionally set the **FilescannerCLI executable** path if you want to run scans from the app.
5. Click **Save Settings**.

### Import Archives

1. Open the **Import** tab.
2. Enter the root directory of your ZIP archives.
3. Click **Scan Directory** – all archives are parsed and imported into `cai.db`.

---

## Architecture

See [`CaiViewer.Docs/docs/ARCHITECTURE.md`](CaiViewer.Docs/docs/ARCHITECTURE.md) for the full architecture document.

### Key Data Flow

```
ZipScanner.ScanAsync(rootPath)
  └─ ZipParser.ParseAsync(zipPath)        → RootModel + RootModelVersion DTOs
  └─ ImportService.ImportAsync(result)    → SQLite upsert with deduplication
	   └─ ContentHasher.Compute()         → SHA-256 fingerprint for change detection
	   └─ ZipLocationRepository           → relative ZIP path aliases
```

### Database

SQLite with FTS5 virtual tables. Schema lives in `CaiViewer.Core/Database/Schema.cs`.

Main tables: `Model`, `ModelVersion`, `ModelVersionFile`, `ModelVersionImage`,  
`ModelVersionTrainedWord`, `ZipLocation`, `Creator`, `Tag`, `ModelTag`.

---

## ZIP Archive Format

Archives are produced by FilescannerCLI and follow this naming convention:

```
<ModelName>_<ModelVersionName>_<ModelVersionId>.zip
```

Each archive contains:
- `*.cai.model.<id>.json` – full model metadata
- `*.cai.model.<id>.v.<versionId>.json` – full version metadata
- `*_<index>_<imageId>.<ext>` – preview images
- `*_preview.webp` – stitched preview (optional)

See [`CaiViewer.Docs/docs/SPEC_ZIP_DATAMODEL.md`](CaiViewer.Docs/docs/SPEC_ZIP_DATAMODEL.md) for the complete specification.

---

## FilescannerCLI Modes (Scanner Tab)

| Mode | Switch | Description |
|---|---|---|
| File/Hash Scan | `-S` | Scan local `.safetensors` by hash lookup |
| AutoV2 Direct | `-A<hash>:` | Look up by AutoV2/V3 hash directly |
| URL/ID Lookup | `-U<id>:` | Look up by model-version ID or CivitAI URL |
| Inspect | `-I[hmi]` | Read safetensor header/metadata (no API call) |

Common flags: `-z` (ZIP), `-a` (all versions), `-u` (update), `-N` (fast-fail), `-d` (debug).

Named parameters: `CCD:<path>`, `DLL:<MB>`, `APIKEY:<token>`, `WORKDIR:<path>`.

See [`CaiViewer.Docs/docs/SPEC_OPERATING_MODES.md`](CaiViewer.Docs/docs/SPEC_OPERATING_MODES.md) for the full specification.

---

## Browser / WASM

The browser build (`CaiViewer.Frontend.Browser`) runs as a WebAssembly app. Limitations:
- Import tab disabled (no direct filesystem access)
- Scanner tab disabled (cannot launch child processes)
- Database loaded via file upload dialog

---

## License

MIT
