#pragma once

#include "../Helper/SignalWrapper.h"
#include <boost/signals2.hpp>
#include <optional>
#include <string>
#include <vector>

class BrowserWindow;
class NavigationEvents;
class ShellBrowserEvents;
class Tab;
class TabEvents;
class TabList;

enum class BrowserTabLockState
{
	NotLocked,
	Locked,
	AddressLocked
};

// Represents the visible tab strip state for a single browser window, independent of any
// particular view implementation.
class BrowserTabStripModel
{
public:
	struct TabInfo
	{
		int id;
		std::wstring title;
		std::wstring tooltip;
		BrowserTabLockState lockState;
		bool selected;
	};

	SignalWrapper<BrowserTabStripModel, void()> updatedSignal;

	BrowserTabStripModel(const BrowserWindow *browser, const TabList *tabList, TabEvents *tabEvents,
		ShellBrowserEvents *shellBrowserEvents, NavigationEvents *navigationEvents);

	std::vector<TabInfo> GetTabs() const;
	std::optional<int> MaybeGetSelectedTabId() const;

private:
	void OnTabsChanged();
	TabInfo BuildTabInfo(const Tab *tab) const;

	const BrowserWindow *const m_browser;
	const TabList *const m_tabList;
	std::vector<boost::signals2::scoped_connection> m_connections;
};