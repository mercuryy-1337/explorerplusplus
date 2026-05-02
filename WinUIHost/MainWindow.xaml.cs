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
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace ExplorerPlusPlus.WinUIHost
{
	[SupportedOSPlatform("windows")]
	public sealed partial class MainWindow : Window
	{
		private const string AppDisplayName = "ExplorerX";
		private const double SelectionMarqueeDragThreshold = 6.0;
		private const uint WmSetIcon = 0x0080;
		private static readonly IntPtr IconSmall = IntPtr.Zero;
		private static readonly IntPtr IconBig = new(1);
		private static readonly Brush s_transparentBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

		private AppWindow? m_appWindow;
		private bool m_isSelectionMarqueeOverlayInitialized;
		private bool m_isUpdatingMarqueeSelection;
		private UIElement? m_selectionMarqueeCaptureElement;
		private bool m_isSelectionMarqueeDragging;
		private Grid m_itemsViewHostGrid = null!;
		private Grid m_itemsViewSelectionOverlay = null!;
		private uint m_selectionMarqueePointerId;
		private Microsoft.UI.Xaml.Shapes.Rectangle m_selectionMarqueeRectangle = null!;
		private Point m_selectionMarqueeStartPoint;
		private ListViewBase? m_selectionMarqueeView;
		private readonly UISettings m_uiSettings = new();
		private ShellRootViewModel ViewModel { get; }

		public MainWindow(string? initialPath = null)
		{
			try
			{
				AppendLog("MainWindow constructor start");
				InitializeComponent();
				AttachFilesViewSelectionHandlers();
				RootLayout.Loaded += RootLayout_Loaded;
				m_uiSettings.ColorValuesChanged += OnSystemColorValuesChanged;
				ApplyCurrentThemeState();
				ConfigureWindowChrome();
				ConfigureAddressPathTextBox();
				AppendLog("InitializeComponent complete");
				Title = AppDisplayName;
				ViewModel = new ShellRootViewModel();
				AppendLog("ShellRootViewModel created");
				RootLayout.DataContext = ViewModel;

				SetupTabView();

				if (!string.IsNullOrWhiteSpace(initialPath))
				{
					RootLayout.Loaded += (_, _) => ViewModel.TryNavigateToPath(initialPath);
				}

				RefreshNavToolbarButtonVisuals();
				AppendLog("DataContext assigned");
			}
			catch (Exception ex)
			{
				AppendExceptionDetails("MainWindow constructor exception", ex);
				throw;
			}
		}

		private void RootLayout_Loaded(object sender, RoutedEventArgs e)
		{
			RootLayout.Loaded -= RootLayout_Loaded;
			EnsureSelectionMarqueeOverlay();
		}

		private void AttachFilesViewSelectionHandlers()
		{
			foreach (var view in new ListViewBase[] { FilesListView, ThisPcDrivesGridView })
			{
				view.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(FilesView_PointerPressed), true);
				view.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(FilesView_PointerMoved), true);
				view.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(FilesView_PointerReleased), true);
				view.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(FilesView_PointerCanceled), true);
				view.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(FilesView_PointerCaptureLost), true);
				view.Tapped += FilesView_Tapped;
				view.ContainerContentChanging += OnFilesViewContainerContentChanging;
				view.AddHandler(UIElement.RightTappedEvent, new RightTappedEventHandler(FilesView_RightTappedBackground), false);
			}
		}

		private void SetupTabView()
		{
			var tabView = new TabView
			{
				IsAddTabButtonVisible = true,
				VerticalAlignment = VerticalAlignment.Bottom
			};

			tabView.SetBinding(TabView.TabItemsSourceProperty, new Binding
			{
				Path = new PropertyPath(nameof(ViewModel.Tabs)),
				Source = ViewModel
			});

			tabView.SelectionChanged += TabView_SelectionChanged;
			tabView.TabCloseRequested += TabView_TabCloseRequested;
			tabView.AddTabButtonClick += TabView_AddTabButtonClick;

			ViewModel.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == nameof(ViewModel.SelectedTab)
					&& !ReferenceEquals(tabView.SelectedItem, ViewModel.SelectedTab))
				{
					tabView.SelectedItem = ViewModel.SelectedTab;
				}
			};

			ApplyTabViewThemeResources(tabView);

			tabView.TabItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				    <TabViewItem Header="{Binding Title}">
				        <TabViewItem.IconSource>
				            <FontIconSource Glyph="{Binding Glyph}" FontFamily="Segoe Fluent Icons" FontSize="14" />
				        </TabViewItem.IconSource>
				    </TabViewItem>
				</DataTemplate>
				""");

			TabViewHost.Children.Add(tabView);
		}

		private static void ApplyTabViewThemeResources(TabView tabView)
		{
			tabView.Resources.ThemeDictionaries.Clear();

			foreach (var themeName in new[] { "Default", "Light", "Dark" })
			{
				var themeDict = new ResourceDictionary();
				var selectedBrush = ResolveThemeBrushForTheme("ShellChromeBrush", themeName);
				var hoverBrush = ResolveThemeBrushForTheme("ShellNavButtonHoverBrush", themeName);
				var pressedBrush = ResolveThemeBrushForTheme("ShellNavButtonPressedBrush", themeName);
				var textBrush = ResolveThemeBrushForTheme("ShellTextBrush", themeName);
				var secondaryTextBrush = ResolveThemeBrushForTheme("ShellSecondaryTextBrush", themeName);
				var borderBrush = ResolveThemeBrushForTheme("ShellBorderBrush", themeName);

				themeDict["TabViewItemHeaderBackground"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
				themeDict["TabViewItemHeaderBackgroundSelected"] = selectedBrush;
				themeDict["TabViewItemHeaderBackgroundPointerOver"] = hoverBrush;
				themeDict["TabViewItemHeaderBackgroundPressed"] = pressedBrush;
				themeDict["TabViewItemHeaderForeground"] = secondaryTextBrush;
				themeDict["TabViewItemHeaderForegroundSelected"] = textBrush;
				themeDict["TabViewItemHeaderIconForeground"] = secondaryTextBrush;
				themeDict["TabViewItemHeaderIconForegroundSelected"] = secondaryTextBrush;
				themeDict["TabViewItemHeaderBorderBrush"] = borderBrush;
				themeDict["TabViewItemHeaderBorderBrushSelected"] = borderBrush;
				themeDict["TabViewItemHeaderBorderBrushPointerOver"] = borderBrush;
				themeDict["TabViewItemSeparatorBrush"] = borderBrush;
				themeDict["TabViewItemHeaderCloseButtonForeground"] = secondaryTextBrush;
				themeDict["TabViewItemHeaderCloseButtonForegroundPointerOver"] = textBrush;
				themeDict["TabViewItemHeaderCloseButtonForegroundPressed"] = textBrush;
				themeDict["TabViewAddButtonForeground"] = textBrush;
				tabView.Resources.ThemeDictionaries[themeName] = themeDict;
			}
		}

		private static Brush ResolveThemeBrushForTheme(string key, string themeName)
		{
			var resources = Application.Current?.Resources;
			if (resources == null) return s_transparentBrush;

			if (resources.ThemeDictionaries.TryGetValue(themeName, out var dictObj)
				&& dictObj is ResourceDictionary dict
				&& dict.TryGetValue(key, out var resource)
				&& resource is Brush brush)
			{
				return brush;
			}

			if (themeName != "Default"
				&& resources.ThemeDictionaries.TryGetValue("Default", out dictObj)
				&& dictObj is ResourceDictionary defaultDict
				&& defaultDict.TryGetValue(key, out var defaultResource)
				&& defaultResource is Brush defaultBrush)
			{
				return defaultBrush;
			}

			return s_transparentBrush;
		}

		private void OnFilesViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.InRecycleQueue)
			{
				args.ItemContainer.DoubleTapped -= FilesContainer_DoubleTapped;
				args.ItemContainer.RightTapped -= FilesContainer_RightTapped;
			}
			else
			{
				args.ItemContainer.DoubleTapped += FilesContainer_DoubleTapped;
				args.ItemContainer.RightTapped += FilesContainer_RightTapped;
			}
		}

		private void FilesContainer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FileItemState item)
			{
				ViewModel.ActivateFileItemCommand.Execute(item);
				e.Handled = true;
			}
		}

		private void FilesContainer_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FileItemState item
				&& !string.IsNullOrWhiteSpace(item.ActivationPath))
			{
				NativeShellContextMenu.ShowContextMenuAt(item.ActivationPath, element, e.GetPosition(element),
					onNavigate: path => ViewModel.TryNavigateToPath(path),
					onOpenInNewTab: path => ViewModel.OpenNewTabAtPath(path),
				onOpenInNewWindow: path =>
				{
					try
					{
						var processPath = Environment.ProcessPath;
						if (!string.IsNullOrEmpty(processPath))
							Process.Start(new ProcessStartInfo(processPath, $"\"{path}\"") { UseShellExecute = true });
					}
					catch { }
				});
				e.Handled = true;
			}
		}

		private void EnsureSelectionMarqueeOverlay()
		{
			if (m_isSelectionMarqueeOverlayInitialized)
			{
				return;
			}

			var itemsHost = FilesListView.Parent as Grid
				?? throw new InvalidOperationException("The items view host grid was not found.");
			m_itemsViewHostGrid = itemsHost;

			m_selectionMarqueeRectangle = new Microsoft.UI.Xaml.Shapes.Rectangle
			{
				Width = 0,
				Height = 0,
				Fill = GetThemeBrush("ShellSelectionBrush", Windows.UI.Color.FromArgb(96, 0, 120, 215)),
				Opacity = 0.35,
				RadiusX = 6,
				RadiusY = 6,
				Stroke = GetThemeBrush("ShellAccentBrush", Windows.UI.Color.FromArgb(255, 0, 120, 215)),
				StrokeThickness = 1,
				Visibility = Visibility.Collapsed
			};

			var canvas = new Canvas();
			canvas.Children.Add(m_selectionMarqueeRectangle);

			m_itemsViewSelectionOverlay = new Grid
			{
				IsHitTestVisible = false
			};

			Grid.SetRow(m_itemsViewSelectionOverlay, Grid.GetRow(FilesListView));
			m_itemsViewSelectionOverlay.Children.Add(canvas);
			itemsHost.Children.Add(m_itemsViewSelectionOverlay);
			itemsHost.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(FilesView_PointerPressed), true);
			itemsHost.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(FilesView_PointerMoved), true);
			itemsHost.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(FilesView_PointerReleased), true);
			itemsHost.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(FilesView_PointerCanceled), true);
			itemsHost.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(FilesView_PointerCaptureLost), true);
			itemsHost.Tapped += FilesView_Tapped;
			m_isSelectionMarqueeOverlayInitialized = true;
		}

		private static Brush GetThemeBrush(string resourceKey, Windows.UI.Color fallbackColor)
		{
			try
			{
				if (Application.Current.Resources[resourceKey] is Brush brush)
				{
					return brush;
				}
			}
			catch
			{
			}

			return new SolidColorBrush(fallbackColor);
		}

		private void ApplySystemTheme()
		{
			RootLayout.RequestedTheme = IsSystemDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
		}

		private void ApplyCurrentThemeState()
		{
			ApplySystemTheme();
			RefreshAddressPathTextBoxThemeResources();

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
			RefreshAddressPathTextBoxThemeResources();
		}

		private void RefreshAddressPathTextBoxThemeResources()
		{
			var textBrush = ResolveThemeBrush("ShellTextBrush");
			var secondaryTextBrush = ResolveThemeBrush("ShellSecondaryTextBrush");

			AddressPathTextBox.Foreground = textBrush;
			AddressPathTextBox.Resources["TextControlForeground"] = textBrush;
			AddressPathTextBox.Resources["TextControlForegroundPointerOver"] = textBrush;
			AddressPathTextBox.Resources["TextControlForegroundFocused"] = textBrush;
			AddressPathTextBox.Resources["TextControlForegroundPointerOverFocused"] = textBrush;
			AddressPathTextBox.Resources["TextControlForegroundDisabled"] = secondaryTextBrush;
			AddressPathTextBox.Resources["TextControlPlaceholderForeground"] = secondaryTextBrush;
			AddressPathTextBox.Resources["TextControlPlaceholderForegroundFocused"] = secondaryTextBrush;
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

		private Brush ResolveThemeBrush(string key)
		{
			var resources = Application.Current?.Resources;

			if (resources == null)
			{
				return s_transparentBrush;
			}

			var themeKey = RootLayout.ActualTheme switch
			{
				ElementTheme.Dark => "Dark",
				ElementTheme.Light => "Light",
				_ => RootLayout.RequestedTheme switch
				{
					ElementTheme.Dark => "Dark",
					ElementTheme.Light => "Light",
					_ => "Default"
				}
			};

			if (TryResolveThemeBrush(resources, themeKey, key, out var themedBrush))
			{
				return themedBrush;
			}

			if (themeKey != "Default" && TryResolveThemeBrush(resources, "Default", key, out themedBrush))
			{
				return themedBrush;
			}

			if (resources.TryGetValue(key, out var resource)
				&& resource is Brush brush)
			{
				return brush;
			}

			return s_transparentBrush;
		}

		private static bool TryResolveThemeBrush(ResourceDictionary resources, string themeKey, string key,
			out Brush brush)
		{
			if (resources.ThemeDictionaries.TryGetValue(themeKey, out var themedDictionary)
				&& themedDictionary is ResourceDictionary resourceDictionary
				&& resourceDictionary.TryGetValue(key, out var resource)
				&& resource is Brush themedBrush)
			{
				brush = themedBrush;
				return true;
			}

			brush = s_transparentBrush;
			return false;
		}

		private void SetNavToolbarButtonBrush(Button button, string resourceKey)
		{
			button.Background = ResolveThemeBrush(resourceKey);
		}

		private void UpdateNavToolbarButtonVisual(Button button)
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

		private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is TabView tabView && tabView.SelectedItem is TabState tab
				&& !ReferenceEquals(tab, ViewModel.SelectedTab))
			{
				ViewModel.ActivateTab(tab);
			}
		}

		private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			if (args.Item is TabState tab && !ViewModel.CloseTab(tab))
				Close();
		}

		private void TabView_AddTabButtonClick(TabView sender, object args)
		{
			ViewModel.OpenNewTab();
		}

		private void FilesView_RightTappedBackground(object sender, RightTappedRoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(ViewModel.CurrentActivationPath)) return;
			if (sender is not FrameworkElement element) return;

			var flyout = NativeShellContextMenu.ShowBackgroundContextMenuAt(
				ViewModel.CurrentActivationPath, element, e.GetPosition(element),
				onSortColumn: param => ViewModel.SortFilesCommand.Execute(param),
				onSortDirection: ascending => ViewModel.SetSortDirection(ascending),
				onRefresh: () => ViewModel.RefreshCommand.Execute(null),
				currentSortColumn: ViewModel.CurrentSortColumn,
				currentSortAscending: ViewModel.CurrentSortAscending);
		}

		private static Border? GetFileListHeaderBackground(Button button)
		{
			if (button.Parent is Grid grid && grid.Children.Count > 0 && grid.Children[0] is Border background)
			{
				return background;
			}

			return null;
		}

		private void SetFileListHeaderButtonBrush(Button button, string resourceKey)
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

		private void SetThisPcDriveTileBrush(Border border, string resourceKey)
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

		private void FolderPaneExpandZone_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FolderPaneItemState folder)
			{
				ViewModel.SelectFolderCommand.Execute(folder);
				ViewModel.ToggleFolderExpansionCommand.Execute(folder);
				e.Handled = true;
			}
		}

		private void FolderPaneRow_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FolderPaneItemState folder)
			{
				ViewModel.SelectFolderCommand.Execute(folder);
				ViewModel.ActivateFolderCommand.Execute(folder);
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
				var point = e.GetCurrentPoint(element);

				if (point.Properties.PointerUpdateKind.Equals(Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed)
					&& point.Position.X >= folder.ActivateHitAreaMargin.Left)
				{
					ViewModel.SelectFolderCommand.Execute(folder);
					ViewModel.ActivateFolderCommand.Execute(folder);
					e.Handled = true;
					return;
				}

				if (point.Properties.PointerUpdateKind.Equals(Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed))
				{
					ViewModel.SelectFolderCommand.Execute(folder);
				}
			}
		}

		private void FolderPaneRow_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (e.Handled || sender is not FrameworkElement element || element.DataContext is not FolderPaneItemState folder)
			{
				return;
			}

			var point = e.GetCurrentPoint(element);

			if (!point.Properties.PointerUpdateKind.Equals(Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased))
			{
				return;
			}

			if (element is not HoverCursorGrid && point.Position.X < folder.ActivateHitAreaMargin.Left)
			{
				return;
			}

			ViewModel.SelectFolderCommand.Execute(folder);
			ViewModel.ActivateFolderCommand.Execute(folder);
			e.Handled = true;
		}

		private void FolderPaneRow_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement element || element.DataContext is not FolderPaneItemState folder
				|| folder.IsHeader)
			{
				return;
			}

			folder.IsRightClicked = true;

			var flyout = NativeShellContextMenu.ShowContextMenuAt(folder.ActivationPath, element, e.GetPosition(element),
				onNavigate: path => ViewModel.TryNavigateToPath(path),
				onOpenInNewTab: path => ViewModel.OpenNewTabAtPath(path),
				onOpenInNewWindow: path =>
				{
					try
					{
						var processPath = Environment.ProcessPath;
						if (!string.IsNullOrEmpty(processPath))
							Process.Start(new ProcessStartInfo(processPath, $"\"{path}\"") { UseShellExecute = true });
					}
					catch { }
				});
			if (flyout != null)
			{
				flyout.Closed += (_, _) =>
				{
					folder.IsRightClicked = false;
				};
			}
			else
			{
				folder.IsRightClicked = false;
			}
			e.Handled = true;
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
			ViewModel.RestoreNavigationPathText();
			ViewModel.Navigation.IsPathTextVisible = false;
		}

		private void AddressPathTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				if (ViewModel.TryNavigateToPath(AddressPathTextBox.Text))
				{
					ViewModel.Navigation.IsPathTextVisible = false;
					FocusCurrentItemsView();
				}

				e.Handled = true;
				return;
			}

			if (e.Key == VirtualKey.Escape)
			{
				ViewModel.RestoreNavigationPathText();
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

		private void FilesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (m_isUpdatingMarqueeSelection)
			{
				return;
			}

			if (sender is ListViewBase view)
			{
				ViewModel.UpdateSelectedItemsSummary(view.SelectedItems.OfType<FileItemState>());
			}
		}

		private void FilesView_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is not UIElement captureElement)
			{
				return;
			}

			var view = sender as ListViewBase ?? GetActiveFilesView();

			if (view == null)
			{
				return;
			}

			var currentPoint = e.GetCurrentPoint(view);
			var pointerKind = currentPoint.Properties.PointerUpdateKind;
			var hitContainer = GetFilesViewItemContainer(view, e.OriginalSource as DependencyObject);

			if (!pointerKind.Equals(Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed)
				|| hitContainer != null)
			{
				return;
			}

			if (m_selectionMarqueePointerId == e.Pointer.PointerId)
			{
				return;
			}

			BeginSelectionMarquee(view, captureElement, e);

			e.Handled = true;
		}

		private void FilesView_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!IsTrackingSelectionMarquee(sender, e))
			{
				return;
			}

			var selectionRect = BuildSelectionMarqueeRect(e.GetCurrentPoint(m_itemsViewSelectionOverlay).Position);

			if (!m_isSelectionMarqueeDragging)
			{
				if (selectionRect.Width < SelectionMarqueeDragThreshold
					&& selectionRect.Height < SelectionMarqueeDragThreshold)
				{
					return;
				}

				m_isSelectionMarqueeDragging = true;
				m_selectionMarqueeRectangle.Visibility = Visibility.Visible;
			}

			UpdateSelectionMarqueeRectangle(selectionRect);
			ApplySelectionMarqueeToCurrentView(selectionRect);
			e.Handled = true;
		}

		private void FilesView_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (!IsTrackingSelectionMarquee(sender, e))
			{
				return;
			}

			EndSelectionMarquee();
			e.Handled = true;
		}

		private void FilesView_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (!IsTrackingSelectionMarquee(sender, e))
			{
				return;
			}

			CancelSelectionMarquee();
		}

		private void FilesView_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (!IsTrackingSelectionMarquee(sender, e))
			{
				return;
			}

			CancelSelectionMarquee();
		}

		private void FilesView_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var view = sender as ListViewBase ?? GetActiveFilesView();

			if (view == null)
			{
				return;
			}

			if (e.OriginalSource is DependencyObject dependencyObject
				&& GetFilesViewItemContainer(view, dependencyObject) != null)
			{
				return;
			}

			ClearFilesViewSelection(view);
			e.Handled = true;
		}

		private void FilesViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is FileItemState item)
			{
				ViewModel.ActivateFileItemCommand.Execute(item);
				e.Handled = true;
			}
		}

		private void FilesView_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key != VirtualKey.Enter || sender is not Selector selector || selector.SelectedItem is not FileItemState item)
			{
				return;
			}

			ViewModel.ActivateFileItemCommand.Execute(item);
			e.Handled = true;
		}

		private void BeginSelectionMarquee(ListViewBase view, PointerRoutedEventArgs e)
		{
			BeginSelectionMarquee(view, view, e);
		}

		private void BeginSelectionMarquee(ListViewBase view, UIElement captureElement, PointerRoutedEventArgs e)
		{
			EnsureSelectionMarqueeOverlay();
			CancelSelectionMarquee();

			m_selectionMarqueeCaptureElement = captureElement;
			m_selectionMarqueeView = view;
			m_selectionMarqueePointerId = e.Pointer.PointerId;
			m_selectionMarqueeStartPoint = e.GetCurrentPoint(m_itemsViewSelectionOverlay).Position;
			m_isSelectionMarqueeDragging = false;

			HideSelectionMarqueeRectangle();
			ClearFilesViewSelection(view);
			view.Focus(FocusState.Programmatic);
			captureElement.CapturePointer(e.Pointer);
		}

		private bool IsTrackingSelectionMarquee(object sender, PointerRoutedEventArgs e)
		{
			return m_selectionMarqueeView != null
				&& m_selectionMarqueePointerId != 0
				&& e.Pointer.PointerId == m_selectionMarqueePointerId;
		}

		private void EndSelectionMarquee()
		{
			m_selectionMarqueeCaptureElement?.ReleasePointerCaptures();
			ResetSelectionMarqueeState();
		}

		private void CancelSelectionMarquee()
		{
			ResetSelectionMarqueeState();
		}

		private void ResetSelectionMarqueeState()
		{
			m_selectionMarqueeCaptureElement = null;
			m_selectionMarqueeView = null;
			m_selectionMarqueePointerId = 0;
			m_isSelectionMarqueeDragging = false;
			HideSelectionMarqueeRectangle();
		}

		private ListViewBase? GetActiveFilesView()
		{
			if (ThisPcDrivesGridView.Visibility == Visibility.Visible)
			{
				return ThisPcDrivesGridView;
			}

			return FilesListView.Visibility == Visibility.Visible
				? FilesListView
				: null;
		}

		private void HideSelectionMarqueeRectangle()
		{
			m_selectionMarqueeRectangle.Visibility = Visibility.Collapsed;
			m_selectionMarqueeRectangle.Width = 0;
			m_selectionMarqueeRectangle.Height = 0;
			Canvas.SetLeft(m_selectionMarqueeRectangle, 0);
			Canvas.SetTop(m_selectionMarqueeRectangle, 0);
		}

		private void ClearFilesViewSelection(ListViewBase view)
		{
			if (view.SelectedItems.Count == 0)
			{
				return;
			}

			view.SelectedItems.Clear();
			ViewModel.UpdateSelectedItemsSummary(Array.Empty<FileItemState>());
		}

		private void ApplySelectionMarqueeToCurrentView(Rect selectionRect)
		{
			if (m_selectionMarqueeView == null)
			{
				return;
			}

			m_isUpdatingMarqueeSelection = true;

			try
			{
				m_selectionMarqueeView.SelectedItems.Clear();
				var selectedCount = 0;

				foreach (var item in m_selectionMarqueeView.Items.Cast<object>())
				{
					if (m_selectionMarqueeView.ContainerFromItem(item) is not SelectorItem container
						|| container.ActualWidth <= 0
						|| container.ActualHeight <= 0)
					{
						continue;
					}

					var containerRect = container.TransformToVisual(m_itemsViewSelectionOverlay)
						.TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

					if (DoRectsIntersect(selectionRect, containerRect))
					{
						m_selectionMarqueeView.SelectedItems.Add(item);
						selectedCount++;
					}
				}
			}
			finally
			{
				m_isUpdatingMarqueeSelection = false;
			}

			ViewModel.UpdateSelectedItemsSummary(m_selectionMarqueeView.SelectedItems.OfType<FileItemState>());
		}

		private Rect BuildSelectionMarqueeRect(Point currentPoint)
		{
			var left = Math.Min(m_selectionMarqueeStartPoint.X, currentPoint.X);
			var top = Math.Min(m_selectionMarqueeStartPoint.Y, currentPoint.Y);
			var width = Math.Abs(currentPoint.X - m_selectionMarqueeStartPoint.X);
			var height = Math.Abs(currentPoint.Y - m_selectionMarqueeStartPoint.Y);

			return new Rect(left, top, width, height);
		}

		private void UpdateSelectionMarqueeRectangle(Rect selectionRect)
		{
			Canvas.SetLeft(m_selectionMarqueeRectangle, selectionRect.X);
			Canvas.SetTop(m_selectionMarqueeRectangle, selectionRect.Y);
			m_selectionMarqueeRectangle.Width = selectionRect.Width;
			m_selectionMarqueeRectangle.Height = selectionRect.Height;
		}

		private static bool DoRectsIntersect(Rect first, Rect second)
		{
			return first.X < second.X + second.Width
				&& first.X + first.Width > second.X
				&& first.Y < second.Y + second.Height
				&& first.Y + first.Height > second.Y;
		}

		private static SelectorItem? GetFilesViewItemContainer(ListViewBase view, DependencyObject? source)
		{
			while (source != null)
			{
				if (source is SelectorItem selectorItem)
				{
					return selectorItem;
				}

				if (ReferenceEquals(source, view))
				{
					break;
				}

				source = VisualTreeHelper.GetParent(source);
			}

			return null;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint ExtractIconEx(string fileName, int iconIndex, out IntPtr largeIcon,
			out IntPtr smallIcon, uint iconCount);
	}
}
