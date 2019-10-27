using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
//Handles BUILD mode.
public class Building : MonoBehaviour
{
  // "real mesh collider" - RMC - plane with vertices set in positions where tile can have its terrain vertices changed
  // "znacznik" - tag - white small box spawned only in FORM mode. Form mode uses some static functions from here.
  public GameObject editorPanel; //-> slidercase.cs
  public Text CURRENTELEMENT; //name of currently selected element on top of the building menu
  public GameObject savePanel; // "save track scheme" menu

  public bool LMBclicked = false;
  public bool AllowLMB = false;
  /// <summary>obj_rmc = current RMC</summary>
  public static GameObject current_rmc;
  /// <summary>current tile</summary>
  GameObject Current_tile;
  /// <summary>former position of temporary placement of tile in build mode</summary>
  Vector3 last_trawa;
  public static bool nad_wczesniej = Highlight.over;
  /// <summary>current rotation of currently showed element</summary>
  int cum_rotation = 0;
  ///<summary> current inversion of an element that is currently being shown</summary>
  bool inversion = false;

  void Update()
  {
    if (!MouseInputUIBlocker.BlockedByUI)
    {
      if (Input.GetMouseButtonDown(1))
        cum_rotation = (cum_rotation == 270) ? 0 : cum_rotation + 90;

      CURRENTELEMENT.text = EditorMenu.tile_name;
      if (Input.GetKeyDown(KeyCode.Q))
        InverseState(); // Q enabled inversion
      if (Input.GetKey(KeyCode.X))
        XButtonState(); // X won't let PlacePrefab work
      if (Input.GetKeyDown(KeyCode.LeftAlt))
        SwitchToNULL();

      if (EditorMenu.tile_name != "NULL" && !Input.GetKey(KeyCode.Space))
      {
        if (!Highlight.over)
        {
          if (nad_wczesniej)
          {
            //Debug.Log("Było przejście na nulla z trawki");
            if (!LMBclicked && AllowLMB)
              DelLastPrefab();
            else
              LMBclicked = false;
          }
          nad_wczesniej = false;
        }
        else
        { //If cursor points on map
          if (!nad_wczesniej)
          {
            //Debug.Log("cursor: void -> terrain");
            PlaceTile(Highlight.t, EditorMenu.tile_name, cum_rotation, inversion);
            last_trawa = Highlight.t;
            nad_wczesniej = true;
          }
          else if (last_trawa.x != Highlight.t.x || last_trawa.z != Highlight.t.z)
          {
            //Debug.Log("cursor: terrain chunk -> terrain chunk");
            if (!LMBclicked)//If element hasn't been placed
              DelLastPrefab();
            PlaceTile(Highlight.t, EditorMenu.tile_name, cum_rotation, inversion);
            last_trawa = Highlight.t;
            LMBclicked = false;
          }

          if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
          { // Pick up tiles that is under cursor
            PickUpTileUnderCursor();
          }
          else if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.X) && AllowLMB)
          {//Place currently showed tile on terrain
            LMBclicked = true;
            Save_tile_properties(EditorMenu.tile_name, inversion, cum_rotation, new Vector3Int(Highlight.t.x / 4, 0, Highlight.t.z / 4));
          }
          if (Input.GetMouseButtonDown(1) && Highlight.over && !LMBclicked)
          {//Rotation with RMB
            DelLastPrefab();
            PlaceTile(Highlight.t, EditorMenu.tile_name, cum_rotation, inversion);
            nad_wczesniej = true;
          }
        }
      }
      if (Input.GetKey(KeyCode.Space) && nad_wczesniej)
      {//Delete currently showed element when moving camera with spacebar
        if (!LMBclicked)
          DelLastPrefab();
        else
          LMBclicked = false;
        nad_wczesniej = false;
      }
    }
    else if (nad_wczesniej)
    {//Delete currently showed element when moving cursor over menu
      if (!LMBclicked)
        DelLastPrefab();
      else
        LMBclicked = false;
      nad_wczesniej = false;
    }
  }
  /// <summary>
  /// Toggles off tile preview 
  /// </summary>
  void SwitchToNULL()
  {
    if (!LMBclicked)
      DelLastPrefab();
    else
      LMBclicked = false;
    nad_wczesniej = false;
    EditorMenu.tile_name = "NULL";
  }
  void InverseState()
  {
    inversion = !inversion;
    DelLastPrefab();
    nad_wczesniej = false;
  }
  void XButtonState()
  {
    DelLastPrefab();
    nad_wczesniej = false;
    if (Input.GetMouseButtonDown(0))
      Del_underlying_element();
  }
  void PickUpTileUnderCursor()
  {
    Vector3Int v = Highlight.pos;
    v.y = Data.maxHeight + 1;
    bool traf = Physics.Raycast(v, Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 9);
    if (traf)
    {
      EditorMenu.tile_name = hit.transform.name;
      editorPanel.GetComponent<SliderCase>().SwitchToTileset(TileManager.TileListInfo[EditorMenu.tile_name].TilesetName);
    }
  }
  public static bool IsCheckpoint(string nazwa_tilesa)
  {
    return TileManager.TileListInfo[nazwa_tilesa].IsCheckpoint;
  }
  public static void Del_underlying_element()
  {
    bool traf = Physics.Raycast(new Vector3(Highlight.pos.x, Data.maxHeight + 1, Highlight.pos.z), Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 9);
    if (traf && hit.transform.gameObject != current_rmc)
    {
      Vector3Int pos = Vpos2epos(hit.transform.gameObject);

      if (IsCheckpoint(Data.TilePlacementArray[pos.z, pos.x].Name))
      {
        Data.TRACK.Checkpoints.Remove((ushort)(pos.x * (SliderHeight.val - 1 - pos.y)));
        Data.TRACK.CheckpointsNumber--;
      }

      Unhide_trawkas(hit.transform.position);
      List<GameObject> to_restore = Get_surrounding_tiles(hit.transform.gameObject);
      DestroyImmediate(hit.transform.gameObject);
      Data.TilePlacementArray[pos.z, pos.x].Name = null;
      Przywroc_teren(Data.TilePlacementArray[pos.z, pos.x].t_verts.ToList());
      UpdateTiles(to_restore);
    }
  }
  static void Unhide_trawkas(Vector3 pos)
  {
    pos.y = Data.maxHeight + 1;
    RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Terraining.rayHeight, 1 << 8);
    foreach (RaycastHit hit in hits)
      hit.transform.gameObject.GetComponent<MeshRenderer>().enabled = true;
  }
  public static Vector3Int Vpos2epos(GameObject rmc)
  {
    Vector3Int to_return = new Vector3Int();
    Vector3 dim = GetTileDims(rmc);
    to_return.x = (int)((rmc.transform.position.x - 2 - 2 * (dim.x - 1)) / 4f);
    to_return.z = Mathf.RoundToInt((rmc.transform.position.z - 2 - 2 * (dim.z - 1)) / 4f);
    return to_return;
  }
  public static void DelLastPrefab()
  {
    if (current_rmc != null)
    {
      Unhide_trawkas(current_rmc.transform.position);
      Vector3Int pos = Vpos2epos(current_rmc);
      List<GameObject> surroundings = Get_surrounding_tiles(current_rmc);
      DestroyImmediate(current_rmc);
      Przywroc_teren(Data.TilePlacementArray[pos.z, pos.x].t_verts.ToList());
      UpdateTiles(surroundings);
    }
  }
  public static int[] GetRmcIndices(GameObject rmc)
  {
    List<int> to_return = new List<int>();
    Vector3Int LD = GetLDpos(rmc);
    Vector3Int tileDims = GetTileDims(current_rmc);
    for (int z = 0; z <= 4 * tileDims.z; z++)
    {
      for (int x = 0; x <= 4 * tileDims.x; x++)
      {
        to_return.Add(LD.x + x + 4 * Data.TRACK.Width * (LD.z + z) + LD.z + z);
      }
    }
    return to_return.ToArray();
  }
  /// <summary>
  /// Returns array of tiles being around object rmc_o.
  /// OR IF (znaczniki != null)
  /// Returns array of tiles being placed on terrain occupied by "znaczniki"
  /// </summary>
  public static List<GameObject> Get_surrounding_tiles(GameObject rmc_o, List<GameObject> znaczniki = null)
  {
    if (rmc_o != null)
    {
      rmc_o.layer = 10;

      List<GameObject> to_return = new List<GameObject>();
      RaycastHit[] hits = Physics.BoxCastAll(rmc_o.transform.position, rmc_o.GetComponent<MeshFilter>().mesh.bounds.size * 0.55f, Vector3.down, Quaternion.identity, Terraining.rayHeight, 1 << 9);
      foreach (RaycastHit hit in hits)
        if (hit.transform.gameObject != rmc_o)
          to_return.Add(hit.transform.gameObject);
      rmc_o.layer = 9;
      return to_return;
    }
    else // Find tiles on map
    {
      List<GameObject> to_return = new List<GameObject>();
      foreach (GameObject znacznik in znaczniki)
      {
        Vector3 pos = znacznik.transform.position;
        pos.y = Data.maxHeight + 1;
        RaycastHit[] hits = Physics.SphereCastAll(pos, 0.1f, Vector3.down, Terraining.rayHeight, 1 << 9);
        foreach (RaycastHit hit in hits)
          if (!to_return.Contains(hit.transform.gameObject))
            to_return.Add(hit.transform.gameObject);
      }
      return to_return;
    }
  }
  /// <summary>
  /// Returns bottom left point of tile in global coords
  /// </summary>
  public static Vector3Int GetLDpos(GameObject rmc_o)
  {
    Vector3Int to_return = new Vector3Int();
    Vector3 el_pos = rmc_o.transform.position;
    Vector3 dim = GetTileDims(rmc_o);
    to_return.x = Mathf.RoundToInt(el_pos.x - 2 - 2 * (dim.x - 1));
    to_return.z = Mathf.RoundToInt(el_pos.z - 2 - 2 * (dim.z - 1));

    if (to_return.z % 4 != 0 || to_return.z % 4 != 0)
    {
      Debug.LogError("Źle ustawiony LD=" + to_return);
    }
    return to_return;
  }
  //public static GameObject FindChildWhoseTagContains(GameObject parent, string tag)
  //{
  //  Transform t = parent.transform;

  //  for (int i = 0; i < t.childCount; i++)
  //  {
  //    if (t.GetChild(i).gameObject.tag.Contains(tag))
  //    {
  //      return t.GetChild(i).gameObject;
  //    }

  //  }
  //  return null;
  //}
  /// <summary>
  /// Returns real dimensions of objects (e.g. 2x1) taking rotation into consideration
  /// </summary>
  public static Vector3Int GetTileDims(GameObject rmc_o)
  {
    bool isRotated = (Mathf.RoundToInt(rmc_o.transform.rotation.eulerAngles.y) == 90 || Mathf.RoundToInt(rmc_o.transform.rotation.eulerAngles.y) == 270) ? true : false;
    Vector2Int dimVec = TileManager.GetRealDims(rmc_o.name, isRotated);
    return new Vector3Int(dimVec.x, 0, dimVec.y);
  }

  static void Save_tile_properties(string nazwa, bool inwersja, int rotacja, Vector3Int p)
  {
    Data.TilePlacementArray[p.z, p.x].Inversion = inwersja;
    Data.TilePlacementArray[p.z, p.x].Name = nazwa;
    Data.TilePlacementArray[p.z, p.x].Rotation = rotacja;
    if (TileManager.TileListInfo[nazwa].IsCheckpoint && !Data.TRACK.Checkpoints.Contains((ushort)(p.x + (Data.TRACK.Height - 1 - p.z) * Data.TRACK.Width)))
    {
      Data.TRACK.Checkpoints.Add((ushort)(p.x + (Data.TRACK.Height - 1 - p.z) * Data.TRACK.Width));
      Data.TRACK.CheckpointsNumber++;
    }

  }
  /// <summary>
  /// Recover terrain before "matching" terrain up. Tile which terrain is recovered has to be already destroyed!
  /// </summary>
  static void Przywroc_teren(List<int> indexes)
  {
    if (indexes == null || indexes.Count == 0)
      return;
    for (int i = 0; i < indexes.Count; i++)
    {
      Vector3 v = Loader.IndexToPos(indexes[i]);
      v.y = Data.maxHeight + 1;
      bool traf = Physics.SphereCast(v, 0.005f, Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 9);
      if (traf)
      {
        Loader.former_heights[indexes[i]] = hit.point.y;
        //Debug.DrawLine(v, new Vector3(v.x, -5, v.z), Color.green, 5);
      }
      else
      {
        //Debug.DrawLine(v, new Vector3(v.x, -5, v.z), Color.yellow, 5);
      }

    }
    Terraining.UpdateMapColliders(indexes, true);
  }

  /// <summary>
  /// Center of tile. real dimensions of tile.
  /// Checks if tile isn't sticking out of map boundaries
  /// </summary>
  static bool IsTherePlace4Tile(Vector3Int pos, Vector3Int tileDims)
  {
    pos.y = Data.maxHeight + 1;
    if (pos.z <= 0 || pos.z >= 4 * Data.TRACK.Height || pos.x <= 0 || pos.x >= 4 * Data.TRACK.Width)
      return false;
    RaycastHit[] hits = Physics.BoxCastAll(pos, new Vector3(4 * tileDims.x * 0.4f, 1, 4 * tileDims.z * 0.4f), Vector3.down, Quaternion.identity, Terraining.rayHeight, 1 << 9);
    return (hits.Length == 0) ? true : false;
  }

  static bool CheckPosition(int offsetx, int offsetz)
  {
    Vector3 v = new Vector3(Highlight.t.x + 2f + 4 * offsetx, Data.maxHeight + 1, Highlight.t.z + 2f + 4 * offsetz);
    //Vector3 x = new Vector3(v.x, -5, v.z);
    //Debug.DrawLine(v, x, Color.yellow, 500);
    bool traf = Physics.Raycast(v, Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 9 | 1 << 8);
    return (traf && hit.transform.gameObject.layer == 8) ? true : false;
  }

  //Looks over 'Pzero' height by tile name
  public static float GetPzero(string nazwa)
  {
    try
    {
      return -TileManager.TileListInfo[nazwa].Model.P3DHeight / (2f * 5f);
    }
    catch
    {
      Debug.LogError(nazwa);
      return 0;
    }
  }

  /// <summary>
  /// Places given tiles again onto terrain. (This function usually runs after changing terrain)
  /// </summary>
  public static void UpdateTiles(List<GameObject> rmcs)
  {
    //1. Updating only vertices of every RMC in list.
    foreach (GameObject rmc_o in rmcs)
    {
      rmc_o.layer = 9;
      Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
      Mesh rmc_mc = rmc_o.GetComponent<MeshCollider>().sharedMesh;
      //Update RMC
      Vector3[] verts = rmc.vertices;
      for (int index = 0; index < rmc.vertices.Length; index++)
      {
        Vector3Int v = Vector3Int.RoundToInt(rmc_o.transform.TransformPoint(rmc.vertices[index]));
        verts[index].y = Loader.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z];
      }
      rmc.vertices = verts;
      rmc.RecalculateBounds();
      rmc.RecalculateNormals();
      rmc_mc = null;
      rmc_mc = rmc;
      rmc_o.SetActive(false);
      rmc_o.SetActive(true);
    }
    //2. Matching edge of every rmc up if under or above given vertex already is another vertex (of another tile)
    foreach (GameObject rmc_o in rmcs)
    {
      rmc_o.layer = 10;
      Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
      Mesh rmc_mc = rmc_o.GetComponent<MeshCollider>().sharedMesh;

      // Match RMC up and take care of current_heights table
      Match_rmc2rmc(rmc_o);
      Vector3Int pos = Vpos2epos(rmc_o);
      bool Inversion = Data.TilePlacementArray[pos.z, pos.x].Inversion;
      int Rotation = Data.TilePlacementArray[pos.z, pos.x].Rotation;
      GameObject prefab = rmc_o.transform.GetChild(0).gameObject;
      //Get original dimensions
      Vector3Int tileDims = GetTileDims(rmc_o);

      //Delete old prefab and replace it with plain new.
      Vector3 old_pos = prefab.transform.position;
      string prefab_name = prefab.name;

      DestroyImmediate(prefab);
      prefab = GetPrefab(prefab_name, old_pos, Quaternion.Euler(0, Rotation, 0), rmc_o.transform);
      bool anythingchanged = false;
      Vector3Int LDpos = GetLDpos(rmc_o);
      //Debug.Log("LDpos po =" + LDpos.x + " " + LDpos.z);
      for (int z = 0; z <= 4 * tileDims.z; z++)
      {
        for (int x = 0; x <= 4 * tileDims.x; x++)
        {
          if (x != 0 && z != 0 && x != 4 * tileDims.x && z != 4 * tileDims.z)
          {
            Schowaj_8(x, z, LDpos, ref anythingchanged); // środek
          }
          else
          {
            Match_boundaries(x, z, LDpos, ref anythingchanged); //obrzeża
          }
        }
      }
      Terraining.UpdateMapColliders(rmc_o.transform.position, tileDims);
      List<Mesh> meshes = GetPrefabMeshList(Inversion, prefab);
      Tiles_to_RMC_Cast(ref meshes, prefab, Inversion);
      rmc_o.layer = 9;
    }
  }

  public static void InverseMesh(Mesh mesh)
  {
    Vector3[] verts = mesh.vertices;
    for (int i = 0; i < verts.Length; i++)
      verts[i] = new Vector3(-verts[i].x, verts[i].y, verts[i].z);
    mesh.vertices = verts;

    for (int i = 0; i < mesh.subMeshCount; i++) // Każdemu materiałowi trzeba przypisać tablicę trójkątów
    {
      int[] trgs = mesh.GetTriangles(i);
      mesh.SetTriangles(trgs.Reverse().ToArray(), i);
    }

  }
  public static GameObject GetPrefab(string TileName, Vector3 position, Quaternion rotation, Transform parent)
  {
    //set the model and textures for the tile
    GameObject Prefab = new GameObject();
    Mesh m = TileManager.TileListInfo[TileName].Model.CreateMesh();
    var mf = Prefab.AddComponent<MeshFilter>();
    mf.mesh = m;
    var mr = Prefab.AddComponent<MeshRenderer>();
    mr.materials = TileManager.TileListInfo[TileName].Materials.ToArray();
    Prefab.transform.position = position;
    Prefab.transform.rotation = rotation;
    Prefab.transform.SetParent(parent);
    Prefab.name = TileName;
    Prefab.transform.localScale /= 5f;
    return Prefab;
  }

  /// <summary>
  /// Places tile having given: bottom-left position, its name, rotation, inversion
  /// </summary>
  public GameObject PlaceTile(Vector3Int LDpos, string name, int cum_rotation, bool inwersja = false)
  {
    AllowLMB = false;
    if (Input.GetKey(KeyCode.X))
      return null;

    Quaternion rotate_q = Quaternion.Euler(new Vector3(0, cum_rotation, 0));
    //Get original dimensions
    Vector3Int tileDims = new Vector3Int(TileManager.TileListInfo[name].Size.x, 0, TileManager.TileListInfo[name].Size.y);
    GameObject rmc_PRE = GetRMC(name);
    //Get real dims of tile
    if (cum_rotation == 90 || cum_rotation == 270)
    {
      int pom = tileDims.x;
      tileDims.x = tileDims.z;
      tileDims.z = pom;
    }
    Vector3Int rmcPlacement = new Vector3Int(LDpos.x + 2 + 2 * (tileDims.x - 1), 0, LDpos.z + 2 + 2 * (tileDims.z - 1));

    if (!Data.Isloading)
    {
      if (!IsTherePlace4Tile(rmcPlacement, tileDims))
      {
        current_rmc = null;
        return null;
      }
    }
    AllowLMB = true;
    //______________________
    //PLACE RMC ONTO TERRAIN
    //----------------------
    current_rmc = Instantiate(rmc_PRE, rmcPlacement, rotate_q);

    if (inwersja)
      InverseMesh(current_rmc.GetComponent<MeshFilter>().mesh);

    current_rmc.name = name;

    Mesh rmc = current_rmc.GetComponent<MeshFilter>().mesh;
    current_rmc.layer = 10;

    rmc.MarkDynamic();

    Vector3[] verts = rmc.vertices;

    MeshCollider rmc_mc = current_rmc.AddComponent<MeshCollider>();

    //Align RMC
    for (int index = 0; index < rmc.vertices.Length; index++)
    {
      Vector3Int v = Vector3Int.RoundToInt(current_rmc.transform.TransformPoint(rmc.vertices[index]));
      verts[index].y = Loader.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z];
    }

    //update RMC
    rmc.vertices = verts;
    rmc.RecalculateBounds();
    rmc.RecalculateNormals();
    rmc_mc.sharedMesh = null;
    rmc_mc.sharedMesh = rmc;
    current_rmc.GetComponent<MeshRenderer>().enabled = false;
    if (!Data.Isloading)
    {
      List<GameObject> rmcsToUpdate = new List<GameObject>();
      bool anythingchanged = false;
      //_______________________
      //Match terrain to rmc
      //---------------------------
      for (int z = 0; z <= 4 * tileDims.z; z++)
      {
        for (int x = 0; x <= 4 * tileDims.x; x++)
        {
          if (z != 0 && z != 4 * tileDims.z && x != 0 && x != 4 * tileDims.x)
          {
            Schowaj_8(x, z, LDpos, ref anythingchanged); // środek
          }
          else
          {
            rmcsToUpdate = Match_boundaries(x, z, LDpos, ref anythingchanged, rmcsToUpdate); //obrzeża
          }
        }
      }
      if (anythingchanged)
      {
        Terraining.UpdateMapColliders(current_rmc.transform.position, tileDims);
      }

      if (rmcsToUpdate != null)
      {
        UpdateTiles(rmcsToUpdate);
      }
    }
    //_________________________
    //PLACE TILE ONTO RMC
    //-------------------------
    GameObject Prefab = GetPrefab(name, rmcPlacement, Quaternion.Euler(new Vector3(0, cum_rotation, 0)), current_rmc.transform);

    List<Mesh> meshes = GetPrefabMeshList(inwersja, Prefab);
    Tiles_to_RMC_Cast(ref meshes, Prefab, inwersja);

    current_rmc.layer = 9;
    Data.TilePlacementArray[LDpos.z / 4, LDpos.x / 4].t_verts = GetRmcIndices(current_rmc);

    if (Data.Isloading)
    {
      Save_tile_properties(name, inwersja, cum_rotation, new Vector3Int(LDpos.x / 4, 0, LDpos.z / 4));
      return current_rmc;
    }
    else
      return null;
  }

  private GameObject GetRMC(string nazwa_tilesa)
  {
    //Debug.Log("GetRMC" + TileManager.TileListInfo[nazwa_tilesa].RMCname + " in " + nazwa_tilesa);
    return Resources.Load<GameObject>("rmcs/" + TileManager.TileListInfo[nazwa_tilesa].RMCname);

  }

  /// <summary>
  /// Lista meshów. \-/ dziecka prefaba zaznacz MarkDynamic(), zainwersjuj, dodaj do listy. Jeśli nie ma dzieci, to prefab dodaj do listy meshów
  /// returns nested meshes of given tile in simple list
  /// </summary>
  static List<Mesh> GetPrefabMeshList(bool inwersja, GameObject prefab)
  {
    List<Mesh> meshes = new List<Mesh>();
    if (prefab.transform.childCount != 0)
    {
      for (int i = 0; i < prefab.transform.childCount; i++)
      {
        if (prefab.transform.GetChild(i).tag != "krzaczor" && prefab.transform.GetChild(i).GetComponent<MeshRenderer>().enabled)
        {
          prefab.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh.MarkDynamic();
          if (inwersja)
            InverseMesh(prefab.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh);
          meshes.Add(prefab.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh);
        }
      }
    }
    else
    {
      prefab.GetComponent<MeshFilter>().mesh.MarkDynamic();
      if (inwersja)
        InverseMesh(prefab.GetComponent<MeshFilter>().mesh);
      meshes.Add(prefab.GetComponent<MeshFilter>().mesh);
    }
    return meshes;
  }

  /// <summary>
  /// getPzero dla nazwa_tilesa, \-/ z meshes ray na 10. Jeśli nie trafił to na 8, jak nie trafił to podstawową wysokością jest Bounding height
  /// Logic for placing tile onto RMC. Used in PlacePrefab and UpdateTiles
  /// </summary>
  static void Tiles_to_RMC_Cast(ref List<Mesh> meshes, GameObject prefab, bool inwersja)
  {
    float pzero = GetPzero(prefab.name);
    // Raycast tiles(H) \ rmc
    foreach (Mesh mesh in meshes)
    {
      Vector3[] verts = mesh.vertices;
      for (int i = 0; i < mesh.vertices.Length; i++)
      {
        Vector3 v = prefab.transform.TransformPoint(mesh.vertices[i]);
        if (Physics.Raycast(new Vector3(v.x, Data.maxHeight + 1, v.z), Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 10))
        { // own rmc
          verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
        }
        else
        if (Physics.SphereCast(new Vector3(v.x, Data.maxHeight + 1, v.z), 0.005f, Vector3.down, out hit, Terraining.rayHeight, 1 << 10))
        { // due to the fact rotation in unity is stored in quaternions using floats you won't always hit mesh collider with one-dimensional raycasts. 
          verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
        }
        else
        { // when tile vertex is out of its dimensions (eg crane), cast on foreign rmc or map
          if (Physics.SphereCast(new Vector3(v.x, Data.minHeight - 1, v.z), 0.2f, Vector3.up, out hit, Terraining.rayHeight, 1 << 9 | 1 << 8))
            verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
          else // out of map boundaries: height of closest edge
            verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, Loader.current_heights[0] + v.y - pzero, v.z));
        }
      }
      mesh.vertices = verts;
      mesh.RecalculateBounds();
      mesh.RecalculateNormals();
    }
    UpdateBushes(prefab, inwersja);
  }

  public static void UpdateBushes(GameObject prefab, bool inwersja)
  {
    // TODO
    //foreach (Vegetation V in TileManager.TileListInfo[prefab.name].Bushes)
    //{
    //  GameObject tree_PRE = Resources.Load<GameObject>("vege/" + V.Name);
    //  Vector3 v = V.Position / 5f;
    //  v.x = (inwersja) ? -v.x : v.x;
    //  v = prefab.transform.TransformPoint(v);
    //  Physics.Raycast(new Vector3(v.x, Data.maxHeight + 1, v.z), Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 10);
    //  v.y = hit.point.y + tree_PRE.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y / 5f;
    //  GameObject tree = Instantiate(tree_PRE, v, prefab.transform.rotation, prefab.transform);
    //}
  }

  /// <summary>
  ///Returns mesh "main" of tile or if tile doesn't have it, mesh of its meshfilter
  /// </summary>
  public static Mesh GetMainMesh(ref GameObject prefab)
  {
    if (prefab.transform.childCount != 0)
    {
      for (int i = 0; i < prefab.transform.childCount; i++)
        if (prefab.transform.GetChild(i).name == "main")
          return prefab.transform.GetChild(i).GetComponent<MeshFilter>().mesh;

      return null; // <-- Never going to end up here, hopefully
    }
    else
      return prefab.GetComponent<MeshFilter>().mesh;
  }
  /// <summary>
  /// Toggles off visibility of terrain chunk laying under tile of bottom-left x,z. 
  /// Updates current_heights array. Layer of RMC has to be 10.
  /// </summary>
  public static void Schowaj_8(int x, int z, Vector3Int LDpos, ref bool anythingchanged)
  {
    x += LDpos.x;
    z += LDpos.z; //Mamy x,y są teraz globalne
    int index = x + 4 * z * Data.TRACK.Width + z;
    Vector3 v = new Vector3(x, Data.minHeight - 1, z);
    if (Physics.Raycast(v, Vector3.up, out RaycastHit hit, Terraining.rayHeight, 1 << 10) && Mathf.Abs(Loader.current_heights[index] - hit.point.y) > 0.01f)
    {
      Loader.current_heights[index] = hit.point.y;
      anythingchanged = true;
    }

    RaycastHit[] hits = Physics.SphereCastAll(v, 0.01f, Vector3.up, Terraining.rayHeight, 1 << 8);
    foreach (RaycastHit h in hits)
      h.transform.gameObject.GetComponent<MeshRenderer>().enabled = false;
  }
  /// <summary>
  /// Matches up height of terrain to height of vertex of current RMC (layer = 10)
  /// </summary>
  public static List<GameObject> Match_boundaries(int x, int z, Vector3Int LDpos, ref bool anythingchanged, List<GameObject> toUpdate = null)
  {
    x += LDpos.x;
    z += LDpos.z;
    Vector3Int v = new Vector3Int(x, Data.maxHeight + 1, z);
    int index = (x + 4 * z * Data.TRACK.Width + z);
    if (Physics.SphereCast(v, 0.005f, Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 10) && Mathf.Abs(Loader.current_heights[index] - hit.point.y) > 0.1f)
    {
      anythingchanged = true;
      Loader.current_heights[index] = hit.point.y;

    }
    if (toUpdate != null)
    {
      RaycastHit[] hits_to_update = Physics.SphereCastAll(v, 0.1f, Vector3.down, Terraining.rayHeight, 1 << 9);
      foreach (RaycastHit h in hits_to_update)
      {
        if (!toUpdate.Contains(h.transform.gameObject))
          toUpdate.Add(h.transform.gameObject);
      }
      return toUpdate;
    }
    return null;
  }
  /// <summary>
  /// Matches vertices of RMCs one to another
  /// </summary>
  public static void Match_rmc2rmc(GameObject rmc_o)
  {

    Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
    Vector3[] verts = rmc.vertices;
    for (int index = 0; index < verts.Length; index++)
    {
      Vector3Int v = Vector3Int.RoundToInt(rmc_o.transform.TransformPoint(rmc.vertices[index]));
      if (Physics.Raycast(new Vector3(v.x, Data.maxHeight + 1, v.z), Vector3.down, out RaycastHit hit, Terraining.rayHeight, 1 << 9))
      {
        //Mamy znaczniki i tutaj jest punkt zmienionej wysokości (czerwony kwadracik)
        if (Physics.SphereCast(new Vector3(v.x, Data.maxHeight + 1, v.z), 0.005f, Vector3.down, out RaycastHit sgnHit, Terraining.rayHeight, 1 << 11) && sgnHit.transform.name == "on")
        {
          //Sprawdź czy rmc layer=9 ma tutaj vertexa.
          {
            bool rmc9matuvertexa = false;
            foreach (Vector3 vo in hit.transform.gameObject.GetComponent<MeshFilter>().mesh.vertices)
            {
              Vector3Int vert = Vector3Int.RoundToInt(hit.transform.gameObject.transform.TransformPoint(vo));
              if (vert.x + 4 * vert.z * Data.TRACK.Width + vert.z == v.x + 4 * v.z * Data.TRACK.Width + v.z)
              {
                rmc9matuvertexa = true;
                break;
              }
            }
            if (rmc9matuvertexa)
            {
              //Debug.DrawLine(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.red, Terenowanie.rayHeight);
              verts[index].y = Loader.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z];
            }
            else
            {
              //Debug.DrawLine(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.gray, Terenowanie.rayHeight);
              verts[index].y = hit.point.y;
              //Helper.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z] = hit.point.y;
            }
          }

        }
        else // Normalne dorównanko
        {
          verts[index].y = hit.point.y;
          //Helper.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z] = hit.point.y;
        }

      }
    }
    rmc.vertices = verts;
    rmc.RecalculateBounds();
    rmc.RecalculateNormals();
    rmc_o.SetActive(false);
    rmc_o.SetActive(true);
  }
}


