using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SimpleFileBrowser
{
	public class FileBrowser : MonoBehaviour, IListViewAdapter
	{
		public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };

		#region Structs
#pragma warning disable 0649
		[Serializable]
		private struct FiletypeIcon
		{
			public string extension;
			public Sprite icon;
		}

		[Serializable]
		private struct QuickLink
		{
#if UNITY_EDITOR || ( !UNITY_WSA && !UNITY_WSA_10_0 )
			public Environment.SpecialFolder target;
#endif
			public string name;
			public Sprite icon;
		}
#pragma warning restore 0649
		#endregion

		#region Inner Classes
		public class Filter
		{
			public readonly string name;
			public readonly HashSet<string> extensions;
			public readonly string defaultExtension;

			internal Filter( string name )
			{
				this.name = name;
				extensions = null;
				defaultExtension = null;
			}

			public Filter( string name, string extension )
			{
				this.name = name;

				extension = extension.ToLowerInvariant();
				extensions = new HashSet<string>() { extension };
				defaultExtension = extension;
			}

			public Filter( string name, params string[] extensions )
			{
				this.name = name;

				for( int i = 0; i < extensions.Length; i++ )
					extensions[i] = extensions[i].ToLowerInvariant();

				this.extensions = new HashSet<string>( extensions );
				defaultExtension = extensions[0];
			}

			public override string ToString()
			{
				string result = string.Empty;

				if( name != null )
					result += name;

				if( extensions != null )
				{
					if( name != null )
						result += " (";

					int index = 0;
					foreach( string extension in extensions )
					{
						if( index++ > 0 )
							result += ", " + extension;
						else
							result += extension;
					}

					if( name != null )
						result += ")";
				}

				return result;
			}
		}
		#endregion

		#region Constants
		private const string ALL_FILES_FILTER_TEXT = "All Files (.*)";
		private const string FOLDERS_FILTER_TEXT = "Folders";
		private string DEFAULT_PATH;

#if !UNITY_EDITOR && UNITY_ANDROID
		private const string SAF_PICK_FOLDER_QUICK_LINK_TEXT = "Pick Folder";
		private const string SAF_PICK_FOLDER_QUICK_LINK_PATH = "SAF_PICK_FOLDER";
#endif
		#endregion

		#region Static Variables
		public static bool IsOpen { get; private set; }

		public static bool Success { get; private set; }
		public static string[] Result { get; private set; }

		private static bool m_askPermissions = true;
		public static bool AskPermissions
		{
			get { return m_askPermissions; }
			set { m_askPermissions = value; }
		}

		private static bool m_singleClickMode = false;
		public static bool SingleClickMode
		{
			get { return m_singleClickMode; }
			set { m_singleClickMode = value; }
		}

		private static FileBrowser m_instance = null;
		private static FileBrowser Instance
		{
			get
			{
				if( !m_instance )
				{
					m_instance = Instantiate( Resources.Load<GameObject>( "SimpleFileBrowserCanvas" ) ).GetComponent<FileBrowser>();
					DontDestroyOnLoad( m_instance.gameObject );
					m_instance.gameObject.SetActive( false );
				}

				return m_instance;
			}
		}
		#endregion

		#region Variables
#pragma warning disable 0649
		[Header( "Settings" )]

		[SerializeField]
		internal Color normalFileColor = Color.white;
		[SerializeField]
		internal Color hoveredFileColor = new Color32( 225, 225, 255, 255 );
		[SerializeField]
		internal Color selectedFileColor = new Color32( 0, 175, 255, 255 );
		[SerializeField]
		internal Color wrongFilenameColor = new Color32( 255, 100, 100, 255 );

		[SerializeField]
		internal int minWidth = 380;
		[SerializeField]
		internal int minHeight = 300;

		[SerializeField]
		private float narrowScreenWidth = 380f;

		[SerializeField]
		private float quickLinksMaxWidthPercentage = 0.4f;

		[SerializeField]
		private string[] excludeExtensions;

#pragma warning disable 0414
		[SerializeField]
		private QuickLink[] quickLinks;
		private static bool quickLinksInitialized;
#pragma warning restore 0414

		private readonly HashSet<string> excludedExtensionsSet = new HashSet<string>();
		private readonly HashSet<string> addedQuickLinksSet = new HashSet<string>();

		[SerializeField]
		private bool generateQuickLinksForDrives = true;

		[SerializeField]
		private bool contextMenuShowDeleteButton = true;

		[SerializeField]
		private bool contextMenuShowRenameButton = true;

		[SerializeField]
		private bool showResizeCursor = true;

		[Header( "Icons" )]

		[SerializeField]
		private Sprite folderIcon;

		[SerializeField]
		private Sprite driveIcon;

		[SerializeField]
		private Sprite defaultIcon;

		[SerializeField]
		private FiletypeIcon[] filetypeIcons;

		private Dictionary<string, Sprite> filetypeToIcon;

		[SerializeField]
		internal Sprite multiSelectionToggleOffIcon;
		[SerializeField]
		internal Sprite multiSelectionToggleOnIcon;

		[Header( "Internal References" )]

		[SerializeField]
		private FileBrowserMovement window;
		private RectTransform windowTR;

		[SerializeField]
		private RectTransform topViewNarrowScreen;

		[SerializeField]
		private RectTransform middleView;
		private Vector2 middleViewOriginalPosition;
		private Vector2 middleViewOriginalSize;

		[SerializeField]
		private RectTransform middleViewQuickLinks;
		private Vector2 middleViewQuickLinksOriginalSize;

		[SerializeField]
		private RectTransform middleViewFiles;

		[SerializeField]
		private RectTransform middleViewSeparator;

		[SerializeField]
		private FileBrowserItem itemPrefab;
		private readonly List<FileBrowserItem> allItems = new List<FileBrowserItem>( 16 );
		private float itemHeight;

		[SerializeField]
		private FileBrowserQuickLink quickLinkPrefab;

		[SerializeField]
		private Text titleText;

		[SerializeField]
		private Button backButton;

		[SerializeField]
		private Button forwardButton;

		[SerializeField]
		private Button upButton;

		[SerializeField]
		private InputField pathInputField;

		[SerializeField]
		private RectTransform pathInputFieldSlotTop;

		[SerializeField]
		private RectTransform pathInputFieldSlotBottom;

		[SerializeField]
		private InputField searchInputField;

		[SerializeField]
		private RectTransform quickLinksContainer;

		[SerializeField]
		private RectTransform filesContainer;

		[SerializeField]
		private ScrollRect filesScrollRect;

		[SerializeField]
		private RecycledListView listView;

		[SerializeField]
		private InputField filenameInputField;

		[SerializeField]
		private Text filenameInputFieldOverlayText;

		[SerializeField]
		private Image filenameImage;

		[SerializeField]
		private Dropdown filtersDropdown;

		[SerializeField]
		private RectTransform filtersDropdownContainer;

		[SerializeField]
		private Text filterItemTemplate;

		[SerializeField]
		private Toggle showHiddenFilesToggle;

		[SerializeField]
		private Text submitButtonText;

		[SerializeField]
		private RectTransform moreOptionsContextMenuPosition;

		[SerializeField]
		private FileBrowserRenamedItem renameItem;

		[SerializeField]
		private FileBrowserContextMenu contextMenu;

		[SerializeField]
		private FileBrowserDeleteConfirmationPanel deleteConfirmationPanel;

		[SerializeField]
		private FileBrowserCursorHandler resizeCursorHandler;
#pragma warning restore 0649

		internal RectTransform rectTransform;
		private Canvas canvas;

		private FileAttributes ignoredFileAttributes = FileAttributes.System;

		private FileSystemEntry[] allFileEntries;
		private readonly List<FileSystemEntry> validFileEntries = new List<FileSystemEntry>();
		private readonly List<int> selectedFileEntries = new List<int>( 4 );
		private readonly List<string> pendingFileEntrySelection = new List<string>();

#pragma warning disable 0414 // Value is assigned but never used on Android & iOS
		private int multiSelectionPivotFileEntry;
#pragma warning restore 0414
		private StringBuilder multiSelectionFilenameBuilder;

		private readonly List<Filter> filters = new List<Filter>();
		private Filter allFilesFilter;

		private bool showAllFilesFilter = true;

		private int currentPathIndex = -1;
		private readonly List<string> pathsFollowed = new List<string>();

		private bool canvasDimensionsChanged;

		// Required in RefreshFiles() function
		private PointerEventData nullPointerEventData;
		#endregion

		#region Properties
		private string m_currentPath = string.Empty;
		private string CurrentPath
		{
			get { return m_currentPath; }
			set
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				if( !FileBrowserHelpers.ShouldUseSAF )
#endif
				if( value != null )
					value = GetPathWithoutTrailingDirectorySeparator( value.Trim() );

				if( value == null )
					return;

				if( m_currentPath != value )
				{
					if( !FileBrowserHelpers.DirectoryExists( value ) )
						return;

					m_currentPath = value;
					pathInputField.text = m_currentPath;

					if( currentPathIndex == -1 || pathsFollowed[currentPathIndex] != m_currentPath )
					{
						currentPathIndex++;
						if( currentPathIndex < pathsFollowed.Count )
						{
							pathsFollowed[currentPathIndex] = value;
							for( int i = pathsFollowed.Count - 1; i >= currentPathIndex + 1; i-- )
								pathsFollowed.RemoveAt( i );
						}
						else
							pathsFollowed.Add( m_currentPath );
					}

					backButton.interactable = currentPathIndex > 0;
					forwardButton.interactable = currentPathIndex < pathsFollowed.Count - 1;
#if !UNITY_EDITOR && UNITY_ANDROID
					if( !FileBrowserHelpers.ShouldUseSAF )
#endif
					upButton.interactable = Directory.GetParent( m_currentPath ) != null;

					m_searchString = string.Empty;
					searchInputField.text = m_searchString;

					multiSelectionPivotFileEntry = 0;
					filesScrollRect.verticalNormalizedPosition = 1;

					filenameImage.color = Color.white;
					if( m_folderSelectMode )
						filenameInputField.text = string.Empty;
				}

				m_multiSelectionToggleSelectionMode = false;
				RefreshFiles( true );
			}
		}

		private string m_searchString = string.Empty;
		private string SearchString
		{
			get
			{
				return m_searchString;
			}
			set
			{
				if( m_searchString != value )
				{
					m_searchString = value;
					searchInputField.text = m_searchString;

					RefreshFiles( false );
				}
			}
		}

		private bool m_acceptNonExistingFilename = false;
		private bool AcceptNonExistingFilename
		{
			get { return m_acceptNonExistingFilename; }
			set { m_acceptNonExistingFilename = value; }
		}

		private bool m_folderSelectMode = false;
		private bool FolderSelectMode
		{
			get
			{
				return m_folderSelectMode;
			}
			set
			{
				if( m_folderSelectMode != value )
				{
					m_folderSelectMode = value;

					if( m_folderSelectMode )
					{
						filtersDropdown.options[0].text = FOLDERS_FILTER_TEXT;
						filtersDropdown.value = 0;
						filtersDropdown.RefreshShownValue();
						filtersDropdown.interactable = false;
					}
					else
					{
						filtersDropdown.options[0].text = filters[0].ToString();
						filtersDropdown.interactable = true;
					}

					Text placeholder = filenameInputField.placeholder as Text;
					if( placeholder != null )
						placeholder.text = m_folderSelectMode ? string.Empty : "Filename";
				}
			}
		}

		private bool m_allowMultiSelection;
		internal bool AllowMultiSelection
		{
			get { return m_allowMultiSelection; }
			private set { m_allowMultiSelection = value; }
		}

		private bool m_multiSelectionToggleSelectionMode;
		internal bool MultiSelectionToggleSelectionMode
		{
			get { return m_multiSelectionToggleSelectionMode; }
			set
			{
				if( m_multiSelectionToggleSelectionMode != value )
				{
					m_multiSelectionToggleSelectionMode = value;

					for( int i = 0; i < allItems.Count; i++ )
					{
						if( allItems[i].gameObject.activeSelf )
							allItems[i].SetSelected( selectedFileEntries.Contains( allItems[i].Position ) );
					}
				}
			}
		}

		private string Title
		{
			get { return titleText.text; }
			set { titleText.text = value; }
		}

		private string SubmitButtonText
		{
			get { return submitButtonText.text; }
			set { submitButtonText.text = value; }
		}
		#endregion

		#region Delegates
		public delegate void OnSuccess( string[] paths );
		public delegate void OnCancel();
