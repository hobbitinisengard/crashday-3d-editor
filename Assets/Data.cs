using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Static class containing fields that need to survive during scene change
/// </summary>
public static class Data
{
  /// <summary>Maximum tile limit</summary>
  public readonly static int TrackTileLimit = 8000;
  /// <summary>
  /// Array of tiles that have bushes on them
  /// </summary>
  public static string[] bushes = new string[] { "tree1", "tree2", "tree3", "tree4", "tree5", "streetl", "alley", "cralley", "dirtalley", "zebracross", "curveb2", "plot1", "plot2", "plot3" };

  public static TrackSavable TRACK { get; set; }
  ///<summary> text file with lines going like this: street 0.1 0.2 -0.452 0.34 0.11 </summary>
  //public static TextAsset Flatters { get; set; }
  /////<summary> path to user folder </summary>
  //public static TextAsset Path { get; set; }
  /////<summary> text file with lines going like this: street -0.4333. Pzero is number stating how much u need to lift tile to match flat terrain</summary>
  //public static TextAsset Pzeros { get; set; }
  /////<summary> text file with lines going like this: street RMC1x1_full </summary>
  //public static TextAsset Rmcs { get; set; }
  ///<summary> Is editor loading map? </summary>
  public static bool Isloading { get; set; } = false;
  ///<summary> array representing placed elements during mapping </summary>
  public static TilePlacement[,] TilePlacementArray { get; set; }
  ///<summary> String showed on the top bar of the editor during mapping </summary>
  public static string UpperBarTrackName { get; set; } = "Untitled";

  public static Dictionary<int, string> TileSets = new Dictionary<int, string>();
}




