using System;
using System.Globalization;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal static class _ResponseStatusTranslator
  {
    public static _VpsDefinitions.StatusCode FromString(string status)
    {
      _VpsDefinitions.StatusCode result;
      if (Enum.TryParse(status, out result))
        return result;

      status = status.ToLower().Replace("_", " ");
      TextInfo info = CultureInfo.CurrentCulture.TextInfo;
      status = info.ToTitleCase(status).Replace(" ", string.Empty);
      Enum.TryParse(status, out result);
      return result;
    }
  }
}
