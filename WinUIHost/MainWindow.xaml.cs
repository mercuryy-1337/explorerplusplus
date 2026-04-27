using ExplorerPlusPlus.WinUIHost.ViewModels;
using Microsoft.UI.Xaml;

namespace ExplorerPlusPlus.WinUIHost
{
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Title = "Explorer++ WinUI Host";
			RootLayout.DataContext = new ShellRootViewModel();
		}
	}
}