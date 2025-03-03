﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if(UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif

namespace Agora_RTC_Plugin.API_Example
{
    public class PermissionHelper
    {
        public static void RequestMicrophonePermission()
        {
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
		if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
		{
			Permission.RequestUserPermission(Permission.Microphone);
		}
#endif
        }

        public static void RequestCameraPermission()
        {
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
		if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
		{
			Permission.RequestUserPermission(Permission.Camera);
		}
#endif
        }
    }
}
