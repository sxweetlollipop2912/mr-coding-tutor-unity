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

            internal IRtcEngineEx RtcEngine = null;

            public uint Uid1 = 123;
            public uint Uid2 = 456;
            public static uint UidTeacherWebcam = 321;
            private static bool _isTeacherWebcamActive = false;

            // Use this for initialization
            private void Start()
            {
                LoadAssetData();
                InitEngine();
                EnableUI();
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
                    Debug.LogError("[StudentAgoraScript] ConfigLoader instance or configuration data is not available.");
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
                    Debug.LogError("Token is missing or empty. Please check external configuration or inspector.");
                    hasError = true;
                }
                
                if (string.IsNullOrEmpty(_channelName))
                {
                    Debug.LogError("Channel name is missing or empty. Please check external configuration or inspector.");
                    hasError = true;
                }
                
                if (hasError)
                {
                    Debug.LogError("Some Agora configuration parameters are missing. The application may not function correctly.");
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
                RtcEngine.JoinChannel(_token, _channelName, this.Uid1, options);
                RtcEngine.MuteRemoteAudioStream(Uid2, true);
                RtcEngine.MuteRemoteVideoStream(Uid2, true);
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
                    new RtcConnection(_channelName, this.Uid2),
                    options
                );
                Debug.Log("JoinChannelEx returns: " + ret);
            }

            private void ScreenShareLeaveChannel()
            {
                RtcEngine.LeaveChannelEx(new RtcConnection(_channelName, Uid2));
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

            private void OnDestroy()
            {
                Debug.Log("OnDestroy");
                if (RtcEngine == null)
                    return;
                OnStopShareScreen();
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
                if (connection.localUid == _desktopScreenShare.Uid1)
                {
                    StudentAgoraScript.MakeVideoView(0);
                }
                else if (connection.localUid == _desktopScreenShare.Uid2)
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
                if (connection.localUid == _desktopScreenShare.Uid1)
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
                if (uid != _desktopScreenShare.Uid1 && uid != _desktopScreenShare.Uid2)
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
                if (uid != _desktopScreenShare.Uid1 && uid != _desktopScreenShare.Uid2)
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
