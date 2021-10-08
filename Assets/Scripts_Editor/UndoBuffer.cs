﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum BufferDirection { BACKWARD = 0, FORWARD = 1};
public static class UndoBuffer
{
	private static readonly byte SIZE = 30;
	/// <summary>
	/// Global coordinates of former vertices. Layers<Vertices<Index, Height before and after oper-n>>
	/// </summary>
	private static List<Dictionary<int, float[]>> Buffer = new List<Dictionary<int, float[]>>();
	
	private static int current_layer = -1;
	private static bool next_operation = true;

	public static void Reset()
	{
		Buffer.Clear();
		current_layer = -1;
	}
	/// <summary>
	/// Called after every operation in form mode
	/// </summary>
	public static void ApplyOperation()
	{
		next_operation = true;
	}
	/// <summary>
	/// Adds new pair of markings to the last layer of the buffer. Moreover if ApplyOperation was run before, f. will create a new layer.
	/// </summary>
	public static void Add(Vector3 mrk1, Vector3 mrk2)
	{
		if (next_operation)
		{
			// This will remove the latest layers if player makes an operation after pressing CTRL + Z
			for (int i = Buffer.Count - 1; i > current_layer; i--)
				Buffer.RemoveAt(i);

			if (Buffer.Count < SIZE)
				current_layer++;
			else
				Buffer.RemoveAt(0);

			Buffer.Add(new Dictionary<int, float[]>());
			next_operation = false;
		}

		int index = Consts.PosToIndex(mrk1);
		float old_height = mrk1.y;
		float new_height = mrk2.y;
		// We only need to overwrite the new heights (if some vertices are changed several times in a single oper-n, usually in manual sub-mode)
		if (Buffer.Last().ContainsKey(index))
			Buffer.Last()[index][1] = new_height;
		else
			Buffer.Last().Add(index, new float[] { old_height, new_height });
	}
	/// <summary>
	/// When Ctrl + Z or Ctrl + Y is clicked
	/// </summary>
	public static void MoveThroughLayers(BufferDirection buffer_direction)
	{
		int direction = (int)buffer_direction;

		if (current_layer == (direction == 1 ? Buffer.Count - 1 : -1))
			return;

		//Indexes of vertices for UpdateMapColliders()
		HashSet<int> indexes = new HashSet<int>();
		// List of tiles lying onto vertices that are now being pasted
		List<GameObject> tiles_to_update = new List<GameObject>();
		foreach (var item in Buffer[current_layer + direction])
		{
			int index = item.Key;
			float height = item.Value[direction];

			// Update arrays of vertex heights
			indexes.Add(index);
			Consts.current_heights[index] = height;
			Consts.former_heights[index] = height;

			// Mark pasted vertices
			Vector3 pom = Consts.IndexToPos(index);
			GameObject mrk = Consts.MarkAndReturnZnacznik(pom);
			if (mrk != null)
				mrk.transform.position = new Vector3(mrk.transform.position.x, height, mrk.transform.position.z);

			// Look for tiles lying here
			pom.y = Consts.MAX_H;
			var hits = Physics.SphereCastAll(pom, .1f, Vector3.down, Consts.MAX_H - Consts.MIN_H, 1 << 9);
			foreach (var hit in hits)
				if (!tiles_to_update.Contains(hit.transform.gameObject))
					tiles_to_update.Add(hit.transform.gameObject);
		}
		Consts.UpdateMapColliders(indexes);
		Build.UpdateTiles(tiles_to_update);

		current_layer += direction * 2 - 1;
	}
}