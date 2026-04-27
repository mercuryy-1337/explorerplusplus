# Roadmap

## Principles

### Lightweight by default
- [x] Keep the first migration slice limited to tabs, navigation, file list, and the folders pane on the left.
- [x] Reuse existing Explorer++ models and services before adding new layers.
- [x] Keep release builds easy to run from the repo root.
- [x] Keep new migration code limited to seams that the WinUI 3 host will actually consume.

### Scope guardrails
- [x] The visible UI target remains WinUI 3.
- [x] Windows 11 is the target platform for the new shell.
- [ ] Keep the old Win32 shell working while the new WinUI 3 shell is being assembled.

## Phase 1: Migration Seam

### Tabs
- [x] Add a UI-neutral tab strip model for a single browser window.
- [ ] Bind a future WinUI 3 tab view to the tab strip model.
- [ ] Carry tab icons into the tab strip model once the WinUI host needs them.

### Navigation
- [x] Define a WinUI-facing navigation surface around the active `ShellBrowser` and `ShellNavigationController`.
- [x] Expose current path, back, forward, up, refresh, and pending navigation state without HWND ownership.
- [x] Keep address-bar behavior and navigation history aligned with the existing shell.

### File list
- [x] Reuse the existing `ListViewModel` as the base seam for the file list.
- [x] Add a first WinUI adapter for file-list columns, item rows, and column sorting.
- [ ] Keep the first WinUI file list focused on browsing and selection before secondary commands.

### Folders pane
- [x] Add a UI-neutral folders pane model around current location, This PC, and drives.
- [x] Seed a Files-inspired flat sidebar shape for the first WinUI host scaffold.
- [x] Add a reusable WinUI context-menu builder for left-pane item commands.
- [ ] Add a WinUI adapter for lazy expansion, selection, and folder activation.
- [ ] Keep shell-specific drag/drop and context-menu behavior behind adapters.

### Deliverables
- [x] Stand up the first WinUI 3 host project in the solution.
- [x] Add an isolated WinUI 3 host scaffold that can evolve without inheriting the native vcpkg build props.
- [ ] Render tabs, navigation, file list, and folders pane from reused Explorer++ backend state.
- [ ] Leave dialogs, menus, bookmarks, plugins, and full release automation outside the first slice unless they block the shell.

## Phase 2: WinUI 3 Host

### App shell
- [ ] Create a Windows 11-only WinUI 3 desktop shell with modern window chrome.
- [ ] Use WinUI 3 controls for the visible shell rather than hosting the old Win32 surfaces.
- [ ] Keep COM and shell integration behind adapters owned by the new shell.

### Runtime bridge
- [ ] Replace UI-thread assumptions that currently depend on hidden HWND dispatch with WinUI-friendly dispatch.
- [ ] Keep background COM STA work for shell operations where it is still needed.

## Packaging and Releases

### Packaging decision gate
- [ ] Validate whether a WinUI 3 MSIX-first release can stay simple and still satisfy the single-release-exe requirement.
- [ ] If MSIX cannot satisfy the single-exe requirement, keep the packaging choice open and prefer the release format that stays lightweight.
- [ ] Record the final packaging decision once the new host can build and ship.

### Local release builds
- [x] Keep a repo-root release script and batch wrapper.
- [x] Resolve MSBuild via `vswhere` before falling back to fixed Visual Studio paths.
- [ ] Extend the release build to include the new WinUI 3 host once it exists.
- [ ] Keep local release output easy to find in the top-level `release` folder.

### Tagged releases
- [x] On version tag push, create a release that includes source archives and one primary executable artifact.
- [x] Keep release automation lightweight and deterministic.
- [ ] Document the version-tag workflow once the first automated release path exists.

## Backlog

### Nice-to-have work that can be added later
- [ ] Define the Windows 11 visual language for spacing, materials, tabs, and navigation chrome.
- [ ] Port Files-style pinned sections, reorderable sidebar items, and compact/expanded sidebar modes once the native sidebar seam is live.
- [ ] Decide how much plugin compatibility the WinUI 3 shell needs in its first usable release.
- [ ] Rework localization once the packaging direction is final.
- [ ] Add future tasks here as the migration becomes clearer.