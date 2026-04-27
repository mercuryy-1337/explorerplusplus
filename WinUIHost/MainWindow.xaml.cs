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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
	[SupportedOSPlatform("windows")]
	public sealed partial class MainWindow : Window
	{
		private const string AppDisplayName = "ExplorerX";
		private const uint WmSetIcon = 0x0080;
		private static readonly IntPtr IconSmall = IntPtr.Zero;
		private static readonly IntPtr IconBig = new(1);
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
			var hwnd = WindowNative.GetWindowHandle(this);
			ApplyWindowIcon(hwnd);

			if (!AppWindowTitleBar.IsCustomizationSupported())
			{
				return;
			}

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
			App.AppendLog(message);
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

		private static Border? GetFileListHeaderBackground(Button button)
		{
			if (button.Parent is Grid grid && grid.Children.Count > 0 && grid.Children[0] is Border background)
			{
				return background;
			}

			return null;
		}

		private static void SetFileListHeaderButtonBrush(Button button, string resourceKey)
		{
			if (GetFileListHeaderBackground(button) is Border background)
			{
				background.Background = ResolveThemeBrush(resourceKey);
			}
		}

		private static void ResetFileListHeaderButtonBrush(Button button)
		{
			if (GetFileListHeaderBackground(button) is Border background)
			{
				background.Background = s_transparentBrush;
			}
		}

		private void FileListHeaderButton_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button && button.IsEnabled)
			{
				SetFileListHeaderButtonBrush(button, "ShellNavButtonHoverBrush");
			}
		}

		private void FileListHeaderButton_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button)
			{
				ResetFileListHeaderButtonBrush(button);
			}
		}

		private void FileListHeaderButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button && button.IsEnabled)
			{
				SetFileListHeaderButtonBrush(button, "ShellNavButtonPressedBrush");
			}
		}

		private void FileListHeaderButton_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button && button.IsEnabled)
			{
				if (button.IsPointerOver)
				{
					SetFileListHeaderButtonBrush(button, "ShellNavButtonHoverBrush");
				}
				else
				{
					ResetFileListHeaderButtonBrush(button);
				}
			}
		}

		private void FileListHeaderButton_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button)
			{
				ResetFileListHeaderButtonBrush(button);
			}
		}

		private void FileListHeaderButton_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Button button)
			{
				ResetFileListHeaderButtonBrush(button);
			}
		}

		private static void SetThisPcDriveTileBrush(Border border, string resourceKey)
		{
			border.Background = ResolveThemeBrush(resourceKey);
		}

		private static void ResetThisPcDriveTileBrush(Border border)
		{
			border.Background = s_transparentBrush;
		}

		private void ThisPcDriveTile_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
			{
				SetThisPcDriveTileBrush(border, "ShellNavButtonHoverBrush");
			}
		}

		private void ThisPcDriveTile_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
			{
				ResetThisPcDriveTileBrush(border);
			}
		}

		private void ThisPcDriveTile_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
			{
				SetThisPcDriveTileBrush(border, "ShellNavButtonPressedBrush");
			}
		}

		private void ThisPcDriveTile_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
			{
				var point = e.GetCurrentPoint(border).Position;

				if (point.X >= 0 && point.X <= border.ActualWidth
					&& point.Y >= 0 && point.Y <= border.ActualHeight)
				{
					SetThisPcDriveTileBrush(border, "ShellNavButtonHoverBrush");
				}
				else
				{
					ResetThisPcDriveTileBrush(border);
				}
			}
		}

		private void ThisPcDriveTile_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
			{
				ResetThisPcDriveTileBrush(border);
			}
		}

		private void ThisPcDriveTile_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
			{
				ResetThisPcDriveTileBrush(border);
			}
		}

		private static void ApplyWindowIcon(IntPtr hwnd)
		{
			if (string.IsNullOrWhiteSpace(Environment.ProcessPath))
			{
				return;
			}

			try
			{
				var iconCount = ExtractIconEx(Environment.ProcessPath, 0, out var largeIcon, out var smallIcon, 1);

				if (iconCount == 0)
				{
					return;
				}

				if (smallIcon != IntPtr.Zero)
				{
					SendMessage(hwnd, WmSetIcon, IconSmall, smallIcon);
				}

				if (largeIcon != IntPtr.Zero)
				{
					SendMessage(hwnd, WmSetIcon, IconBig, largeIcon);
				}
			}
			catch
			{
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

		private void ShowFolderProperties(string folderPath)
		{
			if (!Directory.Exists(folderPath) && !File.Exists(folderPath))
			{
				return;
			}

			var windowHandle = WindowNative.GetWindowHandle(this);

			if (TryShowPropertiesWithDataObject(folderPath, windowHandle))
			{
				return;
			}

			if (TryShowPropertiesWithPath(folderPath, windowHandle))
			{
				return;
			}

			TryShowPropertiesWithShellVerb(folderPath, windowHandle);
		}

		private static bool TryShowPropertiesWithDataObject(string path, IntPtr windowHandle)
		{
			IShellFolder? desktopFolder = null;
			object? dataObject = null;
			IntPtr itemIdList = IntPtr.Zero;

			try
			{
				int hr = SHGetDesktopFolder(out desktopFolder);
				if (hr < 0 || desktopFolder == null)
				{
					return false;
				}

				uint eaten = 0;
				uint attributes = 0;
				hr = desktopFolder.ParseDisplayName(windowHandle, IntPtr.Zero, path, ref eaten, out itemIdList, ref attributes);
				if (hr < 0 || itemIdList == IntPtr.Zero)
				{
					return false;
				}

				var itemIdLists = new[] { itemIdList };
				Guid dataObjectId = typeof(IDataObject).GUID;
				hr = desktopFolder.GetUIObjectOf(windowHandle, (uint)itemIdLists.Length, itemIdLists, ref dataObjectId, IntPtr.Zero, out dataObject);
				if (hr < 0 || dataObject is not IDataObject shellDataObject)
				{
					return false;
				}

				return SHMultiFileProperties(shellDataObject, 0) >= 0;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (dataObject != null && Marshal.IsComObject(dataObject))
				{
					Marshal.ReleaseComObject(dataObject);
				}

				if (desktopFolder != null && Marshal.IsComObject(desktopFolder))
				{
					Marshal.ReleaseComObject(desktopFolder);
				}

				if (itemIdList != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(itemIdList);
				}
			}
		}

		private static bool TryShowPropertiesWithPath(string path, IntPtr windowHandle)
		{
			try
			{
				return SHObjectProperties(windowHandle, ShopFilePath, path, null);
			}
			catch
			{
				return false;
			}
		}

		private static bool TryShowPropertiesWithShellVerb(string path, IntPtr windowHandle)
		{
			try
			{
				var executeInfo = new ShellExecuteInfo
				{
					cbSize = Marshal.SizeOf<ShellExecuteInfo>(),
					fMask = SeeMaskInvokeIdList | SeeMaskFlagNoUi,
					hwnd = windowHandle,
					lpVerb = "properties",
					lpFile = path,
					nShow = SwShownormal
				};

				return ShellExecuteEx(ref executeInfo);
			}
			catch
			{
				return false;
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
				FocusCurrentItemsView();
				e.Handled = true;
			}
		}

		private void FocusCurrentItemsView()
		{
			if (ThisPcDrivesGridView.Visibility == Visibility.Visible)
			{
				ThisPcDrivesGridView.Focus(FocusState.Programmatic);
				return;
			}

			FilesListView.Focus(FocusState.Programmatic);
		}

		private void FilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FileItemState item)
			{
				ViewModel.ActivateFileItemCommand.Execute(item);
			}
		}

		private const uint SeeMaskInvokeIdList = 0x0000000C;
		private const uint SeeMaskFlagNoUi = 0x00000400;
		private const uint ShopFilePath = 0x00000002;
		private const int SwShownormal = 1;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

		[DllImport("shell32.dll")]
		private static extern int SHGetDesktopFolder([MarshalAs(UnmanagedType.Interface)] out IShellFolder shellFolder);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint ExtractIconEx(string fileName, int iconIndex, out IntPtr largeIcon,
			out IntPtr smallIcon, uint iconCount);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool ShellExecuteEx(ref ShellExecuteInfo executeInfo);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern int SHMultiFileProperties([MarshalAs(UnmanagedType.Interface)] IDataObject dataObject, uint flags);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, string objectName, string? propertyPage);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct ShellExecuteInfo
		{
			public int cbSize;
			public uint fMask;
			public IntPtr hwnd;
			public string? lpVerb;
			public string? lpFile;
			public string? lpParameters;
			public string? lpDirectory;
			public int nShow;
			public IntPtr hInstApp;
			public IntPtr lpIDList;
			public string? lpClass;
			public IntPtr hkeyClass;
			public uint dwHotKey;
			public IntPtr hIconOrMonitor;
			public IntPtr hProcess;
		}

		[ComImport]
		[Guid("000214E6-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellFolder
		{
			[PreserveSig]
			int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string displayName,
				ref uint eaten, out IntPtr itemIdList, ref uint attributes);

			[PreserveSig]
			int EnumObjects(IntPtr hwnd, int flags, out IntPtr enumIdList);

			[PreserveSig]
			int BindToObject(IntPtr itemIdList, IntPtr pbc, ref Guid interfaceId, out IntPtr shellObject);

			[PreserveSig]
			int BindToStorage(IntPtr itemIdList, IntPtr pbc, ref Guid interfaceId, out IntPtr shellStorage);

			[PreserveSig]
			int CompareIDs(IntPtr lParam, IntPtr firstItemIdList, IntPtr secondItemIdList);

			[PreserveSig]
			int CreateViewObject(IntPtr hwndOwner, ref Guid interfaceId, out IntPtr viewObject);

			[PreserveSig]
			int GetAttributesOf(uint itemCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] itemIdLists,
				ref uint attributes);

			[PreserveSig]
			int GetUIObjectOf(IntPtr hwndOwner, uint itemCount,
				[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] itemIdLists,
				ref Guid interfaceId, IntPtr reserved,
				[MarshalAs(UnmanagedType.IUnknown)] out object shellObject);

			[PreserveSig]
			int GetDisplayNameOf(IntPtr itemIdList, uint flags, out IntPtr name);

			[PreserveSig]
			int SetNameOf(IntPtr hwnd, IntPtr itemIdList, [MarshalAs(UnmanagedType.LPWStr)] string name,
				uint flags, out IntPtr outputItemIdList);
		}
	}
}