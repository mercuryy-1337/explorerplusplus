// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace ExplorerPlusPlus.WinUIHost.Controls
{
	internal static class NativeShellContextMenu
	{
		private const uint CMF_NORMAL = 0x00000000;
		private const uint CMF_EXTENDEDVERBS = 0x00000100;

		private const uint GCS_HELPTEXTW = 0x00000005;
		private const uint GCS_VERBW = 0x00000004;

		private const uint MFT_SEPARATOR = 0x00000800;
		private const uint MFT_OWNERDRAW = 0x00000100;

		private const uint MF_GRAYED = 0x00000001;
		private const uint MF_DISABLED = 0x00000002;

		private const uint MIIM_FTYPE = 0x00000100;
		private const uint MIIM_ID = 0x00000002;
		private const uint MIIM_SUBMENU = 0x00000004;
		private const uint MIIM_STATE = 0x00000001;
		private const uint MIIM_BITMAP = 0x00000080;

		private const uint MF_BYPOSITION = 0x00000400;

		private const uint WM_MEASUREITEM = 0x002C;
		private const uint WM_DRAWITEM = 0x002B;
		private const uint WM_INITMENUPOPUP = 0x0117;
		private const uint WM_MENUCHAR = 0x0120;

		private const int HBMMENU_CALLBACK = -1;

		private const double ContextMenuFontSize = 13;
		private const double ContextMenuMinWidth = 226;
		private static readonly CornerRadius s_itemCornerRadius = new(8);
		private static readonly Thickness s_presenterPadding = new(10, 8, 10, 8);
		private static readonly Thickness s_itemPadding = new(18, 9, 18, 9);
		private static readonly Thickness s_separatorMargin = new(0, 5, 0, 5);

		[ComImport]
		[Guid("000214E4-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IContextMenu
		{
			[PreserveSig]
			int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);

			[PreserveSig]
			int InvokeCommand(ref CMINVOKECOMMANDINFO pici);

			[PreserveSig]
			int GetCommandString(uint idCmd, uint uFlags, IntPtr pwReserved,
				[MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, uint cchMax);
		}

		[ComImport]
		[Guid("000214F4-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IContextMenu2 : IContextMenu
		{
			[PreserveSig]
			int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
		}

		[ComImport]
		[Guid("BCFCE0A0-EC17-11D0-8D10-00A0C90F2719")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IContextMenu3 : IContextMenu2
		{
			[PreserveSig]
			int HandleMenuMsg2(uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct CMINVOKECOMMANDINFO
		{
			public uint cbSize;
			public uint fMask;
			public IntPtr hwnd;
			public IntPtr lpVerb;
			[MarshalAs(UnmanagedType.LPStr)]
			public string? lpParameters;
			[MarshalAs(UnmanagedType.LPStr)]
			public string? lpDirectory;
			public int nShow;
			public uint dwHotKey;
			public IntPtr hIcon;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MENUITEMINFO
		{
			public uint cbSize;
			public uint fMask;
			public uint fType;
			public uint fState;
			public uint wID;
			public IntPtr hSubMenu;
			public IntPtr hbmpChecked;
			public IntPtr hbmpUnchecked;
			public IntPtr dwItemData;
			public IntPtr dwTypeData;
			public uint cch;
			public IntPtr hbmpItem;
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

		private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

		[DllImport("shell32.dll")]
		private static extern int SHBindToParent(IntPtr pidl, ref Guid riid, out IntPtr ppv, out IntPtr ppidlLast);

		[DllImport("shell32.dll")]
		private static extern void ILFree(IntPtr pidl);

		[DllImport("user32.dll")]
		private static extern IntPtr CreatePopupMenu();

		[DllImport("user32.dll")]
		private static extern bool DestroyMenu(IntPtr hMenu);

		[DllImport("user32.dll")]
		private static extern int GetMenuItemCount(IntPtr hMenu);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetMenuItemInfo(IntPtr hMenu, uint uItem, bool fByPosition, ref MENUITEMINFO lpmii);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetMenuString(IntPtr hMenu, uint uIDItem, StringBuilder lpString, int cchMax, uint uFlag);

		[DllImport("user32.dll")]
		private static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

		[DllImport("comctl32.dll")]
		private static extern IntPtr SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

		[DllImport("comctl32.dll")]
		private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass);

		[DllImport("comctl32.dll")]
		private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("gdi32.dll")]
		private static extern int GetObject(IntPtr hBitmap, int cbBuffer, out BITMAP lpBitmap);

		[DllImport("gdi32.dll")]
		private static extern int GetDIBits(IntPtr hdc, IntPtr hBitmap, uint uStartScan, uint cScanLines,
			byte[] lpvBits, ref BITMAPINFO lpbmi, uint uUsage);

		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hdc);

		[StructLayout(LayoutKind.Sequential)]
		private struct BITMAP
		{
			public int bmType;
			public int bmWidth;
			public int bmHeight;
			public int bmWidthBytes;
			public ushort bmPlanes;
			public ushort bmBitsPixel;
			public IntPtr bmBits;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BITMAPINFOHEADER
		{
			public uint biSize;
			public int biWidth;
			public int biHeight;
			public ushort biPlanes;
			public ushort biBitCount;
			public uint biCompression;
			public uint biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public uint biClrUsed;
			public uint biClrImportant;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BITMAPINFO
		{
			public BITMAPINFOHEADER bmiHeader;
			// bmiColors is a variable-length array, not needed for 32bpp
		}

		private static IContextMenu2? s_contextMenu2;
		private static IContextMenu3? s_contextMenu3;
		private static readonly SubclassProc s_subclassProc = MenuSubclassProc;

		public static MenuFlyout? BuildFlyout(string path, IntPtr hwndOwner)
		{
			var contextMenu = GetContextMenuForPath(path, hwndOwner);
			if (contextMenu == null)
				return null;

			var hMenu = CreatePopupMenu();
			if (hMenu == IntPtr.Zero)
				return null;

			const uint idCmdFirst = 1;
			const uint idCmdLast = 0x7FFF;

			try
			{
				var hr = contextMenu.QueryContextMenu(hMenu, 0, idCmdFirst, idCmdLast, CMF_NORMAL);
				if (hr < 0)
					return null;

				var itemStyle = CreateItemStyle(typeof(MenuFlyoutItem));
				var subItemStyle = CreateItemStyle(typeof(MenuFlyoutSubItem));
				var flyout = new MenuFlyout
				{
					MenuFlyoutPresenterStyle = CreatePresenterStyle()
				};

				flyout.SystemBackdrop = new DesktopAcrylicBackdrop();

				PopulateFlyoutItems(flyout.Items, hMenu, contextMenu, idCmdFirst, hwndOwner, path, itemStyle, subItemStyle);

				RemoveTrailingSeparators(flyout.Items);
				RemoveDuplicateSeparators(flyout.Items);

				if (flyout.Items.Count == 0)
					return null;

				return flyout;
			}
			finally
			{
				DestroyMenu(hMenu);
			}
		}

		private static void PopulateFlyoutItems(IList<MenuFlyoutItemBase> items, IntPtr hMenu, IContextMenu contextMenu,
			uint idCmdFirst, IntPtr hwndOwner, string? directory, Style itemStyle, Style subItemStyle, int depth = 0)
		{
			int count = GetMenuItemCount(hMenu);
			for (int i = 0; i < count; i++)
			{
				var mii = new MENUITEMINFO
				{
					cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
					fMask = MIIM_FTYPE | MIIM_ID | MIIM_SUBMENU | MIIM_STATE | MIIM_BITMAP
				};

				if (!GetMenuItemInfo(hMenu, (uint)i, true, ref mii))
					continue;

				if ((mii.fType & MFT_SEPARATOR) != 0)
				{
					items.Add(new MenuFlyoutSeparator
					{
						Margin = s_separatorMargin
					});
					continue;
				}

				if (mii.hSubMenu != IntPtr.Zero)
				{
					if (depth >= 2)
					{
						PopulateFlyoutItems(items, mii.hSubMenu, contextMenu, idCmdFirst, hwndOwner, directory, itemStyle, subItemStyle, depth + 1);
						continue;
					}

					var subItem = new MenuFlyoutSubItem
					{
						Text = GetMenuItemText(hMenu, i, mii, contextMenu, idCmdFirst),
						Style = subItemStyle
					};
					subItem.AllowFocusOnInteraction = false;
					subItem.IsTapEnabled = false;
					ApplyItemResources(subItem, includeSubMenuStateResources: true);

					PopulateFlyoutItems(subItem.Items, mii.hSubMenu, contextMenu, idCmdFirst, hwndOwner, directory, itemStyle, subItemStyle, depth + 1);
					RemoveTrailingSeparators(subItem.Items);
					RemoveDuplicateSeparators(subItem.Items);

					if (subItem.Items.Count > 0)
						items.Add(subItem);

					continue;
				}

				string text = GetMenuItemText(hMenu, i, mii, contextMenu, idCmdFirst);
				if (string.IsNullOrEmpty(text))
					continue;

				var item = new MenuFlyoutItem
				{
					Text = text,
					Style = itemStyle
				};
				ApplyItemResources(item, includeSubMenuStateResources: false);

				bool isDisabled = (mii.fState & MF_GRAYED) != 0 || (mii.fState & MF_DISABLED) != 0;
				item.IsEnabled = !isDisabled;

				// Extract icon
				var icon = TryExtractIcon(mii.hbmpItem);
				if (icon != null)
				{
					item.Icon = icon;
				}

				uint commandOffset = mii.wID - idCmdFirst;

				if (!isDisabled)
				{
					item.Click += (_, _) =>
					{
						InvokeCommand(contextMenu, commandOffset, hwndOwner, directory);
					};
				}

				items.Add(item);
			}
		}

		private static string GetMenuItemText(IntPtr hMenu, int position, MENUITEMINFO mii, IContextMenu contextMenu, uint idCmdFirst)
		{
			var sb = new StringBuilder(512);
			int len = GetMenuString(hMenu, (uint)position, sb, sb.Capacity, MF_BYPOSITION);
			if (len > 0)
				return StripAccelerators(sb.ToString(0, len));

			if ((mii.fType & MFT_OWNERDRAW) != 0)
			{
				var text = GetCommandString(contextMenu, mii.wID - idCmdFirst, GCS_HELPTEXTW);
				if (!string.IsNullOrEmpty(text))
					return StripAccelerators(text);

				text = GetCommandString(contextMenu, mii.wID - idCmdFirst, GCS_VERBW);
				if (!string.IsNullOrEmpty(text))
					return StripAccelerators(text);
			}

			return string.Empty;
		}

		private static string? GetCommandString(IContextMenu contextMenu, uint idCmd, uint uFlags)
		{
			var sb = new StringBuilder(512);
			var hr = contextMenu.GetCommandString(idCmd, uFlags, IntPtr.Zero, sb, (uint)sb.Capacity);
			if (hr < 0)
				return null;

			var text = sb.ToString().Trim();
			return string.IsNullOrEmpty(text) ? null : text;
		}

		private static string StripAccelerators(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			var sb = new StringBuilder(text.Length);
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '&')
				{
					if (i + 1 < text.Length && text[i + 1] == '&')
					{
						sb.Append('&');
						i++;
					}
					// Single & is accelerator marker, skip it
				}
				else
				{
					sb.Append(text[i]);
				}
			}
			return sb.ToString();
		}

		private static IconElement? TryExtractIcon(IntPtr hBitmap)
		{
			if (hBitmap == IntPtr.Zero)
				return null;

			if (hBitmap == (IntPtr)HBMMENU_CALLBACK)
				return null;

			try
			{
				GetObject(hBitmap, Marshal.SizeOf<BITMAP>(), out var bmp);
				int width = bmp.bmWidth;
				int height = Math.Abs(bmp.bmHeight);

				if (width <= 0 || height <= 0 || bmp.bmBitsPixel != 32)
					return null;

				var screenDc = GetDC(IntPtr.Zero);

				var header = new BITMAPINFOHEADER
				{
					biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
					biWidth = width,
					biHeight = -height, // Negative = top-down, no need to flip
					biPlanes = 1,
					biBitCount = 32,
					biCompression = 0 // BI_RGB
				};

				var bmpInfo = new BITMAPINFO { bmiHeader = header };
				int stride = width * 4;
				var buffer = new byte[stride * height];

				int result = GetDIBits(screenDc, hBitmap, 0, (uint)height,
					buffer, ref bmpInfo, 0); // DIB_RGB_COLORS = 0

				ReleaseDC(IntPtr.Zero, screenDc);

				if (result == 0)
					return null;

				var wb = new WriteableBitmap(width, height);
				using (var stream = wb.PixelBuffer.AsStream())
				{
					stream.Write(buffer, 0, buffer.Length);
				}

				wb.Invalidate();
				return new ImageIcon { Source = wb };
			}
			catch
			{
				return null;
			}
		}

		private static void InvokeCommand(IContextMenu contextMenu, uint commandOffset, IntPtr hwndOwner, string? directory)
		{
			s_contextMenu2 = contextMenu as IContextMenu2;
			s_contextMenu3 = contextMenu as IContextMenu3;

			if (s_contextMenu2 != null || s_contextMenu3 != null)
			{
				SetWindowSubclass(hwndOwner, s_subclassProc, 1, IntPtr.Zero);
			}

			try
			{
				var info = new CMINVOKECOMMANDINFO
				{
					cbSize = (uint)Marshal.SizeOf<CMINVOKECOMMANDINFO>(),
					hwnd = hwndOwner,
					lpVerb = (IntPtr)commandOffset,
					lpDirectory = directory,
					nShow = 1 // SW_SHOWNORMAL
				};

				contextMenu.InvokeCommand(ref info);
			}
			finally
			{
				if (s_contextMenu2 != null || s_contextMenu3 != null)
				{
					RemoveWindowSubclass(hwndOwner, s_subclassProc, 1);
				}

				s_contextMenu2 = null;
				s_contextMenu3 = null;
			}
		}

		private static IntPtr MenuSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
		{
			switch (uMsg)
			{
				case WM_MEASUREITEM:
				case WM_DRAWITEM:
				case WM_INITMENUPOPUP:
				case WM_MENUCHAR:
					if (s_contextMenu3 != null)
					{
						var hr = s_contextMenu3.HandleMenuMsg2(uMsg, wParam, lParam, out var result);
						if (hr >= 0)
							return result;
					}
					else if (s_contextMenu2 != null)
					{
						var hr = s_contextMenu2.HandleMenuMsg(uMsg, wParam, lParam);
						if (hr >= 0)
							return IntPtr.Zero;
					}
					break;
			}

			return DefSubclassProc(hWnd, uMsg, wParam, lParam);
		}

		private static IContextMenu? GetContextMenuForPath(string path, IntPtr hwndOwner)
		{
			IntPtr pidl = IntPtr.Zero;
			IntPtr parentPtr = IntPtr.Zero;
			IntPtr childPidl = IntPtr.Zero;

			try
			{
				var hr = SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out _);
				if (hr < 0 || pidl == IntPtr.Zero)
					return null;

				var iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
				hr = SHBindToParent(pidl, ref iidShellFolder, out parentPtr, out childPidl);
				if (hr < 0 || parentPtr == IntPtr.Zero || childPidl == IntPtr.Zero)
					return null;

				var parent = (IShellFolder)Marshal.GetObjectForIUnknown(parentPtr);

				var iidContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");
				hr = parent.GetUIObjectOf(hwndOwner, 1, new[] { childPidl }, iidContextMenu, IntPtr.Zero, out var obj);
				if (hr < 0 || obj == null)
					return null;

				return (IContextMenu)obj;
			}
			finally
			{
				if (pidl != IntPtr.Zero)
					ILFree(pidl);
				if (parentPtr != IntPtr.Zero)
					Marshal.Release(parentPtr);
			}
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
			style.Setters.Add(new Setter(Control.CornerRadiusProperty, s_itemCornerRadius));
			style.Setters.Add(new Setter(Control.UseSystemFocusVisualsProperty, false));
			return style;
		}

		private static void ApplyItemResources(FrameworkElement item, bool includeSubMenuStateResources)
		{
			var hoverBackgroundBrush = ResolveThemeBrush("ShellNavButtonHoverBrush");
			var textBrush = ResolveThemeBrush("ShellTextBrush");
			var chevronBrush = ResolveThemeBrush("ShellSecondaryTextBrush");

			item.Resources["MenuFlyoutItemBackgroundPointerOver"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutItemBackgroundPressed"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutItemForegroundPointerOver"] = textBrush;
			item.Resources["MenuFlyoutItemForegroundPressed"] = textBrush;
			item.Resources["MenuFlyoutItemRevealBackgroundPointerOver"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutItemRevealBackgroundPressed"] = hoverBackgroundBrush;

			if (!includeSubMenuStateResources)
			{
				return;
			}

			item.Resources["MenuFlyoutSubItemBackgroundPointerOver"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutSubItemBackgroundPressed"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutSubItemBackgroundSubMenuOpened"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutSubItemForegroundPointerOver"] = textBrush;
			item.Resources["MenuFlyoutSubItemForegroundPressed"] = textBrush;
			item.Resources["MenuFlyoutSubItemForegroundSubMenuOpened"] = textBrush;
			item.Resources["MenuFlyoutSubItemChevronPointerOver"] = chevronBrush;
			item.Resources["MenuFlyoutSubItemChevronPressed"] = chevronBrush;
			item.Resources["MenuFlyoutSubItemChevronSubMenuOpened"] = chevronBrush;
			item.Resources["MenuFlyoutSubItemRevealBackgroundPointerOver"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutSubItemRevealBackgroundPressed"] = hoverBackgroundBrush;
			item.Resources["MenuFlyoutSubItemRevealBackgroundSubMenuOpened"] = hoverBackgroundBrush;
		}

		private static Brush CreateContextMenuBackgroundBrush()
		{
			var darkTheme = IsDarkTheme();
			return new AcrylicBrush
			{
				FallbackColor = darkTheme
					? Windows.UI.Color.FromArgb(255, 27, 31, 37)
					: Windows.UI.Color.FromArgb(255, 247, 248, 250),
				TintColor = darkTheme
					? Windows.UI.Color.FromArgb(255, 27, 31, 37)
					: Windows.UI.Color.FromArgb(255, 247, 248, 250),
				TintOpacity = darkTheme ? 0.6 : 0.65,
				TintLuminosityOpacity = darkTheme ? 0.45 : 0.55
			};
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

		private static void RemoveTrailingSeparators(IList<MenuFlyoutItemBase> items)
		{
			while (items.Count > 0 && items[items.Count - 1] is MenuFlyoutSeparator)
			{
				items.RemoveAt(items.Count - 1);
			}
		}

		private static void RemoveDuplicateSeparators(IList<MenuFlyoutItemBase> items)
		{
			for (int i = items.Count - 1; i > 0; i--)
			{
				if (items[i] is MenuFlyoutSeparator && items[i - 1] is MenuFlyoutSeparator)
				{
					items.RemoveAt(i);
				}
			}
		}
	}
}
