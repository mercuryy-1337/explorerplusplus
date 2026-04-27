using ExplorerPlusPlus.WinUIHost.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ExplorerPlusPlus.WinUIHost.Models
{
	public sealed class FolderPaneItemState : ObservableObject
	{
		private string m_title = string.Empty;
		private string m_glyph = string.Empty;
		private string m_activationPath = string.Empty;
		private ImageSource? m_iconSource;
		private bool m_isHeader;
		private bool m_canExpand;
		private bool m_isExpanded;
		private bool m_isPointerOver;
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

		public ImageSource? IconSource
		{
			get => m_iconSource;
			set
			{
				if (SetProperty(ref m_iconSource, value))
				{
					OnPropertyChanged(nameof(NativeIconOpacity));
					OnPropertyChanged(nameof(GlyphOpacity));
				}
			}
		}

		public bool IsHeader
		{
			get => m_isHeader;
			set
			{
				if (SetProperty(ref m_isHeader, value))
				{
					OnPropertyChanged(nameof(HeaderVisibility));
					OnPropertyChanged(nameof(RowVisibility));
					OnPropertyChanged(nameof(IsInteractive));
					OnPropertyChanged(nameof(ChevronHitTestVisible));
					OnPropertyChanged(nameof(ExpandIndicator));
					OnPropertyChanged(nameof(SelectedBackgroundOpacity));
					OnPropertyChanged(nameof(SelectionBarOpacity));
					OnPropertyChanged(nameof(ChevronOpacity));
				}
			}
		}

		public bool CanExpand
		{
			get => m_canExpand;
			set
			{
				if (SetProperty(ref m_canExpand, value))
				{
					OnPropertyChanged(nameof(ChevronHitTestVisible));
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

		public bool IsPointerOver
		{
			get => m_isPointerOver;
			set
			{
				if (SetProperty(ref m_isPointerOver, value))
				{
					OnPropertyChanged(nameof(RowBackgroundOpacity));
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
					OnPropertyChanged(nameof(RowBackgroundOpacity));
					OnPropertyChanged(nameof(SelectionBarOpacity));
						OnPropertyChanged(nameof(RowBackgroundOpacity));
						OnPropertyChanged(nameof(SelectedBackgroundOpacity));
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

		public string ExpandIndicator => !CanExpand || IsHeader ? string.Empty : (IsExpanded ? "\uE70D" : "\uE76C");
		public double RowBackgroundOpacity => IsHeader ? 0.0 : (IsSelected ? 1.0 : (IsPointerOver ? 0.55 : 0.0));
		public double SelectedBackgroundOpacity => IsSelected && !IsHeader ? 1.0 : 0.0;
		public double SelectionBarOpacity => IsSelected && !CanExpand && !IsHeader ? 1.0 : 0.0;
		public double ChevronOpacity => CanExpand && !IsHeader ? 1.0 : 0.0;
		public bool ChevronHitTestVisible => CanExpand && !IsHeader;
		public bool IsInteractive => !IsHeader;
		public Visibility HeaderVisibility => IsHeader ? Visibility.Visible : Visibility.Collapsed;
		public Visibility RowVisibility => IsHeader ? Visibility.Collapsed : Visibility.Visible;
		public Thickness IndentMargin => new Thickness(12 + (Depth * 18), 0, 10, 0);
		public double NativeIconOpacity => IconSource == null ? 0.0 : 1.0;
		public double GlyphOpacity => IconSource == null ? 1.0 : 0.0;
	}
}