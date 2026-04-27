#include "stdafx.h"
#include "BrowserTabStripModel.h"
#include "BrowserWindow.h"
#include "ShellBrowser/NavigationEvents.h"
#include "ShellBrowser/ShellBrowser.h"
#include "ShellBrowser/ShellBrowserEvents.h"
#include "Tab.h"
#include "TabEvents.h"
#include "TabList.h"
#include "../Helper/ShellHelper.h"

namespace
{

BrowserTabLockState DetermineLockState(const Tab *tab)
{
	switch (tab->GetLockState())
	{
	case Tab::LockState::NotLocked:
		return BrowserTabLockState::NotLocked;

	case Tab::LockState::Locked:
		return BrowserTabLockState::Locked;

	case Tab::LockState::AddressLocked:
		return BrowserTabLockState::AddressLocked;
	}

	return BrowserTabLockState::NotLocked;
}

}

BrowserTabStripModel::BrowserTabStripModel(const BrowserWindow *browser, const TabList *tabList,
	TabEvents *tabEvents, ShellBrowserEvents *shellBrowserEvents,
	NavigationEvents *navigationEvents) :
	m_browser(browser),
	m_tabList(tabList)
{
	m_connections.push_back(tabEvents->AddCreatedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this), TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddSelectedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this), TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddUpdatedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this), TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddMovedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this), TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddRemovedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this), TabEventScope::ForBrowser(*browser)));

	m_connections.push_back(shellBrowserEvents->AddDirectoryPropertiesChangedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this),
		ShellBrowserEventScope::ForBrowser(*browser)));

	m_connections.push_back(navigationEvents->AddCommittedObserver(
		std::bind(&BrowserTabStripModel::OnTabsChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
}

std::vector<BrowserTabStripModel::TabInfo> BrowserTabStripModel::GetTabs() const
{
	std::vector<TabInfo> tabs;

	for (Tab *tab : m_tabList->GetForBrowser(m_browser))
	{
		tabs.push_back(BuildTabInfo(tab));
	}

	return tabs;
}

std::optional<int> BrowserTabStripModel::MaybeGetSelectedTabId() const
{
	for (const auto &tab : GetTabs())
	{
		if (tab.selected)
		{
			return tab.id;
		}
	}

	return std::nullopt;
}

void BrowserTabStripModel::OnTabsChanged()
{
	updatedSignal.m_signal();
}

BrowserTabStripModel::TabInfo BrowserTabStripModel::BuildTabInfo(const Tab *tab) const
{
	const auto &directory = tab->GetShellBrowser()->GetDirectory();

	return {
		.id = tab->GetId(),
		.title = tab->GetName(),
		.tooltip = GetFolderPathForDisplayWithFallback(directory.Raw()),
		.lockState = DetermineLockState(tab),
		.selected = m_browser->IsShellBrowserActive(tab->GetShellBrowser())
	};
}