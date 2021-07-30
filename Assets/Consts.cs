using System.Collections.Generic;
using System.IO;
using UnityEngine;
/// <summary>
/// Static class containing fields that need to survive during scene change
/// </summary>
public static class Consts
{
	public readonly static string VERSION = "2.5";
	/// <summary>Maximum tile limit</summary>
	public readonly static int MAX_ELEMENTS = 8000;
	internal static readonly string CHKPOINTS_STR = "Checkpoints";
	public readonly static int MAX_H = 20000;
	public readonly static int MIN_H = -MAX_H;
	public readonly static int RAY_H = MAX_H - MIN_H + 5;
	public static GameObject Cone;
	public static int GravityValue = 0;
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
	public static readonly string[] RMC_NAMES = { "1x1", "1x1H1", "1x1H1V1", "1x1V1", "1x1V1H1", "1x2", "1x2H1H2", "1x2H1V1", "1x2H1V1H2", "1x2V1", "1x2H1", "1x2V1H1", "1x2V1H1H2", "2x1", "2x1H1", "2x1H1V1", "2x1H1V1V2", "2x1V1", "2x1V1H1", "2x1V1H1V2", "2x1V1V2", "2x2", "2x2H1V1", "2x2H1V1H2", "2x2H1V1H2V2", "2x2H2V2", "2x2V1", "2x2V1H1", "2x2V1H1H2", "2x2V1H1V2", "2x2V1H1V2H2", "2x2V1V2", "2x2V1V2H1H2", "2x2V1V2H2", "2x2V1V2H2H2", "2x2V2H2", "2x2V2H2H1", "2x2V2H1", "2x2V1H2"};
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
	public static string LoadTrackPath()
	{
		StreamReader w = new StreamReader(Application.dataPath + "/StreamingAssets/path.txt");
		string LastTrackPath = w.ReadLine();
		w.Close();
		if (LastTrackPath == "")
			LastTrackPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
		return LastTrackPath;
	}

	/// <summary>
	/// Saves latest path to StreamingAssets/Path.txt
	/// </summary>
	public static void SaveTrackPath(string path)
	{
		StreamWriter w = new StreamWriter(Application.dataPath + "/StreamingAssets/path.txt");
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
			return new Vector3(Mathf.RoundToInt(point.x), point.y, Mathf.RoundToInt(point.z)); // return it
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
		return Mathf.RoundToInt(v.x + 4 * v.z * Consts.TRACK.Width + v.z);
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
			Object.Destroy(znacznik.GetComponent<BoxCollider>());

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
					Mesh mcMesh = Object.Instantiate(Resources.Load<Mesh>("rmcs/basic"));
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
	public static Vector3 Round_X_Z_InVector(Vector3 v)
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
				Mesh mcMesh = Object.Instantiate(Resources.Load<Mesh>("rmcs/basic"));
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
		RaycastHit[] hits = Physics.BoxCastAll(rmc_pos, new Vector3(4 * tileDims.x, 1, 4 * tileDims.z),
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
}




