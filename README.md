# SecurityLendingTool

.NET 8 WPF desktop app demonstrating a `DataGrid` with **cell-unit selection** bound to an `ObservableCollection<Person>`.

## Requirements

- Windows
- .NET 8 SDK

## Build & run

```bash
dotnet build
dotnet run
```

## DataGrid selection

- `SelectionUnit="Cell"` — selects individual cells instead of whole rows
- `SelectionMode="Extended"` — allows multi-cell selection with Ctrl/Shift
- F3 / F4 — move cell selection down / up one row (wraps at ends)
- Numeric selection stats (Count / Average / Sum) shown bottom-right

## Project layout

- `Models/Person.cs` — row model
- `ViewModels/MainViewModel.cs` — sample `People` collection (CommunityToolkit.Mvvm)
- `Views/DemoOneView.xaml` — Demo One tab content
- `Behaviors/` — reusable DataGrid attached behaviors
- `MainWindow.xaml` — tab host window
