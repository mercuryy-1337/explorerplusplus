# ExplorerX Changelog

This changelog tracks the WinUI host line separately from the upstream Explorer++ history.

## 1.1.0.7 - 2026-05-02

### Added

- Added "Open in new tab" and "Open in new window" options in the right click context menu

### Changed

- Completely overhauled the right click context menu to be on par with the native windows context menu
- Modifed the background of the right click context menu
- The open button in the context menu now opens the selected folder within the app
- Made the tab colour the same as the rest of the app

### Fixed

- Double clicking a folder or file in the main window pane would not work if you clicked outside of the text area


### Packaging

- Updated the WinUI host version from 1.1.0.6 to 1.1.0.7.

## 1.1.0.6 - 2026-04-27

### Added

- Added a bottom info section below the folder pane and content area that shows the live item count for the current location.
- Added live selected-item status text that appears only when items are selected and updates the selected count plus combined size as selection changes.

### Changed

- Updated file browsing to use Explorer-style selection with double-click or Enter activation so the new info section can track selections without opening items on a single click.
- Updated selected-item summaries so any selection that includes folders now shows counts only and skips size calculation entirely.
- Updated folder listings to show a real folder icon immediately while native shell icons finish loading, instead of falling back to the outline glyph.
- Updated the Downloads quick-access icon path to honor the native Windows Downloads folder icon resource when it is defined through desktop.ini.
- Updated the WinUI file views so clicking empty space clears the current selection and dragging from empty space creates a marquee for multi-select.

### Fixed

- Fixed intermittent folder-pane navigation misses where a row could highlight on press without switching to the chosen folder when the tap gesture failed to resolve.

### Packaging

- Updated the WinUI host version from 1.1.0.5 to 1.1.0.6.

## 1.1.0.5 - 2026-04-27

### Changed

- Added a functional Explorer-style tab strip with selectable tabs, a persistent add-tab button, selected-tab close buttons, and short dividers between tabs.
- Updated the tab strip so every tab keeps a visible hoverable close button, and the add-tab button now has its own divider from the last tab.
- Lowered the add-tab button slightly for better vertical alignment and strengthened the light-mode hover/pressed feedback on the tab close and add buttons.
- Made the address bar editable so typed paths can be submitted with Enter, including environment-variable paths such as %LOCALAPPDATA% and %USERPROFILE%.
- Updated the This PC drive summary to switch from GB to TB when capacities reach four-digit gigabyte ranges.
- Split the folder-pane interaction zones so the strip before the icon stays dedicated to expansion with the normal cursor, while the icon-through-right-edge region keeps the hand cursor for navigation.

### Fixed

- Fixed the address bar text colors in dark mode so typed paths remain readable against the shell input background.
- Fixed the split folder-pane hit zones so hover, open, and expand/collapse interactions stay functional while keeping the separate arrow and hand cursor regions.
- Fixed tab switching so moving between This PC and folder tabs updates the correct grid/list layout immediately instead of carrying the previous tab's layout mode across tabs.

### Packaging

- Updated the WinUI host version from 1.1.0.4 to 1.1.0.5.

## 1.1.0.4 - 2026-04-27

### Added

- Added clickable file-list headers for Type, Modified, and Size with visible ascending and descending arrows.
- Added a clickable Name file-list header so the view can be sorted back by name without resetting the page.

### Changed

- Realigned the file-list header and detail rows so Type, Modified, and Size data sits directly under its matching column.
- Added header-only divider lines between the file-list detail columns while keeping the same column boundaries flowing through every row.
- Kept name sorting as the default file-list order and preserved the chosen column sort across refreshes.
- Updated file-list header hover and pressed feedback so each sortable column highlights as one full rectangular block between dividers instead of only around the label text.
- Changed This PC to use a native-style drive grid with a large icon on the left, the drive title beside it, a live usage bar in the middle row, and the free-space summary on the bottom row.
- Rounded the This PC drive free-space summary to two decimal places and switch the usage bar to red when free space drops to 10% or lower.
- Updated This PC drive tiles to use larger shell icons, letter-based drive ordering, hover-only highlighting without a permanent card background, and a slightly sharper usage bar profile.
- Removed the extra outer hover outline from This PC drive tiles so only the intended background highlight remains visible.
- Simplified the This PC tile container chrome so the hover state no longer draws a second outer frame, and vertically centered the drive icon block against the tile content.

### Fixed

- Fixed the WinUI host window icon path so Alt+Tab uses the ExplorerX app icon instead of falling back to a generic placeholder.
- Fixed a This PC navigation crash by replacing the drive usage ProgressBar template dependency with a custom usage bar that does not pull missing WinUI theme resources at runtime.

### Packaging

- Updated the WinUI host version from 1.1.0.3 to 1.1.0.4.

## 1.1.0.3 - 2026-04-27

### Fixed

- Reworked the folder-pane Properties action to use the shell data-object properties path first, with additional shell API fallbacks for stripped-down Windows environments where the plain properties verb is unreliable.
- Moved startup log writes off the UI thread so first-run creation of `startup.log` no longer stalls app launch.

### Packaging

- Updated the WinUI host version from 1.1.0.2 to 1.1.0.3.

## 1.1.0.2 - 2026-04-27

### Fixed

- Fixed the folder-pane Properties action so it invokes the Windows shell properties sheet reliably.
- Fixed submenu trigger styling so pressed and submenu-opened states now match the hover highlight instead of switching to a different color.
- Fixed context-menu highlight styling so item hover backgrounds use rounded corners that match the rest of the app.
- Fixed the custom context-menu surface so it uses a proper frosted acrylic background again instead of a flat semi-transparent fill.

### Changed

- Adjusted submenu trigger interaction so nested options behave more like hover-driven Explorer menus instead of latching onto click focus.

### Packaging

- Updated the WinUI host version from 1.1.0.1 to 1.1.0.2.

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