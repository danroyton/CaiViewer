# ADR-005: MVVM Pattern and State Management
## Status: Accepted
## Date: 2025-08

---

## Context

The Viewer UI requires reactive data binding between:
- Search/filter state → result list
- Selected result → detail panel
- Scanner process output → log view
- Import progress → progress bar

Options considered:
- **ReactiveUI** (commonly used with Avalonia): powerful but complex; steep learning curve; heavyweight RxNET dependency
- **CommunityToolkit.Mvvm**: source-generator-based, minimal boilerplate, no RxNET; well-documented; works with any XAML framework
- **Hand-rolled INotifyPropertyChanged**: too much boilerplate

## Decision

Use **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`) throughout all ViewModels in `CaiViewer.Desktop` and `CaiViewer.Browser`.

Search/filter state is held in a single `BrowseViewModel` with a debounced `SearchQuery` property.  
Debouncing (300 ms) is implemented via `CancellationTokenSource` replacement in the property setter — no RxNET required.

Scanner output is streamed via `System.Threading.Channels.Channel<string>` from `ScannerService` to `ScannerViewModel`, which appends to an `ObservableCollection<string>` log.

## Consequences

- **Positive:** Source generators reduce boilerplate to near zero; no runtime reflection overhead.
- **Positive:** No RxNET dependency keeps WASM bundle small.
- **Negative:** Complex async data flows (e.g. cancellable FTS queries) require manual `CancellationToken` management rather than RxNET's built-in operators.
- **Mitigation:** A small `AsyncDebouncer<T>` helper class in `CaiViewer.Core` encapsulates the pattern.
