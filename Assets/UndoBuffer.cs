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
  /// Clear buffer before next call of AddZnacznik
  /// </summary>
  private static bool Clear_buffer_before_next_add = false;
  /// <summary>
  /// Adds new znacznik to buffer. Moreover if ApplyOperation was run before, f. will clear buffer once before addition.
  /// </summary>
  /// <param name="mrk"></param>
  public static void AddZnacznik(Vector3 mrk)
  {
    if (Clear_buffer_before_next_add)
    {
      UndoZnaczniki.Clear();
      Clear_buffer_before_next_add = false;
    }
    UndoZnaczniki.Add(mrk);
  }
  /// <summary>
  /// Saves list of znaczniki to buffer as one operation to possible undo
  /// </summary>
  /// <param name="Mrks"></param>
  public static void AddOperation(List<Vector3> Mrks)
  {
    UndoZnaczniki = Mrks;
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
  /// Recovers all height points that were previously added to buffer as undo
  /// </summary>
  public static void PasteUndoZnaczniki()
  {
    //Indexes of vertices for UpdateMapColliders()
    List<int> indexes = new List<int>();

    // List of tiles lying onto vertices that are now being pasted
    List<GameObject> to_update = new List<GameObject>();
    foreach (var mrk in UndoZnaczniki)
    {
      if (Terraining.IsWithinMapBounds(mrk))
      {
        // Update arrays of vertex heights
        indexes.Add(Loader.PosToIndex(mrk));
        Loader.current_heights[indexes[indexes.Count - 1]] = mrk.y;
        Loader.former_heights[indexes[indexes.Count - 1]] = mrk.y;

        Vector3 pom = mrk;

        // Mark pasted vertices
        GameObject zn = Terraining.MarkAndReturnZnacznik(pom);
        if (zn != null)
          zn.transform.position = new Vector3(zn.transform.position.x, mrk.y, zn.transform.position.z);

        // Look for tiles lying here
        pom.y = Data.maxHeight;
        RaycastHit[] tile_raycasts = Physics.SphereCastAll(pom, 0.1f, Vector3.down, Data.maxHeight - Data.minHeight, 1 << 9);
        GameObject[] tiles = tile_raycasts.Where(tile => !to_update.Contains(tile.transform.gameObject)).Select(tile => tile.transform.gameObject).ToArray();
        to_update.AddRange(tiles);

      }
    }
    Terraining.UpdateMapColliders(indexes);
    Building.UpdateTiles(to_update);
    UndoZnaczniki.Clear();
  }
}