#if !UNITY_EDITOR && UNITY_ANDROID
		public delegate void DirectoryPickCallback( string rawUri, string name );
#endif

		private OnSuccess onSuccess;
		private OnCancel onCancel;
		#endregion

		#region Messages
		private void Awake()
		{
			m_instance = this;

			rectTransform = (RectTransform) transform;
			windowTR = (RectTransform) window.transform;
			canvas = GetComponent<Canvas>();

			middleViewOriginalPosition = middleView.anchoredPosition;
			middleViewOriginalSize = middleView.sizeDelta;
			middleViewQuickLinksOriginalSize = middleViewQuickLinks.sizeDelta;

			itemHeight = ( (RectTransform) itemPrefab.transform ).sizeDelta.y;
			nullPointerEventData = new PointerEventData( null );

#if !UNITY_EDITOR && ( UNITY_ANDROID || UNITY_IOS || UNITY_WSA || UNITY_WSA_10_0 )
			DEFAULT_PATH = Application.persistentDataPath;
#else
			DEFAULT_PATH = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
#endif

#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAF )
			{
				// These UI elements have no use in Storage Access Framework mode (Android 10+)
				upButton.gameObject.SetActive( false );
				pathInputField.gameObject.SetActive( false );
				showHiddenFilesToggle.gameObject.SetActive( false );
			}
#endif

			InitializeFiletypeIcons();
			filetypeIcons = null;

			SetExcludedExtensions( excludeExtensions );
			excludeExtensions = null;

			backButton.interactable = false;
			forwardButton.interactable = false;
			upButton.interactable = false;

			filenameInputField.onValidateInput += OnValidateFilenameInput;
			filenameInputField.onValueChanged.AddListener( OnFilenameInputChanged );

			allFilesFilter = new Filter( ALL_FILES_FILTER_TEXT );
			filters.Add( allFilesFilter );

			window.Initialize( this );
			listView.SetAdapter( this );

			if( !showResizeCursor )
				Destroy( resizeCursorHandler );
		}

		private void OnRectTransformDimensionsChange()
		{
			canvasDimensionsChanged = true;
		}

		private void LateUpdate()
		{
			if( canvasDimensionsChanged )
			{
				canvasDimensionsChanged = false;

				Vector2 windowSize = windowTR.sizeDelta;
				EnsureWindowIsWithinBounds();
				if( windowTR.sizeDelta != windowSize )
					OnWindowDimensionsChanged( windowTR.sizeDelta );

				deleteConfirmationPanel.OnCanvasDimensionsChanged( rectTransform.sizeDelta );

				if( contextMenu.gameObject.activeSelf )
					contextMenu.Hide();
			}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
			// Handle keyboard shortcuts
			if( !EventSystem.current.currentSelectedGameObject )
			{
				if( Input.GetKeyDown( KeyCode.Delete ) )
					DeleteSelectedFiles();

				if( Input.GetKeyDown( KeyCode.F2 ) )
					RenameSelectedFile();

				if( Input.GetKeyDown( KeyCode.A ) && ( Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.LeftCommand ) ) )
					SelectAllFiles();
			}
