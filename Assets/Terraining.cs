using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class DuVec3
{
  public Vector3 P1;
  public Vector3 P2;
  public DuVec3(Vector3 p1, Vector3 p2)
  {
    P1 = p1;
    P2 = p2;
  }
}
//handles terrain forming (FORM mode)
public class Terraining : MonoBehaviour
{
  public GameObject FormMenu;
  public GameObject savePanel;
  public GameObject CopyText;
  public Text state_help_text;
  public Slider slider;
  public Material transp;
  public Material red;
  public Material white;
  public Button toslider; //button TO SLIDER
  public Button Flatten;
  public Button Jumper;
  public Button Prostry;
  public Button JumperEnd;
  public Button Integral;
  public Button CopyButton;
  public Button InverseButton;
  public Button RotateButton;
  public Toggle KeepShape;
  public Toggle Connect;
  public Text HelperInputField; // Text for setting height by numpad
  public Text SelectionRotation; // Helper text for showing current rotation
  int index = 0; //Indexy do meshow dla vertexa
  float slider_realheight;
  private int SelectionRotationVal = 0;
  public static GameObject indicator;
  public static List<GameObject> znaczniki = new List<GameObject>();
  public static List<GameObject> surroundings = new List<GameObject>();
  public static int rayHeight = Data.maxHeight - Data.minHeight + 5;
  public static Vector3Int max_verts_visible_dim = new Vector3Int(60, 0, 60); // Vector3 of visible vertices in second form mode

  /// <summary>
  /// Currently selected tile in terraining mode
  /// </summary>
  GameObject current;

  public static bool firstFormingMode = true; //  F - toggles forming mode
  public static bool istilemanip = false;
  public static bool isSelecting = false; //Selecting vertices mode (white <-> red)
  bool waiting4LD = false; //After selecting shape, state of waiting for bottom-left vertex
  bool waiting4LDpassed = false; // state of execution of shape after waiting for bottom-left vertex
  bool is_entering_keypad_value = false; // used in numericenter();
  int menucontrol = 1; // 1=firstFormingMode 2=second forming mode
  string last_form_button;
  Vector3Int LD;
  float LDH; // auxiliary value for height of bottom-left vertex
  Vector3 mousePosition1;
  string buffer;
  void Awake()
  {
    toslider.onClick.AddListener(() =>
    {
      if (KeepShape.isOn)
        last_form_button = "to_slider";
      else
        FormMenu_toSlider();
    });
    Prostry.onClick.AddListener(() => last_form_button = "prostry");
    Integral.onClick.AddListener(() => last_form_button = "integral");
    Jumper.onClick.AddListener(() => last_form_button = "jumper");
    JumperEnd.onClick.AddListener(() => last_form_button = "jumperend");
    Flatten.onClick.AddListener(() => last_form_button = "flatter");
    CopyButton.onClick.AddListener(() => last_form_button = "copy");
    RotateButton.onClick.AddListener(RotateClockwiseSelection);
    InverseButton.onClick.AddListener(InverseSelection);
    state_help_text.text = "Manual forming..";
  }
  void Update()
  {

    UndoKeyWorks();
    ManageCopyPasteVertices();
    Numericenter();
    mousewheelcheck();
    SetFormingMode();
    Ctrl_key_works();
    istilemanip_state(); //selecting vertices. (PPM disables it)
    waiting4LD_state(); //  to get bottom-left vertex
    selectShape();
    menucontrol = Control();
    if (menucontrol == 1)
    {
      if (!Input.GetKey(KeyCode.LeftControl)) //jeżeli nie było ctrl_key_works()
      {
        if (Input.GetMouseButtonUp(0))
          UndoBuffer.ApplyOperation();
        if (!MouseInputUIBlocker.BlockedByUI)
        {
          if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
            Single_vertex_manipulation(); // single-action
          else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
            Single_vertex_manipulation(); //auto-fire
          else if (Input.GetMouseButtonDown(1) && Highlight.over)
            Make_elevation();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {//ESC toggles off Make_Elevation()
          index = 0;
          if (indicator != null)
            Destroy(indicator);
        }
      }
    }
    else if (menucontrol == 2)
    {
      // form Menu
      if (indicator != null)
      {
        Destroy(indicator);
        index = 0;
      }
      if (!MouseInputUIBlocker.BlockedByUI && Input.GetMouseButtonDown(0))
      {
        HandleVertexBoxes(Input.GetKey(KeyCode.Q));
      }
    }
  }
  private void UndoKeyWorks()
  {
    if (Input.GetKeyUp(KeyCode.Z))
      UndoBuffer.PasteUndoZnaczniki();
  }
  private void Hide_text_helper()
  {
    slider.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
    HelperInputField.text = "";
    is_entering_keypad_value = false;
  }
  public void ManageCopyPasteVertices()
  {
    if (Input.GetKeyDown(KeyCode.C) && IsAnyZnacznikMarked())
      last_form_button = "copy";
    if (Input.GetKeyDown(KeyCode.R))
      RotateClockwiseSelection();
    if (Input.GetKeyDown(KeyCode.V))
      PasteSelectionOntoTerrain();
    if (Input.GetKeyDown(KeyCode.M))
      InverseSelection();
    if (Input.GetKeyDown(KeyCode.LeftAlt))
    {
      CopyText.SetActive(false);
      Data.CopyClipboard.Clear();
    }
  }
  public bool IsAnyZnacznikMarked()
  {
    foreach (var z in znaczniki)
    {
      if (z.name == "on")
        return true;
    }
    return false;
  }
  private void Numericenter()
  {
    if (Input.GetKeyDown(KeyCode.KeypadMultiply))
    {
      try
      {
        slider.value = float.Parse(HelperInputField.text);
        Hide_text_helper();
      }
      catch
      {
        slider.value = 0f;
        Hide_text_helper();
      }
    }
    KeyCode[] keyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.KeypadPeriod, KeyCode.KeypadMinus };
    for (int i = 0; i < keyCodes.Length; i++)
    {
      if (Input.GetKeyDown(keyCodes[i]))
      {
        slider.transform.GetChild(1).gameObject.transform.GetChild(1).gameObject.SetActive(false);
        if (i < 10)
          HelperInputField.text += i.ToString();
        else if (i == 10)
          HelperInputField.text += ",";
        else if (i == 11)
          HelperInputField.text += "-";
        is_entering_keypad_value = true;
        return;
      }
    }
  }

