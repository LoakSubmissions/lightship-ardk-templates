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

        public bool arOnStart = false;

        public UnityEvent OnSessionJoined;
        public UnityEvent OnSessionStarted;
        public UnityEvent OnSessionLocalized;
        public UnityEvent<IPeer> OnPeerJoined;
        public UnityEvent<IPeer> OnPeerLeft;
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

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Initialize();

            if (arOnStart)
                StartSoloSession();
        }

        private void Initialize()
        {
            sessionBegan = false;

            if (networking == null)
            {
                if (arNetworking != null)
                {
                    arNetworking.Dispose();
                    arNetworking = null;
                }

                networking = MultipeerNetworkingFactory.Create(arSession == null ? default : arSession.StageIdentifier);
                networking.Connected += OnConnected;
                networking.PeerAdded += OnPeerAdded;
                networking.PeerRemoved += OnPeerRemoved;
                networking.PeerDataReceived += OnPeerDataRecieved;
            }

            if (arSession == null)
            {
                if (arNetworking != null)
                {
                    arNetworking.Dispose();
                    arNetworking = null;
                }

                arSession = ARSessionFactory.Create(networking.StageIdentifier);
            }

            if (arNetworking == null)
            {
                arNetworking = ARNetworkingFactory.Create(arSession, networking);
                arNetworking.PeerStateReceived += OnPeerStateReceived;
            }

            if (configuration == null)
            {
                configuration = ARWorldTrackingConfigurationFactory.Create();
                configuration.IsAutoFocusEnabled = true;
                configuration.IsLightEstimationEnabled = true;
            }
        }

        public void JoinSession(string sessionIdentifier)
        {
            this.sessionIdentifier = sessionIdentifier;

            networking.Join(Encoding.UTF8.GetBytes(sessionIdentifier));
        }

        private void OnConnected(ConnectedArgs args)
        {
            IsHost = args.IsHost;
            me = networking.Self;
            OnSessionJoined.Invoke();
        }

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

        public void StartSoloSession()
        {
            if (networking.IsConnected)
            {
                LeaveSession();
            }

            configuration.IsSharedExperienceEnabled = false;
            arSession.Run(configuration);
        }

        public void StartMultiplayerSession()
        {
            if (!networking.IsConnected)
                return;

            configuration.IsSharedExperienceEnabled = true;
            arSession.Run(configuration, ARSessionRunOptions.None);
            OnSessionStarted.Invoke();
            sessionBegan = true;
        }

        private void OnPeerAdded(PeerAddedArgs args)
        {
            OnPeerJoined.Invoke(args.Peer);
        }

        private void OnPeerStateReceived(PeerStateReceivedArgs args)
        {
            if (args.Peer == me)
            {
                if (args.State == PeerState.Stable && (prevState == PeerState.WaitingForLocalizationData || prevState == PeerState.Localizing))
                    OnSessionLocalized.Invoke();

                prevState = args.State;
            }
        }

        private void OnPeerRemoved(PeerRemovedArgs args)
        {
            OnPeerLeft.Invoke(args.Peer);
        }

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
