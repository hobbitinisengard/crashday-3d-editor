using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BufferDirection { BACKWARD = 0, FORWARD = 1};
public enum BufferOperationType { PLACE = 0, REMOVE = 1 };

public static class UndoBuffer
{
	private static readonly byte SIZE = 30;
	/// <summary>
	/// Global coordinates of former vertices. Layers<Vertices<Index, Height before and after oper-n>>
	/// </summary>
	private static List<Dictionary<int, float[]>> TerrainBuffer = new List<Dictionary<int, float[]>>();
	/// <summary>
	/// Layers<(tile position, tile properties, place/remove operation)>
	/// </summary>
	private static List<(Vector3Int, TilePlacement, BufferOperationType)> TileBuffer
		= new List<(Vector3Int, TilePlacement, BufferOperationType)>();
	private static int current_layer_terrain = -1;
	private static int current_layer_tiles;
	/// <summary>
	/// Sets true after every ApplyTerrainOperation call.
	/// </summary>
	private static bool next_operation = true;

	public static void Reset()
	{
		TerrainBuffer.Clear();
		TileBuffer.Clear();
		current_layer_terrain = -1;
		current_layer_tiles = -1;
	}

	public static void ApplyTileOperation(Vector3Int pos, TilePlacement props, BufferOperationType type)
    {
		for (int i = TileBuffer.Count - 1; i > current_layer_tiles; i--)
			TileBuffer.RemoveAt(i);

		if (TileBuffer.Count < SIZE)
			current_layer_tiles++;
		else
			TileBuffer.RemoveAt(0);

		TileBuffer.Add((pos, props, type));
	}

	/// <summary>
	/// Called after every operation in form mode
	/// </summary>
	public static void ApplyTerrainOperation()
	{
		next_operation = true;
	}

	/// <summary>
	/// Adds new pair of markings to the last layer of the buffer. Moreover if ApplyOperation was run before, f. will create a new layer.
	/// </summary>
	public static void AddVertexPair(Vector3 mrk1, Vector3 mrk2)
	{
		if (next_operation)
		{
			// This will remove the latest layers if player makes an operation after pressing CTRL + Z
			for (int i = TerrainBuffer.Count - 1; i > current_layer_terrain; i--)
				TerrainBuffer.RemoveAt(i);

			if (TerrainBuffer.Count < SIZE)
				current_layer_terrain++;
			else
				TerrainBuffer.RemoveAt(0);

			TerrainBuffer.Add(new Dictionary<int, float[]>());
			next_operation = false;
		}

		int index = Consts.PosToIndex(mrk1);
		float old_height = mrk1.y;
		float new_height = mrk2.y;
		// We only need to overwrite the new heights (if some vertices are changed several times in a single oper-n, usually in manual sub-mode)
		if (TerrainBuffer.Last().ContainsKey(index))
			TerrainBuffer.Last()[index][1] = new_height;
		else
			TerrainBuffer.Last().Add(index, new float[] { old_height, new_height });
	}

	/// <summary>
	/// When Ctrl + Z or Ctrl + Y is clicked
	/// </summary>
	public static void MoveThroughTerrainLayers(BufferDirection buffer_direction)
	{
		int direction = (int)buffer_direction;

		if (current_layer_terrain == (direction == 1 ? TerrainBuffer.Count - 1 : -1))
			return;

		//Indexes of vertices for UpdateMapColliders()
		HashSet<int> indexes = new HashSet<int>();
		// List of tiles lying onto vertices that are now being pasted
		List<GameObject> tiles_to_update = new List<GameObject>();
		foreach (var item in TerrainBuffer[current_layer_terrain + direction])
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

		current_layer_terrain += direction * 2 - 1;
	}

	public static void MoveThroughTileLayers(BufferDirection buffer_direction)
    {
		int direction = (int)buffer_direction;

		if (current_layer_tiles == (direction == 1 ? TileBuffer.Count - 1 : -1))
			return;

		Vector3Int pos = TileBuffer[current_layer_tiles + direction].Item1;
		TilePlacement info = TileBuffer[current_layer_tiles + direction].Item2;
		int op_type = (int)TileBuffer[current_layer_tiles + direction].Item3;

		// Tile placement - remove to undo / place to redo
		// Tile removal - place to undo / remove to redo
		if (direction == 0 && op_type == 0 || direction == 1 && op_type == 1)
		{
			Build.DeleteTile(pos);
			Build.UpdateOrDeleteActiveTile();
		}
		else
		{
			Build.PlaceTile(pos, info, true);
			Build.SaveTileProperties(pos, info);
			Build.UpdateOrDeleteActiveTile(true);
		}

		current_layer_tiles += direction * 2 - 1;
	}
}
