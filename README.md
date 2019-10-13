# Unity Simple File Browser

![screenshot](screenshots/filebrowser.png)

**Available on Asset Store:** https://www.assetstore.unity3d.com/en/#!/content/113006

**Forum Thread:** https://forum.unity.com/threads/simple-file-browser-open-source.441908/

## FEATURES

- Behaves similar to Windows file chooser
- Ability to search by name or filter by type
- Quick links
- Simple user interface
- Draggable and resizable
- Ability to choose folders instead of files
- Supports runtime permissions on Android M+
- Optimized using a recycled list view (makes *Instantiate* calls sparingly)

**NOTE:** Universal Windows Platform (UWP) is not supported!

## HOW TO

Simply import [SimpleFileBrowser.unitypackage](https://github.com/yasirkula/UnitySimpleFileBrowser/releases) to your project. Afterwards, add `using SimpleFileBrowser;` to your script.

*for Android*: set **Write Permission** to **External (SDCard)** in **Player Settings**

**NOTE:** On *Android Q (10)* or later, it is impossible to work with *File* APIs. On these devices, SimpleFileBrowser uses *Storage Access Framework (SAF)* to browse the files. However, paths returned by SAF are not File API compatible. To simulate the behaviour of the File API on all devices (including SAF), you can check out the **FileBrowserHelpers** functions. For reference, here is an example SAF path: `content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3APictures`

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

You can force close an open dialog using the following function:

```csharp
public static void HideDialog( bool invokeCancelCallback = false );
```

If there is an open dialog and the **invokeCancelCallback** parameter is set to *true*, the *onCancel* callback of the dialog will be invoked. This function can also be used to initialize the file browser ahead of time, which in turn will reduce the lag when you first open a dialog.

To add a quick link to the browser, you can use the following function:

```csharp
public static bool AddQuickLink( string name, string path, Sprite icon = null );
```

When **icon** parameter is left as *null*, the quick link will have a folder icon.

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

To open files or directories in the file browser with a single click (instead of double clicking), you can set **FileBrowser.SingleClickMode** to *true*.

On Android, file browser requires external storage access to function properly. You can use the following function to check if we have runtime permission to access the external storage:

```csharp
public static FileBrowser.Permission CheckPermission();
```

**FileBrowser.Permission** is an enum that can take 3 values:

- **Granted**: we have the permission to access the external storage
- **ShouldAsk**: we don't have permission yet, but we can ask the user for permission via *RequestPermission* function (see below). As long as the user doesn't select "Don't ask again" while denying the permission, ShouldAsk is returned
- **Denied**: we don't have permission and we can't ask the user for permission. In this case, user has to give the permission from Settings. This happens when user selects "Don't ask again" while denying the permission or when user is not allowed to give that permission (parental controls etc.)

To request permission to access the external storage, use the following function:

```csharp
public static FileBrowser.Permission RequestPermission();
```

Note that FileBrowser automatically calls RequestPermission before opening a dialog. If you want, you can turn this feature off by setting **FileBrowser.AskPermissions** to *false*.

The following file manipulation functions work on all platforms (including *Storage Access Framework (SAF)* on *Android 10+*). These functions should be called with the paths returned by the FileBrowser functions only:

```csharp
public static bool FileBrowserHelpers.FileExists( string path );
public static bool FileBrowserHelpers.DirectoryExists( string path );
public static bool FileBrowserHelpers.IsDirectory( string path );
public static FileSystemEntry[] FileBrowserHelpers.GetEntriesInDirectory( string path ); // Returns all files and folders in a directory
public static string FileBrowserHelpers.CreateFileInDirectory( string directoryPath, string filename ); // Returns the created file's path
public static string FileBrowserHelpers.CreateFolderInDirectory( string directoryPath, string folderName ); // Returns the created folder's path
public static void FileBrowserHelpers.WriteBytesToFile( string targetPath, byte[] bytes );
public static void FileBrowserHelpers.WriteTextToFile( string targetPath, string text );
public static void FileBrowserHelpers.WriteCopyToFile( string targetPath, string sourceFile ); // Copies the contents of sourceFile to target file. Here, sourceFile must be a file path (i.e. don't use a SAF path as sourceFile)
public static void FileBrowserHelpers.AppendBytesToFile( string targetPath, byte[] bytes );
public static void FileBrowserHelpers.AppendTextToFile( string targetPath, string text );
public static void FileBrowserHelpers.AppendCopyToFile( string targetPath, string sourceFile ); // Appends the contents of sourceFile to target file. Here, sourceFile must be a file path
public static byte[] FileBrowserHelpers.ReadBytesFromFile( string sourcePath );
public static string FileBrowserHelpers.ReadTextFromFile( string sourcePath );
public static void FileBrowserHelpers.ReadCopyFromFile( string sourcePath, string destinationFile ); // Copies the contents of source to destinationFile. Here, destinationFile must be a file path
public static string FileBrowserHelpers.RenameFile( string path, string newName ); // Returns the new path of the file
public static string FileBrowserHelpers.RenameDirectory( string path, string newName ); // Returns the new path of the directory
public static void FileBrowserHelpers.DeleteFile( string path );
public static void FileBrowserHelpers.DeleteDirectory( string path );
public static string FileBrowserHelpers.GetFilename( string path );
public static long FileBrowserHelpers.GetFilesize( string path );
public static DateTime FileBrowserHelpers.GetLastModifiedDate( string path );
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
		
		if( FileBrowser.Success )
		{
			// If a file was chosen, read its bytes via FileBrowserHelpers
			// Contrary to File.ReadAllBytes, this function works on Android 10+, as well
			byte[] bytes = FileBrowserHelpers.ReadBytesFromFile( FileBrowser.Result );
		}
	}
}
```
