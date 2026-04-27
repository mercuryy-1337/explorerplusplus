// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Text;

namespace ExplorerPlusPlus.WinUIHost
{
	public partial class App : Application
	{
		private static readonly string LogPath = Path.Combine(
			Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory,
			"startup.log");

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
				ShellWindow = new MainWindow();
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
	}
}