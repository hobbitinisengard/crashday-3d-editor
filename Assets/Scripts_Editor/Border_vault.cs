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
		//check left and right border (H1)

	 if (left_Hx && up_Vx)
		{
			return QuarterType.Both_restricted;
		}
		if (left_Hx && !up_Vx)
		{
			return QuarterType.Hx_restricted;
		}
		if (!left_Hx && up_Vx)
		{
			return QuarterType.Vx_restricted;
		}

		return QuarterType.Unrestricted;
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
		Vector3 TL = Build.GetTLPos(rmc);
		Vector3 dims = Build.GetRealTileDims(rmc);

		// for x
		for (int i = 1; i <= dims.x; i++)
		{
			if (only_restricted && !rmcName.Contains("V" + i.ToString()))
				continue;
			Vector3 initialPos;
			if (i == 1)
				initialPos = TL + 2 * Vector3.right;
			else // i = 2
				initialPos = TL + 6 * Vector3.right;

			for (int z = 0; z <= dims.z; z++)
			{
				Vector3 pos = initialPos + z * 4 * Vector3.back;
				pos = Consts.RotatePointAroundPivot(pos, rmc.transform.position, rmc.transform.rotation.eulerAngles);
				borders.Add(Consts.PosToIndex(pos));
			}
		}
		// for z
		for (int i = 1; i <= dims.z; i++)
		{
			if (only_restricted && !rmcName.Contains("H" + i.ToString()))
				continue;
			Vector3 initialPos;
			if (i == 1)
				initialPos = TL + 2 * Vector3.back;
			else
				initialPos = TL + 6 * Vector3.back;

			for (int x = 0; x <= dims.x; x++)
			{
				Vector3 pos = initialPos + x * 4 * Vector3.right;
				pos = Consts.RotatePointAroundPivot(pos, rmc.transform.position, rmc.transform.rotation.eulerAngles);
				borders.Add(Consts.PosToIndex(pos));
			}
		}
		return borders;
	}

	internal List<Vector3> Get_sensitive_vertices(GameObject tile)
	{
		var quarters = Quarter.Generate_Quarters(tile);
		List<Vector3> sensitive_vertices = new List<Vector3>();

		foreach(var q in quarters)
		{
			sensitive_vertices.AddRange(Quarter.Generate_sensitive_pattern(q));
		}
		return sensitive_vertices;
	}
	List<Vector3> Get_border_points(int key)
	{
		Vector3 pos = Consts.IndexToPos(key);
		List<Vector3> vertices = new List<Vector3>();
		BorderType border_type = Vault[Consts.PosToIndex(pos)].border_type;
		bool unrestricted = Vault[Consts.PosToIndex(pos)].tiles_constraining == 0;
		if (border_type == BorderType.Horizontal)
		{
			for (int i = -2; i <= 2;)
			{
				vertices.Add(pos + i * Vector3.right);
				if (unrestricted)
					i++;
				else
					i += 4;
			}
		}
		else
		{
			for (int i = -2; i <= 2;)
			{
				vertices.Add(pos + i * Vector3.forward);
				if (unrestricted)
					i++;
				else
					i += 4;
			}
		}
		return vertices;
	}
}
