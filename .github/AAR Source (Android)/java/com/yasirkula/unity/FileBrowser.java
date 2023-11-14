package com.yasirkula.unity;

import android.Manifest;
import android.annotation.TargetApi;
import android.app.Activity;
import android.app.Fragment;
import android.content.Context;
import android.content.Intent;
import android.content.UriPermission;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.os.storage.StorageManager;
import android.os.storage.StorageVolume;
import android.provider.DocumentsContract;
import android.util.Log;
import android.webkit.MimeTypeMap;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;

/**
 * Created by yasirkula on 30.10.2017.
 */

public class FileBrowser
{
	@TargetApi( Build.VERSION_CODES.Q )
	private static class UriPermissionSorter implements Comparator<UriPermission>
	{
		public int compare( UriPermission a, UriPermission b )
		{
			long difference = b.getPersistedTime() - a.getPersistedTime();
			if( difference < 0 )
				return -1;
			if( difference > 0 )
				return 1;

			return 0;
		}
	}

	private static final StringBuilder stringBuilder = new StringBuilder();

	public static String GetExternalDrives( Context context )
	{
		File primary = Environment.getExternalStorageDirectory();
		String primaryPath = primary.getAbsolutePath();
		String primaryCanonicalPath = primaryPath;
		try
		{
			primaryCanonicalPath = primary.getCanonicalPath();
		}
		catch( Exception e )
		{
		}

		stringBuilder.setLength( 0 );
		stringBuilder.append( primaryPath ).append( ":" );

		HashSet<String> potentialDrives = new HashSet<String>( 16 );

		// Try paths saved at system environments
		// Credit: https://stackoverflow.com/a/32088396/2373034
		String strSDCardPath = System.getenv( "SECONDARY_STORAGE" );
		if( strSDCardPath == null || strSDCardPath.length() == 0 )
			strSDCardPath = System.getenv( "EXTERNAL_SDCARD_STORAGE" );

		if( strSDCardPath != null && strSDCardPath.length() > 0 )
		{
			String[] externalPaths = strSDCardPath.split( ":" );
			for( int i = 0; i < externalPaths.length; i++ )
			{
				String path = externalPaths[i];
				if( path != null && path.length() > 0 )
					potentialDrives.add( path );
			}
		}

		// Try most common possible paths
		// Credit: https://gist.github.com/PauloLuan/4bcecc086095bce28e22
		String[] possibleRoots = new String[] { "/storage", "/mnt", "/storage/removable",
				"/removable", "/data", "/mnt/media_rw", "/mnt/sdcard0" };
		for( String root : possibleRoots )
		{
			try
			{
				File[] fileList = new File( root ).listFiles();
				for( File file : fileList )
				{
					if( file.exists() && file.isDirectory() && file.canRead() )
						potentialDrives.add( file.getAbsolutePath() );
				}
			}
			catch( Exception e )
			{
			}
		}

		// This is the only working method on some Android 11+ devices (when Storage Access Framework isn't used)
		if( android.os.Build.VERSION.SDK_INT >= 30 )
		{
			for( StorageVolume volume : ( (StorageManager) context.getSystemService( Context.STORAGE_SERVICE ) ).getStorageVolumes() )
			{
				File volumeDirectory = volume.getDirectory();
				if( volumeDirectory != null )
					potentialDrives.add( volumeDirectory.toString() );
			}
		}
		else if( android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N )
		{
			try
			{
				Method getPath = StorageVolume.class.getMethod( "getPath" );
				for( StorageVolume volume : ( (StorageManager) context.getSystemService( Context.STORAGE_SERVICE ) ).getStorageVolumes() )
					potentialDrives.add( (String) getPath.invoke( volume ) );
			}
			catch( Exception e )
			{
			}
		}

		for( String potentialDrive : potentialDrives )
		{
			File file = new File( potentialDrive );
			if( file.exists() && file.isDirectory() && file.canRead() && !file.getAbsolutePath().equalsIgnoreCase( primaryPath ) )
			{
				String absolutePath = file.getAbsolutePath() + File.separator + "Android";
				if( new File( absolutePath ).exists() )
				{
					try
					{
						// Check if two paths lead to same storage (aliases)
						if( !primaryCanonicalPath.equals( file.getCanonicalPath() ) )
							stringBuilder.append( file.getAbsolutePath() ).append( ":" );
					}
					catch( Exception ex )
					{
					}
				}
			}
		}

		return stringBuilder.toString();
	}