#endif

			// 2 Text objects are used in the filename input field:
			// filenameInputField.textComponent: visible when editing the text, has Horizontal Overflow set to Wrap (cuts out words, ugly)
			// filenameInputFieldOverlayText: visible when not editing the text, has Horizontal Overflow set to Overflow (doesn't cut out words)
			if( EventSystem.current.currentSelectedGameObject == filenameInputField.gameObject )
			{
				if( filenameInputFieldOverlayText.enabled )
				{
					filenameInputFieldOverlayText.enabled = false;

					Color c = filenameInputField.textComponent.color;
					c.a = 1f;
					filenameInputField.textComponent.color = c;
				}
			}
			else if( !filenameInputFieldOverlayText.enabled )
			{
				filenameInputFieldOverlayText.enabled = true;

				Color c = filenameInputField.textComponent.color;
				c.a = 0f;
				filenameInputField.textComponent.color = c;
			}
		}

		private void OnApplicationFocus( bool focus )
		{
			if( !focus )
				PersistFileEntrySelection();
			else
				RefreshFiles( true );
		}
		#endregion

		#region Interface Methods
		OnItemClickedHandler IListViewAdapter.OnItemClicked { get { return null; } set { } }

		int IListViewAdapter.Count { get { return validFileEntries.Count; } }
		float IListViewAdapter.ItemHeight { get { return itemHeight; } }

		ListItem IListViewAdapter.CreateItem()
		{
			FileBrowserItem item = (FileBrowserItem) Instantiate( itemPrefab, filesContainer, false );
			item.SetFileBrowser( this );
			allItems.Add( item );

			return item;
		}

		void IListViewAdapter.SetItemContent( ListItem item )
		{
			FileBrowserItem file = (FileBrowserItem) item;
			FileSystemEntry fileInfo = validFileEntries[item.Position];

			file.SetFile( GetIconForFileEntry( fileInfo ), fileInfo.Name, fileInfo.IsDirectory );
			file.SetSelected( selectedFileEntries.Contains( file.Position ) );
			file.SetHidden( ( fileInfo.Attributes & FileAttributes.Hidden ) == FileAttributes.Hidden );
		}
		#endregion

		#region Initialization Functions
		private void InitializeFiletypeIcons()
		{
			filetypeToIcon = new Dictionary<string, Sprite>();
			for( int i = 0; i < filetypeIcons.Length; i++ )
			{
				FiletypeIcon thisIcon = filetypeIcons[i];
				filetypeToIcon[thisIcon.extension] = thisIcon.icon;
			}
		}

		private void InitializeQuickLinks()
		{
			Vector2 anchoredPos = new Vector2( 0f, -quickLinksContainer.sizeDelta.y );

#if !UNITY_EDITOR && UNITY_ANDROID
			if( !FileBrowserHelpers.ShouldUseSAF )
			{
#endif
			if( generateQuickLinksForDrives )
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				string drivesList = FileBrowserHelpers.AJC.CallStatic<string>( "GetExternalDrives" );
				if( drivesList != null && drivesList.Length > 0 )
				{
					bool defaultPathInitialized = false;
					int driveIndex = 1;
					string[] drives = drivesList.Split( ':' );
					for( int i = 0; i < drives.Length; i++ )
					{
						try
						{
							//string driveName = new DirectoryInfo( drives[i] ).Name;
							//if( driveName.Length <= 1 )
							//{
							//	try
							//	{
							//		driveName = Directory.GetParent( drives[i] ).Name + "/" + driveName;
							//	}
							//	catch
							//	{
							//		driveName = "Drive " + driveIndex++;
							//	}
							//}	

							string driveName;
							if( !defaultPathInitialized )
							{
								DEFAULT_PATH = drives[i];
								defaultPathInitialized = true;

								driveName = "Primary Drive";
							}
							else
							{
								if( driveIndex == 1 )
									driveName = "External Drive";
								else
									driveName = "External Drive " + driveIndex;

								driveIndex++;
							}

							AddQuickLink( driveIcon, driveName, drives[i], ref anchoredPos );
						}
						catch { }
					}
				}
#elif !UNITY_EDITOR && ( UNITY_IOS || UNITY_WSA || UNITY_WSA_10_0 )
				AddQuickLink( driveIcon, "Files", Application.persistentDataPath, ref anchoredPos );
#else
				string[] drives = Directory.GetLogicalDrives();

				for( int i = 0; i < drives.Length; i++ )
				{
					if( string.IsNullOrEmpty( drives[i] ) )
						continue;

#if UNITY_STANDALONE_OSX
					// There are a number of useless drives listed on Mac OS, filter them
					if( drives[i] == "/" )
						AddQuickLink( driveIcon, "Root", drives[i], ref anchoredPos );
					else if( drives[i].StartsWith( "/Volumes/" ) && drives[i] != "/Volumes/Recovery" )
						AddQuickLink( driveIcon, drives[i].Substring( drives[i].LastIndexOf( '/' ) + 1 ), drives[i], ref anchoredPos );
#else
					AddQuickLink( driveIcon, drives[i], drives[i], ref anchoredPos );
#endif
				}

#if UNITY_STANDALONE_OSX
				// Add a quick link for user directory on Mac OS
				string userDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
				if( !string.IsNullOrEmpty( userDirectory ) )
					AddQuickLink( driveIcon, userDirectory.Substring( userDirectory.LastIndexOf( '/' ) + 1 ), userDirectory, ref anchoredPos );
#endif
#endif
			}

#if UNITY_EDITOR || ( !UNITY_ANDROID && !UNITY_WSA && !UNITY_WSA_10_0 )
			for( int i = 0; i < quickLinks.Length; i++ )
			{
				QuickLink quickLink = quickLinks[i];
				string quickLinkPath = Environment.GetFolderPath( quickLink.target );
#if UNITY_STANDALONE_OSX
				// Documents folder must be appended manually on Mac OS
				if( quickLink.target == Environment.SpecialFolder.MyDocuments && !string.IsNullOrEmpty( quickLinkPath ) )
					quickLinkPath = Path.Combine( quickLinkPath, "Documents" );
#endif

				AddQuickLink( quickLink.icon, quickLink.name, quickLinkPath, ref anchoredPos );
			}

			quickLinks = null;
#endif
#if !UNITY_EDITOR && UNITY_ANDROID
			}
			else
			{
				AddQuickLink( driveIcon, SAF_PICK_FOLDER_QUICK_LINK_TEXT, SAF_PICK_FOLDER_QUICK_LINK_PATH, ref anchoredPos );
				
				try
				{
					FetchPersistedSAFQuickLinks( ref anchoredPos );
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}
#endif

			quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
		}
		#endregion

		#region Button Events
		public void OnBackButtonPressed()
		{
			if( currentPathIndex > 0 )
				CurrentPath = pathsFollowed[--currentPathIndex];
		}

		public void OnForwardButtonPressed()
		{
			if( currentPathIndex < pathsFollowed.Count - 1 )
				CurrentPath = pathsFollowed[++currentPathIndex];
		}

		public void OnUpButtonPressed()
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAF )
				return;
