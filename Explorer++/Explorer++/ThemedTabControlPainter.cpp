// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#include "stdafx.h"
#include "ThemedTabControlPainter.h"
#include "Revamp/RevampThemeTokens.h"
#include "../Helper/DpiCompatibility.h"
#include "../Helper/TabHelper.h"
#include "../Helper/WindowHelper.h"

namespace
{

void AddRoundedTopTabPath(Gdiplus::GraphicsPath &path, const RECT &rect, int radius)
{
	int diameter = radius * 2;

	path.AddArc(rect.left, rect.top, diameter, diameter, 180.0f, 90.0f);
	path.AddArc(rect.right - diameter, rect.top, diameter, diameter, 270.0f, 90.0f);
	path.AddLine(static_cast<INT>(rect.right), static_cast<INT>(rect.top + radius),
		static_cast<INT>(rect.right), static_cast<INT>(rect.bottom));
	path.AddLine(static_cast<INT>(rect.right), static_cast<INT>(rect.bottom),
		static_cast<INT>(rect.left), static_cast<INT>(rect.bottom));
	path.AddLine(static_cast<INT>(rect.left), static_cast<INT>(rect.bottom),
		static_cast<INT>(rect.left), static_cast<INT>(rect.top + radius));
	path.CloseFigure();
}

COLORREF GetTabBandColor(bool darkMode)
{
	return Revamp::ResolveShellChromeColor(darkMode);
}

COLORREF GetTabFillColor(bool darkMode, bool selected, bool hot)
{
	if (selected)
	{
		return Revamp::ResolveShellSurfaceColor(darkMode);
	}

	if (hot)
	{
		return Revamp::ResolveShellButtonHoverColor(darkMode);
	}

	return Revamp::ResolveShellChromeColor(darkMode);
}

COLORREF GetTabTextColor(bool darkMode, bool selected)
{
	return selected ? Revamp::ResolveShellTextColor(darkMode)
					 : Revamp::ResolveShellSecondaryTextColor(darkMode);
}

} // namespace

ThemedTabControlPainter::ThemedTabControlPainter(HWND hwnd, bool darkMode) :
	m_hwnd(hwnd),
	m_darkMode(darkMode)
{
}

void ThemedTabControlPainter::SetHotItem(int hotItem)
{
	if (hotItem < 0 || hotItem >= TabCtrl_GetItemCount(m_hwnd))
	{
		m_hotItem.reset();
		return;
	}

	m_hotItem = hotItem;
}

void ThemedTabControlPainter::ClearHotItem()
{
	m_hotItem.reset();
}

void ThemedTabControlPainter::Paint(HDC hdc, const RECT &paintRect)
{
	auto style = GetWindowLongPtr(m_hwnd, GWL_STYLE);

	// These styles aren't handled and it's not expected that they'll be set.
	CHECK(WI_AreAllFlagsClear(style, TCS_BUTTONS | TCS_VERTICAL));

	// Conversely, it's expected that these styles will always be set and there is no handling for
	// any case where they're not set.
	CHECK(WI_AreAllFlagsSet(style, TCS_FOCUSNEVER | TCS_SINGLELINE));

	// Fill in the background from the parent control. This is what the control does normally.
	HRESULT hr = DrawThemeParentBackground(m_hwnd, hdc, &paintRect);
	DCHECK(SUCCEEDED(hr));

	RECT clientRect;
	auto res = GetClientRect(m_hwnd, &clientRect);
	DCHECK(res);

	auto bandBrush = wil::unique_hbrush(CreateSolidBrush(GetTabBandColor(m_darkMode)));
	int fillRes = FillRect(hdc, &clientRect, bandBrush.get());
	DCHECK_NE(fillRes, 0);

	auto borderBrush = wil::unique_hbrush(CreateSolidBrush(Revamp::ResolveShellBorderColor(m_darkMode)));
	RECT bottomEdgeRect = { clientRect.left, clientRect.bottom - 1, clientRect.right,
		clientRect.bottom };
	int frameRes = FillRect(hdc, &bottomEdgeRect, borderBrush.get());
	DCHECK_NE(frameRes, 0);

	int modeRes = SetBkMode(hdc, TRANSPARENT);
	DCHECK_NE(modeRes, 0);

	auto font = reinterpret_cast<HFONT>(SendMessage(m_hwnd, WM_GETFONT, 0, 0));
	wil::unique_select_object selectFont;

	if (font)
	{
		selectFont = wil::SelectObject(hdc, font);
	}

	int numTabs = TabCtrl_GetItemCount(m_hwnd);

	for (int i = 0; i < numTabs; i++)
	{
		auto itemRect = GetTabRect(i);

		RECT intersectionRect;
		if (!IntersectRect(&intersectionRect, &paintRect, &itemRect))
		{
			continue;
		}

		DrawTab(i, hdc);
	}
}

