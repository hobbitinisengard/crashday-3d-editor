using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Border_vault
{
	//	terrain index corresponding to the middle of the border is the key
	private static Dictionary<int, Border> Vault = new Dictionary<int, Border>();
	public void InitializeBorderInfo(int Height, int Width)
	{
		for (int z = 0; z <= 4*Height; z+=4)
			for (int x = 2; x <= 4*Width; x+=4)
				Vault.Add(Consts.PosToIndex(x, z), new Border(BorderType.Horizontal));

		for (int z = 2; z <= 4 * Height; z += 4)
			for (int x = 0; x <= 4 * Width; x += 4)
				Vault.Add(Consts.PosToIndex(x, z), new Border(BorderType.Vertical));

	}
	/// <param name="vert_pos">Has to lie on a border</param>
	public bool Is_restricted(Vector3Int vert_pos)
	{
		Border border = Get_Border(vert_pos);
		return border.tiles_occupying > 0;
	}
	Border Get_Border(Vector3Int vert_pos)
	{
		// calculate type of border
		bool On_vertical_border = vert_pos.x % 4 == 0;
		bool On_horizontal_border = vert_pos.z % 4 == 0;

		// normalize pos so it is centered on border
		if (On_vertical_border)
			vert_pos.z = 2 + 4*(vert_pos.z / 4);
		else // On_horizontal_border
			vert_pos.x = 2 + 4*(vert_pos.x / 4);
		try
		{
			return Vault[Consts.PosToIndex(vert_pos)];
		}
		catch
		{
			return null;
		}
	}
	public void Add_Borders_of(GameObject rmc)
	{
		var indices = Get_Vault_Keys(rmc);
		foreach(var b in indices)
			Vault[b].tiles_occupying++;
	}

	public void Remove_borders_of(GameObject rmc)
	{
		var Borders = Get_Vault_Keys(rmc);
		foreach (var b in Borders)
			Vault[b].tiles_occupying--;
	}
	List<int> Get_Vault_Keys(GameObject rmc)
	{
		List<int> borders = new List<int>();
		String rmcName = rmc.GetComponent<BorderInfo>().info;
		Vector3 TL = Build.GetTLPos(rmc);
		Vector3 dims = Build.GetRealTileDims(rmc);
		var restr = rmcName.Substring(3).SplitBy(2).ToArray();
		// r is for instance V1
		foreach(var r in restr)
		{
			Vector3 initialPos;
			if (r[0] == 'V')
			{
				if (r == "V1")
				{
					initialPos = TL + 2 * Vector3.right;
				}
				else
				{
					initialPos = TL + 6 * Vector3.right;
				}

				for (int i = 0; i < dims.z; i++)
					borders.Add(Consts.PosToIndex(initialPos + i * 4 * Vector3.back));
			}
			else if(r[0] == 'H')
			{
				if (r == "H1")
				{
					initialPos = TL + 2 * Vector3.back;
				}
				else
				{
					initialPos = TL + 6 * Vector3.back;
				}

				for (int i = 0; i < dims.x; i++)
					borders.Add(Consts.PosToIndex(initialPos + i * 4 * Vector3.right));
			}
			else
			{
				throw new Exception(rmcName + " is not a valid rmcname");
			}
		}
		return borders;
	}

	internal List<Vector3> Get_sensitive_vertices(GameObject tile)
	{
		var Border_Keys = Get_Vault_Keys(tile);
		List<Vector3> all_vertex_pts = new List<Vector3>();

		foreach(int Key in Border_Keys)
		{
			List<Vector3> border_vertex_points = Get_border_points(Key);
			foreach (var index in border_vertex_points)
				all_vertex_pts.Add(index);
		}
		return all_vertex_pts;
	}
	List<Vector3> Get_border_points(int key)
	{
		Vector3 pos = Consts.IndexToPos(key);
		List<Vector3> indices = new List<Vector3>();
		BorderType border_type = Vault[Consts.PosToIndex(pos)].border_type;
		bool unrestricted = Vault[Consts.PosToIndex(pos)].tiles_occupying == 0;
		if (border_type == BorderType.Horizontal)
		{
			for (int i = -2; i <= 2;)
			{
				indices.Add(pos + i * Vector3.right);
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
				indices.Add(pos + i * Vector3.forward);
				if (unrestricted)
					i++;
				else
					i += 4;
			}
		}
		return indices;
	}
}
