# ADR-001: UI Framework – AvaloniaUI
## Status: Accepted
## Date: 2025-08

---

## Context

The Viewer must run on **Windows**, **Linux**, and **Browser (WASM)** from a single .NET codebase.  
The primary alternative was WPF, which is Windows-only.  
The specification calls for virtualized lists, image rendering, custom theming, and a lightbox overlay.

Candidates evaluated:

| Framework | Windows | Linux | Browser | XAML | Maturity |
|---|---|---|---|---|---|
| **WPF** | ✓ | ✗ | ✗ | ✓ | Very high |
| **WinUI 3** | ✓ | ✗ | ✗ | ✓ | Medium |
| **MAUI Blazor Hybrid** | ✓ | ✓ | ✓ | ✗ (HTML) | Medium |
| **AvaloniaUI 11** | ✓ | ✓ | ✓ (WASM) | ✓ | High |
| **Uno Platform** | ✓ | ✓ | ✓ | ✓ | Medium |

## Decision

**AvaloniaUI 11** is chosen as the UI framework.

## Rationale

- Single XAML codebase runs natively on Windows and Linux **and** compiles to WebAssembly via Avalonia's browser target (same project, different entry point in `CaiViewer.Browser`).
- Supports `ItemsRepeater` / `VirtualizingPanel` for the virtualized results list.
- Native image rendering from streams (required for ZIP-embedded images).
- Active development, MIT license, large community, NuGet-available.
- MVVM with `CommunityToolkit.Mvvm` integrates cleanly.
- No dependency on Windows APIs; file pickers abstracted via `Avalonia.Platform.Storage.IStorageProvider`.

## Consequences

- **Positive:** One UI project compiles to all three targets; no UI duplication.
- **Positive:** HTML rendering for model descriptions achievable via `WebView` control or `HtmlLabel` community package.
- **Negative:** Browser target has no access to the local file system directly; the Import and Scanner tabs must be disabled or replaced with a file-upload flow in WASM mode. This is accepted and documented in the architecture.
- **Negative:** WASM bundle size is larger than a pure JS SPA; acceptable for a tool-oriented app.
- **Mitigation:** Platform capability flags (`IPlatformCapabilities`) injected at startup to enable/disable unavailable features per platform.
