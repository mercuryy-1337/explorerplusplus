# WinUI Host Scaffold

This project is the first isolated WinUI 3 shell scaffold for the migration.

- It lives under its own `Directory.Build.props` and `Directory.Build.targets` so the repo-root vcpkg imports don't flow into the C# project.
- It currently mirrors the shape of the native seams with design-time state, rather than a live native interop bridge.
- The intended runtime inputs are `BrowserTabStripModel`, `BrowserNavigationModel`, and `BrowserFoldersPaneModel`, followed by a file-list adapter.

Local build:

```powershell
dotnet build .\WinUIHost\ExplorerPlusPlus.WinUIHost.csproj -p:Platform=x64
```