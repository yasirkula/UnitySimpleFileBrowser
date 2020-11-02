using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleFileBrowser
{
	public class FileBrowserDeleteConfirmationPanel : MonoBehaviour
	{
		public delegate void OnDeletionConfirmed();

#pragma warning disable 0649
		[SerializeField]
		private GameObject[] deletedItems;

		[SerializeField]
		private Image[] deletedItemIcons;

		[SerializeField]
		private Text[] deletedItemNames;

		[SerializeField]
		private GameObject deletedItemsRest;

		[SerializeField]
		private Text deletedItemsRestLabel;

		[SerializeField]
		private RectTransform yesButtonTransform;

		[SerializeField]
		private RectTransform noButtonTransform;

		[SerializeField]
		private float narrowScreenWidth = 380f;
#pragma warning restore 0649

		private OnDeletionConfirmed onDeletionConfirmed;

		internal void Show( FileBrowser fileBrowser, List<FileSystemEntry> items, List<int> selectedItemIndices, OnDeletionConfirmed onDeletionConfirmed )
		{
			this.onDeletionConfirmed = onDeletionConfirmed;

			for( int i = 0; i < deletedItems.Length; i++ )
				deletedItems[i].SetActive( i < selectedItemIndices.Count );

			for( int i = 0; i < deletedItems.Length && i < selectedItemIndices.Count; i++ )
			{
				deletedItemIcons[i].sprite = fileBrowser.GetIconForFileEntry( items[selectedItemIndices[i]] );
				deletedItemNames[i].text = items[selectedItemIndices[i]].Name;
			}

			if( selectedItemIndices.Count > deletedItems.Length )
			{
				deletedItemsRestLabel.text = string.Concat( "...and ", ( selectedItemIndices.Count - deletedItems.Length ).ToString(), " other" );
				deletedItemsRest.SetActive( true );
			}
			else
				deletedItemsRest.SetActive( false );

			gameObject.SetActive( true );
		}

		// Handles responsive user interface
		internal void OnCanvasDimensionsChanged( Vector2 size )
		{
			if( size.x >= narrowScreenWidth )
			{
				yesButtonTransform.anchorMin = new Vector2( 0.5f, 0f );
				yesButtonTransform.anchorMax = new Vector2( 0.75f, 1f );
				noButtonTransform.anchorMin = new Vector2( 0.75f, 0f );
			}
			else
			{
				yesButtonTransform.anchorMin = Vector2.zero;
				yesButtonTransform.anchorMax = new Vector2( 0.5f, 1f );
				noButtonTransform.anchorMin = new Vector2( 0.5f, 0f );
			}
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
		private void LateUpdate()
		{
			// Handle keyboard shortcuts
			if( Input.GetKeyDown( KeyCode.Return ) || Input.GetKeyDown( KeyCode.KeypadEnter ) )
				YesButtonClicked();

			if( Input.GetKeyDown( KeyCode.Escape ) )
				NoButtonClicked();
		}
#endif

		public void YesButtonClicked()
		{
			gameObject.SetActive( false );

			if( onDeletionConfirmed != null )
				onDeletionConfirmed();
		}

		public void NoButtonClicked()
		{
			gameObject.SetActive( false );
		}
	}
}