	@TargetApi( Build.VERSION_CODES.M )
	public static int CheckPermission( Context context )
	{
		if( Build.VERSION.SDK_INT < Build.VERSION_CODES.M )
			return 1;

		if( ( Build.VERSION.SDK_INT < 33 || context.getApplicationInfo().targetSdkVersion < 33 ) && context.checkSelfPermission( Manifest.permission.READ_EXTERNAL_STORAGE ) != PackageManager.PERMISSION_GRANTED )
			return 0;

		if( Build.VERSION.SDK_INT < 30 && context.checkSelfPermission( Manifest.permission.WRITE_EXTERNAL_STORAGE ) != PackageManager.PERMISSION_GRANTED )
			return 0;

		return 1;
	}

	// Credit: https://github.com/Over17/UnityAndroidPermissions/blob/0dca33e40628f1f279decb67d901fd444b409cd7/src/UnityAndroidPermissions/src/main/java/com/unity3d/plugin/UnityAndroidPermissions.java
	public static void RequestPermission( Context context, final FileBrowserPermissionReceiver permissionReceiver, final int lastCheckResult )
	{
		if( CheckPermission( context ) == 1 )
		{
			permissionReceiver.OnPermissionResult( 1 );
			return;
		}

		if( lastCheckResult == 0 ) // If user clicked "Don't ask again" before, don't bother asking them again
		{
			permissionReceiver.OnPermissionResult( 0 );
			return;
		}

		final Fragment request = new FileBrowserPermissionFragment( permissionReceiver );
		( (Activity) context ).getFragmentManager().beginTransaction().add( 0, request ).commitAllowingStateLoss();
	}

	// Returns whether or not Storage Access Framework (SAF) should be used
	public static boolean CheckSAF()
	{
		return android.os.Build.VERSION.SDK_INT >= 29 && !Environment.isExternalStorageLegacy();
	}

	// Prompts the user to pick a Storage Access Framework (SAF) folder
	@TargetApi( Build.VERSION_CODES.Q )
	public static void PickSAFFolder( Context context, final FileBrowserDirectoryReceiver directoryReceiver )
	{
		final Fragment request = new FileBrowserDirectoryPickerFragment( directoryReceiver );
		( (Activity) context ).getFragmentManager().beginTransaction().add( 0, request ).commitAllowingStateLoss();
	}

	// Retrieves the previously picked Storage Access Framework (SAF) folder uris
	@TargetApi( Build.VERSION_CODES.Q )
	public static String FetchSAFQuickLinks( Context context )
	{
		// Store only the most recent maxPersistedUriPermissions quick links
		final int maxPersistedUriPermissions = 5;

		stringBuilder.setLength( 0 );

		List<UriPermission> uriPermissions = context.getContentResolver().getPersistedUriPermissions();
		uriPermissions.sort( new UriPermissionSorter() );

		int count = 0;
		for( int i = 0; i < uriPermissions.size(); i++ )
		{
			UriPermission uriPermission = uriPermissions.get( i );

			if( count >= maxPersistedUriPermissions || uriPermission.getPersistedTime() == UriPermission.INVALID_TIME || !uriPermission.isReadPermission() || !uriPermission.isWritePermission() )
				context.getContentResolver().releasePersistableUriPermission( uriPermission.getUri(), Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION );
			else
			{
				FileBrowserSAFEntry directory = FileBrowserSAFEntry.fromTreeUri( context, uriPermission.getUri() );
				if( directory != null && directory.exists() && directory.isDirectory() )
				{
					stringBuilder.append( directory.getName() ).append( "<>" ).append( directory.getUri().toString() ).append( "<>" );
					count++;
				}
				else
					context.getContentResolver().releasePersistableUriPermission( uriPermission.getUri(), Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION );
			}
		}

		stringBuilder.append( count );
		return stringBuilder.toString();
	}

