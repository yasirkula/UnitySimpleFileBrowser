package com.yasirkula.unity;

import android.Manifest;
import android.annotation.TargetApi;
import android.app.Activity;
import android.app.Fragment;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Environment;

import java.io.File;

/**
 * Created by yasirkula on 30.10.2017.
 */

public class FileBrowser
{
	public static String GetExternalDrives()
	{
		File primary = Environment.getExternalStorageDirectory();
		String primaryPath = primary.getAbsolutePath();

		String result = primaryPath + ":";

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
									result += file.getAbsolutePath() + ":";
							}
							catch( Exception e ) { }
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
				File fileList[] = new File( root ).listFiles();
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
									result += file.getAbsolutePath() + ":";
							}
							catch( Exception ex ) { }
						}
					}
				}
			}
			catch( Exception e ) { }
		}

		return result;
	}

	public static int CheckPermission( Context context )
	{
		if( Build.VERSION.SDK_INT < Build.VERSION_CODES.M )
			return 1;

		return CheckPermissionInternal( context );
	}

	// Credit: https://github.com/Over17/UnityAndroidPermissions/blob/0dca33e40628f1f279decb67d901fd444b409cd7/src/UnityAndroidPermissions/src/main/java/com/unity3d/plugin/UnityAndroidPermissions.java
	@TargetApi( Build.VERSION_CODES.M )
	private static int CheckPermissionInternal( Context context )
	{
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
}
