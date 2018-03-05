using UnityEngine;
using UnityEngine.UI;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace SimpleFileBrowser
{
	public class FileBrowser : MonoBehaviour, IListViewAdapter
	{
		public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };

		#region Structs
		[Serializable]
		private struct FiletypeIcon
		{
			public string extension;
			public Sprite icon;
		}

		[Serializable]
		private struct QuickLink
		{
			public Environment.SpecialFolder target;
			public string name;
			public Sprite icon;
		}
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

				extension = extension.ToLower();
				extensions = new HashSet<string>() { extension };
				defaultExtension = extension;
			}

			public Filter( string name, params string[] extensions )
			{
				this.name = name;

				for( int i = 0; i < extensions.Length; i++ )
					extensions[i] = extensions[i].ToLower();

				this.extensions = new HashSet<string>( extensions );
				defaultExtension = extensions[0];
			}

			public override string ToString()
			{
				string result = "";

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
		#endregion

		#region Static Variables
		public static bool IsOpen { get; private set; }

		public static bool Success { get; private set; }
		public static string Result { get; private set; }

		private static bool m_askPermissions = true;
		public static bool AskPermissions
		{
			get { return m_askPermissions; }
			set { m_askPermissions = value; }
		}

		private static FileBrowser m_instance = null;
		private static FileBrowser Instance
		{
			get
			{
				if( m_instance == null )
				{
					m_instance = Instantiate( Resources.Load<GameObject>( "SimpleFileBrowserCanvas" ) ).GetComponent<FileBrowser>();
					DontDestroyOnLoad( m_instance.gameObject );
					m_instance.gameObject.SetActive( false );
				}

				return m_instance;
			}
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		private static AndroidJavaClass m_ajc = null;
		private static AndroidJavaClass AJC
		{
			get
			{
				if( m_ajc == null )
					m_ajc = new AndroidJavaClass( "com.yasirkula.unity.FileBrowser" );

				return m_ajc;
			}
		}

		private static AndroidJavaObject m_context = null;
		private static AndroidJavaObject Context
		{
			get
			{
				if( m_context == null )
				{
					using( AndroidJavaObject unityClass = new AndroidJavaClass( "com.unity3d.player.UnityPlayer" ) )
					{
						m_context = unityClass.GetStatic<AndroidJavaObject>( "currentActivity" );
					}
				}

				return m_context;
			}
		}
#endif
		#endregion

		#region Variables
		[Header( "References" )]

		[SerializeField]
		private FileBrowserItem itemPrefab;

		[SerializeField]
		private FileBrowserQuickLink quickLinkPrefab;

		private List<FileSystemInfo> allItems = new List<FileSystemInfo>();
		private List<FileSystemInfo> validItems = new List<FileSystemInfo>();

		[SerializeField]
		private Text titleText;

		[SerializeField]
		private InputField pathInputField;

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

		[Header( "Other" )]

		public Color normalFileColor = Color.white;
		public Color hoveredFileColor = new Color32( 225, 225, 255, 255 );
		public Color selectedFileColor = new Color32( 0, 175, 255, 255 );

		public Color wrongFilenameColor = new Color32( 255, 100, 100, 255 );

		public int minWidth = 380;
		public int minHeight = 300;

		[SerializeField]
		private string[] excludeExtensions;

#pragma warning disable 0414
		[SerializeField]
		private QuickLink[] quickLinks;
#pragma warning restore 0414

		private HashSet<string> excludedExtensionsSet;
		private HashSet<string> addedQuickLinksSet;
		
		[SerializeField]
		private bool generateQuickLinksForDrives = true;

		private FileAttributes ignoredFileAttributes = FileAttributes.System;

		private List<Filter> filters = new List<Filter>();
		private Filter allFilesFilter;

		private bool showAllFilesFilter = true;

		private float itemHeight;

		private int currentPathIndex = -1;
		private List<string> pathsFollowed = new List<string>();

		// Required in RefreshFiles() function
		private UnityEngine.EventSystems.PointerEventData nullPointerEventData;
#endregion

#region Properties
		private string m_currentPath = string.Empty;
		private string CurrentPath
		{
			get
			{
				return m_currentPath;
			}
			set
			{
				value = GetPathWithoutTrailingDirectorySeparator( value );
				if( value == null )
					return;

				if( m_currentPath != value )
				{
					if( !Directory.Exists( value ) )
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

					m_searchString = string.Empty;
					searchInputField.text = m_searchString;

					filesScrollRect.verticalNormalizedPosition = 1;

					filenameImage.color = Color.white;
					if( m_folderSelectMode )
						filenameInputField.text = string.Empty;
				}

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

		private int m_selectedFilePosition = -1;
		public int SelectedFilePosition { get { return m_selectedFilePosition; } }

		private FileBrowserItem m_selectedFile;
		private FileBrowserItem SelectedFile
		{
			get
			{
				return m_selectedFile;
			}
			set
			{
				if( value == null )
				{
					if( m_selectedFile != null )
						m_selectedFile.Deselect();

					m_selectedFilePosition = -1;
					m_selectedFile = null;
				}
				else if( m_selectedFilePosition != value.Position )
				{
					if( m_selectedFile != null )
						m_selectedFile.Deselect();

					m_selectedFile = value;
					m_selectedFilePosition = value.Position;

					if( m_folderSelectMode || !m_selectedFile.IsDirectory )
						filenameInputField.text = m_selectedFile.Name;

					m_selectedFile.Select();
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
		public delegate void OnSuccess( string path );
		public delegate void OnCancel();

		private OnSuccess onSuccess;
		private OnCancel onCancel;
#endregion

#region Messages
		void Awake()
		{
			m_instance = this;

			itemHeight = ( (RectTransform) itemPrefab.transform ).sizeDelta.y;
			nullPointerEventData = new UnityEngine.EventSystems.PointerEventData( null );

#if !UNITY_EDITOR && UNITY_IOS
			DEFAULT_PATH = Application.persistentDataPath;
#else
			DEFAULT_PATH = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
#endif

			InitializeFiletypeIcons();
			filetypeIcons = null;

			SetExcludedExtensions( excludeExtensions );
			excludeExtensions = null;

			filenameInputField.onValidateInput += OnValidateFilenameInput;

			InitializeQuickLinks();
			quickLinks = null;

			allFilesFilter = new Filter( ALL_FILES_FILTER_TEXT );
			filters.Add( allFilesFilter );

			listView.SetAdapter( this );
		}

		void OnApplicationFocus( bool focus )
		{
			if( focus )
				RefreshFiles( true );
		}
#endregion

#region Interface Methods
		public OnItemClickedHandler OnItemClicked { get { return null; } set { } }

		public int Count { get { return validItems.Count; } }
		public float ItemHeight { get { return itemHeight; } }

		public ListItem CreateItem()
		{
			FileBrowserItem item = Instantiate( itemPrefab, filesContainer, false );
			item.SetFileBrowser( this );

			return item;
		}

		public void SetItemContent( ListItem item )
		{
			FileBrowserItem file = (FileBrowserItem) item;
			FileSystemInfo fileInfo = validItems[item.Position];

			bool isDirectory = ( fileInfo.Attributes & FileAttributes.Directory ) == FileAttributes.Directory;

			Sprite icon;
			if( isDirectory )
				icon = folderIcon;
			else if( !filetypeToIcon.TryGetValue( fileInfo.Extension.ToLower(), out icon ) )
				icon = defaultIcon;

			file.SetFile( icon, fileInfo.Name, isDirectory );
			file.SetHidden( ( fileInfo.Attributes & FileAttributes.Hidden ) == FileAttributes.Hidden );

			if( item.Position == m_selectedFilePosition )
			{
				m_selectedFile = file;
				file.Select();
			}
			else
				file.Deselect();
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
			addedQuickLinksSet = new HashSet<string>();

			Vector2 anchoredPos = new Vector2( 0f, -quickLinksContainer.sizeDelta.y );

			if( generateQuickLinksForDrives )
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				string drivesList = AJC.CallStatic<string>( "GetExternalDrives" );
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
#elif !UNITY_EDITOR && UNITY_IOS
				AddQuickLink( driveIcon, "Files", Application.persistentDataPath, ref anchoredPos );
#else
				string[] drives = Directory.GetLogicalDrives();

				for( int i = 0; i < drives.Length; i++ )
					AddQuickLink( driveIcon, drives[i], drives[i], ref anchoredPos );
#endif
			}

#if UNITY_EDITOR || !UNITY_ANDROID
			for( int i = 0; i < quickLinks.Length; i++ )
			{
				QuickLink quickLink = quickLinks[i];
				string quickLinkPath = Environment.GetFolderPath( quickLink.target );

				AddQuickLink( quickLink.icon, quickLink.name, quickLinkPath, ref anchoredPos );
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
			DirectoryInfo parentPath = Directory.GetParent( m_currentPath );

			if( parentPath != null )
				CurrentPath = parentPath.FullName;
		}

		public void OnSubmitButtonClicked()
		{
			string path = m_currentPath;
			if( filenameInputField.text.Length > 0 )
				path = Path.Combine( path, filenameInputField.text );

			if( File.Exists( path ) )
			{
				if( !m_folderSelectMode )
					OnOperationSuccessful( path );
				else
					filenameImage.color = wrongFilenameColor;
			}
			else if( Directory.Exists( path ) )
			{
				if( m_folderSelectMode )
					OnOperationSuccessful( path );
				else
				{
					if( m_currentPath == path )
						filenameImage.color = wrongFilenameColor;
					else
						CurrentPath = path;
				}
			}
			else
			{
				if( m_acceptNonExistingFilename )
				{
					if( !m_folderSelectMode && filters[filtersDropdown.value].defaultExtension != null )
						path = Path.ChangeExtension( path, filters[filtersDropdown.value].defaultExtension );

					OnOperationSuccessful( path );
				}
				else
					filenameImage.color = wrongFilenameColor;
			}
		}

		public void OnCancelButtonClicked()
		{
			OnOperationCanceled( true );
		}
#endregion

#region Other Events
		private void OnOperationSuccessful( string path )
		{
			Success = true;
			Result = path;

			Hide();

			if( onSuccess != null )
				onSuccess( path );

			onSuccess = null;
			onCancel = null;
		}

		private void OnOperationCanceled( bool invokeCancelCallback )
		{
			Success = false;
			Result = null;

			Hide();

			if( invokeCancelCallback && onCancel != null )
				onCancel();

			onSuccess = null;
			onCancel = null;
		}

		public void OnPathChanged( string newPath )
		{
			CurrentPath = newPath;
		}

		public void OnSearchStringChanged( string newSearchString )
		{
			SearchString = newSearchString;
		}

		public void OnFilterChanged()
		{
			RefreshFiles( false );
		}

		public void OnShowHiddenFilesToggleChanged()
		{
			RefreshFiles( false );
		}

		public void OnQuickLinkSelected( FileBrowserQuickLink quickLink )
		{
			if( quickLink != null )
				CurrentPath = quickLink.TargetPath;
		}

		public void OnItemSelected( FileBrowserItem item )
		{
			SelectedFile = item;
		}

		public void OnItemOpened( FileBrowserItem item )
		{
			if( item.IsDirectory )
				CurrentPath = Path.Combine( m_currentPath, item.Name );
			else
				OnSubmitButtonClicked();
		}

		public char OnValidateFilenameInput( string text, int charIndex, char addedChar )
		{
			if( addedChar == '\n' )
			{
				OnSubmitButtonClicked();
				return '\0';
			}

			return addedChar;
		}
#endregion

#region Helper Functions
		public void Show( string initialPath )
		{
			if( AskPermissions )
				RequestPermission();

			SelectedFile = null;

			m_searchString = string.Empty;
			searchInputField.text = m_searchString;

			filesScrollRect.verticalNormalizedPosition = 1;

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

			gameObject.SetActive( false );
		}

		public void RefreshFiles( bool pathChanged )
		{
			if( pathChanged )
			{
				allItems.Clear();

				try
				{
					DirectoryInfo dir = new DirectoryInfo( m_currentPath );

					FileSystemInfo[] items = dir.GetFileSystemInfos();
					for( int i = 0; i < items.Length; i++ )
						allItems.Add( items[i] );
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}

			validItems.Clear();
			
			SelectedFile = null;

			if( !showHiddenFilesToggle.isOn )
				ignoredFileAttributes |= FileAttributes.Hidden;
			else
				ignoredFileAttributes &= ~FileAttributes.Hidden;

			string searchStringLowercase = m_searchString.ToLower();

			for( int i = 0; i < allItems.Count; i++ )
			{
				try
				{
					FileSystemInfo item = allItems[i];

					if( ( item.Attributes & FileAttributes.Directory ) == 0 )
					{
						if( m_folderSelectMode )
							continue;

						FileInfo fileInfo = (FileInfo) item;
						if( ( fileInfo.Attributes & ignoredFileAttributes ) != 0 )
							continue;

						string extension = fileInfo.Extension.ToLower();
						if( excludedExtensionsSet.Contains( extension ) )
							continue;

						HashSet<string> extensions = filters[filtersDropdown.value].extensions;
						if( extensions != null && !extensions.Contains( extension ) )
							continue;
					}
					else
					{
						DirectoryInfo directoryInfo = (DirectoryInfo) item;
						if( ( directoryInfo.Attributes & ignoredFileAttributes ) != 0 )
							continue;
					}

					if( m_searchString.Length == 0 || item.Name.ToLower().Contains( searchStringLowercase ) )
						validItems.Add( item );
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}

			listView.UpdateList();

			// Prevent the case where all the content stays offscreen after changing the search string
			filesScrollRect.OnScroll( nullPointerEventData );
		}

		private bool AddQuickLink( Sprite icon, string name, string path, ref Vector2 anchoredPos )
		{
			if( string.IsNullOrEmpty( path ) )
				return false;
			
			if( !Directory.Exists( path ) )
				return false;
			
			// Don't add quick link if it already exists
			if( addedQuickLinksSet.Contains( path ) )
				return false;

			FileBrowserQuickLink quickLink = Instantiate( quickLinkPrefab, quickLinksContainer, false );
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
										   bool folderMode = false, string initialPath = null,
										   string title = "Save", string saveButtonText = "Save" )
		{
			if( Instance.gameObject.activeSelf )
			{
				Debug.LogError( "Error: Multiple dialogs are not allowed!" );
				return false;
			}
			
			Instance.onSuccess = onSuccess;
			Instance.onCancel = onCancel;

			Instance.FolderSelectMode = folderMode;
			Instance.Title = title;
			Instance.SubmitButtonText = saveButtonText;
			Instance.AcceptNonExistingFilename = !folderMode;

			Instance.Show( initialPath );

			return true;
		}

		public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel,
										   bool folderMode = false, string initialPath = null,
										   string title = "Load", string loadButtonText = "Select" )
		{
			if( Instance.gameObject.activeSelf )
			{
				Debug.LogError( "Error: Multiple dialogs are not allowed!" );
				return false;
			}
			
			Instance.onSuccess = onSuccess;
			Instance.onCancel = onCancel;

			Instance.FolderSelectMode = folderMode;
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

		public static IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null,
													 string title = "Save", string saveButtonText = "Save" )
		{
			if( !ShowSaveDialog( null, null, folderMode, initialPath, title, saveButtonText ) )
				yield break;

			while( Instance.gameObject.activeSelf )
				yield return null;
		}

		public static IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null,
													 string title = "Load", string loadButtonText = "Select" )
		{
			if( !ShowLoadDialog( null, null, folderMode, initialPath, title, loadButtonText ) )
				yield break;

			while( Instance.gameObject.activeSelf )
				yield return null;
		}

		public static bool AddQuickLink( string name, string path, Sprite icon = null )
		{
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
			if( Instance.excludedExtensionsSet == null )
				Instance.excludedExtensionsSet = new HashSet<string>();
			else
				Instance.excludedExtensionsSet.Clear();

			if( excludedExtensions != null )
			{
				for( int i = 0; i < excludedExtensions.Length; i++ )
					Instance.excludedExtensionsSet.Add( excludedExtensions[i].ToLower() );
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

			defaultFilter = defaultFilter.ToLower();

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
			Permission result = (Permission) AJC.CallStatic<int>( "CheckPermission", Context );
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

				AJC.CallStatic( "RequestPermission", Context, nativeCallback, PlayerPrefs.GetInt( "FileBrowserPermission", (int) Permission.ShouldAsk ) );

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