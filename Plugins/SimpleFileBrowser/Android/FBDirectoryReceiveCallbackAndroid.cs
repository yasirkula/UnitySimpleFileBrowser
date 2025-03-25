#if UNITY_EDITOR || UNITY_ANDROID
using UnityEngine;

namespace SimpleFileBrowser
{
	public class FBDirectoryReceiveCallbackAndroid : AndroidJavaProxy
	{
		private readonly FileBrowser.AndroidSAFDirectoryPickCallback callback;
		private readonly FBCallbackHelper callbackHelper;

		public FBDirectoryReceiveCallbackAndroid( FileBrowser.AndroidSAFDirectoryPickCallback callback ) : base( "com.yasirkula.unity.FileBrowserDirectoryReceiver" )
		{
			this.callback = callback;
			callbackHelper = FBCallbackHelper.Create( true );
		}

		[UnityEngine.Scripting.Preserve]
		public void OnDirectoryPicked( string rawUri, string name )
		{
			callbackHelper.CallOnMainThread( () => callback( rawUri, name ) );
		}
	}
}
#endif