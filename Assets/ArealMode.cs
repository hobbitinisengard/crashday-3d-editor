using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// First submode of manual mode: Areal mode. Second one is Single mode
/// You switch between them with [Tab]
/// </summary>
public class ArealMode : MonoBehaviour
{
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
    if (!Input.GetKey(KeyCode.LeftControl)) //if ctrl key wasn't pressed (height pickup)
    {
      if (Input.GetMouseButtonUp(0))
        UndoBuffer.ApplyOperation();

      if (Input.GetKeyDown(KeyCode.Escape)) //ESC deletes white indicator in Make_Elevation()
      {
        index = 0;
        if (indicator != null)
          Destroy(indicator);
      }
      if (!MouseInputUIBlocker.BlockedByUI)
      {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
          Areal_vertex_manipulation(); // single-action
        else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
          Areal_vertex_manipulation(); //auto-fire
        else if (Input.GetMouseButtonDown(1) && Highlight.over)
          Make_areal_elevation();

      }
    }
  }

  void Areal_vertex_manipulation()
  {
    if (Service.IsWithinMapBounds(Highlight.pos))
    {
      // Highlight.pos is center vertex

      List<int> indexes = new List<int>();
      float MaxRadius = RadiusSlider.Radius * 1.41f;
      for (float z = Highlight.pos.z - RadiusSlider.Radius; z <= Highlight.pos.z + RadiusSlider.Radius; z++)
      {
        for (float x = Highlight.pos.x - RadiusSlider.Radius; x <= Highlight.pos.x + RadiusSlider.Radius; x++)
        {
          if (Service.IsWithinMapBounds(x, z))
          {
            Vector3 currentpos = new Vector3(x, 0, z);
            int idx = Service.PosToIndex(currentpos);
            float dist = Service.Distance(currentpos, Highlight.pos);
            if (dist > MaxRadius)
              continue;

            UndoBuffer.AddZnacznik(currentpos);
            float Hdiff = Service.SliderValue2RealHeight(HeightSlider.value) - Service.current_heights[idx];

            float fullpossibleheight = Hdiff * StepSlider.GetPercent();
            Service.former_heights[idx] += fullpossibleheight * Service.Smoothstep(0, 1, (MaxRadius - dist) / MaxRadius);
            Service.current_heights[idx] = Service.former_heights[idx];
            indexes.Add(idx);
          }
        }
      }
      Service.UpdateMapColliders(indexes);
      //Search for any tiles 
      RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Service.maxHeight, Highlight.pos.z),
                                             new Vector3(RadiusSlider.Radius + 1, 1, RadiusSlider.Radius),
                                              Vector3.down, Quaternion.identity, Service.rayHeight, 1 << 9);
      List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
      Build.UpdateTiles(hitsList);
      UndoBuffer.ApplyOperation();
    }
  }
  void Make_areal_elevation()
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
        Vector3Int a = Vector3Int.RoundToInt(Service.IndexToPos(index));
        Vector3Int b = Vector3Int.RoundToInt(Service.IndexToPos(index2));
        Vector3Int LD = new Vector3Int(Mathf.Min(a.x, b.x), 0, Mathf.Min(a.z, b.z));
        Vector3Int PG = new Vector3Int(Mathf.Max(a.x, b.x), 0, Mathf.Max(a.z, b.z));
        {
          List<int> indexes = new List<int>();
          int x_edge = PG.x - LD.x;
          int z_edge = PG.z - LD.z;
          for (int z = LD.z - RadiusSlider.Radius; z <= PG.z + RadiusSlider.Radius; z++)
          {
            for (int x = LD.x - RadiusSlider.Radius; x <= PG.x + RadiusSlider.Radius; x++)
            {
              if (Service.IsWithinMapBounds(x, z))
              {
                Vector3 currentpos = new Vector3(x, 0, z);
                int idx = x + 4 * z * Service.TRACK.Width + z;

                if (x >= LD.x && x <= PG.x && z >= LD.z && z <= PG.z)
                {
                  Service.former_heights[idx] = Service.SliderValue2RealHeight(HeightSlider.value);
                }
                else
                {
                  Vector3 Closest = GetClosestPointOfEdgeOfSelection(currentpos, LD, PG);
                  float dist = Service.Distance(currentpos, Closest);
                  if (dist > RadiusSlider.Radius)
                    continue;
                  float Hdiff = Service.SliderValue2RealHeight(HeightSlider.value) - Service.current_heights[idx];
                  Service.former_heights[idx] += Hdiff * Service.Smoothstep(0, 1, (RadiusSlider.Radius - dist) / RadiusSlider.Radius);
                }
                UndoBuffer.AddZnacznik(currentpos);
                Service.current_heights[idx] = Service.former_heights[idx];
                indexes.Add(idx);
              }
            }
          }
          Service.UpdateMapColliders(indexes);
        }
        Destroy(indicator);
        index = 0;
        RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Service.maxHeight, 0.5f * (a.z + b.z)),
          new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))),
          Vector3.down, Quaternion.identity, Service.rayHeight, 1 << 9); //Search for tiles
        Build.UpdateTiles(hits.Select(hit => hit.transform.gameObject).ToList());
        UndoBuffer.ApplyOperation();
      }
    }
  }
  private Vector3 GetClosestPointOfEdgeOfSelection(Vector3 v, Vector3 LD, Vector3 PG)
  {
    Vector3 result = new Vector3();
    if (v.x < LD.x)
      result.x = LD.x;
    else if (v.x > PG.x)
      result.x = PG.x;
    else
      result.x = v.x;

    if (v.z < LD.z)
      result.z = LD.z;
    else if (v.z > PG.z)
      result.z = PG.z;
    else
      result.z = v.z;
    return result;
  }
}
