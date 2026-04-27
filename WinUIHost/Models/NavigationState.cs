using ExplorerPlusPlus.WinUIHost.Infrastructure;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class NavigationState : ObservableObject
	{
		private string m_currentPath = string.Empty;
		private bool m_canGoBack;
		private bool m_canGoForward;
		private bool m_canGoUp;
		private bool m_canRefresh;
		private bool m_isNavigating;

		public string CurrentPath
		{
			get => m_currentPath;
			set => SetProperty(ref m_currentPath, value);
		}

		public bool CanGoBack
		{
			get => m_canGoBack;
			set => SetProperty(ref m_canGoBack, value);
		}

		public bool CanGoForward
		{
			get => m_canGoForward;
			set => SetProperty(ref m_canGoForward, value);
		}

		public bool CanGoUp
		{
			get => m_canGoUp;
			set => SetProperty(ref m_canGoUp, value);
		}

		public bool CanRefresh
		{
			get => m_canRefresh;
			set => SetProperty(ref m_canRefresh, value);
		}

		public bool IsNavigating
		{
			get => m_isNavigating;
			set => SetProperty(ref m_isNavigating, value);
		}
	}
}