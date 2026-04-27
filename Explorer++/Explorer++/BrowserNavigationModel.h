// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#pragma once

#include "../Helper/SignalWrapper.h"
#include <boost/signals2.hpp>
#include <string>
#include <vector>

class BrowserWindow;
class HistoryEntry;
class NavigationEvents;
class ShellBrowserEvents;
class TabEvents;

class BrowserNavigationModel
{
public:
	struct HistoryEntryInfo
	{
		int id;
		std::wstring title;
		std::wstring path;
	};

	struct NavigationState
	{
		std::wstring currentLocationTitle;
		std::wstring currentLocationPath;
		std::wstring currentLocationParsingPath;
		bool canGoBack = false;
		bool canGoForward = false;
		bool canGoUp = false;
		bool canRefresh = false;
		bool isNavigating = false;
		std::vector<HistoryEntryInfo> backHistory;
		std::vector<HistoryEntryInfo> forwardHistory;
	};

	SignalWrapper<BrowserNavigationModel, void()> updatedSignal;

	BrowserNavigationModel(const BrowserWindow *browser, TabEvents *tabEvents,
		ShellBrowserEvents *shellBrowserEvents, NavigationEvents *navigationEvents);

	NavigationState GetState() const;

private:
	void OnNavigationStateChanged();
	HistoryEntryInfo BuildHistoryEntryInfo(const HistoryEntry *entry) const;

	const BrowserWindow *const m_browser;
	std::vector<boost::signals2::scoped_connection> m_connections;
};