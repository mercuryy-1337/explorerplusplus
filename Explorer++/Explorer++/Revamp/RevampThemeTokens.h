// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#pragma once

#include <windows.h>

namespace Revamp
{

struct ThemeColorTokens
{
	static constexpr COLORREF ShellBackgroundLight = RGB(0xF5, 0xF6, 0xF8);
	static constexpr COLORREF ShellSurfaceLight = RGB(0xFF, 0xFF, 0xFF);
	static constexpr COLORREF ShellChromeLight = RGB(0xF7, 0xF8, 0xFA);
	static constexpr COLORREF ShellTopBarLightBase = RGB(0xF7, 0xF8, 0xFA);
	static constexpr BYTE ShellTopBarLightAlpha = 0x99;
	static constexpr COLORREF ShellSidebarLight = RGB(0xF7, 0xF8, 0xFA);
	static constexpr COLORREF ShellInputBackgroundLight = RGB(0xFF, 0xFF, 0xFF);
	static constexpr COLORREF ShellButtonLight = RGB(0xF1, 0xF3, 0xF6);
	static constexpr COLORREF ShellNavButtonHoverLight = RGB(0xE7, 0xEB, 0xF0);
	static constexpr COLORREF ShellNavButtonPressedLight = RGB(0xDD, 0xE2, 0xE8);
	static constexpr COLORREF ShellTabActionHoverLight = RGB(0xD5, 0xDC, 0xE5);
	static constexpr COLORREF ShellTabActionPressedLight = RGB(0xC6, 0xCE, 0xD9);
	static constexpr COLORREF ShellBorderLight = RGB(0xE3, 0xE6, 0xEA);
	static constexpr COLORREF ShellTextLight = RGB(0x17, 0x18, 0x1A);
	static constexpr COLORREF ShellSecondaryTextLight = RGB(0x61, 0x65, 0x6F);
	static constexpr COLORREF ShellSidebarSectionLight = RGB(0x70, 0x75, 0x7F);
	static constexpr COLORREF ShellSelectionLight = RGB(0xDC, 0xEB, 0xFF);
	static constexpr COLORREF ShellAccentLight = RGB(0x0A, 0x64, 0xD6);
	static constexpr COLORREF ShellCriticalLight = RGB(0xC4, 0x2B, 0x1C);

	static constexpr COLORREF ShellBackgroundDark = RGB(0x17, 0x1A, 0x1F);
	static constexpr COLORREF ShellSurfaceDark = RGB(0x21, 0x25, 0x2B);
	static constexpr COLORREF ShellChromeDark = RGB(0x1B, 0x1F, 0x25);
	static constexpr COLORREF ShellTopBarDarkBase = RGB(0x1B, 0x1F, 0x25);
	static constexpr BYTE ShellTopBarDarkAlpha = 0x99;
	static constexpr COLORREF ShellSidebarDark = RGB(0x1B, 0x1F, 0x25);
	static constexpr COLORREF ShellInputBackgroundDark = RGB(0x15, 0x18, 0x1D);
	static constexpr COLORREF ShellButtonDark = RGB(0x2A, 0x30, 0x38);
	static constexpr COLORREF ShellNavButtonHoverDark = RGB(0x2A, 0x30, 0x38);
	static constexpr COLORREF ShellNavButtonPressedDark = RGB(0x36, 0x3D, 0x47);
	static constexpr COLORREF ShellTabActionHoverDark = RGB(0x2A, 0x30, 0x38);
	static constexpr COLORREF ShellTabActionPressedDark = RGB(0x36, 0x3D, 0x47);
	static constexpr COLORREF ShellBorderDark = RGB(0x36, 0x3C, 0x46);
	static constexpr COLORREF ShellTextDark = RGB(0xF3, 0xF5, 0xF7);
	static constexpr COLORREF ShellSecondaryTextDark = RGB(0xB6, 0xBD, 0xC8);
	static constexpr COLORREF ShellSidebarSectionDark = RGB(0x98, 0xA2, 0xAF);
	static constexpr COLORREF ShellSelectionDark = RGB(0x31, 0x50, 0x7C);
	static constexpr COLORREF ShellAccentDark = RGB(0x78, 0xB2, 0xFF);
	static constexpr COLORREF ShellCriticalDark = RGB(0xFF, 0x6B, 0x61);
};

struct ThemeMetricTokens
{
	static constexpr int TitleBarHeight = 44;
	static constexpr int TopLevelHorizontalMargin = 16;
	static constexpr int ContentColumnSpacing = 20;
	static constexpr int StandardSpacing = 8;
	static constexpr int LargeSpacing = 12;
	static constexpr int SidebarWidth = 300;
	static constexpr int TabHeight = 36;
	static constexpr int TabMinimumWidth = 168;
	static constexpr int SmallActionButtonSize = 28;
	static constexpr int InputMinimumHeight = 36;
	static constexpr int FolderPaneRowHeight = 36;
	static constexpr int ContainerCornerRadius = 12;
	static constexpr int TabCornerRadius = 10;
	static constexpr int InputCornerRadius = 8;
	static constexpr int SmallButtonCornerRadius = 6;
	static constexpr int SelectionBarWidth = 3;
	static constexpr int SelectionBarHeight = 16;
	static constexpr int SidebarHeaderTopMargin = 6;
	static constexpr int SidebarHeaderBottomMargin = 10;
	static constexpr int SidebarBaseIndent = 12;
	static constexpr int SidebarIndentStep = 18;
	static constexpr int ThisPcTileWidth = 352;
	static constexpr int ThisPcTileHeight = 96;
};

struct ThemeOpacityTokens
{
	static constexpr double Hidden = 0.0;
	static constexpr double Hovered = 0.55;
	static constexpr double Visible = 1.0;
};

[[nodiscard]] COLORREF BlendColor(COLORREF foreground, COLORREF background, BYTE alpha);
[[nodiscard]] COLORREF ResolveShellBackgroundColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellSurfaceColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellChromeColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellTopBarColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellInputBackgroundColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellButtonColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellButtonHoverColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellButtonPressedColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellBorderColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellTextColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellSecondaryTextColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellSelectionColor(bool darkMode);
[[nodiscard]] COLORREF ResolveShellAccentColor(bool darkMode);

} // namespace Revamp