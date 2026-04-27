using ExplorerPlusPlus.WinUIHost.Models;
using ExplorerPlusPlus.WinUIHost.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Windows.System;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace ExplorerPlusPlus.WinUIHost
{
	public sealed partial class MainWindow : Window
	{
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
				AppendLog("InitializeComponent complete");
				Title = "Explorer++ WinUI Host";
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