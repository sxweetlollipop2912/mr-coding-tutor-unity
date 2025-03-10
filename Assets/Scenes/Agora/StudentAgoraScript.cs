using System.Collections;
using Agora.Rtc;
using io.agora.rtc.demo;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShareWhileVideoCall
{
    namespace StudentMrCodingTutorUnity
    {
        public class StudentAgoraScript : MonoBehaviour
        {
            [Header("_____________Basic Configuration_____________")]
            [FormerlySerializedAs("APP_ID")]
            [SerializeField]
            private string _appID = "";

            [FormerlySerializedAs("TOKEN")]
            [SerializeField]
            private string _token = "";

            [FormerlySerializedAs("CHANNEL_NAME")]
            [SerializeField]
            private string _channelName = "";

            [SerializeField]
            public Transform redDot;

            [SerializeField]
            private bool useExternalConfig = true;

            [Header("_____________Avatar Video Configuration_____________")]
            [SerializeField]
            private Camera avatarCamera;

            [SerializeField]
            private GameObject avatarObject;

            [SerializeField]
            private bool sendAvatarVideo = true;

            [SerializeField]
            private RenderTexture avatarRenderTexture;

            [SerializeField]
            private int avatarVideoWidth = 640;

            [SerializeField]
            private int avatarVideoHeight = 480;

            private Texture2D _avatarTexture2D;
            private bool _isCapturingAvatar = false;
            public uint UidAvatarStream = 789; // Unique ID for avatar video stream
            private int _customVideoTrackId = -1;

            internal IRtcEngineEx RtcEngine = null;

            public uint UidStudentWebcam = 123;
            public uint UidStudentDesktop = 456;
            public static uint UidTeacherWebcam = 321;
            private static bool _isTeacherWebcamActive = false;

            // Use this for initialization
            private void Start()
            {
                LoadAssetData();
                InitEngine();
                EnableUI();
                JoinChannel();

                // Add a delay before starting avatar video to ensure main connection is established
                if (sendAvatarVideo)
                {
                    Debug.Log("Will set up avatar video capture after 2 seconds");
                    Invoke("SetupAvatarVideoCapture", 2.0f);
                }
            }

            //Show data in AgoraBasicProfile
            [ContextMenu("ShowAgoraBasicProfileData")]
            private void LoadAssetData()
            {
                if (useExternalConfig)
                {
                    LoadConfigFromConfigLoader();
                }
                else
                {
                    // When external config is disabled, use empty values
                    // These would need to be set in the inspector
                    Debug.LogWarning("External config disabled. Using values from inspector.");
                }

                // The appID is now directly set in the inspector
                Debug.Log("Using AppID set directly in the component");

                // Validate all parameters
                ValidateParameters();
            }

            private void LoadConfigFromConfigLoader()
            {
                var config = ConfigLoader.Instance?.ConfigData;
                if (config == null)
                {
                    Debug.LogError(
                        "[StudentAgoraScript] ConfigLoader instance or configuration data is not available."
                    );
                    return;
                }

                _token = config.agoraToken;
                _channelName = config.agoraChannelName;

                Debug.Log("Loaded token and channel from ConfigLoader");
            }

            /// <summary>
            /// Validates that all required parameters are present and not empty.
            /// </summary>
            private void ValidateParameters()
            {
                bool hasError = false;

                if (string.IsNullOrEmpty(_appID))
                {
                    Debug.LogError("AppID is missing or empty. Please set it in the inspector.");
                    hasError = true;
                }

                if (string.IsNullOrEmpty(_token))
                {
                    Debug.LogError(
                        "Token is missing or empty. Please check external configuration or inspector."
                    );
                    hasError = true;
                }

                if (string.IsNullOrEmpty(_channelName))
                {
                    Debug.LogError(
                        "Channel name is missing or empty. Please check external configuration or inspector."
                    );
                    hasError = true;
                }

                if (hasError)
                {
                    Debug.LogError(
                        "Some Agora configuration parameters are missing. The application may not function correctly."
                    );
                }
                else
                {
                    Debug.Log("All Agora configuration parameters are valid.");
                }
            }

            private void JoinChannel()
            {
                RtcEngine.EnableAudio();
                RtcEngine.EnableVideo();
                RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

                ChannelMediaOptions options = new ChannelMediaOptions();
                options.autoSubscribeAudio.SetValue(true);
                options.autoSubscribeVideo.SetValue(true);

                options.publishCameraTrack.SetValue(true);
                options.publishScreenTrack.SetValue(false);
                options.enableAudioRecordingOrPlayout.SetValue(true);
                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
                RtcEngine.JoinChannel(_token, _channelName, this.UidStudentWebcam, options);
                RtcEngine.MuteRemoteAudioStream(UidStudentDesktop, true);
                RtcEngine.MuteRemoteVideoStream(UidStudentDesktop, true);
            }

            private void ScreenShareJoinChannel()
            {
                int ret = 0;
                ChannelMediaOptions options = new ChannelMediaOptions();
                options.autoSubscribeAudio.SetValue(false);
                options.autoSubscribeVideo.SetValue(false);
                options.publishCameraTrack.SetValue(false);
                options.publishScreenTrack.SetValue(true);
                options.enableAudioRecordingOrPlayout.SetValue(false);
#if UNITY_ANDROID || UNITY_IPHONE
                options.publishScreenCaptureAudio.SetValue(true);
                options.publishScreenCaptureVideo.SetValue(true);
#endif

#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                //If you want to share audio when sharing the desktop screen, you need to use this interface.
                //For details, please refer to the annotation of this interface
                //ret = RtcEngine.EnableLoopbackRecordingEx(new RtcConnection(_channelName, this.Uid2), true, "");
                //Debug.Log("EnableLoopbackRecording returns: " + ret);
#endif
                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
                ret = RtcEngine.JoinChannelEx(
                    _token,
                    new RtcConnection(_channelName, this.UidStudentDesktop),
                    options
                );
                Debug.Log("JoinChannelEx returns: " + ret);
            }

            private void ScreenShareLeaveChannel()
            {
                RtcEngine.LeaveChannelEx(new RtcConnection(_channelName, UidStudentDesktop));
            }

            private void InitEngine()
            {
                RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngineEx();
                UserEventHandler handler = new UserEventHandler(this);
                RtcEngineContext context = new RtcEngineContext();
                context.appId = _appID;
                context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
                context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT;
                context.areaCode = AREA_CODE.AREA_CODE_GLOB;
                RtcEngine.Initialize(context);
                RtcEngine.InitEventHandler(handler);
            }

            private void EnableUI()
            {
                OnStartShareScreen();
            }

            private void OnStartShareScreen()
            {
                if (RtcEngine == null)
                    return;

#if UNITY_ANDROID || UNITY_IPHONE
                var parameters2 = new ScreenCaptureParameters2();
                parameters2.captureAudio = true;
                parameters2.captureVideo = true;
                var nRet = RtcEngine.StartScreenCapture(parameters2);
                Debug.Log("StartScreenCapture :" + nRet);
#else
                RtcEngine.StopScreenCapture();

                SIZE t = new SIZE();
                t.width = 360;
                t.height = 240;
                SIZE s = new SIZE();
                s.width = 360;
                s.height = 240;
                var info = RtcEngine.GetScreenCaptureSources(t, s, true);

                var dispId = (uint)info[0].sourceId;
                Debug.Log(string.Format(">>>>> Start sharing display {0}", dispId));
                var nRet = RtcEngine.StartScreenCaptureByDisplayId(
                    dispId,
                    default(Rectangle),
                    new ScreenCaptureParameters { captureMouseCursor = true, frameRate = 30 }
                );
                Debug.Log("StartScreenCaptureByDisplayId:" + nRet);
#endif

                ScreenShareJoinChannel();
            }

            private void OnStopShareScreen()
            {
                ScreenShareLeaveChannel();
                RtcEngine.StopScreenCapture();
            }

            #region -- Avatar Video Capture

            private void SetupAvatarVideoCapture()
            {
                if (avatarCamera == null)
                {
                    Debug.LogError("Avatar camera not assigned. Cannot capture avatar video.");
                    return;
                }

                if (avatarObject == null)
                {
                    Debug.LogError("Avatar object not assigned. Cannot capture avatar video.");
                    return;
                }

                Debug.Log("Setting up avatar video capture with UID: " + UidAvatarStream);

                // Create render texture if not assigned
                if (avatarRenderTexture == null)
                {
                    avatarRenderTexture = new RenderTexture(
                        avatarVideoWidth,
                        avatarVideoHeight,
                        24
                    );
                    avatarRenderTexture.Create();
                    Debug.Log(
                        "Created new render texture for avatar: "
                            + avatarVideoWidth
                            + "x"
                            + avatarVideoHeight
                    );
                }

                // Assign render texture to camera
                avatarCamera.targetTexture = avatarRenderTexture;
                Debug.Log("Assigned render texture to avatar camera");

                // Create texture for reading pixels
                _avatarTexture2D = new Texture2D(
                    avatarVideoWidth,
                    avatarVideoHeight,
                    TextureFormat.RGBA32,
                    false
                );
                Debug.Log("Created texture for reading pixels");

                // Setup external video source
                VideoEncoderConfiguration encoderConfig = new VideoEncoderConfiguration();
                encoderConfig.dimensions = new VideoDimensions(avatarVideoWidth, avatarVideoHeight);
                encoderConfig.frameRate = (int)FRAME_RATE.FRAME_RATE_FPS_30;
                encoderConfig.bitrate = 1000;
                RtcEngine.SetVideoEncoderConfiguration(encoderConfig);
                Debug.Log("Set video encoder configuration");

                // Enable custom video source
                SenderOptions senderOptions = new SenderOptions();
                int result = RtcEngine.SetExternalVideoSource(
                    true,
                    false,
                    EXTERNAL_VIDEO_SOURCE_TYPE.VIDEO_FRAME,
                    senderOptions
                );
                Debug.Log("Set external video source result: " + result);

                // Create a custom video track
                _customVideoTrackId = (int)RtcEngine.CreateCustomVideoTrack();
                Debug.Log("Created custom video track ID: " + _customVideoTrackId);

                // Start capturing
                _isCapturingAvatar = true;

                // Join channel with avatar video
                AvatarVideoJoinChannel();

                // Start a coroutine to check if frames are being pushed
                StartCoroutine(CheckFramePushing());
            }

            private void SetLayerRecursively(GameObject obj, int newLayer)
            {
                if (obj == null)
                    return;

                obj.layer = newLayer;

                foreach (Transform child in obj.transform)
                {
                    if (child == null)
                        continue;
                    SetLayerRecursively(child.gameObject, newLayer);
                }
            }

            private System.Collections.IEnumerator CheckFramePushing()
            {
                int frameCount = 0;
                int successCount = 0;

                // Check for 5 seconds
                for (int i = 0; i < 50; i++)
                {
                    yield return new WaitForSeconds(0.1f);

                    if (_isCapturingAvatar)
                    {
                        frameCount++;

                        // Capture and push a frame
                        if (CaptureAndPushFrame())
                        {
                            successCount++;
                        }
                    }
                }

                Debug.Log(
                    $"Frame pushing check complete: {successCount}/{frameCount} frames successfully pushed"
                );
            }

            private bool CaptureAndPushFrame()
            {
                if (
                    !_isCapturingAvatar
                    || avatarCamera == null
                    || avatarRenderTexture == null
                    || _avatarTexture2D == null
                )
                    return false;

                // Make sure we're rendering to the texture
                avatarCamera.Render();

                // Read pixels from render texture
                RenderTexture.active = avatarRenderTexture;
                _avatarTexture2D.ReadPixels(
                    new Rect(0, 0, avatarVideoWidth, avatarVideoHeight),
                    0,
                    0
                );
                _avatarTexture2D.Apply();
                RenderTexture.active = null;

                // Send frame to Agora
                byte[] bytes = _avatarTexture2D.GetRawTextureData();

                ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame();
                externalVideoFrame.type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA;
                externalVideoFrame.format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA;
                externalVideoFrame.buffer = bytes;
                externalVideoFrame.stride = avatarVideoWidth;
                externalVideoFrame.height = avatarVideoHeight;
                externalVideoFrame.cropLeft = 0;
                externalVideoFrame.cropTop = 0;
                externalVideoFrame.cropRight = 0;
                externalVideoFrame.cropBottom = 0;
                externalVideoFrame.rotation = 0;
                externalVideoFrame.timestamp = System.DateTime.Now.Ticks / 10000; // Use current time in milliseconds

                // Push the frame to Agora
                // First, check if we need to create a custom video track
                if (_customVideoTrackId < 0)
                {
                    VideoEncoderConfiguration config = new VideoEncoderConfiguration();
                    config.dimensions = new VideoDimensions(avatarVideoWidth, avatarVideoHeight);
                    config.frameRate = (int)FRAME_RATE.FRAME_RATE_FPS_30;
                    config.bitrate = 1000;

                    // Create a custom video track
                    _customVideoTrackId = (int)RtcEngine.CreateCustomVideoTrack();
                    Debug.Log("Created custom video track ID: " + _customVideoTrackId);
                }

                // Push the frame to the custom video track
                int ret = RtcEngine.PushVideoFrame(externalVideoFrame);
                if (ret != 0)
                {
                    Debug.LogWarning("Failed to push video frame: " + ret);
                    return false;
                }

                return true;
            }

            private void CaptureAvatarFrame()
            {
                CaptureAndPushFrame();
            }

            private void AvatarVideoJoinChannel()
            {
                Debug.Log("Joining channel for avatar video with UID: " + UidAvatarStream);

                // Configure as broadcaster to send video
                ChannelMediaOptions option = new ChannelMediaOptions();

                // Use reflection to set properties if the direct assignment doesn't work
                // This is a workaround for the API version differences
                try
                {
                    var clientRoleTypeProperty = option.GetType().GetProperty("clientRoleType");
                    if (clientRoleTypeProperty != null)
                    {
                        var optionalType = clientRoleTypeProperty.PropertyType;
                        var optionalConstructor = optionalType.GetConstructor(
                            new[] { typeof(CLIENT_ROLE_TYPE) }
                        );
                        if (optionalConstructor != null)
                        {
                            var optionalValue = optionalConstructor.Invoke(
                                new object[] { CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER }
                            );
                            clientRoleTypeProperty.SetValue(option, optionalValue);
                            Debug.Log("Set clientRoleType to BROADCASTER");
                        }
                    }

                    // Set other properties similarly
                    SetOptionalBoolProperty(option, "publishCameraTrack", false);
                    SetOptionalBoolProperty(option, "publishCustomVideoTrack", true);
                    SetOptionalBoolProperty(option, "publishMicrophoneTrack", false);
                    SetOptionalBoolProperty(option, "autoSubscribeVideo", false);
                    SetOptionalBoolProperty(option, "autoSubscribeAudio", false);

                    // Set the custom video track ID to publish
                    var customVideoTrackIdProperty = option
                        .GetType()
                        .GetProperty("customVideoTrackId");
                    if (customVideoTrackIdProperty != null && _customVideoTrackId > 0)
                    {
                        var optionalType = customVideoTrackIdProperty.PropertyType;
                        var optionalConstructor = optionalType.GetConstructor(
                            new[] { typeof(int) }
                        );
                        if (optionalConstructor != null)
                        {
                            var optionalValue = optionalConstructor.Invoke(
                                new object[] { _customVideoTrackId }
                            );
                            customVideoTrackIdProperty.SetValue(option, optionalValue);
                            Debug.Log("Set customVideoTrackId to: " + _customVideoTrackId);
                        }
                    }

                    Debug.Log("Set channel media options");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error setting channel media options: " + e.Message);
                }

                // Create connection for avatar stream
                RtcConnection connection = new RtcConnection();
                connection.channelId = _channelName;
                connection.localUid = UidAvatarStream;

                // Join channel with avatar UID
                var ret = RtcEngine.JoinChannelEx(_token, connection, option);
                Debug.Log("Avatar video JoinChannel returns: " + ret);

                if (ret != 0)
                {
                    Debug.LogError("Failed to join channel for avatar video. Error code: " + ret);
                }
                else
                {
                    Debug.Log(
                        "Successfully joined channel for avatar video with UID: " + UidAvatarStream
                    );
                }
            }

            private void SetOptionalBoolProperty(object target, string propertyName, bool value)
            {
                var property = target.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var optionalType = property.PropertyType;
                    var optionalConstructor = optionalType.GetConstructor(new[] { typeof(bool) });
                    if (optionalConstructor != null)
                    {
                        var optionalValue = optionalConstructor.Invoke(new object[] { value });
                        property.SetValue(target, optionalValue);
                    }
                }
            }

            private void StopAvatarVideoCapture()
            {
                _isCapturingAvatar = false;

                // Create connection for avatar stream to leave
                RtcConnection connection = new RtcConnection();
                connection.channelId = _channelName;
                connection.localUid = UidAvatarStream;

                // Leave channel for avatar video
                RtcEngine.LeaveChannelEx(connection);

                // Clean up resources
                if (avatarRenderTexture != null)
                {
                    avatarRenderTexture.Release();
                    Destroy(avatarRenderTexture);
                    avatarRenderTexture = null;
                }

                if (_avatarTexture2D != null)
                {
                    Destroy(_avatarTexture2D);
                    _avatarTexture2D = null;
                }

                if (avatarCamera != null)
                {
                    avatarCamera.targetTexture = null;
                }

                // Disable external video source
                SenderOptions senderOptions = new SenderOptions();
                RtcEngine.SetExternalVideoSource(
                    false,
                    false,
                    EXTERNAL_VIDEO_SOURCE_TYPE.VIDEO_FRAME,
                    senderOptions
                );
            }

            #endregion

            // Update is called once per frame
            void Update()
            {
                if (_isCapturingAvatar)
                {
                    CaptureAvatarFrame();
                }
            }

            private void OnDestroy()
            {
                if (RtcEngine == null)
                    return;

                if (_isCapturingAvatar)
                {
                    StopAvatarVideoCapture();
                }

                OnStopShareScreen();
                RtcEngine.InitEventHandler(null);
                RtcEngine.LeaveChannel();
                RtcEngine.Dispose();
                RtcEngine = null;
            }

            internal string GetChannelName()
            {
                return _channelName;
            }

            #region -- Video Render UI Logic ---

            internal static void MakeVideoView(
                uint uid,
                string channelId = "",
                VIDEO_SOURCE_TYPE videoSourceType = VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA
            )
            {
                var go = GameObject.Find(uid.ToString());
                if (!ReferenceEquals(go, null))
                {
                    return; // reuse
                }

                // create a GameObject and assign to this new user
                VideoSurface videoSurface = new VideoSurface();

                if (videoSourceType == VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN)
                {
                    return;
                }
                else if (uid == UidTeacherWebcam)
                {
                    go = GameObject.Find("TeacherCameraView");
                    videoSurface = go.GetComponent<VideoSurface>();
                }
                else
                {
                    return;
                }
                if (ReferenceEquals(videoSurface, null))
                    return;
                // configure videoSurface
                videoSurface.SetForUser(uid, channelId, videoSourceType);
                videoSurface.SetEnable(true);
            }

            // VIDEO TYPE 1: 3D Object
            private static VideoSurface MakePlaneSurface(string goName)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Plane);

                if (go == null)
                {
                    return null;
                }

                go.name = goName;
                var mesh = go.GetComponent<MeshRenderer>();
                if (mesh != null)
                {
                    Debug.LogWarning("VideoSureface update shader");
                    mesh.material = new Material(Shader.Find("Unlit/Texture"));
                }
                // set up transform
                go.transform.Rotate(-90.0f, 0.0f, 0.0f);
                go.transform.position = Vector3.zero;
                go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

                // configure videoSurface
                var videoSurface = go.AddComponent<VideoSurface>();
                return videoSurface;
            }

            internal static void DestroyVideoView(string name)
            {
                var go = GameObject.Find(name);
                if (!ReferenceEquals(go, null))
                {
                    Destroy(go);
                }
            }

            #endregion

            #region -- Teacher Mouse Display

            internal void PositionRedDot(Vector2 normalizedCoordinate)
            {
                var _screenShareRect = GameObject
                    .Find("ScreenCanvas")
                    ?.GetComponent<RectTransform>();
                if (_screenShareRect == null)
                {
                    Debug.LogError("Screen share rect not set.");
                    return;
                }

                if (redDot == null)
                {
                    Debug.LogError("Red dot not found.");
                    return;
                }

                // Check for the "hide dot" condition
                if (normalizedCoordinate == new Vector2(-1, -1))
                {
                    redDot.gameObject.SetActive(false);
                    return;
                }

                // Calculate the new position for the red dot
                Vector2 imageSize = _screenShareRect.rect.size;
                Vector2 position = new Vector2(
                    normalizedCoordinate.x * imageSize.x,
                    -normalizedCoordinate.y * imageSize.y
                );

                // Check if the position has actually changed to avoid redundant updates
                RectTransform redDotTransform = redDot.GetComponent<RectTransform>();
                redDotTransform.anchoredPosition = position;

                // Ensure the red dot is visible
                redDot.gameObject.SetActive(true);

                redDot.GetComponent<Image>().color = Color.red;
            }

            #endregion
        }

        #region -- Agora Event ---

        internal class UserEventHandler : IRtcEngineEventHandler
        {
            private readonly StudentAgoraScript _desktopScreenShare;

            internal UserEventHandler(StudentAgoraScript desktopScreenShare)
            {
                _desktopScreenShare = desktopScreenShare;
            }

            public override void OnError(int err, string msg)
            {
                Debug.Log(string.Format("OnError err: {0}, msg: {1}", err, msg));
            }

            public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                int build = 0;
                Debug.Log(
                    string.Format(
                        "sdk version: ${0}",
                        _desktopScreenShare.RtcEngine.GetVersion(ref build)
                    )
                );
                Debug.Log(
                    string.Format(
                        "OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                        connection.channelId,
                        connection.localUid,
                        elapsed
                    )
                );
                if (connection.localUid == _desktopScreenShare.UidStudentWebcam)
                {
                    StudentAgoraScript.MakeVideoView(0);
                }
                else if (connection.localUid == _desktopScreenShare.UidStudentDesktop)
                {
                    StudentAgoraScript.MakeVideoView(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN);
                }
            }

            public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                Debug.Log("OnRejoinChannelSuccess");
            }

            public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
            {
                Debug.Log("OnLeaveChannel");
                if (connection.localUid == _desktopScreenShare.UidStudentWebcam)
                {
                    StudentAgoraScript.DestroyVideoView("MainCameraView");
                }
            }

            public override void OnClientRoleChanged(
                RtcConnection connection,
                CLIENT_ROLE_TYPE oldRole,
                CLIENT_ROLE_TYPE newRole,
                ClientRoleOptions newRoleOptions
            )
            {
                Debug.Log("OnClientRoleChanged");
            }

            public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
            {
                Debug.Log(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
                if (
                    uid != _desktopScreenShare.UidStudentWebcam
                    && uid != _desktopScreenShare.UidStudentDesktop
                )
                {
                    StudentAgoraScript.MakeVideoView(
                        uid,
                        _desktopScreenShare.GetChannelName(),
                        VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                    );
                }
            }

            public override void OnUserOffline(
                RtcConnection connection,
                uint uid,
                USER_OFFLINE_REASON_TYPE reason
            )
            {
                Debug.Log(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
                if (
                    uid != _desktopScreenShare.UidStudentWebcam
                    && uid != _desktopScreenShare.UidStudentDesktop
                )
                {
                    StudentAgoraScript.DestroyVideoView(uid.ToString());
                }
            }

            public override void OnStreamMessage(
                RtcConnection connection,
                uint remoteUid,
                int streamId,
                byte[] data,
                ulong length,
                ulong sentTs
            )
            {
                // Convert the data to a string
                string coordinateString = System.Text.Encoding.Default.GetString(data);
                // Parse the coordinate string
                if (TryParseCoordinate(coordinateString, out Vector2 coordinate))
                {
                    // Update the red dot's position
                    _desktopScreenShare.PositionRedDot(coordinate);
                }
                else
                {
                    Debug.LogError("Invalid coordinate format received.");
                }
            }

            private bool TryParseCoordinate(string coordinateString, out Vector2 coordinate)
            {
                coordinate = Vector2.zero;
                var parts = coordinateString.Split(',');
                if (
                    parts.Length == 2
                    && float.TryParse(parts[0], out float x)
                    && float.TryParse(parts[1], out float y)
                )
                {
                    coordinate = new Vector2(x, y);
                    return true;
                }
                return false;
            }

            public override void OnStreamMessageError(
                RtcConnection connection,
                uint remoteUid,
                int streamId,
                int code,
                int missed,
                int cached
            )
            {
                Debug.Log(
                    string.Format(
                        "OnStreamMessageError remoteUid: {0}, streamId: {1}, code: {2}, missed: {3}, cached: {4}",
                        remoteUid,
                        streamId,
                        code,
                        missed,
                        cached
                    )
                );
            }
        }

        #endregion
    }
}
