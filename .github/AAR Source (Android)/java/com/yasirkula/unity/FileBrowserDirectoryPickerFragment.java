package com.yasirkula.unity;

import android.annotation.TargetApi;
import android.app.Activity;
import android.app.Fragment;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.DocumentsContract;
import android.widget.Toast;

@TargetApi( Build.VERSION_CODES.Q )
public class FileBrowserDirectoryPickerFragment extends Fragment
{
	private static final int DIRECTORY_PICK_REQUEST_CODE = 74425;

	private final FileBrowserDirectoryReceiver directoryReceiver;

	public FileBrowserDirectoryPickerFragment()
	{
		directoryReceiver = null;
	}

	public FileBrowserDirectoryPickerFragment( final FileBrowserDirectoryReceiver directoryReceiver )
	{
		this.directoryReceiver = directoryReceiver;
	}

	@Override
	public void onCreate( Bundle savedInstanceState )
	{
		super.onCreate( savedInstanceState );

		if( directoryReceiver == null )
			getFragmentManager().beginTransaction().remove( this ).commit();
		else
		{
			Intent intent = new Intent( Intent.ACTION_OPEN_DOCUMENT_TREE );
			intent.addFlags( Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION | Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION );

			// Try to set the initial folder of the picker as sdcard root
			intent.putExtra( "android.content.extra.SHOW_ADVANCED", true );
			intent.putExtra( "android.content.extra.FANCY", true );
			intent.putExtra( "android.content.extra.SHOW_FILESIZE", true );
			intent.putExtra( Intent.EXTRA_LOCAL_ONLY, true );

			try
			{
				startActivityForResult( intent, DIRECTORY_PICK_REQUEST_CODE );
			}
			catch( ActivityNotFoundException e )
			{
				Toast.makeText( getActivity(), "No apps can perform this action.", Toast.LENGTH_LONG ).show();
				onActivityResult( DIRECTORY_PICK_REQUEST_CODE, Activity.RESULT_CANCELED, null );
			}
		}
	}

	@Override
	public void onActivityResult( int requestCode, int resultCode, Intent data )
	{
		if( requestCode != DIRECTORY_PICK_REQUEST_CODE )
			return;

		String rawUri = "";
		String name = "";
		if( resultCode == Activity.RESULT_OK && data != null )
		{
			Uri directoryUri = data.getData();
			if( directoryUri != null )
			{
				FileBrowserSAFEntry directory = FileBrowserSAFEntry.fromTreeUri( getActivity(), directoryUri );
				if( directory != null && directory.exists() )
				{
					rawUri = directory.getUri().toString();
					name = directory.getName();

					getActivity().getContentResolver().takePersistableUriPermission( data.getData(), Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION );
				}
			}
		}

		if( directoryReceiver != null )
			directoryReceiver.OnDirectoryPicked( rawUri, name );

		getFragmentManager().beginTransaction().remove( this ).commit();
	}
}