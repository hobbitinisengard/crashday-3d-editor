using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Quarter
{
	/// <summary>
	/// Type of restriction corresponding to current state of borders
	/// </summary>
	public QuarterType qt = QuarterType.Unrestricted;
	public Vector3Int pos = new Vector3Int();
	/// <summary>
	/// Contains terrain indices corresponding to default restrictions of tile's quarter
	/// </summary>
	public HashSet<int> original_grid;

	public Quarter(Vector3 move_to_center, (char, char) VH, GameObject rmc)
	{
		this.pos = Vector3Int.RoundToInt(rmc.transform.position + move_to_center);
		this.qt = Build.Border_Vault.Get_quarter(Vector3Int.RoundToInt(pos));
		this.original_grid = Generate_grid(move_to_center, VH,rmc);
	}

	internal static Quarter[] Generate_Quarters(GameObject rmc)
	{
		Vector3Int dims = Build.GetRealTileDims(rmc);
		Quarter[] tile_quarters = new Quarter[dims.x * dims.z];
		
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
				tile_quarters[2] = new Quarter(2 * Vector3.left + 2 * Vector3.back, ('1', '2'), rmc);
				tile_quarters[3] = new Quarter(2 * Vector3.right + 2 * Vector3.back, ('2', '2'), rmc);
			}
		}
		return tile_quarters;
	}
	/// <summary>
	/// generates a collection of vertices set in global space, according to original restriction pattern of given tile
	/// </summary>
	private static HashSet<int> Generate_grid(Vector3 move_to_center, (char, char) VH, GameObject rmc)
	{
		string restr_str = rmc.GetComponent<BorderInfo>().info;
		List<Vector3> vertices = new List<Vector3>();
		
		bool Vx_restricted = false;
		bool Hx_restricted = false;

		if (restr_str.Contains("V" + VH.Item1))
			Vx_restricted = true;
		if (restr_str.Contains("H" + VH.Item2))
			Hx_restricted = true;

		for (int z = -2; z <= 2;)
		{
			for (int x = -2; x <= 2;)
			{
				Vector3 move = new Vector3(move_to_center.x + x, 0, move_to_center.z + z);
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
		if (rmc.transform.rotation.eulerAngles.y != 0)
		{
			for(int i=0; i<vertices.Count; i++)
			{
				vertices[i] = rmc.transform.position + Consts.RotatePointAroundPivot(vertices[i], Vector3.zero, rmc.transform.rotation.eulerAngles);
				indices.Add(Consts.PosToIndex(vertices[i]));
			}
		}
		return indices;
	}
	/// <summary>
	/// Generates pattern from quarter (border vault)
	/// </summary>
	/// <param name="q"></param>
	/// <returns></returns>
	internal static List<Vector3> Generate_sensitive_pattern(Quarter q)
	{
		HashSet<Vector3> sensitive = new HashSet<Vector3>();
		for(int z = -2; z<= 2;)
		{
			for(int x = -2; x<= 2;)
			{
				sensitive.Add(new Vector3(q.pos.x + x,
					Consts.current_heights[Consts.PosToIndex(q.pos.x + x, q.pos.z + z)],
					q.pos.z + z));
				if (q.qt == QuarterType.Unrestricted || q.qt == QuarterType.Hx_restricted)
					x++;
				else
					x += 4;
				
			}
			if (q.qt == QuarterType.Unrestricted || q.qt == QuarterType.Vx_restricted)
				z++;
			else
				z += 4;
		}
		return sensitive.Distinct().ToList();
	}
}
