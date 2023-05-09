using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.AR;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.AR.Configuration;
using System.IO;
using Niantic.ARDK.Utilities.BinarySerialization;
using System;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;

namespace Loak.Unity
{
    public class LoakSessionManager : MonoBehaviour
    {
        public static LoakSessionManager Instance;

        [Tooltip("True if you want the AR camera to turn on immediately.")]
        public bool arOnStart = false;

        [Tooltip("Invoked when a multiplayer networking session is connected to.")]
        public UnityEvent OnSessionJoined;
        [Tooltip("Invoked when the AR session is started.")]
        public UnityEvent OnSessionStarted;
        [Tooltip("Invoked when the multiplayer AR session finishes localizing.")]
        public UnityEvent OnSessionLocalized;
        [Tooltip("Invoked when a user joins the multiplayer networking session.")]
        public UnityEvent<IPeer> OnPeerJoined;
        [Tooltip("Invoked when a user leaves the multiplayer networking session.")]
        public UnityEvent<IPeer> OnPeerLeft;
        [Tooltip("Invoked when we recieve data from a peer.")]
        public UnityEvent<uint, Guid, object[]> OnDataRecieved;

        [HideInInspector] public bool IsHost = false;
        [HideInInspector] public IPeer me;
        [HideInInspector] public bool sessionBegan = false;

        private string sessionIdentifier;
        private IMultipeerNetworking networking;
        private IARSession arSession;
        private IARNetworking arNetworking;
        private IARWorldTrackingConfiguration configuration;
        private PeerState prevState;

        // Just sets up the singleton reference.
        void Awake()
        {
            Instance = this;
        }

        // Initializes session objects and starts AR if arOnStart is true.
        void Start()
        {
            Initialize();

            if (arOnStart)
            {
                configuration.IsSharedExperienceEnabled = false;
                arSession.Run(configuration);
            }
        }

        /// <summary>
        /// Creates and initializes the session objects.
        /// </summary>
        private void Initialize()
        {
            sessionBegan = false;

            // If we don't have a networking object, create one.
            if (networking == null)
            {
                // Dispose old arNetworking if one exists.
                if (arNetworking != null)
                {
                    arNetworking.Dispose();
                    arNetworking = null;
                }

                // Create the session object and subscribe all necessary event listeners.
                networking = MultipeerNetworkingFactory.Create(arSession == null ? default : arSession.StageIdentifier);
                networking.Connected += OnConnected;
                networking.PeerAdded += OnPeerAdded;
                networking.PeerRemoved += OnPeerRemoved;
                networking.PeerDataReceived += OnPeerDataRecieved;
            }

            // If we don't have an ar session object, create one.
            if (arSession == null)
            {
                // Dispose old arNetworking if one exists.
                if (arNetworking != null)
                {
                    arNetworking.Dispose();
                    arNetworking = null;
                }

                arSession = ARSessionFactory.Create(networking.StageIdentifier);
            }

            // Create new arNetworking object if anything changed.
            if (arNetworking == null)
            {
                arNetworking = ARNetworkingFactory.Create(arSession, networking);
                arNetworking.PeerStateReceived += OnPeerStateReceived;
            }

            // If we haven't already created a configuration, create it and set correct settings.
            if (configuration == null)
            {
                configuration = ARWorldTrackingConfigurationFactory.Create();
                configuration.IsAutoFocusEnabled = true;
                configuration.IsLightEstimationEnabled = true;
            }
        }

        /// <summary>
        /// Joins a multiplayer networking session.
        /// </summary>
        /// <param name="sessionIdentifier">A string identifier for matching players together.</param>
        public void JoinSession(string sessionIdentifier)
        {
            this.sessionIdentifier = sessionIdentifier;

            networking.Join(Encoding.UTF8.GetBytes(sessionIdentifier));
        }

        // Called when we connect successfully. Sets IsHost and me, then invokes the public event.
        private void OnConnected(ConnectedArgs args)
        {
            IsHost = args.IsHost;
            me = networking.Self;
            OnSessionJoined.Invoke();
        }

        /// <summary>
        /// Leaves the current multiplayer session and reinitializes all objects.
        /// </summary>
        public void LeaveSession()
        {
            if (networking.IsConnected)
            {
                sessionIdentifier = null;
                networking.Leave();
                networking.Dispose();
                networking = null;
            }

            if (arSession.State == ARSessionState.Running)
            {
                arSession.Dispose();
                arSession = null;
            }

            Initialize();
        }

        /// <summary>
        /// Starts a normal AR session without localization or networking.
        /// </summary>
        public void StartSoloSession()
        {
            if (networking.IsConnected)
            {
                LeaveSession();
            }

            configuration.IsSharedExperienceEnabled = false;
            arSession.Run(configuration);
            OnSessionStarted.Invoke();
        }

