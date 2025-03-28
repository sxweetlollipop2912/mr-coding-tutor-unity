﻿using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Agora.Rtc;


using System.Collections.Generic;
using io.agora.rtc.demo;


namespace Agora_RTC_Plugin.API_Example.Examples.Basic.RenderWithYUVSample
{
    public class RenderWithYUVSample : MonoBehaviour
    {
        [FormerlySerializedAs("appIdInput")]
        [SerializeField]
        private AppIdInput _appIdInput;

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

        public Text LogText;
        internal Logger Log;
        internal IRtcEngine RtcEngine = null;

        public Toggle YUVToggle;
        public Toggle PlaneToggle;


        // Use this for initialization
        private void Start()
        {

            LoadAssetData();
            if (CheckAppId())
            {
                InitEngine();
                SetBasicConfiguration();

                var shader = Shader.Find("Unlit/Texture");
                if (shader == null)
                {
                    Debug.Log("It looks like Unlit/Texture shader not be packaged in this project. May be some videosurface will look like pink");
                }
            }
        }

        // Update is called once per frame
        private void Update()
        {
            PermissionHelper.RequestMicrophonePermission();
            PermissionHelper.RequestCameraPermission();
        }

        //Show data in AgoraBasicProfile
        [ContextMenu("ShowAgoraBasicProfileData")]
        private void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID;
            _token = _appIdInput.token;
            _channelName = _appIdInput.channelName;
        }

