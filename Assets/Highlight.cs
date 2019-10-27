using System.IO;
using UnityEngine;
//Handles constant position marking
public class Highlight : MonoBehaviour
{
  /// <summary>
  /// Vertexowy wymiar Lewego Dolnego Rogu elementu
  /// </summary>
  public static Vector3Int t = new Vector3Int();
  /// <summary>
  /// Is mouse pointer currently over map?
  /// </summary>
  public static bool over = false;
  public static Vector3Int pos; // konkretynych vertexów (podniesiony o 0.01f)
  RaycastHit hit;
  float last_z = 99;

  void Update()
  {

    Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
    pos = get_valid_init_vector(r);
    if (pos.x == -1)
    {
      over = false;
    }
    else
    {
      t.x = 4 * Mathf.FloorToInt(pos.x / 4f); //Zwraca lewy dolny róg bieżącej trawki
      t.z = 4 * Mathf.FloorToInt(pos.z / 4f);
      over = true;

    }

  }

  //Returns position of map's vertex that is closest to pointer
  Vector3Int get_valid_init_vector(Ray r)
  {
    bool traf = Physics.Raycast(r.origin, r.direction, out hit, Data.maxHeight - Data.minHeight, 1 << 8);
    if (traf && hit.transform.gameObject.layer != 5)
    { // Raycast nie przejdzie przez elementy UI
      return Vector3Int.RoundToInt(hit.point);
    }
    else
    {
      return new Vector3Int(-1, -1, -1);
    }
  }


  void DebugRayCast(Vector3 pos, Ray r)
  {
    if (Input.GetKeyDown(KeyCode.Delete))
    {
      if (Physics.Raycast(pos, Vector3.down, out hit, Terraining.rayHeight, 1 << 12))
      {
        StreamWriter writer = new StreamWriter("Assets/Resources/flatters.txt", true);
        writer.Write(hit.transform.name.Substring(0, hit.transform.name.IndexOf('(')) + " ");
        Debug.Log("name =" + hit.transform.name.Substring(0, hit.transform.name.IndexOf('(')));
        writer.Close();
      }
    }
    if (Input.GetKeyDown(KeyCode.Insert))
    {
      RaycastHit hit1;
      if (Physics.Raycast(r.origin, r.direction, out hit1, Terraining.rayHeight, 1 << 12) && Physics.Raycast(new Vector3(Mathf.Round(pos.x), hit1.point.y + 0.5f, Mathf.Round(pos.z)), Vector3.down, out hit, Terraining.rayHeight, 1 << 12) && Mathf.Round(hit.point.z) != Mathf.Round(last_z))
      {
        StreamWriter writer = new StreamWriter("Assets/Resources/flatters.txt", true);
        writer.Write(hit.point.y + " ");
        last_z = Mathf.Round(hit.point.z);
        writer.Close();
        Debug.Log("DEBUG CAST Y=" + hit.point + "lastz=" + last_z);

      }

    }
    if (Input.GetKeyDown(KeyCode.Return))
    {
      if (Physics.Raycast(r.origin, r.direction, out hit, Terraining.rayHeight, 1 << 12))
        Debug.Log("free Y=" + hit.point.y);
    }
  }

}