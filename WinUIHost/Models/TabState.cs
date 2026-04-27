using ExplorerPlusPlus.WinUIHost.Infrastructure;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class TabState : ObservableObject
	{
		private string m_title = string.Empty;
		private string m_glyph = "\uE8B7";
		private bool m_selected;

		public string Title
		{
			get => m_title;
			set => SetProperty(ref m_title, value);
		}

		public string Glyph
		{
			get => m_glyph;
			set => SetProperty(ref m_glyph, value);
		}

		public bool Selected
		{
			get => m_selected;
			set => SetProperty(ref m_selected, value);
		}
	}
}