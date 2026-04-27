// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#pragma once

#include "../Helper/SignalWrapper.h"
#include <boost/signals2.hpp>
#include <optional>
#include <string>
#include <vector>

class BrowserWindow;
class DriveModel;
class NavigationEvents;
class ShellBrowserEvents;
class TabEvents;

enum class BrowserFoldersPaneItemKind
{
	Home,
	ThisPc,
	Drive,
	Folder,
	CurrentLocation
};

class BrowserFoldersPaneModel
{
public:
	struct ItemInfo
	{
		std::wstring key;
		std::wstring title;
		std::wstring activationPath;
		std::wstring tooltip;
		BrowserFoldersPaneItemKind kind;
		int depth;
		bool canExpand;
		bool expanded;
		bool selected;
		bool currentBranch;
	};

	SignalWrapper<BrowserFoldersPaneModel, void()> updatedSignal;

	BrowserFoldersPaneModel(const BrowserWindow *browser, DriveModel *driveModel,
		TabEvents *tabEvents, ShellBrowserEvents *shellBrowserEvents,
		NavigationEvents *navigationEvents);

	std::vector<ItemInfo> GetVisibleItems() const;
	std::optional<std::wstring> MaybeGetSelectedActivationPath() const;

private:
	void OnFoldersPaneChanged();

	const BrowserWindow *const m_browser;
	DriveModel *const m_driveModel;
	std::vector<boost::signals2::scoped_connection> m_connections;
};