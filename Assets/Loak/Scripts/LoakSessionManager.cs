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

namespace Loak.Unity
{
    public class LoakSessionManager : MonoBehaviour
    {
        public UnityEvent OnSessionJoined;
        public UnityEvent OnSessionStarted;
        public UnityEvent<IPeer> OnPeerJoined;
        public UnityEvent<IPeer> OnPeerLeft;

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
    }
}
