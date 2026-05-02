// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#include "stdafx.h"
#include "Revamp/RevampShellHost.h"
#include "Revamp/RevampThemeTokens.h"
#include "SystemFontHelper.h"
#include "../Helper/DpiCompatibility.h"
#include "../Helper/WindowHelper.h"
#include "../Helper/WindowSubclass.h"
#include <wil/resource.h>

namespace Revamp
{

namespace
{

void AddRoundedRectPath(Gdiplus::GraphicsPath &path, const RECT &rect, int radius)
{
	int diameter = radius * 2;

	path.AddArc(rect.left, rect.top, diameter, diameter, 180.0f, 90.0f);
	path.AddArc(rect.right - diameter, rect.top, diameter, diameter, 270.0f, 90.0f);
	path.AddArc(rect.right - diameter, rect.bottom - diameter, diameter, diameter, 0.0f, 90.0f);
	path.AddArc(rect.left, rect.bottom - diameter, diameter, diameter, 90.0f, 90.0f);
	path.CloseFigure();
}

HBRUSH GetTopBarBrush(bool darkMode)
{
	static const wil::unique_hbrush lightBrush(CreateSolidBrush(ResolveShellTopBarColor(false)));
	static const wil::unique_hbrush darkBrush(CreateSolidBrush(ResolveShellTopBarColor(true)));

	return darkMode ? darkBrush.get() : lightBrush.get();
}

HBRUSH GetChromeBrush(bool darkMode)
{
	static const wil::unique_hbrush lightBrush(CreateSolidBrush(ResolveShellChromeColor(false)));
	static const wil::unique_hbrush darkBrush(CreateSolidBrush(ResolveShellChromeColor(true)));

	return darkMode ? darkBrush.get() : lightBrush.get();
}

HBRUSH GetBorderBrush(bool darkMode)
{
	static const wil::unique_hbrush lightBrush(CreateSolidBrush(ResolveShellBorderColor(false)));
	static const wil::unique_hbrush darkBrush(CreateSolidBrush(ResolveShellBorderColor(true)));

	return darkMode ? darkBrush.get() : lightBrush.get();
}

bool IsDarkMode(HWND hwnd)
{
	BOOL darkMode = FALSE;
	DwmGetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(darkMode));
	return darkMode != FALSE;
}

} // namespace

std::unique_ptr<ShellHost> ShellHost::Create(HWND parent)
{
	return std::unique_ptr<ShellHost>(new ShellHost(parent));
}

