# ExplorerX Changelog

This changelog tracks the WinUI host line separately from the upstream Explorer++ history.

## 1.1.0.1 - 2026-04-27

### Added

- Added a reusable acrylic-styled WinUI context menu builder so the same right-click flyout pattern can be reused on future shell surfaces.
- Added a left folder-pane item context menu with Expand, Open in CMD, Copy as path, Send to, Copy, New, and Properties entries.

### Changed

- Updated the folder pane so the right-click menu only opens when a real item row is targeted.
- Updated the WinUIHost `.cs` and `.xaml` files to include the Explorer++ GPL header block.
- Refined the new folder-pane context menu with smaller text, wider symmetric side padding, and a background treatment that matches the app top bar more closely.

### Packaging

- Updated the WinUI host version from 1.1.0.0 to 1.1.0.1.

## 1.1.0.0 - 2026-04-27

### Added

- Introduced ExplorerX branding for the WinUI host while keeping the project rooted in Explorer++.
- Added a Windows 11-inspired WinUI shell with a custom title bar and integrated caption buttons.
- Added Explorer-style tabs and top chrome that more closely match the structure of modern File Explorer.
- Added a redesigned navigation row with back, forward, up, and refresh controls.
- Added breadcrumb-based address navigation with a click-to-path editing mode.
- Added a dedicated command bar above the file list for a more Explorer-like hierarchy.
- Added lazy loading for folder-pane expansion so large trees do not fully populate up front.
- Added native shell icons for drives, folders, and files throughout the WinUI host.
- Added an on-disk shell icon cache to avoid repeatedly extracting the same icons.
- Added a self-contained single-file x64 release path for the WinUI host.
- Added embedded application icon support for the WinUI executable and staged release output.

### Changed

- Refined the title bar, tabs, address bar, command bar, and content spacing to better match Windows 11 File Explorer.
- Tightened the command bar placement so it sits closer to the divider and top chrome.
- Updated navigation buttons with smaller sizing, clearer disabled states, and stronger hover and pressed feedback.
- Updated the selected tab styling so it blends into the surrounding chrome instead of reading like a separate card.
- Updated the address bar breadcrumbs so Home and filesystem paths read more naturally.
- Updated the top chrome to use stronger acrylic and mica-backed styling where the platform allows it.
- Updated the app to react live to system light and dark theme changes without requiring a restart.
- Updated folder-pane interactions so the main row keeps the hand cursor while the expand and collapse chevron reverts to the normal arrow cursor.

### Fixed

- Fixed a dark-mode address-box issue where focusing the directory path field could show an oversized bright rectangle.
- Fixed cursor affordance in the folder pane so the chevron hit target feels distinct from the row click target.
- Fixed several light and dark theme inconsistencies across the top chrome and navigation surfaces.

### Packaging

- Updated the WinUI host version from 1.0.0.0 to 1.1.0.0.
- Updated release automation so a push to the default branch can create the matching version tag automatically.
- Updated GitHub release publishing so the release uploads the WinUI host executable as explorerx.exe.
- GitHub source archives remain available automatically on each published release.