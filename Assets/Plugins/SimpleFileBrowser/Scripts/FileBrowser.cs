using UnityEngine;
using UnityEngine.UI;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using SimpleRecycledListView;

namespace SimpleFileBrowser
{
	public class FileBrowser : MonoBehaviour, IListViewAdapter
	{
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
				extensions = new HashSet<string>();
				extensions.Add( extension );
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
		private string DEFAULT_PATH = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
		#endregion

		#region Static Variables
		public static bool IsOpen = false;

		public static bool Success = false;
		public static string Result = null;

		private static FileBrowser m_instance = null;
		private static FileBrowser instance
		{
			get
			{
				if( m_instance == null )
				{
					m_instance = Instantiate<GameObject>( Resources.Load<GameObject>( "SimpleFileBrowserCanvas" ) ).GetComponent<FileBrowser>();
					DontDestroyOnLoad( m_instance.gameObject );
					m_instance.gameObject.SetActive( false );
				}

				return m_instance;
			}
		}
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

		[SerializeField]
		private QuickLink[] quickLinks;

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
							{
								pathsFollowed.RemoveAt( i );
							}
						}
						else
						{
							pathsFollowed.Add( m_currentPath );
						}
					}

					m_searchString = string.Empty;
					searchInputField.text = m_searchString;

					filesScrollRect.verticalNormalizedPosition = 1;

					filenameImage.color = Color.white;

					RefreshFiles( true );
				}
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
		public FileBrowserItem SelectedFile
		{
			get
			{
				return m_selectedFile;
			}
			private set
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
		public bool AcceptNonExistingFilename
		{
			get
			{
				return m_acceptNonExistingFilename;
			}
			set
			{
				if( m_acceptNonExistingFilename != value )
				{
					m_acceptNonExistingFilename = value;
				}
			}
		}

		private bool m_folderSelectMode = false;
		public bool FolderSelectMode
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

		public string Title
		{
			get
			{
				return titleText.text;
			}
			set
			{
				titleText.text = value;
			}
		}

		public string SubmitButtonText
		{
			get
			{
				return submitButtonText.text;
			}
			set
			{
				submitButtonText.text = value;
			}
		}
		#endregion

		#region Delegates
		public delegate void OnSuccess( string path );
		public delegate void OnCancel();

		public event OnSuccess onSuccess;
		public event OnCancel onCancel;
		#endregion

		#region Messages
		void Awake()
		{
			m_instance = this;

			itemHeight = ( (RectTransform) itemPrefab.transform ).sizeDelta.y;

			nullPointerEventData = new UnityEngine.EventSystems.PointerEventData( null );

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
			{
				RefreshFiles( true );
			}
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

			if( item.Position == m_selectedFilePosition )
			{
				m_selectedFile = file;
				file.Select();
			}
			else
			{
				file.Deselect();
			}
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
				string[] drives = Directory.GetLogicalDrives();

				for( int i = 0; i < drives.Length; i++ )
				{
					AddQuickLink( driveIcon, drives[i], drives[i], ref anchoredPos );
				}
			}

			for( int i = 0; i < quickLinks.Length; i++ )
			{
				QuickLink quickLink = quickLinks[i];
				string quickLinkPath = Environment.GetFolderPath( quickLink.target );

				AddQuickLink( quickLink.icon, quickLink.name, quickLinkPath, ref anchoredPos );
			}

			quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
		}
		#endregion

		#region Button Events
		public void OnBackButtonPressed()
		{
			if( currentPathIndex > 0 )
			{
				currentPathIndex--;
				CurrentPath = pathsFollowed[currentPathIndex];
			}
		}

		public void OnForwardButtonPressed()
		{
			if( currentPathIndex < pathsFollowed.Count - 1 )
			{
				currentPathIndex++;
				CurrentPath = pathsFollowed[currentPathIndex];
			}
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
			else
				path = GetPathWithoutTrailingDirectorySeparator( path );

			if( File.Exists( path ) )
			{
				if( !m_folderSelectMode )
				{
					OnOperationSuccessful( path );
				}
				else
				{
					filenameImage.color = wrongFilenameColor;
				}
			}
			else if( Directory.Exists( path ) )
			{
				if( m_folderSelectMode )
				{
					OnOperationSuccessful( path );
				}
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
				{
					filenameImage.color = wrongFilenameColor;
				}
			}
		}

		public void OnCancelButtonClicked()
		{
			OnOperationCanceled();
		}
		#endregion

		#region Other Events
		private void OnOperationSuccessful( string path )
		{
			Success = true;
			Result = path;

			Hide();

			if( onSuccess != null )
			{
				onSuccess( path );
			}
		}

		private void OnOperationCanceled()
		{
			Success = false;
			Result = null;

			Hide();

			if( onCancel != null )
			{
				onCancel();
			}
		}

		public void OnPathChanged( string newPath )
		{
			newPath = GetPathWithoutTrailingDirectorySeparator( newPath );
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
			{
				CurrentPath = quickLink.TargetPath;
			}
		}

		public void OnItemSelected( FileBrowserItem item )
		{
			SelectedFile = item;
		}

		public void OnItemOpened( FileBrowserItem item )
		{
			if( item.IsDirectory )
			{
				CurrentPath = Path.Combine( m_currentPath, item.Name );
			}
			else
			{
				OnSubmitButtonClicked();
			}
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
		public void Show()
		{
			currentPathIndex = -1;
			pathsFollowed.Clear();

			SelectedFile = null;

			m_searchString = string.Empty;
			searchInputField.text = m_searchString;

			filesScrollRect.verticalNormalizedPosition = 1;

			filenameImage.color = Color.white;

			IsOpen = true;
			Success = false;
			Result = null;

			gameObject.SetActive( true );
		}

		public void Hide()
		{
			IsOpen = false;

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
			if( path == null || path.Length == 0 )
				return false;

			try
			{
				path = GetPathWithoutTrailingDirectorySeparator( path );
			}
			catch( ArgumentException )
			{
				return false;
			}
			catch( PathTooLongException )
			{
				return false;
			}

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

			quickLink.transformComponent.anchoredPosition = anchoredPos;

			anchoredPos.y -= itemHeight;

			addedQuickLinksSet.Add( path );

			return true;
		}

		private string GetPathWithoutTrailingDirectorySeparator( string path )
		{
			// Credit: http://stackoverflow.com/questions/6019227/remove-the-last-character-if-its-directoryseparatorchar-with-c-sharp
			if( Path.GetDirectoryName( path ) != null )
			{
				char lastChar = path[path.Length - 1];
				if( lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar )
					path = path.Substring( 0, path.Length - 1 );
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
		#endregion

		#region File Browser Functions (static)
		public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel,
										   bool folderMode = false, string initialPath = null,
										   string title = "Save", string saveButtonText = "Save" )
		{
			if( instance.gameObject.activeSelf )
			{
				Debug.LogError( "Error: Multiple dialogs are not allowed!" );
				return false;
			}

			if( ( initialPath == null || !Directory.Exists( initialPath ) ) && instance.m_currentPath.Length == 0 )
				initialPath = instance.DEFAULT_PATH;

			instance.onSuccess = onSuccess;
			instance.onCancel = onCancel;

			instance.FolderSelectMode = folderMode;
			instance.Title = title;
			instance.SubmitButtonText = saveButtonText;

			instance.AcceptNonExistingFilename = !folderMode;

			instance.Show();

			if( instance.CurrentPath != initialPath )
				instance.CurrentPath = initialPath;
			else
				instance.RefreshFiles( true );

			return true;
		}

		public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel,
										   bool folderMode = false, string initialPath = null,
										   string title = "Load", string loadButtonText = "Select" )
		{
			if( instance.gameObject.activeSelf )
			{
				Debug.LogError( "Error: Multiple dialogs are not allowed!" );
				return false;
			}

			if( ( initialPath == null || !Directory.Exists( initialPath ) ) && instance.m_currentPath.Length == 0 )
				initialPath = instance.DEFAULT_PATH;

			instance.onSuccess = onSuccess;
			instance.onCancel = onCancel;

			instance.FolderSelectMode = folderMode;
			instance.Title = title;
			instance.SubmitButtonText = loadButtonText;

			instance.AcceptNonExistingFilename = false;

			instance.Show();

			if( instance.CurrentPath != initialPath )
				instance.CurrentPath = initialPath;
			else
				instance.RefreshFiles( true );

			return true;
		}

		public static IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null,
													 string title = "Save", string saveButtonText = "Save" )
		{
			if( instance.gameObject.activeSelf )
			{
				Debug.LogError( "Error: Multiple dialogs are not allowed!" );
				yield break;
			}

			ShowSaveDialog( null, null, folderMode, initialPath, title, saveButtonText );

			while( instance.gameObject.activeSelf )
				yield return null;
		}

		public static IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null,
													 string title = "Load", string loadButtonText = "Select" )
		{
			if( instance.gameObject.activeSelf )
			{
				Debug.LogError( "Error: Multiple dialogs are not allowed!" );
				yield break;
			}

			ShowLoadDialog( null, null, folderMode, initialPath, title, loadButtonText );

			while( instance.gameObject.activeSelf )
				yield return null;
		}

		public static bool AddQuickLink( Sprite icon, string name, string path )
		{
			Vector2 anchoredPos = new Vector2( 0f, -instance.quickLinksContainer.sizeDelta.y );
			
			if( instance.AddQuickLink( icon, name, path, ref anchoredPos ) )
			{
				instance.quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
				return true;
			}

			return false;
		}

		public static void SetExcludedExtensions( params string[] excludedExtensions )
		{
			if( instance.excludedExtensionsSet == null )
				instance.excludedExtensionsSet = new HashSet<string>();
			else
				instance.excludedExtensionsSet.Clear();

			if( excludedExtensions != null )
			{
				for( int i = 0; i < excludedExtensions.Length; i++ )
				{
					instance.excludedExtensionsSet.Add( excludedExtensions[i].ToLower() );
				}
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
						instance.filters.Add( new Filter( null, filter ) );
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
						instance.filters.Add( new Filter( null, filters[i] ) );
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
						instance.filters.Add( filter );
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
						instance.filters.Add( filters[i] );
				}
			}

			SetFiltersPostProcessing();
		}

		private static void SetFiltersPreProcessing( bool showAllFilesFilter )
		{
			instance.showAllFilesFilter = showAllFilesFilter;

			instance.filters.Clear();

			if( showAllFilesFilter )
				instance.filters.Add( instance.allFilesFilter );
		}

		private static void SetFiltersPostProcessing()
		{
			List<Filter> filters = instance.filters;

			if( filters.Count == 0 )
				filters.Add( instance.allFilesFilter );

			int maxFilterStrLength = 100;
			List<string> dropdownValues = new List<string>( filters.Count );
			for( int i = 0; i < filters.Count; i++ )
			{
				string filterStr = filters[i].ToString();
				dropdownValues.Add( filterStr );

				maxFilterStrLength = Mathf.Max( maxFilterStrLength, instance.CalculateLengthOfDropdownText( filterStr ) );
			}

			Vector2 size = instance.filtersDropdownContainer.sizeDelta;
			size.x = maxFilterStrLength + 28;
			instance.filtersDropdownContainer.sizeDelta = size;

			instance.filtersDropdown.ClearOptions();
			instance.filtersDropdown.AddOptions( dropdownValues );
		}

		public static bool SetDefaultFilter( string defaultFilter )
		{
			if( defaultFilter == null )
			{
				if( instance.showAllFilesFilter )
				{
					instance.filtersDropdown.value = 0;
					instance.filtersDropdown.RefreshShownValue();

					return true;
				}

				return false;
			}

			defaultFilter = defaultFilter.ToLower();

			for( int i = 0; i < instance.filters.Count; i++ )
			{
				HashSet<string> extensions = instance.filters[i].extensions;
				if( extensions != null && extensions.Contains( defaultFilter ) )
				{
					instance.filtersDropdown.value = i;
					instance.filtersDropdown.RefreshShownValue();

					return true;
				}
			}

			return false;
		}
		#endregion
	}
}