using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using TMPro;
using UnityEngine;

namespace Loak.Unity
{
    public class LoakRoomManagement : MonoBehaviour
    {
        public static LoakRoomManagement Instance;

        [SerializeField] private string roomPrefix = "LoakTemplate";
        public int roomCap { get; private set; } = 5;
        public string roomCode { get; private set; } = null;
        public List<string> connectedPlayers { get; private set; } = new List<string>();

        private Canvas canvas;
        private GameObject joinView;
        private TMP_InputField joinInput;
        private GameObject lobbyView;
        private TMP_Text lobbyCode;
        private TMP_Text lobbyList;

        private LoakSessionManager seshMan;
        private bool creating = false;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            seshMan = GetComponent<LoakSessionManager>();

            canvas = GetComponentInChildren<Canvas>(true);
            joinView = canvas.transform.GetChild(0).gameObject;
            joinInput = joinView.GetComponentInChildren<TMP_InputField>(true);
            lobbyView = canvas.transform.GetChild(1).gameObject;
            lobbyCode = lobbyView.GetComponentsInChildren<TMP_Text>(true)[0];
            lobbyList = lobbyView.GetComponentsInChildren<TMP_Text>(true)[1];
        }

        private string GenerateRoomCode()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 4);
        }

        public void SetRoomCode(string code)
        {
            roomCode = code;
        }

        public void JoinRoom()
        {
            if (joinInput.text == null || joinInput.text == "")
                return;

            roomCode = joinInput.text;
            joinInput.text = "";
            
            creating = false;
            seshMan.JoinSession(roomPrefix + roomCode);
        }

        public void CreateRoom()
        {
            roomCode = GenerateRoomCode();
            joinInput.text = "";
            
            creating = true;
            seshMan.JoinSession(roomPrefix + roomCode);
        }

        public void OnRoomJoined()
        {
            if (creating && !seshMan.IsHost)
            {
                // TODO: Display failed to create error
                // seshMan.LeaveSession();
                roomCode = null;
                return;
            }
            else if (!creating && seshMan.IsHost)
            {
                // TODO: Display failed to join error
                // seshMan.LeaveSession();
                roomCode = null;
                return;
            }

            joinView.SetActive(false);
            lobbyView.SetActive(true);

            lobbyCode.text = roomCode;
            connectedPlayers.Add("You");
            lobbyList.text = "You";
        }

        public void StartRoom()
        {
            seshMan.StartSession();
        }

        public void OnRoomStarted()
        {
            canvas.enabled = false;
        }

        public void OnPlayerJoined(IPeer player)
        {
            connectedPlayers.Add(player.Identifier.ToString());
            lobbyList.text = String.Join('\n', connectedPlayers.ToArray());
        }

        public void OnPlayerLeft(IPeer player)
        {
            connectedPlayers.Remove(player.Identifier.ToString());
            lobbyList.text = String.Join('\n', connectedPlayers.ToArray());
        }
    }
}