void ThemedTabControlPainter::DrawTab(int index, HDC hdc)
{
	bool isHot = (index == m_hotItem);
	int selectedIndex = TabCtrl_GetCurSel(m_hwnd);
	bool isSelected = (index == selectedIndex);

	auto itemRect = GetTabRect(index);
	RECT backgroundRect = itemRect;
	int radius = DpiCompatibility::GetInstance().ScaleValue(m_hwnd,
		Revamp::ThemeMetricTokens::TabCornerRadius);

	if (isSelected)
	{
		// There's a bottom edge drawn directly underneath the tabs. The background for the selected
		// tab is drawn over that edge.
		backgroundRect.bottom += 1;
	}

	backgroundRect.left += DpiCompatibility::GetInstance().ScaleValue(m_hwnd, 4);
	backgroundRect.right -= DpiCompatibility::GetInstance().ScaleValue(m_hwnd, 4);

	Gdiplus::Graphics graphics(hdc);
	graphics.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);

	Gdiplus::Color fillColor;
	fillColor.SetFromCOLORREF(GetTabFillColor(m_darkMode, isSelected, isHot));
	Gdiplus::SolidBrush fillBrush(fillColor);
	Gdiplus::GraphicsPath path;
	AddRoundedTopTabPath(path, backgroundRect, radius);
	graphics.FillPath(&fillBrush, &path);

	Gdiplus::Color borderColor;
	borderColor.SetFromCOLORREF(Revamp::ResolveShellBorderColor(m_darkMode));
	Gdiplus::Pen borderPen(borderColor);
	Gdiplus::Status status;
	status = graphics.DrawPath(&borderPen, &path);
	DCHECK_EQ(status, Gdiplus::Ok);

	if (isSelected)
	{
		Gdiplus::Color accentColor;
		accentColor.SetFromCOLORREF(Revamp::ResolveShellAccentColor(m_darkMode));
		Gdiplus::Pen accentPen(accentColor,
			static_cast<Gdiplus::REAL>(DpiCompatibility::GetInstance().ScaleValue(m_hwnd,
				Revamp::ThemeMetricTokens::SelectionBarWidth)));
		status = graphics.DrawLine(&accentPen,
			static_cast<int>(backgroundRect.left) + radius,
			static_cast<int>(backgroundRect.bottom)
				- DpiCompatibility::GetInstance().ScaleValue(m_hwnd,
					Revamp::ThemeMetricTokens::SelectionBarWidth),
			static_cast<int>(backgroundRect.right) - radius,
			static_cast<int>(backgroundRect.bottom)
				- DpiCompatibility::GetInstance().ScaleValue(m_hwnd,
					Revamp::ThemeMetricTokens::SelectionBarWidth));
		DCHECK_EQ(status, Gdiplus::Ok);
	}

	auto text = TabHelper::GetItemText(m_hwnd, index);

	RECT textRect = { 0, 0, 0, 0 };
	auto textExtentRes =
		DrawText(hdc, text.c_str(), static_cast<int>(text.size()), &textRect, DT_CALCRECT);
	DCHECK_NE(textExtentRes, 0);

	auto imageList = TabCtrl_GetImageList(m_hwnd);

	TCITEM tcItem = {};
	tcItem.mask = TCIF_IMAGE;
	auto getItemRes = TabCtrl_GetItem(m_hwnd, index, &tcItem);
	CHECK(getItemRes);

	RECT drawRect = itemRect;

	if (imageList && tcItem.iImage != -1)
	{
		int iconWidth;
		int iconHeight;
		auto imageListSizeRes = ImageList_GetIconSize(imageList, &iconWidth, &iconHeight);
		CHECK(imageListSizeRes);

		// Although a TCM_SETPADDING message exists, there is no corresponding message to retrieve
		// the padding. Therefore, the padding will be calculated using the same method the control
		// uses. Note that this implicitly assumes that the padding hasn't been customized.
		int xPadding = GetSystemMetrics(SM_CXEDGE) * 3;
		DCHECK_NE(xPadding, 0);

		int contentWidth = iconWidth + xPadding + GetRectWidth(&textRect);
		POINT imageOrigin = { drawRect.left + (GetRectWidth(&drawRect) - contentWidth) / 2,
			drawRect.top + (GetRectHeight(&drawRect) - iconHeight) / 2 };
		auto drawRes =
			ImageList_Draw(imageList, tcItem.iImage, hdc, imageOrigin.x, imageOrigin.y, ILD_NORMAL);
		DCHECK(drawRes);

		drawRect.left += iconWidth + xPadding;
	}

	auto colorRes = SetTextColor(hdc, GetTabTextColor(m_darkMode, isSelected));
	DCHECK_NE(colorRes, CLR_INVALID);
	auto drawTextRes = DrawText(hdc, text.c_str(), static_cast<int>(text.size()), &drawRect,
		DT_CENTER | DT_VCENTER | DT_SINGLELINE);
	DCHECK_NE(drawTextRes, 0);
}

RECT ThemedTabControlPainter::GetTabRect(int index)
{
	RECT itemRect;
	auto res = TabCtrl_GetItemRect(m_hwnd, index, &itemRect);
	CHECK(res);

	bool isSelected = (index == TabCtrl_GetCurSel(m_hwnd));

	if (isSelected)
	{
		// Each tab in the control is at a slight vertical offset. The selected tab, however, will
		// align with the top of the viewport.
		itemRect.top = 0;
	}

	return itemRect;
}
