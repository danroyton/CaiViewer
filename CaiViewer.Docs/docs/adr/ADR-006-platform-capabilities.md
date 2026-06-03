# ADR-006: Platform Capability Abstraction
## Status: Accepted
## Date: 2025-08

---

## Context

Three runtime targets (Windows, Linux, Browser/WASM) have different capabilities:

| Capability | Windows | Linux | Browser |
|---|---|---|---|
| Local file system access | ✓ | ✓ | ✗ (File API only) |
| Recursive directory scan | ✓ | ✓ | ✗ |
| Launch child process (FilescannerCLI) | ✓ | ✓ | ✗ |
| Open folder in file manager | ✓ (Explorer) | ✓ (xdg-open) | ✗ |
| Clipboard write | ✓ | ✓ | ✓ (async) |
| Persistent settings (file) | ✓ | ✓ | LocalStorage only |

The UI must gracefully disable or replace unavailable features per platform rather than crash.

## Decision

Define an **`IPlatformCapabilities`** interface in `CaiViewer.Core` with boolean flags and optional helper methods:

```csharp
public interface IPlatformCapabilities
{
	bool CanAccessFileSystem { get; }
	bool CanLaunchProcess { get; }
	bool CanOpenFolderInExplorer { get; }
	bool IsWasm { get; }
	Task OpenFolderAsync(string path);
	Task<string?> PickFileAsync(string title, IEnumerable<FilePickerFileType> filters);
	Task<string?> PickFolderAsync(string title);
}
```

Implementations:
- `DesktopPlatformCapabilities` (Windows + Linux) — uses `Avalonia.Platform.Storage.IStorageProvider` + `Process.Start`
- `BrowserPlatformCapabilities` — uses Avalonia's browser storage API; `CanLaunchProcess = false`

Registered in DI at app startup. ViewModels inject `IPlatformCapabilities` and bind tab `IsVisible` / button `IsEnabled` to the capability flags.

## Consequences

- **Positive:** Platform differences are isolated to two small implementation classes; ViewModels stay platform-agnostic.
- **Positive:** Import and Scanner tabs are automatically hidden in WASM without any conditional compilation (`#if`).
- **Negative:** Slight added abstraction; developers must remember to add new capabilities to the interface.
- **Mitigation:** Interface kept intentionally thin; only capabilities that actually differ across platforms are included.