	// Copies/moves a Storage Access Framework (SAF) file/raw file
	@TargetApi( Build.VERSION_CODES.Q )
	public static void CopyFile( Context context, String sourceRawUri, String destinationRawUri, boolean isMoveOperation )
	{
		// Parameters can point to either SAF files or raw filesystem files
		boolean isSourceSAFFile = sourceRawUri.contains( "://" );
		boolean isDestinationSAFFile = destinationRawUri.contains( "://" );

		if( isSourceSAFFile )
		{
			if( isDestinationSAFFile )
			{
				// Copy SAF file to SAF file
				CopySAFEntry( context, sourceRawUri, destinationRawUri );
			}
			else
			{
				// Copy SAF file to raw file
				ReadFromSAFEntry( context, sourceRawUri, destinationRawUri );
			}
		}
		else
		{
			if( isDestinationSAFFile )
			{
				// Copy raw file to SAF file
				WriteToSAFEntry( context, destinationRawUri, sourceRawUri, false );
			}
			else
			{
				// Copy raw file to raw file
				CopyRawFile( sourceRawUri, destinationRawUri );
			}
		}

		if( isMoveOperation )
		{
			if( isSourceSAFFile )
				DeleteSAFEntry( context, sourceRawUri );
			else
				new File( sourceRawUri ).delete();
		}
	}

	// Copies/moves a Storage Access Framework (SAF) directory/raw directory
	@TargetApi( Build.VERSION_CODES.Q )
	public static void CopyDirectory( Context context, String sourceRawUri, String destinationRawUri, boolean isMoveOperation )
	{
		// Parameters can point to either SAF directories or raw filesystem directories
		boolean isSourceSAFDirectory = sourceRawUri.contains( "://" );
		boolean isDestinationSAFDirectory = destinationRawUri.contains( "://" );

		if( isSourceSAFDirectory )
			CopySAFDirectoryRecursively( context, new FileBrowserSAFEntry( context, Uri.parse( sourceRawUri ) ), destinationRawUri, isDestinationSAFDirectory );
		else
			CopyRawDirectoryRecursively( context, new File( sourceRawUri ), destinationRawUri, isDestinationSAFDirectory );

		if( isMoveOperation )
		{
			if( isSourceSAFDirectory )
				DeleteSAFEntry( context, sourceRawUri );
			else
				DeleteRawDirectoryRecursively( new File( sourceRawUri ) );
		}
	}

	// Fetches the contents of a Storage Access Framework (SAF) folder
	@TargetApi( Build.VERSION_CODES.Q )
	public static String OpenSAFFolder( Context context, String rawUri )
	{
		FileBrowserSAFEntry directory = new FileBrowserSAFEntry( context, Uri.parse( rawUri ) );

		stringBuilder.setLength( 0 );
		directory.appendFilesToStringBuilder( stringBuilder );

		return stringBuilder.toString();
	}

