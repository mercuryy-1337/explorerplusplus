// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using ExplorerPlusPlus.WinUIHost.Infrastructure;
using System.Collections.Generic;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class TabState : ObservableObject
	{
		private string m_title = string.Empty;
		private string m_glyph = "\uE8B7";
		private string m_activationPath = string.Empty;
		private string? m_selectedFolderActivationPath;

		public TabState()
		{
			BackHistory = new List<string>();
			ForwardHistory = new List<string>();
		}

		public string Title
		{
			get => m_title;
			set => SetProperty(ref m_title, value);
		}

		public string Glyph
		{
			get => m_glyph;
			set => SetProperty(ref m_glyph, value);
		}

		public string ActivationPath
		{
			get => m_activationPath;
			set => SetProperty(ref m_activationPath, value);
		}

		public string? SelectedFolderActivationPath
		{
			get => m_selectedFolderActivationPath;
			set => SetProperty(ref m_selectedFolderActivationPath, value);
		}

		public List<string> BackHistory { get; }

		public List<string> ForwardHistory { get; }
	}
}
