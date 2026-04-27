using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ExplorerPlusPlus.WinUIHost.Controls
{
	public sealed class HoverCursorGrid : Grid
	{
		private static readonly InputCursor s_arrowCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
		private static readonly InputCursor s_handCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);

		public HoverCursorGrid()
		{
			PointerEntered += OnPointerEntered;
			PointerExited += OnPointerExited;
			PointerCanceled += OnPointerCanceled;
			PointerCaptureLost += OnPointerCaptureLost;
		}

		private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			ProtectedCursor = s_handCursor;
		}

		private void OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			ProtectedCursor = s_arrowCursor;
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			ProtectedCursor = s_arrowCursor;
		}

		private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			ProtectedCursor = s_arrowCursor;
		}
	}
}