# Unity Simple File Browser

![screenshot](https://yasirkula.files.wordpress.com/2016/11/simplefileexplorer.png)

##Features##
- Costs ~13 draw calls and 3 SetPass calls
- Ability to search by name or filter by type
- Quick links
- Simple user interface
- Draggable and resizable
- Behaves similar to Windows file chooser
- Ability to choose folders instead of files
- Optimized using a recycled list view (makes Instantiate calls sparingly)

**IMPORTANT: After importing the unitypackage, sometimes the references between the plugin's components are lost. After running the game once, the references are restored, strangely. Just make sure that nothing in the Project view is selected while running the game the first time after importing the package.**

-- *Documentation soon; you can inspect the example code below for now to have an idea...*

##Example Code##
```csharp
using UnityEngine;
using System.Collections;
using SimpleFileBrowser;

public class FileBrowserTest : MonoBehaviour
{
	// Warning: paths returned by FileBrowser dialogs do not contain a trailing '\' character
	// Warning: FileBrowser can only show 1 dialog at a time

	void Start()
	{
		// Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Images", ".jpg", ".png" ), new FileBrowser.Filter( "Text Files", ".txt", ".pdf" ) );

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".jpg" );

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

		// Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
		// It is sufficient to add a quick link just once
		// Icon: default (folder icon)
		// Name: Users
		// Path: C:\Users
		FileBrowser.AddQuickLink( null, "Users", "C:\\Users" );

		// Show a save file dialog 
		// onSuccess event: not registered (which means this dialog is pretty useless)
		// onCancel event: not registered
		// Save file/folder: file, Initial path: "C:\", Title: "Save As", submit button text: "Save"
		// FileBrowser.ShowSaveDialog( null, null, false, "C:\\", "Save As", "Save" );

		// Show a select folder dialog 
		// onSuccess event: print the selected folder's path
		// onCancel event: print "Canceled"
		// Load file/folder: folder, Initial path: default (Documents), Title: "Select Folder", submit button text: "Select"
		// FileBrowser.ShowLoadDialog( (path) => { Debug.Log( "Selected: " + path ); }, 
		//                                () => { Debug.Log( "Canceled" ); }, 
		//                                true, null, "Select Folder", "Select" );

		// Coroutine example
		StartCoroutine( ShowLoadDialogCoroutine() );
	}

	IEnumerator ShowLoadDialogCoroutine()
	{
		// Show a load file dialog and wait for a response from user
		// Load file/folder: file, Initial path: default (Documents), Title: "Load File", submit button text: "Load"
		yield return FileBrowser.WaitForLoadDialog( false, null, "Load File", "Load" );

		// Dialog is closed
		// Print whether a file is chosen (FileBrowser.Success)
		// and the path to the selected file (FileBrowser.Result) (null, if FileBrowser.Success is false)
		Debug.Log( FileBrowser.Success + " " + FileBrowser.Result );
	}
}
```

##Method Signatures##

```csharp
public delegate void OnSuccess( string path );
public delegate void OnCancel();

public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel,
								   bool folderMode = false, string initialPath = null,
								   string title = "Save", string saveButtonText = "Save" );
								   
public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, 
								   bool folderMode = false, string initialPath = null,
								   string title = "Load", string loadButtonText = "Select" );
								   
public static IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null,
											 string title = "Save", string saveButtonText = "Save" );
											 
public static IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null,
											 string title = "Load", string loadButtonText = "Select" );
											 
public static bool AddQuickLink( Sprite icon, string name, string path );

public static void SetExcludedExtensions( params string[] excludedExtensions );

public static void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters );
public static void SetFilters( bool showAllFilesFilter, params string[] filters );
public static void SetFilters( bool showAllFilesFilter, IEnumerable<FileBrowser.Filter> filters );
public static void SetFilters( bool showAllFilesFilter, params FileBrowser.Filter[] filters );
public static bool SetDefaultFilter( string defaultFilter )
```