        /// <summary>
        /// Starts a multiplayer AR session and begins localization.
        /// </summary>
        public void StartMultiplayerSession()
        {
            if (!networking.IsConnected)
                return;

            if (arSession.State == ARSessionState.Running || arSession.State == ARSessionState.Paused)
            {
                arSession.Dispose();
                arSession = null;
                Initialize();
            }

            configuration.IsSharedExperienceEnabled = true;
            arSession.Run(configuration, ARSessionRunOptions.None);
            OnSessionStarted.Invoke();
            sessionBegan = true;
        }

        // Called when a peer joins the session. Just invokes the public event.
        private void OnPeerAdded(PeerAddedArgs args)
        {
            OnPeerJoined.Invoke(args.Peer);
        }

        // Called when a peer or self changes state. Invokes OnSessionLocalized if self finishes localizing and becomes stable.
        private void OnPeerStateReceived(PeerStateReceivedArgs args)
        {
            if (args.Peer == me)
            {
                if (args.State == PeerState.Stable && (prevState == PeerState.WaitingForLocalizationData || prevState == PeerState.Localizing))
                    OnSessionLocalized.Invoke();

                prevState = args.State;
            }
        }

        // Called when a peer disconnects. Just invokes the public event.
        private void OnPeerRemoved(PeerRemovedArgs args)
        {
            OnPeerLeft.Invoke(args.Peer);
        }

        /// <summary>
        /// Sends a list of serializable objects to the session host.
        /// </summary>
        /// <param name="tag">An integer representing the content to expect.</param>
        /// <param name="objs">A list of serializable objects to be sent. Serializable types include most primitives, arrays, Guids, strings, and Vectors.</param>
        /// <param name="tt">How the message should be sent. Use Unreliable for frequent updates (like position) and Reliable for infrequent essential communication.</param>
        public void SendToHost(uint tag, object[] objs, TransportType tt = TransportType.UnreliableUnordered)
        {
            if (!networking.IsConnected)
                return;

            var stream = new MemoryStream();

            using (var serializer = new BinarySerializer(stream))
            {
                serializer.Serialize(me.Identifier);
                serializer.Serialize(objs);
            }

            byte[] data = stream.ToArray();

            networking.SendDataToPeer(tag, data, networking.Host, tt);
        }

        /// <summary>
        /// Sends a list of serializable objects to all connected peers. This is only to be used by the host when relaying peer updates.
        /// </summary>
        /// <param name="tag">An integer representing the content to expect.</param>
        /// <param name="origin">The Guid of the original sender of the data.</param>
        /// <param name="objs">A list of serializable objects to be sent. Serializable types include most primitives, arrays, Guids, strings, and Vectors.</param>
        /// <param name="tt">How the message should be sent. Use Unreliable for frequent updates (like position) and Reliable for infrequent essential communication.</param>
        public void SendToAll(uint tag, Guid origin, object[] objs, TransportType tt = TransportType.UnreliableUnordered)
        {
            if (!networking.IsConnected || !IsHost)
                return;

            var stream = new MemoryStream();

            using (var serializer = new BinarySerializer(stream))
            {
                serializer.Serialize(origin);
                serializer.Serialize(objs);
            }

            byte[] data = stream.ToArray();

            networking.BroadcastData(tag, data, tt);
        }

        /// <summary>
        /// Sends a list of serializable objects to a specific peer. This is only to be used by the host.
        /// </summary>
        /// <param name="tag">An integer representing the content to expect.</param>
        /// <param name="target">The peer object representing the user to send the message to.</param>
        /// <param name="objs">A list of serializable objects to be sent. Serializable types include most primitives, arrays, Guids, strings, and Vectors.</param>
        /// <param name="tt">How the message should be sent. Use Unreliable for frequent updates (like position) and Reliable for infrequent essential communication.</param>
        public void SendToPeer(uint tag, IPeer target, object[] objs, TransportType tt = TransportType.UnreliableUnordered)
        {
            if (!networking.IsConnected || !IsHost)
                return;

            var stream = new MemoryStream();

            using (var serializer = new BinarySerializer(stream))
            {
                serializer.Serialize(me.Identifier);
                serializer.Serialize(objs);
            }

            byte[] data = stream.ToArray();

            networking.SendDataToPeer(tag, data, target, tt);
        }

        // Called when recieving data from a peer. Deserializes the data and invokes the public event.
        private void OnPeerDataRecieved(PeerDataReceivedArgs args)
        {
            var stream = new MemoryStream(args.CopyData());
            Guid sender;
            object[] data;

            using (var deserializer = new BinaryDeserializer(stream))
            {
                sender = (Guid)deserializer.Deserialize();
                data = (object[])deserializer.Deserialize();
            }

            OnDataRecieved.Invoke(args.Tag, sender, data);
        }
    }
}
