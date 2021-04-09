using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Static class containing fields that need to survive during scene change
/// </summary>
public static class Service
{
  public readonly static string VERSION = "2.5";
  /// <summary>Maximum tile limit</summary>
  public readonly static int TrackTileLimit = 8000;
  /// <summary>
  /// visible vertices in second form mode
  /// </summary>
  public static Vector2Int MarkerBounds = new Vector2Int(60, 60);
  internal static readonly string CheckpointString = "Checkpoints";

  public static TrackSavable TRACK { get; set; }
  ///<summary> Is editor loading map? </summary>
  public static bool Isloading { get; set; } = false;
  ///<summary> array representing placed elements during mapping </summary>
  public static TilePlacement[,] TilePlacementArray { get; set; }
  ///<summary> String showed on the top bar of the editor during mapping </summary>
  public static string UpperBarTrackName { get; set; } = "Untitled";
  public static string DefaultTilesetName { get; set; } = "Default";
  public static float[] former_heights;
  public static float[] current_heights;
  /// <summary>
  /// Load track by inversing elements
  /// </summary>
  public static bool LoadMirrored = false;
  public readonly static int maxHeight = 20000;
  public readonly static int minHeight = -maxHeight;
  /// <summary>
  /// rayHeight = maxHeight - minHeight + 5
  /// </summary>
  public readonly static int rayHeight = maxHeight - minHeight + 5;
  public static List<string> MissingTilesNames = new List<string>();

