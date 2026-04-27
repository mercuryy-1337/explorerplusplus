// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
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

		public static string? GetFolderIconPath(string activationPath)
		{
			if (string.IsNullOrWhiteSpace(activationPath))
			{
				return null;
			}

			var normalizedPath = NormalizePath(activationPath);

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
			return s_iconCache.GetOrAdd($"drive:{normalizedPath}", _ => CreatePathIcon($"drive:{normalizedPath}", normalizedPath));
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

		private static string? CreatePathIcon(string cacheKey, string path)
		{
			return CreateIconFromShellInfo(cacheKey, path, 0, 0);
		}

		private static string? CreateIconFromShellInfo(string cacheKey, string path, uint fileAttributes, uint extraFlags)
		{
			var flags = ShgfiIcon | ShgfiSmallIcon | extraFlags;
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