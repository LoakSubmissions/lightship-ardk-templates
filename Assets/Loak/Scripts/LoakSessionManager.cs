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
        public UnityEvent OnPeerJoined;
        public UnityEvent OnPeerLeft;

        private string sessionIdentifier;
        private IMultipeerNetworking networking;
        private IARSession arSession;
        private IARNetworking arNetworking;
        private IARWorldTrackingConfiguration configuration;

        private bool IsHost = false;

        void Start()
        {
            networking = MultipeerNetworkingFactory.Create();
            networking.Connected += OnConnected;

            arSession = ARSessionFactory.Create(networking.StageIdentifier);
            arNetworking = ARNetworkingFactory.Create(arSession, networking);

            configuration = ARWorldTrackingConfigurationFactory.Create();
            configuration.IsSharedExperienceEnabled = true;
        }

        public void TryJoinSession(string sessionIdentifier)
        {
            this.sessionIdentifier = sessionIdentifier;

            networking.Join(Encoding.UTF8.GetBytes(sessionIdentifier));
        }

        private void OnConnected(ConnectedArgs args)
        {
            IsHost = args.IsHost;
            OnSessionJoined.Invoke();
        }

        public void TryStartSession()
        {
            if (!networking.IsConnected || !IsHost)
                return;

            arNetworking.ARSession.Run(configuration);
            OnSessionStarted.Invoke();
        }
    }
}
