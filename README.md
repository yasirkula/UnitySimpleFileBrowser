# Unity Simple File Browser

![screenshot](https://yasirkula.files.wordpress.com/2016/11/simplefileexplorer.png)

##Features##
- Costs about 10 draw calls and 3 SetPass calls
- Ability to search by name or filter by type
- Quick links
- Simple user interface
- Draggable and resizable
- Behaves similar to Windows file chooser
- Ability to choose folders instead of files
- Slightly optimized (reuses inactive file templates but does not recycle them while scrolling)

-- *Documentation soon; you can inspect the example code below for now to have an idea...*

##Example Code##
```csharp
using UnityEngine;
using System.Collections;

public class FileBrowserTest : MonoBehaviour
{
	// Warning: paths returned by SimpleFileBrowser dialogs do not contain a '\' character at the end
	// Warning: SimpleFileBrowser can only show 1 dialog at a time

	void Start()
	{
		// Set filters (optional)
		SimpleFileBrowser.SetFilters( ".jpg", ".png" );

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		SimpleFileBrowser.SetDefaultFilter( ".jpg" );

		// Set excluded file extensions (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you add them as parameters to the function
		SimpleFileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

		// Add a new quick link to the browser (returns true if quick link is added successfully)
		// Icon: default (folder icon)
		// Name: Users
		// Path: C:\Users
		// Warning: it is sufficient to add a quick link just once, otherwise there may be duplicates
		SimpleFileBrowser.AddQuickLink( null, "Users", "C:\\Users" );

		// Show a save file dialog 
		// onSuccess event: not registered (which means this dialog is pretty useless)
		// onCancel event: not registered
		// Save file/folder: file, Initial path: "C:\", Title: "Save As", submit button text: "Save"
		// SimpleFileBrowser.ShowSaveDialog( null, null, false, "C:\\", "Save As", "Save" );

		// Show a select folder dialog 
		// onSuccess event: print the selected folder's path
		// onCancel event: print "Canceled"
		// Load file/folder: folder, Initial path: default (Documents), Title: "Select Folder", submit button text: "Select"
		// SimpleFileBrowser.ShowLoadDialog( (path) => { Debug.Log( "Selected: " + path ); }, 
		//								  () => { Debug.Log( "Canceled" ); }, 
		//								  true, null, "Select Folder", "Select" );

		// Coroutine example
		StartCoroutine( ShowLoadDialogCoroutine() );
	}

	IEnumerator ShowLoadDialogCoroutine()
	{
		// Show a load file dialog and wait for a response from user
		// Load file/folder: file, Initial path: default (Documents), Title: "Load File", submit button text: "Load"
		yield return StartCoroutine( SimpleFileBrowser.WaitForLoadDialog( false, null, "Load File", "Load" ) );

		// Dialog is closed
		// Print whether a file is chosen (SimpleFileBrowser.Success)
		// and the path to the selected file (SimpleFileBrowser.Result) (null, if SimpleFileBrowser.Success is false)
		Debug.Log( SimpleFileBrowser.Success + " " + SimpleFileBrowser.Result );
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

public static void SetFilters( List<string> filters );
public static void SetFilters( params string[] filters );
public static bool SetDefaultFilter( string defaultFilter )
```
