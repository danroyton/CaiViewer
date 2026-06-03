# ADR-002: Target Framework and ZIP API Strategy – .NET 10
## Status: Accepted
## Date: 2025-08

---

## Context

FilescannerCLI currently targets .NET 8. The Viewer is a new project and can target any current .NET version.  
.NET 10 is the next LTS release and introduces improvements relevant to this application:

- **`System.IO.Compression` enhancements** in .NET 9/10:
  - `ZipArchive` gains support for extracting to a `Stream` without temp files
  - New overloads for `ZipFile.Open` with `CompressionLevel` control
  - `ZipArchiveEntry.ExternalAttributes` cross-platform support
- **Performance improvements** to `Span<T>`-based ZIP reading reduce memory allocation when scanning large archives
- **`System.IO.Compression.ZipArchive`** in .NET 10 supports the `Deflate64` decompression algorithm natively (previously required a third-party package)
- `ReadExactly` / `ReadAtLeast` stream helpers simplify image byte extraction

ZIP algorithms used in the archives produced by FilescannerCLI:

| Content | Compression | Rationale |
|---|---|---|
| JSON metadata files | `Deflate` (default) | Small text, high compression ratio |
| JPEG / WEBP images | `Stored` (no compression) | Already compressed binary; re-compressing wastes CPU |
| `.txt` files | `Deflate` | Small |

## Decision

- Target **net10.0** for all new projects (`CaiViewer.Core`, `CaiViewer.Desktop`, `CaiViewer.Browser`).
- Use **`System.IO.Compression.ZipFile` / `ZipArchive`** from the BCL exclusively. No third-party ZIP library.
- `ZipArchiveMode.Read` only during Import and image loading (never modify source ZIPs).
- Image entries are read via `ZipArchiveEntry.Open()` streamed directly to `Avalonia.Media.Imaging.Bitmap(Stream)` — no temp file on disk.
- For content-hash computation during deduplication, use `System.Security.Cryptography.SHA256.HashData(ReadOnlySpan<byte>)` (.NET 5+ static API).

## Consequences

- **Positive:** No extra NuGet dependencies for ZIP handling.
- **Positive:** Deflate64 support means even older archives with non-standard compression open correctly.
- **Positive:** Stream-based image decode avoids temporary files and reduces I/O.
- **Negative:** .NET 10 may be in RC at project start; use a stable RC build for development, upgrade to RTM on release.
- **Mitigation:** Core library uses only stable BCL APIs; no preview features required.
