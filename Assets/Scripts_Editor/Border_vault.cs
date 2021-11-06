using System;
using System.Collections.Generic;
using UnityEngine;

public class Border_vault
{
	//		 [Y,X]
	public static Border[,] horizontal_border_array;
	public static Border[,] vertical_border_array;

	/// <summary>
	/// Height = track tiles number corresponding to its height
	/// Width = track tiles number corresponding to its width
	/// </summary>
	/// <param name="Height"></param>
	/// <param name="Width"></param>
	public void InitializeBorderInfo(int Height, int Width)
	{
		horizontal_border_array = new Border[Height + 1, Width];
		vertical_border_array = new Border[Height, Width + 1];
	}
	/// <param name="vert_pos">Has to lie on a border</param>
	public bool Is_restricted(Vector3Int vert_pos)
	{
		bool Vertical_check = vert_pos.x % 4 == 0;
		bool Horizontal_check = vert_pos.z % 4 == 0;
		if (!(Vertical_check ^ Horizontal_check))
			throw new Exception(vert_pos.ToString() + " doesn't lie on a border");

		if (Vertical_check)
		{
			return vertical_border_array[vert_pos.x / 4, vert_pos.z / 4].tiles_occupying > 0;
		}
		else
		{
			return horizontal_border_array[vert_pos.z / 4, vert_pos.x / 4].tiles_occupying > 0;
		}
	}
	public void Add_Borders_of(GameObject rmc)
	{
		var Borders = Get_borders(rmc);
		foreach(var b in Borders)
			b.tiles_occupying++;
	}

	public void Remove_borders_of(GameObject rmc)
	{
		var Borders = Get_borders(rmc);
		foreach (var b in Borders)
			b.tiles_occupying--;
	}
	List<Border> Get_borders(GameObject rmc)
	{
		List<Border> to_return = new List<Border>();
		String rmcName = rmc.GetComponent<BorderInfo>().info;

		return to_return;
	}
}
