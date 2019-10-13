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

NOTE: On Android Q (10) or later, it is impossible to work with File APIs. On these devices, SimpleFileBrowser uses Storage Access Framework (SAF) to browse the files. However, paths returned by SAF are not File API compatible. To simulate the behaviour of the File API on all devices (including SAF), you can check out the FileBrowserHelpers functions. For reference, here is an example SAF path: content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3APictures

// Namespace
using SimpleFileBrowser;

public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };

public delegate void OnSuccess( string path );
public delegate void OnCancel();

// Showing dialog
bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel, bool folderMode = false, string initialPath = null, string title = "Save", string saveButtonText = "Save" );
bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, bool folderMode = false, string initialPath = null, string title = "Load", string loadButtonText = "Select" );

IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null, string title = "Save", string saveButtonText = "Save" );
IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null, string title = "Load", string loadButtonText = "Select" );

// Force closing an open dialog
void HideDialog( bool invokeCancelCallback = false );

// Customizing the dialog
bool AddQuickLink( string name, string path, Sprite icon = null );
void SetExcludedExtensions( params string[] excludedExtensions );

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
FileSystemEntry[] FileBrowserHelpers.GetEntriesInDirectory( string path ); // Returns all files and folders in a directory
string FileBrowserHelpers.CreateFileInDirectory( string directoryPath, string filename ); // Returns the created file's path
string FileBrowserHelpers.CreateFolderInDirectory( string directoryPath, string folderName ); // Returns the created folder's path
void FileBrowserHelpers.WriteBytesToFile( string targetPath, byte[] bytes );
void FileBrowserHelpers.WriteTextToFile( string targetPath, string text );
void FileBrowserHelpers.WriteCopyToFile( string targetPath, string sourceFile ); // Copies the contents of sourceFile to target file. Here, sourceFile must be a file path (i.e. don't use a SAF path as sourceFile)
void FileBrowserHelpers.AppendBytesToFile( string targetPath, byte[] bytes );
void FileBrowserHelpers.AppendTextToFile( string targetPath, string text );
void FileBrowserHelpers.AppendCopyToFile( string targetPath, string sourceFile ); // Appends the contents of sourceFile to target file. Here, sourceFile must be a file path
byte[] FileBrowserHelpers.ReadBytesFromFile( string sourcePath );
string FileBrowserHelpers.ReadTextFromFile( string sourcePath );
void FileBrowserHelpers.ReadCopyFromFile( string sourcePath, string destinationFile ); // Copies the contents of source to destinationFile. Here, destinationFile must be a file path
string FileBrowserHelpers.RenameFile( string path, string newName ); // Returns the new path of the file
string FileBrowserHelpers.RenameDirectory( string path, string newName ); // Returns the new path of the directory
void FileBrowserHelpers.DeleteFile( string path );
void FileBrowserHelpers.DeleteDirectory( string path );
string FileBrowserHelpers.GetFilename( string path );
long FileBrowserHelpers.GetFilesize( string path );
DateTime FileBrowserHelpers.GetLastModifiedDate( string path );