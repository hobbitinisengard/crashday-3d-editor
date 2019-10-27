using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// 2nd script. First in editor scene.
/// <summary>
/// Takes care of creating and loading track
/// </summary>
public class Loader : MonoBehaviour
{
  public GameObject TilesetContainer;
  public GameObject TabTemplate;
  public GameObject Tile1x1Template;
  public GameObject Tile2x1Template;
  public GameObject Tile1x2Template;
  public GameObject Tile2x2Template;
  public TextAsset flatters;
  public Material thismaterial;
  /// <summary>
  /// Text in upperPanel
  /// </summary>
  public Text nazwa_toru;
  public GameObject editorPanel;
  public static float[] former_heights;
  public static float[] current_heights;
  public static float multiplier = 1;

  void Awake()
  {

    if (Data.Isloading)
    {
      Data.MissingTilesNames.Clear();
      InitializeTilePlacementArray(Data.TRACK.Height, Data.TRACK.Width);
      // Load tiles layout from TRACK to TilePlacementArray
      for (int z = 0; z < Data.TRACK.Height; z++)
      {
        for (int x = 0; x < Data.TRACK.Width; x++)
        {
          //  tiles bigger than 1x1 have funny max uint numbers around center block. We ignore them as well as empty grass fields (FieldId = 0)  
          if (Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x].FieldId < Data.TRACK.FieldFiles.Count && Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x].FieldId != 0)
          {
            // assignment for clarity
            TrackTileSavable tile = Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x];

            // without .cfl suffix
            string TileName = Data.TRACK.FieldFiles[tile.FieldId].Substring(0, Data.TRACK.FieldFiles[tile.FieldId].Length - 4);

            // Don't load tiles that haven't been loaded
            if (!TileManager.TileListInfo.ContainsKey(TileName))
            {
              if (!Data.MissingTilesNames.Contains(TileName))
                Data.MissingTilesNames.Add(TileName);
              continue;
            }

            int Rotation = tile.Rotation * 90;
            bool Inversion = tile.IsMirrored == 0 ? false : true;
            //Inversed tiles rotate anti-clockwise so..
            if (Inversion && Rotation != 0)
              Rotation = 360 - Rotation;

            Vector2Int dim = TileManager.GetRealDims(TileName, (Rotation == 90 || Rotation == 270) ? true : false);
            if (!Data.LoadMirrored)
              Data.TilePlacementArray[z - dim.y + 1, x].Set(TileName, Rotation, Inversion);
            else
              Data.TilePlacementArray[z - dim.y + 1, Data.TRACK.Width - 1 - x - dim.x + 1].Set(TileName, 360 - Rotation, !Inversion);
          }
        }
      }
      InitializeHeightArrays(Data.TRACK.Height, Data.TRACK.Width);
      LoadTerrain(Data.LoadMirrored);
    }
    else
    { // load new track
      Data.TRACK = new TrackSavable((ushort)SliderWidth.val, (ushort)SliderHeight.val);
      InitializeHeightArrays(Data.TRACK.Height, Data.TRACK.Width);
      InitializeTilePlacementArray(SliderHeight.val, SliderWidth.val);
      Data.UpperBarTrackName = "Untitled";
    }

    if (Data.LoadMirrored)
      Data.UpperBarTrackName += " (mirrored)";

    nazwa_toru.text = Data.UpperBarTrackName;
    CreateScatteredMeshColliders();

    if (Data.Isloading)
      PlaceLoadedTilesOnMap();
    else
      Data.Isloading = false;

    // Fills sliderCase slider with tabs. This includes custom tilesets.
    // -----------------------------------------------------------------
    // Create empty tileset tabs
    string[] Tilesets = TileManager.TileListInfo.Select(t => t.Value.TilesetName).Distinct().ToArray();
    editorPanel.GetComponent<SliderCase>().InitializeSlider(Tilesets);
    foreach (var tileset_name in Tilesets)
    {
      GameObject NewTab = Instantiate(TabTemplate, TilesetContainer.transform);
      // tabs' name are the same as tilesets' names
      NewTab.name = tileset_name;
    }
    // Populate created tabs with tiles
    foreach (var TileKV in TileManager.TileListInfo)
    {
      GameObject NewTile = Instantiate(Tile1x1Template, TilesetContainer.transform.Find(TileKV.Value.TilesetName.ToString()).GetComponent<ScrollRect>().content);
      NewTile.name = TileKV.Key;
      NewTile.transform.GetChild(0).name = TileKV.Key;
      NewTile.transform.GetChild(0).GetComponent<Image>().sprite = Sprite.Create(TileKV.Value.Icon, new Rect(Vector2.zero, new Vector2(TileKV.Value.Icon.width, TileKV.Value.Icon.height)), Vector2.zero);
      NewTile.transform.GetChild(0).localScale = new Vector3(0.92f, 0.92f, 1);
      NewTile.AddComponent<ShowTileName>();
      NewTile.SetActive(true);
    }
    editorPanel.GetComponent<SliderCase>().SwitchToTileset(Data.CheckpointString);
  }
  private void InitializeHeightArrays(int Height, int Width)
  {
    // initialize height arrays
    former_heights = Enumerable.Repeat(0f, (4 * Height + 1) * (4 * Width + 1)).ToArray();
    current_heights = Enumerable.Repeat(0f, (4 * Height + 1) * (4 * Width + 1)).ToArray();
  }

  private void InitializeTilePlacementArray(int height, int width)
  {
    Data.TilePlacementArray = new TilePlacement[height, width];
    for (int z = 0; z < height; z++)
    {
      for (int x = 0; x < width; x++)
      {
        Data.TilePlacementArray[z, x] = new TilePlacement();
      }
    }
  }

  /// <summary>
  ///Loads terrain from Data.TRACK to current_heights and former_heights
  /// </summary>
  void LoadTerrain(bool LoadMirrored)
  {
    for (int z = 0; z < 4 * Data.TRACK.Height + 1; z++)
    {
      for (int x = 0; x < 4 * Data.TRACK.Width + 1; x++)
      {
        int i;
        if (LoadMirrored)
          i = 4 * Data.TRACK.Width - x + z * (4 * Data.TRACK.Width + 1);
        else
          i = x + z * (4 * Data.TRACK.Width + 1);

        current_heights[i] = multiplier * Data.TRACK.Heightmap[4 * Data.TRACK.Height - z][x] / 5f;
        former_heights[i] = current_heights[i];
      }
    }
  }
  /// <summary>
  /// Creates 5x5 vertices 1x1 grass
  /// </summary>
  void CreateScatteredMeshColliders()
  {
    Mesh basic = Resources.Load<Mesh>("rmcs/basic");
    for (int z = 0; z < Data.TRACK.Height; z++)
    {
      for (int x = 0; x < Data.TRACK.Width; x++)
      {
        GameObject element = new GameObject("Map " + x + " " + z);
        element.transform.position = new Vector3Int(4 * x, 0, 4 * z);
        MeshCollider mc = element.AddComponent<MeshCollider>();
        MeshFilter mf = element.AddComponent<MeshFilter>();
        MeshRenderer mr = element.AddComponent<MeshRenderer>();
        mr.material = thismaterial;
        mf.mesh = Instantiate(basic);
        mc.sharedMesh = Instantiate(basic);
        Terraining.UpdateMapColliders(new List<GameObject> { element });
        element.layer = 8;
      }
    }
  }

  void PlaceLoadedTilesOnMap()
  {
    List<GameObject> to_update = new List<GameObject>();
    for (int z = 0; z < Data.TRACK.Height; z++)
    {
      for (int x = 0; x < Data.TRACK.Width; x++)
      {
        if (Data.TilePlacementArray[z, x].Name == null)
          continue;
        to_update.Add(editorPanel.GetComponent<Building>().PlaceTile(new Vector3Int(4 * x, 0, 4 * z), Data.TilePlacementArray[z, x].Name, Data.TilePlacementArray[z, x].Rotation, Data.TilePlacementArray[z, x].Inversion));
      }
    }
    Building.UpdateTiles(to_update);
    Data.Isloading = false;
  }

  /// <summary>
  /// Zwraca globalne położenie vertexa. Ustawia Y z current_heights
  /// </summary>
  public static Vector3 IndexToPos(int index)
  {
    int x = index % (4 * Data.TRACK.Width + 1);
    Vector3 to_return = new Vector3(x, current_heights[index], (index - x) / (4 * Data.TRACK.Width + 1));
    return to_return;
  }
  public static int PosToIndex(Vector3 v)
  {
    return Mathf.RoundToInt(v.x + 4 * v.z * Data.TRACK.Width + v.z);
  }
  //static GameObject CreatePlane(int pnx, int pny, Material feedmaterial)
  //{
  //    List<Vector3> vertz = new List<Vector3>();
  //    List<Vector2> uvs = new List<Vector2>();
  //    List<int> tris = new List<int>();
  //    //Tworzę vertexy i uv
  //    for (int y = 0; y < pny; y++)
  //    {
  //        for (int x = 0; x < pnx; x++)
  //        {
  //            vertz.Add(new Vector3(x, 0, y));
  //            uvs.Add(new Vector2(x, y));
  //        }
  //    }
  //    //Tworzę tris
  //    for (int y = 0; y < pny - 1; y++)
  //    {
  //        for (int i = 0; i < pnx - 1; i++)
  //        {
  //            tris.Add(y * pnx + i);
  //            tris.Add(y * pnx + i + pnx);
  //            tris.Add(y * pnx + i + 1);

  //            tris.Add(y * pnx + i + pnx);
  //            tris.Add(y * pnx + i + pnx + 1);
  //            tris.Add(y * pnx + i + 1);
  //        }
  //    }
  //    go = new GameObject("Map");
  //    go.transform.position = new Vector3(0, 0, 0);
  //    MeshFilter mf = go.AddComponent<MeshFilter>();
  //    MeshRenderer mr = go.AddComponent<MeshRenderer>();
  //    Mesh m = new Mesh();
  //    m.SetVertices(vertz);
  //    m.SetUVs(0, uvs);
  //    m.triangles = tris.ToArray();
  //    mf.sharedMesh = m;
  //    mr.material = feedmaterial;
  //    mf.sharedMesh.RecalculateNormals();
  //    return go;
  //}

}
