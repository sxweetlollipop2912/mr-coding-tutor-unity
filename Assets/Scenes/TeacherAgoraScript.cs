using System;
using System.Linq;
using Agora.Rtc;
using io.agora.rtc.demo;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShareWhileVideoCall
{
    namespace TeacherMrCodingTutorUnity
    {
        public class TeacherAgoraScript : MonoBehaviour
        {
            [FormerlySerializedAs("appIdInput")]
            [SerializeField]
            private AgoraTeacherInput _appIdInput;

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

            internal IRtcEngineEx RtcEngine = null;

            public uint UidWebcam = 321;
            public uint UidScreen = 654;
            public static uint UidStudentScreen = 456;

            private int _streamId = -1;
            private static GameObject _mainScreen = null;
            private static GameObject _studentScreen = null;
            private bool _isStreamingMouse = false;

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
                if (_appIdInput == null)
                    return;
                _appID = _appIdInput.appID;
                _token = _appIdInput.token;
                _channelName = _appIdInput.channelName;
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

                if (videoSourceType == VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA)
                {
                    videoSurface = MakeImageSurface("MainCameraView");
                }
                else if (videoSourceType == VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN)
                {
                    videoSurface = MakeImageSurface("ScreenShareView");
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
                go.transform.Rotate(0f, 0.0f, 180.0f);
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

            private void UpdateMousePosition()
            {
                if (_studentScreen == null)
                    return;

                RectTransform canvasRect = _studentScreen.GetComponent<RectTransform>();

                if (canvasRect != null && Input.GetMouseButton(0)) // Check if left mouse button is pressed
                {
                    // Get the mouse position in screen space
                    Vector2 screenMousePosition = Input.mousePosition;

                    // Convert the screen position to the Canvas's local space
                    if (
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvasRect,
                            screenMousePosition,
                            null,
                            out Vector2 localPoint
                        )
                    )
                    {
                        // Check if the mouse position is within the bounds of the Canvas
                        if (IsInsideCanvas(localPoint, canvasRect))
                        {
                            // Normalize the position relative to the Canvas
                            float normalizedX =
                                (localPoint.x + canvasRect.rect.width / 2) / canvasRect.rect.width;
                            float normalizedY =
                                (localPoint.y + canvasRect.rect.height / 2)
                                / canvasRect.rect.height;

                            // Send the normalized position via Agora data channel
                            string mouseData = $"{normalizedX},{normalizedY}";
                            StreamMessage(mouseData);
                            _isStreamingMouse = true;
                            return;
                        }
                    }
                }

                if (_isStreamingMouse)
                {
                    // Send a message to stop streaming the mouse position
                    StreamMessage("-1,-1");
                    _isStreamingMouse = false;
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
                    TeacherAgoraScript.MakeVideoView(
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
                if (uid != _desktopScreenShare.UidWebcam && uid != _desktopScreenShare.UidScreen)
                {
                    TeacherAgoraScript.DestroyVideoView(uid.ToString());
                }
            }
        }

        #endregion
    }
}
