using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingleMode : MonoBehaviour
{
  public GameObject ArealMenu;
  public Slider HeightSlider;
  private GameObject indicator;
  private int index;

  public void OnDisable()
  {
    index = 0;
    if (indicator != null)
      Destroy(indicator);
  }
  void Update()
  {
    RunManualFormingMode();
  }
  void RunManualFormingMode()
  {
    if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
    {
      if (Input.GetMouseButtonUp(0))
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
        RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Service.maxHeight + 1, 0.5f * (a.z + b.z)), new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))), Vector3.down, Quaternion.identity, Service.rayHeight, 1 << 9); //Szukaj jakiegokolwiek tilesa w zaznaczeniu
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
  void Single_vertex_manipulation(bool SaveUndo = false)
  {
    if (Highlight.over && Service.IsWithinMapBounds(Highlight.pos))
    {
      Vector3 v = Highlight.pos;
      int index = Service.PosToIndex(v);
      UndoBuffer.AddZnacznik(Service.IndexToPos(index));
      RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Service.maxHeight + 1, v.z), 0.5f, Vector3.down, Service.rayHeight, 1 << 9);
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
