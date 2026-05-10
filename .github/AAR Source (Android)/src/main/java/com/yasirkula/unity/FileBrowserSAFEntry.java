package com.yasirkula.unity;


import android.annotation.TargetApi;
import android.content.ContentResolver;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.database.Cursor;
import android.net.Uri;
import android.os.Build;
import android.provider.DocumentsContract;
import android.text.TextUtils;
import android.util.Log;

import java.util.ArrayList;

/*
 * Copyright (C) 2014 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * Edited by yasirkula on 12.10.2019.
 */

// A Storage Access Framework (SAF) file/folder
@TargetApi( Build.VERSION_CODES.Q )
public class FileBrowserSAFEntry
{
	private static final String TAG = "DocumentFile";

	private Context mContext;
	private Uri mUri;

	public static FileBrowserSAFEntry fromTreeUri( Context context, Uri uri )
	{
		uri = DocumentsContract.buildDocumentUriUsingTree( uri, DocumentsContract.getTreeDocumentId( uri ) );
		if( uri == null )
			return null;

		return new FileBrowserSAFEntry( context, uri );
	}

	public FileBrowserSAFEntry( Context context, Uri uri )
	{
		mContext = context;
		mUri = uri;
	}

	public FileBrowserSAFEntry createFile( String mimeType, String displayName )
	{
		try
		{
			final Uri result = DocumentsContract.createDocument( mContext.getContentResolver(), mUri, mimeType, displayName );
			return ( result != null ) ? new FileBrowserSAFEntry( mContext, result ) : null;
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
			return null;
		}
	}

	public FileBrowserSAFEntry createDirectory( String displayName )
	{
		try
		{
			final Uri result = DocumentsContract.createDocument( mContext.getContentResolver(), mUri, DocumentsContract.Document.MIME_TYPE_DIR, displayName );
			return ( result != null ) ? new FileBrowserSAFEntry( mContext, result ) : null;
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
			return null;
		}
	}

	public Uri getUri()
	{
		return mUri;
	}

	public String getName()
	{
		return queryForString( DocumentsContract.Document.COLUMN_DISPLAY_NAME, null );
	}

	public String getType()
	{
		final String rawType = getRawType();
		if( DocumentsContract.Document.MIME_TYPE_DIR.equals( rawType ) )
		{
			return null;
		}
		else
		{
			return rawType;
		}
	}

	public boolean isDirectory()
	{
		return DocumentsContract.Document.MIME_TYPE_DIR.equals( getRawType() );
	}

