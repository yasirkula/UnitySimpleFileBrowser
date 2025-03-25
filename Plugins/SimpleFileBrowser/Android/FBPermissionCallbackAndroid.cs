#if UNITY_EDITOR || UNITY_ANDROID
using UnityEngine;

namespace SimpleFileBrowser
{
	public class FBPermissionCallbackAndroid : AndroidJavaProxy
	{
		private readonly FileBrowser.PermissionCallback callback;
		private readonly FBCallbackHelper callbackHelper;

		public FBPermissionCallbackAndroid( FileBrowser.PermissionCallback callback ) : base( "com.yasirkula.unity.FileBrowserPermissionReceiver" )
		{
			this.callback = callback;
			callbackHelper = FBCallbackHelper.Create( true );
		}

		[UnityEngine.Scripting.Preserve]
		public void OnPermissionResult( int result )
		{
			callbackHelper.CallOnMainThread( () => callback( (FileBrowser.Permission) result ) );
		}
	}
}
#endif