	// Creates a new Storage Access Framework (SAF) file/folder
	@TargetApi( Build.VERSION_CODES.Q )
	public static String CreateSAFEntry( Context context, String folderRawUri, boolean isFolder, String name )
	{
		FileBrowserSAFEntry directory = new FileBrowserSAFEntry( context, Uri.parse( folderRawUri ) );
		if( isFolder )
			return directory.createDirectory( name ).getUri().toString();

		int extensionSeparator = name.lastIndexOf( '.' );
		String extension = extensionSeparator >= 0 ? name.substring( extensionSeparator + 1 ) : "";

		// Credit: https://stackoverflow.com/a/31691791/2373034
		String mimeType = extension.length() > 0 ? MimeTypeMap.getSingleton().getMimeTypeFromExtension( extension.toLowerCase( Locale.ENGLISH ) ) : null;
		if( mimeType == null || mimeType.length() == 0 )
			mimeType = "application/octet-stream";

		return directory.createFile( mimeType, name ).getUri().toString();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static void WriteToSAFEntry( Context context, String rawUri, String sourceFile, boolean appendMode )
	{
		try
		{
			InputStream input = new FileInputStream( new File( sourceFile ) );
			try
			{
				OutputStream output = context.getContentResolver().openOutputStream( Uri.parse( rawUri ), appendMode ? "wa" : "rwt" );
				if( output == null )
					return;

				try
				{
					byte[] buf = new byte[4096];
					int len;
					while( ( len = input.read( buf ) ) > 0 )
						output.write( buf, 0, len );
				}
				finally
				{
					output.close();
				}
			}
			finally
			{
				input.close();
			}
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static void ReadFromSAFEntry( Context context, String rawUri, String destinationFile )
	{
		try
		{
			InputStream input = context.getContentResolver().openInputStream( Uri.parse( rawUri ) );
			if( input == null )
				return;

			try
			{
				OutputStream output = new FileOutputStream( new File( destinationFile ), false );
				try
				{
					byte[] buf = new byte[4096];
					int len;
					while( ( len = input.read( buf ) ) > 0 )
						output.write( buf, 0, len );
				}
				finally
				{
					output.close();
				}
			}
			finally
			{
				input.close();
			}
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static void CopySAFEntry( Context context, String sourceRawUri, String destinationRawUri )
	{
		try
		{
			InputStream input = context.getContentResolver().openInputStream( Uri.parse( sourceRawUri ) );
			if( input == null )
				return;

			try
			{
				OutputStream output = context.getContentResolver().openOutputStream( Uri.parse( destinationRawUri ), "rwt" );
				if( output == null )
					return;

				try
				{
					byte[] buf = new byte[4096];
					int len;
					while( ( len = input.read( buf ) ) > 0 )
						output.write( buf, 0, len );
				}
				finally
				{
					output.close();
				}
			}
			finally
			{
				input.close();
			}
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static boolean SAFEntryExists( Context context, String rawUri, boolean isDirectory )
	{
		FileBrowserSAFEntry entry = new FileBrowserSAFEntry( context, Uri.parse( rawUri ) );
		return entry.exists() && entry.isDirectory() == isDirectory;
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static boolean SAFEntryDirectory( Context context, String rawUri )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).isDirectory();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static String SAFEntryName( Context context, String rawUri )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).getName();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static long SAFEntrySize( Context context, String rawUri )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).length();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static long SAFEntryLastModified( Context context, String rawUri )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).lastModified();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static String RenameSAFEntry( Context context, String rawUri, String newName )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).renameTo( newName );
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static boolean DeleteSAFEntry( Context context, String rawUri )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).delete();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static String GetParentDirectory( Context context, String rawUri )
	{
		try
		{
			if( !rawUri.contains( "://" ) )
			{
				// This is a raw filepath, not a SAF path
				String parentPath = new File( rawUri ).getParent();
				return parentPath != null ? parentPath : "";
			}

			// The most promising method is to calculate the URI's path using findDocumentPath, omit the last path segment from it
			// and then replace the rawUri's path entirely
			DocumentsContract.Path rawUriPath = DocumentsContract.findDocumentPath( context.getContentResolver(), Uri.parse( rawUri ) );
			if( rawUriPath != null )
			{
				List<String> pathSegments = rawUriPath.getPath();
				if( pathSegments != null && pathSegments.size() > 0 )
				{
					String rawUriParentPath;
					if( pathSegments.size() > 1 )
						rawUriParentPath = Uri.encode( pathSegments.get( pathSegments.size() - 2 ) );
					else
					{
						String fullPath = pathSegments.get( 0 );
						int separatorIndex = Math.max( fullPath.lastIndexOf( '/' ), fullPath.lastIndexOf( ':' ) + 1 );
						rawUriParentPath = separatorIndex > 0 ? Uri.encode( fullPath.substring( 0, separatorIndex ) ) : null;
					}

					if( rawUriParentPath != null && rawUriParentPath.length() > 0 )
					{
						int rawUriLastPathSegmentIndex = rawUri.lastIndexOf( '/' ) + 1;
						if( rawUriLastPathSegmentIndex > 0 )
						{
							String parentRawUri = rawUri.substring( 0, rawUriLastPathSegmentIndex ) + rawUriParentPath;
							if( !parentRawUri.equals( rawUri ) && SAFEntryExists( context, parentRawUri, true ) )
								return parentRawUri;
						}
					}
				}
			}

			// Omit the last path segment (this method won't work for Downloads folder and probably some other ContentProviders, too)
			int pathSeparatorIndex = rawUri.lastIndexOf( "%3A" ); // Encoded colon index
			if( pathSeparatorIndex > 0 )
				pathSeparatorIndex += 3; // Encoded colon shouldn't be omitted by substring

			pathSeparatorIndex = Math.max( pathSeparatorIndex, Math.max( rawUri.lastIndexOf( '/' ), rawUri.lastIndexOf( "%2F" ) ) );
			if( pathSeparatorIndex < 0 || pathSeparatorIndex >= rawUri.length() )
				return "";

			rawUri = rawUri.substring( 0, pathSeparatorIndex );

			if( SAFEntryExists( context, rawUri, true ) )
				return rawUri;

			// When we form the SAF URI using a subfolder as root (i.e. /storage/SomeFolder/), that subfolder is reflected in SAF URI
			// in the form /tree/primary%3ASomeFolder/ and restricts our access to SomeFolder's parent directories. However, if we
			// actually have permission to access the /storage/ directory (parent folder), we can remove the subfolder from the URI
			// (i.e. change it to /tree/primary%3A/) and voila!
			int treeStartIndex = rawUri.indexOf( "/tree/" );
			if( treeStartIndex >= 0 )
			{
				treeStartIndex += 6;
				int treeEndIndex = rawUri.indexOf( '/', treeStartIndex );
				if( treeEndIndex > treeStartIndex + 4 ) // +4: "/tree/SOMETHING/" here, SOMETHING should be able to contain at least 1 %2F/%3A and 1 other character
				{
					String treeComponent = rawUri.substring( treeStartIndex, treeEndIndex );
					String preTreeComponent = rawUri.substring( 0, treeStartIndex );
					String postTreeComponent = rawUri.substring( treeEndIndex );

					String _treeComponent = treeComponent;
					int treeSeparatorIndex = _treeComponent.length() - 3; // -3: if treeComponent ends with %2F, skip it
					while( ( treeSeparatorIndex = _treeComponent.lastIndexOf( "%2F", treeSeparatorIndex - 1 ) ) > 0 )
					{
						_treeComponent = _treeComponent.substring( 0, treeSeparatorIndex );

						String _rawUri = preTreeComponent + _treeComponent + postTreeComponent;
						if( SAFEntryExists( context, _rawUri, true ) )
							return _rawUri;
					}

					_treeComponent = treeComponent;
					treeSeparatorIndex = _treeComponent.length() - 3; // -3: if treeComponent ends with %3A, skip it
					while( ( treeSeparatorIndex = _treeComponent.lastIndexOf( "%3A", treeSeparatorIndex - 1 ) ) > 0 )
					{
						_treeComponent = _treeComponent.substring( 0, treeSeparatorIndex + 3 ); // Encoded colon (%3A) shouldn't be omitted by substring

						String _rawUri = preTreeComponent + _treeComponent + postTreeComponent;
						if( SAFEntryExists( context, _rawUri, true ) )
							return _rawUri;
					}
				}
			}
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}

		return "";
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static boolean IsSAFEntryChildOfAnother( Context context, String rawUri, String parentRawUri )
	{
		try
		{
			return DocumentsContract.isChildDocument( context.getContentResolver(), Uri.parse( parentRawUri ), Uri.parse( rawUri ) );
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
			return false;
		}
	}

	//// BEGIN UTILITY FUNCTIONS
	@TargetApi( Build.VERSION_CODES.Q )
	private static void CopyRawFile( String sourcePath, String destinationPath )
	{
		try
		{
			InputStream input = new FileInputStream( new File( sourcePath ) );
			try
			{
				OutputStream output = new FileOutputStream( new File( destinationPath ), false );
				try
				{
					byte[] buf = new byte[4096];
					int len;
					while( ( len = input.read( buf ) ) > 0 )
						output.write( buf, 0, len );
				}
				finally
				{
					output.close();
				}
			}
			finally
			{
				input.close();
			}
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	private static void CopySAFDirectoryRecursively( Context context, FileBrowserSAFEntry sourceDirectory, String destinationRawUri, boolean isDestinationSAFDirectory )
	{
		File destinationDirectory = null;
		ArrayList<FileBrowserSAFEntry> destinationContents = null;
		if( isDestinationSAFDirectory )
			destinationContents = new FileBrowserSAFEntry( context, Uri.parse( destinationRawUri ) ).listFiles();
		else
		{
			destinationDirectory = new File( destinationRawUri );
			destinationDirectory.mkdirs();
		}

		ArrayList<FileBrowserSAFEntry> contents = sourceDirectory.listFiles();
		for( int i = 0; i < contents.size(); i++ )
		{
			FileBrowserSAFEntry content = contents.get( i );
			if( content.isDirectory() )
			{
				String targetRawUri;
				if( isDestinationSAFDirectory )
					targetRawUri = FindSAFEntryWithNameOrCreateNew( context, destinationRawUri, destinationContents, true, content.getName() );
				else
					targetRawUri = new File( destinationDirectory, content.getName() ).getPath();

				CopySAFDirectoryRecursively( context, content, targetRawUri, isDestinationSAFDirectory );
			}
			else
			{
				if( isDestinationSAFDirectory )
				{
					String targetRawUri = FindSAFEntryWithNameOrCreateNew( context, destinationRawUri, destinationContents, false, content.getName() );
					CopySAFEntry( context, content.getUri().toString(), targetRawUri );
				}
				else
				{
					String targetRawUri = new File( destinationDirectory, content.getName() ).getPath();
					ReadFromSAFEntry( context, content.getUri().toString(), targetRawUri );
				}
			}
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	private static void CopyRawDirectoryRecursively( Context context, File sourceDirectory, String destinationRawUri, boolean isDestinationSAFDirectory )
	{
		File destinationDirectory = null;
		ArrayList<FileBrowserSAFEntry> destinationContents = null;
		if( isDestinationSAFDirectory )
			destinationContents = new FileBrowserSAFEntry( context, Uri.parse( destinationRawUri ) ).listFiles();
		else
		{
			destinationDirectory = new File( destinationRawUri );
			destinationDirectory.mkdirs();
		}

		File[] contents = sourceDirectory.listFiles();
		if( contents != null )
		{
			for( int i = 0; i < contents.length; i++ )
			{
				File content = contents[i];
				if( content.isDirectory() )
				{
					String targetRawUri;
					if( isDestinationSAFDirectory )
						targetRawUri = FindSAFEntryWithNameOrCreateNew( context, destinationRawUri, destinationContents, true, content.getName() );
					else
						targetRawUri = new File( destinationDirectory, content.getName() ).getPath();

					CopyRawDirectoryRecursively( context, content, targetRawUri, isDestinationSAFDirectory );
				}
				else
				{
					if( isDestinationSAFDirectory )
					{
						String targetRawUri = FindSAFEntryWithNameOrCreateNew( context, destinationRawUri, destinationContents, false, content.getName() );
						WriteToSAFEntry( context, targetRawUri, content.getPath(), false );
					}
					else
					{
						String targetRawUri = new File( destinationDirectory, content.getName() ).getPath();
						CopyRawFile( content.getPath(), targetRawUri );
					}
				}
			}
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	private static void DeleteRawDirectoryRecursively( File directory )
	{
		File[] contents = directory.listFiles();
		if( contents != null )
		{
			for( int i = 0; i < contents.length; i++ )
			{
				if( contents[i].isDirectory() )
					DeleteRawDirectoryRecursively( contents[i] );
				else
					contents[i].delete();
			}
		}

		directory.delete();
	}

	@TargetApi( Build.VERSION_CODES.Q )
	private static String FindSAFEntryWithNameOrCreateNew( Context context, String folderRawUri, ArrayList<FileBrowserSAFEntry> folderContents, boolean isDirectory, String entryName )
	{
		for( int i = 0; i < folderContents.size(); i++ )
		{
			FileBrowserSAFEntry entry = folderContents.get( i );
			if( entry.getName().equals( entryName ) )
			{
				if( entry.isDirectory() == isDirectory )
					return entry.getUri().toString();
				else
				{
					// SAF entry's type doesn't match the type we want, delete the entry
					entry.delete();
					break;
				}
			}
		}

		return CreateSAFEntry( context, folderRawUri, isDirectory, entryName );
	}
	//// END UTILITY FUNCTIONS
}