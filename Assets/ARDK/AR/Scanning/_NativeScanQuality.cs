// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using AOT;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  
  internal sealed class NativeScanQualityClassifier: IScanQualityClassifier
  {
    private IntPtr _nativeHandle;

    private SafeGCHandle<NativeScanQualityClassifier> _handle;

    private Dictionary<UInt64, Action<ScanQualityResult>> _pendingCallbacks;
    private static UInt64 _nextRequestId = 0;

    private string _dataPathRoot;
    
    internal NativeScanQualityClassifier(IARSession session, string dataPath)
    {
      _NativeAccess.AssertNativeAccessValid();
      _nativeHandle = _ScanQuality_Create(session.StageIdentifier.ToString());
      _handle = SafeGCHandle.Alloc(this);
      _ScanQuality_Set_ScanQualityPredictionCallback
      (
        _handle.ToIntPtr(),
        _nativeHandle,
        _onScanQualityProcessComplete
      );
      _pendingCallbacks = new Dictionary<ulong, Action<ScanQualityResult>>();
      this._dataPathRoot = dataPath;
    }
    
    ~NativeScanQualityClassifier()
    {
      if (_nativeHandle != IntPtr.Zero)
      {
        _ScanQuality_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
        _handle.Free();
      }
    }

    internal struct _ScanQualityResult
    {
      internal double score;
      internal int detailLength;
      internal IntPtr detail;
    }
    
    /// <summary>
    /// This is to find the maximum score in the sub-score types in the given category. For example, if the category is "dark",
    /// and Dark_Yes is 0.7 and Dark_No is 0.3, then this will return Dark_Yes with 0.7.
    /// </summary>
    private static Tuple<ScanQualityScoreType, float> GetMaxScoreForCategory(Dictionary<ScanQualityScoreType, float> scoreMap, ScanQualityCategory category)
    {
      return category.GetScores().Aggregate(
        new Tuple<ScanQualityScoreType, float>(ScanQualityScoreType.ScoreTypeOverall, 0),
        (currentMaxAndScore, newType) =>
        {
          if (scoreMap[newType] > currentMaxAndScore.Item2)
          {
            return new Tuple<ScanQualityScoreType, float>(newType, scoreMap[newType]);
          }
          else
          {
            return currentMaxAndScore;
          }
        }
      );
    }
    
    private static List<ScanQualityRejectionReason> GetScanRejectionReason(Dictionary<ScanQualityScoreType, float> scoreMap)
    {
      List<ScanQualityRejectionReason> reasons = new List<ScanQualityRejectionReason>();
      if (GetMaxScoreForCategory(scoreMap, ScanQualityCategory.Blurry).Item1 == ScanQualityScoreType.ScoreTypeBlurryYes)
      {
        reasons.Add(ScanQualityRejectionReason.TooBlurry);
      }
      if (GetMaxScoreForCategory(scoreMap, ScanQualityCategory.Dark).Item1 == ScanQualityScoreType.ScoreTypeDarkYes)
      {
        reasons.Add(ScanQualityRejectionReason.TooDark);
      }
      if (GetMaxScoreForCategory(scoreMap, ScanQualityCategory.Location).Item1 != ScanQualityScoreType.ScoreTypeLocationOutdoor)
      {
        ScanQualityScoreType location = GetMaxScoreForCategory(scoreMap,ScanQualityCategory.Location).Item1;
        if (location == ScanQualityScoreType.ScoreTypeLocationIndoorPrivate ||
            location == ScanQualityScoreType.ScoreTypeLocationIndoorPublic ||
            location == ScanQualityScoreType.ScoreTypeLocationIndoorUnclear)
        {
          reasons.Add(ScanQualityRejectionReason.ScanIndoors);
        } else if (location == ScanQualityScoreType.ScoreTypeLocationCar)
        {
          reasons.Add(ScanQualityRejectionReason.ScanFromCar);
        }
      }
      if (GetMaxScoreForCategory(scoreMap, ScanQualityCategory.Obstruction).Item1 == ScanQualityScoreType.ScoreTypeObstructedFully)
      {
        reasons.Add(ScanQualityRejectionReason.Obstructed);
      }
      if (GetMaxScoreForCategory(scoreMap, ScanQualityCategory.TargetVisibility).Item1 == ScanQualityScoreType.ScoreTypeTargetNotVisible)
      {
        reasons.Add(ScanQualityRejectionReason.TargetNotVisible);
      }

      if (GetMaxScoreForCategory(scoreMap, ScanQualityCategory.GroundOrFeet).Item1 == ScanQualityScoreType.ScoreTypeGroundOrFeetYes)
      {
        reasons.Add(ScanQualityRejectionReason.GroundOrFeet);
      }
      return reasons;
    }

    [MonoPInvokeCallback(typeof(_NativeScanQualityCallback))]
    private static void _onScanQualityProcessComplete(IntPtr context, IntPtr result)
    {
      NativeScanQualityClassifier scanQualityClassifier = SafeGCHandle.TryGetInstance<NativeScanQualityClassifier>(context);
      if (scanQualityClassifier == null)
      {
        // scanQuality was deallocated. Should never happen.
        ARLog._Error("Unexpected null native object");
        return;
      }
      
      Dictionary<ScanQualityScoreType, float> scoreMap = new Dictionary<ScanQualityScoreType, float>();
      foreach (ScanQualityScoreType scoreType in Enum.GetValues(typeof(ScanQualityScoreType)))
      {
        scoreMap.Add(scoreType, _ScanQuality_GetScore(result, scoreType));
      }
      UInt64 requestId = _ScanQuality_GetRequestIdForResult(result);

      _CallbackQueue.QueueCallback(() =>
      {
        scanQualityClassifier._pendingCallbacks[requestId](new ScanQualityResult(scoreMap[ScanQualityScoreType.ScoreTypeOverall], GetScanRejectionReason(scoreMap)));
        scanQualityClassifier._pendingCallbacks.Remove(requestId);
      });
      
    }
    
    public void ComputeScanQuality(string scanId, Action<ScanQualityResult> onResult)
    {
      string scanPath = ScanPath.GetScanPath(_dataPathRoot, scanId, RuntimeEnvironment.LiveDevice);
      _ScanQuality_ComputeScanQuality(_nativeHandle, scanPath, _nextRequestId);
      this._pendingCallbacks.Add(_nextRequestId, onResult);
      _nextRequestId++;
    }
    
    private delegate void _NativeScanQualityCallback(IntPtr context, IntPtr result);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ScanQuality_Create(string stageUuid);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ScanQuality_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ScanQuality_ComputeScanQuality(IntPtr nativeHandle, string scanPath, UInt64 requestId);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _ScanQuality_GetRequestIdForResult(IntPtr resultPtr);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _ScanQuality_GetScore(IntPtr resultPtr, ScanQualityScoreType scoreType);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _ScanQuality_Set_ScanQualityPredictionCallback
    (
      IntPtr applicationScanner,
      IntPtr platformScanner,
      _NativeScanQualityCallback callback
    );
  }
}
