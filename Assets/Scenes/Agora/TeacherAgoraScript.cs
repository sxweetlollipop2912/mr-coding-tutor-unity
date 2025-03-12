using System;
using Agora.Rtc;
using io.agora.rtc.demo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShareWhileVideoCall
{
    namespace TeacherMrCodingTutorUnity
    {
        public class TeacherAgoraScript : MonoBehaviour, IPointerExitHandler
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
            private float mouseMessagesPerSecond = 30f;

            [SerializeField]
            private bool useExternalConfig = true;

            private float _lastMouseMessageTime = 0f;

            internal IRtcEngineEx RtcEngine = null;

            public uint UidWebcam = 321;
            public uint UidScreen = 654;
            public static uint UidStudentScreen = 456;
            public static uint UidStudentWebcam = 123;

            private int _streamId = -1;
            private static GameObject _mainScreen = null;
            private static GameObject _studentScreen = null;
            private bool _isStreamingMouse = false;

            // Current orientation index for cycling
            private int _currentOrientationIndex = 0;

            // Use this for initialization
            private void Start()
            {
                LoadAssetData();
                InitEngine();
                JoinChannel();
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
                        "[TeacherAgoraScript] ConfigLoader instance or configuration data is not available."
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
                RtcEngine.JoinChannel(_token, _channelName, this.UidWebcam, options);
                RtcEngine.MuteRemoteAudioStream(UidScreen, true);
                RtcEngine.MuteRemoteVideoStream(UidScreen, true);
            }

            void Update()
            {
                // PermissionHelper.RequestMicrophonePermission();
                UpdateMousePosition();
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

#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                //If you want to share audio when sharing the desktop screen, you need to use this interface.
                //For details, please refer to the annotation of this interface
                ret = RtcEngine.EnableLoopbackRecordingEx(
                    new RtcConnection(_channelName, this.UidScreen),
                    true,
                    ""
                );
                Debug.Log("EnableLoopbackRecording returns: " + ret);
#endif
                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
                ret = RtcEngine.JoinChannelEx(
                    _token,
                    new RtcConnection(_channelName, this.UidScreen),
                    options
                );
                Debug.Log("JoinChannelEx returns: " + ret);
            }

            private void ScreenShareLeaveChannel()
            {
                RtcEngine.LeaveChannelEx(new RtcConnection(_channelName, UidScreen));
            }

            private void UpdateChannelMediaOptions()
            {
                ChannelMediaOptions options = new ChannelMediaOptions();
                options.autoSubscribeAudio.SetValue(false);
                options.autoSubscribeVideo.SetValue(false);

                options.publishCameraTrack.SetValue(false);
                options.publishScreenTrack.SetValue(true);

                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

                var ret = RtcEngine.UpdateChannelMediaOptions(options);
                Debug.Log("UpdateChannelMediaOptions returns: " + ret);
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

            private void OnDestroy()
            {
                Debug.Log("OnDestroy");
                if (RtcEngine == null)
                    return;
                RtcEngine.InitEventHandler(null);
                RtcEngine.LeaveChannel();
                RtcEngine.Dispose();
            }

            // Method to refresh the avatar view manually if needed
            [ContextMenu("Refresh Avatar View")]
            public void RefreshAvatarView()
            {
                Debug.Log("Manually refreshing avatar view...");

                // Destroy existing view if it exists
                var existingView = GameObject.Find("AvatarView");
                if (existingView != null)
                {
                    Debug.Log("Found existing avatar view, destroying it");
                    Destroy(existingView);
                }

                // Create a new view
                Debug.Log("Creating new avatar view");
                VideoSurface videoSurface = MakeImageSurface("AvatarView");
                if (videoSurface != null)
                {
                    // Configure for avatar UID
                    videoSurface.SetForUser(
                        789,
                        _channelName,
                        VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                    );
                    videoSurface.SetEnable(true);

                    // Set proper orientation
                    RectTransform rectTransform =
                        videoSurface.gameObject.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = new Vector3(1, -1, 1);
                        rectTransform.localRotation = Quaternion.identity;
                    }

                    Debug.Log("Avatar view refreshed successfully");
                }
                else
                {
                    Debug.LogError("Failed to create new avatar view");
                }
            }

            // Method to fix the avatar view orientation
            [ContextMenu("Fix Avatar Orientation")]
            public void FixAvatarOrientation()
            {
                Debug.Log("Attempting to fix avatar orientation...");

                // Find the avatar view
                var avatarView = GameObject.Find("AvatarView");
                if (avatarView != null)
                {
                    // Get the RectTransform component
                    RectTransform rectTransform = avatarView.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // Cycle to the next orientation
                        _currentOrientationIndex = (_currentOrientationIndex + 1) % 8;

                        switch (_currentOrientationIndex)
                        {
                            case 0: // Normal
                                rectTransform.localScale = new Vector3(1, 1, 1);
                                rectTransform.localRotation = Quaternion.identity;
                                Debug.Log("Avatar Orientation 1/8: Normal");
                                break;
                            case 1: // Flipped horizontally
                                rectTransform.localScale = new Vector3(-1, 1, 1);
                                rectTransform.localRotation = Quaternion.identity;
                                Debug.Log("Avatar Orientation 2/8: Flipped horizontally");
                                break;
                            case 2: // Flipped vertically
                                rectTransform.localScale = new Vector3(1, -1, 1);
                                rectTransform.localRotation = Quaternion.identity;
                                Debug.Log("Avatar Orientation 3/8: Flipped vertically");
                                break;
                            case 3: // Flipped both
                                rectTransform.localScale = new Vector3(-1, -1, 1);
                                rectTransform.localRotation = Quaternion.identity;
                                Debug.Log(
                                    "Avatar Orientation 4/8: Flipped both horizontally and vertically"
                                );
                                break;
                            case 4: // Rotated 90 degrees
                                rectTransform.localScale = new Vector3(1, 1, 1);
                                rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
                                Debug.Log("Avatar Orientation 5/8: Rotated 90 degrees");
                                break;
                            case 5: // Rotated 180 degrees
                                rectTransform.localScale = new Vector3(1, 1, 1);
                                rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
                                Debug.Log("Avatar Orientation 6/8: Rotated 180 degrees");
                                break;
                            case 6: // Rotated 270 degrees
                                rectTransform.localScale = new Vector3(1, 1, 1);
                                rectTransform.localRotation = Quaternion.Euler(0, 0, 270);
                                Debug.Log("Avatar Orientation 7/8: Rotated 270 degrees");
                                break;
                            case 7: // Rotated 180 degrees and flipped
                                rectTransform.localScale = new Vector3(-1, -1, 1);
                                rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
                                Debug.Log(
                                    "Avatar Orientation 8/8: Rotated 180 degrees and flipped"
                                );
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogError("Avatar view doesn't have a RectTransform component");
                    }
                }
                else
                {
                    Debug.LogError("Avatar view not found in the scene");
                }
            }

            // Method to cycle through different orientations
            [ContextMenu("Cycle Avatar Orientation")]
            public void CycleAvatarOrientation()
            {
                // Just call the fix method which now cycles through orientations
                FixAvatarOrientation();
            }

            // Method to apply the correct orientation for the avatar view
            // This is called automatically when the avatar stream joins
            public void ApplyAvatarCorrectOrientation()
            {
                Debug.Log("Automatically applying correct orientation for avatar view");

                // Find the avatar view
                var avatarView = GameObject.Find("AvatarView");
                if (avatarView != null)
                {
                    // Get the RectTransform component
                    RectTransform rectTransform = avatarView.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // Apply the orientation that works best
                        // Based on testing, this is the correct orientation
                        rectTransform.localScale = new Vector3(1, 1, 1);
                        rectTransform.localRotation = Quaternion.identity;
                        Debug.Log("Applied correct orientation to avatar view: Flipped vertically");
                    }
                    else
                    {
                        Debug.LogError("Avatar view doesn't have a RectTransform component");
                    }
                }
                else
                {
                    Debug.LogWarning("Avatar view not found yet, will try again in 0.5 seconds");
                    // Try again after a short delay
                    Invoke("ApplyAvatarCorrectOrientation", 0.5f);
                }
            }

            // Method to debug the current orientation of the avatar view
            [ContextMenu("Debug Avatar View")]
            public void DebugAvatarView()
            {
                Debug.Log("Debugging avatar view...");

                // Find the avatar view
                var avatarView = GameObject.Find("AvatarView");
                if (avatarView != null)
                {
                    // Get the RectTransform component
                    RectTransform rectTransform = avatarView.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Debug.Log($"Avatar view found with name: {avatarView.name}");
                        Debug.Log($"Current scale: {rectTransform.localScale}");
                        Debug.Log($"Current rotation: {rectTransform.localRotation.eulerAngles}");
                        Debug.Log($"Current size: {rectTransform.sizeDelta}");
                        Debug.Log($"Current position: {rectTransform.localPosition}");

                        // Get the RawImage component
                        RawImage rawImage = avatarView.GetComponent<RawImage>();
                        if (rawImage != null)
                        {
                            Debug.Log(
                                $"RawImage texture: {(rawImage.texture != null ? "Present" : "None")}"
                            );
                            if (rawImage.texture != null)
                            {
                                Debug.Log(
                                    $"Texture size: {rawImage.texture.width}x{rawImage.texture.height}"
                                );
                            }
                        }

                        // Get the VideoSurface component
                        VideoSurface videoSurface = avatarView.GetComponent<VideoSurface>();
                        if (videoSurface != null)
                        {
                            Debug.Log($"VideoSurface enabled: {videoSurface.enabled}");
                        }
                    }
                    else
                    {
                        Debug.LogError("Avatar view doesn't have a RectTransform component");
                    }
                }
                else
                {
                    Debug.LogError("Avatar view not found in the scene");
                }
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

                if (uid == UidStudentWebcam)
                {
                    videoSurface = MakeImageSurface("MainCameraView");
                }
                else if (uid == UidStudentScreen)
                {
                    videoSurface = MakeImageSurface("ScreenShareView");
                }
                else if (uid == 785) // Avatar video stream UID
                {
                    videoSurface = MakeImageSurface("AvatarView");
                    Debug.Log("Creating view for Avatar video stream with UID: " + uid);
                }
                else
                {
                    videoSurface = MakeImageSurface(uid.ToString());
                }
                if (ReferenceEquals(videoSurface, null))
                    return;

                // configure videoSurface
                videoSurface.SetForUser(uid, channelId, videoSourceType);
                videoSurface.SetEnable(true);
                videoSurface.OnTextureSizeModify += (int width, int height) =>
                {
                    var transform = videoSurface.GetComponent<RectTransform>();
                    if (transform)
                    {
                        if (uid == 0 && videoSourceType == VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA)
                        {
                            transform.localScale = new Vector3(1, 1, 1);
                        }
                        else if (videoSourceType == VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN)
                        {
                            transform.localScale = new Vector3(1, 1, 1);
                        }
                        else if (uid == 785) // Avatar video stream needs special handling
                        {
                            // Don't change the scale here as it might override our fix
                            // Just log that we received a texture size modification
                            Debug.Log($"Avatar video texture size modified: {width}x{height}");

                            // Ensure our orientation fix is preserved
                            // We want it flipped vertically but not horizontally
                            if (transform.localScale.y > 0) // If it's not already flipped vertically
                            {
                                transform.localScale = new Vector3(1, -1, 1);
                                Debug.Log("Re-applied vertical flip to avatar view");
                            }
                        }
                        else
                        {
                            transform.localScale = new Vector3(-1, 1, 1);
                        }

                        transform.sizeDelta = new Vector2(width / 2, height / 2);
                    }
                    else
                    {
                        //If render in MeshRenderer, just set localSize with MeshRenderer
                        float scale = (float)height / (float)width;
                        videoSurface.transform.localScale = new Vector3(-1, 1, scale);
                    }
                    Debug.Log("OnTextureSizeModify: " + width + "  " + height);
                };

                if (uid == UidStudentScreen)
                {
                    _studentScreen = videoSurface.gameObject;
                }
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
                    Debug.LogWarning("VideoSurface update shader");
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

            // Video TYPE 2: RawImage
            private static VideoSurface MakeImageSurface(string goName)
            {
                var go = new GameObject();

                if (go == null)
                {
                    return null;
                }

                go.name = goName;

                // To be rendered onto
                go.AddComponent<RawImage>();

                // Set up transform properties
                if (goName == "AvatarView")
                {
                    // Special handling for avatar view - no rotation needed
                    go.transform.Rotate(0f, 0.0f, 0.0f);
                    Debug.Log("Created AvatarView with correct orientation");
                }
                else
                {
                    // Default rotation for other views
                    go.transform.Rotate(0f, 0.0f, 180.0f);
                }

                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = new Vector3(3f, 4f, 1f);

                // Set the sizeDelta to allow the layout group to control size
                var rectTransform = go.GetComponent<RectTransform>();
                float width = 160; // Example width
                float height = width * 9f / 16f; // Calculate height based on 16:9 aspect ratio
                rectTransform.sizeDelta = new Vector2(width, height); // Apply size

                if (_mainScreen != null)
                {
                    ConvertToStandby(_mainScreen);
                }
                ConvertToMainScreen(go);

                // Configure VideoSurface
                var videoSurface = go.AddComponent<VideoSurface>();
                return videoSurface;
            }

            private static void ConvertToStandby(GameObject go)
            {
                var canvas = GameObject.Find("StandByScreensRow");
                if (canvas != null)
                {
                    // Properly set the parent of the new GameObject
                    go.transform.SetParent(canvas.transform, false);

                    Debug.Log("Added video view to StandByScreensRow");
                }
                else
                {
                    Debug.LogError("Canvas is null. Video view not added.");
                }

                var aspectRatioFilter = go.GetComponent<AspectRatioFitter>();
                if (aspectRatioFilter != null)
                {
                    Destroy(aspectRatioFilter);
                }

                // Set LayoutElement for proper size in Horizontal Layout Group
                var layoutElement = go.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 160;
                layoutElement.preferredHeight = 90;

                // Set the sizeDelta to allow the layout group to control size
                var rectTransform = go.GetComponent<RectTransform>();
                float width = 160; // Example width
                float height = width * 9f / 16f; // Calculate height based on 16:9 aspect ratio
                rectTransform.sizeDelta = new Vector2(width, height); // Apply size

                var button = go.AddComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    ConvertToStandby(_mainScreen);
                    ConvertToMainScreen(go);
                });
            }

            private static void ConvertToMainScreen(GameObject go)
            {
                var canvas = GameObject.Find("MainScreen");
                if (canvas != null)
                {
                    // Properly set the parent of the new GameObject
                    go.transform.SetParent(canvas.transform, false);

                    Debug.Log("Added video view to StandByScreensRow");
                }
                else
                {
                    Debug.LogError("Canvas is null. Video view not added.");
                }

                var layoutElement = go.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    Destroy(layoutElement);
                }
                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    Destroy(button);
                }

                // Configure the RectTransform to stretch
                RectTransform rectTransform = go.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0); // Stretch horizontally and vertically
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = Vector2.zero; // Remove offsets
                rectTransform.offsetMax = Vector2.zero;

                // Step 4: Add Aspect Ratio Fitter to maintain the aspect ratio
                var aspectRatioFitter = go.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter == null)
                {
                    aspectRatioFitter = go.gameObject.AddComponent<AspectRatioFitter>();
                }
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // Maintains ratio while stretching
                aspectRatioFitter.aspectRatio = 16f / 9f; // Example 16:9 ratio (adjust as needed)

                _mainScreen = go;
            }

            internal static void DestroyVideoView(string name)
            {
                var go = GameObject.Find(name);
                if (!ReferenceEquals(go, null))
                {
                    Destroy(go);

                    if (ReferenceEquals(go, _mainScreen))
                    {
                        var standbyScreensRow = GameObject.Find("StandByScreensRow").transform;
                        // Get the last screen in the standby row
                        Transform lastScreen = standbyScreensRow.GetChild(
                            standbyScreensRow.childCount - 1
                        );
                        ConvertToMainScreen(lastScreen.gameObject);
                    }
                }
            }

            #endregion

            #region -- Mouse Position Streaming Logic ---

            private bool _isMouseDown = false; // Explicitly track mouse down state

            private void UpdateMousePosition()
            {
                if (_studentScreen == null)
                    return;

                // Only proceed if enough time has passed
                if (Time.time - _lastMouseMessageTime < 1f / mouseMessagesPerSecond)
                    return;

                if (Input.GetMouseButtonDown(0))
                {
                    _isMouseDown = true;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    _isMouseDown = false;
                    StopMouseStreaming();
                }

                if (_isMouseDown)
                {
                    RectTransform canvasRect = _studentScreen.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        Vector2 screenMousePosition = Input.mousePosition;
                        if (
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                canvasRect,
                                screenMousePosition,
                                null,
                                out Vector2 localPoint
                            )
                        )
                        {
                            if (IsInsideCanvas(localPoint, canvasRect))
                            {
                                float normalizedX =
                                    (localPoint.x + canvasRect.rect.width / 2)
                                    / canvasRect.rect.width;
                                float normalizedY =
                                    (localPoint.y + canvasRect.rect.height / 2)
                                    / canvasRect.rect.height;

                                string mouseData = $"{normalizedX},{normalizedY}";
                                StreamMessage(mouseData);
                                _isStreamingMouse = true;
                                _lastMouseMessageTime = Time.time; // Mark send time
                                return;
                            }
                        }
                    }
                }

                if (_isStreamingMouse)
                {
                    StopMouseStreaming();
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                Debug.Log("Pointer exited the canvas.");
                StopMouseStreaming();
            }

            private void StopMouseStreaming()
            {
                if (_isStreamingMouse)
                {
                    Debug.Log("Sending stop streaming signal (-1,-1).");
                    StreamMessage("-1,-1");
                    _isStreamingMouse = false;
                }
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                if (!hasFocus)
                {
                    Debug.Log("Application lost focus. Stopping mouse streaming.");
                    StopMouseStreaming();
                }
            }

            private void OnApplicationPause(bool isPaused)
            {
                if (isPaused)
                {
                    Debug.Log("Application paused. Stopping mouse streaming.");
                    StopMouseStreaming();
                }
            }

            bool IsInsideCanvas(Vector2 localPoint, RectTransform canvasRect)
            {
                // Check if the point is within the bounds of the Canvas
                return localPoint.x >= -canvasRect.rect.width / 2
                    && localPoint.x <= canvasRect.rect.width / 2
                    && localPoint.y >= -canvasRect.rect.height / 2
                    && localPoint.y <= canvasRect.rect.height / 2;
            }

            private void StreamMessage(string msg)
            {
                if (msg == "")
                {
                    Debug.Log("Dont send empty message!");
                    return;
                }

                int streamId = this.CreateDataStreamId();
                if (streamId < 0)
                {
                    Debug.Log("CreateDataStream failed!");
                    return;
                }
                else
                {
                    SendStreamMessage(streamId, msg);
                }
            }

            private int CreateDataStreamId()
            {
                if (this._streamId == -1)
                {
                    var config = new DataStreamConfig();
                    config.syncWithAudio = false;
                    config.ordered = true;
                    var nRet = RtcEngine.CreateDataStream(ref this._streamId, config);
                    Debug.Log(
                        string.Format("CreateDataStream: nRet{0}, streamId{1}", nRet, _streamId)
                    );
                }
                return _streamId;
            }

            private void SendStreamMessage(int streamId, string message)
            {
                byte[] byteArray = System.Text.Encoding.Default.GetBytes(message);
                var nRet = RtcEngine.SendStreamMessage(
                    streamId,
                    byteArray,
                    Convert.ToUInt32(byteArray.Length)
                );
            }

            // Method to recreate the avatar view if it can't be found
            private void RecreateAvatarView()
            {
                Debug.Log("Attempting to recreate AvatarView...");

                // First check if there's an existing view with a different name
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    VideoSurface surface = obj.GetComponent<VideoSurface>();
                    if (surface != null)
                    {
                        uint uid = 0;
                        try
                        {
                            // Try to get the UID from the VideoSurface
                            System.Reflection.FieldInfo uidField = surface
                                .GetType()
                                .GetField(
                                    "mUid",
                                    System.Reflection.BindingFlags.NonPublic
                                        | System.Reflection.BindingFlags.Instance
                                );
                            if (uidField != null)
                            {
                                uid = (uint)uidField.GetValue(surface);
                                if (uid == 785)
                                {
                                    Debug.Log(
                                        $"Found existing avatar video surface on GameObject: {obj.name}"
                                    );
                                    obj.name = "AvatarView"; // Rename it

                                    // Apply orientation
                                    RectTransform rectTransform = obj.GetComponent<RectTransform>();
                                    if (rectTransform != null)
                                    {
                                        rectTransform.localScale = new Vector3(1, -1, 1);
                                        Debug.Log("Applied orientation to renamed AvatarView");
                                    }
                                    return;
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error checking VideoSurface: {e.Message}");
                        }
                    }
                }

                // If we couldn't find an existing view, create a new one
                VideoSurface videoSurface = MakeImageSurface("AvatarView");
                if (videoSurface != null)
                {
                    videoSurface.SetForUser(
                        785,
                        _channelName,
                        VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                    );
                    videoSurface.SetEnable(true);

                    // Apply orientation
                    RectTransform rectTransform =
                        videoSurface.gameObject.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = new Vector3(1, -1, 1);
                        Debug.Log("Created new AvatarView and applied orientation");
                    }
                }
            }
        }

            #endregion

        #region -- Agora Event ---

        internal class UserEventHandler : IRtcEngineEventHandler
        {
            private readonly TeacherAgoraScript _desktopScreenShare;

            internal UserEventHandler(TeacherAgoraScript desktopScreenShare)
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
                if (connection.localUid == _desktopScreenShare.UidWebcam)
                {
                    TeacherAgoraScript.MakeVideoView(0);
                }
                else if (connection.localUid == _desktopScreenShare.UidScreen)
                {
                    TeacherAgoraScript.MakeVideoView(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN);
                }
            }

            public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                Debug.Log("OnRejoinChannelSuccess");
            }

            public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
            {
                Debug.Log("OnLeaveChannel");
                if (connection.localUid == _desktopScreenShare.UidWebcam)
                {
                    TeacherAgoraScript.DestroyVideoView("MainCameraView");
                }
                else if (connection.localUid == _desktopScreenShare.UidScreen)
                {
                    TeacherAgoraScript.DestroyVideoView("ScreenShareView");
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
                if (uid != _desktopScreenShare.UidWebcam && uid != _desktopScreenShare.UidScreen)
                {
                    // Check if this is the avatar video stream (UID 785)
                    if (uid == 785)
                    {
                        Debug.Log("*********************************************************");
                        Debug.Log("***** AVATAR VIDEO STREAM JOINED WITH UID: " + uid + " *****");
                        Debug.Log("*********************************************************");

                        // First check if the view already exists
                        var existingView = GameObject.Find("AvatarView");
                        if (existingView != null)
                        {
                            Debug.Log("AvatarView already exists, will update it");

                            // Update the existing view
                            VideoSurface existingSurface =
                                existingView.GetComponent<VideoSurface>();
                            if (existingSurface != null)
                            {
                                existingSurface.SetForUser(
                                    uid,
                                    _desktopScreenShare.GetChannelName(),
                                    VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                                );
                                existingSurface.SetEnable(true);
                                Debug.Log("Updated existing AvatarView");
                            }

                            // Apply orientation immediately
                            _desktopScreenShare.ApplyAvatarCorrectOrientation();
                        }
                        else
                        {
                            // Make sure we create a dedicated view for it
                            Debug.Log("Creating new AvatarView");

                            // Create a dedicated view for the avatar stream
                            TeacherAgoraScript.MakeVideoView(
                                uid,
                                _desktopScreenShare.GetChannelName(),
                                VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                            );

                            // Apply the correct orientation after a short delay to ensure the view is created
                            _desktopScreenShare.Invoke("ApplyAvatarCorrectOrientation", 1.0f);
                        }
                    }
                    else
                    {
                        TeacherAgoraScript.MakeVideoView(
                            uid,
                            _desktopScreenShare.GetChannelName(),
                            VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                        );
                    }
                }
            }

            public override void OnUserOffline(
                RtcConnection connection,
                uint uid,
                USER_OFFLINE_REASON_TYPE reason
            )
            {
                Debug.Log(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
                if (uid != _desktopScreenShare.UidWebcam && uid != _desktopScreenShare.UidScreen)
                {
                    // Check if this is the avatar video stream (UID 785)
                    if (uid == 785)
                    {
                        Debug.Log("Avatar video stream went offline with UID: " + uid);
                        TeacherAgoraScript.DestroyVideoView("AvatarView");
                    }
                    else
                    {
                        TeacherAgoraScript.DestroyVideoView(uid.ToString());
                    }
                }
            }
        }

        #endregion
    }
}
