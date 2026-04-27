// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ExplorerPlusPlus.WinUIHost.Infrastructure
{
	internal static class ShellIconCache
	{
		private const uint ShgfiIcon = 0x000000100;
		private const uint ShgfiSmallIcon = 0x000000001;
		private const uint ShgfiUseFileAttributes = 0x000000010;
		private const uint FileAttributeDirectory = 0x00000010;
		private const uint FileAttributeNormal = 0x00000080;

		private static readonly ConcurrentDictionary<string, string?> s_iconCache =
			new(StringComparer.OrdinalIgnoreCase);
		private static readonly string s_iconCacheDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"ExplorerX",
			"IconCache");

		public static ImageSource? GetFolderIcon(string activationPath)
		{
			return CreateImageSource(GetFolderIconPath(activationPath));
		}

		public static ImageSource? GetGenericFolderIcon()
		{
			return CreateImageSource(s_iconCache.GetOrAdd("folder", _ => CreateGenericDirectoryIcon()));
		}

		public static string? GetFolderIconPath(string activationPath)
		{
			if (string.IsNullOrWhiteSpace(activationPath))
			{
				return null;
			}

			var normalizedPath = NormalizePath(activationPath);
			var customFolderIconPath = GetCustomFolderIconPath(normalizedPath);

			if (!string.IsNullOrWhiteSpace(customFolderIconPath))
			{
				return customFolderIconPath;
			}

			if (IsDriveRoot(normalizedPath) || Directory.Exists(normalizedPath))
			{
				return s_iconCache.GetOrAdd($"folder:{normalizedPath}",
					_ => CreatePathIcon($"folder:{normalizedPath}", normalizedPath) ?? CreateGenericDirectoryIcon());
			}

			return s_iconCache.GetOrAdd("folder", _ => CreateGenericDirectoryIcon());
		}

		public static ImageSource? GetDriveIcon(string activationPath)
		{
			return CreateImageSource(GetDriveIconPath(activationPath));
		}

		public static string? GetDriveIconPath(string activationPath)
		{
			if (string.IsNullOrWhiteSpace(activationPath))
			{
				return null;
			}

			var normalizedPath = NormalizePath(activationPath);
			return s_iconCache.GetOrAdd($"drive:large:{normalizedPath}",
				_ => CreatePathIcon($"drive:large:{normalizedPath}", normalizedPath, useSmallIcon: false));
		}

		public static ImageSource? GetFileIcon(string filePath)
		{
			return CreateImageSource(GetFileIconPath(filePath));
		}

		public static string? GetFileIconPath(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return null;
			}

			var extension = Path.GetExtension(filePath);
			var normalizedPath = NormalizePath(filePath);

			if (string.IsNullOrWhiteSpace(extension))
			{
				if (File.Exists(normalizedPath))
				{
					return s_iconCache.GetOrAdd($"file:{normalizedPath}",
						_ => CreatePathIcon($"file:{normalizedPath}", normalizedPath) ?? CreateGenericFileIcon());
				}

				return s_iconCache.GetOrAdd("file", _ => CreateGenericFileIcon());
			}

			if (File.Exists(normalizedPath) && RequiresPathSpecificIcon(extension))
			{
				return s_iconCache.GetOrAdd($"file:{normalizedPath}",
					_ => CreatePathIcon($"file:{normalizedPath}", normalizedPath) ?? CreateExtensionIcon(extension));
			}

			return s_iconCache.GetOrAdd($"file:{extension}", _ => CreateExtensionIcon(extension));
		}

		public static ImageSource? CreateImageSource(string? iconPath)
		{
			if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
			{
				return null;
			}

			try
			{
				var bitmapImage = new BitmapImage();
				bitmapImage.UriSource = new Uri(iconPath, UriKind.Absolute);
				return bitmapImage;
			}
			catch
			{
				return null;
			}
		}

		private static bool RequiresPathSpecificIcon(string extension)
		{
			return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
				|| extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
				|| extension.Equals(".url", StringComparison.OrdinalIgnoreCase)
				|| extension.Equals(".ico", StringComparison.OrdinalIgnoreCase);
		}

		private static string? CreateGenericDirectoryIcon()
		{
			return CreateIconFromShellInfo("folder", "folder", FileAttributeDirectory, ShgfiUseFileAttributes);
		}

		private static string? CreateGenericFileIcon()
		{
			return CreateIconFromShellInfo("file", "file.txt", FileAttributeNormal, ShgfiUseFileAttributes);
		}

		private static string? CreateExtensionIcon(string extension)
		{
			return CreateIconFromShellInfo($"file:{extension}", $"placeholder{extension}", FileAttributeNormal, ShgfiUseFileAttributes);
		}

		private static string? CreatePathIcon(string cacheKey, string path, bool useSmallIcon = true)
		{
			return CreateIconFromShellInfo(cacheKey, path, 0, 0, useSmallIcon);
		}

		private static string? GetCustomFolderIconPath(string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
			{
				return null;
			}

			var desktopIniPath = Path.Combine(directoryPath, "desktop.ini");

			if (!File.Exists(desktopIniPath))
			{
				return null;
			}

			var iconResource = TryReadDesktopIniIconResource(desktopIniPath);

			if (string.IsNullOrWhiteSpace(iconResource))
			{
				return null;
			}

			return s_iconCache.GetOrAdd($"folder:custom:{directoryPath}",
				_ => CreateIconFromResource($"folder:custom:{directoryPath}", iconResource));
		}

		private static string? TryReadDesktopIniIconResource(string desktopIniPath)
		{
			try
			{
				string? iconFile = null;
				string? iconIndex = null;

				foreach (var rawLine in File.ReadAllLines(desktopIniPath))
				{
					var line = rawLine.Trim();

					if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal))
					{
						continue;
					}

					if (line.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase))
					{
						return line["IconResource=".Length..].Trim();
					}

					if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
					{
						iconFile = line["IconFile=".Length..].Trim();
						continue;
					}

					if (line.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
					{
						iconIndex = line["IconIndex=".Length..].Trim();
					}
				}

				if (string.IsNullOrWhiteSpace(iconFile))
				{
					return null;
				}

				return string.IsNullOrWhiteSpace(iconIndex)
					? iconFile
					: string.Create(CultureInfo.InvariantCulture, $"{iconFile},{iconIndex}");
			}
			catch
			{
				return null;
			}
		}

		private static string? CreateIconFromResource(string cacheKey, string iconResource, bool useSmallIcon = true)
		{
			if (!TryParseIconResource(iconResource, out var iconPath, out var iconIndex))
			{
				return null;
			}

			var iconCount = ExtractIconEx(iconPath, iconIndex, out var largeIcon, out var smallIcon, 1);
			var iconHandle = useSmallIcon
				? (smallIcon != IntPtr.Zero ? smallIcon : largeIcon)
				: (largeIcon != IntPtr.Zero ? largeIcon : smallIcon);

			if (iconCount == 0 || iconHandle == IntPtr.Zero)
			{
				if (smallIcon != IntPtr.Zero)
				{
					DestroyIcon(smallIcon);
				}

				if (largeIcon != IntPtr.Zero && largeIcon != smallIcon)
				{
					DestroyIcon(largeIcon);
				}

				return null;
			}

			try
			{
				using var nativeIcon = Icon.FromHandle(iconHandle);
				using var icon = (Icon)nativeIcon.Clone();
				return SaveIconToCacheFile(cacheKey, icon);
			}
			catch
			{
				return null;
			}
			finally
			{
				if (smallIcon != IntPtr.Zero)
				{
					DestroyIcon(smallIcon);
				}

				if (largeIcon != IntPtr.Zero && largeIcon != smallIcon)
				{
					DestroyIcon(largeIcon);
				}
			}
		}

		private static bool TryParseIconResource(string iconResource, out string iconPath, out int iconIndex)
		{
			iconPath = string.Empty;
			iconIndex = 0;

			if (string.IsNullOrWhiteSpace(iconResource))
			{
				return false;
			}

			var trimmedResource = Environment.ExpandEnvironmentVariables(iconResource.Trim().Trim('"'));
			var separatorIndex = trimmedResource.LastIndexOf(',');

			if (separatorIndex > 0 && int.TryParse(trimmedResource[(separatorIndex + 1)..], NumberStyles.Integer,
				CultureInfo.InvariantCulture, out var parsedIndex))
			{
				iconPath = trimmedResource[..separatorIndex].Trim().Trim('"');
				iconIndex = parsedIndex;
			}
			else
			{
				iconPath = trimmedResource;
			}

			return !string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath);
		}

		private static string? CreateIconFromShellInfo(string cacheKey, string path, uint fileAttributes,
			uint extraFlags, bool useSmallIcon = true)
		{
			var flags = ShgfiIcon | extraFlags;

			if (useSmallIcon)
			{
				flags |= ShgfiSmallIcon;
			}

			SHFILEINFOW shellFileInfo = default;

			if (SHGetFileInfo(path, fileAttributes, ref shellFileInfo,
				(uint)Marshal.SizeOf<SHFILEINFOW>(), flags) == IntPtr.Zero)
			{
				return null;
			}

			if (shellFileInfo.hIcon == IntPtr.Zero)
			{
				return null;
			}

			try
			{
				using var nativeIcon = Icon.FromHandle(shellFileInfo.hIcon);
				using var icon = (Icon)nativeIcon.Clone();
				return SaveIconToCacheFile(cacheKey, icon);
			}
			catch
			{
				return null;
			}
			finally
			{
				DestroyIcon(shellFileInfo.hIcon);
			}
		}

		private static string? SaveIconToCacheFile(string cacheKey, Icon icon)
		{
			try
			{
				Directory.CreateDirectory(s_iconCacheDirectory);
				var cachePath = Path.Combine(s_iconCacheDirectory, GetCacheFileName(cacheKey));

				if (!File.Exists(cachePath))
				{
					using var bitmap = icon.ToBitmap();
					bitmap.Save(cachePath, ImageFormat.Png);
				}

				return cachePath;
			}
			catch
			{
				return null;
			}
		}

		private static string GetCacheFileName(string cacheKey)
		{
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey));
			return Convert.ToHexString(hash) + ".png";
		}

		private static bool IsDriveRoot(string path)
		{
			try
			{
				var normalizedPath = NormalizePath(path);
				var root = Path.GetPathRoot(normalizedPath);
				return !string.IsNullOrWhiteSpace(root)
					&& string.Equals(NormalizePath(root), normalizedPath, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private static string NormalizePath(string path)
		{
			var normalizedPath = Path.GetFullPath(path);

			while (normalizedPath.Length > 3
				&& (normalizedPath.EndsWith(Path.DirectorySeparatorChar)
					|| normalizedPath.EndsWith(Path.AltDirectorySeparatorChar)))
			{
				normalizedPath = normalizedPath[..^1];
			}

			return normalizedPath;
		}

		[DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
		private static extern IntPtr SHGetFileInfo(string path, uint fileAttributes,
			ref SHFILEINFOW shellFileInfo, uint shellFileInfoSize, uint flags);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint ExtractIconEx(string fileName, int iconIndex, out IntPtr largeIcon,
			out IntPtr smallIcon, uint iconCount);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DestroyIcon(IntPtr iconHandle);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct SHFILEINFOW
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

	}
}