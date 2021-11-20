using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class QuarterType
{
	public bool Vx_up_restricted = false;
	public bool Vx_down_restricted = false;
	public bool Hx_left_restricted = false;
	public bool Hx_right_restricted = false;
	public QuarterType(bool Up, bool Down, bool Left, bool Right)
	{
		this.Vx_up_restricted = Up;
		this.Vx_down_restricted = Down;
		this.Hx_left_restricted = Left;
		this.Hx_right_restricted = Right;
	}
	public bool Unrestricted()
	{
		return !Vx_up_restricted && !Vx_down_restricted && !Hx_left_restricted && !Hx_right_restricted;
	}
	public bool Fully_restricted()
	{
		return (Vx_up_restricted || Vx_down_restricted) && (Hx_left_restricted || Hx_right_restricted);
	}
	public bool Horizontal_restricted()
	{
		return Vx_down_restricted || Vx_up_restricted;
	}
	public bool Vertical_restricted()
	{
		return Hx_left_restricted || Hx_right_restricted;
	}
}
public class Quarter
{
	/// <summary>
	/// Type of restriction corresponding to current state of borders
	/// </summary>
	public QuarterType qt;
	public Vector3Int pos = new Vector3Int();
	/// <summary>
	/// Contains terrain indices corresponding to default restrictions of tile's quarter
	/// </summary>
	public HashSet<int> original_grid;

	public Quarter(Vector3 move_to_center, (char, char) VH, GameObject rmc)
	{
		if (rmc.layer == 8)
			Debug.LogError("rmc can't be a grass");

		this.pos = Vector3Int.RoundToInt(rmc.transform.TransformPoint(move_to_center));
		this.original_grid = Generate_grid(move_to_center, VH, rmc);
		this.qt = Build.Border_Vault.Get_quarter(Vector3Int.RoundToInt(pos));
	}

	internal static Quarter[] Generate_Quarters(GameObject rmc)
	{
		// get unrotated dims
		Vector3Int dims = new Vector3Int(TileManager.TileListInfo[rmc.name].Size.x, 0, TileManager.TileListInfo[rmc.name].Size.y);
		Quarter[] tile_quarters = new Quarter[dims.x * dims.z];

		// create template and apply it to a tile
		if (dims.x == 1)
		{ // 1x1
			if (dims.z == 1)
			{
				tile_quarters[0] = new Quarter(Vector3.zero, ('1', '1'), rmc);
			}
			else
			{//1x2
				tile_quarters[0] = new Quarter(2 * Vector3.forward, ('1', '1'), rmc);
				tile_quarters[1] = new Quarter(2 * Vector3.back, ('1', '2'), rmc);
			}
		}
		else
		{//2x1
			if (dims.z == 1)
			{
				tile_quarters[0] = new Quarter(2 * Vector3.left, ('1', '1'), rmc);
				tile_quarters[1] = new Quarter(2 * Vector3.right, ('2', '1'), rmc);
			}
			else
			{//2x2
				tile_quarters[0] = new Quarter(2 * Vector3.left + 2 * Vector3.forward, ('1', '1'), rmc);
				tile_quarters[1] = new Quarter(2 * Vector3.right + 2 * Vector3.forward, ('2', '1'), rmc);
				tile_quarters[2] = new Quarter(2 * Vector3.right + 2 * Vector3.back, ('2', '2'), rmc);
				tile_quarters[3] = new Quarter(2 * Vector3.left + 2 * Vector3.back, ('1', '2'), rmc);
			}
		}
		return tile_quarters;
	}
	/// <summary>
	/// generates a collection of vertices set in global space, according to original restriction pattern of given tile
	/// </summary>
	private HashSet<int> Generate_grid(Vector3 move_to_quarter_center, (char, char) VH, GameObject rmc)
	{
		List<Vector3> vertices = new List<Vector3>();
		bool Vx_restricted = false;
		bool Hx_restricted = false;

		string restr_str = rmc.GetComponent<BorderInfo>().info;
		if (restr_str.Contains("V" + VH.Item1))
			Vx_restricted = true;
		if (restr_str.Contains("H" + VH.Item2))
			Hx_restricted = true;

		for (int z = -2; z <= 2;)
		{
			for (int x = -2; x <= 2;)
			{
				Vector3 move = new Vector3(x, 0, z);
				vertices.Add(move);

				if (Vx_restricted)
					x += 4;
				else
					x++;
			}
			if (Hx_restricted)
				z += 4;
			else
				z++;
		}
		HashSet<int> indices = new HashSet<int>();
		for (int i = 0; i < vertices.Count; i++)
		{
			Vector3 v = rmc.transform.TransformPoint(vertices[i] + move_to_quarter_center);
			indices.Add(Consts.PosToIndex(v));
		}
		return indices;
	}
}
