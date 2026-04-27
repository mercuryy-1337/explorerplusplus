using ExplorerPlusPlus.WinUIHost.Models;
using ExplorerPlusPlus.WinUIHost.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExplorerPlusPlus.WinUIHost
{
	public sealed partial class MainWindow : Window
	{
		private ShellRootViewModel ViewModel { get; }

		public MainWindow()
		{
			InitializeComponent();
			Title = "Explorer++ WinUI Host";
			ViewModel = new ShellRootViewModel();
			RootLayout.DataContext = ViewModel;
		}

		private void FoldersPaneListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FolderPaneItemState folder)
			{
				ViewModel.ActivateFolderCommand.Execute(folder);
			}
		}

		private void FoldersPaneListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (FoldersPaneListView.SelectedItem is FolderPaneItemState folder)
			{
				ViewModel.SelectFolderCommand.Execute(folder);
			}
		}

		private void FilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FileItemState item)
			{
				ViewModel.ActivateFileItemCommand.Execute(item);
			}
		}
	}
}