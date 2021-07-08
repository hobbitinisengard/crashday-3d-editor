using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UndoBuffer
{
	/// <summary>
	/// Global coordinates of former vertices before last terrain operation
	/// </summary>
	private static List<Vector3> UndoZnaczniki = new List<Vector3>();
	/// <summary>
	/// is this vertex already in UndoZnaczniki?
	/// </summary>
	private static Dictionary<int, bool> UndoCreated = new Dictionary<int, bool>();
	/// <summary>
	/// Clear buffer before next call of AddZnacznik
	/// </summary>
	private static bool Clear_buffer_before_next_add = false;

	private static void AddNewPoint(Vector3 point, bool ItIsAlreadyPresent = false)
	{
		UndoZnaczniki.Add(point);
		if (!ItIsAlreadyPresent)
			UndoCreated.Add(Service.PosToIndex(point), true);
	}

	private static bool IsAlreadyPresent(Vector3 point)
	{
		return UndoCreated.ContainsKey(Service.PosToIndex(point));
	}

	private static void ClearBuffer()
	{
		UndoZnaczniki.Clear();
		UndoCreated.Clear();
	}

	public static void AddZnacznik(int x, int z, bool EnableOverwriting = false)
	{
		Vector3 mrk = new Vector3(x, Service.current_heights[Service.PosToIndex(x, z)], z);
		AddZnacznik(mrk, EnableOverwriting);
	}

	/// <summary>
	/// Adds new znacznik to buffer. Moreover if ApplyOperation was run before, f. will clear buffer once before addition.
	/// </summary>
	/// <param name="mrk"></param>
	public static void AddZnacznik(Vector3 mrk, bool EnableOverwriting = false)
	{
		if (Clear_buffer_before_next_add)
		{
			ClearBuffer();
			Clear_buffer_before_next_add = false;
		}
		bool ItIsAlreadyPresent = false;

		if (IsAlreadyPresent(mrk))
		{
			if (EnableOverwriting)
				ItIsAlreadyPresent = true;
			else
				return;
		}

		AddNewPoint(mrk, ItIsAlreadyPresent);
	}
	/// <summary>
	/// Saves list of znaczniki to buffer as one operation to possible undo
	/// </summary>
	/// <param name="Mrks"></param>
	public static void AddZnaczniki(List<Vector3> Mrks)
	{
		UndoZnaczniki = Mrks.ToList();
	}
	/// <summary>
	/// Signalizes that next call of AddZnacznik will belong to new operation
	/// </summary>
	public static void ApplyOperation()
	{
		Clear_buffer_before_next_add = true;
		UndoZnaczniki = UndoZnaczniki.Distinct().ToList();
	}
	/// <summary>
	/// When Ctrl + Z is clicked
	/// </summary>
	public static void PasteUndoZnaczniki()
	{
		//Indexes of vertices for UpdateMapColliders()
		List<int> indexes = new List<int>();
		// List of tiles lying onto vertices that are now being pasted
		List<GameObject> tiles_to_update = new List<GameObject>();
		foreach (var mrk in UndoZnaczniki)
		{
			if (Service.IsWithinMapBounds(mrk))
			{
				// Update arrays of vertex heights
				indexes.Add(Service.PosToIndex(mrk));
				Service.current_heights[indexes[indexes.Count - 1]] = mrk.y;
				Service.former_heights[indexes[indexes.Count - 1]] = mrk.y;

				Vector3 pom = mrk;

				// Mark pasted vertices
				GameObject zn = Service.MarkAndReturnZnacznik(pom);
				if (zn != null)
					zn.transform.position = new Vector3(zn.transform.position.x, mrk.y, zn.transform.position.z);

				// Look for tiles lying here
				{
					RaycastHit tile;
					pom.y = Service.MAX_H;
					if (Physics.SphereCast(pom, 0.1f, Vector3.down, out tile, Service.MAX_H - Service.MIN_H, 1 << 9) && !tiles_to_update.Contains(tile.transform.gameObject))
						tiles_to_update.Add(tile.transform.gameObject);
				}
			}
		}
		Service.UpdateMapColliders(indexes);
		Build.UpdateTiles(tiles_to_update);
		ClearBuffer();
	}

}

