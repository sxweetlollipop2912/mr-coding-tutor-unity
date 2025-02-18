using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace io.agora.rtc.demo
{
    [CreateAssetMenu(
        menuName = "Agora/AgoraStudentInput",
        fileName = "AgoraStudentInput",
        order = 1
    )]
    [Serializable]
    public class AgoraStudentInput : ScriptableObject
    {
        [FormerlySerializedAs("APP_ID")]
        [SerializeField]
        public string appID = "";

        [FormerlySerializedAs("TOKEN")]
        [SerializeField]
        public string token = "";

        [FormerlySerializedAs("CHANNEL_NAME")]
        [SerializeField]
        public string channelName = "YOUR_CHANNEL_NAME";
    }
}
