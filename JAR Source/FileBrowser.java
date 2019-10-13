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
import android.util.Log;
import android.webkit.MimeTypeMap;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.Comparator;
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

	public static String GetExternalDrives()
	{
		File primary = Environment.getExternalStorageDirectory();
		String primaryPath = primary.getAbsolutePath();

		stringBuilder.setLength( 0 );
		stringBuilder.append( primaryPath ).append( ":" );

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
				{
					File file = new File( path );
					if( file.exists() && file.isDirectory() && file.canRead() && !file.getAbsolutePath().equalsIgnoreCase( primaryPath ) )
					{
						String absolutePath = file.getAbsolutePath() + File.separator + "Android";
						if( new File( absolutePath ).exists() )
						{
							try
							{
								// Check if two paths lead to same storage (aliases)
								if( !primary.getCanonicalPath().equals( file.getCanonicalPath() ) )
									stringBuilder.append( file.getAbsolutePath() ).append( ":" );
							}
							catch( Exception e )
							{
							}
						}
					}
				}
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
					if( file.exists() && file.isDirectory() && file.canRead() && !file.getAbsolutePath().equalsIgnoreCase( primaryPath ) )
					{
						String absolutePath = file.getAbsolutePath() + File.separator + "Android";
						if( new File( absolutePath ).exists() )
						{
							try
							{
								// Check if two paths lead to same storage (aliases)
								if( !primary.getCanonicalPath().equals( file.getCanonicalPath() ) )
									stringBuilder.append( file.getAbsolutePath() ).append( ":" );
							}
							catch( Exception ex )
							{
							}
						}
					}
				}
			}
			catch( Exception e )
			{
			}
		}

		return stringBuilder.toString();
	}

	@TargetApi( Build.VERSION_CODES.M )
	public static int CheckPermission( Context context )
	{
		if( Build.VERSION.SDK_INT < Build.VERSION_CODES.M )
			return 1;

		if( context.checkSelfPermission( Manifest.permission.WRITE_EXTERNAL_STORAGE ) == PackageManager.PERMISSION_GRANTED &&
				context.checkSelfPermission( Manifest.permission.READ_EXTERNAL_STORAGE ) == PackageManager.PERMISSION_GRANTED )
			return 1;

		return 0;
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
		( (Activity) context ).getFragmentManager().beginTransaction().add( 0, request ).commit();
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
		( (Activity) context ).getFragmentManager().beginTransaction().add( 0, request ).commit();
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

	// Fetches the contents of a Storage Access Framework (SAF) folder
	@TargetApi( Build.VERSION_CODES.Q )
	public static String OpenSAFFolder( Context context, String rawUri )
	{
		FileBrowserSAFEntry directory = new FileBrowserSAFEntry( context, Uri.parse( rawUri ) );
		ArrayList<FileBrowserSAFEntry> entries = directory.listFiles();

		stringBuilder.setLength( 0 );
		stringBuilder.append( entries.size() ).append( "<>" );

		for( int i = 0; i < entries.size(); i++ )
		{
			FileBrowserSAFEntry entry = entries.get( i );
			stringBuilder.append( entry.isDirectory() ? "d" : "f" ).append( entry.getName() ).append( "<>" ).append( entry.getUri().toString() ).append( "<>" );
		}

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
			InputStream in = new FileInputStream( new File( sourceFile ) );
			try
			{
				OutputStream out = context.getContentResolver().openOutputStream( Uri.parse( rawUri ), appendMode ? "wa" : "rwt" );
				try
				{
					byte[] buf = new byte[1024];
					int len;
					while( ( len = in.read( buf ) ) > 0 )
						out.write( buf, 0, len );
				}
				finally
				{
					out.close();
				}
			}
			finally
			{
				in.close();
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

			OutputStream output = null;
			try
			{
				output = new FileOutputStream( new File( destinationFile ), false );

				byte[] buf = new byte[4096];
				int len;
				while( ( len = input.read( buf ) ) > 0 )
					output.write( buf, 0, len );
			}
			finally
			{
				if( output != null )
					output.close();

				input.close();
			}
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}
	}

	@TargetApi( Build.VERSION_CODES.Q )
	public static boolean SAFEntryExists( Context context, String rawUri )
	{
		return new FileBrowserSAFEntry( context, Uri.parse( rawUri ) ).exists();
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
}