// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#include "stdafx.h"
#include "BrowserFoldersPaneModel.h"
#include "BrowserWindow.h"
#include "DriveModel.h"
#include "ShellBrowser/HistoryEntry.h"
#include "ShellBrowser/NavigationEvents.h"
#include "ShellBrowser/ShellBrowser.h"
#include "ShellBrowser/ShellBrowserEvents.h"
#include "ShellBrowser/ShellNavigationController.h"
#include "TabEvents.h"
#include "../Helper/ShellHelper.h"
#include <boost/algorithm/string/predicate.hpp>
#include <cwctype>

namespace
{

struct CurrentLocation
{
	std::wstring displayPath;
	std::wstring activationPath;
	std::wstring title;
};

bool PathsEqual(const std::wstring &first, const std::wstring &second)
{
	return boost::iequals(first, second);
}

std::wstring TrimTrailingSeparators(const std::wstring &path)
{
	std::wstring trimmedPath = path;

	while (trimmedPath.size() > 3
		&& (trimmedPath.ends_with(L"\\") || trimmedPath.ends_with(L"/")))
	{
		trimmedPath.pop_back();
	}

	return trimmedPath;
}

bool IsFilesystemDrivePath(const std::wstring &path)
{
	return path.size() >= 3 && std::iswalpha(static_cast<wint_t>(path[0])) != 0 && path[1] == L':'
		&& (path[2] == L'\\' || path[2] == L'/');
}

std::wstring GetDriveRoot(const std::wstring &path)
{
	if (!IsFilesystemDrivePath(path))
	{
		return {};
	}

	return std::wstring(path.substr(0, 3));
}

std::vector<std::wstring> BuildFilesystemBranch(const std::wstring &path)
{
	std::vector<std::wstring> branch;
	std::wstring normalizedPath = TrimTrailingSeparators(path);

	if (!IsFilesystemDrivePath(normalizedPath))
	{
		return branch;
	}

	branch.push_back(GetDriveRoot(normalizedPath));

	size_t segmentStart = 3;

	while (segmentStart < normalizedPath.size())
	{
		size_t nextSeparator = normalizedPath.find_first_of(L"\\/", segmentStart);
		std::wstring segment = normalizedPath.substr(segmentStart, nextSeparator - segmentStart);

		if (!segment.empty())
		{
			std::wstring currentSegment = branch.back();

			if (!currentSegment.ends_with(L"\\"))
			{
				currentSegment += L'\\';
			}

			currentSegment += segment;
			branch.push_back(std::move(currentSegment));
		}

		if (nextSeparator == std::wstring::npos)
		{
			break;
		}

		segmentStart = nextSeparator + 1;
	}

	return branch;
}

std::wstring GetDisplayNameForPath(const std::wstring &path)
{
	std::wstring displayName;

	if (FAILED(GetDisplayName(path, SHGDN_INFOLDER, displayName)))
	{
		return path;
	}

	return displayName;
}

std::wstring GetHomeTitle()
{
	std::wstring title;

	if (FAILED(GetDisplayName(std::wstring(HOME_FOLDER_PATH), SHGDN_INFOLDER, title)))
	{
		return L"Home";
	}

	return title;
}

std::wstring GetThisPcTitle()
{
	std::wstring title;

	if (FAILED(GetCsidlDisplayName(CSIDL_DRIVES, SHGDN_INFOLDER, title)))
	{
		return L"This PC";
	}

	return title;
}

std::optional<std::wstring> MaybeGetThisPcActivationPath()
{
	std::wstring parsingPath;

	if (FAILED(GetCsidlDisplayName(CSIDL_DRIVES, SHGDN_FORPARSING, parsingPath)))
	{
		return std::nullopt;
	}

	return parsingPath;
}

std::optional<CurrentLocation> MaybeGetCurrentLocation(const BrowserWindow *browser)
{
	const auto *shellBrowser = browser->GetActiveShellBrowser();

	if (!shellBrowser)
	{
		return std::nullopt;
	}

	const auto *navigationController = shellBrowser->GetNavigationController();

	if (!navigationController)
	{
		return std::nullopt;
	}

	const auto *currentEntry = navigationController->GetCurrentEntry();

	if (!currentEntry)
	{
		return std::nullopt;
	}

	CurrentLocation currentLocation;
	currentLocation.displayPath = GetFolderPathForDisplayWithFallback(currentEntry->GetPidl().Raw());
	currentLocation.title = GetDisplayNameWithFallback(currentEntry->GetPidl().Raw(), SHGDN_INFOLDER);

	if (FAILED(GetDisplayName(currentEntry->GetPidl().Raw(), SHGDN_FORPARSING,
		currentLocation.activationPath)))
	{
		currentLocation.activationPath = currentLocation.displayPath;
	}

	return currentLocation;
}

}

