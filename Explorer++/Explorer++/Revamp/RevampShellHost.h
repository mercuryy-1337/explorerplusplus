// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#pragma once

#include <memory>

class WindowSubclass;

namespace Revamp
{

class ShellHost
{
public:
	static std::unique_ptr<ShellHost> Create(HWND parent);

	~ShellHost() = default;

	HWND GetHWND() const;
	int GetTitleBarHeight() const;
	void UpdateLayout(int width, int rebarHeight, bool showTopTabBand, int tabBandHeight);
	void UpdateControlBounds(const RECT &toolbarRect, const RECT &addressBarRect);
	void Invalidate();

private:
	explicit ShellHost(HWND parent);

	int ScaleMetric(int value) const;
	void UpdateFonts();
	std::wstring GetFolderTitle() const;
	std::wstring GetAppTitle() const;
	void Paint(HDC hdc, const RECT &paintRect);
	void BeginWindowDrag(const POINT &pt) const;

	LRESULT WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
	LRESULT ParentWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

	const HWND m_hwnd;
	const HWND m_parent;
	int m_rebarHeight = 0;
	int m_tabBandHeight = 0;
	bool m_showTopTabBand = false;
	RECT m_toolbarRect = {};
	RECT m_addressBarRect = {};
	bool m_hasToolbarRect = false;
	bool m_hasAddressBarRect = false;
	wil::unique_hfont m_titleFont;
	wil::unique_hfont m_captionFont;
	std::vector<std::unique_ptr<WindowSubclass>> m_windowSubclasses;
};

} // namespace Revamp