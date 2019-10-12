using System;
using System.Collections.Generic;
using UnityEngine;

public class TileListEntry
{
  public string ModelPath;
  public P3DModel Model;
  public List<Material> Materials;
  public Texture2D Icon;
  public Vector2Int Size;
  /// <summary>
  /// Placement of tile in given editor tab
  /// </summary>
  public Vector3Int EditorPlacement;
  
  /// <summary>
  /// string with H1
  /// </summary>
  public string RMCname { get; set; }
  ///<summary>relative 0 height</summary>
  /// <summary>
  /// points of tiles that can be 'flattened' (e.g tunnel entry), else null
  /// </summary>
  public float[] FlatterPoints { get; set; }
  public Vegetation[] Bushes { get; set; }
  public bool IsCheckpoint { get; set; }
  public string Custom_tileset_id { get; set; }
  public string TilesetName { get; set; }
  public TileListEntry(float[] flatterpoints)
  {
    FlatterPoints = flatterpoints;
  }

  /// <summary>
  /// cfl constructor
  /// </summary>
  public TileListEntry(string tilesetName)
  {
    TilesetName = tilesetName;
  }

  /// <summary>
  /// cat constructor
  /// </summary>
  /// <param name="size"></param>
  /// <param name="Restrictions"></param>
  /// <param name="isCheckpoint"></param>
  /// <param name="flatterPoints"></param>
  /// <param name="bushes"></param>
  public TileListEntry(Vector2Int size, string Restrictions, bool isCheckpoint, P3DModel model, List<Material> materials, Texture2D icon, Vegetation[] bushes, string custom_tileset_id)
  {
    Size = size;
    Bushes = bushes;
    IsCheckpoint = isCheckpoint;
    Model = model;
    Materials = materials;
    Icon = icon;
    Restrictions = NormalizeRestrictions(Restrictions);
    Custom_tileset_id = custom_tileset_id;

    RMCname = Size.x.ToString() + "x" + Size.y.ToString() + Restrictions;
  }

  private string NormalizeRestrictions(string Restrictions)
  {
    // Normalize Restrictions bcoz devs fucked them up >:D
    if (Size.x == 1 && Restrictions.Contains("V2"))
      Restrictions = Restrictions.Replace("V2", "");
    if (Size.y == 1 && Restrictions.Contains("H2"))
      Restrictions = Restrictions.Replace("H2", "");

    return Restrictions;
  }

  public void Set(Vector2Int size, string Restrictions, bool isCheckpoint, P3DModel model, List<Material> materials, Texture2D icon, Vegetation[] bushes, string custom_tileset_id)
  {
    Size = size;
    Bushes = bushes;
    IsCheckpoint = isCheckpoint;
    Model = model;
    Materials = materials;
    Icon = icon;
    Custom_tileset_id = custom_tileset_id;
    Restrictions = NormalizeRestrictions(Restrictions);

    RMCname = Size.x.ToString() + "x" + Size.y.ToString() + Restrictions;
  }

  public string Show()
  {
    return this.Size.ToString() + " " + this.Icon.name;
  }
}
