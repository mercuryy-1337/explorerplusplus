// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#pragma once

#include <memory>
#include <string_view>
#include <windows.h>

class App;
struct WindowStorageData;

namespace Revamp
{

// Placeholder host for the future native revamp shell. This stays compile-safe while the legacy
// shell remains active and gives the revamp a concrete top-level type to build around.
class RevampWindow
{
public:
	static std::unique_ptr<RevampWindow> CreatePlaceholder(App *app,
		const WindowStorageData *storageData = nullptr);

	HWND GetHWND() const;
	std::wstring_view GetDebugName() const;

private:
	explicit RevampWindow(App *app);

	App *const m_app;
	HWND m_hwnd;
};

} // namespace Revamp