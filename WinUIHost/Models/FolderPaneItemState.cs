using ExplorerPlusPlus.WinUIHost.Infrastructure;
using Microsoft.UI.Xaml;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class FolderPaneItemState : ObservableObject
	{
		private string m_title = string.Empty;
		private string m_glyph = string.Empty;
		private string m_activationPath = string.Empty;
		private bool m_canExpand;
		private bool m_isExpanded;
		private bool m_isSelected;
		private int m_depth;

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

		public string ActivationPath
		{
			get => m_activationPath;
			set => SetProperty(ref m_activationPath, value);
		}

		public bool CanExpand
		{
			get => m_canExpand;
			set
			{
				if (SetProperty(ref m_canExpand, value))
				{
					OnPropertyChanged(nameof(ExpandIndicator));
					OnPropertyChanged(nameof(SelectionBarOpacity));
					OnPropertyChanged(nameof(ChevronOpacity));
				}
			}
		}

		public bool IsExpanded
		{
			get => m_isExpanded;
			set
			{
				if (SetProperty(ref m_isExpanded, value))
				{
					OnPropertyChanged(nameof(ExpandIndicator));
				}
			}
		}

		public bool IsSelected
		{
			get => m_isSelected;
			set
			{
				if (SetProperty(ref m_isSelected, value))
				{
					OnPropertyChanged(nameof(SelectionBarOpacity));
				}
			}
		}

		public int Depth
		{
			get => m_depth;
			set
			{
				if (SetProperty(ref m_depth, value))
				{
					OnPropertyChanged(nameof(IndentMargin));
				}
			}
		}

		public string ExpandIndicator => !CanExpand ? string.Empty : (IsExpanded ? "\uE70D" : "\uE76C");
		public double SelectedBackgroundOpacity => IsSelected ? 1.0 : 0.0;
		public double SelectionBarOpacity => IsSelected && !CanExpand ? 1.0 : 0.0;
		public double ChevronOpacity => CanExpand ? 1.0 : 0.0;
		public Thickness IndentMargin => new Thickness(12 + (Depth * 18), 0, 0, 0);
	}
}