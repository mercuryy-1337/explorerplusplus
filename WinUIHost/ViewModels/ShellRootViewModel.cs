using ExplorerPlusPlus.WinUIHost.Infrastructure;
using ExplorerPlusPlus.WinUIHost.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
		private readonly DispatcherQueue m_dispatcherQueue;

		private TabState? m_selectedTab;
		private FolderPaneItemState? m_selectedFolder;
		private CancellationTokenSource? m_filesRefreshCancellationSource;
		private string m_currentActivationPath = HomeActivationPath;
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
			m_dispatcherQueue = DispatcherQueue.GetForCurrentThread()
				?? throw new InvalidOperationException("A UI DispatcherQueue is required.");

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

			InitializeFolderPane();
			m_selectedFolderActivationPath = m_currentActivationPath;
			RefreshState(false);
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
			NavigateTo(previousPath, false, false);
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
			NavigateTo(nextPath, false, false);
		}

		private void GoUp()
		{
			var parentPath = GetParentActivationPath(m_currentActivationPath);

			if (parentPath == null)
			{
				return;
			}

			NavigateTo(parentPath, true, false);
		}

		private bool CanGoUp()
		{
			return GetParentActivationPath(m_currentActivationPath) != null;
		}

		private void Refresh()
		{
			RefreshState(true);
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

			NavigateTo(folder.ActivationPath, true, false);
		}

		private void ToggleFolderExpansion(FolderPaneItemState? folder)
		{
			if (folder == null || folder.IsHeader || !folder.CanExpand)
			{
				return;
			}

			m_selectedFolderActivationPath = folder.ActivationPath;

			if (folder.IsExpanded)
			{
				CollapseFolderInPane(folder);
			}
			else
			{
				ExpandFolderInPane(folder);
			}

			ApplyFolderSelectionState();
		}

		private void ActivateFileItem(FileItemState? item)
		{
			if (item == null)
			{
				return;
			}

			if (item.IsFolder)
			{
				NavigateTo(item.ActivationPath, true, false);
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

		private void NavigateTo(string activationPath, bool recordHistory, bool refreshFolderPane = true)
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

			if (!refreshFolderPane)
			{
				EnsureFolderPaneVisibleForPath(normalizedActivationPath);
			}

			RefreshState(refreshFolderPane);
		}

		private void RefreshState(bool refreshFolderPane)
		{
			Navigation.IsNavigating = false;

			if (refreshFolderPane)
			{
				RefreshFoldersPane();
			}

			RefreshTabs();
			RefreshNavigation();
			ApplyFolderSelectionState();
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
			Navigation.CanShowPathText = IsFileSystemPath(m_currentActivationPath);
			Navigation.PathText = Navigation.CanShowPathText ? NormalizeFileSystemPath(m_currentActivationPath) : string.Empty;
			Navigation.IsPathTextVisible = false;
			RefreshNavigationBreadcrumbs();
		}

		private void RefreshNavigationBreadcrumbs()
		{
			Navigation.BreadcrumbSegments.Clear();

			foreach (var segment in BuildNavigationSegments(m_currentActivationPath))
			{
				Navigation.BreadcrumbSegments.Add(segment);
			}
		}

		private void RefreshFoldersPane()
		{
			FolderPane.Clear();
			InitializeFolderPane();
			EnsureFolderPaneVisibleForPath(m_currentActivationPath);
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
			m_filesRefreshCancellationSource?.Cancel();
			m_filesRefreshCancellationSource?.Dispose();
			m_filesRefreshCancellationSource = new CancellationTokenSource();

			var activationPath = m_currentActivationPath;
			var cancellationToken = m_filesRefreshCancellationSource.Token;
			Files.Clear();

			if (IsHomeLocation(activationPath) || IsThisPcLocation(activationPath))
			{
				foreach (var item in BuildFileItemsForLocation(activationPath, includeIcons: true))
				{
					Files.Add(item);
				}

				return;
			}

			_ = RefreshFilesAsync(activationPath, cancellationToken);
		}

		private async Task RefreshFilesAsync(string activationPath, CancellationToken cancellationToken)
		{
			List<FileItemState> items;

			try
			{
				items = await Task.Run(
					() => BuildFileItemsForLocation(activationPath, includeIcons: false).ToList(),
					cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (cancellationToken.IsCancellationRequested || !PathsEqual(m_currentActivationPath, activationPath))
			{
				return;
			}

			m_dispatcherQueue.TryEnqueue(() =>
			{
				if (cancellationToken.IsCancellationRequested || !PathsEqual(m_currentActivationPath, activationPath))
				{
					return;
				}

				Files.Clear();

				foreach (var item in items)
				{
					Files.Add(item);
				}
			});

			_ = PopulateFileIconsAsync(activationPath, items, cancellationToken);
		}

		private async Task PopulateFileIconsAsync(string activationPath, IReadOnlyList<FileItemState> items,
			CancellationToken cancellationToken)
		{
			List<(FileItemState Item, string? IconPath)> iconUpdates;

			try
			{
				iconUpdates = await Task.Run(() =>
				{
					var updates = new List<(FileItemState Item, string? IconPath)>(items.Count);

					foreach (var item in items)
					{
						if (cancellationToken.IsCancellationRequested)
						{
							break;
						}

						string? iconPath = item.IsFolder
							? ShellIconCache.GetFolderIconPath(item.ActivationPath)
							: ShellIconCache.GetFileIconPath(item.ActivationPath);

						updates.Add((item, iconPath));
					}

					return updates;
				}, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (cancellationToken.IsCancellationRequested || !PathsEqual(m_currentActivationPath, activationPath))
			{
				return;
			}

			m_dispatcherQueue.TryEnqueue(() =>
			{
				if (cancellationToken.IsCancellationRequested || !PathsEqual(m_currentActivationPath, activationPath))
				{
					return;
				}

				foreach (var update in iconUpdates)
				{
					if (!string.IsNullOrWhiteSpace(update.IconPath))
					{
						update.Item.IconSource = ShellIconCache.CreateImageSource(update.IconPath);
					}
				}
			});
		}

		private IEnumerable<FileItemState> BuildFileItemsForLocation(string activationPath, bool includeIcons)
		{
			var omitSlowMetadata = IsSlowFileSystemLocation(activationPath);

			if (IsHomeLocation(activationPath))
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
						IconSource = includeIcons && IsFileSystemPath(pinnedLocation.ActivationPath)
							? ShellIconCache.GetFolderIcon(pinnedLocation.ActivationPath)
							: null,
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

			if (IsThisPcLocation(activationPath))
			{
				foreach (var drive in GetSortedDrives())
				{
					yield return new FileItemState
					{
						Name = BuildDriveTitle(drive),
						Glyph = "\uEDA2",
						IconSource = includeIcons ? ShellIconCache.GetDriveIcon(drive.RootDirectory.FullName) : null,
						ItemType = BuildDriveTypeLabel(drive),
						Modified = string.Empty,
						Size = BuildDriveCapacityLabel(drive),
						ActivationPath = NormalizeFileSystemPath(drive.RootDirectory.FullName),
						IsFolder = true
					};
				}

				yield break;
			}

			if (!Directory.Exists(activationPath))
			{
				yield break;
			}

			foreach (var directory in EnumerateDirectoriesSafe(activationPath))
			{
				yield return new FileItemState
				{
					Name = GetFileSystemDisplayName(directory),
					Glyph = "\uE8B7",
					IconSource = includeIcons ? ShellIconCache.GetFolderIcon(directory) : null,
					ItemType = "Folder",
					Modified = string.Empty,
					Size = string.Empty,
					ActivationPath = NormalizeFileSystemPath(directory),
					IsFolder = true
				};
			}

			foreach (var file in EnumerateFilesSafe(activationPath))
			{
				yield return new FileItemState
				{
					Name = Path.GetFileName(file),
					Glyph = "\uE8A5",
					IconSource = includeIcons ? ShellIconCache.GetFileIcon(file) : null,
					ItemType = BuildFileTypeLabel(file),
					Modified = omitSlowMetadata ? string.Empty : FormatTimestamp(GetFileWriteTime(file)),
					Size = omitSlowMetadata ? string.Empty : FormatFileSize(GetFileSize(file)),
					ActivationPath = file,
					IsFolder = false
				};
			}
		}

		private void InitializeFolderPane()
		{
			FolderPane.Add(new FolderPaneItemState
			{
				Title = "Home",
				Glyph = "\uE80F",
				ActivationPath = HomeActivationPath,
				CanExpand = false,
				IsExpanded = false,
				Depth = 0
			});

			var quickAccessLocations = BuildKnownFolderDefinitions().ToList();

			if (quickAccessLocations.Count > 0)
			{
				FolderPane.Add(new FolderPaneItemState
				{
					Title = "Quick access",
					IsHeader = true
				});

				foreach (var pinnedLocation in quickAccessLocations)
				{
					FolderPane.Add(new FolderPaneItemState
					{
						Title = pinnedLocation.Title,
						Glyph = pinnedLocation.Glyph,
						ActivationPath = pinnedLocation.ActivationPath,
						IconSource = IsFileSystemPath(pinnedLocation.ActivationPath)
							? ShellIconCache.GetFolderIcon(pinnedLocation.ActivationPath)
							: null,
						CanExpand = IsQuickAccessFolderExpandable(pinnedLocation.ActivationPath),
						IsExpanded = false,
						Depth = 0
					});
				}
			}

			FolderPane.Add(new FolderPaneItemState
			{
				Title = "Devices and drives",
				IsHeader = true
			});

			FolderPane.Add(new FolderPaneItemState
			{
				Title = "This PC",
				Glyph = "\uE7F8",
				ActivationPath = ThisPcActivationPath,
				CanExpand = true,
				IsExpanded = false,
				Depth = 0
			});
		}

		private void EnsureFolderPaneVisibleForPath(string activationPath)
		{
			if (IsHomeLocation(activationPath))
			{
				return;
			}

			if (IsThisPcLocation(activationPath))
			{
				return;
			}

			if (!IsFileSystemPath(activationPath))
			{
				return;
			}

			var quickAccessItem = FindQuickAccessBranchRoot(activationPath);

			if (quickAccessItem != null)
			{
				ExpandBranchToPath(quickAccessItem, activationPath);
				return;
			}

			var thisPcItem = FindFolderPaneItem(ThisPcActivationPath);

			if (thisPcItem == null)
			{
				return;
			}

			ExpandBranchToPath(thisPcItem, activationPath);
		}

		private FolderPaneItemState? FindQuickAccessBranchRoot(string activationPath)
		{
			return FolderPane
				.Where(item => !item.IsHeader && item.Depth == 0 && IsFileSystemPath(item.ActivationPath)
					&& IsFileSystemBranch(activationPath, item.ActivationPath))
				.OrderByDescending(item => item.ActivationPath.Length)
				.FirstOrDefault();
		}

		private void ExpandBranchToPath(FolderPaneItemState rootItem, string activationPath)
		{
			if (!PathsEqual(rootItem.ActivationPath, activationPath) && rootItem.CanExpand)
			{
				ExpandFolderInPane(rootItem);
			}

			var ancestors = new Stack<string>();
			var currentPath = NormalizeFileSystemPath(activationPath);

			while (!string.IsNullOrEmpty(currentPath) && !PathsEqual(currentPath, rootItem.ActivationPath))
			{
				ancestors.Push(currentPath);
				var parentPath = GetParentDirectoryPath(currentPath);

				if (parentPath == null || !IsFileSystemBranch(activationPath, parentPath))
				{
					break;
				}

				currentPath = parentPath;
			}

			FolderPaneItemState? parentItem = rootItem;

			while (ancestors.Count > 0)
			{
				var path = ancestors.Pop();
				var pathItem = FindFolderPaneItem(path);

				if (pathItem == null && parentItem != null)
				{
					ExpandFolderInPane(parentItem);
					pathItem = FindFolderPaneItem(path);
				}

				if (pathItem == null)
				{
					break;
				}

				if (ancestors.Count > 0)
				{
					ExpandFolderInPane(pathItem);
				}

				parentItem = pathItem;
			}
		}

		private bool IsQuickAccessFolderExpandable(string activationPath)
		{
			return IsFileSystemPath(activationPath)
				&& (HasChildDirectories(activationPath) || IsFileSystemBranch(m_currentActivationPath, activationPath));
		}

		private FolderPaneItemState? FindFolderPaneItem(string activationPath)
		{
			return FolderPane.FirstOrDefault(item => !item.IsHeader && PathsEqual(item.ActivationPath, activationPath));
		}

		private void ExpandFolderInPane(FolderPaneItemState folder)
		{
			if (folder.IsExpanded)
			{
				m_expandedPaths.Add(folder.ActivationPath);
				return;
			}

			var childItems = BuildChildFolderItems(folder).ToList();

			if (childItems.Count == 0)
			{
				folder.CanExpand = false;
				folder.IsExpanded = false;
				m_expandedPaths.Remove(folder.ActivationPath);
				return;
			}

			var insertIndex = FolderPane.IndexOf(folder) + 1;

			foreach (var child in childItems)
			{
				FolderPane.Insert(insertIndex++, child);
			}

			folder.IsExpanded = true;
			m_expandedPaths.Add(folder.ActivationPath);
		}

		private void CollapseFolderInPane(FolderPaneItemState folder)
		{
			var folderIndex = FolderPane.IndexOf(folder);

			if (folderIndex < 0)
			{
				return;
			}

			folder.IsExpanded = false;
			m_expandedPaths.Remove(folder.ActivationPath);

			for (int index = folderIndex + 1; index < FolderPane.Count;)
			{
				if (FolderPane[index].Depth <= folder.Depth)
				{
					break;
				}

				FolderPane.RemoveAt(index);
			}
		}

		private IEnumerable<FolderPaneItemState> BuildChildFolderItems(FolderPaneItemState parent)
		{
			if (IsThisPcLocation(parent.ActivationPath))
			{
				foreach (var drive in GetSortedDrives())
				{
					yield return new FolderPaneItemState
					{
						Title = BuildDriveTitle(drive),
						Glyph = "\uEDA2",
						ActivationPath = NormalizeFileSystemPath(drive.RootDirectory.FullName),
						IconSource = ShellIconCache.GetDriveIcon(drive.RootDirectory.FullName),
						CanExpand = true,
						IsExpanded = false,
						Depth = parent.Depth + 1
					};
				}

				yield break;
			}

			if (!IsFileSystemPath(parent.ActivationPath))
			{
				yield break;
			}

			foreach (var childDirectory in EnumerateDirectoriesSafe(parent.ActivationPath))
			{
				yield return new FolderPaneItemState
				{
					Title = GetFileSystemDisplayName(childDirectory),
					Glyph = "\uE8B7",
					ActivationPath = NormalizeFileSystemPath(childDirectory),
					IconSource = ShellIconCache.GetFolderIcon(childDirectory),
					CanExpand = true,
					IsExpanded = false,
					Depth = parent.Depth + 1
				};
			}
		}

		private void EnsureCurrentPathExpanded(string activationPath)
		{
			if (IsThisPcLocation(activationPath))
			{
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
			var driveName = TrimTrailingSeparators(drive.Name);
			var driveDisplay = driveName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var driveLabel = TryGetDriveVolumeLabel(drive);

			if (string.IsNullOrWhiteSpace(driveLabel))
			{
				return driveDisplay;
			}

			return $"{driveLabel} ({driveDisplay})";
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
			if (drive.DriveType == DriveType.Network)
			{
				return string.Empty;
			}

			try
			{
				if (!drive.IsReady)
				{
					return string.Empty;
				}

				return $"{FormatFileSize(drive.AvailableFreeSpace)} free of {FormatFileSize(drive.TotalSize)}";
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string? TryGetDriveVolumeLabel(DriveInfo drive)
		{
			if (drive.DriveType == DriveType.Network)
			{
				return null;
			}

			try
			{
				if (!drive.IsReady)
				{
					return null;
				}

				return string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
			}
			catch
			{
				return null;
			}
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

		private static IEnumerable<NavigationSegmentState> BuildNavigationSegments(string activationPath)
		{
			if (IsHomeLocation(activationPath))
			{
				yield return new NavigationSegmentState
				{
					Glyph = "\uE80F"
				};

				yield return new NavigationSegmentState
				{
					Text = "Home",
					ShowSeparator = true
				};
				yield break;
			}

			yield return new NavigationSegmentState
			{
				Glyph = "\uE7F8"
			};

			if (IsThisPcLocation(activationPath))
			{
				yield return new NavigationSegmentState
				{
					Text = "This PC",
					ShowSeparator = true
				};
				yield break;
			}

			if (!IsFileSystemPath(activationPath))
			{
				yield return new NavigationSegmentState
				{
					Text = GetDisplayTitle(activationPath),
					ShowSeparator = true
				};
				yield break;
			}

			var normalizedPath = NormalizeFileSystemPath(activationPath);
			var knownFolder = FindKnownFolderDefinition(normalizedPath);

			if (!string.IsNullOrWhiteSpace(knownFolder.ActivationPath))
			{
				yield return new NavigationSegmentState
				{
					Text = knownFolder.Title,
					ShowSeparator = true
				};

				foreach (var segment in BuildRelativeNavigationSegments(normalizedPath, knownFolder.ActivationPath))
				{
					yield return segment;
				}

				yield break;
			}

			yield return new NavigationSegmentState
			{
				Text = "This PC",
				ShowSeparator = true
			};

			var rootPath = Path.GetPathRoot(normalizedPath);

			if (string.IsNullOrWhiteSpace(rootPath))
			{
				yield break;
			}

			var normalizedRootPath = NormalizeFileSystemPath(rootPath);
			yield return new NavigationSegmentState
			{
				Text = GetDriveBreadcrumbTitle(normalizedRootPath),
				ShowSeparator = true
			};

			foreach (var segment in BuildRelativeNavigationSegments(normalizedPath, normalizedRootPath))
			{
				yield return segment;
			}
		}

		private static (string Title, string ActivationPath, string Glyph) FindKnownFolderDefinition(string activationPath)
		{
			return BuildKnownFolderDefinitions()
				.Where(definition => IsFileSystemBranch(activationPath, definition.ActivationPath))
				.OrderByDescending(definition => definition.ActivationPath.Length)
				.FirstOrDefault();
		}

		private static IEnumerable<NavigationSegmentState> BuildRelativeNavigationSegments(string fullPath,
			string rootPath)
		{
			var relativePath = Path.GetRelativePath(rootPath, fullPath);

			if (string.IsNullOrWhiteSpace(relativePath) || PathsEqual(relativePath, "."))
			{
				yield break;
			}

			foreach (var segmentText in relativePath.Split(
				new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
				StringSplitOptions.RemoveEmptyEntries))
			{
				yield return new NavigationSegmentState
				{
					Text = segmentText,
					ShowSeparator = true
				};
			}
		}

		private static string GetDriveBreadcrumbTitle(string drivePath)
		{
			try
			{
				return BuildDriveTitle(new DriveInfo(drivePath));
			}
			catch
			{
				return GetFileSystemDisplayName(drivePath);
			}
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

		private static bool IsSlowFileSystemLocation(string activationPath)
		{
			if (!IsFileSystemPath(activationPath))
			{
				return false;
			}

			var root = Path.GetPathRoot(NormalizeFileSystemPath(activationPath));

			if (string.IsNullOrWhiteSpace(root))
			{
				return false;
			}

			try
			{
				return new DriveInfo(root).DriveType == DriveType.Network;
			}
			catch
			{
				return false;
			}
		}
	}
}