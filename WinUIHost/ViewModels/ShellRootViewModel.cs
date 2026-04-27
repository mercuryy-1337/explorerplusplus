using ExplorerPlusPlus.WinUIHost.Infrastructure;
using ExplorerPlusPlus.WinUIHost.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace ExplorerPlusPlus.WinUIHost.ViewModels
{
	public sealed class ShellRootViewModel : ObservableObject
	{
		private const string HomeActivationPath = "shell:home";
		private const string ThisPcActivationPath = "shell:this-pc";

		private readonly RelayCommand m_goBackCommand;
		private readonly RelayCommand m_goForwardCommand;
		private readonly RelayCommand m_goUpCommand;
		private readonly RelayCommand m_refreshCommand;
		private readonly RelayCommand<FolderPaneItemState> m_selectFolderCommand;
		private readonly RelayCommand<FolderPaneItemState> m_activateFolderCommand;
		private readonly RelayCommand<FolderPaneItemState> m_toggleFolderExpansionCommand;
		private readonly RelayCommand<FileItemState> m_activateFileItemCommand;
		private readonly List<string> m_backHistory = new();
		private readonly List<string> m_forwardHistory = new();
		private readonly HashSet<string> m_expandedPaths = new(StringComparer.OrdinalIgnoreCase);

		private TabState? m_selectedTab;
		private FolderPaneItemState? m_selectedFolder;
		private string m_currentActivationPath = ThisPcActivationPath;
		private string? m_selectedFolderActivationPath;

		public ObservableCollection<TabState> Tabs { get; }
		public NavigationState Navigation { get; }
		public ObservableCollection<FolderPaneItemState> FolderPane { get; }
		public ObservableCollection<FileItemState> Files { get; }

		public ICommand GoBackCommand => m_goBackCommand;
		public ICommand GoForwardCommand => m_goForwardCommand;
		public ICommand GoUpCommand => m_goUpCommand;
		public ICommand RefreshCommand => m_refreshCommand;
		public ICommand SelectFolderCommand => m_selectFolderCommand;
		public ICommand ActivateFolderCommand => m_activateFolderCommand;
		public ICommand ToggleFolderExpansionCommand => m_toggleFolderExpansionCommand;
		public ICommand ActivateFileItemCommand => m_activateFileItemCommand;

		public TabState? SelectedTab
		{
			get => m_selectedTab;
			set => SetProperty(ref m_selectedTab, value);
		}

		public FolderPaneItemState? SelectedFolder
		{
			get => m_selectedFolder;
			set => SetProperty(ref m_selectedFolder, value);
		}

		public ShellRootViewModel()
		{
			Tabs = new ObservableCollection<TabState>();
			Navigation = new NavigationState();
			FolderPane = new ObservableCollection<FolderPaneItemState>();
			Files = new ObservableCollection<FileItemState>();

			m_goBackCommand = new RelayCommand(GoBack, () => m_backHistory.Count > 0);
			m_goForwardCommand = new RelayCommand(GoForward, () => m_forwardHistory.Count > 0);
			m_goUpCommand = new RelayCommand(GoUp, CanGoUp);
			m_refreshCommand = new RelayCommand(Refresh);
			m_selectFolderCommand = new RelayCommand<FolderPaneItemState>(SelectFolder);
			m_activateFolderCommand = new RelayCommand<FolderPaneItemState>(ActivateFolder);
			m_toggleFolderExpansionCommand = new RelayCommand<FolderPaneItemState>(ToggleFolderExpansion);
			m_activateFileItemCommand = new RelayCommand<FileItemState>(ActivateFileItem);

			m_expandedPaths.Add(ThisPcActivationPath);
			NavigateTo(ThisPcActivationPath, false);
		}

		private static IEnumerable<(string Title, string ActivationPath, string Glyph)> BuildPinnedLocationDefinitions()
		{
			yield return ("Home", HomeActivationPath, "\uE80F");

			foreach (var definition in BuildKnownFolderDefinitions())
			{
				yield return definition;
			}
		}

		private static IEnumerable<(string Title, string ActivationPath, string Glyph)> BuildKnownFolderDefinitions()
		{
			foreach (var definition in new[]
			{
				("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "\uE8CC"),
				("Downloads", GetDownloadsPath(), "\uE896"),
				("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "\uE8A5"),
				("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "\uE91B"),
				("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "\uEC4F"),
				("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "\uEC0D")
			})
			{
				var activationPath = definition.Item2;

				if (!string.IsNullOrWhiteSpace(activationPath) && Directory.Exists(activationPath))
				{
					yield return (definition.Item1, NormalizeFileSystemPath(activationPath), definition.Item3);
				}
			}

			var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

			if (!string.IsNullOrWhiteSpace(oneDrivePath) && Directory.Exists(oneDrivePath))
			{
				yield return ("OneDrive", NormalizeFileSystemPath(oneDrivePath), "\uE753");
			}
		}

		private static string GetDownloadsPath()
		{
			var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			if (string.IsNullOrWhiteSpace(userProfilePath))
			{
				return string.Empty;
			}

			return Path.Combine(userProfilePath, "Downloads");
		}

		private void GoBack()
		{
			if (m_backHistory.Count == 0)
			{
				return;
			}

			m_forwardHistory.Add(m_currentActivationPath);
			var previousIndex = m_backHistory.Count - 1;
			var previousPath = m_backHistory[previousIndex];
			m_backHistory.RemoveAt(previousIndex);
			NavigateTo(previousPath, false);
		}

		private void GoForward()
		{
			if (m_forwardHistory.Count == 0)
			{
				return;
			}

			m_backHistory.Add(m_currentActivationPath);
			var nextIndex = m_forwardHistory.Count - 1;
			var nextPath = m_forwardHistory[nextIndex];
			m_forwardHistory.RemoveAt(nextIndex);
			NavigateTo(nextPath, false);
		}

		private void GoUp()
		{
			var parentPath = GetParentActivationPath(m_currentActivationPath);

			if (parentPath == null)
			{
				return;
			}

			NavigateTo(parentPath, true);
		}

		private bool CanGoUp()
		{
			return GetParentActivationPath(m_currentActivationPath) != null;
		}

		private void Refresh()
		{
			RefreshState();
		}

		private void SelectFolder(FolderPaneItemState? folder)
		{
			if (folder?.IsHeader == true)
			{
				return;
			}

			m_selectedFolderActivationPath = folder?.ActivationPath;
			ApplyFolderSelectionState();
		}

		private void ActivateFolder(FolderPaneItemState? folder)
		{
			if (folder == null || folder.IsHeader)
			{
				return;
			}

			NavigateTo(folder.ActivationPath, true);
		}

		private void ToggleFolderExpansion(FolderPaneItemState? folder)
		{
			if (folder == null || folder.IsHeader || !folder.CanExpand)
			{
				return;
			}

			if (!m_expandedPaths.Remove(folder.ActivationPath))
			{
				m_expandedPaths.Add(folder.ActivationPath);
			}

			m_selectedFolderActivationPath = folder.ActivationPath;
			RefreshFoldersPane();
		}

		private void ActivateFileItem(FileItemState? item)
		{
			if (item == null)
			{
				return;
			}

			if (item.IsFolder)
			{
				NavigateTo(item.ActivationPath, true);
				return;
			}

			if (!string.IsNullOrWhiteSpace(item.ActivationPath) && File.Exists(item.ActivationPath))
			{
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = item.ActivationPath,
						UseShellExecute = true
					});
				}
				catch
				{
				}
			}
		}

		private void NavigateTo(string activationPath, bool recordHistory)
		{
			var normalizedActivationPath = NormalizeActivationPath(activationPath);

			if (recordHistory && !PathsEqual(m_currentActivationPath, normalizedActivationPath))
			{
				m_backHistory.Add(m_currentActivationPath);
				m_forwardHistory.Clear();
			}

			m_currentActivationPath = normalizedActivationPath;
			m_selectedFolderActivationPath = normalizedActivationPath;
			EnsureCurrentPathExpanded(normalizedActivationPath);
			RefreshState();
		}

		private void RefreshState()
		{
			Navigation.IsNavigating = false;
			RefreshTabs();
			RefreshNavigation();
			RefreshFoldersPane();
			RefreshFiles();
			NotifyCommandStateChanged();
		}

		private void RefreshTabs()
		{
			Tabs.Clear();
			var tab = new TabState
			{
				Title = GetDisplayTitle(m_currentActivationPath),
				Glyph = GetLocationGlyph(m_currentActivationPath),
				Selected = true
			};

			Tabs.Add(tab);
			SelectedTab = tab;
		}

		private void RefreshNavigation()
		{
			Navigation.CurrentPath = GetDisplayPath(m_currentActivationPath);
			Navigation.CanGoBack = m_backHistory.Count > 0;
			Navigation.CanGoForward = m_forwardHistory.Count > 0;
			Navigation.CanGoUp = CanGoUp();
			Navigation.CanRefresh = true;
		}

		private void RefreshFoldersPane()
		{
			var items = new List<FolderPaneItemState>();
			var quickAccessLocations = BuildKnownFolderDefinitions().ToList();

			items.Add(new FolderPaneItemState
			{
				Title = "Home",
				Glyph = "\uE80F",
				ActivationPath = HomeActivationPath,
				CanExpand = false,
				IsExpanded = false,
				Depth = 0
			});

			if (quickAccessLocations.Count > 0)
			{
				items.Add(new FolderPaneItemState
				{
					Title = "Quick access",
					IsHeader = true
				});

				foreach (var pinnedLocation in quickAccessLocations)
				{
					items.Add(new FolderPaneItemState
					{
						Title = pinnedLocation.Title,
						Glyph = pinnedLocation.Glyph,
						ActivationPath = pinnedLocation.ActivationPath,
						CanExpand = false,
						IsExpanded = false,
						Depth = 0
					});
				}
			}

			items.Add(new FolderPaneItemState
			{
				Title = "Devices and drives",
				IsHeader = true
			});

			var thisPcItem = new FolderPaneItemState
			{
				Title = "This PC",
				Glyph = "\uE7F8",
				ActivationPath = ThisPcActivationPath,
				CanExpand = true,
				IsExpanded = m_expandedPaths.Contains(ThisPcActivationPath) || IsThisPcBranch(m_currentActivationPath),
				Depth = 0
			};

			items.Add(thisPcItem);

			if (thisPcItem.IsExpanded)
			{
				foreach (var drive in GetSortedDrives())
				{
					AddDirectoryTree(items, drive.RootDirectory.FullName, BuildDriveTitle(drive), "\uEDA2", 1);
				}
			}

			FolderPane.Clear();

			foreach (var item in items)
			{
				FolderPane.Add(item);
			}

			ApplyFolderSelectionState();
		}

		private void ApplyFolderSelectionState()
		{
			FolderPaneItemState? selectedItem = null;
			var selectedActivationPath = m_selectedFolderActivationPath ?? m_currentActivationPath;

			foreach (var item in FolderPane)
			{
				if (item.IsHeader)
				{
					item.IsSelected = false;
					continue;
				}

				item.IsSelected = PathsEqual(item.ActivationPath, selectedActivationPath);

				if (item.IsSelected)
				{
					selectedItem = item;
				}
			}

			SelectedFolder = selectedItem;
		}

		private void RefreshFiles()
		{
			Files.Clear();

			foreach (var item in BuildFileItemsForCurrentLocation())
			{
				Files.Add(item);
			}
		}

		private IEnumerable<FileItemState> BuildFileItemsForCurrentLocation()
		{
			if (IsHomeLocation(m_currentActivationPath))
			{
				foreach (var pinnedLocation in BuildPinnedLocationDefinitions())
				{
					if (pinnedLocation.ActivationPath == HomeActivationPath)
					{
						continue;
					}

					yield return new FileItemState
					{
						Name = pinnedLocation.Title,
						Glyph = pinnedLocation.Glyph,
						ItemType = "Folder",
						ActivationPath = pinnedLocation.ActivationPath,
						IsFolder = true
					};
				}

				yield return new FileItemState
				{
					Name = "This PC",
					Glyph = "\uE7F8",
					ItemType = "Location",
					ActivationPath = ThisPcActivationPath,
					IsFolder = true
				};

				yield break;
			}

			if (IsThisPcLocation(m_currentActivationPath))
			{
				foreach (var drive in GetSortedDrives())
				{
					yield return new FileItemState
					{
						Name = BuildDriveTitle(drive),
						Glyph = "\uEDA2",
						ItemType = BuildDriveTypeLabel(drive),
						Modified = string.Empty,
						Size = BuildDriveCapacityLabel(drive),
						ActivationPath = NormalizeFileSystemPath(drive.RootDirectory.FullName),
						IsFolder = true
					};
				}

				yield break;
			}

			if (!Directory.Exists(m_currentActivationPath))
			{
				yield break;
			}

			foreach (var directory in EnumerateDirectoriesSafe(m_currentActivationPath))
			{
				yield return new FileItemState
				{
					Name = GetFileSystemDisplayName(directory),
					Glyph = "\uE8B7",
					ItemType = "Folder",
					Modified = FormatTimestamp(GetDirectoryWriteTime(directory)),
					Size = string.Empty,
					ActivationPath = NormalizeFileSystemPath(directory),
					IsFolder = true
				};
			}

			foreach (var file in EnumerateFilesSafe(m_currentActivationPath))
			{
				yield return new FileItemState
				{
					Name = Path.GetFileName(file),
					Glyph = "\uE8A5",
					ItemType = BuildFileTypeLabel(file),
					Modified = FormatTimestamp(GetFileWriteTime(file)),
					Size = FormatFileSize(GetFileSize(file)),
					ActivationPath = file,
					IsFolder = false
				};
			}
		}

		private void AddDirectoryTree(ICollection<FolderPaneItemState> items, string directoryPath,
			string title, string glyph, int depth)
		{
			var normalizedPath = NormalizeFileSystemPath(directoryPath);
			var isCurrentBranch = IsFileSystemBranch(m_currentActivationPath, normalizedPath);
			var canExpand = HasChildDirectories(normalizedPath) || isCurrentBranch;
			var isExpanded = canExpand && (m_expandedPaths.Contains(normalizedPath) || isCurrentBranch);

			var item = new FolderPaneItemState
			{
				Title = title,
				Glyph = glyph,
				ActivationPath = normalizedPath,
				CanExpand = canExpand,
				IsExpanded = isExpanded,
				Depth = depth
			};

			items.Add(item);

			if (!isExpanded)
			{
				return;
			}

			foreach (var childDirectory in EnumerateDirectoriesSafe(normalizedPath))
			{
				AddDirectoryTree(items, childDirectory, GetFileSystemDisplayName(childDirectory),
					"\uE8B7", depth + 1);
			}
		}

		private void EnsureCurrentPathExpanded(string activationPath)
		{
			if (IsThisPcLocation(activationPath))
			{
				m_expandedPaths.Add(ThisPcActivationPath);
				return;
			}

			if (!IsFileSystemPath(activationPath))
			{
				return;
			}

			m_expandedPaths.Add(ThisPcActivationPath);
			var currentPath = NormalizeFileSystemPath(activationPath);

			while (!string.IsNullOrEmpty(currentPath))
			{
				m_expandedPaths.Add(currentPath);
				var parent = GetParentDirectoryPath(currentPath);

				if (parent == null)
				{
					break;
				}

				currentPath = parent;
			}
		}

		private void NotifyCommandStateChanged()
		{
			m_goBackCommand.NotifyCanExecuteChanged();
			m_goForwardCommand.NotifyCanExecuteChanged();
			m_goUpCommand.NotifyCanExecuteChanged();
		}

		private static IEnumerable<DriveInfo> GetSortedDrives()
		{
			return DriveInfo.GetDrives()
				.Where(drive => drive.IsReady)
				.OrderBy(drive => drive.DriveType == DriveType.Fixed ? 0 : 1)
				.ThenBy(drive => drive.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static IEnumerable<string> EnumerateDirectoriesSafe(string directoryPath)
		{
			try
			{
				return Directory.EnumerateDirectories(directoryPath)
					.OrderBy(GetFileSystemDisplayName, StringComparer.CurrentCultureIgnoreCase)
					.ToList();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}

		private static IEnumerable<string> EnumerateFilesSafe(string directoryPath)
		{
			try
			{
				return Directory.EnumerateFiles(directoryPath)
					.OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
					.ToList();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}

		private static bool HasChildDirectories(string directoryPath)
		{
			try
			{
				return Directory.EnumerateDirectories(directoryPath).Any();
			}
			catch
			{
				return false;
			}
		}

		private static DateTimeOffset? GetDirectoryWriteTime(string directoryPath)
		{
			try
			{
				return Directory.GetLastWriteTime(directoryPath);
			}
			catch
			{
				return null;
			}
		}

		private static DateTimeOffset? GetFileWriteTime(string filePath)
		{
			try
			{
				return File.GetLastWriteTime(filePath);
			}
			catch
			{
				return null;
			}
		}

		private static long? GetFileSize(string filePath)
		{
			try
			{
				return new FileInfo(filePath).Length;
			}
			catch
			{
				return null;
			}
		}

		private static string BuildDriveTitle(DriveInfo drive)
		{
			var driveLabel = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
			var driveName = TrimTrailingSeparators(drive.Name);
			return $"{driveLabel} ({driveName[^2..]})";
		}

		private static string BuildDriveTypeLabel(DriveInfo drive)
		{
			return drive.DriveType switch
			{
				DriveType.Network => "Network location",
				DriveType.Removable => "Removable drive",
				DriveType.CDRom => "Optical drive",
				_ => "Drive"
			};
		}

		private static string BuildDriveCapacityLabel(DriveInfo drive)
		{
			if (!drive.IsReady)
			{
				return string.Empty;
			}

			return $"{FormatFileSize(drive.AvailableFreeSpace)} free of {FormatFileSize(drive.TotalSize)}";
		}

		private static string BuildFileTypeLabel(string filePath)
		{
			var extension = Path.GetExtension(filePath);

			if (string.IsNullOrWhiteSpace(extension))
			{
				return "File";
			}

			return $"{extension.TrimStart('.').ToUpperInvariant()} File";
		}

		private static string FormatTimestamp(DateTimeOffset? timestamp)
		{
			return timestamp?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;
		}

		private static string FormatFileSize(long? size)
		{
			if (size == null)
			{
				return string.Empty;
			}

			double value = size.Value;
			string[] units = ["B", "KB", "MB", "GB", "TB"];
			int unitIndex = 0;

			while (value >= 1024 && unitIndex < units.Length - 1)
			{
				value /= 1024;
				unitIndex++;
			}

			return unitIndex == 0
				? string.Format(CultureInfo.CurrentCulture, "{0:0} {1}", value, units[unitIndex])
				: string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", value, units[unitIndex]);
		}

		private static string GetDisplayTitle(string activationPath)
		{
			if (IsHomeLocation(activationPath))
			{
				return "Home";
			}

			if (IsThisPcLocation(activationPath))
			{
				return "This PC";
			}

			return GetFileSystemDisplayName(activationPath);
		}

		private static string GetDisplayPath(string activationPath)
		{
			if (IsHomeLocation(activationPath))
			{
				return "Home";
			}

			if (IsThisPcLocation(activationPath))
			{
				return "This PC";
			}

			return activationPath;
		}

		private static string GetLocationGlyph(string activationPath)
		{
			if (IsHomeLocation(activationPath))
			{
				return "\uE80F";
			}

			if (IsThisPcLocation(activationPath))
			{
				return "\uE7F8";
			}

			return Directory.Exists(activationPath) ? "\uE8B7" : "\uE8A5";
		}

		private static string GetFileSystemDisplayName(string path)
		{
			var trimmedPath = TrimTrailingSeparators(path);

			if (trimmedPath.Length <= 3 && trimmedPath.Contains(':'))
			{
				return trimmedPath;
			}

			return Path.GetFileName(trimmedPath);
		}

		private static string NormalizeActivationPath(string activationPath)
		{
			if (string.IsNullOrWhiteSpace(activationPath))
			{
				return ThisPcActivationPath;
			}

			if (IsHomeLocation(activationPath))
			{
				return HomeActivationPath;
			}

			if (IsThisPcLocation(activationPath))
			{
				return ThisPcActivationPath;
			}

			return NormalizeFileSystemPath(Environment.ExpandEnvironmentVariables(activationPath));
		}

		private static string NormalizeFileSystemPath(string path)
		{
			return TrimTrailingSeparators(Path.GetFullPath(path));
		}

		private static string TrimTrailingSeparators(string path)
		{
			var trimmedPath = path;

			while (trimmedPath.Length > 3 && (trimmedPath.EndsWith(Path.DirectorySeparatorChar)
				|| trimmedPath.EndsWith(Path.AltDirectorySeparatorChar)))
			{
				trimmedPath = trimmedPath[..^1];
			}

			return trimmedPath;
		}

		private static bool PathsEqual(string first, string second)
		{
			return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsHomeLocation(string activationPath)
		{
			return PathsEqual(activationPath, HomeActivationPath);
		}

		private static bool IsThisPcLocation(string activationPath)
		{
			return PathsEqual(activationPath, ThisPcActivationPath);
		}

		private static bool IsFileSystemPath(string activationPath)
		{
			return Path.IsPathRooted(activationPath) && !activationPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsThisPcBranch(string activationPath)
		{
			return IsThisPcLocation(activationPath) || IsFileSystemPath(activationPath);
		}

		private static bool IsFileSystemBranch(string currentPath, string candidatePath)
		{
			if (!IsFileSystemPath(currentPath) || !IsFileSystemPath(candidatePath))
			{
				return false;
			}

			if (PathsEqual(currentPath, candidatePath))
			{
				return true;
			}

			var prefix = candidatePath.EndsWith("\\", StringComparison.Ordinal)
				? candidatePath
				: candidatePath + "\\";

			return currentPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}

		private static string? GetParentActivationPath(string activationPath)
		{
			if (IsHomeLocation(activationPath))
			{
				return null;
			}

			if (IsThisPcLocation(activationPath))
			{
				return HomeActivationPath;
			}

			if (!IsFileSystemPath(activationPath))
			{
				return ThisPcActivationPath;
			}

			var parentPath = GetParentDirectoryPath(activationPath);

			if (parentPath == null)
			{
				return ThisPcActivationPath;
			}

			return parentPath;
		}

		private static string? GetParentDirectoryPath(string activationPath)
		{
			var normalizedPath = NormalizeFileSystemPath(activationPath);
			var root = Path.GetPathRoot(normalizedPath);

			if (root != null && PathsEqual(TrimTrailingSeparators(root), normalizedPath))
			{
				return null;
			}

			return Directory.GetParent(normalizedPath)?.FullName is string parentPath
				? NormalizeFileSystemPath(parentPath)
				: null;
		}
	}
}