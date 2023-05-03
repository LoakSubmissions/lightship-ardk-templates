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

        public string username = null;
        [SerializeField] private string roomPrefix = "LoakTemplate";
        public int roomCap { get; private set; } = 5;
        public string roomCode { get; private set; } = null;
        public Dictionary<Guid, Player> connectedPlayers { get; private set; } = new Dictionary<Guid, Player>();

        private Canvas canvas;
        private GameObject modeSelectView;
        private GameObject multiplayerView;
        private GameObject joinView;
        private TMP_InputField joinInput;
        private GameObject lobbyView;
        private TMP_Text lobbyCode;
        private Transform lobbyListParent;
        private GameObject lobbyListPrefab;
        private Dictionary<Guid, GameObject> lobbyListItems = new Dictionary<Guid, GameObject>();

        private LoakSessionManager seshMan;
        private bool creating = false;

        public class Player
        {
            public Guid identifier;
            public string username;

            public Player(IPeer peer)
            {
                identifier = peer.Identifier;
                username = null;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            seshMan = GetComponent<LoakSessionManager>();

            canvas = GetComponentInChildren<Canvas>(true);
            modeSelectView = canvas.transform.GetChild(0).gameObject;
            multiplayerView = canvas.transform.GetChild(1).gameObject;
            joinView = canvas.transform.GetChild(2).gameObject;
            joinInput = joinView.GetComponentInChildren<TMP_InputField>(true);
            lobbyView = canvas.transform.GetChild(3).gameObject;
            lobbyCode = lobbyView.transform.GetChild(3).GetComponentsInChildren<TMP_Text>(true)[1];
            lobbyListParent = lobbyView.transform.GetChild(3).GetChild(2);
            lobbyListPrefab = lobbyListParent.GetChild(1).gameObject;
        }

        private string GenerateRoomCode()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 4);
        }

        public void SetRoomCode(string code)
        {
            roomCode = code;
            lobbyCode.text = roomCode;
        }

        public void Back()
        {
            if (multiplayerView.activeSelf)
            {
                multiplayerView.SetActive(false);
                modeSelectView.SetActive(true);
            }
            else if (joinView.activeSelf)
            {
                joinView.SetActive(false);
                multiplayerView.SetActive(true);
            }
            else if (lobbyView.activeSelf)
            {
                lobbyView.SetActive(false);
                seshMan.LeaveSession();
                multiplayerView.SetActive(true);
            }
        }

        public void PlayWithFriends()
        {
            modeSelectView.SetActive(false);
            multiplayerView.SetActive(true);
        }

        public void PlaySolo()
        {
            seshMan.StartSoloSession();
            canvas.enabled = false;
        }

        public void CreateRoom()
        {
            SetRoomCode(GenerateRoomCode());
            creating = true;
            seshMan.JoinSession(roomPrefix + roomCode);
        }

        public void JoinRoom()
        {
            multiplayerView.SetActive(false);
            joinInput.text = "";
            joinView.SetActive(true);
        }

        public void SubmitCode()
        {
            if (joinInput.text == null || joinInput.text == "")
                return;

            SetRoomCode(joinInput.text);
            joinInput.text = "";
            
            creating = false;
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

            seshMan.SendToHost(0, username);

            multiplayerView.SetActive(false);
            joinView.SetActive(false);
            lobbyView.SetActive(true);

            lobbyCode.text = roomCode;
        }

        public void StartRoom()
        {
            seshMan.StartMultiplayerSession();
        }

        public void OnRoomStarted()
        {
            canvas.enabled = false;
        }

        public void OnPlayerJoined(IPeer peer)
        {
            if (!seshMan.IsHost)
                return;

            // TODO: Validate and accept or reject join.
        }

        public void OnPlayerLeft(IPeer player)
        {
            Destroy(lobbyListItems[player.Identifier]);
            lobbyListItems.Remove(player.Identifier);
        }

        public void OnDataRecieved(uint tag, Guid sender, object data)
        {
            switch (tag)
            {
                case 0:
                    var username = (string)data;
                    var myEntry = Instantiate(lobbyListPrefab, lobbyListParent);
                    lobbyListItems.Add(sender, myEntry);
                    connectedPlayers[sender].username = username;

                    if (username == null)
                        username = $"Player {connectedPlayers.Count}";

                    myEntry.transform.GetChild(1).GetComponentInChildren<TMP_Text>(true).text = username.Substring(0, 1);
                    myEntry.transform.GetChild(2).GetComponent<TMP_Text>().text = username;

                    if (seshMan.IsHost)
                        seshMan.SendToAll(0, sender, username);
                    
                    break;
            }
        }
    }
}
