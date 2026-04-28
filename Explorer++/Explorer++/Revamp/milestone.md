# Native Revamp Milestones

This folder tracks the native Explorer++ visual revamp that will replace the legacy shell while keeping the shipping executable small and self-contained.

## Goals

- Match the current WinUIHost look as closely as possible in native C++.
- Match the current WinUIHost behavior, not just its colors and spacing.
- Reuse the existing Explorer++ core for tabs, navigation, shell integration, commands, and settings.
- Preserve the lightweight native distribution target and avoid WinUI 3, .NET, WebView2, and other heavyweight runtime dependencies.

## Phase Status

### Phase 1 - Visual And Behavior Spec

Status: In progress

Deliverables:

- Freeze the WinUIHost design tokens, layout metrics, and interaction rules.
- Capture light and dark palettes, radii, spacing, icon rules, and hover or pressed states.
- Document parity requirements for tabs, sidebar, content view, footer, and context menus.

Current output:

- `phase-01-visual-spec.md`
- `RevampThemeTokens.h`
- `RevampThemeTokens.cpp`

Exit criteria:

- One native source of truth exists for the revamp palette and metrics.
- The phase-1 token scaffolding compiles in the Explorer++ native project.

### Phase 2 - Native Revamp Subtree And Theme Scaffolding

Status: In progress

Deliverables:

- Keep all revamp work under `Explorer++/Explorer++/Revamp`.
- Start compile-safe native scaffolding for colors, metrics, and helpers.
- Keep the legacy shell buildable while the revamp is assembled.

Exit criteria:

- The revamp subtree is present in the project.
- Native token helpers are available for later surfaces.

### Phase 3 - Revamp Window Host

Status: Not started

Deliverables:

- Add a top-level revamp host that can eventually replace the legacy shell window.
- Keep the host behind a controlled seam so the old shell can coexist during migration.
- Mirror the current WinUIHost title bar and top-level layout regions.

Exit criteria:

- A native revamp host window can be created without destabilizing the current app startup.

### Phase 4 - Core Surface Replacement

Status: Not started

Deliverables:

- Rebuild the title bar and tab strip.
- Rebuild the command row and address bar.
- Rebuild the sidebar, content view, and footer.
- Rebuild context menus and transient surfaces.

Exit criteria:

- The main visible shell surfaces exist in native form and follow the phase-1 spec.

### Phase 5 - Behavior Parity

Status: Not started

Deliverables:

- Match Explorer-style selection, whitespace deselection, and marquee selection.
- Match tab behavior, folder pane activation, sorting, and footer summaries.
- Match current hover, pressed, focus, and disabled states.

Exit criteria:

- Native revamp behavior is at parity with the current WinUIHost interactions on the main shell surfaces.

### Phase 6 - Cutover And Hardening

Status: Not started

Deliverables:

- Make the revamp shell the default shipping shell once parity is acceptable.
- Retire or freeze legacy visible shell code that is no longer needed.
- Keep release size and startup time inside the current native budget.

Exit criteria:

- The revamp shell is the primary native shell.
- The executable stays within the lightweight shipping target.

## Working Rules

- Keep phases additive and compile-safe.
- Validate after each phase before widening scope.
- Treat binary size as a release gate, not a nice-to-have.
- Prefer Windows-native APIs and project-local code over new dependencies.