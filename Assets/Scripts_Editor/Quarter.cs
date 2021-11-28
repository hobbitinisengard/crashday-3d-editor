using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class Extensions
{
	public static HashSet<T> ToHashSet<T>(
					this IEnumerable<T> source,
					IEqualityComparer<T> comparer = null)
	{
		return new HashSet<T>(source, comparer);
	}
}
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
	/// <summary>
	/// logical AND
	/// </summary>
	public static QuarterType operator &(QuarterType a, QuarterType b)
	{
		QuarterType q = new QuarterType(false, false, false, false);
		q.Hx_left_restricted = a.Hx_left_restricted && b.Hx_left_restricted;
		q.Hx_right_restricted = a.Hx_right_restricted && b.Hx_right_restricted;
		q.Vx_down_restricted = a.Vx_down_restricted && b.Vx_down_restricted;
		q.Vx_up_restricted = a.Vx_up_restricted && b.Vx_up_restricted;
		return q;
	}

	/// <summary>
	/// All borders unrestricted
	/// </summary>
	/// <returns></returns>
	public bool Unrestricted()
	{
		return !Vx_up_restricted && !Vx_down_restricted && !Hx_left_restricted && !Hx_right_restricted;
	}
	/// <summary>
	/// Either of vertical borders and either of horizontal borders restricted
	/// </summary>
	/// <returns></returns>
	public bool Both_restricted()
	{
		return (Vx_up_restricted || Vx_down_restricted) && (Hx_left_restricted || Hx_right_restricted);
	}
	/// <summary>
	/// All of the borders restricted
	/// </summary>
	/// <returns></returns>
	public bool All_restricted()
	{
		return Vx_up_restricted && Vx_down_restricted && Hx_left_restricted & Hx_right_restricted;
	}
	/// <summary>
	/// One of the horizontal borders restricted
	/// </summary>
	/// <returns></returns>
	public bool Horizontal_restricted()
	{
		return Vx_down_restricted || Vx_up_restricted;
	}
	public bool All_horizontal_restricted()
	{
		return Vx_down_restricted && Vx_up_restricted;
	}
	/// <summary>
	/// One of the vertical borders restricted
	/// </summary>
	/// <returns></returns>
	public bool Vertical_restricted()
	{
		return Hx_left_restricted || Hx_right_restricted;
	}
	public bool All_vertical_restricted()
	{
		return Hx_left_restricted && Hx_right_restricted;
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
		this.qt = Build.Border_Vault.Get_quarterType(Vector3Int.RoundToInt(pos));
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
	internal static Quarter[] Generate_All_Quarters(GameObject feed_tile)
	{
		// get unrotated dims
		Vector3 tile_pos = feed_tile.transform.position;
		tile_pos.y = Consts.RAY_H;
		RaycastHit[] raycastHits = Physics.SphereCastAll(tile_pos, .4f, Vector3.down, Consts.RAY_H, 1 << 9);
		List<Quarter> container = new List<Quarter>();
		foreach (var raycasthit in raycastHits)
		{
			GameObject tile = raycasthit.transform.gameObject;
			Vector3Int dims = new Vector3Int(TileManager.TileListInfo[tile.name].Size.x, 0, TileManager.TileListInfo[tile.name].Size.y);
			Quarter[] tile_quarters = new Quarter[dims.x * dims.z];

			// create template and apply it to a tile
			if (dims.x == 1)
			{ // 1x1
				if (dims.z == 1)
				{
					tile_quarters[0] = new Quarter(Vector3.zero, ('1', '1'), tile);
				}
				else
				{//1x2
					tile_quarters[0] = new Quarter(2 * Vector3.forward, ('1', '1'), tile);
					tile_quarters[1] = new Quarter(2 * Vector3.back, ('1', '2'), tile);
				}
			}
			else
			{//2x1
				if (dims.z == 1)
				{
					tile_quarters[0] = new Quarter(2 * Vector3.left, ('1', '1'), tile);
					tile_quarters[1] = new Quarter(2 * Vector3.right, ('2', '1'), tile);
				}
				else
				{//2x2
					tile_quarters[0] = new Quarter(2 * Vector3.left + 2 * Vector3.forward, ('1', '1'), tile);
					tile_quarters[1] = new Quarter(2 * Vector3.right + 2 * Vector3.forward, ('2', '1'), tile);
					tile_quarters[2] = new Quarter(2 * Vector3.right + 2 * Vector3.back, ('2', '2'), tile);
					tile_quarters[3] = new Quarter(2 * Vector3.left + 2 * Vector3.back, ('1', '2'), tile);
				}
			}
			container.AddRange(tile_quarters);
		}

		for(int i=0; i<container.Count-1; i++)
		{
			for (int j = i+1; j < container.Count; j++)
			{
				if (container[i].pos.x == -1)
					continue;
				if (container[i].pos == container[j].pos)
				{ // perform q AND w; then remove w
					container[i].qt &= container[j].qt;
					container[i].original_grid = container[i].original_grid.Join(container[j].original_grid, v1 => v1, v2 => v2, (v1, v2) => v1).ToHashSet();
					container[j].pos.x = -1;
				}
			}
		}
		container.RemoveAll(q => q.pos.x == -1);
		return container.ToArray();
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
