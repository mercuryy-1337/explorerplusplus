# Phase 1 Visual And Behavior Spec

This document freezes the first native design baseline for the revamp. The WinUIHost remains the source of truth for how the native revamp should look and behave.

## Source Of Truth

- `WinUIHost/App.xaml`
- `WinUIHost/MainWindow.xaml`
- `WinUIHost/MainWindow.xaml.cs`

## Visual Direction

The target is the same modern Windows 11 style currently shown by the WinUIHost:

- Semi-transparent top chrome with acrylic-first and mica-fallback behavior.
- Rounded containers and buttons.
- High-contrast light and dark palettes with restrained borders.
- Segoe Fluent Icons for toolbar, tabs, sidebar, and system actions.
- Clean spacing and low-noise status surfaces.

## Color Tokens

### Light theme

- Shell background: `#F5F6F8`
- Shell surface: `#FFFFFF`
- Shell chrome: `#F7F8FA`
- Shell top bar base: `#F7F8FA` with alpha `#99`
- Shell sidebar: `#F7F8FA`
- Shell input background: `#FFFFFF`
- Shell button: `#F1F3F6`
- Nav button hover: `#E7EBF0`
- Nav button pressed: `#DDE2E8`
- Tab action hover: `#D5DCE5`
- Tab action pressed: `#C6CED9`
- Border: `#E3E6EA`
- Primary text: `#17181A`
- Secondary text: `#61656F`
- Sidebar section text: `#70757F`
- Selection fill: `#DCEBFF`
- Accent: `#0A64D6`
- Critical: `#C42B1C`

### Dark theme

- Shell background: `#171A1F`
- Shell surface: `#21252B`
- Shell chrome: `#1B1F25`
- Shell top bar base: `#1B1F25` with alpha `#99`
- Shell sidebar: `#1B1F25`
- Shell input background: `#15181D`
- Shell button: `#2A3038`
- Nav button hover: `#2A3038`
- Nav button pressed: `#363D47`
- Tab action hover: `#2A3038`
- Tab action pressed: `#363D47`
- Border: `#363C46`
- Primary text: `#F3F5F7`
- Secondary text: `#B6BDC8`
- Sidebar section text: `#98A2AF`
- Selection fill: `#31507C`
- Accent: `#78B2FF`
- Critical: `#FF6B61`

## Layout Metrics

- Title bar height: `44`
- Top-level horizontal margin: `16`
- Content column spacing: `20`
- Standard row spacing: `8` to `12`
- Sidebar width: `300`
- Tab height: `36`
- Tab min width: `168`
- Small action button size: `28`
- Input minimum height: `36`
- Folder pane row height: `36`
- Container corner radius: `12`
- Tab corner radius: `10`
- Input corner radius: `8`
- Small button corner radius: `6`
- Selection bar width: `3`
- This PC tile size: `352 x 96`

## Surface Inventory

### Window frame and title bar

- Semi-transparent top region with a bottom divider.
- Custom tab strip integrated into the title bar.
- App icon, tab close buttons, add-tab button, and selected-tab fill state.
- Caption buttons must visually harmonize with the custom top bar.

### Command row and address bar

- Back, forward, up, and refresh buttons.
- Rounded address container with breadcrumb mode and edit mode.
- Shared spacing and hover rules across buttons.

### Sidebar

- Section headers with muted labels and divider lines.
- Row selection background with rounded corners.
- Thin accent bar on the selected row.
- Separate expand and open hit zones.
- Native shell icon first, glyph fallback second.

### Content area

- Folder view list with headers and sort indicators.
- This PC tile view with large icons and usage bars.
- Selection states that align with Explorer behavior.

### Footer

- Divider line above the footer.
- Item count on the left.
- Optional selection summary when selection is present.

### Context menus

- Same visual tone as the WinUIHost menus.
- Rounded, light-weight, frosted look.
- Strong hover readability in light and dark themes.

## Behavior Parity Requirements

- Single click selects in the content area.
- Double click or Enter activates the selected item.
- Clicking empty content whitespace clears selection.
- Dragging from whitespace starts a marquee selection.
- Folder pane rows preserve the current split expand or navigate behavior.
- Mixed file and folder selections suppress size aggregation.
- Footer state follows the same item-count and selection-summary rules as the current WinUIHost.

## Native Implementation Constraints

- Stay inside the native Explorer++ executable.
- Prefer DWM system backdrop, transparent painting, and custom drawing.
- Prefer Segoe Fluent Icons and existing shell icon flows over bundled image packs.
- Avoid WinUI 3, .NET, WebView2, and new heavyweight UI frameworks.

## Phase 1 Completion Target

Phase 1 is complete when the revamp has a stable token baseline, a stable surface inventory, and a stable interaction checklist that can guide native implementation without going back to re-decide the look on every new surface.