using ExplorerPlusPlus.WinUIHost.Infrastructure;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class NavigationState : ObservableObject
	{
		private string m_currentPath = string.Empty;
		private string m_pathText = string.Empty;
		private bool m_canGoBack;
		private bool m_canGoForward;
		private bool m_canGoUp;
		private bool m_canRefresh;
		private bool m_canShowPathText;
		private bool m_isPathTextVisible;
		private bool m_isNavigating;

		public NavigationState()
		{
			BreadcrumbSegments = new ObservableCollection<NavigationSegmentState>();
		}

		public string CurrentPath
		{
			get => m_currentPath;
			set => SetProperty(ref m_currentPath, value);
		}

		public string PathText
		{
			get => m_pathText;
			set => SetProperty(ref m_pathText, value);
		}

		public ObservableCollection<NavigationSegmentState> BreadcrumbSegments { get; }

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

		public bool CanShowPathText
		{
			get => m_canShowPathText;
			set => SetProperty(ref m_canShowPathText, value);
		}

		public bool IsPathTextVisible
		{
			get => m_isPathTextVisible;
			set
			{
				if (SetProperty(ref m_isPathTextVisible, value))
				{
					OnPropertyChanged(nameof(BreadcrumbVisibility));
					OnPropertyChanged(nameof(PathTextVisibility));
				}
			}
		}

		public Visibility BreadcrumbVisibility => IsPathTextVisible ? Visibility.Collapsed : Visibility.Visible;

		public Visibility PathTextVisibility => IsPathTextVisible ? Visibility.Visible : Visibility.Collapsed;

		public bool IsNavigating
		{
			get => m_isNavigating;
			set => SetProperty(ref m_isNavigating, value);
		}
	}
}