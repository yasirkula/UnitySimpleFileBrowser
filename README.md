# Unity Simple File Browser

![screenshot](https://yasirkula.files.wordpress.com/2016/11/simplefileexplorer.png)

## FEATURES
- Behaves similar to Windows file chooser
- Costs 3 SetPass calls and ~13 draw calls
- Ability to search by name or filter by type
- Quick links
- Simple user interface
- Draggable and resizable
- Ability to choose folders instead of files
- Optimized using a recycled list view (makes *Instantiate* calls sparingly)

## HOW TO
Simply import **SimpleFileBrowser.unitypackage** to your project. Afterwards, you should add `using SimpleFileBrowser;` to the top of the script that will call the file browser.

The file browser can be shown either as a **save dialog** or a **load dialog**. In load mode, the returned path always leads to an existing file or folder. In save mode, the returned path can point to a non-existing file, as well. You can use the following functions to show the file browser:

```csharp
public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel, bool folderMode = false, string initialPath = null, string title = "Save", string saveButtonText = "Save" );
public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, bool folderMode = false, string initialPath = null, string title = "Load", string loadButtonText = "Select" );

public delegate void OnSuccess( string path );
public delegate void OnCancel();
```

There can only be one dialog active at a time. These functions will return *true* if the dialog is shown successfully (if no other dialog is active), *false* otherwise. You can query the **FileBrowser.IsOpen** property to see if there is an active dialog at the moment.

If user presses the *Cancel* button, **onCancel** callback is called. Otherwise, **onSuccess** callback is called with the path of the selected file/folder as parameter. When **folderMode** is set to *true*, the file browser will show only folders and the user will pick a folder instead of a file.

There are also coroutine variants of these functions that will yield while the dialog is active:

```csharp
public static IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null, string title = "Save", string saveButtonText = "Save" );									 
public static IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null, string title = "Load", string loadButtonText = "Select" );
```

After the dialog is closed, you can check the **FileBrowser.Success** property to see whether the user selected a file/folder or cancelled the operation and if FileBrowser.Success was set to *true*, you can use the **FileBrowser.Result** property to get the path of the selected file/folder.

To add a quick link to the browser, you can use the following function:

```csharp
public static bool AddQuickLink( string name, string path, Sprite icon = null );
```

When **icon** parameter is left *null*, the quick link will have a folder icon.

By default, the file browser doesn't show files with *.lnk* or *.tmp* extensions. You can extend this list or remove this restriction altogether using the following function:

```csharp
public static void SetExcludedExtensions( params string[] excludedExtensions );
```

Lastly, you can use the following functions to set the file filters:

```csharp
public static void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters );
public static void SetFilters( bool showAllFilesFilter, params string[] filters );
public static void SetFilters( bool showAllFilesFilter, IEnumerable<FileBrowser.Filter> filters );
public static void SetFilters( bool showAllFilesFilter, params FileBrowser.Filter[] filters );
```

When **showAllFilesFilter** is set to true, a filter by the name "*All Files (.\*)*" will appear that will show all the files when selected. To select a default filter, use the following function:

```csharp
public static bool SetDefaultFilter( string defaultFilter );
```

## EXAMPLE CODE
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
		// Name: Users
		// Path: C:\Users
		// Icon: default (folder icon)
		FileBrowser.AddQuickLink( "Users", "C:\\Users", null );

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
