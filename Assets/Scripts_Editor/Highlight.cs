﻿
using System.IO;
using UnityEngine;
//Handles constant position marking
public class Highlight : MonoBehaviour
{
	/// <summary>
	/// Vertex dimension of Top left of grass
	/// </summary>
	public static Vector3Int TL = new Vector3Int();
	/// <summary>
	/// Is mouse pointer currently over map?
	/// </summary>
	public static bool over = false;
	/// <summary>
	/// Position over map Rounded to Int
	/// </summary>
	public static Vector3 pos;
	/// <summary>
	/// Position over map not rounded to int
	/// </summary>
	public static Vector3 pos_float;
	RaycastHit hit;

	void Update()
	{

		Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
		Get_valid_init_vector(r);
		if (pos.x == -1)
		{
			over = false;
		}
		else
		{
			TL.x = 4 * Mathf.FloorToInt(pos.x / 4); // Get TopLeft of current 1x1 tile
			TL.z = 4 * Mathf.FloorToInt(pos.z / 4) + 4;
			over = true;
		}
	}

	//Returns position of map's vertex that is closest to pointer
	void Get_valid_init_vector(Ray r)
	{
		bool traf = Physics.Raycast(r.origin, r.direction, out hit, Consts.RAY_H, 1 << 8);
		if (traf && hit.transform.gameObject.layer != 5)
		{ // Raycast nie przejdzie przez elementy UI
			pos_float = hit.point;
			pos = Vector3Int.RoundToInt(hit.point);
			pos.y = Consts.current_heights[Consts.PosToIndex((int)pos.x, (int)pos.z)];
			pos_float.y = pos.y;
		}
		else
			pos =  new Vector3Int(-1, -1, -1);
		
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