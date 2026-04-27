// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using ExplorerPlusPlus.WinUIHost.Controls;
using ExplorerPlusPlus.WinUIHost.Models;
using ExplorerPlusPlus.WinUIHost.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.Storage;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace ExplorerPlusPlus.WinUIHost
{
	public sealed partial class MainWindow : Window
	{
		private const string AppDisplayName = "ExplorerX";
		private static readonly string LogPath = Path.Combine(
			Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory,
			"startup.log");
		private static readonly Brush s_transparentBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

		private AppWindow? m_appWindow;
		private readonly UISettings m_uiSettings = new();
		private ShellRootViewModel ViewModel { get; }

		public MainWindow()
		{
			try
			{
				AppendLog("MainWindow constructor start");
				InitializeComponent();
				m_uiSettings.ColorValuesChanged += OnSystemColorValuesChanged;
				ApplyCurrentThemeState();
				ConfigureWindowChrome();
				ConfigureAddressPathTextBox();
				AppendLog("InitializeComponent complete");
				Title = AppDisplayName;
				ViewModel = new ShellRootViewModel();
				AppendLog("ShellRootViewModel created");
				RootLayout.DataContext = ViewModel;
				RefreshNavToolbarButtonVisuals();
				AppendLog("DataContext assigned");
			}
			catch (Exception ex)
			{
				AppendExceptionDetails("MainWindow constructor exception", ex);
				throw;
			}
		}

		private void ApplySystemTheme()
		{
			RootLayout.RequestedTheme = IsSystemDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
		}

		private void ApplyCurrentThemeState()
		{
			ApplySystemTheme();

			if (m_appWindow != null)
			{
				ApplyTitleBarButtonColors();
			}

			RefreshNavToolbarButtonVisuals();
		}

		private void OnSystemColorValuesChanged(UISettings sender, object args)
		{
			DispatcherQueue.TryEnqueue(() => ApplyCurrentThemeState());
		}

		private void RefreshNavToolbarButtonVisuals()
		{
			UpdateNavToolbarButtonVisual(BackButton);
			UpdateNavToolbarButtonVisual(ForwardButton);
			UpdateNavToolbarButtonVisual(UpButton);
			UpdateNavToolbarButtonVisual(RefreshButton);
		}

		private void ConfigureAddressPathTextBox()
		{
			AddressPathTextBox.UseSystemFocusVisuals = false;
			AddressPathTextBox.Background = s_transparentBrush;
			AddressPathTextBox.BorderBrush = s_transparentBrush;
			AddressPathTextBox.Resources["TextControlBackground"] = s_transparentBrush;
			AddressPathTextBox.Resources["TextControlBackgroundPointerOver"] = s_transparentBrush;
			AddressPathTextBox.Resources["TextControlBackgroundFocused"] = s_transparentBrush;
			AddressPathTextBox.Resources["TextControlBorderBrush"] = s_transparentBrush;
			AddressPathTextBox.Resources["TextControlBorderBrushPointerOver"] = s_transparentBrush;
			AddressPathTextBox.Resources["TextControlBorderBrushFocused"] = s_transparentBrush;
		}

		private void ConfigureWindowChrome()
		{
			if (!AppWindowTitleBar.IsCustomizationSupported())
			{
				return;
			}

			var hwnd = WindowNative.GetWindowHandle(this);
			var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
			m_appWindow = AppWindow.GetFromWindowId(windowId);

			try
			{
				SystemBackdrop = new DesktopAcrylicBackdrop();
			}
			catch
			{
				SystemBackdrop = new MicaBackdrop();
			}
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);
			UpdateTitleBarInsets();
			ApplyTitleBarButtonColors();
		}

		private void UpdateTitleBarInsets()
		{
			if (m_appWindow == null)
			{
				return;
			}

			TitleBarLeftInsetColumn.Width = new GridLength(Math.Max(12, m_appWindow.TitleBar.LeftInset));
			TitleBarRightInsetColumn.Width = new GridLength(Math.Max(138, m_appWindow.TitleBar.RightInset));
		}

		private void ApplyTitleBarButtonColors()
		{
			if (m_appWindow == null)
			{
				return;
			}

			bool darkTheme = IsSystemDarkTheme();
			var titleBar = m_appWindow.TitleBar;

			if (darkTheme)
			{
				titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
				titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
				titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 243, 245, 247);
				titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 152, 162, 175);
				titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 42, 48, 56);
				titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 54, 61, 71);
				titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 243, 245, 247);
				titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
			}
			else
			{
				titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
				titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
				titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 23, 24, 26);
				titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 112, 117, 127);
				titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 232, 235, 240);
				titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 222, 226, 232);
				titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 23, 24, 26);
				titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 23, 24, 26);
			}
		}

		[SupportedOSPlatform("windows")]
		private static bool IsSystemDarkTheme()
		{
			object? value = Registry.GetValue(
				@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
				"AppsUseLightTheme",
				1);

			return value is int intValue && intValue == 0;
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

		private static Brush ResolveThemeBrush(string key)
		{
			if (Application.Current.Resources.TryGetValue(key, out var resource)
				&& resource is Brush brush)
			{
				return brush;
			}

			return s_transparentBrush;
		}

		private static void SetNavToolbarButtonBrush(Button button, string resourceKey)
		{
			button.Background = ResolveThemeBrush(resourceKey);
		}

		private static void UpdateNavToolbarButtonVisual(Button button)
		{
			button.Foreground = ResolveThemeBrush(button.IsEnabled ? "ShellTextBrush" : "ShellSecondaryTextBrush");

			if (!button.IsEnabled)
			{
				ResetNavToolbarButtonBrush(button);
			}
		}

		private static void ResetNavToolbarButtonBrush(Button button)
		{
			button.Background = s_transparentBrush;
		}

		private void NavToolbarButton_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is Button button)
			{
				UpdateNavToolbarButtonVisual(button);
			}
		}

		private void NavToolbarButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Button button)
			{
				UpdateNavToolbarButtonVisual(button);
			}
		}

		private void NavToolbarButton_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button && button.IsEnabled)
			{
				SetNavToolbarButtonBrush(button, "ShellNavButtonHoverBrush");
			}
		}

		private void NavToolbarButton_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button)
			{
				ResetNavToolbarButtonBrush(button);
			}
		}

		private void NavToolbarButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button && button.IsEnabled)
			{
				SetNavToolbarButtonBrush(button, "ShellNavButtonPressedBrush");
			}
		}

		private void NavToolbarButton_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button && button.IsEnabled)
			{
				if (button.IsPointerOver)
				{
					SetNavToolbarButtonBrush(button, "ShellNavButtonHoverBrush");
				}
				else
				{
					ResetNavToolbarButtonBrush(button);
				}
			}
		}

		private void NavToolbarButton_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button)
			{
				ResetNavToolbarButtonBrush(button);
			}
		}

		private void NavToolbarButton_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button)
			{
				ResetNavToolbarButtonBrush(button);
			}
		}

		private void FoldersPaneListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FolderPaneItemState folder)
			{
				ViewModel.SelectFolderCommand.Execute(folder);
				Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(
					() => ViewModel.ActivateFolderCommand.Execute(folder));
			}
		}

		private void FolderPaneChevron_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FolderPaneItemState folder)
			{
				ViewModel.ToggleFolderExpansionCommand.Execute(folder);
				e.Handled = true;
			}
		}

		private void FolderPaneRow_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FolderPaneItemState folder)
			{
				folder.IsPointerOver = true;
			}
		}

		private void FolderPaneRow_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FolderPaneItemState folder)
			{
				folder.IsPointerOver = false;
			}
		}

		private void FolderPaneRow_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FolderPaneItemState folder)
			{
				ViewModel.SelectFolderCommand.Execute(folder);
			}
		}

		private void FolderPaneRow_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement element || element.DataContext is not FolderPaneItemState folder
				|| folder.IsHeader)
			{
				return;
			}

			ViewModel.SelectFolderCommand.Execute(folder);
			var flyout = ShellContextMenuBuilder.Build(CreateFolderPaneContextMenuItems(folder));
			flyout.ShowAt(element, new FlyoutShowOptions
			{
				Position = e.GetPosition(element)
			});
			e.Handled = true;
		}

		private ShellContextMenuItem[] CreateFolderPaneContextMenuItems(FolderPaneItemState folder)
		{
			var hasExistingFolderPath = TryGetExistingFolderPath(folder, out var folderPath);
			var effectiveFolderPath = folderPath ?? string.Empty;

			return new[]
			{
				new ShellContextMenuItem("Expand", () => ViewModel.ToggleFolderExpansionCommand.Execute(folder))
				{
					IsEnabled = folder.CanExpand && !folder.IsExpanded
				},
				ShellContextMenuItem.Separator(),
				new ShellContextMenuItem("Open in CMD", () => OpenFolderInCommandPrompt(effectiveFolderPath))
				{
					IsEnabled = hasExistingFolderPath
				},
				ShellContextMenuItem.Separator(),
				new ShellContextMenuItem("Copy as path", () => CopyTextToClipboard(QuotePath(effectiveFolderPath)))
				{
					IsEnabled = hasExistingFolderPath
				},
				new ShellContextMenuItem("Send to")
				{
					IsEnabled = hasExistingFolderPath,
					Items = new[]
					{
						new ShellContextMenuItem("Coming soon")
						{
							IsEnabled = false
						}
					}
				},
				new ShellContextMenuItem("Copy", () => _ = CopyFolderToClipboardAsync(effectiveFolderPath))
				{
					IsEnabled = hasExistingFolderPath
				},
				new ShellContextMenuItem("New")
				{
					IsEnabled = hasExistingFolderPath,
					Items = new[]
					{
						new ShellContextMenuItem("Folder", () => CreateFolder(folder, effectiveFolderPath))
						{
							IsEnabled = hasExistingFolderPath
						}
					}
				},
				ShellContextMenuItem.Separator(),
				new ShellContextMenuItem("Properties", () => ShowFolderProperties(effectiveFolderPath))
				{
					IsEnabled = hasExistingFolderPath
				}
			};
		}

		private static bool TryGetExistingFolderPath(FolderPaneItemState folder, out string? folderPath)
		{
			folderPath = string.IsNullOrWhiteSpace(folder.ActivationPath)
				? null
				: folder.ActivationPath;

			if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
			{
				folderPath = null;
				return false;
			}

			return true;
		}

		private static string QuotePath(string path)
		{
			return $"\"{path}\"";
		}

		private static void OpenFolderInCommandPrompt(string folderPath)
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/K cd /d {QuotePath(folderPath)}",
					UseShellExecute = true,
					WorkingDirectory = folderPath
				});
			}
			catch
			{
			}
		}

		private static void CopyTextToClipboard(string text)
		{
			var package = new DataPackage();
			package.SetText(text);
			Clipboard.SetContent(package);
			Clipboard.Flush();
		}

		private static async Task CopyFolderToClipboardAsync(string folderPath)
		{
			try
			{
				var storageFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
				var package = new DataPackage
				{
					RequestedOperation = DataPackageOperation.Copy
				};

				package.SetStorageItems(new[] { storageFolder });
				Clipboard.SetContent(package);
				Clipboard.Flush();
			}
			catch
			{
			}
		}

		private void CreateFolder(FolderPaneItemState folder, string parentPath)
		{
			try
			{
				var newFolderPath = GetUniqueNewFolderPath(parentPath);
				Directory.CreateDirectory(newFolderPath);
				folder.CanExpand = true;
				ViewModel.RefreshCommand.Execute(null);
			}
			catch
			{
			}
		}

		private static string GetUniqueNewFolderPath(string parentPath)
		{
			const string baseFolderName = "New folder";
			var candidatePath = Path.Combine(parentPath, baseFolderName);
			var suffix = 2;

			while (Directory.Exists(candidatePath) || File.Exists(candidatePath))
			{
				candidatePath = Path.Combine(parentPath, $"{baseFolderName} ({suffix++})");
			}

			return candidatePath;
		}

		private static void ShowFolderProperties(string folderPath)
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = folderPath,
					UseShellExecute = true,
					Verb = "properties"
				});
			}
			catch
			{
			}
		}

		private void AddressBar_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (!ViewModel.Navigation.CanShowPathText || ViewModel.Navigation.IsPathTextVisible)
			{
				return;
			}

			ViewModel.Navigation.IsPathTextVisible = true;
			Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				AddressPathTextBox.Focus(FocusState.Programmatic);
				AddressPathTextBox.SelectAll();
			});
		}

		private void AddressPathTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			ViewModel.Navigation.IsPathTextVisible = false;
		}

		private void AddressPathTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Escape)
			{
				ViewModel.Navigation.IsPathTextVisible = false;
				FilesListView.Focus(FocusState.Programmatic);
				e.Handled = true;
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