using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loak.Unity
{
    public class LoakRoomManagement : MonoBehaviour
    {
        public static LoakRoomManagement Instance;

        public string username = null;
        [SerializeField] private string roomPrefix = "LoakTemplate";
        public int roomCap = 5;
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
        private Button lobbyPlayButton;

        private LoakSessionManager seshMan;
        private bool creating = false;

        public class Player
        {
            public Guid identifier;
            public string username;

            public Player(Guid identifier, string username)
            {
                this.identifier = identifier;
                this.username = username;
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
            lobbyPlayButton = lobbyView.GetComponentInChildren<Button>(true);
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
                connectedPlayers.Clear();

                foreach (var item in lobbyListItems.Values)
                {
                    Destroy(item);
                }

                lobbyListItems.Clear();
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
                seshMan.LeaveSession();
                roomCode = null;
                return;
            }
            else if (!creating && seshMan.IsHost)
            {
                // TODO: Display failed to join error
                seshMan.LeaveSession();
                roomCode = null;
                return;
            }

            if (seshMan.IsHost)
                JoinAccepted();
        }

        private void JoinAccepted()
        {
            seshMan.SendToHost(0, username);

            multiplayerView.SetActive(false);
            joinView.SetActive(false);
            lobbyView.SetActive(true);

            lobbyPlayButton.interactable = seshMan.IsHost;
            if (!seshMan.IsHost)
                lobbyPlayButton.GetComponentInChildren<TMP_Text>().text = "Waiting for Host...";

            lobbyCode.text = roomCode;
        }

        public void StartRoom()
        {
            seshMan.StartMultiplayerSession();
            seshMan.SendToAll(3, seshMan.me.Identifier, null);
        }

        public void OnRoomStarted()
        {
            canvas.enabled = false;
        }

        public void OnPlayerJoined(IPeer peer)
        {
            if (!seshMan.IsHost)
                return;

            bool accepted = true;

            if (connectedPlayers.Count >= roomCap)
                accepted = false;

            if (seshMan.sessionBegan)
                accepted = false;

            seshMan.SendToPeer(2, peer, accepted);

            if (!accepted)
                return;

            (List<Guid>, List<string>) payload = (new List<Guid>(), new List<string>());

            foreach (KeyValuePair<Guid, Player> pair in connectedPlayers)
            {
                payload.Item1.Add(pair.Key);
                payload.Item2.Add(pair.Value.username);
            }

            seshMan.SendToPeer(1, peer, payload);
        }

        public void OnPlayerLeft(IPeer peer)
        {
            if (!connectedPlayers.ContainsKey(peer.Identifier))
                return;

            Destroy(lobbyListItems[peer.Identifier]);
            lobbyListItems.Remove(peer.Identifier);
            connectedPlayers.Remove(peer.Identifier);
        }

        public void OnDataRecieved(uint tag, Guid sender, object[] data)
        {
            switch (tag)
            {
                case 0:
                    var username = (string)data[0];
                    var newEntry = Instantiate(lobbyListPrefab, lobbyListParent);
                    lobbyListItems.Add(sender, newEntry);
                    connectedPlayers[sender] = new Player(sender, username);

                    if (username == null || username == "")
                        username = $"Player {connectedPlayers.Count}";

                    newEntry.transform.GetChild(1).GetComponentInChildren<TMP_Text>(true).text = username.Substring(0, 1);
                    newEntry.transform.GetChild(2).GetComponent<TMP_Text>().text = username;
                    newEntry.SetActive(true);

                    if (seshMan.IsHost)
                        seshMan.SendToAll(0, sender, username);
                    
                    break;

                case 1:
                    var players = ((Guid[])data[0], (string[])data[1]);

                    for (int i = 0; i < players.Item1.Length; i++)
                    {
                        username = players.Item2[i];
                        newEntry = Instantiate(lobbyListPrefab, lobbyListParent);
                        lobbyListItems.Add(players.Item1[i], newEntry);
                        connectedPlayers[players.Item1[i]] = new Player(players.Item1[i], username);

                        if (username == null || username == "")
                            username = $"Player {connectedPlayers.Count}";

                        newEntry.transform.GetChild(1).GetComponentInChildren<TMP_Text>(true).text = username.Substring(0, 1);
                        newEntry.transform.GetChild(2).GetComponent<TMP_Text>().text = username;
                        newEntry.SetActive(true);
                    }

                    break;

                case 2:
                    bool canJoin = (bool)data[0];

                    if (canJoin)
                    {
                        JoinAccepted();
                    }
                    else
                    {
                        // TODO: Display join rejected error.
                        seshMan.LeaveSession();
                        roomCode = null;
                    }

                    break;

                case 3:
                    if (!seshMan.IsHost)
                        seshMan.StartMultiplayerSession();
                    break;
            }
        }
    }
}