ShellHost::ShellHost(HWND parent) :
	m_hwnd(CreateWindow(WC_STATIC, L"",
		WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN | SS_NOTIFY, 0, 0, 0, 0, parent,
		nullptr, GetModuleHandle(nullptr), nullptr)),
	m_parent(parent)
{
	CHECK(m_hwnd);

	m_windowSubclasses.push_back(
		std::make_unique<WindowSubclass>(m_hwnd, std::bind_front(&ShellHost::WndProc, this)));
	m_windowSubclasses.push_back(std::make_unique<WindowSubclass>(m_parent,
		std::bind_front(&ShellHost::ParentWndProc, this)));

	UpdateFonts();
	SetWindowPos(m_hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
}

HWND ShellHost::GetHWND() const
{
	return m_hwnd;
}

int ShellHost::GetTitleBarHeight() const
{
	return ScaleMetric(ThemeMetricTokens::TitleBarHeight);
}

void ShellHost::UpdateLayout(int width, int rebarHeight, bool showTopTabBand, int tabBandHeight)
{
	m_rebarHeight = rebarHeight;
	m_showTopTabBand = showTopTabBand;
	m_tabBandHeight = showTopTabBand ? tabBandHeight : 0;

	auto height = GetTitleBarHeight() + m_rebarHeight + m_tabBandHeight;
	SetWindowPos(m_hwnd, HWND_BOTTOM, 0, 0, width, height,
		SWP_NOACTIVATE | SWP_SHOWWINDOW);
	Invalidate();
}

void ShellHost::Invalidate()
{
	RedrawWindow(m_hwnd, nullptr, nullptr, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE);
}

void ShellHost::UpdateControlBounds(const RECT &toolbarRect, const RECT &addressBarRect)
{
	m_toolbarRect = toolbarRect;
	m_addressBarRect = addressBarRect;
	m_hasToolbarRect = !IsRectEmpty(&toolbarRect);
	m_hasAddressBarRect = !IsRectEmpty(&addressBarRect);
	Invalidate();
}

int ShellHost::ScaleMetric(int value) const
{
	return DpiCompatibility::GetInstance().ScaleValue(m_hwnd, value);
}

void ShellHost::UpdateFonts()
{
	auto titleFont = GetSystemFontScaledToWindow(SystemFont::Caption, m_hwnd);
	titleFont.lfWeight = FW_SEMIBOLD;
	m_titleFont.reset(CreateFontIndirect(&titleFont));

	auto captionFont = GetSystemFontScaledToWindow(SystemFont::SmallCaption, m_hwnd);
	m_captionFont.reset(CreateFontIndirect(&captionFont));
}

std::wstring ShellHost::GetFolderTitle() const
{
	auto title = GetWindowString(m_parent);
	auto separatorIndex = title.find(L" - ");

	if (separatorIndex != std::wstring::npos)
	{
		title.resize(separatorIndex);
	}

	if (title.empty())
	{
		title = L"Explorer++";
	}

	return title;
}

std::wstring ShellHost::GetAppTitle() const
{
	auto title = GetWindowString(m_parent);
	auto separatorIndex = title.find(L" - ");

	if (separatorIndex != std::wstring::npos)
	{
		return title.substr(separatorIndex + 3);
	}

	return L"Native Revamp";
}

void ShellHost::Paint(HDC hdc, const RECT &paintRect)
{
	UNREFERENCED_PARAMETER(paintRect);

	bool darkMode = IsDarkMode(m_parent);
	RECT clientRect;
	GetClientRect(m_hwnd, &clientRect);

	RECT titleRect = clientRect;
	titleRect.bottom = GetTitleBarHeight();
	FillRect(hdc, &titleRect, GetTopBarBrush(darkMode));

	RECT rebarRect = clientRect;
	rebarRect.top = titleRect.bottom;
	rebarRect.bottom = rebarRect.top + m_rebarHeight;
	if (rebarRect.bottom > rebarRect.top)
	{
		FillRect(hdc, &rebarRect, GetChromeBrush(darkMode));
	}

	RECT tabRect = clientRect;
	tabRect.top = rebarRect.bottom;
	if (tabRect.bottom > tabRect.top)
	{
		FillRect(hdc, &tabRect, GetChromeBrush(darkMode));
	}

	FrameRect(hdc, &clientRect, GetBorderBrush(darkMode));

	RECT separatorRect = { clientRect.left, titleRect.bottom - 1, clientRect.right,
		titleRect.bottom };
	FillRect(hdc, &separatorRect, GetBorderBrush(darkMode));

	if (m_rebarHeight > 0)
	{
		separatorRect = { clientRect.left, rebarRect.bottom - 1, clientRect.right,
			rebarRect.bottom };
		FillRect(hdc, &separatorRect, GetBorderBrush(darkMode));
	}

	int horizontalMargin = ScaleMetric(ThemeMetricTokens::TopLevelHorizontalMargin);
	int accentWidth = ScaleMetric(ThemeMetricTokens::SelectionBarWidth);
	int accentHeight = ScaleMetric(ThemeMetricTokens::SelectionBarHeight);
	RECT accentRect = { horizontalMargin,
		titleRect.top + ((GetRectHeight(&titleRect) - accentHeight) / 2),
		horizontalMargin + accentWidth,
		titleRect.top + ((GetRectHeight(&titleRect) + accentHeight) / 2) };
	auto accentBrush = wil::unique_hbrush(CreateSolidBrush(ResolveShellAccentColor(darkMode)));
	FillRect(hdc, &accentRect, accentBrush.get());

	RECT folderTextRect = titleRect;
	folderTextRect.left = accentRect.right + ScaleMetric(ThemeMetricTokens::LargeSpacing);
	folderTextRect.right -= horizontalMargin + ScaleMetric(ThemeMetricTokens::SidebarBaseIndent);

	auto folderTitle = GetFolderTitle();
	auto appTitle = GetAppTitle();

	auto textColor = SetTextColor(hdc, ResolveShellTextColor(darkMode));
	DCHECK_NE(textColor, CLR_INVALID);
	auto modeRes = SetBkMode(hdc, TRANSPARENT);
	DCHECK_NE(modeRes, 0);

	wil::unique_select_object selectTitleFont;
	if (m_titleFont)
	{
		selectTitleFont = wil::SelectObject(hdc, m_titleFont.get());
	}

	DrawText(hdc, folderTitle.c_str(), static_cast<int>(folderTitle.size()), &folderTextRect,
		DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);

	RECT appTitleRect = titleRect;
	appTitleRect.right -= horizontalMargin;
	appTitleRect.left = std::max(folderTextRect.left, clientRect.right - ScaleMetric(240));

	wil::unique_select_object selectCaptionFont;
	if (m_captionFont)
	{
		selectCaptionFont = wil::SelectObject(hdc, m_captionFont.get());
	}

	textColor = SetTextColor(hdc, ResolveShellSecondaryTextColor(darkMode));
	DCHECK_NE(textColor, CLR_INVALID);
	DrawText(hdc, appTitle.c_str(), static_cast<int>(appTitle.size()), &appTitleRect,
		DT_RIGHT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);

	Gdiplus::Graphics graphics(hdc);
	graphics.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);

	auto drawCapsule = [&](const RECT &baseRect, COLORREF fillColor, COLORREF borderColor,
		int radius, int horizontalPadding, int verticalPadding)
	{
		RECT capsuleRect = baseRect;
		InflateRect(&capsuleRect, horizontalPadding, verticalPadding);

		Gdiplus::GraphicsPath path;
		AddRoundedRectPath(path, capsuleRect, radius);

		Gdiplus::Color gdipFillColor;
		gdipFillColor.SetFromCOLORREF(fillColor);
		Gdiplus::SolidBrush fillBrush(gdipFillColor);
		graphics.FillPath(&fillBrush, &path);

		Gdiplus::Color gdipBorderColor;
		gdipBorderColor.SetFromCOLORREF(borderColor);
		Gdiplus::Pen borderPen(gdipBorderColor);
		graphics.DrawPath(&borderPen, &path);
	};

	int capsuleRadius = ScaleMetric(ThemeMetricTokens::InputCornerRadius);
	if (m_hasToolbarRect)
	{
		drawCapsule(m_toolbarRect,
			BlendColor(ResolveShellSurfaceColor(darkMode), ResolveShellChromeColor(darkMode),
				darkMode ? 0x55 : 0xCC),
			ResolveShellBorderColor(darkMode), capsuleRadius,
			ScaleMetric(ThemeMetricTokens::StandardSpacing),
			ScaleMetric(ThemeMetricTokens::StandardSpacing) / 2);
	}

	if (m_hasAddressBarRect)
	{
		drawCapsule(m_addressBarRect, ResolveShellInputBackgroundColor(darkMode),
			ResolveShellBorderColor(darkMode), capsuleRadius,
			ScaleMetric(ThemeMetricTokens::StandardSpacing),
			ScaleMetric(ThemeMetricTokens::StandardSpacing) / 2);
	}
}

