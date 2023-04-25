namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal interface _IContentVisualizer
  {
    void CreateDisplays();
    void DestroyDisplays();
    void UpdateDisplay(VPSLocationManifest prev, VPSLocationManifest curr);
  }
}
