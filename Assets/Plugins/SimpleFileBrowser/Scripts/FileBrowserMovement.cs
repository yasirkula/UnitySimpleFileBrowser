using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleFileBrowser
{
	public class FileBrowserMovement : MonoBehaviour
	{
		#region Variables
		[SerializeField]
		private Canvas canvas;
		private RectTransform canvasTR;
		private Camera canvasCam;

		[SerializeField]
		private RectTransform window;

		[SerializeField]
		private FileBrowser fileBrowser;

		[SerializeField]
		private RecycledListView listView;

		private Vector2 initialTouchPos = Vector2.zero;
		private Vector2 initialAnchoredPos, initialSizeDelta;
		#endregion

		#region Messages
		private void Awake()
		{
			canvasTR = canvas.GetComponent<RectTransform>();
		}
		#endregion

		#region Pointer Events
		public void OnDragStarted( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			canvasCam = pointer.pressEventCamera;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( window, pointer.pressPosition, canvasCam, out initialTouchPos );
		}

		public void OnDrag( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( window, pointer.position, canvasCam, out touchPos );
			window.anchoredPosition += touchPos - initialTouchPos;
		}

		public void OnResizeStarted( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			canvasCam = pointer.pressEventCamera;
			initialAnchoredPos = window.anchoredPosition;
			initialSizeDelta = window.sizeDelta;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasTR, pointer.pressPosition, canvasCam, out initialTouchPos );
		}

		public void OnResize( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasTR, pointer.position, canvasCam, out touchPos );

			Vector2 delta = touchPos - initialTouchPos;
			Vector2 newSize = initialSizeDelta + new Vector2( delta.x, -delta.y );

			if( newSize.x < fileBrowser.minWidth ) newSize.x = fileBrowser.minWidth;
			if( newSize.y < fileBrowser.minHeight ) newSize.y = fileBrowser.minHeight;

			newSize.x = (int) newSize.x;
			newSize.y = (int) newSize.y;

			delta = newSize - initialSizeDelta;

			window.anchoredPosition = initialAnchoredPos + new Vector2( delta.x * 0.5f, delta.y * -0.5f );
			window.sizeDelta = newSize;

			listView.OnViewportDimensionsChanged();
		}
		#endregion
	}
}