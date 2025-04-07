#if UNITY_EDITOR || UNITY_ANDROID
using System;
using UnityEngine;

namespace SimpleFileBrowser
{
	public class FBCallbackHelper : MonoBehaviour
	{
		private bool autoDestroyWithCallback;
		private Action mainThreadAction = null;

		public static FBCallbackHelper Create( bool autoDestroyWithCallback )
		{
			FBCallbackHelper result = new GameObject( "FBCallbackHelper" ).AddComponent<FBCallbackHelper>();
			result.autoDestroyWithCallback = autoDestroyWithCallback;
			DontDestroyOnLoad( result.gameObject );
			return result;
		}

		public void CallOnMainThread( Action function )
		{
			lock( this )
			{
				mainThreadAction += function;
			}
		}

		private void Update()
		{
			if( mainThreadAction != null )
			{
				try
				{
					Action temp;
					lock( this )
					{
						temp = mainThreadAction;
						mainThreadAction = null;
					}

					temp();
				}
				finally
				{
					if( autoDestroyWithCallback )
						Destroy( gameObject );
				}
			}
		}
	}
}
#endif