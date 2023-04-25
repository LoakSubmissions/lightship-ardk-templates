// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace Niantic.ARDK.AR.Scanning
{
  internal enum ScanQualityScoreType
  {
    // The overall score of the scan. Higher is better. Range is 0 to 1.
    ScoreTypeOverall = 0,
    
    // The scan is not blurry.
    ScoreTypeBlurryNo = 4,
    
    // The scan is blurry.
    ScoreTypeBlurryYes = 5,
    
    // The scan is not too dark.
    ScoreTypeDarkNo = 8,
    
    // The scan is too dark.
    ScoreTypeDarkYes = 9,
    
    // Not sure if the scan is too dark or not.
    ScoreTypeDarkUnclear = 10,
    
    // The scan is not of good quality.
    ScoreTypeGoodQualityNo = 6,
    
    // The scan is of good quality.
    ScoreTypeGoodQualityYes = 7,
    
    // The scan is not just ground or feet.
    ScoreTypeGroundOrFeetNo = 12,
    
    // The scan is just ground or feet.
    ScoreTypeGroundOrFeetYes = 13,
    
    // The scan is a private, indoor space.
    ScoreTypeLocationIndoorPrivate = 16,
    
    // The scan is a public, indoor space.
    ScoreTypeLocationIndoorPublic = 17,
    
    // The scan is indoor, but not sure about private vs. public.
    ScoreTypeLocationIndoorUnclear = 18,
    
    // The scan is outdoor.
    ScoreTypeLocationOutdoor = 19,
    
    // The scan is in a car.
    ScoreTypeLocationCar = 20,
    
    // Not sure where the scan is taken from.
    ScoreTypeLocationUnclear = 21,
    
    // The scan target is not obstructed.
    ScoreTypeObstructedNo = 24,
    
    // The scan target is fully obstructed.
    ScoreTypeObstructedFully = 25,
    
    // The scan target is partially obstructed.
    ScoreTypeObstructedPartial = 26,
    
    // The scan target is not visible.
    ScoreTypeTargetNotVisible = 28,
    
    // The scan target is visible.
    ScoreTypeTargetVisible = 29,
    
    // Not sure if the scan target is visible or not.
    ScoreTypeTargetUnclear = 30,
  }

  /// <summary>
  /// The ScanQualityClassifier returns scores of these categories.
  /// </summary>
  internal enum ScanQualityCategory
  {
    // The overall score of the scan.
    Overall,
    // Is the scan blurry?
    Blurry,
    // Is the scan too dark?
    Dark, 
    // Is the scan of good quality?
    GoodQuality,
    // Is the scan focused on the ground or feet?
    GroundOrFeet,
    // Is the scan indoors, outdoors, or in a car?
    Location,
    // Is the scan target obstructed?
    Obstruction,
    // Is the scan target visible?
    TargetVisibility
  }

  /// Reasons for a scan to be rejected by the classifier.
  public enum ScanQualityRejectionReason
  {
    /// The images in the scan were too blurry to be usable for VPS.
    TooBlurry,
    /// The images in the scan were too dark to be usable for VPS.
    TooDark,
    /// The scan is predominantly of the ground or the user's feet, not the scan target.
    GroundOrFeet,
    /// The scan was captured indoors.
    ScanIndoors,
    /// The scan was captured from inside a car.
    ScanFromCar,
    /// The scan target was obstructed.
    Obstructed,
    /// The scan target was not visible in the scan
    TargetNotVisible
  }

  internal static class ScanQualityExtension
  {
    private static readonly ScanQualityScoreType[] Overall = { ScanQualityScoreType.ScoreTypeOverall };

    private static readonly ScanQualityScoreType[] Blurry =
      { ScanQualityScoreType.ScoreTypeBlurryNo, ScanQualityScoreType.ScoreTypeBlurryYes };

    private static readonly ScanQualityScoreType[] Dark =
    {
      ScanQualityScoreType.ScoreTypeDarkNo, ScanQualityScoreType.ScoreTypeDarkYes,
      ScanQualityScoreType.ScoreTypeDarkUnclear
    };

    private static readonly ScanQualityScoreType[] GoodQuality =
      { ScanQualityScoreType.ScoreTypeGoodQualityNo, ScanQualityScoreType.ScoreTypeGoodQualityYes };

    private static readonly ScanQualityScoreType[] GroundOrFeet =
      { ScanQualityScoreType.ScoreTypeGroundOrFeetNo, ScanQualityScoreType.ScoreTypeGroundOrFeetYes };

    private static readonly ScanQualityScoreType[] Location =
    {
      ScanQualityScoreType.ScoreTypeLocationCar, ScanQualityScoreType.ScoreTypeLocationOutdoor,
      ScanQualityScoreType.ScoreTypeLocationUnclear,
      ScanQualityScoreType.ScoreTypeLocationIndoorPrivate, ScanQualityScoreType.ScoreTypeLocationIndoorPublic,
      ScanQualityScoreType.ScoreTypeLocationIndoorUnclear
    };

    private static readonly ScanQualityScoreType[] Obstruction =
    {
      ScanQualityScoreType.ScoreTypeObstructedNo, ScanQualityScoreType.ScoreTypeObstructedPartial,
      ScanQualityScoreType.ScoreTypeObstructedFully
    };

    private static readonly ScanQualityScoreType[] TargetVisibility =
    {
      ScanQualityScoreType.ScoreTypeTargetNotVisible, ScanQualityScoreType.ScoreTypeTargetUnclear,
      ScanQualityScoreType.ScoreTypeTargetVisible
    };

    /// <summary>
    /// Returns the concrete score types related to a category of score.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    internal static ScanQualityScoreType[] GetScores(this ScanQualityCategory category)
    {
      switch (category)
      {
        case ScanQualityCategory.Overall:
          return Overall;
        case ScanQualityCategory.Blurry:
          return Blurry;
        case ScanQualityCategory.Dark:
          return Dark;
        case ScanQualityCategory.GoodQuality:
          return GoodQuality;
        case ScanQualityCategory.GroundOrFeet:
          return GroundOrFeet;
        case ScanQualityCategory.Location:
          return Location;
        case ScanQualityCategory.Obstruction:
          return Obstruction;
        case ScanQualityCategory.TargetVisibility:
          return TargetVisibility;
        default:
          throw new InvalidEnumArgumentException();
      }
    }
  }

  /// Result returned by the <see cref="IScanQualityClassifier"/>.
  public class ScanQualityResult
  {
    /// An overall score of the scan's quality. Range is 0-1, higher is better.
    public float ScanQualityScore { get; private set; }

    /// Returns a list of problems with the scan that may contribute to it receiving a lower
    /// scan quality score. This list will be empty for high-quality scans.
    public List<ScanQualityRejectionReason> RejectionReasons { get; private set; }

    public ScanQualityResult(float scanQualityScore, List<ScanQualityRejectionReason> rejectionReasons)
    {
      this.ScanQualityScore = scanQualityScore;
      this.RejectionReasons = rejectionReasons;
    }

  }

  /// Calculates a scan quality score and a list of reasons why a scan may be of low quality. 
  public interface IScanQualityClassifier
  {
    /// Calculates the scan quality for a saved scan. 
    /// @param scanId The ID of the scan to score with the classifier. This scan must exist on the local device.
    /// @param onResult Callback invoked when result is available.
    public void ComputeScanQuality(string scanId, Action<ScanQualityResult> onResult);
  }
}
