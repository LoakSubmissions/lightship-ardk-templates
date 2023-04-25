// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Niantic.ARDK.AR.Scanning
{
  /// Manages the persistence of scans created by <see cref="IScanner"/>. An instance
  /// can be obtained by calling <see cref="IScanner.GetScanStore"/>.
  public interface IScanStore
  {
    /// Returns a list of IDs for all scans that are saved on this device.
    public List<string> GetScanIDs();

    /// Returns the <see cref="SavedScan"/> for a given scan ID.
    /// @param scanId The ID of the scan to return
    /// @returns the <see cref="SavedScan"/> or null if it does not exist
    public SavedScan GetSavedScan(string scanId);

    /// Saves the current scan into persistent storage on device.
    /// The Scanner must be in the <see cref="IScanner.State.Done">Done</see> state. The current scan ID can be
    /// obtained by calling <see cref="IScanner.GetScanId"/>.
    /// @param scanId The current scanID, obtained by calling <see cref="IScanner.GetScanId">GetScanId</see>
    /// @exception InvalidOperationException If there is no scan corresponding to the scanId.
    public void SaveCurrentScan(string scanId);

    /// Deletes the saved scan with the given scan ID.
    /// @param scanId the ID of the scan to delete  
    public void DeleteSavedScan(string scanId);
    
    /// Additional metadata relating to an uploaded scan.
    [Serializable]
    public class UploadUserInfo
    {
      /// A list of labels labels to associate with the scan.
      public List<string> scanLabels;

      /// An optional note describing the scan. 
      public string note;
    }

    /// Uploads the saved scan to Niantic for VPS activation.
    /// @param scanId The ID of the scan to upload
    /// @param uploadUserInfo Additional metadata to upload along with the scan
    /// @param onProgress Callback invoked periodically while the scan uploads with a float between 0 and 1
    ///                   to indicate the upload progress.
    /// @param onResult Called when upload completes, with a boolean indicating whether the upload succeeded and
    ///                 a string describing the failure (if any).
    public void UploadScan(string scanId, UploadUserInfo uploadUserInfo, Action<float> onProgress,
      Action<bool, string> onResult);
    
  }
}
