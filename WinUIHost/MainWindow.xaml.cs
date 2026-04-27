using ExplorerPlusPlus.WinUIHost.Models;
using ExplorerPlusPlus.WinUIHost.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace ExplorerPlusPlus.WinUIHost
{
	public sealed partial class MainWindow : Window
	{
		private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "startup.log");

		private ShellRootViewModel ViewModel { get; }

		public MainWindow()
		{
			try
			{
				AppendLog("MainWindow constructor start");
				InitializeComponent();
				AppendLog("InitializeComponent complete");
				Title = "Explorer++ WinUI Host";
				ViewModel = new ShellRootViewModel();
				AppendLog("ShellRootViewModel created");
				RootLayout.DataContext = ViewModel;
				AppendLog("DataContext assigned");
			}
			catch (Exception ex)
			{
				AppendExceptionDetails("MainWindow constructor exception", ex);
				throw;
			}
		}

		private static void AppendLog(string message)
		{
			try
			{
				File.AppendAllText(LogPath,
					$"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}",
					Encoding.UTF8);
			}
			catch
			{
			}
		}

		private static void AppendExceptionDetails(string prefix, Exception ex)
		{
			AppendLog($"{prefix}: {ex}");

			try
			{
				foreach (PropertyInfo property in ex.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (property.GetIndexParameters().Length != 0)
					{
						continue;
					}

					object? value;

					try
					{
						value = property.GetValue(ex);
					}
					catch (Exception propertyEx)
					{
						value = $"<error reading property: {propertyEx.Message}>";
					}

					AppendLog($"{prefix} property {property.Name}: {value}");
				}
			}
			catch (Exception reflectionEx)
			{
				AppendLog($"{prefix} reflection failure: {reflectionEx}");
			}
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
			if (sender is ListView listView && listView.SelectedItem is FolderPaneItemState folder)
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