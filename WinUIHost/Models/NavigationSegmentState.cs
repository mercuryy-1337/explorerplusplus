// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using ExplorerPlusPlus.WinUIHost.Infrastructure;
using Microsoft.UI.Xaml;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class NavigationSegmentState : ObservableObject
	{
		private string m_glyph = string.Empty;
		private string m_text = string.Empty;
		private bool m_showSeparator;

		public string Glyph
		{
			get => m_glyph;
			set
			{
				if (SetProperty(ref m_glyph, value))
				{
					OnPropertyChanged(nameof(GlyphVisibility));
				}
			}
		}

		public string Text
		{
			get => m_text;
			set
			{
				if (SetProperty(ref m_text, value))
				{
					OnPropertyChanged(nameof(TextVisibility));
				}
			}
		}

		public bool ShowSeparator
		{
			get => m_showSeparator;
			set
			{
				if (SetProperty(ref m_showSeparator, value))
				{
					OnPropertyChanged(nameof(SeparatorVisibility));
				}
			}
		}

		public Visibility GlyphVisibility => string.IsNullOrWhiteSpace(Glyph)
			? Visibility.Collapsed
			: Visibility.Visible;

		public Visibility TextVisibility => string.IsNullOrWhiteSpace(Text)
			? Visibility.Collapsed
			: Visibility.Visible;

		public Visibility SeparatorVisibility => ShowSeparator ? Visibility.Visible : Visibility.Collapsed;
	}
}