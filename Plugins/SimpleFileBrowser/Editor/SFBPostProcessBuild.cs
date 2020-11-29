using System.IO;
using UnityEditor;
using UnityEngine;

public class SFBPostProcessBuild
{
	[InitializeOnLoadMethod]
	public static void ValidatePlugin()
	{
		string jarPath = "Assets/Plugins/SimpleFileBrowser/Android/SimpleFileBrowser.jar";
		if( File.Exists( jarPath ) )
		{
			Debug.Log( "Deleting obsolete " + jarPath );
			AssetDatabase.DeleteAsset( jarPath );
		}
	}
}