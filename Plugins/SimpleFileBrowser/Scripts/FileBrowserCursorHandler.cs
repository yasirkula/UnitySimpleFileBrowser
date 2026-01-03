using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleFileBrowser
{
	public class FileBrowserCursorHandler : MonoBehaviour
#if UNITY_EDITOR || ( !UNITY_ANDROID && !UNITY_IOS )
		, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler
#endif
	{
#if UNITY_EDITOR || ( !UNITY_ANDROID && !UNITY_IOS )
		[SerializeField]
		private Texture2D resizeCursor;

		private bool isHovering;
		private bool isResizing;

		void IPointerEnterHandler.OnPointerEnter( PointerEventData eventData )
		{
			isHovering = true;

			if( !eventData.dragging )
				ShowResizeCursor();
		}

		void IPointerExitHandler.OnPointerExit( PointerEventData eventData )
		{
			isHovering = false;

			if( !isResizing )
				ShowDefaultCursor();
		}

		void IBeginDragHandler.OnBeginDrag( PointerEventData eventData )
		{
			isResizing = true;
			ShowResizeCursor();
		}

		void IEndDragHandler.OnEndDrag( PointerEventData eventData )
		{
			isResizing = false;

			if( !isHovering )
				ShowDefaultCursor();
		}

		private void ShowDefaultCursor()
		{
			Cursor.SetCursor( null, Vector2.zero, CursorMode.Auto );
		}

		private void ShowResizeCursor()
		{
			Cursor.SetCursor( resizeCursor, new Vector2( resizeCursor.width * 0.5f, resizeCursor.height * 0.5f ), CursorMode.Auto );
		}
#endif
	}
}