	public boolean isFile()
	{
		final String type = getRawType();
		if( DocumentsContract.Document.MIME_TYPE_DIR.equals( type ) || TextUtils.isEmpty( type ) )
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	public long lastModified()
	{
		return queryForLong( DocumentsContract.Document.COLUMN_LAST_MODIFIED, 0 );
	}

	public long length()
	{
		return queryForLong( DocumentsContract.Document.COLUMN_SIZE, 0 );
	}

	public boolean canRead()
	{
		// Ignore if grant doesn't allow read
		if( mContext.checkCallingOrSelfUriPermission( mUri, Intent.FLAG_GRANT_READ_URI_PERMISSION )
				!= PackageManager.PERMISSION_GRANTED )
		{
			return false;
		}
		// Ignore documents without MIME
		if( TextUtils.isEmpty( getRawType() ) )
		{
			return false;
		}
		return true;
	}

	public boolean canWrite()
	{
		// Ignore if grant doesn't allow write
		if( mContext.checkCallingOrSelfUriPermission( mUri, Intent.FLAG_GRANT_WRITE_URI_PERMISSION )
				!= PackageManager.PERMISSION_GRANTED )
		{
			return false;
		}
		final String type = getRawType();
		final int flags = queryForInt( DocumentsContract.Document.COLUMN_FLAGS, 0 );
		// Ignore documents without MIME
		if( TextUtils.isEmpty( type ) )
		{
			return false;
		}
		// Deletable documents considered writable
		if( ( flags & DocumentsContract.Document.FLAG_SUPPORTS_DELETE ) != 0 )
		{
			return true;
		}
		if( DocumentsContract.Document.MIME_TYPE_DIR.equals( type )
				&& ( flags & DocumentsContract.Document.FLAG_DIR_SUPPORTS_CREATE ) != 0 )
		{
			// Directories that allow create considered writable
			return true;
		}
		else if( !TextUtils.isEmpty( type )
				&& ( flags & DocumentsContract.Document.FLAG_SUPPORTS_WRITE ) != 0 )
		{
			// Writable normal files considered writable
			return true;
		}
		return false;
	}

	public boolean delete()
	{
		try
		{
			return DocumentsContract.deleteDocument( mContext.getContentResolver(), mUri );
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}

		return false;
	}

	public boolean exists()
	{
		final ContentResolver resolver = mContext.getContentResolver();
		Cursor c = null;
		try
		{
			c = resolver.query( mUri, new String[] {
					DocumentsContract.Document.COLUMN_DOCUMENT_ID }, null, null, null );
			return c.getCount() > 0;
		}
		catch( Exception e )
		{
			Log.w( TAG, "Failed query: " + e );
			return false;
		}
		finally
		{
			try
			{
				if( c != null )
					c.close();
			}
			catch( Exception e )
			{
				Log.e( TAG, "Exception:", e );
			}
		}
	}

	public ArrayList<FileBrowserSAFEntry> listFiles()
	{
		final ContentResolver resolver = mContext.getContentResolver();
		final Uri childrenUri = DocumentsContract.buildChildDocumentsUriUsingTree( mUri,
				DocumentsContract.getDocumentId( mUri ) );
		final ArrayList<FileBrowserSAFEntry> results = new ArrayList<FileBrowserSAFEntry>();
		Cursor c = null;
		try
		{
			c = resolver.query( childrenUri, new String[] {
					DocumentsContract.Document.COLUMN_DOCUMENT_ID }, null, null, null );
			while( c.moveToNext() )
			{
				final String documentId = c.getString( 0 );
				final Uri documentUri = DocumentsContract.buildDocumentUriUsingTree( mUri,
						documentId );
				results.add( new FileBrowserSAFEntry( mContext, documentUri ) );
			}
		}
		catch( Exception e )
		{
			Log.w( "Unity", "Failed query: " + e );
		}
		finally
		{
			try
			{
				if( c != null )
					c.close();
			}
			catch( Exception e )
			{
				Log.e( TAG, "Exception:", e );
			}
		}

		return results;
	}

	public void appendFilesToStringBuilder( StringBuilder stringBuilder )
	{
		final ContentResolver resolver = mContext.getContentResolver();
		final Uri childrenUri = DocumentsContract.buildChildDocumentsUriUsingTree( mUri, DocumentsContract.getDocumentId( mUri ) );
		Cursor c = null;
		try
		{
			c = resolver.query( childrenUri, new String[] { DocumentsContract.Document.COLUMN_DOCUMENT_ID, DocumentsContract.Document.COLUMN_MIME_TYPE, DocumentsContract.Document.COLUMN_DISPLAY_NAME }, null, null, null );
			stringBuilder.append( c.getCount() ).append( "<>" );
			if( c.moveToNext() )
			{
				int documentIdIndex = c.getColumnIndex( DocumentsContract.Document.COLUMN_DOCUMENT_ID );
				int mimeTypeIndex = c.getColumnIndex( DocumentsContract.Document.COLUMN_MIME_TYPE );
				int nameIndex = c.getColumnIndex( DocumentsContract.Document.COLUMN_DISPLAY_NAME );

				do
				{
					final boolean isDirectory = DocumentsContract.Document.MIME_TYPE_DIR.equals( c.getString( mimeTypeIndex ) );
					final String name = c.getString( nameIndex );
					final String uri = DocumentsContract.buildDocumentUriUsingTree( mUri, c.getString( documentIdIndex ) ).toString();

					stringBuilder.append( isDirectory ? "d" : "f" ).append( name ).append( "<>" ).append( uri ).append( "<>" );
				} while( c.moveToNext() );
			}
		}
		catch( Exception e )
		{
			Log.w( "Unity", "Failed query: " + e );
		}
		finally
		{
			try
			{
				if( c != null )
					c.close();
			}
			catch( Exception e )
			{
				Log.e( TAG, "Exception:", e );
			}
		}
	}

	public String renameTo( String displayName )
	{
		try
		{
			final Uri result = DocumentsContract.renameDocument( mContext.getContentResolver(), mUri, displayName );
			if( result != null )
				mUri = result;
		}
		catch( Exception e )
		{
			Log.e( "Unity", "Exception:", e );
		}

		return mUri.toString();
	}

	private String getRawType()
	{
		return queryForString( DocumentsContract.Document.COLUMN_MIME_TYPE, null );
	}

	private String queryForString( String column, String defaultValue )
	{
		final ContentResolver resolver = mContext.getContentResolver();
		Cursor c = null;
		try
		{
			c = resolver.query( mUri, new String[] { column }, null, null, null );
			if( c.moveToFirst() && !c.isNull( 0 ) )
				return c.getString( 0 );

			return defaultValue;
		}
		catch( Exception e )
		{
			Log.w( TAG, "Failed query: " + e );
			return defaultValue;
		}
		finally
		{
			try
			{
				if( c != null )
					c.close();
			}
			catch( Exception e )
			{
				Log.e( TAG, "Exception:", e );
			}
		}
	}

	private int queryForInt( String column, int defaultValue )
	{
		return (int) queryForLong( column, defaultValue );
	}

	private long queryForLong( String column, long defaultValue )
	{
		final ContentResolver resolver = mContext.getContentResolver();
		Cursor c = null;
		try
		{
			c = resolver.query( mUri, new String[] { column }, null, null, null );
			if( c.moveToFirst() && !c.isNull( 0 ) )
				return c.getLong( 0 );

			return defaultValue;
		}
		catch( Exception e )
		{
			Log.w( TAG, "Failed query: " + e );
			return defaultValue;
		}
		finally
		{
			try
			{
				if( c != null )
					c.close();
			}
			catch( Exception e )
			{
				Log.e( TAG, "Exception:", e );
			}
		}
	}
}