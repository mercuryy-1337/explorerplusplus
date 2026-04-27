using Microsoft.UI.Xaml;

namespace ExplorerPlusPlus.WinUIHost
{
	public partial class App : Application
	{
		public static MainWindow? ShellWindow { get; private set; }

		public App()
		{
			InitializeComponent();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			ShellWindow = new MainWindow();
			ShellWindow.Activate();
		}
	}
}