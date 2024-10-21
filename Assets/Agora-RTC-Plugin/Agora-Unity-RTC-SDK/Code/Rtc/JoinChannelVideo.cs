using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Agora.Rtc;

#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif

public class JoinChannelVideo : MonoBehaviour
{
    // Fill in your app ID
    private string _appID= "569637ea0042450ba2151ceca12d46a6";
    // Fill in your channel name
    private string _channelName = "main";
    // Fill in a temporary token
    private string _token = "007eJxTYJia4W6eZf0vzMV76vmkk/6sfzwTrFNFDOd5sdt4vRZK4VJgMDWzNDM2T000MDAxMjE1SEo0MjQ1TE5NTjQ0SjExSzSzKRdObwhkZDhpUsvKyACBID4LQ25iZh4DAwAYxBrX";
    internal VideoSurface LocalView;
    internal VideoSurface RemoteView;
    internal IRtcEngine RtcEngine;

    #if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
    private ArrayList permissionList = new ArrayList() { Permission.Camera, Permission.Microphone };
    #endif

    // Start is called before the first frame update
    void Start()
    {
        SetupVideoSDKEngine();
        InitEventHandler();
        SetupUI();
        PreviewSelf();
    }

    // Update is called once per frame
    void Update()
    {
        CheckPermissions();
    }

    private void SetupVideoSDKEngine()
    {
        // Create an IRtcEngine instance
        RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
        RtcEngineContext context = new RtcEngineContext();
        context.appId = _appID;
        context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
        context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT;
        // Initialize the instance
        RtcEngine.Initialize(context);
    }

    private void SetupUI()
    {
        GameObject go = GameObject.Find("LocalView");
        LocalView = go.AddComponent<VideoSurface>();
        go.transform.Rotate(0.0f, 0.0f, -180.0f);

        go = GameObject.Find("RemoteView");
        RemoteView = go.AddComponent<VideoSurface>();
        go.transform.Rotate(0.0f, 0.0f, -180.0f);

        go = GameObject.Find("LeaveButton");
        go.GetComponent<Button>().onClick.AddListener(Leave);

        go = GameObject.Find("JoinButton");
        go.GetComponent<Button>().onClick.AddListener(Join);
    }

    private void PreviewSelf()
    {
        // Enable the video module
        RtcEngine.EnableVideo();
        // Enable local video preview
        RtcEngine.StartPreview();
        // Set up local video display
        LocalView.SetForUser(0, "");
        // Render the video
        LocalView.SetEnable(true);
    }

    public void Join()
    {
        // Set channel media options
        ChannelMediaOptions options = new ChannelMediaOptions();
        // Publish the audio stream collected from the microphone
        options.publishMicrophoneTrack.SetValue(true);
        // Publish the video stream collected from the camera
        options.publishCameraTrack.SetValue(true);
        // Automatically subscribe to all audio streams
        options.autoSubscribeAudio.SetValue(true);
        // Automatically subscribe to all video streams
        options.autoSubscribeVideo.SetValue(true);
        // Set the channel profile to live broadcasting
        options.channelProfile.SetValue(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        // Set the user role to broadcaster
        options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        // Join the channel
        RtcEngine.JoinChannel(_token, _channelName, 0, options);
    }

    public void Leave()
    {
        Debug.Log("Leaving " + _channelName);
        // Leave the channel
        RtcEngine.LeaveChannel();
        // Disable the video module
        RtcEngine.DisableVideo();
        // Stop remote video rendering0
        RemoteView.SetEnable(false);
        // Stop local video rendering
        LocalView.SetEnable(false);
    }

    void OnApplicationQuit()
    {
        if (RtcEngine != null) {
            Leave();
            // Destroy IRtcEngine
            RtcEngine.Dispose();
            RtcEngine = null;
        }
    }

    private void CheckPermissions()
    {
    #if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
            foreach (string permission in permissionList)
            {
                if (!Permission.HasUserAuthorizedPermission(permission))
                {
                    Permission.RequestUserPermission(permission);
                }
            }
    #endif
    }

    // Create an instance of the user callback class and set the callback
    private void InitEventHandler()
    {
        UserEventHandler handler = new UserEventHandler(this);
        RtcEngine.InitEventHandler(handler);
    }

    // Implement your own callback class by inheriting from the IRtcEngineEventHandler interface
    internal class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly JoinChannelVideo _videoSample;
        internal UserEventHandler(JoinChannelVideo videoSample)
        {
            _videoSample = videoSample;
        }

        // This callback is triggered when the local user successfully joins the channel
        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            // TODO
        }

        // The OnUserJoined callback is triggered when the SDK receives and successfully decodes the first frame of remote video
        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            // Set the display for the remote video
            _videoSample.RemoteView.SetForUser(uid, connection.channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            // Start video rendering
            _videoSample.RemoteView.SetEnable(true);
            Debug.Log("Remote user joined");
        }

        // This callback is triggered when the remote user leaves the current channel
        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _videoSample.RemoteView.SetEnable(false);
        }
    }
}
