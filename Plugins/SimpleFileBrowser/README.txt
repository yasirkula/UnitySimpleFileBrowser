= Simple File Browser =

Online documentation & example code available at: https://github.com/yasirkula/UnitySimpleFileBrowser
E-mail: yasirkula@gmail.com

1. ABOUT
This plugin helps you show save/load dialogs during gameplay with its uGUI based file browser.


2. HOW TO
The file browser can be shown either as a save dialog or a load dialog. In load mode, the returned path(s) always lead to existing files or folders. In save mode, the returned path(s) can point to non-existing files, as well.


3. FAQ
- Android build fails, it says "error: attribute android:requestLegacyExternalStorage not found" in Console
"android:requestLegacyExternalStorage" attribute in AndroidManifest.xml grants full access to device's storage on Android 10 but requires you to update your Android SDK to at least SDK 29. If this isn't possible for you, you should open SimpleFileBrowser.aar with WinRAR or 7-Zip and then remove the "<application ... />" tag from AndroidManifest.xml.

- Can't show the file browser on Android, it says "java.lang.ClassNotFoundException: com.yasirkula.unity.FileBrowserPermissionReceiver" in Logcat
If your project uses ProGuard, try adding the following line to ProGuard filters: -keep class com.yasirkula.unity.* { *; }

- File browser doesn't show any files on Android 10+
File browser uses Storage Access Framework on these Android versions and users must first click the "Pick Folder" button in the quick links section

- RequestPermission returns Permission.Denied on Android
Declare the WRITE_EXTERNAL_STORAGE permission manually in your Plugins/Android/AndroidManifest.xml file as follows: <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" tools:node="replace"/>
You'll need to add the following attribute to the '<manifest ...>' element: xmlns:tools="http://schemas.android.com/tools"


4. SCRIPTING API
Please see the online documentation for a more in-depth documentation of the Scripting API: https://github.com/yasirkula/UnitySimpleFileBrowser

NOTE: On Android Q (10) or later, it is impossible to work with File APIs. On these devices, SimpleFileBrowser uses Storage Access Framework (SAF) to browse the files. However, paths returned by SAF are not File API compatible. To simulate the behaviour of the File API on all devices (including SAF), you can check out the FileBrowserHelpers functions. For reference, here is an example SAF path: content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3APictures

// Namespace
using SimpleFileBrowser;

public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };
public enum PickMode { Files = 0, Folders = 1, FilesAndFolders = 2 };

public delegate void OnSuccess( string[] paths );
public delegate void OnCancel();

// Showing dialog
bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel, PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Save", string saveButtonText = "Save" );
bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Load", string loadButtonText = "Select" );

IEnumerator WaitForSaveDialog( PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Save", string saveButtonText = "Save" );
IEnumerator WaitForLoadDialog( PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Load", string loadButtonText = "Select" );

// Force closing an open dialog
void HideDialog( bool invokeCancelCallback = false );

// Customizing the dialog
bool AddQuickLink( string name, string path, Sprite icon = null );
void SetExcludedExtensions( params string[] excludedExtensions );

// Filters should include the period (e.g. ".jpg" instead of "jpg")
void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters );
void SetFilters( bool showAllFilesFilter, params string[] filters );
void SetFilters( bool showAllFilesFilter, IEnumerable<FileBrowser.Filter> filters );
void SetFilters( bool showAllFilesFilter, params FileBrowser.Filter[] filters );

bool SetDefaultFilter( string defaultFilter );

// Android runtime permissions
FileBrowser.Permission CheckPermission();
FileBrowser.Permission RequestPermission();

// File manipulation functions that work on all platforms (including Storage Access Framework (SAF) on Android 10+)
// These functions should be called with the paths returned by the FileBrowser functions only
bool FileBrowserHelpers.FileExists( string path );
bool FileBrowserHelpers.DirectoryExists( string path );
bool FileBrowserHelpers.IsDirectory( string path );
string FileBrowserHelpers.GetDirectoryName( string path );
FileSystemEntry[] FileBrowserHelpers.GetEntriesInDirectory( string path ); // Returns all files and folders in a directory
string FileBrowserHelpers.CreateFileInDirectory( string directoryPath, string filename ); // Returns the created file's path
string FileBrowserHelpers.CreateFolderInDirectory( string directoryPath, string folderName ); // Returns the created folder's path
void FileBrowserHelpers.WriteBytesToFile( string targetPath, byte[] bytes );
void FileBrowserHelpers.WriteTextToFile( string targetPath, string text );
void FileBrowserHelpers.AppendBytesToFile( string targetPath, byte[] bytes );
void FileBrowserHelpers.AppendTextToFile( string targetPath, string text );
byte[] FileBrowserHelpers.ReadBytesFromFile( string sourcePath );
string FileBrowserHelpers.ReadTextFromFile( string sourcePath );
void FileBrowserHelpers.CopyFile( string sourcePath, string destinationPath );
void FileBrowserHelpers.CopyDirectory( string sourcePath, string destinationPath );
void FileBrowserHelpers.MoveFile( string sourcePath, string destinationPath );
void FileBrowserHelpers.MoveDirectory( string sourcePath, string destinationPath );
string FileBrowserHelpers.RenameFile( string path, string newName ); // Returns the new path of the file
string FileBrowserHelpers.RenameDirectory( string path, string newName ); // Returns the new path of the directory
void FileBrowserHelpers.DeleteFile( string path );
void FileBrowserHelpers.DeleteDirectory( string path );
string FileBrowserHelpers.GetFilename( string path );
long FileBrowserHelpers.GetFilesize( string path );
DateTime FileBrowserHelpers.GetLastModifiedDate( string path );