#endif
			DirectoryInfo parentPath = Directory.GetParent( m_currentPath );

			if( parentPath != null )
				CurrentPath = parentPath.FullName;
		}

		public void OnMoreOptionsButtonClicked()
		{
			ShowContextMenuAt( rectTransform.InverseTransformPoint( moreOptionsContextMenuPosition.position ), true );
		}

		internal void OnContextMenuTriggered()
		{
			filesScrollRect.velocity = Vector2.zero;

			Vector2 position;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, Input.mousePosition, canvas.worldCamera, out position );

			ShowContextMenuAt( position, false );
		}

		private void ShowContextMenuAt( Vector2 position, bool isMoreOptionsMenu )
		{
			if( string.IsNullOrEmpty( m_currentPath ) )
				return;

			bool selectAllButtonVisible = isMoreOptionsMenu && m_allowMultiSelection && validFileEntries.Count > 0;
			bool deselectAllButtonVisible = isMoreOptionsMenu && selectedFileEntries.Count > 1;
			bool deleteButtonVisible = contextMenuShowDeleteButton && selectedFileEntries.Count > 0;
			bool renameButtonVisible = contextMenuShowRenameButton && selectedFileEntries.Count == 1;

			contextMenu.Show( selectAllButtonVisible, deselectAllButtonVisible, deleteButtonVisible, renameButtonVisible, position, isMoreOptionsMenu );
		}

		public void OnSubmitButtonClicked()
		{
			string filenameInput = filenameInputField.text.Trim();
			if( filenameInput.Length == 0 )
			{
				if( m_folderSelectMode )
					OnOperationSuccessful( new string[1] { m_currentPath } );
				else
					filenameImage.color = wrongFilenameColor;

				return;
			}

			// In the first iteration, verify that all filenames entered to the input field are valid
			// ExtractFilenameFromInput doesn't use Substring, so this iteration is GC-free
			int startIndex = 0, nextStartIndex;
			int fileCount = 0;
			int indexOfDirectoryEntryToOpen = -1;
			while( startIndex < filenameInput.Length )
			{
				int filenameLength = ExtractFilenameFromInput( filenameInput, ref startIndex, out nextStartIndex );
				if( filenameLength == 0 )
					continue;

				if( m_acceptNonExistingFilename )
					fileCount++;
				else
				{
					int fileEntryIndex = FilenameInputToFileEntryIndex( filenameInput, startIndex, filenameLength );
					if( fileEntryIndex < 0 )
					{
						// File doesn't exist
						filenameImage.color = wrongFilenameColor;
						return;
					}

					if( validFileEntries[fileEntryIndex].IsDirectory )
					{
						if( m_folderSelectMode )
							fileCount++;
						else
						{
							// Selected a directory in file selection mode, we'll open that directory if no files are selected
							indexOfDirectoryEntryToOpen = fileEntryIndex;
						}
					}
					else
					{
						if( !m_folderSelectMode )
							fileCount++;
						else
						{
							// Can't select a file in folder selection mode
							filenameImage.color = wrongFilenameColor;
							return;
						}
					}
				}

				startIndex = nextStartIndex;
			}

			if( indexOfDirectoryEntryToOpen >= 0 )
				CurrentPath = validFileEntries[indexOfDirectoryEntryToOpen].Path;
			else if( fileCount == 0 )
				filenameImage.color = wrongFilenameColor;
			else
			{
				string[] result = new string[fileCount];

				// In the second iteration, extract filenames from the input field
				startIndex = 0;
				fileCount = 0;
				while( startIndex < filenameInput.Length )
				{
					int filenameLength = ExtractFilenameFromInput( filenameInput, ref startIndex, out nextStartIndex );
					if( filenameLength == 0 )
						continue;

					int fileEntryIndex = FilenameInputToFileEntryIndex( filenameInput, startIndex, filenameLength );
					if( fileEntryIndex >= 0 )
					{
						// This is an existing file
						result[fileCount++] = validFileEntries[fileEntryIndex].Path;
					}
					else
					{
						// This is a nonexisting file
						string filename = filenameInput.Substring( startIndex, filenameLength );
						if( !m_folderSelectMode && filters[filtersDropdown.value].defaultExtension != null )
						{
							// In file selection mode, make sure that nonexisting files' extensions match one of the required extensions
							string fileExtension = Path.GetExtension( filename );
							if( string.IsNullOrEmpty( fileExtension ) || !filters[filtersDropdown.value].extensions.Contains( fileExtension.ToLowerInvariant() ) )
								filename = Path.ChangeExtension( filename, filters[filtersDropdown.value].defaultExtension );
						}

#if !UNITY_EDITOR && UNITY_ANDROID
						if( FileBrowserHelpers.ShouldUseSAF )
						{
							if( m_folderSelectMode )
								result[fileCount++] = FileBrowserHelpers.CreateFolderInDirectory( m_currentPath, filename );
							else
								result[fileCount++] = FileBrowserHelpers.CreateFileInDirectory( m_currentPath, filename );
						}
						else
#endif
						{
							result[fileCount++] = Path.Combine( m_currentPath, filename );
						}
					}

					startIndex = nextStartIndex;
				}

				OnOperationSuccessful( result );
			}
		}

		public void OnCancelButtonClicked()
		{
			OnOperationCanceled( true );
		}
		#endregion

		#region Other Events
		private void OnOperationSuccessful( string[] paths )
		{
			Success = true;
			Result = paths;

			Hide();

			OnSuccess _onSuccess = onSuccess;
			onSuccess = null;
			onCancel = null;

			if( _onSuccess != null )
				_onSuccess( paths );
		}

		private void OnOperationCanceled( bool invokeCancelCallback )
		{
			Success = false;
			Result = null;

			Hide();

			OnCancel _onCancel = onCancel;
			onSuccess = null;
			onCancel = null;

			if( invokeCancelCallback && _onCancel != null )
				_onCancel();
		}

		public void OnPathChanged( string newPath )
		{
			CurrentPath = newPath;
		}

		public void OnSearchStringChanged( string newSearchString )
		{
			PersistFileEntrySelection();
			SearchString = newSearchString;
		}

		public void OnFilterChanged()
		{
			PersistFileEntrySelection();
			RefreshFiles( false );
		}

		public void OnShowHiddenFilesToggleChanged()
		{
			PersistFileEntrySelection();
			RefreshFiles( false );
		}

		public void OnItemSelected( FileBrowserItem item, bool isDoubleClick )
		{
			if( item == null )
				return;

			if( item is FileBrowserQuickLink )
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				if( ( (FileBrowserQuickLink) item ).TargetPath == SAF_PICK_FOLDER_QUICK_LINK_PATH )
					FileBrowserHelpers.AJC.CallStatic( "PickSAFFolder", FileBrowserHelpers.Context, new FBDirectoryReceiveCallbackAndroid( OnSAFDirectoryPicked ) );
				else
#endif
				CurrentPath = ( (FileBrowserQuickLink) item ).TargetPath;

				return;
			}

			// We want to toggle the selected states of the files even when they are double clicked
			if( m_multiSelectionToggleSelectionMode )
				isDoubleClick = false;

			if( !isDoubleClick )
			{
				if( !m_allowMultiSelection )
				{
					selectedFileEntries.Clear();
					selectedFileEntries.Add( item.Position );
				}
				else
				{
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
					// When Shift key is held, all items from the pivot item to the clicked item will be selected
					if( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) )
					{
						multiSelectionPivotFileEntry = Mathf.Clamp( multiSelectionPivotFileEntry, 0, validFileEntries.Count - 1 );

						selectedFileEntries.Clear();
						selectedFileEntries.Add( item.Position );

						for( int i = multiSelectionPivotFileEntry; i < item.Position; i++ )
							selectedFileEntries.Add( i );

						for( int i = multiSelectionPivotFileEntry; i > item.Position; i-- )
							selectedFileEntries.Add( i );
					}
					else
#endif
					{
						multiSelectionPivotFileEntry = item.Position;

						// When in toggle selection mode or Control key is held, individual items can be multi-selected
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
						if( m_multiSelectionToggleSelectionMode || Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) )
#else
						if( m_multiSelectionToggleSelectionMode )
#endif
						{
							if( !selectedFileEntries.Contains( item.Position ) )
								selectedFileEntries.Add( item.Position );
							else
							{
								selectedFileEntries.Remove( item.Position );

								if( selectedFileEntries.Count == 0 )
									MultiSelectionToggleSelectionMode = false;
							}
						}
						else
						{
							selectedFileEntries.Clear();
							selectedFileEntries.Add( item.Position );
						}
					}
				}

				UpdateFilenameInputFieldWithSelection();
			}

			for( int i = 0; i < allItems.Count; i++ )
			{
				if( allItems[i].gameObject.activeSelf )
					allItems[i].SetSelected( selectedFileEntries.Contains( allItems[i].Position ) );
			}

			if( selectedFileEntries.Count > 0 && ( isDoubleClick || ( SingleClickMode && !m_multiSelectionToggleSelectionMode ) ) )
			{
				if( !item.IsDirectory )
				{
					// Submit selection
					OnSubmitButtonClicked();
				}
				else
				{
					// Enter the directory
#if !UNITY_EDITOR && UNITY_ANDROID
					if( FileBrowserHelpers.ShouldUseSAF )
					{
						for( int i = 0; i < validFileEntries.Count; i++ )
						{
							FileSystemEntry fileInfo = validFileEntries[i];
							if( fileInfo.IsDirectory && fileInfo.Name == item.Name )
							{
								CurrentPath = fileInfo.Path;
								return;
							}
						}
					}
					else
#endif
					CurrentPath = Path.Combine( m_currentPath, item.Name );
				}
			}
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		private void OnSAFDirectoryPicked( string rawUri, string name )
		{
			if( !string.IsNullOrEmpty( rawUri ) )
			{
				Vector2 anchoredPos = new Vector2( 0f, -quickLinksContainer.sizeDelta.y );

				if( AddQuickLink( folderIcon, name, rawUri, ref anchoredPos ) )
				{
					quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
					CurrentPath = rawUri;
				}
			}
		}

		private void FetchPersistedSAFQuickLinks( ref Vector2 anchoredPos )
		{
			string resultRaw = FileBrowserHelpers.AJC.CallStatic<string>( "FetchSAFQuickLinks", FileBrowserHelpers.Context );
			if( resultRaw == "0" )
				return;

			int separatorIndex = resultRaw.LastIndexOf( "<>" );
			if( separatorIndex <= 0 )
			{
				Debug.LogError( "Entry count does not exist" );
				return;
			}

			int entryCount = 0;
			for( int i = separatorIndex + 2; i < resultRaw.Length; i++ )
			{
				char ch = resultRaw[i];
				if( ch < '0' && ch > '9' )
				{
					Debug.LogError( "Couldn't parse entry count" );
					return;
				}
					
				entryCount = entryCount * 10 + ( ch - '0' );
			}

			if( entryCount <= 0 )
				return;
		
			bool defaultPathInitialized = false;

			separatorIndex = 0;
			for( int i = 0; i < entryCount; i++ )
			{
				int nextSeparatorIndex = resultRaw.IndexOf( "<>", separatorIndex );
				if( nextSeparatorIndex <= 0 )
				{
					Debug.LogError( "Entry name is empty" );
					return;
				}

				string entryName = resultRaw.Substring( separatorIndex, nextSeparatorIndex - separatorIndex );

				separatorIndex = nextSeparatorIndex + 2;
				nextSeparatorIndex = resultRaw.IndexOf( "<>", separatorIndex );
				if( nextSeparatorIndex <= 0 )
				{
					Debug.LogError( "Entry rawUri is empty" );
					return;
				}

				string rawUri = resultRaw.Substring( separatorIndex, nextSeparatorIndex - separatorIndex );

				separatorIndex = nextSeparatorIndex + 2;

				if( AddQuickLink( folderIcon, entryName, rawUri, ref anchoredPos ) && !defaultPathInitialized )
				{
					DEFAULT_PATH = rawUri;
					defaultPathInitialized = true;
				}
			}
		}
