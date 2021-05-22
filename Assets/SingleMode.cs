using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingleMode : MonoBehaviour
{
  private enum SingleModes { Immediate, Incremental }
  private SingleModes CurrentMode;
  Color32 Color_selected = new Color32(219, 203, 178, 255);
  public GameObject ArealMenu;
  public Slider HeightSlider;
  public Slider IntensitySlider;
  public Slider DistortionSlider;
  public Slider RadiusSlider;
  public Button SingleModeButton;
  public Button SmoothModeButton;

  private GameObject indicator;
  private int index;
  private float TargetDistValue = 0;
  private bool DistortionFirstValueSelected;
  private Vector3 InitialPos;

  public void OnDisable()
  {
    index = 0;
    if (indicator != null)
      Destroy(indicator);
  }
  // buttons use this function
  public void SwitchMode(float mode)
  {
    CurrentMode = (SingleModes)mode;
    SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
    SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
    if (mode == 0)
      SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
    else
      SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Alpha1))
      SwitchMode(0);
    else if (Input.GetKeyDown(KeyCode.Alpha2))
      SwitchMode(1);
    if (CurrentMode == SingleModes.Immediate)
    {
      IntensitySlider.enabled = false;
      DistortionSlider.enabled = false;
      if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
      {
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt))
          UndoBuffer.ApplyOperation();

        if (Input.GetKeyDown(KeyCode.Escape)) //ESC toggles off Make_Elevation()
        {
          index = 0;
          if (indicator != null)
            Destroy(indicator);
        }
        if (!MouseInputUIBlocker.BlockedByUI)
        {
          if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
            Single_vertex_manipulation(); // single-action
          else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
            Single_vertex_manipulation(); //auto-fire
          else if (Input.GetMouseButtonDown(1) && Highlight.over)
            Make_elevation();
        }
      }
    }
    else if (CurrentMode == SingleModes.Incremental)
    {

      IntensitySlider.enabled = true;
      DistortionSlider.enabled = true;
      if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
      {
        if (Input.GetMouseButtonUp(1))
          DistortionFirstValueSelected = false;
        if ((Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0)) && !Input.GetKey(KeyCode.LeftAlt))
          UndoBuffer.ApplyOperation();
        
          

        if (!MouseInputUIBlocker.BlockedByUI)
        {
          if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
            Single_smoothing(); // single-action
          else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
            Single_smoothing(); //auto-fire
          else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
            Single_distortion(); // single-action
          else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
            Single_distortion(); //auto-fire
        }
      }
    }
  }
  void Single_distortion()
  {
    
    if (Service.IsWithinMapBounds(Highlight.pos))
    {
      float dist_val = Service.SliderValue2RealHeight(DistortionSlider.value);
      float h_val = Service.SliderValue2RealHeight(HeightSlider.value);

      if (Highlight.pos.x != InitialPos.x || Highlight.pos.z != InitialPos.z)
      { // vertex -> vertex
        InitialPos = Highlight.pos;
        TargetDistValue = Random.Range(h_val - dist_val, h_val + dist_val);
      }
      int idx = Service.PosToIndex(Highlight.pos);
      UndoBuffer.AddZnacznik(Highlight.pos);
      Service.current_heights[idx] += (TargetDistValue - Service.current_heights[idx]) * IntensitySlider.value / 100f;
      Service.former_heights[idx] = Service.current_heights[idx];
      Service.UpdateMapColliders(new List<int> { idx });
      var tiles = Build.Get_surrounding_tiles(new List<int> { idx });
      Build.UpdateTiles(tiles);
    }
  }
  void Single_smoothing()
  {
    if (Service.IsWithinMapBounds(Highlight.pos))
    {
      Vector3 pos = Highlight.pos;
      pos.y = Service.maxHeight;
      
      float height_sum = 0;
      for (int x = -1; x <= 1; x++)
      {
        for (int z = -1; z <= 1; z++)
        {
          if (x == 0 && x == z)
            continue;
          pos.x = Highlight.pos.x + x;
          pos.z = Highlight.pos.z + z;
          if(Physics.Raycast(pos, Vector3.down, out RaycastHit hit, Service.rayHeight, 1 << 8))
            height_sum += hit.point.y;
        }
      }
      float avg = height_sum / 8f;
      int idx = Service.PosToIndex(Highlight.pos);
      UndoBuffer.AddZnacznik(Highlight.pos);
      Service.former_heights[idx] += (avg - Service.former_heights[idx]) * IntensitySlider.value / 100f;
      Service.current_heights[idx] = Service.former_heights[idx];
      Service.UpdateMapColliders(new List<int> { idx });
      var tiles = Build.Get_surrounding_tiles(new List<int> { idx });
      Build.UpdateTiles(tiles);
    }
  }
  /// <summary>
  /// Handles quick rectangular selection in first form mode with RMB
  /// </summary>
  void Make_elevation()
  {
    if (index == 0)
    {
      // Get initial position and set znacznik there
      if (Service.IsWithinMapBounds(Highlight.pos))
      {
        index = (int)(Highlight.pos.x + 4 * Service.TRACK.Width * Highlight.pos.z + Highlight.pos.z);
        //Debug.Log("I1="+index+" "+m.vertices[index]+" pos="+highlight.pos);
        indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.transform.localScale = new Vector3(.25f, 1, .25f);
        indicator.transform.position = Highlight.pos;
      }
    }
    else
    {
      // Time to get second position
      if (Service.IsWithinMapBounds(Highlight.pos))
      {
        int index2 = (int)(Highlight.pos.x + 4 * Service.TRACK.Width * Highlight.pos.z + Highlight.pos.z);
        //Debug.Log("I2="+index2+" "+m.vertices[index]+" pos="+highlight.pos);
        Vector3Int a = Vector3Int.RoundToInt(Service.IndexToPos(index));
        Vector3Int b = Vector3Int.RoundToInt(Service.IndexToPos(index2));
        {
          List<int> indexes = new List<int>();
          for (int z = Mathf.Min(a.z, b.z); z <= Mathf.Max(a.z, b.z); z++)
          {
            for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
            {
              int idx = x + 4 * z * Service.TRACK.Width + z;
              UndoBuffer.AddZnacznik(Service.IndexToPos(idx));
              Service.former_heights[idx] = Service.SliderValue2RealHeight(HeightSlider.value);
              Service.current_heights[idx] = Service.former_heights[idx];
              indexes.Add(idx);
            }
          }
          Service.UpdateMapColliders(indexes);
        }
        Destroy(indicator);
        index = 0;
        RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Service.maxHeight, 0.5f * (a.z + b.z)), new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))), Vector3.down, Quaternion.identity, Service.rayHeight, 1 << 9); //Szukaj jakiegokolwiek tilesa w zaznaczeniu
        List<GameObject> to_update = new List<GameObject>();
        foreach (RaycastHit hit in hits)
          to_update.Add(hit.transform.gameObject);

        Build.UpdateTiles(to_update);
        UndoBuffer.ApplyOperation();
      }
    }
  }
  /// <summary>
  /// Handles quick sculpting mode
  /// </summary>
  void Single_vertex_manipulation()
  {
    if (Highlight.over && Service.IsWithinMapBounds(Highlight.pos))
    {
      Vector3 v = Highlight.pos;
      int index = Service.PosToIndex(v);
      UndoBuffer.AddZnacznik(Service.IndexToPos(index));
      RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Service.maxHeight, v.z), 0.5f, Vector3.down, Service.rayHeight, 1 << 9);
      List<GameObject> to_update = new List<GameObject>();
      foreach (RaycastHit hit in hits)
        to_update.Add(hit.transform.gameObject);

      if (to_update.Count > 0)
      {
        if (AreListedObjectsHavingRMCVertexHere(to_update, index))
        {
          Service.current_heights[index] = Service.SliderValue2RealHeight(HeightSlider.value);
          //Helper.current_heights[index] = Helper.former_heights[index];
          Service.UpdateMapColliders(new List<int> { index });
          Build.UpdateTiles(to_update);
        }
      }
      else
      {
        Service.former_heights[index] = Service.SliderValue2RealHeight(HeightSlider.value);
        Service.current_heights[index] = Service.former_heights[index];
        Service.UpdateMapColliders(new List<int> { index });
      }
    }
  }
  bool AreListedObjectsHavingRMCVertexHere(List<GameObject> to_update, int index)
  {
    foreach (GameObject rmc in to_update)
    {
      bool found_matching = false;
      foreach (Vector3 v in rmc.GetComponent<MeshCollider>().sharedMesh.vertices)
      {
        Vector3Int V = Vector3Int.RoundToInt(rmc.transform.TransformPoint(v));
        if (Service.PosToIndex(V) == index)
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
}
