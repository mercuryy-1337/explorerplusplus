namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class NavigationState
	{
		public string CurrentPath { get; set; } = string.Empty;
		public bool CanGoBack { get; set; }
		public bool CanGoForward { get; set; }
		public bool CanGoUp { get; set; }
		public bool CanRefresh { get; set; }
		public bool IsNavigating { get; set; }
	}
}