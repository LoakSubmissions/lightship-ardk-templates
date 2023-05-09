using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

        [Tooltip("Should be set to the name of your game.")]
        [SerializeField] private string roomPrefix = "LoakTemplate";
        [Tooltip("Max number of users per room. Lightship has problems with more than 5.")]
        public int roomCap = 5;

        [HideInInspector] public string username = null; // Username of the local player.
        public string roomCode { get; private set; } = null; // Unique code for matching players to a room.
        public Dictionary<Guid, Player> connectedPlayers { get; private set; } = new Dictionary<Guid, Player>(); // Collection of all players currently in the room.

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
        private GameObject localizeView;

        private LoakSessionManager seshMan;
        private bool creating = false;

        // Nested class that just represents a connected player.
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

        // Just sets up the Singleton reference.
        void Awake()
        {
            Instance = this;
        }

        // Grabs all UI references.
        void Start()
        {
            seshMan = LoakSessionManager.Instance;

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
            localizeView = canvas.transform.GetChild(4).gameObject;
        }

        /// <summary>
        /// Generates an alphanumeric 6 digit code.
        /// </summary>
        public string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            var result = new StringBuilder(6);

            for (int i = 0; i < 6; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Sets the room code used when joining a session. Is overridden by editing the inputField and by CreateRoom.
        /// </summary>
        /// <param name="code">What to set the room code to.</param>
        public void SetRoomCode(string code)
        {
            roomCode = code.ToUpper();
            lobbyCode.text = roomCode;
        }

        /// <summary>
        /// Regresses back through the menu.
        /// </summary>
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
            else if (localizeView.activeSelf)
            {
                localizeView.SetActive(false);
                seshMan.LeaveSession();
                connectedPlayers.Clear();

                foreach (var item in lobbyListItems.Values)
                {
                    Destroy(item);
                }

                lobbyListItems.Clear();
                multiplayerView.SetActive(true);

                seshMan.StartSoloSession();
            }
        }

        /// <summary>
        /// Progresses from mode select to multiplayer view.
        /// </summary>
        public void PlayWithFriends()
        {
            modeSelectView.SetActive(false);
            multiplayerView.SetActive(true);
        }

        /// <summary>
        /// Starts a solo AR session.
        /// </summary>
        public void PlaySolo()
        {
            seshMan.StartSoloSession();
            canvas.enabled = false;
        }

        /// <summary>
        /// Attempts to create and join a new room with a random room code.
        /// </summary>
        public void CreateRoom()
        {
            SetRoomCode(GenerateRoomCode());
            creating = true;
            seshMan.JoinSession(roomPrefix + roomCode);
        }

        /// <summary>
        /// Progresses from multiplayer to join view.
        /// </summary>
        public void JoinRoom()
        {
            multiplayerView.SetActive(false);
            joinInput.text = "";
            joinView.SetActive(true);
        }

        /// <summary>
        /// Attempts to join an existing room with roomCode.
        /// </summary>
        public void SubmitCode()
        {
            if (joinInput.text == null || joinInput.text == "")
                return;

            SetRoomCode(joinInput.text);
            joinInput.text = "";
            
            creating = false;
            seshMan.JoinSession(roomPrefix + roomCode);
        }

        // Called when we connect to a room.
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

        // Called when we are confirmed to join the room. Sets up lobby and sends username to host.
        private void JoinAccepted()
        {
            seshMan.SendToHost(0, new object[] {username});

            multiplayerView.SetActive(false);
            joinView.SetActive(false);
            lobbyView.SetActive(true);

            lobbyPlayButton.interactable = seshMan.IsHost;
            if (!seshMan.IsHost)
                lobbyPlayButton.GetComponentInChildren<TMP_Text>().text = "Waiting for Host...";

            lobbyCode.text = roomCode;
        }

        /// <summary>
        /// Starts localization and AR for all joined peers.
        /// </summary>
        public void StartRoom()
        {
            seshMan.StartMultiplayerSession();
            seshMan.SendToAll(3, seshMan.me.Identifier, null);
        }

        // Called when the host successfully starts the room.
        public void OnRoomStarted()
        {
            lobbyView.SetActive(false);
            localizeView.SetActive(true);
        }

        // Called when the room finishes localizing.
        public void OnRoomLocalized()
        {
            canvas.enabled = false;
        }

        // Called when another player attempts to join the room. Validates and accepts or rejects the join request.
        public void OnPlayerJoined(IPeer peer)
        {
            if (!seshMan.IsHost)
                return;

            bool accepted = true;

            if (connectedPlayers.Count >= roomCap)
                accepted = false;

            if (seshMan.sessionBegan)
                accepted = false;

            seshMan.SendToPeer(2, peer, new object[] {accepted});

            if (!accepted)
                return;

            List<Guid> idList = new List<Guid>();
            List<string> nameList = new List<string>();

            foreach (KeyValuePair<Guid, Player> pair in connectedPlayers)
            {
                idList.Add(pair.Key);
                nameList.Add(pair.Value.username);
            }

            seshMan.SendToPeer(1, peer, new object[] {idList.ToArray(), nameList.ToArray()});
        }

        // Called when a player leaves the room. Clears their name from lobby list.
        public void OnPlayerLeft(IPeer peer)
        {
            if (!connectedPlayers.ContainsKey(peer.Identifier))
                return;

            Destroy(lobbyListItems[peer.Identifier]);
            lobbyListItems.Remove(peer.Identifier);
            connectedPlayers.Remove(peer.Identifier);
        }

        // Called when we recieve data. Parses and acts on the data based on the tag.
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
                        seshMan.SendToAll(0, sender, new object[] {username});
                    
                    break;

                case 1:
                    var idList = (Guid[])data[0];
                    var nameList = (string[])data[1];

                    for (int i = 0; i < idList.Length; i++)
                    {
                        username = nameList[i];
                        newEntry = Instantiate(lobbyListPrefab, lobbyListParent);
                        lobbyListItems.Add(idList[i], newEntry);
                        connectedPlayers[idList[i]] = new Player(idList[i], username);

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
