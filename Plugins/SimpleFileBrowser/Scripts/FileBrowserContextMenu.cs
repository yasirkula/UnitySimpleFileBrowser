using UnityEngine;
using UnityEngine.UI;

namespace SimpleFileBrowser
{
	public class FileBrowserContextMenu : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private FileBrowser fileBrowser;

		[SerializeField]
		private RectTransform rectTransform;

		[SerializeField]
		private Button selectAllButton;
		[SerializeField]
		private Button deselectAllButton;
		[SerializeField]
		private Button deleteButton;
		[SerializeField]
		private Button renameButton;

		[SerializeField]
		private GameObject selectAllButtonSeparator;

		[SerializeField]
		private float minDistanceToEdges = 10f;
#pragma warning restore 0649

		internal void Show( bool selectAllButtonVisible, bool deselectAllButtonVisible, bool deleteButtonVisible, bool renameButtonVisible, Vector2 position, bool isMoreOptionsMenu )
		{
			selectAllButton.gameObject.SetActive( selectAllButtonVisible );
			deselectAllButton.gameObject.SetActive( deselectAllButtonVisible );
			deleteButton.gameObject.SetActive( deleteButtonVisible );
			renameButton.gameObject.SetActive( renameButtonVisible );
			selectAllButtonSeparator.SetActive( !deselectAllButtonVisible );

			rectTransform.anchoredPosition = position;
			gameObject.SetActive( true );

			if( isMoreOptionsMenu )
				rectTransform.pivot = Vector2.one;
			else
			{
				// Find the optimal pivot value
				LayoutRebuilder.ForceRebuildLayoutImmediate( rectTransform );

				Vector2 size = rectTransform.sizeDelta;
				Vector2 canvasSize = fileBrowser.rectTransform.sizeDelta;

				// Take canvas' Pivot into consideration
				Vector2 positionOffset = canvasSize;
				positionOffset.Scale( fileBrowser.rectTransform.pivot );
				position += positionOffset;

				// Try bottom-right corner first
				Vector2 cornerPos = position + new Vector2( size.x + minDistanceToEdges, -size.y - minDistanceToEdges );
				if( cornerPos.x <= canvasSize.x && cornerPos.y >= 0f )
					rectTransform.pivot = new Vector2( 0f, 1f );
				else
				{
					// Try bottom-left corner
					cornerPos = position - new Vector2( size.x + minDistanceToEdges, size.y + minDistanceToEdges );
					if( cornerPos.x >= 0f && cornerPos.y >= 0f )
						rectTransform.pivot = Vector2.one;
					else
					{
						// Try top-right corner
						cornerPos = position + new Vector2( size.x + minDistanceToEdges, size.y + minDistanceToEdges );
						if( cornerPos.x <= canvasSize.x && cornerPos.y <= canvasSize.y )
							rectTransform.pivot = Vector2.zero;
						else
						{
							// Use top-left corner
							rectTransform.pivot = new Vector2( 1f, 0f );
						}
					}
				}
			}
		}

		public void Hide()
		{
			gameObject.SetActive( false );
		}

		public void OnSelectAllButtonClicked()
		{
			Hide();
			fileBrowser.SelectAllFiles();
		}

		public void OnDeselectAllButtonClicked()
		{
			Hide();
			fileBrowser.DeselectAllFiles();
		}

		public void OnCreateFolderButtonClicked()
		{
			Hide();
			fileBrowser.CreateNewFolder();
		}

		public void OnDeleteButtonClicked()
		{
			Hide();
			fileBrowser.DeleteSelectedFiles();
		}

		public void OnRenameButtonClicked()
		{
			Hide();
			fileBrowser.RenameSelectedFile();
		}
	}
}