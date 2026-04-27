// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#include "stdafx.h"
#include "BrowserNavigationModel.h"
#include "BrowserWindow.h"
#include "ShellBrowser/HistoryEntry.h"
#include "ShellBrowser/NavigationEvents.h"
#include "ShellBrowser/ShellBrowser.h"
#include "ShellBrowser/ShellBrowserEvents.h"
#include "ShellBrowser/ShellNavigationController.h"
#include "TabEvents.h"
#include "../Helper/ShellHelper.h"

BrowserNavigationModel::BrowserNavigationModel(const BrowserWindow *browser, TabEvents *tabEvents,
	ShellBrowserEvents *shellBrowserEvents, NavigationEvents *navigationEvents) :
	m_browser(browser)
{
	m_connections.push_back(tabEvents->AddCreatedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddSelectedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddUpdatedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddMovedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddRemovedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		TabEventScope::ForBrowser(*browser)));

	m_connections.push_back(shellBrowserEvents->AddDirectoryPropertiesChangedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		ShellBrowserEventScope::ForBrowser(*browser)));

	m_connections.push_back(navigationEvents->AddStartedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddCommittedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddFailedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddCancelledObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddStoppedObserver(
		std::bind(&BrowserNavigationModel::OnNavigationStateChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
}

BrowserNavigationModel::NavigationState BrowserNavigationModel::GetState() const
{
	NavigationState state;

	const auto *shellBrowser = m_browser->GetActiveShellBrowser();

	if (!shellBrowser)
	{
		return state;
	}

	const auto *navigationController = shellBrowser->GetNavigationController();

	if (!navigationController)
	{
		return state;
	}

	const auto *currentEntry = navigationController->GetCurrentEntry();

	if (!currentEntry)
	{
		return state;
	}

	state.currentLocationTitle = GetDisplayNameWithFallback(currentEntry->GetPidl().Raw(), SHGDN_INFOLDER);
	state.currentLocationPath = GetFolderPathForDisplayWithFallback(currentEntry->GetPidl().Raw());

	if (FAILED(GetDisplayName(currentEntry->GetPidl().Raw(), SHGDN_FORPARSING,
		state.currentLocationParsingPath)))
	{
		state.currentLocationParsingPath = state.currentLocationPath;
	}

	state.canGoBack = navigationController->CanGoBack();
	state.canGoForward = navigationController->CanGoForward();
	state.canGoUp = navigationController->CanGoUp();
	state.canRefresh = true;
	state.isNavigating = shellBrowser->MaybeGetLatestActiveNavigation() != nullptr;

	for (const auto *entry : navigationController->GetBackHistory())
	{
		state.backHistory.push_back(BuildHistoryEntryInfo(entry));
	}

	for (const auto *entry : navigationController->GetForwardHistory())
	{
		state.forwardHistory.push_back(BuildHistoryEntryInfo(entry));
	}

	return state;
}

void BrowserNavigationModel::OnNavigationStateChanged()
{
	updatedSignal.m_signal();
}

BrowserNavigationModel::HistoryEntryInfo BrowserNavigationModel::BuildHistoryEntryInfo(
	const HistoryEntry *entry) const
{
	return {
		.id = entry->GetId(),
		.title = GetDisplayNameWithFallback(entry->GetPidl().Raw(), SHGDN_INFOLDER),
		.path = GetFolderPathForDisplayWithFallback(entry->GetPidl().Raw())
	};
}