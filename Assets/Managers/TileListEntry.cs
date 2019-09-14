using System.Collections.Generic;
using UnityEngine;

public class TileListEntry
{
  public string ModelPath;
  public P3DModel Model;
  public List<Material> Materials;
  public Texture Icon;
  public IntVector2 Size;
  /// <summary>
  /// Placement of tile in given editor tab
  /// </summary>
  public Vector3Int EditorPlacement;
  public int TileSetKey { get; set; }
  /// <summary>
  /// Custom RMC that this tile requires, else null
  /// </summary>
  public string CustomRMCName { get; set; }
  ///<summary>relative 0 height</summary>
  public float Pzero { get; set; }
  /// <summary>
  /// points of tiles that can be 'flattened' (e.g tunnel entry), else null
  /// </summary>
  public float[] FlatterPoints { get; set; }
  public Vegetation[] Bushes { get; set; }
  public bool IsCheckpoint { get; set; }

  public TileListEntry(float[] flatterpoints)
  {
    FlatterPoints = flatterpoints;
  }

  /// <summary>
  /// cfl constructor
  /// </summary>
  /// <param name="name"></param>
  /// <param name="editorplacement">x,y - position; z - rotation (can be 0 or 1)</param>
  public TileListEntry(Vector3Int editorplacement)
  {
    EditorPlacement = editorplacement;
  }
 
  /// <summary>
  /// cat constructor
  /// </summary>
  /// <param name="size"></param>
  /// <param name="customRMCName"></param>
  /// <param name="isCheckpoint"></param>
  /// <param name="flatterPoints"></param>
  /// <param name="bushes"></param>
  public TileListEntry(IntVector2 size, string customRMCName, bool isCheckpoint, P3DModel model, List<Material> materials, Texture icon, Vegetation[] bushes)
  {
    Size = size;
    CustomRMCName = customRMCName;
    Bushes = bushes;
    IsCheckpoint = isCheckpoint;
    Model = model;
    Materials = materials;
    Icon = icon;
  }
  public void Set(IntVector2 size, string customRMCName, bool isCheckpoint, P3DModel model, List<Material> materials, Texture icon, Vegetation[] bushes)
  {
    Size = size;
    CustomRMCName = customRMCName;
    Bushes = bushes;
    IsCheckpoint = isCheckpoint;
    Model = model;
    Materials = materials;
    Icon = icon;
  }
}