#endif

		private char OnValidateFilenameInput( string text, int charIndex, char addedChar )
		{
			if( addedChar == '\n' )
			{
				OnSubmitButtonClicked();
				return '\0';
			}

			return addedChar;
		}

		private void OnFilenameInputChanged( string text )
		{
			filenameInputFieldOverlayText.text = text;
			filenameImage.color = Color.white;
		}
		#endregion

		#region Helper Functions
		public void Show( string initialPath )
		{
			if( AskPermissions )
				RequestPermission();

			if( !quickLinksInitialized )
			{
				quickLinksInitialized = true;
				InitializeQuickLinks();
			}

			selectedFileEntries.Clear();
			m_multiSelectionToggleSelectionMode = false;

			m_searchString = string.Empty;
			searchInputField.text = m_searchString;

			filesScrollRect.verticalNormalizedPosition = 1;

			filenameInputField.text = string.Empty;
			filenameImage.color = Color.white;

			IsOpen = true;
			Success = false;
			Result = null;

			gameObject.SetActive( true );

			CurrentPath = GetInitialPath( initialPath );
		}

		public void Hide()
		{
			IsOpen = false;

			currentPathIndex = -1;
			pathsFollowed.Clear();

			backButton.interactable = false;
			forwardButton.interactable = false;
			upButton.interactable = false;

			gameObject.SetActive( false );
		}

		public void RefreshFiles( bool pathChanged )
		{
			if( pathChanged )
			{
				if( !string.IsNullOrEmpty( m_currentPath ) )
					allFileEntries = FileBrowserHelpers.GetEntriesInDirectory( m_currentPath );
				else
					allFileEntries = null;
			}

			selectedFileEntries.Clear();

			if( !showHiddenFilesToggle.isOn )
				ignoredFileAttributes |= FileAttributes.Hidden;
			else
				ignoredFileAttributes &= ~FileAttributes.Hidden;

			string searchStringLowercase = m_searchString.ToLower();

			validFileEntries.Clear();

			if( allFileEntries != null )
			{
				for( int i = 0; i < allFileEntries.Length; i++ )
				{
					try
					{
						FileSystemEntry item = allFileEntries[i];

						if( !item.IsDirectory )
						{
							if( m_folderSelectMode )
								continue;

							if( ( item.Attributes & ignoredFileAttributes ) != 0 )
								continue;

							string extension = item.Extension.ToLowerInvariant();
							if( excludedExtensionsSet.Contains( extension ) )
								continue;

							HashSet<string> extensions = filters[filtersDropdown.value].extensions;
							if( extensions != null && !extensions.Contains( extension ) )
								continue;
						}
						else
						{
							if( ( item.Attributes & ignoredFileAttributes ) != 0 )
								continue;
						}

						if( m_searchString.Length == 0 || item.Name.ToLower().Contains( searchStringLowercase ) )
							validFileEntries.Add( item );
					}
					catch( Exception e )
					{
						Debug.LogException( e );
					}
				}
			}

			// Restore the selection
			if( pendingFileEntrySelection.Count > 0 )
			{
				for( int i = 0; i < pendingFileEntrySelection.Count; i++ )
				{
					string pendingFileEntry = pendingFileEntrySelection[i];
					for( int j = 0; j < validFileEntries.Count; j++ )
					{
						if( validFileEntries[j].Name == pendingFileEntry )
						{
							selectedFileEntries.Add( j );
							break;
						}
					}
				}

				pendingFileEntrySelection.Clear();
			}

			listView.UpdateList();

			// Prevent the case where all the content stays offscreen after changing the search string
			filesScrollRect.OnScroll( nullPointerEventData );
		}

		// Quickly selects all files and folders in the current directory
		public void SelectAllFiles()
		{
			if( !m_allowMultiSelection || validFileEntries.Count == 0 )
				return;

			multiSelectionPivotFileEntry = 0;

			selectedFileEntries.Clear();
			for( int i = 0; i < validFileEntries.Count; i++ )
				selectedFileEntries.Add( i );

#if !UNITY_EDITOR && !UNITY_STANDALONE && !UNITY_WSA && !UNITY_WSA_10_0
			MultiSelectionToggleSelectionMode = true;
#endif

			UpdateFilenameInputFieldWithSelection();
			listView.UpdateList();
		}

		// Quickly deselects all files and folders in the current directory
		public void DeselectAllFiles()
		{
			if( selectedFileEntries.Count == 0 )
				return;

			selectedFileEntries.Clear();
			MultiSelectionToggleSelectionMode = false;

			filenameInputField.text = string.Empty;
			listView.UpdateList();
		}

		// Prompts user to create a new folder in the current directory
		public void CreateNewFolder()
		{
			StartCoroutine( CreateNewFolderCoroutine() );
		}

		private IEnumerator CreateNewFolderCoroutine()
		{
			filesScrollRect.verticalNormalizedPosition = 1f;
			filesScrollRect.velocity = Vector2.zero;

			if( selectedFileEntries.Count > 0 )
			{
				selectedFileEntries.Clear();
				MultiSelectionToggleSelectionMode = false;

				listView.UpdateList();
			}

			filesScrollRect.movementType = ScrollRect.MovementType.Unrestricted;

			// The easiest way to insert a new item to the top of the list view is to just shift
			// the list view downwards. However, it doesn't always work if we don't shift it twice
			yield return null;
			filesContainer.anchoredPosition = new Vector2( 0f, -itemHeight );
			yield return null;
			filesContainer.anchoredPosition = new Vector2( 0f, -itemHeight );

			( (RectTransform) renameItem.transform ).anchoredPosition = new Vector2( 1f, itemHeight );
			renameItem.Show( string.Empty, selectedFileColor, folderIcon, ( folderName ) =>
			{
				filesScrollRect.movementType = ScrollRect.MovementType.Clamped;
				filesContainer.anchoredPosition = Vector2.zero;

				if( string.IsNullOrEmpty( folderName ) )
					return;

				FileBrowserHelpers.CreateFolderInDirectory( CurrentPath, folderName );

				pendingFileEntrySelection.Clear();
				pendingFileEntrySelection.Add( folderName );

				RefreshFiles( true );

				if( m_folderSelectMode )
					filenameInputField.text = folderName;

				// Focus on the newly created folder
				int fileEntryIndex = 0;
				for( int i = 0; i < validFileEntries.Count; i++ )
				{
					if( validFileEntries[i].Name == folderName )
					{
						fileEntryIndex = i;
						break;
					}
				}

				filesScrollRect.verticalNormalizedPosition = validFileEntries.Count > 1 ? ( 1f - (float) fileEntryIndex / ( validFileEntries.Count - 1 ) ) : 1f;
			} );
		}

		// Prompts user to rename the selected file/folder
		public void RenameSelectedFile()
		{
			if( selectedFileEntries.Count != 1 )
				return;

			MultiSelectionToggleSelectionMode = false;

			int fileEntryIndex = selectedFileEntries[0];
			FileSystemEntry fileInfo = validFileEntries[fileEntryIndex];

			// Check if selected file is currently visible in ScrollRect
			// We consider it visible if both the previous file entry and the next file entry are visible
			bool prevFileEntryVisible = false, nextFileEntryVisible = false;
			for( int i = 0; i < allItems.Count; i++ )
			{
				if( !allItems[i].gameObject.activeSelf )
					continue;

				if( allItems[i].Position == fileEntryIndex - 1 )
				{
					prevFileEntryVisible = true;

					if( prevFileEntryVisible && nextFileEntryVisible )
						break;
				}
				else if( allItems[i].Position == fileEntryIndex + 1 )
				{
					nextFileEntryVisible = true;

					if( prevFileEntryVisible && nextFileEntryVisible )
						break;
				}
			}

			if( !prevFileEntryVisible || !nextFileEntryVisible )
				filesScrollRect.verticalNormalizedPosition = validFileEntries.Count > 1 ? ( 1f - (float) fileEntryIndex / ( validFileEntries.Count - 1 ) ) : 1f;

			filesScrollRect.velocity = Vector2.zero;

			( (RectTransform) renameItem.transform ).anchoredPosition = new Vector2( 1f, -fileEntryIndex * itemHeight );
			renameItem.Show( fileInfo.Name, selectedFileColor, GetIconForFileEntry( fileInfo ), ( newName ) =>
			{
				if( string.IsNullOrEmpty( newName ) || newName == fileInfo.Name )
					return;

				if( fileInfo.IsDirectory )
					FileBrowserHelpers.RenameDirectory( fileInfo.Path, newName );
				else
					FileBrowserHelpers.RenameFile( fileInfo.Path, newName );

				pendingFileEntrySelection.Clear();
				pendingFileEntrySelection.Add( newName );

				RefreshFiles( true );

				if( fileInfo.IsDirectory == m_folderSelectMode )
					filenameInputField.text = newName;
			} );
		}

		// Prompts user to delete the selected files & folders
		public void DeleteSelectedFiles()
		{
			if( selectedFileEntries.Count == 0 )
				return;

			selectedFileEntries.Sort();

			Sprite[] icons = new Sprite[selectedFileEntries.Count];
			string[] filenames = new string[selectedFileEntries.Count];
			for( int i = 0; i < selectedFileEntries.Count; i++ )
			{
				FileSystemEntry fileInfo = validFileEntries[selectedFileEntries[i]];
				icons[i] = GetIconForFileEntry( fileInfo );
				filenames[i] = fileInfo.Name;
			}

			deleteConfirmationPanel.Show( icons, filenames, () =>
			{
				for( int i = selectedFileEntries.Count - 1; i >= 0; i-- )
				{
					FileSystemEntry fileInfo = validFileEntries[selectedFileEntries[i]];
					if( fileInfo.IsDirectory )
						FileBrowserHelpers.DeleteDirectory( fileInfo.Path );
					else
						FileBrowserHelpers.DeleteFile( fileInfo.Path );
				}

				selectedFileEntries.Clear();

				MultiSelectionToggleSelectionMode = false;
				RefreshFiles( true );
			} );
		}

		// Makes sure that the selection persists after Refreshing the file entries
		private void PersistFileEntrySelection()
		{
			pendingFileEntrySelection.Clear();
			for( int i = 0; i < selectedFileEntries.Count; i++ )
				pendingFileEntrySelection.Add( validFileEntries[selectedFileEntries[i]].Name );
		}

		private bool AddQuickLink( Sprite icon, string name, string path, ref Vector2 anchoredPos )
		{
			if( string.IsNullOrEmpty( path ) )
				return false;

#if !UNITY_EDITOR && UNITY_ANDROID
			if( !FileBrowserHelpers.ShouldUseSAF )
#endif
			if( !Directory.Exists( path ) )
				return false;

			// Don't add quick link if it already exists
			if( addedQuickLinksSet.Contains( path ) )
				return false;

			FileBrowserQuickLink quickLink = (FileBrowserQuickLink) Instantiate( quickLinkPrefab, quickLinksContainer, false );
			quickLink.SetFileBrowser( this );

			if( icon != null )
				quickLink.SetQuickLink( icon, name, path );
			else
				quickLink.SetQuickLink( folderIcon, name, path );

			quickLink.TransformComponent.anchoredPosition = anchoredPos;
			anchoredPos.y -= itemHeight;

			addedQuickLinksSet.Add( path );

			return true;
		}

		internal void EnsureWindowIsWithinBounds()
		{
			Vector2 canvasSize = rectTransform.sizeDelta;
			Vector2 windowSize = windowTR.sizeDelta;

			if( windowSize.x < minWidth )
				windowSize.x = minWidth;
			if( windowSize.y < minHeight )
				windowSize.y = minHeight;

			if( windowSize.x > canvasSize.x )
				windowSize.x = canvasSize.x;
			if( windowSize.y > canvasSize.y )
				windowSize.y = canvasSize.y;

			Vector2 windowPos = windowTR.anchoredPosition;
			Vector2 canvasHalfSize = canvasSize * 0.5f;
			Vector2 windowHalfSize = windowSize * 0.5f;
			Vector2 windowBottomLeft = windowPos - windowHalfSize + canvasHalfSize;
			Vector2 windowTopRight = windowPos + windowHalfSize + canvasHalfSize;

			if( windowBottomLeft.x < 0f )
				windowPos.x -= windowBottomLeft.x;
			else if( windowTopRight.x > canvasSize.x )
				windowPos.x -= windowTopRight.x - canvasSize.x;

			if( windowBottomLeft.y < 0f )
				windowPos.y -= windowBottomLeft.y;
			else if( windowTopRight.y > canvasSize.y )
				windowPos.y -= windowTopRight.y - canvasSize.y;

			windowTR.anchoredPosition = windowPos;
			windowTR.sizeDelta = windowSize;
		}

		// Handles responsive user interface
		internal void OnWindowDimensionsChanged( Vector2 size )
		{
			float windowWidth = size.x;
			float quickLinksWidth = Mathf.Min( middleViewQuickLinksOriginalSize.x, windowWidth * quickLinksMaxWidthPercentage );

			if( middleViewQuickLinks.sizeDelta.x != quickLinksWidth )
			{
				middleViewQuickLinks.sizeDelta = new Vector2( quickLinksWidth, middleViewQuickLinksOriginalSize.y );
				middleViewFiles.anchoredPosition = new Vector2( quickLinksWidth, 0f );
				middleViewFiles.sizeDelta = new Vector2( -quickLinksWidth, middleViewQuickLinksOriginalSize.y );
				middleViewSeparator.anchoredPosition = new Vector2( quickLinksWidth, 0f );
			}

#if !UNITY_EDITOR && UNITY_ANDROID
			// Responsive layout doesn't affect any other visible UI elements on Storage Access Framework
			if( FileBrowserHelpers.ShouldUseSAF )
				return;
#endif

			if( windowWidth >= narrowScreenWidth )
			{
				if( pathInputField.transform.parent == pathInputFieldSlotBottom )
				{
					pathInputField.transform.SetParent( pathInputFieldSlotTop, false );

					middleView.anchoredPosition = middleViewOriginalPosition;
					middleView.sizeDelta = middleViewOriginalSize;

					showHiddenFilesToggle.gameObject.SetActive( true );

					listView.OnViewportDimensionsChanged();
					filesScrollRect.OnScroll( nullPointerEventData );
				}
			}
			else
			{
				if( pathInputField.transform.parent == pathInputFieldSlotTop )
				{
					pathInputField.transform.SetParent( pathInputFieldSlotBottom, false );

					float topViewAdditionalHeight = topViewNarrowScreen.sizeDelta.y;
					middleView.anchoredPosition = middleViewOriginalPosition - new Vector2( 0f, topViewAdditionalHeight * 0.5f );
					middleView.sizeDelta = middleViewOriginalSize - new Vector2( 0f, topViewAdditionalHeight );

					// Responsive layout for narrow screens doesn't include "Show Hidden Files" toggle.
					// We simply hide it because I think creating a new row for it would be an overkill
					showHiddenFilesToggle.gameObject.SetActive( false );

					listView.OnViewportDimensionsChanged();
					filesScrollRect.OnScroll( nullPointerEventData );
				}
			}
		}

		private Sprite GetIconForFileEntry( FileSystemEntry fileInfo )
		{
			Sprite icon;
			if( fileInfo.IsDirectory )
				icon = folderIcon;
			else if( !filetypeToIcon.TryGetValue( fileInfo.Extension.ToLowerInvariant(), out icon ) )
				icon = defaultIcon;

			return icon;
		}

		private string GetPathWithoutTrailingDirectorySeparator( string path )
		{
			if( string.IsNullOrEmpty( path ) )
				return null;

			// Credit: http://stackoverflow.com/questions/6019227/remove-the-last-character-if-its-directoryseparatorchar-with-c-sharp
			try
			{
				if( Path.GetDirectoryName( path ) != null )
				{
					char lastChar = path[path.Length - 1];
					if( lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar )
						path = path.Substring( 0, path.Length - 1 );
				}
			}
			catch
			{
				return null;
			}

			return path;
		}

		private void UpdateFilenameInputFieldWithSelection()
		{
			// Refresh filenameInputField as follows:
			// 0 files selected: *blank*
			// 1 file selected: file.Name
			// 2+ files selected: "file1.Name" "file2.Name" ...
			int filenameContributingFileCount = 0;
			if( FolderSelectMode )
				filenameContributingFileCount = selectedFileEntries.Count;
			else
			{
				for( int i = 0; i < selectedFileEntries.Count; i++ )
				{
					if( !validFileEntries[selectedFileEntries[i]].IsDirectory )
						filenameContributingFileCount++;
				}
			}

			if( filenameContributingFileCount == 0 )
				filenameInputField.text = string.Empty;
			else
			{
				if( filenameContributingFileCount > 1 )
				{
					if( multiSelectionFilenameBuilder == null )
						multiSelectionFilenameBuilder = new StringBuilder( 75 );
					else
						multiSelectionFilenameBuilder.Length = 0;
				}

				for( int i = 0; i < selectedFileEntries.Count; i++ )
				{
					FileSystemEntry selectedFile = validFileEntries[selectedFileEntries[i]];
					if( FolderSelectMode || !selectedFile.IsDirectory )
					{
						if( filenameContributingFileCount == 1 )
						{
							filenameInputField.text = selectedFile.Name;
							break;
						}
						else
							multiSelectionFilenameBuilder.Append( "\"" ).Append( selectedFile.Name ).Append( "\" " );
					}
				}

				if( filenameContributingFileCount > 1 )
					filenameInputField.text = multiSelectionFilenameBuilder.ToString();
			}
		}

		// Extracts filenames from input field. Input can be in 2 formats:
		// 1 filename: file.Name
		// 2+ filenames: "file1.Name" "file2.Name" ...
		// Returns the length of the iterated filename
		private int ExtractFilenameFromInput( string input, ref int startIndex, out int nextStartIndex )
		{
			if( !m_allowMultiSelection || input[startIndex] != '"' )
			{
				// Single file is selected, return it
				nextStartIndex = input.Length;
				return input.Length - startIndex;
			}

			// Seems like multiple files are selected

			// Filename is " (a single quotation mark), very unlikely to happen but probably possible on some platforms
			if( startIndex + 1 >= input.Length )
			{
				nextStartIndex = input.Length;
				return 1;
			}

			int filenameEndIndex = input.IndexOf( '"', startIndex + 1 );
			while( true )
			{
				// 1st iteration: filename is "abc
				// 2nd iteration: filename is "abc"def
				if( filenameEndIndex == -1 )
				{
					nextStartIndex = input.Length;
					return input.Length - startIndex;
				}

				// 1st iteration: filename is abc (extracted from "abc")
				// 2nd iteration: filename is abc"def (extracted from "abc"def")
				if( filenameEndIndex == input.Length - 1 || input[filenameEndIndex + 1] == ' ' )
				{
					startIndex++;

					nextStartIndex = filenameEndIndex + 1;
					while( nextStartIndex < input.Length && input[nextStartIndex] == ' ' )
						nextStartIndex++;

					return filenameEndIndex - startIndex;
				}

				// Filename contains a " char
				filenameEndIndex = input.IndexOf( '"', filenameEndIndex + 1 );
			}
		}

		// Checks if a substring of the input field points to an existing file
		private int FilenameInputToFileEntryIndex( string input, int startIndex, int length )
		{
			for( int i = 0; i < validFileEntries.Count; i++ )
			{
				if( validFileEntries[i].Name.Length == length && input.IndexOf( validFileEntries[i].Name ) == startIndex )
					return i;
			}

			return -1;
		}

		// Credit: http://answers.unity3d.com/questions/898770/how-to-get-the-width-of-ui-text-with-horizontal-ov.html
		private int CalculateLengthOfDropdownText( string str )
		{
			int totalLength = 0;

			Font myFont = filterItemTemplate.font;
			CharacterInfo characterInfo = new CharacterInfo();

			myFont.RequestCharactersInTexture( str, filterItemTemplate.fontSize, filterItemTemplate.fontStyle );

			for( int i = 0; i < str.Length; i++ )
			{
				if( !myFont.GetCharacterInfo( str[i], out characterInfo, filterItemTemplate.fontSize ) )
					totalLength += 5;

				totalLength += characterInfo.advance;
			}

			return totalLength;
		}

		private string GetInitialPath( string initialPath )
		{
			if( !string.IsNullOrEmpty( initialPath ) && !Directory.Exists( initialPath ) && File.Exists( initialPath ) )
			{
				// Path points to a file, use its parent directory's path instead
				initialPath = Path.GetDirectoryName( initialPath );
			}

			if( string.IsNullOrEmpty( initialPath ) || !Directory.Exists( initialPath ) )
			{
				if( CurrentPath.Length == 0 )
					initialPath = DEFAULT_PATH;
				else
					initialPath = CurrentPath;
			}

			m_currentPath = string.Empty; // Needed to correctly reset the pathsFollowed

			return initialPath;
		}
		#endregion

		#region File Browser Functions (static)
		public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel,
										   bool folderMode = false, bool allowMultiSelection = false, string initialPath = null,
										   string title = "Save", string saveButtonText = "Save" )
		{
			// Instead of ignoring this dialog request, let's just override the currently visible dialog's properties
			//if( Instance.gameObject.activeSelf )
			//{
			//	Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			//	return false;
			//}

			Instance.onSuccess = onSuccess;
			Instance.onCancel = onCancel;

			Instance.FolderSelectMode = folderMode;
			Instance.AllowMultiSelection = allowMultiSelection;
			Instance.Title = title;
			Instance.SubmitButtonText = saveButtonText;
			Instance.AcceptNonExistingFilename = !folderMode;

			Instance.Show( initialPath );

			return true;
		}

		public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel,
										   bool folderMode = false, bool allowMultiSelection = false, string initialPath = null,
										   string title = "Load", string loadButtonText = "Select" )
		{
			// Instead of ignoring this dialog request, let's just override the currently visible dialog's properties
			//if( Instance.gameObject.activeSelf )
			//{
			//	Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			//	return false;
			//}

			Instance.onSuccess = onSuccess;
			Instance.onCancel = onCancel;

			Instance.FolderSelectMode = folderMode;
			Instance.AllowMultiSelection = allowMultiSelection;
			Instance.Title = title;
			Instance.SubmitButtonText = loadButtonText;
			Instance.AcceptNonExistingFilename = false;

			Instance.Show( initialPath );

			return true;
		}

		public static void HideDialog( bool invokeCancelCallback = false )
		{
			Instance.OnOperationCanceled( invokeCancelCallback );
		}

		public static IEnumerator WaitForSaveDialog( bool folderMode = false, bool allowMultiSelection = false, string initialPath = null,
													 string title = "Save", string saveButtonText = "Save" )
		{
			if( !ShowSaveDialog( null, null, folderMode, allowMultiSelection, initialPath, title, saveButtonText ) )
				yield break;

			while( Instance.gameObject.activeSelf )
				yield return null;
		}

		public static IEnumerator WaitForLoadDialog( bool folderMode = false, bool allowMultiSelection = false, string initialPath = null,
													 string title = "Load", string loadButtonText = "Select" )
		{
			if( !ShowLoadDialog( null, null, folderMode, allowMultiSelection, initialPath, title, loadButtonText ) )
				yield break;

			while( Instance.gameObject.activeSelf )
				yield return null;
		}

		public static bool AddQuickLink( string name, string path, Sprite icon = null )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAF )
				return false;
