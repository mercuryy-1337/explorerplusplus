// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using Microsoft.UI.Xaml;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExplorerPlusPlus.WinUIHost
{
	public partial class App : Application
	{
		private static readonly string LogPath = Path.Combine(
			Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory,
			"startup.log");
		private static readonly ConcurrentQueue<string> s_pendingLogEntries = new();
		private static readonly UTF8Encoding s_utf8Encoding = new(false);
		private static int s_logFlushScheduled;

		public static MainWindow? ShellWindow { get; private set; }

		public App()
		{
			InitializeComponent();
			UnhandledException += OnUnhandledException;
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			try
			{
				AppendLog("OnLaunched start");

				var initialPath = args.Arguments;

				if (string.IsNullOrWhiteSpace(initialPath))
				{
					var rawArgs = Environment.GetCommandLineArgs();
					// Args[0] is the executable path, Args[1] (if any) is the path to open
					if (rawArgs.Length > 1 && !string.IsNullOrWhiteSpace(rawArgs[1]))
						initialPath = rawArgs[1];
				}

				AppendLog($"OnLaunched initialPath={initialPath ?? "(null)"}");
				ShellWindow = new MainWindow(initialPath);
				AppendLog("MainWindow created");
				ShellWindow.Activate();
				AppendLog("MainWindow activated");
			}
			catch (Exception ex)
			{
				AppendLog($"Launch exception: {ex}");
				throw;
			}
		}

		private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			AppendLog($"Unhandled exception: {e.Exception}");
		}

		internal static void AppendLog(string message)
		{
			try
			{
				s_pendingLogEntries.Enqueue($"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");

				if (Interlocked.Exchange(ref s_logFlushScheduled, 1) == 0)
				{
					_ = Task.Run(FlushPendingLogsAsync);
				}
			}
			catch
			{
			}
		}

		private static async Task FlushPendingLogsAsync()
		{
			try
			{
				await using var stream = new FileStream(
					LogPath,
					FileMode.Append,
					FileAccess.Write,
					FileShare.ReadWrite,
					4096,
					useAsync: true);
				await using var writer = new StreamWriter(stream, s_utf8Encoding);

				while (s_pendingLogEntries.TryDequeue(out string? entry))
				{
					await writer.WriteAsync(entry);
				}

				await writer.FlushAsync();
			}
			catch
			{
			}
			finally
			{
				Interlocked.Exchange(ref s_logFlushScheduled, 0);

				if (!s_pendingLogEntries.IsEmpty
					&& Interlocked.Exchange(ref s_logFlushScheduled, 1) == 0)
				{
					_ = Task.Run(FlushPendingLogsAsync);
				}
			}
		}
	}
}