  /// <summary>
  /// a) Given list of map colliders => \-/ MapCollider \-/ vertex of that mc update its position (using former_heights)
  /// b) Given list of znaczniki(tags) => List of indexes of vertices, \-/ tag save its position to list, then run overload using list of int 
  /// </summary>
  public static void UpdateMapColliders(List<GameObject> mcs, bool IsRecoveringTerrain = false)
  {
    if (mcs[0].layer == 11) // Argumentami znaczniki
    {
      List<int> indexes = new List<int>();
      foreach (GameObject znacznik in mcs)
      {
        Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
        indexes.Add(v.x + 4 * v.z * Data.TRACK.Width + v.z);
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
            verts[i].y = Loader.former_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z];
            Loader.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z] = Loader.former_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z];
          }
          else
            verts[i].y = Loader.current_heights[v.x + 4 * v.z * Data.TRACK.Width + v.z];
        }
        mc.GetComponent<MeshCollider>().sharedMesh.vertices = verts;
        mc.GetComponent<MeshCollider>().sharedMesh.RecalculateBounds();
        mc.GetComponent<MeshCollider>().sharedMesh.RecalculateNormals();
        mc.GetComponent<MeshFilter>().mesh = mc.GetComponent<MeshCollider>().sharedMesh;
        mc.GetComponent<MeshCollider>().enabled = false;
        mc.GetComponent<MeshCollider>().enabled = true;
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
      Vector3Int v = Vector3Int.RoundToInt(Loader.IndexToPos(i));
      v.y = Data.maxHeight + 1;
      RaycastHit[] tile_raycasts = Physics.SphereCastAll(v, 0.1f, Vector3.down, rayHeight, 1 << 8);
      GameObject[] tiles = tile_raycasts.Where(tile => !mcs.Contains(tile.transform.gameObject)).Select(tile => tile.transform.gameObject).ToArray();
      mcs.AddRange(tiles);
    }
    UpdateMapColliders(mcs, przywrocenie_terenu);
  }
  public static void UpdateMapColliders(Vector3 rmc_pos, Vector3Int tileDims, bool przywrocenie_terenu = false)
  {
    rmc_pos.y = Data.maxHeight + 1;
    RaycastHit[] hits = Physics.BoxCastAll(rmc_pos, new Vector3(4 * tileDims.x * 0.6f, 1, 4 * tileDims.z * 0.6f), Vector3.down, Quaternion.identity, rayHeight, 1 << 8);
    List<GameObject> mcs = new List<GameObject>();
    mcs.AddRange(hits.Select(hit => hit.transform.gameObject).ToArray());
    UpdateMapColliders(mcs, przywrocenie_terenu);
  }

  float FindLowestY(List<GameObject> znaczniki)
  {
    float lowest = 40;
    foreach (GameObject znacznik in znaczniki)
      if (znacznik.name == "on" && lowest > znacznik.transform.position.y)
        lowest = znacznik.transform.position.y;
    return lowest;
  }
  float FindHighestY(List<GameObject> znaczniki)
  {
    float highest = -20;
    foreach (GameObject znacznik in znaczniki)
      if (znacznik.name == "on" && highest < znacznik.transform.position.y)
        highest = znacznik.transform.position.y;
    return highest;
  }
  void SetFormingMode()
  {
    if (!firstFormingMode && Input.GetKeyDown(KeyCode.F)) // Going from 2nd mode to 1st
    {
      waiting4LD = false;
      isSelecting = false;
      istilemanip = false;
      Del_znaczniki();
      state_help_text.text = "Manual forming..";
      firstFormingMode = true;
    }
    else if (firstFormingMode && Input.GetKeyDown(KeyCode.F)) //Going from 1st mode to 2nd
    {
      firstFormingMode = false;
      state_help_text.text = "Shape forming..";
    }
  }

  void istilemanip_state()
  {
    if (!waiting4LD)
    {
      if (istilemanip)
      {
        if (Input.GetMouseButtonDown(1) || Input.GetKey(KeyCode.Escape))
        { // RMB  or ESC turns formMenu off
          istilemanip = false;
          Del_znaczniki();
          state_help_text.text = "Shape forming..";
        }
        else
          MarkVerticesOfSelectedTile();
      }
    }
  }

  void selectShape()
  {
    if (waiting4LDpassed)
    {
      state_help_text.text = "Shape forming..";
      if (last_form_button != "")
      {
        if (last_form_button == "to_slider")
          FormMenu_toSlider();
        else if (last_form_button == "copy")
          CopySelectionToClipboard();
        else
          ApplyFancyShape();

        waiting4LDpassed = false;
        last_form_button = null;
        KeepShape.isOn = false;
      }
    }
  }
  public void CopySelectionToClipboard()
  {
    RaycastHit hit;
    Physics.Raycast(new Vector3(LD.x, LDH + 1, LD.z), Vector3.down, out hit, rayHeight, 1 << 11);
    Data.CopyClipboard.Clear();
    Data.CopyClipboard.Add(Vector3.zero);
    foreach (var mrk in znaczniki)
    {
      if (mrk.name == "on")
      {
        Vector3 pom = mrk.transform.position - hit.transform.position;
        if (pom == Vector3.zero)
          continue;
        pom.y = mrk.transform.position.y;
        Data.CopyClipboard.Add(pom);
      }
    }
    SelectionRotationVal = 0;
    CopyText.GetComponent<Text>().text = SelectionRotationVal.ToString();
    CopyText.SetActive(true);
  }

  public void RotateClockwiseSelection()
  {
    if (Data.CopyClipboard.Count == 0)
      return;
    SelectionRotationVal = SelectionRotationVal == 270 ? 0 : SelectionRotationVal + 90;
    for (int i = 1; i < Data.CopyClipboard.Count; i++)
    {
      Data.CopyClipboard[i] = RotatePointAroundPivot(Data.CopyClipboard[i], Data.CopyClipboard[0], new Vector3(0, 90, 0));
    }
    CopyText.GetComponent<Text>().text = SelectionRotationVal.ToString();
  }

  public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
  {
    Vector3 dir = point - pivot; // get point direction relative to pivot
    dir = Quaternion.Euler(angles) * dir; // rotate it
    point = dir + pivot; // calculate rotated point
    return new Vector3(Mathf.RoundToInt(point.x), point.y, Mathf.RoundToInt(point.z)); // return it
  }
  /// <summary>
  /// Mirrors selection always along Z axis
  /// </summary>
  public void InverseSelection()
  {
    if (Data.CopyClipboard.Count < 2)
      return;

    for (int i = 0; i < Data.CopyClipboard.Count; i++)
    {
      Data.CopyClipboard[i] = Vector3.Scale(Data.CopyClipboard[i], new Vector3(-1, 1, 1));
    }
    CopyText.GetComponent<Text>().text = "Inversed.";
    Invoke("RefreshCopyText", 1);
  }

  private void PasteSelectionOntoTerrain()
  {
    if (Data.CopyClipboard.Count == 0)
      return;

    UndoBuffer.AddOperation(Data.CopyClipboard);
    //Indexes of vertices for UpdateMapColliders()
    List<int> indexes = new List<int>();

    // List of tiles lying onto vertices that are now being pasted
    List<GameObject> to_update = new List<GameObject>();

    foreach (var mrk in Data.CopyClipboard)
    {
      if (IsWithinMapBounds(Highlight.pos + mrk))
      {
        // Update arrays of vertex heights
        indexes.Add(Loader.PosToIndex(Highlight.pos + mrk));
        Loader.current_heights[indexes[indexes.Count - 1]] = mrk.y;
        Loader.former_heights[indexes[indexes.Count - 1]] = mrk.y;

        Vector3 pom = Highlight.pos + mrk;

        // Mark pasted vertices
        GameObject zn = MarkAndReturnZnacznik(pom);
        if (zn != null)
          zn.transform.position = new Vector3(zn.transform.position.x, mrk.y, zn.transform.position.z);

        // Look for tiles lying here
        {
          pom.y = Data.maxHeight;
          RaycastHit[] tile_raycasts = Physics.SphereCastAll(pom, 0.1f, Vector3.down, rayHeight, 1 << 9);
          GameObject[] tiles = tile_raycasts.Where(tile => !to_update.Contains(tile.transform.gameObject)).Select(tile=> tile.transform.gameObject).ToArray();
          to_update.AddRange(tiles);
        }
      }
    }
    UpdateMapColliders(indexes);
    Building.UpdateTiles(to_update);
  }

  private void waiting4LD_state()
  {
    RaycastHit hit;
    if (waiting4LD && !waiting4LDpassed)
    {
      state_help_text.text = "Waiting for bottom-left vertex..";
      foreach (GameObject znacznik in znaczniki)
      {
        znacznik.GetComponent<BoxCollider>().enabled = true;
        znacznik.layer = 11;
      }

      if (Input.GetMouseButtonDown(0)) //Pierwszy klik
      {
        if (Physics.Raycast(new Vector3(Highlight.pos.x, 100, Highlight.pos.z), Vector3.down, out hit, Data.maxHeight + 1, 1 << 11) && hit.transform.gameObject.name == "on")
        {
          LD = Vector3Int.RoundToInt(hit.transform.position);
          LDH = hit.transform.position.y;
          waiting4LDpassed = true;
          waiting4LD = false;

        }
      }
      else if (Input.GetMouseButtonDown(1)) // Anulowanie zaznaczenia
      {
        waiting4LD = false;
        istilemanip = true;
        last_form_button = null;
        state_help_text.text = "Marking vertices..";

      }

    }
  }
  void mousewheelcheck()
  {
    if (Input.GetAxis("Mouse ScrollWheel") != 0 && is_entering_keypad_value)
    {
      Hide_text_helper();
    }
    if (Input.GetKey(KeyCode.LeftShift))
    {
      if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < Data.maxHeight)
      {
        slider.value += 10;
      }
      else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > Data.minHeight)
      {
        slider.value -= 10;
      }
    }
    else if (Input.GetKey(KeyCode.LeftAlt))
    {
      if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < Data.maxHeight)
      {
        slider.value += 0.25f;
      }
      else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > Data.minHeight)
      {
        slider.value -= 0.25f;
      }
    }
    else if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < Data.maxHeight)
    {
      slider.value += 1;
    }
    else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > Data.minHeight)
    {
      slider.value -= 1;
    }
    slider_realheight = SliderValue2RealHeight(slider.value);
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
  /// TO SLIDER button logic
  /// </summary>
  void FormMenu_toSlider()
  {
    surroundings = Building.Get_surrounding_tiles(null, znaczniki);
    float elevateby = 0;
    float slider_realheight = SliderValue2RealHeight(slider.value);
    if (KeepShape.isOn)
      elevateby = slider_realheight - LDH;
    if (istilemanip)
    {
      //Aktualizuj teren
      List<int> indexes = new List<int>();
      foreach (GameObject znacznik in znaczniki)
      {
        if (znacznik.name == "on")
        {
          Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
          int index = v.x + 4 * v.z * Data.TRACK.Width + v.z;
          UndoBuffer.AddZnacznik(Loader.IndexToPos(index));
          indexes.Add(index);
          if (KeepShape.isOn)
            Loader.current_heights[index] += elevateby;
          else
            Loader.current_heights[index] = slider_realheight;

          znacznik.transform.position = new Vector3(znacznik.transform.position.x, Loader.current_heights[index], znacznik.transform.position.z);
          Loader.former_heights[index] = Loader.current_heights[index];
        }
      }
      UndoBuffer.ApplyOperation();
      UpdateMapColliders(indexes);

      if (current != null)
        surroundings.Add(current);
      Building.UpdateTiles(surroundings);
      surroundings.Clear();
    }
    else
      Debug.LogError("istilemanip = false");

    KeepShape.isOn = false;
    last_form_button = "";
  }
  /// <summary>
  /// Searches for znacznik in given pos. If found znacznik isn't marked, f. marks it and returns it.
  /// </summary>
  public static GameObject MarkAndReturnZnacznik(Vector3 z_pos)
  {
    RaycastHit hit;
    z_pos.y = Data.maxHeight;
    if (Physics.Raycast(z_pos, Vector3.down, out hit, rayHeight, 1 << 11))
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
  /// <summary>
  /// Returns x++ if low < high; else returns x--
  /// </summary>
  float Go2High(int low, int high, ref int x)
  {
    return (low < high) ? x++ : x--;
  }
  /// <summary>
  /// helper function ensuring that:
  /// x goes from bottom left pos (considering rotation of selection; see: bottom-left vertex) to (upper)-right pos
  /// </summary>
  bool Ld_aims4_pg(int ld, int pg, int x)
  {
    return (ld < pg) ? x <= pg : x >= pg;
  }

  /// <summary>
  /// Returns List of vertices contains their global position (x or z, depending on bottom-left v-x) and height  
  /// var going shows where u need to go (in global coords) to get into next height-level line
  /// </summary>
  public List<DuVec3> GetOpposingVerticesForConnect(Vector3Int LD, Vector3Int PG)
  {
    List<DuVec3> Extremes = new List<DuVec3>();
    if ((LD.x < PG.x && LD.z > PG.z) || (LD.x > PG.x && LD.z < PG.z))
    { // equal heights along Z axis ||||
      //string going = (LD.x < PG.x && LD.z > PG.z) ? "right" : "left";

      for (int z = LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
      {
        Vector3 P1 = new Vector3(float.MaxValue, 0, 0);
        Vector3 P2 = new Vector3(float.MinValue, 0, 0);
        foreach (var mrk in znaczniki)
        {
          if (mrk.name == "on" && mrk.transform.position.z == z)
          {
            if (mrk.transform.position.x < P1.x)
              P1 = mrk.transform.position;
            if (mrk.transform.position.x > P2.x)
              P2 = mrk.transform.position;
          }
        }
        if (P1.x == LD.x)
          Extremes.Add(new DuVec3(new Vector3(P1.x, P1.y, P1.z), new Vector3(P2.x, P2.y, P2.z)));
        else
          Extremes.Add(new DuVec3(new Vector3(P2.x, P2.y, P2.z), new Vector3(P1.x, P1.y, P1.z)));
      }
    }
    else
    {
      //equal heights along X axis _---
      //string going = (LD.x < PG.x && LD.z < PG.z) ? "forward" : "back";
      for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
      {
        Vector3 P1 = new Vector3(0, 0, float.MaxValue);
        Vector3 P2 = new Vector3(0, 0, float.MinValue);
        foreach (var mrk in znaczniki)
        {
          if (mrk.name == "on" && mrk.transform.position.x == x)
          {
            if (mrk.transform.position.z < P1.z)
              P1 = mrk.transform.position;
            if (mrk.transform.position.z > P2.z)
              P2 = mrk.transform.position;
          }
        }
        if (P1.z == LD.z)
          Extremes.Add(new DuVec3(new Vector3(P1.x, P1.y, P1.z), new Vector3(P2.x, P2.y, P2.z)));
        else
          Extremes.Add(new DuVec3(new Vector3(P2.x, P2.y, P2.z), new Vector3(P1.x, P1.y, P1.z)));
      }
    }
    return Extremes;
  }
  /// <summary>
  /// Handles placing more complicated shapes.
  /// </summary>
  void ApplyFancyShape()
  {
    RaycastHit hit;

    //Flatter check
    if (last_form_button == "flatter")
    {
      if (current == null || !IsFlatter(current.name))
        return;
    }
    surroundings = Building.Get_surrounding_tiles(null, znaczniki);
    if (waiting4LDpassed)
    {
      //We have bottom-left, now we're searching for upper-right (all relative to 'rotation' of selection)
      Vector3Int PG = FindPG(LD);
      List<DuVec3> extremes = new List<DuVec3>();
      float heightdiff = slider_realheight - LDH;
      if (KeepShape.isOn)
        heightdiff -= FindHighestY(znaczniki) - LDH;
      if (Connect.isOn)
        extremes = GetOpposingVerticesForConnect(LD, PG);
      if ((LD.x < PG.x && LD.z >= PG.z) || (LD.x > PG.x && LD.z <= PG.z))
      { // equal heights along Z axis ||||
        float steps = Mathf.Abs(LD.x - PG.x);
        int step = 0;
        if (steps != 0 && (heightdiff != 0 || last_form_button == "flatter"))
        {
          for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
          {
            for (int z = LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
            {
              if (Connect.isOn)
              {
                heightdiff = extremes[Mathf.Abs(z - LD.z)].P2.y - extremes[Mathf.Abs(z - LD.z)].P1.y;
                steps = Mathf.Abs(extremes[Mathf.Abs(z - LD.z)].P1.x - extremes[Mathf.Abs(z - LD.z)].P2.x);
                slider_realheight = extremes[Mathf.Abs(z - LD.z)].P2.y;
                LDH = extremes[Mathf.Abs(z - LD.z)].P1.y;
              }
              bool traf = Physics.Raycast(new Vector3(x, Data.maxHeight + 1, z), Vector3.down, out hit, rayHeight, 1 << 11);
              index = x + 4 * z * Data.TRACK.Width + z;
              UndoBuffer.AddZnacznik(Loader.IndexToPos(index));
              Vector3 vertpos = Loader.IndexToPos(index);
              if (traf && hit.transform.gameObject.name == "on" && IsWithinMapBounds(vertpos))
              {
                float old_Y = vertpos.y; // tylko do keepshape
                if (last_form_button == "prostry")
                  vertpos.y = LDH + step / steps * heightdiff;
                else if (last_form_button == "integral")
                  vertpos.y = LDH + Smootherstep(LDH, slider_realheight, LDH + step / steps * heightdiff) * heightdiff;
                else if (last_form_button == "jumper")
                  vertpos.y = LDH + 2 * Smootherstep(LDH, slider_realheight, LDH + 0.5f * step / steps * heightdiff) * heightdiff;
                else if (last_form_button == "jumperend")
                  vertpos.y = LDH + 2 * (Smootherstep(LDH, slider_realheight, LDH + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
                else if (last_form_button == "flatter")
                  vertpos.y = LDH - TileManager.TileListInfo[current.name].FlatterPoints[step];
                if (KeepShape.isOn)
                  vertpos.y += old_Y - LDH;
                Loader.former_heights[index] = vertpos.y;
                Loader.current_heights[index] = Loader.former_heights[index];
                GameObject znacznik = hit.transform.gameObject;
                znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);

              }
              //if (Connect.isOn)
              //    step += 1;
            }
            //Debug.Log(x + " "+ LDH +" "+ slider_realheight + " "+ LDH + step / steps * heightdiff + "HEIGHT="+ verts[index].y);
            //if (!Connect.isOn)
            step += 1;
          }
        }
      }
      else
      { // equal heights along X axis _-_-
        float steps = Mathf.Abs(LD.z - PG.z);
        //Debug.Log("steps = " + steps);
        int step = 0;
        if (steps != 0 && (heightdiff != 0 || last_form_button == "flatter"))
        {
          for (int z = LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
          {
            for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
            {
              if (Connect.isOn)
              {
                heightdiff = extremes[Mathf.Abs(x - LD.x)].P2.y - extremes[Mathf.Abs(x - LD.x)].P1.y;
                steps = Mathf.Abs(extremes[Mathf.Abs(x - LD.x)].P1.z - extremes[Mathf.Abs(x - LD.x)].P2.z);
                slider_realheight = extremes[Mathf.Abs(x - LD.x)].P2.y;
                LDH = extremes[Mathf.Abs(x - LD.x)].P1.y;
              }
              //Debug.DrawLine(new Vector3(x, Terenowanie.Data.maxHeight+1, z), new Vector3(x, -5, z), Color.green, 60);
              bool traf = Physics.Raycast(new Vector3(x, Data.maxHeight + 1, z), Vector3.down, out hit, rayHeight, 1 << 11);
              index = x + 4 * z * Data.TRACK.Width + z;
              UndoBuffer.AddZnacznik(Loader.IndexToPos(index));
              Vector3 vertpos = Loader.IndexToPos(index);
              if (traf && hit.transform.gameObject.name == "on" && IsWithinMapBounds(vertpos))
              {
                //Debug.DrawRay(new Vector3(x, Terenowanie.Data.maxHeight+1, z), Vector3.down, Color.blue, 40);

                float old_Y = vertpos.y; // tylko do keepshape
                if (last_form_button == "prostry")
                  vertpos.y = LDH + step / steps * heightdiff;
                else if (last_form_button == "integral")
                  vertpos.y = LDH + Smootherstep(LDH, slider_realheight, LDH + step / steps * heightdiff) * heightdiff;
                else if (last_form_button == "jumper")
                  vertpos.y = LDH + 2 * Smootherstep(LDH, slider_realheight, LDH + 0.5f * step / steps * heightdiff) * heightdiff;
                else if (last_form_button == "jumperend")
                  vertpos.y = LDH + 2 * (Smootherstep(LDH, slider_realheight, LDH + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
                else if (last_form_button == "flatter")
                  vertpos.y = LDH - TileManager.TileListInfo[current.name].FlatterPoints[step];
                if (KeepShape.isOn)
                  vertpos.y += old_Y - LDH;
                Loader.former_heights[index] = vertpos.y;
                Loader.current_heights[index] = Loader.former_heights[index];
                GameObject znacznik = hit.transform.gameObject;
                znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);
              }
              //if (Connect.isOn)
              //    step += 1;
            }
            //if (!Connect.isOn)
            step += 1;
          }

        }
      }
      UpdateMapColliders(znaczniki);
      if (current != null)
        Building.UpdateTiles(new List<GameObject> { current });
      Building.UpdateTiles(surroundings);
      surroundings.Clear();
      UndoBuffer.ApplyOperation();
    }
  }

  private bool IsFlatter(string Name)
  {
    return TileManager.TileListInfo[Name].FlatterPoints.Length != 0 ? true : false;
  }

  public static bool IsWithinMapBounds(Vector3 v)
  {
    return (v.x > 0 && v.x < 4 * Data.TRACK.Width && v.z > 0 && v.z < 4 * Data.TRACK.Height) ? true : false;
  }
  public static bool IsWithinMapBounds(int x, int z)
  {
    return (x > 0 && x < 4 * Data.TRACK.Width && z > 0 && z < 4 * Data.TRACK.Height) ? true : false;
  }
  float Smootherstep(float edge0, float edge1, float x)
  {
    if (edge1 == edge0)
      return 0;
    // Scale to 0 - 1
    x = (x - edge0) / (edge1 - edge0);
    // 
    return (x * x * x * (x * (x * 6f - 15f) + 10f));
  }
  Vector3Int FindPG(Vector3 LD)
  {
    int lowX = int.MaxValue, hiX = int.MinValue, lowZ = int.MaxValue, hiZ = int.MinValue;
    foreach (GameObject znacznik in znaczniki)
    {
      if (znacznik.name == "on")
      {
        if (lowX > znacznik.transform.position.x)
          lowX = Mathf.RoundToInt(znacznik.transform.position.x);
        if (hiX < znacznik.transform.position.x)
          hiX = Mathf.RoundToInt(znacznik.transform.position.x);

        if (lowZ > znacznik.transform.position.z)
          lowZ = Mathf.RoundToInt(znacznik.transform.position.z);
        if (hiZ < znacznik.transform.position.z)
          hiZ = Mathf.RoundToInt(znacznik.transform.position.z);
      }

    }
    //Debug.Log("lowX"+lowX+ ", hiX " + hiX+ ", lowZ " + lowZ+ ", hiZ " + hiZ);
    if (lowX < LD.x)
    {
      if (lowZ < LD.z)
      {
        return new Vector3Int(lowX, 0, lowZ);
      }
      else
      {
        return new Vector3Int(lowX, 0, hiZ);
      }
    }
    else
    { // lowX = LD.x
      //Debug.Log("lowX = LD.x");
      if (lowZ < LD.z)
      {
        return new Vector3Int(hiX, 0, lowZ);
      }
      else
      {
        return new Vector3Int(hiX, 0, hiZ);
      }
    }
  }

  void MarkVerticesOfSelectedTile()
  {
    state_help_text.text = "Marking vertices..";
    //Zaznaczanie vertexów tylko w trybie manipulacji tilesa
    if (Input.GetMouseButtonDown(0))
    { // Rozpoczęcie zaznaczania..
      isSelecting = true;
      mousePosition1 = Input.mousePosition;
    }
    if (Input.GetMouseButtonUp(0))
    { // ..i zakończenie.
      foreach (GameObject znacznik in znaczniki)
      {
        if (IsWithinSelectionBounds(znacznik))
        {
          //Debug.Log (znacznik.transform.position);
          if (znacznik.GetComponent<MeshRenderer>().sharedMaterial == white)
          {
            znacznik.name = "on";
            znacznik.GetComponent<MeshRenderer>().sharedMaterial = red;
          }
          else
          {
            znacznik.name = "off";
            znacznik.GetComponent<MeshRenderer>().sharedMaterial = white;
          }
        }
      }
      isSelecting = false;
    }
    if (last_form_button != null && last_form_button != "" && isSelecting == false)
      waiting4LD = true;
  }
  void OnGUI()
  {
    if (isSelecting)
    {
      // Create a rect from both mouse positions
      Rect rect = Utils.GetScreenRect(mousePosition1, Input.mousePosition);
      Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
    }
  }
  public bool IsWithinSelectionBounds(GameObject gameObject)
  {
    if (!isSelecting)
      return false;
    Camera camera = Camera.main;
    Bounds viewportBounds = Utils.GetViewportBounds(camera, mousePosition1, Input.mousePosition);
    return viewportBounds.Contains(camera.WorldToViewportPoint(gameObject.transform.position));
  }

  void HandleVertexBoxes(bool checkTerrain)
  {
    if (!Highlight.over)
      return;
    FormMenu.gameObject.SetActive(true);
    if (current != null && !checkTerrain)
    {
      Mesh rmc = current.GetComponent<MeshFilter>().mesh;
      if (znaczniki.Count != 0)
      {
        if (znaczniki[0].name == current.name + "_mrk")
        {
          //Żądanie identycznego ustawienia wskaźników => nic nie rób
          return;
        }
        else
        {
          Del_znaczniki();
        }
      }
      for (int i = 0; i < rmc.vertexCount; i++)
      {
        GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
        znacznik.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        znacznik.transform.position = current.transform.TransformPoint(rmc.vertices[i]);
        znacznik.GetComponent<MeshRenderer>().material = white;
        znacznik.GetComponent<BoxCollider>().enabled = true;
        znacznik.layer = 11;
        if (i == 0)
          znacznik.name = current.name + "_mrk";
        znaczniki.Add(znacznik);
      }
    }
    else // Mamy mapę. Liczba vertexów jest ograniczona dla wydajności =  max_verts_visible_dim
    {
      Vector3Int v = Highlight.pos;
      if (znaczniki.Count != 0)
      {
        if (znaczniki[0].name == "first")//Żądanie identycznego ustawienia wskaźników => nic nie rób
          return;
        else
          Del_znaczniki();
      }

      for (int z = v.z - max_verts_visible_dim.z / 2; z <= v.z + max_verts_visible_dim.z / 2; z++)
      {
        for (int x = v.x - max_verts_visible_dim.x / 2; x <= v.x + max_verts_visible_dim.x / 2; x++)
        {
          if (IsWithinMapBounds(x, z))//Nie zmieniamy vertexów na obrzeżach i poza mapą
          {
            GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
            znacznik.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            int index = x + 4 * z * Data.TRACK.Width + z;
            znacznik.transform.position = Loader.IndexToPos(index);
            znacznik.GetComponent<MeshRenderer>().material = white;
            znacznik.GetComponent<BoxCollider>().enabled = true;
            znacznik.layer = 11;
            if (znaczniki.Count == 0)
              znacznik.name = "first";
            znaczniki.Add(znacznik);
          }
        }
      }
    }

    istilemanip = true;
  }
  public static void Del_znaczniki()
  {
    if (znaczniki.Count != 0)
    {
      for (int i = 0; i < znaczniki.Count; i++)
      {
        Destroy(znaczniki[i]);
      }
      znaczniki.Clear();
      GameObject.Find("e_formPANEL").GetComponent<Terraining>().FormMenu.gameObject.SetActive(false);
    }

  }
  /// <summary>
  /// handles simple and advanced terrain forming
  /// </summary>
  /// <returns></returns>
  int Control()
  {
    RaycastHit hit;
    if (istilemanip && !waiting4LDpassed)
      return 0;
    if (firstFormingMode)
    {
      current = null;
      return 1;
    }
    else if (Physics.Raycast(new Vector3(Highlight.pos.x, Data.maxHeight + 1, Highlight.pos.z), Vector3.down, out hit, rayHeight, 1 << 9))
    {
      if (hit.transform.gameObject.layer == 9)
      {
        current = hit.transform.gameObject;
        return 2;
      }
    }
    else
    {
      current = null;
      return 2;
    }
    return 0;
  }
  /// <summary>
  /// Handles quick rectangular selection in first form mode with RMB
  /// </summary>
  void Make_elevation()
  {
    if (index == 0)
    {
      // Get initial position and set znacznik there
      if (IsWithinMapBounds(Highlight.pos))
      {
        index = Highlight.pos.x + 4 * Data.TRACK.Width * Highlight.pos.z + Highlight.pos.z;
        //Debug.Log("I1="+index+" "+m.vertices[index]+" pos="+highlight.pos);
        indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.transform.localScale = new Vector3(.25f, 1, .25f);
        indicator.transform.position = Highlight.pos;
      }
    }
    else
    {
      // Time to get second position
      if (IsWithinMapBounds(Highlight.pos))
      {
        int index2 = Highlight.pos.x + 4 * Data.TRACK.Width * Highlight.pos.z + Highlight.pos.z;
        //Debug.Log("I2="+index2+" "+m.vertices[index]+" pos="+highlight.pos);
        Vector3Int a = Vector3Int.RoundToInt(Loader.IndexToPos(index));
        Vector3Int b = Vector3Int.RoundToInt(Loader.IndexToPos(index2));
        {
          List<int> indexes = new List<int>();
          for (int z = Mathf.Min(a.z, b.z); z <= Mathf.Max(a.z, b.z); z++)
          {
            for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
            {
              int idx = x + 4 * z * Data.TRACK.Width + z;
              UndoBuffer.AddZnacznik(Loader.IndexToPos(idx));
              Loader.former_heights[idx] = SliderValue2RealHeight(slider.value);
              Loader.current_heights[idx] = Loader.former_heights[idx];
              indexes.Add(idx);
            }
          }
          UpdateMapColliders(indexes);
        }
        Destroy(indicator);
        index = 0;
        RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Data.maxHeight + 1, 0.5f * (a.z + b.z)), new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))), Vector3.down, Quaternion.identity, rayHeight, 1 << 9); //Szukaj jakiegokolwiek tilesa w zaznaczeniu
        List<GameObject> to_update = new List<GameObject>();
        foreach (RaycastHit hit in hits)
        {
          to_update.Add(hit.transform.gameObject);
        }
        Building.UpdateTiles(to_update);
        UndoBuffer.ApplyOperation();
      }
    }
  }
  void Ctrl_key_works()
  {

    if (Input.GetKey(KeyCode.LeftControl) && Highlight.over && !Input.GetKey(KeyCode.LeftAlt))
    {
      if (is_entering_keypad_value)
        Hide_text_helper();

      Vector3Int v = Highlight.pos;
      int index = v.x + 4 * v.z * Data.TRACK.Width + v.z;
      slider.value = RealHeight2SliderValue(Loader.current_heights[index]);
    }
  }
  /// <summary>
  /// Handles quick sculpting mode
  /// </summary>
  void Single_vertex_manipulation(bool SaveUndo = false)
  {
    if (Highlight.over && IsWithinMapBounds(Highlight.pos))
    {
      Vector3Int v = Highlight.pos;
      int index = v.x + 4 * v.z * Data.TRACK.Width + v.z;
      UndoBuffer.AddZnacznik(Loader.IndexToPos(index));

      //Debug.DrawLine(new Vector3(v.x, Terenowanie.Data.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.red, 5);
      RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Data.maxHeight + 1, v.z), 0.5f, Vector3.down, rayHeight, 1 << 9);
      List<GameObject> to_update = new List<GameObject>();
      to_update.AddRange(hits.Select(h => h.transform.gameObject).ToArray());

      if (to_update.Count > 0)
      {
        if (AreListedObjectsHaveRMCVertexHere(to_update, index))
        {
          Loader.current_heights[index] = SliderValue2RealHeight(slider.value);
          //Helper.current_heights[index] = Helper.former_heights[index];
          UpdateMapColliders(new List<int> { index });
          Building.UpdateTiles(to_update);
        }
      }
      else
      {
        Loader.former_heights[index] = SliderValue2RealHeight(slider.value);
        Loader.current_heights[index] = Loader.former_heights[index];
        UpdateMapColliders(new List<int> { index });
      }
    }
  }

  bool AreListedObjectsHaveRMCVertexHere(List<GameObject> to_update, int index)
  {
    foreach (GameObject rmc in to_update)
    {
      bool found_matching = false;
      foreach (Vector3 v in rmc.GetComponent<MeshCollider>().sharedMesh.vertices)
      {
        Vector3Int V = Vector3Int.RoundToInt(rmc.transform.TransformPoint(v));
        if (V.x + 4 * V.z * Data.TRACK.Width + V.z == index)
        {
          found_matching = true;
          break;
        }
      }
      if (!found_matching)
        return false;
    }
    return true;
  }
  /// <summary>
  /// Distance on 2D map between 3D points
  /// </summary>
  float Distance(Vector3 v1, Vector3 v2)
  {
    return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z));
  }
}

