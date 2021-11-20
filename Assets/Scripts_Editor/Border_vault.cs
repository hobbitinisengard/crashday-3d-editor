using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Border_vault
{
	//	terrain index corresponding to the middle of the border is the key
	private Dictionary<int, Border> Vault = new Dictionary<int, Border>();
	public void InitializeBorderInfo(int Height, int Width)
	{
		Vault.Clear();
		for (int z = 0; z <= 4 * Height; z += 4)
			for (int x = 2; x <= 4 * Width; x += 4)
				Vault.Add(Consts.PosToIndex(x, z), new Border(BorderType.Horizontal));

		for (int z = 2; z <= 4 * Height; z += 4)
			for (int x = 0; x <= 4 * Width; x += 4)
				Vault.Add(Consts.PosToIndex(x, z), new Border(BorderType.Vertical));

	}
	public void Add_borders_of(GameObject rmc)
	{
		var Keys = Get_border_keys(rmc, true);
		foreach (var Key in Keys)
		{
			try
			{
				Vault[Key].tiles_constraining++;
			}
			catch
			{

			}
		}
	}
	public QuarterType Get_quarter(Vector3Int quarter_center)
	{
		// borders; true if constrained else false
		bool left_Hx = Vault[Consts.PosToIndex(quarter_center + 2 * Vector3.left)].tiles_constraining > 0;
		bool up_Vx = Vault[Consts.PosToIndex(quarter_center + 2 * Vector3.forward)].tiles_constraining > 0;
		bool right_Hx = Vault[Consts.PosToIndex(quarter_center + 2 * Vector3.right)].tiles_constraining > 0;
		bool down_Vx = Vault[Consts.PosToIndex(quarter_center + 2 * Vector3.back)].tiles_constraining > 0;

		return new QuarterType(up_Vx, down_Vx, left_Hx, right_Hx);
	}
	public void Remove_borders_of(GameObject rmc)
	{
		var Keys = Get_border_keys(rmc, true);
		foreach (var Key in Keys)
			Vault[Key].tiles_constraining--;
	}
	/// <summary>
	/// Returns ALL or ONLY RESTRICTED borders of tile
	/// </summary>
	List<int> Get_border_keys(GameObject rmc, bool only_restricted = false)
	{
		List<int> borders = new List<int>();
		string rmcName = rmc.GetComponent<BorderInfo>().info;
		Vector3 dims = new Vector3Int(TileManager.TileListInfo[rmc.name].Size.x, 0, TileManager.TileListInfo[rmc.name].Size.y);
		Vector3 TL = new Vector3(-2 * dims.x, 0, 2 * dims.z);
		

		// move in local space for x
		for (int x = 1; x <= dims.x; x++)
		{
			if (only_restricted && !rmcName.Contains("V" + x.ToString()))
				continue;
			Vector3 initialPos;
			if (x == 1)
				initialPos = TL + 2 * Vector3.right;
			else // i = 2
				initialPos = TL + 6 * Vector3.right;

			for (int z = 0; z <= dims.z; z++)
			{
				Vector3 pos = initialPos + z * 4 * Vector3.back;
				pos = rmc.transform.TransformPoint(pos);
				borders.Add(Consts.PosToIndex(pos));
			}
		}
		// move in local space for z
		for (int z = 1; z <= dims.z; z++)
		{
			if (only_restricted && !rmcName.Contains("H" + z.ToString()))
				continue;
			Vector3 initialPos;
			if (z == 1)
				initialPos = TL + 2 * Vector3.back;
			else
				initialPos = TL + 6 * Vector3.back;

			for (int x = 0; x <= dims.x; x++)
			{
				Vector3 pos = initialPos + x * 4 * Vector3.right;
				pos = rmc.transform.TransformPoint(pos);
				borders.Add(Consts.PosToIndex(pos));
			}
		}
		return borders;
	}

	internal List<Vector3> Get_sensitive_vertices(GameObject rmc)
	{
		List<Vector3> sensitive_vertices = new List<Vector3>();

		// suppose MeshVerts returns nxm unconstrained mesh
		Vector3[] verts = Build.GetMeshVerts(rmc);

		Quarter[] tile_quarters = Quarter.Generate_Quarters(rmc);

		for (int index = 0; index < verts.Length; index++)
		{
			Vector3 v = rmc.transform.TransformPoint(verts[index]);
			v.x = Mathf.Round(v.x);
			v.y = Consts.current_heights[Consts.PosToIndex(v)];
			v.z = Mathf.Round(v.z);

			if (!Consts.IsWithinMapBounds(v))
				continue;
			if (v.x % 4 == 0 && v.z % 4 == 0)
			{
				sensitive_vertices.Add(v);
				continue;
			}

			// find a quarter that given vertex belongs to and get information about restriction pattern
			Quarter quarter = tile_quarters.Aggregate(
				(minItem, nextItem) => Consts.Distance(minItem.pos, v) < Consts.Distance(nextItem.pos, v) ? minItem : nextItem);
			

			if (quarter.qt.Unrestricted())
			{
				sensitive_vertices.Add(v);
			}
			else if (quarter.qt.Fully_restricted())
			{
				if(Consts.Lies_on_both_borders(v))
					sensitive_vertices.Add(v);
			}
			else if (quarter.qt.Horizontal_restricted())
			{
				if (quarter.original_grid.Contains(Consts.PosToIndex(v)) && !Consts.Lies_on_restricted_border(v, BorderType.Horizontal, quarter))
					sensitive_vertices.Add(v);
			}
			else if (quarter.qt.Vertical_restricted())
			{
				if (quarter.original_grid.Contains(Consts.PosToIndex(v)) && !Consts.Lies_on_restricted_border(v, BorderType.Vertical, quarter))
					sensitive_vertices.Add(v);
			}
		}
		return sensitive_vertices;
	}
}