#endif

			if( !quickLinksInitialized )
			{
				quickLinksInitialized = true;

				// Fetching the list of external drives is only possible with the READ_EXTERNAL_STORAGE permission granted on Android
				if( AskPermissions )
					RequestPermission();

				Instance.InitializeQuickLinks();
			}

			Vector2 anchoredPos = new Vector2( 0f, -Instance.quickLinksContainer.sizeDelta.y );

			if( Instance.AddQuickLink( icon, name, path, ref anchoredPos ) )
			{
				Instance.quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
				return true;
			}

			return false;
		}

		public static void SetExcludedExtensions( params string[] excludedExtensions )
		{
			Instance.excludedExtensionsSet.Clear();

			if( excludedExtensions != null )
			{
				for( int i = 0; i < excludedExtensions.Length; i++ )
					Instance.excludedExtensionsSet.Add( excludedExtensions[i].ToLowerInvariant() );
			}
		}

		public static void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				foreach( string filter in filters )
				{
					if( filter != null && filter.Length > 0 )
						Instance.filters.Add( new Filter( null, filter ) );
				}
			}

			SetFiltersPostProcessing();
		}

		public static void SetFilters( bool showAllFilesFilter, params string[] filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				for( int i = 0; i < filters.Length; i++ )
				{
					if( filters[i] != null && filters[i].Length > 0 )
						Instance.filters.Add( new Filter( null, filters[i] ) );
				}
			}

			SetFiltersPostProcessing();
		}

		public static void SetFilters( bool showAllFilesFilter, IEnumerable<Filter> filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				foreach( Filter filter in filters )
				{
					if( filter != null && filter.defaultExtension.Length > 0 )
						Instance.filters.Add( filter );
				}
			}

			SetFiltersPostProcessing();
		}

		public static void SetFilters( bool showAllFilesFilter, params Filter[] filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				for( int i = 0; i < filters.Length; i++ )
				{
					if( filters[i] != null && filters[i].defaultExtension.Length > 0 )
						Instance.filters.Add( filters[i] );
				}
			}

			SetFiltersPostProcessing();
		}

		private static void SetFiltersPreProcessing( bool showAllFilesFilter )
		{
			Instance.showAllFilesFilter = showAllFilesFilter;

			Instance.filters.Clear();

			if( showAllFilesFilter )
				Instance.filters.Add( Instance.allFilesFilter );
		}

		private static void SetFiltersPostProcessing()
		{
			List<Filter> filters = Instance.filters;

			if( filters.Count == 0 )
				filters.Add( Instance.allFilesFilter );

			int maxFilterStrLength = 100;
			List<string> dropdownValues = new List<string>( filters.Count );
			for( int i = 0; i < filters.Count; i++ )
			{
				string filterStr = filters[i].ToString();
				dropdownValues.Add( filterStr );

				maxFilterStrLength = Mathf.Max( maxFilterStrLength, Instance.CalculateLengthOfDropdownText( filterStr ) );
			}

			Vector2 size = Instance.filtersDropdownContainer.sizeDelta;
			size.x = maxFilterStrLength + 28;
			Instance.filtersDropdownContainer.sizeDelta = size;

			Instance.filtersDropdown.ClearOptions();
			Instance.filtersDropdown.AddOptions( dropdownValues );
			Instance.filtersDropdown.value = 0;
		}

		public static bool SetDefaultFilter( string defaultFilter )
		{
			if( defaultFilter == null )
			{
				if( Instance.showAllFilesFilter )
				{
					Instance.filtersDropdown.value = 0;
					Instance.filtersDropdown.RefreshShownValue();

					return true;
				}

				return false;
			}

			defaultFilter = defaultFilter.ToLowerInvariant();

			for( int i = 0; i < Instance.filters.Count; i++ )
			{
				HashSet<string> extensions = Instance.filters[i].extensions;
				if( extensions != null && extensions.Contains( defaultFilter ) )
				{
					Instance.filtersDropdown.value = i;
					Instance.filtersDropdown.RefreshShownValue();

					return true;
				}
			}

			return false;
		}

		public static Permission CheckPermission()
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			Permission result = (Permission) FileBrowserHelpers.AJC.CallStatic<int>( "CheckPermission", FileBrowserHelpers.Context );
			if( result == Permission.Denied && (Permission) PlayerPrefs.GetInt( "FileBrowserPermission", (int) Permission.ShouldAsk ) == Permission.ShouldAsk )
				result = Permission.ShouldAsk;

			return result;
#else
			return Permission.Granted;
#endif
		}

		public static Permission RequestPermission()
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			object threadLock = new object();
			lock( threadLock )
			{
				FBPermissionCallbackAndroid nativeCallback = new FBPermissionCallbackAndroid( threadLock );

				FileBrowserHelpers.AJC.CallStatic( "RequestPermission", FileBrowserHelpers.Context, nativeCallback, PlayerPrefs.GetInt( "FileBrowserPermission", (int) Permission.ShouldAsk ) );

				if( nativeCallback.Result == -1 )
					System.Threading.Monitor.Wait( threadLock );

				if( (Permission) nativeCallback.Result != Permission.ShouldAsk && PlayerPrefs.GetInt( "FileBrowserPermission", -1 ) != nativeCallback.Result )
				{
					PlayerPrefs.SetInt( "FileBrowserPermission", nativeCallback.Result );
					PlayerPrefs.Save();
				}

				return (Permission) nativeCallback.Result;
			}
#else
			return Permission.Granted;
#endif
		}
		#endregion
	}
}