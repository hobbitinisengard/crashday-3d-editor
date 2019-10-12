using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Static class containing fields that need to survive during scene change
/// </summary>
public static class Data
{
  public readonly static string VERSION = "2.3";
  /// <summary>Maximum tile limit</summary>
  public readonly static int TrackTileLimit = 8000;
  internal static readonly string CheckpointString = "Checkpoints";

  /// <summary>
  /// Array of tiles that have bushes on them (obsolete)
  /// </summary>
  //public static string[] bushes = new string[] { "tree1", "tree2", "tree3", "tree4", "tree5", "streetl", "alley", "cralley", "dirtalley", "zebracross", "curveb2", "plot1", "plot2", "plot3" };

  public static TrackSavable TRACK { get; set; }
  ///<summary> Is editor loading map? </summary>
  public static bool Isloading { get; set; } = false;
  ///<summary> array representing placed elements during mapping </summary>
  public static TilePlacement[,] TilePlacementArray { get; set; }
  ///<summary> String showed on the top bar of the editor during mapping </summary>
  public static string UpperBarTrackName { get; set; } = "Untitled";
  public static string[] AllowedBushes { get; } = new []{"bush","rosebush","shrub","smltree","treetop","treetop2","treetop3","treetop4"};
  public static string DefaultTileset { get; set; } = "Default";

  /// <summary>
  /// Local coordinates of copied vertices. Put here to live when switching form-build tabs
  /// </summary>
  public static List<Vector3> CopyClipboard = new List<Vector3>();
  /// <summary>
  /// Load track by inversing elements flag
  /// </summary>
  public static bool LoadMirrored = false;
  public static int minHeight = -2000;
  public static int maxHeight = 2000;
  public static List<string> MissingTilesNames = new List<string>();
}




