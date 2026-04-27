// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using ExplorerPlusPlus.WinUIHost.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class FileItemState : ObservableObject
	{
		private string m_name = string.Empty;
		private string m_glyph = string.Empty;
		private string m_itemType = string.Empty;
		private string m_modified = string.Empty;
		private string m_size = string.Empty;
		private string m_activationPath = string.Empty;
		private DateTimeOffset? m_modifiedValue;
		private long? m_sizeValue;
		private ImageSource? m_iconSource;
		private bool m_isFolder;
		private bool m_hasDriveUsage;
		private bool m_isDriveUsageCritical;
		private double m_driveUsagePercentage;

		public string Name
		{
			get => m_name;
			set => SetProperty(ref m_name, value);
		}

		public string Glyph
		{
			get => m_glyph;
			set => SetProperty(ref m_glyph, value);
		}

		public string ItemType
		{
			get => m_itemType;
			set => SetProperty(ref m_itemType, value);
		}

		public string Modified
		{
			get => m_modified;
			set => SetProperty(ref m_modified, value);
		}

		public string Size
		{
			get => m_size;
			set => SetProperty(ref m_size, value);
		}

		public string ActivationPath
		{
			get => m_activationPath;
			set => SetProperty(ref m_activationPath, value);
		}

		public DateTimeOffset? ModifiedValue
		{
			get => m_modifiedValue;
			set => SetProperty(ref m_modifiedValue, value);
		}

		public long? SizeValue
		{
			get => m_sizeValue;
			set => SetProperty(ref m_sizeValue, value);
		}

		public ImageSource? IconSource
		{
			get => m_iconSource;
			set
			{
				if (SetProperty(ref m_iconSource, value))
				{
					OnPropertyChanged(nameof(NativeIconOpacity));
					OnPropertyChanged(nameof(GlyphOpacity));
				}
			}
		}

		public bool IsFolder
		{
			get => m_isFolder;
			set => SetProperty(ref m_isFolder, value);
		}

		public bool HasDriveUsage
		{
			get => m_hasDriveUsage;
			set
			{
				if (SetProperty(ref m_hasDriveUsage, value))
				{
					OnPropertyChanged(nameof(DriveUsageVisibility));
					OnPropertyChanged(nameof(NormalDriveUsageVisibility));
					OnPropertyChanged(nameof(CriticalDriveUsageVisibility));
				}
			}
		}

		public bool IsDriveUsageCritical
		{
			get => m_isDriveUsageCritical;
			set
			{
				if (SetProperty(ref m_isDriveUsageCritical, value))
				{
					OnPropertyChanged(nameof(NormalDriveUsageVisibility));
					OnPropertyChanged(nameof(CriticalDriveUsageVisibility));
				}
			}
		}

		public double DriveUsagePercentage
		{
			get => m_driveUsagePercentage;
			set => SetProperty(ref m_driveUsagePercentage, value);
		}

		public double NativeIconOpacity => IconSource == null ? 0.0 : 1.0;
		public double GlyphOpacity => IconSource == null ? 1.0 : 0.0;
		public Visibility DriveUsageVisibility => HasDriveUsage ? Visibility.Visible : Visibility.Collapsed;
		public Visibility NormalDriveUsageVisibility => HasDriveUsage && !IsDriveUsageCritical ? Visibility.Visible : Visibility.Collapsed;
		public Visibility CriticalDriveUsageVisibility => HasDriveUsage && IsDriveUsageCritical ? Visibility.Visible : Visibility.Collapsed;
	}
}