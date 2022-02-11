
using System.IO;
using UnityEngine;
//Handles constant position marking
public class Highlight : MonoBehaviour
{
	/// <summary>
	/// Position of the tile (in cells) the cursor is pointing at
	/// </summary>
	public static Vector3Int tile_pos = new Vector3Int();
	/// <summary>
	/// Is mouse pointer currently over map?
	/// </summary>
	public static bool over = false;
	/// <summary>
	/// Is mouse pointer currently over grass?
	/// </summary>
	public static bool over_grass = false;
	/// <summary>
	/// Position over map Rounded to Int
	/// </summary>
	public static Vector3 pos;
	/// <summary>
	/// Position over map not rounded to int
	/// </summary>
	public static Vector3 pos_float;
	/// <summary>
	/// RMC currently being under the cursor
	/// </summary>
	public static GameObject tile;

	void Update()
	{
		Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
		GetPos(r);
		GetTLgrass(r);
		over = pos.x != -1;
		over_grass = tile_pos.x != -1;
	}

	//Returns position of tile's or map's vertex that is closest to pointer
	void GetPos(Ray r)
	{
		RaycastHit hit;
		if (Physics.Raycast(r.origin, r.direction, out hit, Consts.RAY_H, 1 << 9)
		    || Physics.Raycast(r.origin, r.direction, out hit, Consts.RAY_H, 1 << 8))
		{
			tile = hit.transform.gameObject;
			pos_float = hit.point;
			pos = Vector3Int.RoundToInt(hit.point);
			pos.y = Consts.current_heights[Consts.PosToIndex((int)pos.x, (int)pos.z)];
			pos_float.y = pos.y;
		}
		else
		{
			pos = new Vector3Int(-1, -1, -1);
			tile = null;
		}
	}

	void GetTLgrass(Ray r)
    {
		if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, Consts.RAY_H, 1 << 8))
		{
			tile_pos.x = Mathf.FloorToInt(hit.point.x / 4);
			tile_pos.z = Mathf.FloorToInt(hit.point.z / 4);
		}
		else
			tile_pos = new Vector3Int(-1, -1, -1);
	}

	//void DebugRayCast(Vector3 pos, Ray r)
	//{
	//  if (Input.GetKeyDown(KeyCode.Delete))
	//  {
	//    if (Physics.Raycast(pos, Vector3.down, out hit, Consts.rayHeight, 1 << 12))
	//    {
	//      StreamWriter writer = new StreamWriter("Assets/Resources/flatters.txt", true);
	//      writer.Write(hit.transform.name.Substring(0, hit.transform.name.IndexOf('(')) + " ");
	//      Debug.Log("name =" + hit.transform.name.Substring(0, hit.transform.name.IndexOf('(')));
	//      writer.Close();
	//    }
	//  }
	//  if (Input.GetKeyDown(KeyCode.Insert))
	//  {
	//    RaycastHit hit1;
	//    if (Physics.Raycast(r.origin, r.direction, out hit1, Consts.rayHeight, 1 << 12) && Physics.Raycast(new Vector3(Mathf.Round(pos.x), hit1.point.y + 0.5f, Mathf.Round(pos.z)), Vector3.down, out hit, Consts.rayHeight, 1 << 12) && Mathf.Round(hit.point.z) != Mathf.Round(last_z))
	//    {
	//      StreamWriter writer = new StreamWriter("Assets/Resources/flatters.txt", true);
	//      writer.Write(hit.point.y + " ");
	//      last_z = Mathf.Round(hit.point.z);
	//      writer.Close();
	//      Debug.Log("DEBUG CAST Y=" + hit.point + "lastz=" + last_z);

	//    }

	//  }
	//  if (Input.GetKeyDown(KeyCode.Return))
	//  {
	//    if (Physics.Raycast(r.origin, r.direction, out hit, Consts.rayHeight, 1 << 14))
	//      Debug.Log("free Y=" + hit.point.y);
	//    else
	//      Debug.Log("-");
	//  }
	//}

}