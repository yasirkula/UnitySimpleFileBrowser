= Simple File Browser =

Online documentation & example code available at: https://github.com/yasirkula/UnitySimpleFileBrowser
E-mail: yasirkula@gmail.com

1. ABOUT
This plugin helps you show save/load dialogs during gameplay with its uGUI based file browser.

2. HOW TO
for Android: set Write Permission to External (SDCard) in Player Settings

The file browser can be shown either as a save dialog or a load dialog. In load mode, the returned path always leads to an existing file or folder. In save mode, the returned path can point to a non-existing file, as well.

3. SCRIPTING API
Please see the online documentation for a more in-depth documentation of the Scripting API: https://github.com/yasirkula/UnitySimpleFileBrowser

// Namespace
using SimpleFileBrowser;

public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };

public delegate void OnSuccess( string path );
public delegate void OnCancel();

// Showing dialog
public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel, bool folderMode = false, string initialPath = null, string title = "Save", string saveButtonText = "Save" );
public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, bool folderMode = false, string initialPath = null, string title = "Load", string loadButtonText = "Select" );

public static IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null, string title = "Save", string saveButtonText = "Save" );
public static IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null, string title = "Load", string loadButtonText = "Select" );

// Force closing an open dialog
public static void HideDialog( bool invokeCancelCallback = false );

// Customizing the dialog
public static bool AddQuickLink( string name, string path, Sprite icon = null );
public static void SetExcludedExtensions( params string[] excludedExtensions );

public static void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters );
public static void SetFilters( bool showAllFilesFilter, params string[] filters );
public static void SetFilters( bool showAllFilesFilter, IEnumerable<FileBrowser.Filter> filters );
public static void SetFilters( bool showAllFilesFilter, params FileBrowser.Filter[] filters );

public static bool SetDefaultFilter( string defaultFilter );

// Android runtime permissions
public static FileBrowser.Permission CheckPermission();
public static FileBrowser.Permission RequestPermission();