using System.IO;
using UnityEditor;
using UnityEngine;

namespace SimpleFileBrowser
{
	public class FBPostProcessBuild
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
}