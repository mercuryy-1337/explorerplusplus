// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace ExplorerPlusPlus.WinUIHost.Controls
{
	internal sealed class ShellContextMenuItem
	{
		public ShellContextMenuItem(string text, Action? invoked = null)
		{
			Text = text;
			Invoked = invoked;
		}

		private ShellContextMenuItem()
		{
			Text = string.Empty;
		}

		public string Text { get; }
		public Action? Invoked { get; init; }
		public bool IsEnabled { get; init; } = true;
		public IReadOnlyList<ShellContextMenuItem>? Items { get; init; }
		public bool IsSeparator { get; init; }

		public static ShellContextMenuItem Separator() => new() { IsSeparator = true };
	}

	internal static class ShellContextMenuBuilder
	{
		private const double ContextMenuFontSize = 13;
		private const double ContextMenuMinWidth = 226;
		private static readonly Thickness s_presenterPadding = new(10, 8, 10, 8);
		private static readonly Thickness s_itemPadding = new(18, 9, 18, 9);
		private static readonly Thickness s_separatorMargin = new(18, 5, 18, 5);

		public static MenuFlyout Build(IEnumerable<ShellContextMenuItem> items)
		{
			var itemStyle = CreateItemStyle(typeof(MenuFlyoutItem));
			var subItemStyle = CreateItemStyle(typeof(MenuFlyoutSubItem));
			var flyout = new MenuFlyout
			{
				MenuFlyoutPresenterStyle = CreatePresenterStyle()
			};

			foreach (var item in items)
			{
				flyout.Items.Add(BuildItem(item, itemStyle, subItemStyle));
			}

			return flyout;
		}

		private static MenuFlyoutItemBase BuildItem(ShellContextMenuItem item, Style itemStyle, Style subItemStyle)
		{
			if (item.IsSeparator)
			{
				return new MenuFlyoutSeparator
				{
					Margin = s_separatorMargin
				};
			}

			if (item.Items != null && item.Items.Count > 0)
			{
				var subItem = new MenuFlyoutSubItem
				{
					Text = item.Text,
					IsEnabled = item.IsEnabled,
					Style = subItemStyle
				};

				foreach (var child in item.Items)
				{
					subItem.Items.Add(BuildItem(child, itemStyle, subItemStyle));
				}

				return subItem;
			}

			var flyoutItem = new MenuFlyoutItem
			{
				Text = item.Text,
				IsEnabled = item.IsEnabled,
				Style = itemStyle
			};

			if (item.Invoked != null)
			{
				flyoutItem.Click += (_, _) => item.Invoked();
			}

			return flyoutItem;
		}

		private static Style CreatePresenterStyle()
		{
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(Control.BackgroundProperty, CreateContextMenuBackgroundBrush()));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateContextMenuBorderBrush()));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
			style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(12)));
			style.Setters.Add(new Setter(Control.ForegroundProperty, ResolveThemeBrush("ShellTextBrush")));
			style.Setters.Add(new Setter(Control.PaddingProperty, s_presenterPadding));
			return style;
		}

		private static Style CreateItemStyle(Type targetType)
		{
			var style = new Style(targetType);
			style.Setters.Add(new Setter(Control.ForegroundProperty, ResolveThemeBrush("ShellTextBrush")));
			style.Setters.Add(new Setter(Control.FontSizeProperty, ContextMenuFontSize));
			style.Setters.Add(new Setter(Control.MinWidthProperty, ContextMenuMinWidth));
			style.Setters.Add(new Setter(Control.PaddingProperty, s_itemPadding));
			return style;
		}

		private static Brush CreateContextMenuBackgroundBrush()
		{
			var topBarBrush = ResolveThemeBrush("ShellTopBarBrush");

			if (topBarBrush is SolidColorBrush solidBrush)
			{
				return new SolidColorBrush(solidBrush.Color);
			}

			return topBarBrush;
		}

		private static Brush CreateContextMenuBorderBrush()
		{
			return new SolidColorBrush(IsDarkTheme()
				? Windows.UI.Color.FromArgb(255, 77, 85, 97)
				: Windows.UI.Color.FromArgb(255, 201, 212, 224));
		}

		private static Brush ResolveThemeBrush(string key)
		{
			if (Application.Current.Resources.TryGetValue(key, out var resource)
				&& resource is Brush brush)
			{
				return brush;
			}

			return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 245, 247));
		}

		private static bool IsDarkTheme()
		{
			if (App.ShellWindow?.Content is FrameworkElement rootElement)
			{
				return rootElement.ActualTheme == ElementTheme.Dark;
			}

			return Application.Current.RequestedTheme == ApplicationTheme.Dark;
		}
	}
}