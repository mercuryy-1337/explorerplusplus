using Microsoft.UI.Xaml;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class FolderPaneItemState
	{
		public string Title { get; set; } = string.Empty;
		public string Glyph { get; set; } = string.Empty;
		public string ActivationPath { get; set; } = string.Empty;
		public bool CanExpand { get; set; }
		public bool IsExpanded { get; set; }
		public string ExpandIndicator => !CanExpand ? string.Empty : (IsExpanded ? "v" : ">");
		public Thickness IndentMargin { get; set; }
	}
}