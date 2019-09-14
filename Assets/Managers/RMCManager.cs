
using UnityEngine;
/// <summary>
/// "Real mesh collider" manager
/// </summary>
public static class RMCManager
{
  /// <summary>
  /// Returns name of RMC based on Restrictions (e.g H1V1H2) and size of unknown tile
  /// </summary>
  public static string GetRMCName(string Restrictions, IntVector2 Size)
  {
    // Normalize Restrictions bcoz devs messed them up a little bit
    if (Size.x == 1 && Restrictions.Contains("V2"))
      Restrictions.Replace("V2", "");
    if (Size.y == 1 && Restrictions.Contains("H2"))
      Restrictions.Replace("H2", "");

    return "RMC" + Size.x.ToString() + "x" + Size.y.ToString() + Restrictions;
  }
}

