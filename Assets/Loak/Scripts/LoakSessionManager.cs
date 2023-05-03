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

namespace Loak.Unity
{
    public class LoakSessionManager : MonoBehaviour
    {
        public UnityEvent OnSessionJoined;
        public UnityEvent OnSessionStarted;
        public UnityEvent<IPeer> OnPeerJoined;
        public UnityEvent<IPeer> OnPeerLeft;
        public UnityEvent<uint, Guid, object[]> OnDataRecieved;

        [HideInInspector] public bool IsHost = false;
        [HideInInspector] public IPeer me;

        private string sessionIdentifier;
        private IMultipeerNetworking networking;
        private IARSession arSession;
        private IARNetworking arNetworking;
        private IARWorldTrackingConfiguration configuration;

        void Start()
        {
            networking = MultipeerNetworkingFactory.Create();
            networking.Connected += OnConnected;
            networking.PeerAdded += OnPeerAdded;
            networking.PeerRemoved += OnPeerRemoved;
            networking.PeerDataReceived += OnPeerDataRecieved;

            arSession = ARSessionFactory.Create(networking.StageIdentifier);
            arNetworking = ARNetworkingFactory.Create(arSession, networking);

            configuration = ARWorldTrackingConfigurationFactory.Create();
            configuration.IsSharedExperienceEnabled = true;
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
            if (!networking.IsConnected)
                return;

            sessionIdentifier = null;
            networking.Leave();
        }

        public void StartSoloSession()
        {
            if (networking.IsConnected)
            {
                networking.Leave();
            }

            arSession.Run(configuration);
            OnSessionStarted.Invoke();
        }

        public void StartMultiplayerSession()
        {
            if (!networking.IsConnected)
                return;

            arNetworking.ARSession.Run(configuration);
            OnSessionStarted.Invoke();
        }

        private void OnPeerAdded(PeerAddedArgs args)
        {
            OnPeerJoined.Invoke(args.Peer);
        }

        private void OnPeerRemoved(PeerRemovedArgs args)
        {
            OnPeerJoined.Invoke(args.Peer);
        }

        public void SendToHost(uint tag, string str)
        {
            if (!networking.IsConnected)
                return;

            var stream = new MemoryStream();

            using (var serializer = new BinarySerializer(stream))
            {
                serializer.Serialize(me.Identifier);
                serializer.Serialize(1);
                serializer.Serialize(str);
            }

            byte[] data = stream.ToArray();

            networking.SendDataToPeer(tag, data, networking.Host, TransportType.ReliableUnordered);
        }

        public void SendToAll(uint tag, Guid origin, string str)
        {
            if (!networking.IsConnected || !IsHost)
                return;

            var stream = new MemoryStream();

            using (var serializer = new BinarySerializer(stream))
            {
                serializer.Serialize(origin);
                serializer.Serialize(1);
                serializer.Serialize(str);
            }

            byte[] data = stream.ToArray();

            networking.BroadcastData(tag, data, TransportType.ReliableUnordered);
        }

        public void SendToPeer(uint tag, Guid target, (List<Guid>, List<string>) payload)
        {
            if (!networking.IsConnected || !IsHost)
                return;

            var stream = new MemoryStream();

            using (var serializer = new BinarySerializer(stream))
            {
                serializer.Serialize(me.Identifier);
                serializer.Serialize(2);
                serializer.Serialize(payload.Item1.ToArray());
                serializer.Serialize(payload.Item2.ToArray());
            }

            byte[] data = stream.ToArray();

            IPeer peer = null;
            foreach (IPeer p in networking.OtherPeers)
            {
                if (p.Identifier == target)
                    peer = p;
            }

            networking.SendDataToPeer(tag, data, peer, TransportType.ReliableUnordered);
        }

        private void OnPeerDataRecieved(PeerDataReceivedArgs args)
        {
            var stream = new MemoryStream(args.CopyData());
            Guid sender;
            int length;
            object[] data;

            using (var deserializer = new BinaryDeserializer(stream))
            {
                sender = (Guid)deserializer.Deserialize();
                length = (int)deserializer.Deserialize();

                data = new object[length];
                for (int i = 0; i < length; i++)
                    data[i] = deserializer.Deserialize();
            }

            OnDataRecieved.Invoke(args.Tag, sender, data);
        }
    }
}
