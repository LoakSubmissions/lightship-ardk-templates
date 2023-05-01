using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using UnityEngine;

namespace Loak.Unity
{
    public class LoakRoomManagement : MonoBehaviour
    {
        public static LoakRoomManagement Instance;

        [SerializeField] private string roomPrefix = "LoakTemplate";
        public int roomCap { get; private set; } = 5;
        public int usersConnected { get; private set; } = 0;
        public string roomCode { get; private set; } = null;
        public List<IPeer> connectedUsers { get; private set; } = new List<IPeer>();

        private LoakSessionManager seshMan;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            seshMan = GetComponent<LoakSessionManager>();
        }

        public string GenerateRoomCode()
        {
            roomCode = Guid.NewGuid().ToString("n").Substring(0, 4);
            return roomCode;
        }

        public void SetRoomCode(string code)
        {
            roomCode = code;
        }

        public void JoinRoom()
        {
            if (roomCode == null)
                return;

            seshMan.TryJoinSession(roomCode);
        }

        public void LeaveRoom()
        {
            
        }

        public void StartRoom()
        {
            seshMan.TryStartSession();
        }
    }
}