void ShellHost::BeginWindowDrag(const POINT &pt) const
{
	POINT screenPoint = pt;
	ClientToScreen(m_hwnd, &screenPoint);
	ReleaseCapture();
	SendMessage(m_parent, WM_NCLBUTTONDOWN, HTCAPTION,
		MAKELPARAM(screenPoint.x, screenPoint.y));
}

LRESULT ShellHost::WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(hwnd);

	switch (msg)
	{
	case WM_ERASEBKGND:
		return 1;

	case WM_PAINT:
	case WM_PRINTCLIENT:
	{
		if (msg == WM_PAINT)
		{
			PAINTSTRUCT ps;
			auto hdc = BeginPaint(m_hwnd, &ps);
			Paint(hdc, ps.rcPaint);
			EndPaint(m_hwnd, &ps);
		}
		else
		{
			auto hdc = reinterpret_cast<HDC>(wParam);
			RECT clientRect;
			GetClientRect(m_hwnd, &clientRect);
			Paint(hdc, clientRect);
		}

		return 0;
	}

	case WM_LBUTTONDOWN:
	{
		POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
		if (pt.y < GetTitleBarHeight())
		{
			BeginWindowDrag(pt);
			return 0;
		}
	}
	break;

	case WM_LBUTTONDBLCLK:
	{
		POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
		if (pt.y < GetTitleBarHeight())
		{
			SendMessage(m_parent, WM_SYSCOMMAND, IsZoomed(m_parent) ? SC_RESTORE : SC_MAXIMIZE, 0);
			return 0;
		}
	}
	break;

	case WM_DPICHANGED_AFTERPARENT:
		UpdateFonts();
		Invalidate();
		break;

	case WM_NCDESTROY:
		return 0;
	}

	return DefSubclassProc(m_hwnd, msg, wParam, lParam);
}

LRESULT ShellHost::ParentWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(hwnd);
	UNREFERENCED_PARAMETER(wParam);
	UNREFERENCED_PARAMETER(lParam);

	if (msg == WM_SETTEXT)
	{
		Invalidate();
	}

	return DefSubclassProc(m_parent, msg, wParam, lParam);
}

} // namespace Revamp