  public static bool IsWithinMapBounds(Vector3 v)
  {
    return (v.x > 0 && v.x < 4 * Service.TRACK.Width && v.z > 0 && v.z < 4 * Service.TRACK.Height) ? true : false;
  }
  public static bool IsWithinMapBounds(float x, float z)
  {
    return (x > 0 && x < 4 * Service.TRACK.Width && z > 0 && z < 4 * Service.TRACK.Height) ? true : false;
  }
  /// <summary>
  /// Distance on 2D map between 3D points
  /// </summary>
  public static float Distance(Vector3 v1, Vector3 v2)
  {
    return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z));
  }
  public static float SliderValue2RealHeight(float sliderval)
  {
    return sliderval / 5f;
  }
  public static float RealHeight2SliderValue(float realheight)
  {
    return 5 * realheight;
  }
  /// <summary>
  /// Returns global position of vertex. Sets Y height from current_points array
  /// </summary>
  public static Vector3 IndexToPos(int index)
  {
    int x = index % (4 * Service.TRACK.Width + 1);
    Vector3 to_return = new Vector3(x, Service.current_heights[index], (index - x) / (4 * Service.TRACK.Width + 1));
    return to_return;
  }
  public static int PosToIndex(int x, int z)
  {
    return Mathf.RoundToInt(x + 4 * z * Service.TRACK.Width + z);
  }
  public static int PosToIndex(Vector3 v)
  {
    return Mathf.RoundToInt(v.x + 4 * v.z * Service.TRACK.Width + v.z);
  }
  public static GameObject CreateMarking(Material material, Vector3? pos = null, bool hasCollider = true)
  {
    GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
    znacznik.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
    znacznik.transform.position = (pos != null) ? (Vector3)pos : Highlight.pos;
    if (hasCollider)
      znacznik.GetComponent<BoxCollider>().enabled = true;
    else
      Object.Destroy(znacznik.GetComponent<BoxCollider>());
    znacznik.GetComponent<MeshRenderer>().material = material;
    
    znacznik.layer = 11;
    return znacznik;
  }
  /// <summary>
  /// Searches for znacznik in given pos. If found znacznik isn't marked, f. marks it and returns it.
  /// </summary>
  public static GameObject MarkAndReturnZnacznik(Vector3 z_pos)
  {
    z_pos.y = Service.maxHeight;
    if (Physics.Raycast(z_pos, Vector3.down, out RaycastHit hit, Service.rayHeight, 1 << 11))
    {
      if (hit.transform.name == "on")
        return hit.transform.gameObject;
      else
      {
        hit.transform.name = "on";
        hit.transform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("red");
        return hit.transform.gameObject;
      }
    }
    return null;
  }
  public static void UpdateMapColliders(List<GameObject> mcs, bool IsRecoveringTerrain = false)
  {
    if (mcs[0].layer == 11) // Argumentami znaczniki
    {
      List<int> indexes = new List<int>();
      foreach (GameObject znacznik in mcs)
      {
        Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
        indexes.Add(v.x + 4 * v.z * Service.TRACK.Width + v.z);
      }
      UpdateMapColliders(indexes, IsRecoveringTerrain);

    }
    else //Argumentami MapCollidery
    {
      foreach (GameObject mc in mcs)
      {
        Vector3[] verts = mc.GetComponent<MeshCollider>().sharedMesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
          Vector3Int v = Vector3Int.RoundToInt(mc.transform.TransformPoint(verts[i]));
          if (IsRecoveringTerrain)
          {
            verts[i].y = Service.former_heights[Service.PosToIndex(v)];
            Service.current_heights[Service.PosToIndex(v)] = Service.former_heights[Service.PosToIndex(v)];
          }
          else
            verts[i].y = Service.current_heights[Service.PosToIndex(v)];
        }
        mc.GetComponent<MeshCollider>().sharedMesh.vertices = verts;
        mc.GetComponent<MeshCollider>().sharedMesh.RecalculateBounds();
        mc.GetComponent<MeshCollider>().sharedMesh.RecalculateNormals();
        mc.GetComponent<MeshFilter>().mesh = mc.GetComponent<MeshCollider>().sharedMesh;
        mc.GetComponent<MeshCollider>().enabled = false;
        mc.GetComponent<MeshCollider>().enabled = true;
        //mc.SetActive(false);
        //mc.SetActive(true);
      }
    }
  }
  /// <summary>
  /// List of map colliders, \-/ position from index cast ray (layer=8). If hit isn't on list, add it. Run overload for gameObjects.
  /// </summary>
  public static void UpdateMapColliders(List<int> indexes, bool przywrocenie_terenu = false)
  {
    if (indexes.Count == 0)
      return;
    List<GameObject> mcs = new List<GameObject>();
    foreach (int i in indexes)
    {
      Vector3Int v = Vector3Int.RoundToInt(Service.IndexToPos(i));
      v.y = Service.maxHeight;
      RaycastHit[] hits = Physics.SphereCastAll(v, 0.002f, Vector3.down, Service.rayHeight, 1 << 8);
      foreach (RaycastHit hit in hits)
        if (!mcs.Contains(hit.transform.gameObject))
        {
          mcs.Add(hit.transform.gameObject);
        }

    }
    UpdateMapColliders(mcs, przywrocenie_terenu);
  }
  public static void UpdateMapColliders(Vector3 rmc_pos, Vector3Int tileDims, bool przywrocenie_terenu = false)
  {
    rmc_pos.y = Service.maxHeight;
    RaycastHit[] hits = Physics.BoxCastAll(rmc_pos, new Vector3(4 * tileDims.x * 0.6f, 1, 4 * tileDims.z * 0.6f), 
      Vector3.down, Quaternion.identity, Service.rayHeight, 1 << 8);
    List<GameObject> mcs = new List<GameObject>();
    foreach (RaycastHit hit in hits)
    {
      mcs.Add(hit.transform.gameObject);
    }
    UpdateMapColliders(mcs, przywrocenie_terenu);
  }
  public static float Smoothstep(float edge0, float edge1, float x)
  {
    if (edge1 == edge0)
      return 0;
    // Scale to 0 - 1
    x = (x - edge0) / (edge1 - edge0);
    //return (x * x * x * (x * (x * 6f - 15f) + 10f));
    return x * x * (3 - 2 * x);
  }
}




