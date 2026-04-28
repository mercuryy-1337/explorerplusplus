// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#pragma once

#include <optional>

class ColorProvider;

// Draws an entire tab control (the tabs, plus background) in the provided theme colors.
class ThemedTabControlPainter
{
public:
	ThemedTabControlPainter(HWND hwnd, bool darkMode);

	void SetHotItem(int hotItem);
	void ClearHotItem();
	void Paint(HDC hdc, const RECT &paintRect);

private:
	void DrawTab(int index, HDC hdc);
	RECT GetTabRect(int index);

	const HWND m_hwnd;
	const bool m_darkMode;
	std::optional<int> m_hotItem;
};
