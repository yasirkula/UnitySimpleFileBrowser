package com.yasirkula.unity;

import android.os.Environment;
import java.io.File;

/**
 * Created by yasirkula on 30.10.2017.
 */

public class ExternalDrivesList
{
	public static String GetExternalDrives()
	{
		String result = "";

		File primary = Environment.getExternalStorageDirectory();
		if( primary != null )
			result += primary.getAbsolutePath() + ":";

		// Credit: https://stackoverflow.com/a/32088396/2373034
		String strSDCardPath = System.getenv( "SECONDARY_STORAGE" );
		if( strSDCardPath == null || strSDCardPath.length() == 0 )
			strSDCardPath = System.getenv( "EXTERNAL_SDCARD_STORAGE" );

		if( strSDCardPath != null && strSDCardPath.length() > 0 )
		{
			if( !strSDCardPath.contains( ":" ) )
				strSDCardPath += ":";

			String[] externalPaths = strSDCardPath.split( ":" );
			for( int i = 0; i < externalPaths.length; i++ )
			{
				String path = externalPaths[i];
				if( path != null && path.length() > 0 )
				{
					File externalFile = new File( path );
					if( externalFile.exists() )
						result += externalFile.getAbsolutePath() + ":";
				}
			}
		}

		return result;
	}
}
