// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#include "stdafx.h"
#include "Revamp/RevampThemeTokens.h"

namespace Revamp
{

COLORREF BlendColor(COLORREF foreground, COLORREF background, BYTE alpha)
{
	auto blendChannel = [alpha](BYTE foregroundChannel, BYTE backgroundChannel)
	{
		return static_cast<BYTE>(
			((foregroundChannel * alpha) + (backgroundChannel * (0xFF - alpha))) / 0xFF);
	};

	return RGB(blendChannel(GetRValue(foreground), GetRValue(background)),
		blendChannel(GetGValue(foreground), GetGValue(background)),
		blendChannel(GetBValue(foreground), GetBValue(background)));
}

COLORREF ResolveShellBackgroundColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellBackgroundDark : ThemeColorTokens::ShellBackgroundLight;
}

COLORREF ResolveShellSurfaceColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellSurfaceDark : ThemeColorTokens::ShellSurfaceLight;
}

COLORREF ResolveShellChromeColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellChromeDark : ThemeColorTokens::ShellChromeLight;
}

COLORREF ResolveShellTopBarColor(bool darkMode)
{
	if (darkMode)
	{
		return BlendColor(ThemeColorTokens::ShellTopBarDarkBase,
			ThemeColorTokens::ShellBackgroundDark, ThemeColorTokens::ShellTopBarDarkAlpha);
	}

	return BlendColor(ThemeColorTokens::ShellTopBarLightBase,
		ThemeColorTokens::ShellBackgroundLight, ThemeColorTokens::ShellTopBarLightAlpha);
}

COLORREF ResolveShellInputBackgroundColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellInputBackgroundDark
						: ThemeColorTokens::ShellInputBackgroundLight;
}

COLORREF ResolveShellButtonColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellButtonDark : ThemeColorTokens::ShellButtonLight;
}

COLORREF ResolveShellButtonHoverColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellNavButtonHoverDark
						: ThemeColorTokens::ShellNavButtonHoverLight;
}

COLORREF ResolveShellButtonPressedColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellNavButtonPressedDark
						: ThemeColorTokens::ShellNavButtonPressedLight;
}

COLORREF ResolveShellBorderColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellBorderDark : ThemeColorTokens::ShellBorderLight;
}

COLORREF ResolveShellTextColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellTextDark : ThemeColorTokens::ShellTextLight;
}

COLORREF ResolveShellSecondaryTextColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellSecondaryTextDark
						: ThemeColorTokens::ShellSecondaryTextLight;
}

COLORREF ResolveShellSelectionColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellSelectionDark : ThemeColorTokens::ShellSelectionLight;
}

COLORREF ResolveShellAccentColor(bool darkMode)
{
	return darkMode ? ThemeColorTokens::ShellAccentDark : ThemeColorTokens::ShellAccentLight;
}

} // namespace Revamp