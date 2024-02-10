using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace SimpleFileBrowser
{
	public class FileBrowserFileOperationConfirmationPanel : MonoBehaviour
	{
		public enum OperationType { Delete = 0, Overwrite = 1 };

		public delegate void OnOperationConfirmed();

#pragma warning disable 0649
		[SerializeField]
		private VerticalLayoutGroup contentLayoutGroup;

		[SerializeField]
		private Text[] titleLabels;

		[SerializeField]
		private GameObject[] targetItems;

		[SerializeField]
		private Image[] targetItemIcons;

		[SerializeField]
		private Text[] targetItemNames;

		[SerializeField]
		private GameObject targetItemsRest;

		[SerializeField]
		private Text targetItemsRestLabel;

		[SerializeField]
		private Button yesButton;

		[SerializeField]
		private Button noButton;

		[SerializeField]
		private float narrowScreenWidth = 380f;
#pragma warning restore 0649

		private OnOperationConfirmed onOperationConfirmed;

		private void Awake()
		{
			yesButton.onClick.AddListener( OnYesButtonClicked );
			noButton.onClick.AddListener( OnNoButtonClicked );
		}

		internal void Show( FileBrowser fileBrowser, List<FileSystemEntry> items, OperationType operationType, OnOperationConfirmed onOperationConfirmed )
		{
			Show( fileBrowser, items, null, operationType, onOperationConfirmed );
		}

		internal void Show( FileBrowser fileBrowser, List<FileSystemEntry> items, List<int> selectedItemIndices, OperationType operationType, OnOperationConfirmed onOperationConfirmed )
		{
			this.onOperationConfirmed = onOperationConfirmed;

			int itemCount = ( selectedItemIndices != null ) ? selectedItemIndices.Count : items.Count;

			for( int i = 0; i < titleLabels.Length; i++ )
				titleLabels[i].gameObject.SetActive( (int) operationType == i );

			for( int i = 0; i < targetItems.Length; i++ )
				targetItems[i].SetActive( i < itemCount );

			for( int i = 0; i < targetItems.Length && i < itemCount; i++ )
			{
				FileSystemEntry item = items[( selectedItemIndices != null ) ? selectedItemIndices[i] : i];
				targetItemIcons[i].sprite = fileBrowser.GetIconForFileEntry( item );
				targetItemNames[i].text = item.Name;
			}

			if( itemCount > targetItems.Length )
			{
				targetItemsRestLabel.text = string.Concat( "...and ", ( itemCount - targetItems.Length ).ToString(), " other" );
				targetItemsRest.SetActive( true );
			}
			else
				targetItemsRest.SetActive( false );

			gameObject.SetActive( true );
		}

		// Handles responsive user interface
		internal void OnCanvasDimensionsChanged( Vector2 size )
		{
			if( size.x >= narrowScreenWidth )
			{
				( yesButton.transform as RectTransform ).anchorMin = new Vector2( 0.5f, 0f );
				( yesButton.transform as RectTransform ).anchorMax = new Vector2( 0.75f, 1f );
				( noButton.transform as RectTransform ).anchorMin = new Vector2( 0.75f, 0f );
			}
			else
			{
				( yesButton.transform as RectTransform ).anchorMin = Vector2.zero;
				( yesButton.transform as RectTransform ).anchorMax = new Vector2( 0.5f, 1f );
				( noButton.transform as RectTransform ).anchorMin = new Vector2( 0.5f, 0f );
			}
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
		private void LateUpdate()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( Keyboard.current != null )
#endif
			{
				// Handle keyboard shortcuts
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				if( Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.NumpadEnter].wasPressedThisFrame )
#else
				if( Input.GetKeyDown( KeyCode.Return ) || Input.GetKeyDown( KeyCode.KeypadEnter ) )
#endif
					OnYesButtonClicked();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				if( Keyboard.current[Key.Escape].wasPressedThisFrame )
#else
				if( Input.GetKeyDown( KeyCode.Escape ) )
#endif
					OnNoButtonClicked();
			}
		}
#endif

		internal void RefreshSkin( UISkin skin )
		{
			contentLayoutGroup.spacing = skin.RowSpacing;
			contentLayoutGroup.padding.bottom = 22 + (int) ( skin.RowSpacing + skin.RowHeight );

			Image background = GetComponentInChildren<Image>();
			background.color = skin.PopupPanelsBackgroundColor;
			background.sprite = skin.PopupPanelsBackground;

			RectTransform buttonsParent = yesButton.transform.parent as RectTransform;
			buttonsParent.sizeDelta = new Vector2( buttonsParent.sizeDelta.x, skin.RowHeight );

			skin.ApplyTo( yesButton );
			skin.ApplyTo( noButton );

			for( int i = 0; i < titleLabels.Length; i++ )
				skin.ApplyTo( titleLabels[i], skin.PopupPanelsTextColor );

			skin.ApplyTo( targetItemsRestLabel, skin.PopupPanelsTextColor );

			for( int i = 0; i < targetItemNames.Length; i++ )
				skin.ApplyTo( targetItemNames[i], skin.PopupPanelsTextColor );

			for( int i = 0; i < targetItems.Length; i++ )
				targetItems[i].GetComponent<LayoutElement>().preferredHeight = skin.FileHeight;
		}

		private void OnYesButtonClicked()
		{
			gameObject.SetActive( false );

			if( onOperationConfirmed != null )
				onOperationConfirmed();
		}

		private void OnNoButtonClicked()
		{
			gameObject.SetActive( false );
			onOperationConfirmed = null;
		}
	}
}