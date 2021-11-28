using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
/// <summary>
/// Static class containing fields that need to survive during scene change
/// </summary>
public static class Consts
{
	public readonly static string VERSION = "3.1";
	/// <summary>Maximum tile limit</summary>
	public readonly static int MAX_ELEMENTS = 8000;
	internal static readonly string CHKPOINTS_STR = "Checkpoints";
	public readonly static int MAX_H = 20000 + 5;
	public readonly static int MIN_H = -MAX_H;
	public readonly static int RAY_H = MAX_H + 1 - MIN_H;
	public static GameObject Cone;
	public static int GravityValue = 0;
	public readonly static string tilesets_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Crashday 3D Editor\\tilesets.txt";
	public readonly static string userdata_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Crashday 3D Editor\\userdata.txt";
	public readonly static string path_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Crashday 3D Editor\\path.txt";
	public readonly static string documents_3deditor_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Crashday 3D Editor";
	/// <summary>
	/// visible vertices in second form mode
	/// </summary>
	public static Vector2Int MarkerBounds = new Vector2Int(60, 60);
	public static TrackSavable TRACK { get; set; }
	///<summary> Is editor loading map? </summary>
	
	///<summary> requires BL pos</summary>
	public static TilePlacement[,] TilePlacementArray { get; set; }
	///<summary> String showed on the top bar of the editor during mapping </summary>
	public static string Trackname { get; set; } = "Untitled";
	public static string DefaultTilesetName { get; set; } = "Hidden";
	public static float[] former_heights;
	public static float[] current_heights;
	/// Load track by inversing elements
	/// </summary>
	public static bool LoadMirrored = false;
	public static List<string> MissingTilesNames = new List<string>();
	public static bool IsWithinMapBounds(Vector3 v)
	{
		return (v.x > 0 && v.x < 4 * Consts.TRACK.Width && v.z > 0 && v.z < 4 * Consts.TRACK.Height) ? true : false;
	}
	public static bool IsWithinMapBounds(float x, float z)
	{
		return x > 0 && x < 4 * Consts.TRACK.Width && z > 0 && z < 4 * Consts.TRACK.Height;
	}
	/// <summary>
	/// Loads latest path from StreamingAssets/Path.txt
	/// </summary>
	/// <returns></returns>
	public static string LoadLastFolderPath()
	{
		string MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		StreamReader w = new StreamReader(Consts.path_path);
		string LastTrackPath = w.ReadLine();
		w.Close();
		if (LastTrackPath == "")
			LastTrackPath = MyDocuments;
		//Debug.Log("LoadPath:" + LastTrackPath);
		return LastTrackPath;
	}