        private bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
        }

        private void InitEngine()
        {
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            RtcEngineContext context = new RtcEngineContext();
            context.appId = _appID;
            context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
            context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT;
            context.areaCode = AREA_CODE.AREA_CODE_GLOB;
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(handler);
        }

        private void SetBasicConfiguration()
        {
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo();
            VideoEncoderConfiguration config = new VideoEncoderConfiguration();
            config.dimensions = new VideoDimensions(640, 360);
            config.frameRate = 15;
            config.bitrate = 0;
            RtcEngine.SetVideoEncoderConfiguration(config);
            RtcEngine.SetChannelProfile(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION);
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        }

        #region -- Button Events ---

        public void JoinChannel()
        {
            RtcEngine.JoinChannel(_token, _channelName, "", 0);
        }

        public void LeaveChannel()
        {
            RtcEngine.LeaveChannel();
        }

        public void AdjustVideoEncodedConfiguration640()
        {
            VideoEncoderConfiguration config = new VideoEncoderConfiguration();
            config.dimensions = new VideoDimensions(640, 360);
            config.frameRate = 15;
            config.bitrate = 0;
            RtcEngine.SetVideoEncoderConfiguration(config);
        }

        public void AdjustVideoEncodedConfiguration480()
        {
            VideoEncoderConfiguration config = new VideoEncoderConfiguration();
            config.dimensions = new VideoDimensions(480, 480);
            config.frameRate = 15;
            config.bitrate = 0;
            RtcEngine.SetVideoEncoderConfiguration(config);
        }

        #endregion

        private void OnDestroy()
        {
            Debug.Log("OnDestroy");
            if (RtcEngine == null) return;
            RtcEngine.InitEventHandler(null);
            RtcEngine.LeaveChannel();
            RtcEngine.Dispose();
        }

        internal string GetChannelName()
        {
            return _channelName;
        }

        #region -- Video Render UI Logic ---

        internal void MakeVideoView(uint uid, string channelId = "", bool useYUV = false, bool usePlane = false)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                return; // reuse
            }

            VideoSurface videoSurface = null;

            if (usePlane)
                videoSurface = MakePlaneSurface(uid.ToString(), useYUV);
            else
                videoSurface = MakeImageSurface(uid.ToString(), useYUV);

            if (ReferenceEquals(videoSurface, null)) return;
            // configure videoSurface
            if (uid == 0)
            {
                videoSurface.SetForUser(uid, channelId);
            }
            else
            {
                videoSurface.SetForUser(uid, channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            }

            videoSurface.OnTextureSizeModify += (int width, int height) =>
            {
                if (usePlane)
                {
                    //If render in MeshRenderer, just set localSize with MeshRenderer
                    float scale = (float)height / (float)width;
                    videoSurface.transform.localScale = new Vector3(-30, 30, 30 * scale);
                }
                else
                {
                    //If render in RawImage. just set rawImage size.
                    RectTransform rectTransform = videoSurface.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(width / 2, height / 2);
                    rectTransform.localScale = Vector3.one;
                }

                Debug.Log("OnTextureSizeModify: " + width + "  " + height);
            };

            videoSurface.SetEnable(true);
        }

        // VIDEO TYPE 1: 3D Object
        private VideoSurface MakePlaneSurface(string goName, bool useYUV = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // set up transform
            go.AddComponent<UIElementDrag>();

            var father = GameObject.Find("3DParent");
            if (father != null)
            {
                go.transform.SetParent(father.transform);
                go.transform.Rotate(-90.0f, 0.0f, 0.0f);
                var random = new System.Random();
                go.transform.position = new Vector3(random.Next(-100, 100), random.Next(-100, 100), 0);
                go.transform.localScale = new Vector3(10, 10, 10);
            }

            var meshRenderer = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Unlit/Texture");
            if (shader != null)
            {
                meshRenderer.material = new Material(shader);
            }
            else
            {
                Log.UpdateLog("It looks like Unlit/Texture shader not include Always Includes Shaders. May be some videosurface will be pink");
            }

            // configure videoSurface
            VideoSurface videoSurface = null;
            if (useYUV)
                videoSurface = go.AddComponent<VideoSurfaceYUV>();
            else
                videoSurface = go.AddComponent<VideoSurface>();

            return videoSurface;
        }

        // Video TYPE 2: RawImage
        private VideoSurface MakeImageSurface(string goName, bool useYUV = false)
        {
            GameObject go = new GameObject();

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // to be renderered onto
            go.AddComponent<RawImage>();
            // make the object draggable
            go.AddComponent<UIElementDrag>();
            var canvas = GameObject.Find("VideoCanvas");
            if (canvas != null)
            {
                go.transform.SetParent(canvas.transform);
                Debug.Log("add video view");
            }
            else
            {
                Debug.Log("Canvas is null video view");
            }

            // set up transform
            go.transform.Rotate(0f, 0.0f, 180.0f);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(2f, 3f, 1f);

            // configure videoSurface
            VideoSurface videoSurface = null;
            if (useYUV)
                videoSurface = go.AddComponent<VideoSurfaceYUV>();
            else
                videoSurface = go.AddComponent<VideoSurface>();

            return videoSurface;
        }

        internal static void DestroyVideoView(uint uid)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                Destroy(go);
            }
        }

        # endregion
    }

    #region -- Agora Event ---

    internal class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly RenderWithYUVSample _sample;

        private HashSet<uint> usersInChannel = new HashSet<uint>();

        internal UserEventHandler(RenderWithYUVSample videoSample)
        {
            _sample = videoSample;
        }

        public override void OnError(int err, string msg)
        {
            _sample.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            int build = 0;
            Debug.Log("Agora: OnJoinChannelSuccess ");
            _sample.Log.UpdateLog(string.Format("sdk version: ${0}",
                _sample.RtcEngine.GetVersion(ref build)));
            _sample.Log.UpdateLog(string.Format("sdk build: ${0}",
              build));
            _sample.Log.UpdateLog(
                string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                                connection.channelId, connection.localUid, elapsed));

            _sample.MakeVideoView(0, "", _sample.YUVToggle.isOn, _sample.PlaneToggle.isOn);
        }

        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            _sample.Log.UpdateLog("OnRejoinChannelSuccess");
        }

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
        {
            _sample.Log.UpdateLog("OnLeaveChannel");
            RenderWithYUVSample.DestroyVideoView(0);
            foreach (var uid in this.usersInChannel)
            {
                RenderWithYUVSample.DestroyVideoView(uid);
            }
            this.usersInChannel.Clear();

        }

        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole, CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions)
        {
            _sample.Log.UpdateLog("OnClientRoleChanged");
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _sample.Log.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
            _sample.MakeVideoView(uid, _sample.GetChannelName(), _sample.YUVToggle.isOn, _sample.PlaneToggle.isOn);
            this.usersInChannel.Add(uid);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _sample.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
                (int)reason));
            RenderWithYUVSample.DestroyVideoView(uid);
            this.usersInChannel.Remove(uid);
        }
    }

    # endregion
}
