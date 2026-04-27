using ExplorerPlusPlus.WinUIHost.Models;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace ExplorerPlusPlus.WinUIHost.ViewModels
{
	public sealed class ShellRootViewModel
	{
		public ObservableCollection<TabState> Tabs { get; }
		public NavigationState Navigation { get; }
		public ObservableCollection<FolderPaneItemState> FolderPane { get; }
		public ObservableCollection<FileItemState> Files { get; }

		public TabState? SelectedTab { get; set; }
		public FolderPaneItemState? SelectedFolder { get; set; }

		public ShellRootViewModel()
		{
			Tabs = new ObservableCollection<TabState>
			{
				new TabState { Title = "Home", Glyph = "\uE80F" },
				new TabState { Title = "Documents", Glyph = "\uE8B7" },
				new TabState { Title = "Designs", Glyph = "\uE8A5" }
			};

			SelectedTab = Tabs[1];

			Navigation = new NavigationState
			{
				CurrentPath = @"%userprofile%\Documents",
				CanGoBack = true,
				CanGoForward = false,
				CanGoUp = true,
				CanRefresh = true,
				IsNavigating = false
			};

			FolderPane = new ObservableCollection<FolderPaneItemState>
			{
				new FolderPaneItemState { Title = "Home", Glyph = "\uE80F", ActivationPath = "shell:::{f874310e-b6b7-47dc-bc84-b9e6b38f5903}", CanExpand = false, IsExpanded = false, IndentMargin = new Thickness(0, 0, 0, 0) },
				new FolderPaneItemState { Title = "This PC", Glyph = "\uE7F8", ActivationPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", CanExpand = true, IsExpanded = true, IndentMargin = new Thickness(0, 0, 0, 0) },
				new FolderPaneItemState { Title = "OS (C:)", Glyph = "\uEDA2", ActivationPath = @"C:\", CanExpand = true, IsExpanded = true, IndentMargin = new Thickness(18, 0, 0, 0) },
				new FolderPaneItemState { Title = "Users", Glyph = "\uE8B7", ActivationPath = @"C:\Users", CanExpand = true, IsExpanded = true, IndentMargin = new Thickness(36, 0, 0, 0) },
				new FolderPaneItemState { Title = "User", Glyph = "\uE8B7", ActivationPath = @"%userprofile%", CanExpand = true, IsExpanded = true, IndentMargin = new Thickness(54, 0, 0, 0) },
				new FolderPaneItemState { Title = "Documents", Glyph = "\uE8B7", ActivationPath = @"%userprofile%\Documents", CanExpand = true, IsExpanded = false, IndentMargin = new Thickness(72, 0, 0, 0) }
			};

			SelectedFolder = FolderPane[FolderPane.Count - 1];

			Files = new ObservableCollection<FileItemState>
			{
				new FileItemState { Name = "Explorer++", Glyph = "\uE8B7", ItemType = "Folder", Modified = "Today, 10:42", Size = "" },
				new FileItemState { Name = "roadmap.md", Glyph = "\uE8A5", ItemType = "Markdown", Modified = "Today, 09:15", Size = "6 KB" },
				new FileItemState { Name = "build-release.ps1", Glyph = "\uE8A5", ItemType = "PowerShell", Modified = "Today, 08:58", Size = "2 KB" },
				new FileItemState { Name = "release", Glyph = "\uE838", ItemType = "Folder", Modified = "Yesterday, 22:11", Size = "" }
			};
		}
	}
}