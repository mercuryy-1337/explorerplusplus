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

} // namespace Revamp