	/// <summary>
	/// Saves latest path to StreamingAssets/Path.txt
	/// </summary>
	public static void SaveLastFolderPath(string path)
	{
		if (path == null)
		{
			Debug.LogError("path null");
			return;
		}
		Debug.Log(path);
		StreamWriter w = new StreamWriter(Consts.path_path);
		w.WriteLine(path);
		w.Close();
	}
	public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles, bool precision = false)
	{
		Vector3 dir = point - pivot; // get point direction relative to pivot
		dir = Quaternion.Euler(angles) * dir; // rotate it
		point = dir + pivot; // calculate rotated point
		if (precision)
			return point;
		else
			return RoundVector3(point);
	}
	/// <summary>
	/// Distance on 2D map between 3D points
	/// </summary>
	public static float Distance(Vector3 v1, Vector3 v2)
	{
		return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z));
	}
	public static float SliderValue2RealHeight(float sliderval)
	{
		return sliderval / 5f;
	}
	public static float RealHeight2SliderValue(float realheight)
	{
		return 5 * realheight;
	}
	/// <summary>
	/// Returns global position of vertex. Sets Y height from current_points array
	/// </summary>
	public static Vector3 IndexToPos(int index)
	{
		int x = index % (4 * Consts.TRACK.Width + 1);
		Vector3 to_return = new Vector3(x, Consts.current_heights[index], (index - x) / (4 * Consts.TRACK.Width + 1));
		return to_return;
	}
	public static int PosToIndex(int x, int z)
	{
		return Mathf.RoundToInt(x + 4 * z * Consts.TRACK.Width + z);
	}
	public static int PosToIndex(Vector3 v)
	{
		int index = Mathf.RoundToInt(v.x + 4 * v.z * Consts.TRACK.Width + v.z);
		return index;
	}

	public static GameObject CreateMarking(Material material, Vector3? pos = null, bool hasCollider = true)
	{
		GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
		znacznik.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
		Vector3 newposition = new Vector3();
		if (pos == null)
			newposition = Highlight.pos;
		else if (float.IsNaN(pos.Value.y))
			newposition.Set(pos.Value.x, 0, pos.Value.z);
		else
			newposition = (Vector3)pos;
		znacznik.transform.position = newposition;

		if (hasCollider)
			znacznik.GetComponent<BoxCollider>().enabled = true;
		else
			UnityEngine.Object.Destroy(znacznik.GetComponent<BoxCollider>());

		znacznik.GetComponent<MeshRenderer>().material = material;

		znacznik.layer = 11;
		return znacznik;
	}
	/// <summary>
	/// Searches for znacznik in given pos. If found znacznik isn't marked, f. marks it and returns it.
	/// </summary>
	public static GameObject MarkAndReturnZnacznik(Vector3 z_pos)
	{
		z_pos.y = Consts.MAX_H;
		if (Physics.Raycast(z_pos, Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11))
		{
			if (hit.transform.name == "on")
				return hit.transform.gameObject;
			else
			{
				hit.transform.name = "on";
				hit.transform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("red");
				return hit.transform.gameObject;
			}
		}
		return null;
	}
	public static void UpdateMapColliders(List<GameObject> mcs, bool IsRecoveringTerrain = false)
	{
		if (mcs[0].layer == 11) // Argumentami znaczniki
		{
			HashSet<int> indexes = new HashSet<int>();
			foreach (GameObject znacznik in mcs)
			{
				Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
				indexes.Add(PosToIndex(v.x, v.z));
			}
			UpdateMapColliders(indexes, IsRecoveringTerrain);
		}
		else //Argumentami MapCollidery
		{
			foreach (GameObject grass in mcs)
			{
				Vector3[] verts = grass.GetComponent<MeshCollider>().sharedMesh.vertices;
				bool HasNaNs = false;
				for (int i = 0; i < verts.Length; i++)
				{
					Vector3Int v = Vector3Int.RoundToInt(grass.transform.TransformPoint(verts[i]));
					if (IsRecoveringTerrain)
					{
						verts[i].y = Consts.former_heights[Consts.PosToIndex(v)];
						Consts.current_heights[Consts.PosToIndex(v)] = Consts.former_heights[Consts.PosToIndex(v)];
					}
					else
						verts[i].y = Consts.current_heights[Consts.PosToIndex(v)];
					if (float.IsNaN(verts[i].y))
						HasNaNs = true;
				}
				var mc = grass.GetComponent<MeshCollider>();
				var mf = grass.GetComponent<MeshFilter>();

				if (!HasNaNs)
				{
					mc.sharedMesh.vertices = verts;
					mc.sharedMesh.RecalculateBounds();
					mc.sharedMesh.RecalculateNormals();
					mf.mesh = mc.sharedMesh;
				}
				else
				{// mesh has to have a collider but it can't be displayed with NaNs so we create collider with lowest points so it can be cast on

					// meshfilter has NaNs
					mf.mesh.vertices = verts;
					mf.mesh.RecalculateBounds();
					mf.mesh.RecalculateNormals();

					// mesh collider has lowestPoint flat mesh
					float lowestPoint = 0;// verts.Select(v => v.y).Where(n => !float.IsNaN(n)).Min();
					Vector3[] newverts = new Vector3[verts.Length];
					for (int i = 0; i < verts.Length; i++)
						newverts[i].Set(verts[i].x, lowestPoint, verts[i].z);
					Mesh mcMesh = UnityEngine.Object.Instantiate(Resources.Load<Mesh>("rmcs/basic"));
					mcMesh.vertices = newverts;
					mcMesh.RecalculateBounds();
					mc.sharedMesh = mcMesh;
				}
				mc.enabled = false;
				mc.enabled = true;
			}
		}
	}
	/// <summary>
	/// Prevent numerical errors
	/// </summary>
	/// <param name="v"></param>
	/// <returns></returns>
	public static Vector3 RoundVector3(Vector3 v)
	{
		return new Vector3(Mathf.RoundToInt(v.x), v.y, Mathf.RoundToInt(v.z));
	}
	/// <summary>
	/// List of map colliders, \-/ position from index cast ray (layer=8). If hit isn't on list, add it. Run overload for gameObjects.
	/// If recovering, the only indexes that are going to be recovered are those from indexes list
	/// </summary>
	public static void UpdateMapColliders(HashSet<int> indexes, bool IsRecoveringTerrain = false)
	{
		if (indexes.Count == 0)
			return;
		List<GameObject> mcs = new List<GameObject>();
		foreach (int i in indexes)
		{
			Vector3Int v = Vector3Int.RoundToInt(Consts.IndexToPos(i));
			v.y = Consts.MAX_H;
			RaycastHit[] hits = Physics.SphereCastAll(v, .1f, Vector3.down, Consts.RAY_H, 1 << 8);
			foreach (RaycastHit hit in hits)
				if (!mcs.Contains(hit.transform.gameObject))
				{
					mcs.Add(hit.transform.gameObject);
				}
		}
		foreach (GameObject grass in mcs)
		{
			Vector3[] verts = grass.GetComponent<MeshCollider>().sharedMesh.vertices;
			bool HasNaNs = false;
			for (int i = 0; i < verts.Length; i++)
			{
				Vector3Int v = Vector3Int.RoundToInt(grass.transform.TransformPoint(verts[i]));
				if (IsRecoveringTerrain)
				{
					if (indexes.Contains(Consts.PosToIndex(v)))
					{ // Recover only listed indexes ...
						verts[i].y = Consts.former_heights[Consts.PosToIndex(v)];
						Consts.current_heights[Consts.PosToIndex(v)] = Consts.former_heights[Consts.PosToIndex(v)];
					}
					else
					{ // ... rest is assigned from current_heights
						verts[i].y = Consts.current_heights[Consts.PosToIndex(v)];
					}
				}
				else
					verts[i].y = Consts.current_heights[Consts.PosToIndex(v)];
				if (float.IsNaN(verts[i].y))
					HasNaNs = true;
			}
			if (!grass.GetComponent<MeshRenderer>().enabled)
				continue;
			var mc = grass.GetComponent<MeshCollider>();
			var mf = grass.GetComponent<MeshFilter>();

			if (!HasNaNs)
			{
				mc.sharedMesh.vertices = verts;
				mc.sharedMesh.RecalculateBounds();
				mc.sharedMesh.RecalculateNormals();
				mf.mesh = mc.sharedMesh;
			}
			else
			{// mesh has to have a collider but it can't be displayed with NaNs
				// so we create collider with lowest points so it can be cast on

				// meshfilter has NaNs
				mf.mesh.vertices = verts;
				mf.mesh.RecalculateBounds();
				mf.mesh.RecalculateNormals();

				// mesh collider has lowestPoint flat mesh
				float lowestPoint = 0;// verts.Select(v => v.y).Where(n => !float.IsNaN(n)).Min();
				Vector3[] newverts = new Vector3[verts.Length];
				for (int i = 0; i < verts.Length; i++)
					newverts[i].Set(verts[i].x, lowestPoint, verts[i].z);
				Mesh mcMesh = UnityEngine.Object.Instantiate(Resources.Load<Mesh>("rmcs/basic"));
				mcMesh.vertices = newverts;
				mcMesh.RecalculateBounds();
				mc.sharedMesh = mcMesh;
			}
			mc.enabled = false;
			mc.enabled = true;
		}
	}
	public static void UpdateMapColliders(Vector3 rmc_pos, Vector3Int tileDims, bool recover_terrain = false)
	{
		rmc_pos.y = Consts.MAX_H;
		RaycastHit[] hits = Physics.BoxCastAll(rmc_pos, new Vector3(2 * tileDims.x, 1, 2 * tileDims.z),
				Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 8);
		List<GameObject> mcs = new List<GameObject>();
		foreach (RaycastHit hit in hits)
		{
			mcs.Add(hit.transform.gameObject);
		}
		UpdateMapColliders(mcs, recover_terrain);
	}
	public static float Smoothstep(float edge0, float edge1, float x)
	{
		if (edge1 == edge0)
			return 0;
		// Scale to 0 - 1
		x = (x - edge0) / (edge1 - edge0);
		return x * x * (3 - 2 * x);
	}
	public static IEnumerable<string> SplitBy(this string str, int chunkLength)
	{
		for (int i = 0; i < str.Length; i += chunkLength)
		{
			if (chunkLength + i > str.Length)
				chunkLength = str.Length - i;

			yield return str.Substring(i, chunkLength);
		}
	}
	public static string ReplaceFirst(this string text, string search, string replace)
	{
		int pos = text.IndexOf(search);
		if (pos < 0)
		{
			return text;
		}
		return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
	}
	/// <summary>
	/// returns true if on one border. 
	/// returns false if on position where borders intersect or not on border
	/// </summary>
	/// <param name="v"></param>
	/// <returns></returns>
	internal static bool Lies_on_border(Vector3Int v)
	{
		if (v.x % 4 == 0 ^ v.z % 4 == 0)
			return true;
		else
			return false;
	}
	internal static bool Lies_on_restricted_border(Vector3 v, BorderType border, Quarter q)
	{
		switch (border)
		{
			case BorderType.Horizontal:
				// check if lies on top/bottom border
				if (v.x % 4 != 0 && v.z % 4 == 0)
				{
					if (v.z > q.pos.z)
					{ // check top border
							// if top border is restricted, v lies on restricted border
						return q.qt.Vx_up_restricted;
					}
					else
					{ // check bottom border
						return q.qt.Vx_down_restricted;
					}
				}
				else
				{
					return false;
				}
			case BorderType.Vertical:
				// check if lies on left/right border
				if (v.z % 4 != 0 && v.x % 4 == 0)
				{
					if (v.x > q.pos.x)
					{ // check right border
							// if right border is restricted, v lies on restricted border
						return q.qt.Hx_right_restricted;
					}
					else
					{ // check bottom border
						return q.qt.Hx_left_restricted;
					}
				}
				else
				{
					return false;
				}
		}
		return false;
	}
	internal static bool Lies_on_any_restricted_borders(Vector3 v, Quarter q)
	{
		foreach(var border in new List<BorderType>(2) { BorderType.Vertical, BorderType.Horizontal })
		{
			if (Lies_on_restricted_border(v, border, q))
				return true;
		}
		return false;
	}
}