BrowserFoldersPaneModel::BrowserFoldersPaneModel(const BrowserWindow *browser,
	DriveModel *driveModel, TabEvents *tabEvents, ShellBrowserEvents *shellBrowserEvents,
	NavigationEvents *navigationEvents) :
	m_browser(browser),
	m_driveModel(driveModel)
{
	m_connections.push_back(tabEvents->AddCreatedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddSelectedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddUpdatedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddMovedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		TabEventScope::ForBrowser(*browser)));
	m_connections.push_back(tabEvents->AddRemovedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		TabEventScope::ForBrowser(*browser)));

	m_connections.push_back(shellBrowserEvents->AddDirectoryPropertiesChangedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		ShellBrowserEventScope::ForBrowser(*browser)));

	m_connections.push_back(navigationEvents->AddStartedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddCommittedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddFailedObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		NavigationEventScope::ForBrowser(*browser)));
	m_connections.push_back(navigationEvents->AddCancelledObserver(
		std::bind(&BrowserFoldersPaneModel::OnFoldersPaneChanged, this),
		NavigationEventScope::ForBrowser(*browser)));

	m_connections.push_back(m_driveModel->AddDriveAddedObserver(
		[this](const std::wstring &, size_t) { OnFoldersPaneChanged(); }));
	m_connections.push_back(m_driveModel->AddDriveUpdatedObserver(
		[this](const std::wstring &) { OnFoldersPaneChanged(); }));
	m_connections.push_back(m_driveModel->AddDriveRemovedObserver(
		[this](const std::wstring &, size_t) { OnFoldersPaneChanged(); }));
}

std::vector<BrowserFoldersPaneModel::ItemInfo> BrowserFoldersPaneModel::GetVisibleItems() const
{
	std::vector<ItemInfo> items;
	auto currentLocation = MaybeGetCurrentLocation(m_browser);
	const auto &drives = m_driveModel->GetDrives();

	const std::wstring homeTitle = GetHomeTitle();
	bool homeSelected = currentLocation
		&& (PathsEqual(currentLocation->activationPath, HOME_FOLDER_PATH)
			|| PathsEqual(currentLocation->displayPath, homeTitle));

	items.push_back({
		.key = HOME_FOLDER_PATH,
		.title = homeTitle,
		.activationPath = HOME_FOLDER_PATH,
		.tooltip = homeTitle,
		.kind = BrowserFoldersPaneItemKind::Home,
		.depth = 0,
		.canExpand = false,
		.expanded = homeSelected,
		.selected = homeSelected,
		.currentBranch = homeSelected
	});

	bool isFilesystemLocation = currentLocation && IsFilesystemDrivePath(currentLocation->activationPath);
	const std::wstring thisPcTitle = GetThisPcTitle();
	const std::wstring thisPcActivationPath =
		MaybeGetThisPcActivationPath().value_or(thisPcTitle);
	bool thisPcSelected = currentLocation
		&& (PathsEqual(currentLocation->activationPath, thisPcActivationPath)
			|| PathsEqual(currentLocation->displayPath, thisPcTitle));

	if (currentLocation && !isFilesystemLocation && !homeSelected && !thisPcSelected)
	{
		items.push_back({
			.key = currentLocation->activationPath,
			.title = currentLocation->title,
			.activationPath = currentLocation->activationPath,
			.tooltip = currentLocation->displayPath,
			.kind = BrowserFoldersPaneItemKind::CurrentLocation,
			.depth = 0,
			.canExpand = false,
			.expanded = true,
			.selected = true,
			.currentBranch = true
		});
	}

	items.push_back({
		.key = thisPcActivationPath,
		.title = thisPcTitle,
		.activationPath = thisPcActivationPath,
		.tooltip = thisPcTitle,
		.kind = BrowserFoldersPaneItemKind::ThisPc,
		.depth = 0,
		.canExpand = !drives.empty(),
		.expanded = isFilesystemLocation,
		.selected = thisPcSelected,
		.currentBranch = isFilesystemLocation || thisPcSelected
	});

	std::wstring currentDrive =
		isFilesystemLocation ? GetDriveRoot(currentLocation->activationPath) : std::wstring();
	std::wstring currentPath = isFilesystemLocation
		? TrimTrailingSeparators(currentLocation->activationPath)
		: std::wstring();

	for (const auto &drive : drives)
	{
		bool isCurrentDrive = isFilesystemLocation && PathsEqual(currentDrive, drive);
		bool driveSelected = isCurrentDrive && PathsEqual(currentPath, TrimTrailingSeparators(drive));

		items.push_back({
			.key = drive,
			.title = GetDisplayNameForPath(drive),
			.activationPath = drive,
			.tooltip = drive,
			.kind = BrowserFoldersPaneItemKind::Drive,
			.depth = 1,
			.canExpand = true,
			.expanded = isCurrentDrive,
			.selected = driveSelected,
			.currentBranch = isCurrentDrive
		});

		if (!isCurrentDrive)
		{
			continue;
		}

		auto branch = BuildFilesystemBranch(currentLocation->activationPath);

		for (size_t index = 1; index < branch.size(); ++index)
		{
			const auto &segmentPath = branch[index];
			bool segmentSelected = PathsEqual(currentPath, TrimTrailingSeparators(segmentPath));

			items.push_back({
				.key = segmentPath,
				.title = GetDisplayNameForPath(segmentPath),
				.activationPath = segmentPath,
				.tooltip = segmentPath,
				.kind = BrowserFoldersPaneItemKind::Folder,
				.depth = static_cast<int>(index + 1),
				.canExpand = true,
				.expanded = !segmentSelected,
				.selected = segmentSelected,
				.currentBranch = true
			});
		}
	}

	return items;
}

std::optional<std::wstring> BrowserFoldersPaneModel::MaybeGetSelectedActivationPath() const
{
	auto currentLocation = MaybeGetCurrentLocation(m_browser);

	if (!currentLocation)
	{
		return std::nullopt;
	}

	return currentLocation->activationPath;
}

void BrowserFoldersPaneModel::OnFoldersPaneChanged()
{
	updatedSignal.m_signal();
}