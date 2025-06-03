using System;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShareWhileVideoCall.StudentMrCodingTutorUnity;
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
        public class TeacherAgoraScript_NewUI : MonoBehaviour, IPointerExitHandler
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

            [Header("_____________UI Components_____________")]
            [SerializeField]
            private RawImage StudentAvatarImage;

            [SerializeField]
            private RawImage StudentDesktopImage;

            [SerializeField]
            private RectTransform MouseExclusionArea;

            [Header("_____________Stream UIDs_____________")]
            [SerializeField]
            private uint UidStudentDesktop;

            [SerializeField]
            private uint UidStudentAvatar;

            private float _lastMouseMessageTime = 0f;

            internal IRtcEngineEx RtcEngine = null;

            public uint UidWebcam = 321;
            public uint UidScreen = 654;

            private int _streamId = -1;

            // Current orientation index for cycling
            private int _currentOrientationIndex = 0;

            // VideoSurface components for the student streams
            private VideoSurface _studentAvatarVideoSurface;
            private VideoSurface _studentDesktopVideoSurface;

            // Use this for initialization
            private void Start()
            {
                LoadAssetData();
                InitEngine();
                JoinChannel();

                // Initialize VideoSurface components for the RawImages
                if (StudentAvatarImage != null)
                {
                    _studentAvatarVideoSurface =
                        StudentAvatarImage.gameObject.AddComponent<VideoSurface>();
                }
                else
                {
                    Debug.LogError("StudentAvatarImage is not assigned!");
                }

                if (StudentDesktopImage != null)
                {
                    _studentDesktopVideoSurface =
                        StudentDesktopImage.gameObject.AddComponent<VideoSurface>();
                }
                else
                {
                    Debug.LogError("StudentDesktopImage is not assigned!");
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
                        "[TeacherAgoraScript_NewUI] ConfigLoader instance or configuration data is not available."
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

                if (StudentAvatarImage == null)
                {
                    Debug.LogError(
                        "StudentAvatarImage is not assigned. Please assign it in the inspector."
                    );
                    hasError = true;
                }

                if (StudentDesktopImage == null)
                {
                    Debug.LogError(
                        "StudentDesktopImage is not assigned. Please assign it in the inspector."
                    );
                    hasError = true;
                }

                if (UidStudentAvatar == 0)
                {
                    Debug.LogError("UidStudentAvatar is not set. Please set it in the inspector.");
                    hasError = true;
                }

                if (UidStudentDesktop == 0)
                {
                    Debug.LogError("UidStudentDesktop is not set. Please set it in the inspector.");
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
            }

            void Update()
            {
                UpdateMousePosition();
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

            // Method to apply the correct orientation for the avatar view
            // This is called automatically when the avatar stream joins
            public void ApplyAvatarCorrectOrientation()
            {
                if (StudentAvatarImage != null)
                {
                    RectTransform rectTransform = StudentAvatarImage.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = new Vector3(1, -1, 1);
                    }
                    else
                    {
                        Debug.LogError("Avatar image doesn't have a RectTransform component");
                    }
                }
                else
                {
                    Debug.LogWarning("StudentAvatarImage is not assigned");
                }
            }

            internal string GetChannelName()
            {
                return _channelName;
            }

            #region -- Mouse Position Streaming Logic ---

            private bool _shouldStreamMouse = false; // Flag to track if we should be streaming mouse position

            private void UpdateMousePosition()
            {
                if (StudentDesktopImage == null)
                    return;

                // Only proceed if enough time has passed
                if (Time.time - _lastMouseMessageTime < 1f / mouseMessagesPerSecond)
                    return;

                if (Input.GetMouseButtonDown(0))
                {
                    // Check if the initial press was inside the exclusion area first
                    if (MouseExclusionArea != null)
                    {
                        Vector2 screenMousePos = Input.mousePosition;
                        if (
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                MouseExclusionArea,
                                screenMousePos,
                                null,
                                out Vector2 localPoint
                            )
                        )
                        {
                            if (IsInsideCanvas(localPoint, MouseExclusionArea))
                            {
                                _shouldStreamMouse = false;
                                Debug.Log("Mouse press detected in exclusion area - ignoring");
                                return;
                            }
                        }
                    }

                    // If not in exclusion area, check if inside desktop area
                    RectTransform desktopRect = StudentDesktopImage.rectTransform;
                    if (desktopRect != null)
                    {
                        Vector2 screenMousePos = Input.mousePosition;
                        if (
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                desktopRect,
                                screenMousePos,
                                null,
                                out Vector2 localPoint
                            )
                        )
                        {
                            bool insideDesktop = IsInsideCanvas(localPoint, desktopRect);
                            _shouldStreamMouse = insideDesktop;
                            Debug.Log(
                                $"Mouse press detected - inside desktop: {insideDesktop}, streaming: {_shouldStreamMouse}"
                            );
                        }
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (_shouldStreamMouse)
                    {
                        StopMouseStreaming();
                    }
                }

                // Only process movement if we should be streaming
                if (_shouldStreamMouse)
                {
                    RectTransform canvasRect = StudentDesktopImage.rectTransform;
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
                                StreamMessage("CURSOR_POS:" + mouseData);
                                _lastMouseMessageTime = Time.time;
                                return;
                            }
                            else
                            {
                                // Mouse has left the canvas area while still pressed
                                StopMouseStreaming();
                            }
                        }
                    }
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                StopMouseStreaming();
            }

            private void StopMouseStreaming()
            {
                if (_shouldStreamMouse)
                {
                    Debug.Log("Sending stop streaming signal (-1,-1).");
                    StreamMessage("CURSOR_POS:-1,-1");
                    _shouldStreamMouse = false;
                }
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                if (!hasFocus)
                {
                    StopMouseStreaming();
                }
            }

            private void OnApplicationPause(bool isPaused)
            {
                if (isPaused)
                {
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
                    Debug.Log("Don't send empty message!");
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

            #endregion

            #region -- Agora Event ---

            internal class UserEventHandler : IRtcEngineEventHandler
            {
                private readonly TeacherAgoraScript_NewUI _desktopScreenShare;

                internal UserEventHandler(TeacherAgoraScript_NewUI desktopScreenShare)
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
                }

                public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
                {
                    Debug.Log("OnRejoinChannelSuccess");
                }

                public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
                {
                    Debug.Log("OnLeaveChannel");
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

                    // Check if this is the student desktop stream
                    if (uid == _desktopScreenShare.UidStudentDesktop)
                    {
                        Debug.Log(
                            $"[DEBUG] Student desktop stream joined with UID: {uid} - initializing desktop VideoSurface"
                        );

                        if (
                            _desktopScreenShare.StudentDesktopImage != null
                            && _desktopScreenShare._studentDesktopVideoSurface != null
                        )
                        {
                            _desktopScreenShare._studentDesktopVideoSurface.SetForUser(
                                uid,
                                _desktopScreenShare.GetChannelName(),
                                VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                            );
                            _desktopScreenShare._studentDesktopVideoSurface.SetEnable(true);

                            // Fix for desktop stream being flipped horizontally - apply horizontal flip
                            RectTransform desktopRectTransform =
                                _desktopScreenShare.StudentDesktopImage.GetComponent<RectTransform>();
                            if (desktopRectTransform != null)
                            {
                                desktopRectTransform.localScale = new Vector3(1, -1, 1);
                            }
                        }
                        else
                        {
                            Debug.LogError("StudentDesktopImage or its VideoSurface is null");
                        }
                    }
                    // Check if this is the student avatar stream
                    else if (uid == _desktopScreenShare.UidStudentAvatar)
                    {
                        Debug.Log(
                            $"[DEBUG] Student avatar stream joined with UID: {uid} - initializing avatar VideoSurface and orientation"
                        );

                        if (
                            _desktopScreenShare.StudentAvatarImage != null
                            && _desktopScreenShare._studentAvatarVideoSurface != null
                        )
                        {
                            _desktopScreenShare._studentAvatarVideoSurface.SetForUser(
                                uid,
                                _desktopScreenShare.GetChannelName(),
                                VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE
                            );
                            _desktopScreenShare._studentAvatarVideoSurface.SetEnable(true);

                            // Fix for avatar stream being flipped vertically - apply vertical flip
                            RectTransform avatarRectTransform =
                                _desktopScreenShare.StudentAvatarImage.GetComponent<RectTransform>();
                            if (avatarRectTransform != null)
                            {
                                avatarRectTransform.localScale = new Vector3(-1, 1, 1);
                            }
                        }
                        else
                        {
                            Debug.LogError("StudentAvatarImage or its VideoSurface is null");
                        }
                    }
                    else
                    {
                        Debug.Log(
                            "Ignoring stream with UID: " + uid + " (not matching configured UIDs)"
                        );
                    }
                }

                public override void OnUserOffline(
                    RtcConnection connection,
                    uint uid,
                    USER_OFFLINE_REASON_TYPE reason
                )
                {
                    Debug.Log(
                        string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason)
                    );

                    // Check if this is the student desktop stream
                    if (uid == _desktopScreenShare.UidStudentDesktop)
                    {
                        Debug.Log("Student desktop stream went offline");
                        if (_desktopScreenShare._studentDesktopVideoSurface != null)
                        {
                            _desktopScreenShare._studentDesktopVideoSurface.SetEnable(false);
                        }
                    }
                    // Check if this is the student avatar stream
                    else if (uid == _desktopScreenShare.UidStudentAvatar)
                    {
                        Debug.Log("Student avatar stream went offline");
                        if (_desktopScreenShare._studentAvatarVideoSurface != null)
                        {
                            _desktopScreenShare._studentAvatarVideoSurface.SetEnable(false);
                        }
                    }
                }
            }

            #endregion

            // Add this method to the TeacherAgoraScript_NewUI class
            public bool SendChatMessage(string message)
            {
                if (string.IsNullOrEmpty(message))
                    return false;

                try
                {
                    // Create the chat message object
                    ChatMessage msg = new ChatMessage
                    {
                        timestamp = DateTime.UtcNow.ToString("o"),
                        content = message,
                    };

                    // Serialize to JSON
                    string json = JsonUtility.ToJson(msg);

                    // Prepend the chat prefix
                    string prefixedMessage = "CHAT_MSG:" + json;

                    // Use the existing StreamMessage method
                    StreamMessage(prefixedMessage);
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[TeacherAgoraScript_NewUI] Error sending chat: " + e.Message);
                    return false;
                }
            }
        }

        // Add this class definition at the bottom of the file, outside the class
        [System.Serializable]
        public class ChatMessage
        {
            public string timestamp; // ISO‐8601 string
            public string content; // the chat text
        }
    }
}
