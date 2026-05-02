// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

#include "stdafx.h"
#include "Revamp/RevampWindow.h"

namespace Revamp
{

std::unique_ptr<RevampWindow> RevampWindow::CreatePlaceholder(App *app,
	const WindowStorageData *storageData)
{
	UNREFERENCED_PARAMETER(storageData);

	return std::unique_ptr<RevampWindow>(new RevampWindow(app));
}

RevampWindow::RevampWindow(App *app) : m_app(app), m_hwnd(nullptr)
{
}

HWND RevampWindow::GetHWND() const
{
	return m_hwnd;
}

std::wstring_view RevampWindow::GetDebugName() const
{
	return L"NativeRevampWindow";
}

} // namespace Revamp