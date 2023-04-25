// Copyright 2022 Niantic, Inc. All Rights Reserved.
#pragma warning disable 0067

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using AOT;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.Experimental.ARDK.SharedAR
{
  public interface INativeNetworking : INetworking {
    IntPtr GetNativeHandle();
  }

  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public class _NativeNetworking :
    INativeNetworking
  {
    private bool _isDestroyed;
    private bool _isServer;
    private bool _didSubscribeToNativeEvents;
    private readonly List<IPeerID> _peerIDs = new List<IPeerID>();
    private IPeerID _selfPeerId;

    private const string DEFAULT_SESSION = "default_session_id";

    internal string SessionId { get; private set; } = DEFAULT_SESSION;
    public event ArdkEventHandler<ConnectionEventArgs> ConnectionEvent;
    public event ArdkEventHandler<PeerIDArgs> PeerAdded;
    public event ArdkEventHandler<PeerIDArgs> PeerRemoved;
    public event ArdkEventHandler<DataReceivedArgs> DataReceived;

    public class NativePeerID : IPeerID
    {
      public NativePeerID(IntPtr data, UInt64 size)
      {
        var array_for_guid = new byte[size];
        Marshal.Copy(data, array_for_guid, 0, (int)size);
        Identifier = new Guid(array_for_guid);
      }

      public Guid Identifier { get; private set; }

      public bool Equals(IPeerID other)
      {
        if (other is NativePeerID nativeOther)
          return Identifier == nativeOther.Identifier;
        else
          return false;
      }

      public override int GetHashCode()
      {
        return Identifier.GetHashCode();
      }
    }

    public _NativeNetworking(string connectionId = "", NetworkingBackend backend = NetworkingBackend.NetworkingV0)
    {
      _nativeHandle = (!string.IsNullOrEmpty(connectionId)) ?
        _NARNetworking_Init
        (
          connectionId,
          _applicationHandle,
          (byte)backend
        ) :
        _NARNetworking_Init
        (
          DEFAULT_SESSION,
          _applicationHandle,
          (byte)backend
        );

      GC.AddMemoryPressure(GCPressure);
      SessionId = connectionId;
      SubscribeToNativeCallbacks();
    }

    public void SetDefaultConnectionType(ConnectionType connectionType)
    {
      _NARNetworking_SetConnectionType(_nativeHandle, (byte)connectionType);
    }

    public void SendData(List<IPeerID> targetPeers, uint tag, byte[] data, ConnectionType connectionType)
    {
      ARLog._DebugFormat
      (
        "Sending {0} bytes with tag {1} to {2} peers: {3}",
        false,
        data.Length,
        tag,
        targetPeers.Count,
        String.Join(",", targetPeers.Select(p => p.ToString()))
      );

      var peerIdentifiers = new byte[targetPeers.Count * 16];
      for (var i = 0; i < targetPeers.Count; i++)
      {
        var peerGuids = targetPeers[i].Identifier.ToByteArray();

        for (var j = 0; j < 16; j++)
          peerIdentifiers[j + i * 16] = peerGuids[j];
      }

      _NARNetworking_SendData
      (
        _nativeHandle,
        tag,
        data,
        (ulong)data.Length,
        peerIdentifiers,
        (ulong)targetPeers.Count,
        (byte)connectionType
      );
    }

    public void SendData<T>(List<IPeerID> dest, uint tag, T data,
      ConnectionType connectionType = ConnectionType.UseDefault)
    {
      throw new System.NotImplementedException();
    }

    public ConnectionState ConnectionState { get; }

    bool INetworking.IsServer => _isServer;

    public IPeerID SelfPeerID
    {
      get
      {
        // only generate a self peer id object once and cache it
        if (_selfPeerId == null)
        {
          // create a 16-byte array to hold the bytes of a guid (since a guid is exactly 16 bytes)
          var selfIdBytes = new byte[16];
          // pin the selfIdBytes array with a GCHandle before passing this to C++ so heap address of array doesn't change from GC
          var handle = GCHandle.Alloc(selfIdBytes, GCHandleType.Pinned);
          // get a pointer to the element at the first index of the selfIdBytes array
          var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(selfIdBytes, 0);
          // pass the pointer and array size of 16 to native side to be populated
          _NARNetworking_GetSelfPeerId(_nativeHandle, ptr, (uint)selfIdBytes.Length);
          // create a guid object from the now populated selfIdBytes array
          var selfIdGuid = new Guid(selfIdBytes);
          // unpin the selfIdBytes array so proper GC cleanup can occur for the array
          handle.Free();
          _selfPeerId = new PeerIDv0(_Peer.FromIdentifier(selfIdGuid));
        }

        return _selfPeerId;
      }
    }

    public IPeerID ServerPeerId { get; }

    List<IPeerID> INetworking.PeerIDs => _peerIDs;

    public NetworkingRequestResult KickOutPeer(IPeerID peerID)
    {
      throw new NotImplementedException();
      // TODO actually implement this
    }

    public NetworkingStats NetworkingStats { get; }

    public void JoinAsServer(byte[] roomId)
    {
      throw new NotImplementedException();
      // TODO this is currently done in the constructor, make it actually work
    }

    public void JoinAsPeer(byte[] roomId)
    {
      throw new NotImplementedException();
      // TODO this is currently done in the constructor, make it actually work
    }

    public void Leave()
    {
      ARLog._Debug("Calling Leave on NativeNetworking");
      _NARNetworking_Leave(_nativeHandle);
      _peerIDs.Clear();
    }

    public RoomParams RoomParams { get; }

    public void Dispose()
    {
      if (_nativeHandle != IntPtr.Zero)
      {
        ARLog._Debug("Calling Release on NativeNetworking");
        _NARNetworking_Release(_nativeHandle);
        GC.RemoveMemoryPressure(GCPressure);
        _nativeHandle = IntPtr.Zero;
      }
    }

    private void SubscribeToNativeCallbacks()
    {
      if(_didSubscribeToNativeEvents)
        return;

      lock (this)
      {
        if(_didSubscribeToNativeEvents)
          return;

        _NARNetworking_Set_connectionEventCallback(_applicationHandle, _nativeHandle, _connectionEventReceivedNative);
        _NARNetworking_Set_peerAddedCallback(_applicationHandle, _nativeHandle, _didAddPeerNative);
        _NARNetworking_Set_peerRemovedCallback(_applicationHandle, _nativeHandle, _didRemovePeerNative);
        _NARNetworking_Set_dataReceivedCallback(_applicationHandle, _nativeHandle, _dataReceivedNative);

        _didSubscribeToNativeEvents = true;
      }
    }

#region Handles
    // Below here are private fields and methods to handle native code and callbacks

    // The pointer to the C++ object handling functionality at the native level
    private IntPtr _nativeHandle;

    public IntPtr GetNativeHandle()
    {
      return _nativeHandle;
    }

    private IntPtr _cachedHandleIntPtr = IntPtr.Zero;
    private SafeGCHandle<_NativeNetworking> _cachedHandle;

    // Approx memory size of native object
    // Magic number for 64
    private const long GCPressure = 64L * 1024L;

    // Used to round-trip a pointer through c++,
    // so that we can keep our this pointer even in c# functions
    // marshaled and called by native code
    private IntPtr _applicationHandle
    {
      get
      {
        if (_cachedHandleIntPtr != IntPtr.Zero)
          return _cachedHandleIntPtr;

        lock (this)
        {
          if (_cachedHandleIntPtr != IntPtr.Zero)
            return _cachedHandleIntPtr;

          // https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.gchandle.tointptr.aspx
          _cachedHandle = SafeGCHandle.Alloc(this);
          _cachedHandleIntPtr = _cachedHandle.ToIntPtr();
        }

        return _cachedHandleIntPtr;
      }
    }
#endregion

#region PInvoke
    // C# -> C++ Calls
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARNetworking_Init
    (
      string connectionId,
      IntPtr applicationHandle,
      byte implementationType
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_SetConnectionType
      (IntPtr nativeHandle, byte connectionType);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_SendData
    (
      IntPtr nativeHandle,
      UInt32 tag,
      byte[] data,
      UInt64 dataSize,
      byte[] peerIdentifiers,
      UInt64 peerIdentifiersCount,
      byte connectionType
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern byte _NARNetworking_GetConnectionState(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_GetSelfPeerId
    (
      IntPtr nativeHandle,
      IntPtr outPeerId,
      uint outPeerIdSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_GetPeerIds
    (
      IntPtr nativeHandle,
      byte[] outPeerIds,
      ref UInt32 outPeerIdListSize,
      UInt32 outPeerIdUnitSize,
      UInt32 maxOutPeerArrayLength
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern byte _NARNetworking_KickoutPeer
    (
      IntPtr nativeHandle,
      byte[] peerId,
      UInt32 peerIdSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARNetworking_GetNetworkingStats(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_Leave(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_GetConnectionId
    (
      IntPtr nativeHandle,
      string outName,
      ulong maxNameSize
    );

    // Setting callbacks from C++
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_Set_connectionEventCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARNetworking_connectionEventCallback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_Set_dataReceivedCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARNetworking_dataReceivedCallback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_Set_peerAddedCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARNetworking_peerAddedOrRemovedCallback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARNetworking_Set_peerRemovedCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARNetworking_peerAddedOrRemovedCallback cb
    );

    // C++ -> C# callbacks
    private delegate void _NARNetworking_connectionEventCallback
    (
      IntPtr context,
      byte connectionEvent
    );

    private delegate void _NARNetworking_peerAddedOrRemovedCallback
    (
      IntPtr context,
      // TODO: use byte array of UUID instead of IPeer pointer
      IntPtr peerIDPtr,
      UInt64 peerIDSize
    );

    private delegate void _NARNetworking_dataReceivedCallback
    (
      IntPtr context,
      UInt32 tag,
      IntPtr rawData,
      UInt64 rawDataSize,
      IntPtr peerIDPtr,
      UInt64 peerIDSize
    );

    [MonoPInvokeCallback(typeof(_NARNetworking_connectionEventCallback))]
    private static void _connectionEventReceivedNative(IntPtr context, byte connectionEvent)
    {
      ARLog._Debug("Invoked _connectionEventReceivedNative");
      var instance = SafeGCHandle.TryGetInstance<_NativeNetworking>(context);
      if (instance == null || instance._isDestroyed)
        return;

      ARLog._WarnFormat("Got connection event {0}",false, connectionEvent);
      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance._isDestroyed)
          {
            ARLog._Warn("Queued _connectionEventReceivedNative invoked after C# instance was destroyed.");
            return;
          }

          var handler = instance.ConnectionEvent;
          if (handler != null)
          {
            ARLog._DebugFormat("Surfacing ConnectionEvent event: {0}", false, ((ConnectionEvents)connectionEvent).ToString());
            var args = new ConnectionEventArgs((ConnectionEvents)connectionEvent);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NativeNetworking._NARNetworking_peerAddedOrRemovedCallback))]
    private static void _didAddPeerNative(IntPtr context, IntPtr peerIDPtr, UInt64 peerIDSize)
    {
      // TODO: use byte array of UUID instead of IPeer pointer

      ARLog._Debug("Invoked _didAddPeerNative");

      var instance = SafeGCHandle.TryGetInstance<_NativeNetworking>(context);
      if (instance == null || instance._isDestroyed)
      {
        ARLog._Warn("_didAddPeerNative invoked after C# instance was destroyed.");
        return;
      }
      var peerId = new NativePeerID(peerIDPtr, peerIDSize);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance._isDestroyed)
          {
            ARLog._Warn("Queued _didAddPeerNative invoked after C# instance was destroyed.");
            return;
          }

          instance._peerIDs.Add(peerId);

          var handler = instance.PeerAdded;
          if (handler != null)
          {
            ARLog._DebugFormat("Surfacing PeerAdded event for peer: {0}", false, peerId.Identifier);
            var args = new PeerIDArgs(peerId);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NativeNetworking._NARNetworking_peerAddedOrRemovedCallback))]
    private static void _didRemovePeerNative(IntPtr context, IntPtr peerIDPtr, UInt64 peerIDSize)
    {
      ARLog._Debug("Invoked _didRemovePeerNative");

      var instance = SafeGCHandle.TryGetInstance<_NativeNetworking>(context);
      if (instance == null || instance._isDestroyed)
      {
        ARLog._Warn("_didRemovePeerNative invoked after C# instance was destroyed.");
        return;
      }
      var peerId = new NativePeerID(peerIDPtr, peerIDSize);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance._isDestroyed)
          {
            ARLog._Warn("Queued _didRemovePeerNative invoked after C# instance was destroyed.");
            return;
          }

          instance._peerIDs.Remove(peerId);

          var handler = instance.PeerRemoved;
          if (handler != null)
          {
            ARLog._Debug("Surfacing PeerRemoved event for peer: " + peerId);
            var args = new PeerIDArgs(peerId);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NARNetworking_dataReceivedCallback))]
    private static void _dataReceivedNative
    (
      IntPtr context,
      UInt32 tag,
      IntPtr rawData,
      UInt64 rawDataSize,
      IntPtr peerIDPtr,
      UInt64 peerIDSize
    )
    {
      // TODO: use byte array of UUID instead of IPeer pointer

      ARLog._Debug("Invoked _dataReceivedNative", true);
      var instance = SafeGCHandle.TryGetInstance<_NativeNetworking>(context);

      if (instance == null || instance._isDestroyed)
      {
        ARLog._Warn("Queued _dataReceivedNative called after C# instance was destroyed.");
        return;
      }

      var data = new byte[rawDataSize];
      Marshal.Copy(rawData, data, 0, (int)rawDataSize);

      var peerId = new NativePeerID(peerIDPtr, peerIDSize);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance._isDestroyed)
          {
            var msg = "Queued _dataReceivedNative called after C# instance was destroyed.";
            ARLog._Warn(msg);
            return;
          }

          var handler = instance.DataReceived;
          if (handler != null)
          {
            ARLog._Debug("Surfacing DataReceived event");
            handler(new DataReceivedArgs(peerId, tag, data));
          }
        }
      );
    }
#endregion
  }
}
#pragma warning restore 0067
