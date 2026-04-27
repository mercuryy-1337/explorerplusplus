using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace ExplorerPlusPlus.WinUIHost.Infrastructure
{
	internal static class ShellIconCache
	{
		private const uint ShgfiIcon = 0x000000100;
		private const uint ShgfiSmallIcon = 0x000000001;
		private const uint ShgfiUseFileAttributes = 0x000000010;
		private const uint FileAttributeDirectory = 0x00000010;
		private const uint FileAttributeNormal = 0x00000080;

		private static readonly ConcurrentDictionary<string, ImageSource?> s_iconCache =
			new(StringComparer.OrdinalIgnoreCase);

		public static ImageSource? GetFolderIcon(string activationPath)
		{
			if (string.IsNullOrWhiteSpace(activationPath))
			{
				return null;
			}

			if (IsDriveRoot(activationPath))
			{
				return GetDriveIcon(activationPath);
			}

			return s_iconCache.GetOrAdd("folder", _ => CreateGenericDirectoryIcon());
		}

		public static ImageSource? GetDriveIcon(string activationPath)
		{
			if (string.IsNullOrWhiteSpace(activationPath))
			{
				return null;
			}

			var normalizedPath = NormalizePath(activationPath);
			return s_iconCache.GetOrAdd($"drive:{normalizedPath}", _ => CreatePathIcon(normalizedPath));
		}

		public static ImageSource? GetFileIcon(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return null;
			}

			var extension = Path.GetExtension(filePath);

			if (string.IsNullOrWhiteSpace(extension))
			{
				return s_iconCache.GetOrAdd("file", _ => CreateGenericFileIcon());
			}

			return s_iconCache.GetOrAdd($"file:{extension}", _ => CreateExtensionIcon(extension));
		}

		private static ImageSource? CreateGenericDirectoryIcon()
		{
			return CreateIconFromShellInfo("folder", FileAttributeDirectory, ShgfiUseFileAttributes);
		}

		private static ImageSource? CreateGenericFileIcon()
		{
			return CreateIconFromShellInfo("file.txt", FileAttributeNormal, ShgfiUseFileAttributes);
		}

		private static ImageSource? CreateExtensionIcon(string extension)
		{
			return CreateIconFromShellInfo($"placeholder{extension}", FileAttributeNormal, ShgfiUseFileAttributes);
		}

		private static ImageSource? CreatePathIcon(string path)
		{
			return CreateIconFromShellInfo(path, 0, 0);
		}

		private static ImageSource? CreateIconFromShellInfo(string path, uint fileAttributes, uint extraFlags)
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
				return ConvertIconToImageSource(icon);
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

		private static ImageSource? ConvertIconToImageSource(Icon icon)
		{
			using var bitmap = icon.ToBitmap();
			using var stream = new MemoryStream();
			bitmap.Save(stream, ImageFormat.Png);
			stream.Position = 0;

			IRandomAccessStream randomAccessStream = stream.AsRandomAccessStream();
			var bitmapImage = new BitmapImage();
			bitmapImage.SetSource(randomAccessStream);
			return bitmapImage;
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