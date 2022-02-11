using PathCreation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum ProfileState { IDLE, SELECTING_P1, SELECTING_P2, MANUAL_SELECTION, PREVIEW_VISIBLE }
/// <summary>
/// Hooked in Profiles Menu. Responsible for Profiles functionality
/// </summary>
public class ProfileCreator : MonoBehaviour
{
	public Toggle tClosedPath;
	public Slider sCurviness;
	public GameObject FormPanel;
	public GameObject HeightSliderAndPreviewToggle;
	public FormSlider formSlider;
	private VertexPath[] paths;
	private GameObject RoadMesh;
	private List<List<GameObject>> Profiles = new List<List<GameObject>>();
	public Material white;
	public Material red;
	public static ProfileState state = ProfileState.IDLE;

	private void OnEnable()
	{
		HeightSliderAndPreviewToggle.SetActive(false);
	}

	private void OnDisable()
	{
		RemovePreview();
		ClearPath();
		HeightSliderAndPreviewToggle.SetActive(true);
	}

	private void StateSwitch(ProfileState s)
	{
		state = s;
		if (state == ProfileState.IDLE)
		{
			formSlider.SwitchTextStatus("Profiles mode");
		}
		else if (state == ProfileState.SELECTING_P1)
		{
			if (Profiles.Count < 3)
				formSlider.SwitchTextStatus("Selecting P1..");
			else
				formSlider.SwitchTextStatus("Selecting P1 / Enter..");
		}
		else if (state == ProfileState.SELECTING_P2)
		{
			formSlider.SwitchTextStatus("Selecting P2..");
		}
		else if (state == ProfileState.MANUAL_SELECTION)
		{
			if (Profiles.Count == 1 && Profiles[0].Count > 1)
				formSlider.SwitchTextStatus("Manual selection / Enter");
			else
				formSlider.SwitchTextStatus("Manual selection");
		}
		else if (state == ProfileState.PREVIEW_VISIBLE)
		{
			formSlider.SwitchTextStatus("Enter/Esc..");
		}
	}

	private void Update()
	{
 		AutoManualSwitch();

		if (Input.GetMouseButtonDown(0) && !MouseInputUIBlocker.BlockedByUI && Consts.IsWithinMapBounds(Highlight.pos))
		{
			if (state == ProfileState.IDLE || state == ProfileState.SELECTING_P1)
				Add_P1();
			else if (state == ProfileState.SELECTING_P2)
				AddProfile();
			else if (state == ProfileState.MANUAL_SELECTION)
				AddPoint();
		}
		else if (Input.GetKeyDown(KeyCode.Return))
		{
			if (state == ProfileState.MANUAL_SELECTION && IsNewProfileValid())
				StateSwitch(ProfileState.SELECTING_P1);
			else if (state == ProfileState.SELECTING_P1 || state == ProfileState.IDLE)
				GeneratePreview();
			else if (state == ProfileState.PREVIEW_VISIBLE)
				ApplyPath();
		}
		else if (state == ProfileState.PREVIEW_VISIBLE && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape)))
		{
			RemovePreview();
		}
		else if (state != ProfileState.PREVIEW_VISIBLE)
		{
			if (Input.GetKeyDown(KeyCode.Backspace))
				RemoveProfile();
			else if (Input.GetKeyDown(KeyCode.Escape))
				ClearPath();
		}
	}

	private void AutoManualSwitch()
	{
		if (state == ProfileState.MANUAL_SELECTION && !Input.GetKey(KeyCode.LeftControl) && Profiles.Last().Count == 1)
		{
			StateSwitch(ProfileState.SELECTING_P2);
		}
		else if (state == ProfileState.SELECTING_P2 && Input.GetKeyDown(KeyCode.LeftControl))
		{
			StateSwitch(ProfileState.MANUAL_SELECTION);
		}
	}

	void Add_P1()
	{
		GameObject znacznik = Consts.CreateMarking(red);
		Profiles.Add(new List<GameObject>() { znacznik });
		if (!Input.GetKey(KeyCode.LeftControl))
			StateSwitch(ProfileState.SELECTING_P2);
		else
			StateSwitch(ProfileState.MANUAL_SELECTION);
	}

	void AddProfile()
	{
		Vector3[] ProfilePoints = GetRemainingPoints(Profiles.Last().Last().transform.position, Highlight.pos);
		if (!IsNewProfileValid(ProfilePoints.Length + 1))
			return;
		foreach (Vector3 pos in ProfilePoints)
			Profiles.Last().Add(Consts.CreateMarking(white, pos));

		StateSwitch(ProfileState.SELECTING_P1);
	}

	void AddPoint()
	{
		GameObject znacznik = Consts.CreateMarking(white);
		Profiles.Last().Add(znacznik);
		if (Profiles.Count > 1 && Profiles.Last().Count == Profiles[0].Count)
			StateSwitch(ProfileState.SELECTING_P1);
		else if (Profiles.Count == 1 && Profiles[0].Count == 2)
			StateSwitch(ProfileState.MANUAL_SELECTION); // Allow Enter
	}

	void RemoveProfile()
	{
		if (Profiles.Count == 0)
			return;
		for (int i = 0; i < Profiles.Last().Count; i++) // delete markings of current profile
			Destroy(Profiles.Last()[i]);
		Profiles.RemoveAt(Profiles.Count - 1);
		if (Profiles.Count == 0)
			StateSwitch(ProfileState.IDLE);
		else
			StateSwitch(ProfileState.SELECTING_P1);
	}

	Vector3[] GetRemainingPoints(Vector3 begin, Vector3 end)
	{
		List<Vector3> to_return = new List<Vector3>();
		while (begin.x != end.x || begin.z != end.z)
		{
			if (begin.x != end.x)
				begin.x += begin.x < end.x ? 1 : -1;
			if (begin.z != end.z)
				begin.z += begin.z < end.z ? 1 : -1;
			to_return.Add(Consts.IndexToPos(Consts.PosToIndex((int)begin.x, (int)begin.z)));
		}
		return to_return.ToArray();
	}
	//void DrawLine(Vector3 start, Color color, float duration = 1)
	//{
	//  GameObject myLine = new GameObject();
	//  myLine.transform.position = start;
	//  myLine.AddComponent<LineRenderer>();
	//  LineRenderer lr = myLine.GetComponent<LineRenderer>();
	//  lr.material = new Material(Shader.Find("Mobile/Bumped Diffuse"));
	//  lr.startColor = color;
	//  lr.endColor = color;
	//  lr.startWidth = 0.5f;
	//  lr.endWidth = 0.5f;
	//  lr.SetPosition(0, start);
	//  lr.SetPosition(1, new Vector3(start.x, Consts.maxHeight, start.z));
	//  Destroy(myLine, duration);
	//}

	bool IsNewProfileValid()
	{
		return Profiles.Count == 1 ? Profiles[0].Count > 1 : Profiles.Last().Count == Profiles[0].Count;
	}

	bool IsNewProfileValid(int NewProfileLength)
	{
		return Profiles.Count == 1 ? NewProfileLength > 1 : NewProfileLength == Profiles[0].Count;
	}

	public void ApplyPath()
	{
		if (RoadMesh == null)
			return;
		Vector3Int minV = Vector3Int.RoundToInt(RoadMesh.GetComponent<MeshFilter>().mesh.bounds.min);
		Vector3Int maxV = Vector3Int.RoundToInt(RoadMesh.GetComponent<MeshFilter>().mesh.bounds.max);
		HashSet<int> indexes = new HashSet<int>();
		for (int x = minV.x; x <= maxV.x; x++)
		{
			for (int z = minV.z; z <= maxV.z; z++)
			{
				if (!Consts.IsWithinMapBounds(x, z))
					continue;
				if (Physics.Raycast(new Vector3(x, Consts.MAX_H, z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 14))
				{
					int idx = Consts.PosToIndex(x, z);
					indexes.Add(idx);
					Vector3 for_buffer = Consts.IndexToPos(idx);
					Consts.former_heights[idx] = hit.point.y;
					Consts.current_heights[idx] = Consts.former_heights[idx];
					UndoBuffer.AddVertexPair(for_buffer, Consts.IndexToPos(idx));
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		//Search for any tiles
		var surr = Build.Get_surrounding_tiles(indexes);
		Build.UpdateTiles(surr);
		UndoBuffer.ApplyTerrainOperation();
		RemovePreview();
	}

	public void RemovePreview()
	{
		if (RoadMesh)
		{
			Destroy(RoadMesh);
			RoadMesh = null;
		}

		if (Profiles.Count > 0)
			StateSwitch(ProfileState.SELECTING_P1);
		else
			StateSwitch(ProfileState.IDLE);
	}

	public void ClearPath()
	{
		foreach (var ProfileList in Profiles)
		{
			for (int i = 0; i < ProfileList.Count; i++)
				Destroy(ProfileList[i]);
		}
		Profiles.Clear();
		StateSwitch(ProfileState.IDLE);
	}

	public void GeneratePreview()
	{
		if (Profiles.Count < 3)
			return;

		paths = new VertexPath[Profiles[0].Count];
		for (int i = 0; i < paths.Length; i++)
		{
			Vector3[] HelperArray = new Vector3[Profiles.Count];
			for (int j = 0; j < Profiles.Count; j++)
				HelperArray[j] = Profiles[j][i].transform.position;

			paths[i] = GeneratePath(HelperArray);
		}
		// preview
		//foreach (var path in paths)
		//{
		//  Color c = new Color(Random.value, Random.value, Random.value);
		//  foreach (var point in path.localPoints)
		//  {
		//    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		//    Spheres.Add(sphere);
		//    Destroy(sphere.GetComponent<SphereCollider>());
		//    MeshRenderer r = sphere.GetComponent<MeshRenderer>();
		//    r.material.color = c;
		//    sphere.transform.position = point;
		//    sphere.transform.localScale /= 8f;
		//  }
		//}
		// find longest path (with most vertices)
		int mostvertices = 0;
		foreach (var path in paths)
		{
			if (mostvertices < path.localPoints.Length)
				mostvertices = path.localPoints.Length;
		}
		// create vertex cloud
		List<Vector3> verts = new List<Vector3>(mostvertices * paths.Length);
		foreach (var path in paths)
		{
			if (mostvertices == path.localPoints.Length)
			{
				verts.AddRange(path.localPoints);
			}
			else
			{
				for (int i = 0; i < mostvertices; i++)
				{
					verts.Add(path.GetPointAtTime(i / (mostvertices - 1f), EndOfPathInstruction.Stop));
				}
			}
		}
		Mesh mesh = CreateProfileMesh(verts, paths.Length, mostvertices);
		RoadMesh = new GameObject("Profile mesh");
		RoadMesh.gameObject.layer = 14;
		MeshFilter mf = RoadMesh.AddComponent<MeshFilter>();
		mf.sharedMesh = mesh;
		MeshRenderer mr = RoadMesh.AddComponent<MeshRenderer>();
		MeshCollider mc = RoadMesh.AddComponent<MeshCollider>();
		mc.sharedMesh = mf.sharedMesh;

		StateSwitch(ProfileState.PREVIEW_VISIBLE);
	}

	private Mesh CreateProfileMesh(List<Vector3> vertz, int pnx, int pny)
	{
		List<int> tris = new List<int>();
		// one side
		for (int y = 0; y < pnx - 1; y++)
		{
			for (int i = 0; i < pny - 1; i++)
			{
				// front
				tris.Add(y * pny + i);
				tris.Add(y * pny + i + 1);
				tris.Add(y * pny + i + pny);

				tris.Add(y * pny + i + 1);
				tris.Add(y * pny + i + pny + 1);
				tris.Add(y * pny + i + pny);

				// back
				tris.Add(y * pny + i + pny);
				tris.Add(y * pny + i + pny + 1);
				tris.Add(y * pny + i);

				tris.Add(y * pny + i + pny + 1);
				tris.Add(y * pny + i + 1);
				tris.Add(y * pny + i);
			}
		}
		Mesh m = new Mesh();
		m.SetVertices(vertz);
		m.triangles = tris.ToArray();
		return m;
	}
	private VertexPath GeneratePath(Vector3[] points)
	{
		BezierPath.autoControlLength = sCurviness.value / 10f;
		BezierPath bezierPath = new BezierPath(points, tClosedPath.isOn);
		// Then create a vertex path from the bezier path, to be used for movement etc
		return new VertexPath(bezierPath, transform, 0.5f);
	}
	//static GameObject CreatePlane(int pnx, int pny, Material feedmaterial)
	//{
	//    List<Vector3> vertz = new List<Vector3>();
	//    List<Vector2> uvs = new List<Vector2>();
	//    List<int> tris = new List<int>();
	//    //Tworzę vertexy i uv
	//    for (int y = 0; y < pny; y++)
	//    {
	//        for (int x = 0; x < pnx; x++)
	//        {
	//            vertz.Add(new Vector3(x, 0, y));
	//            uvs.Add(new Vector2(x, y));
	//        }
	//    }
	//    //Tworzę tris
	//    for (int y = 0; y < pny - 1; y++)
	//    {
	//        for (int i = 0; i < pnx - 1; i++)
	//        {
	//            tris.Add(y * pnx + i);
	//            tris.Add(y * pnx + i + pnx);
	//            tris.Add(y * pnx + i + 1);

	//            tris.Add(y * pnx + i + pnx);
	//            tris.Add(y * pnx + i + pnx + 1);
	//            tris.Add(y * pnx + i + 1);
	//        }
	//    }
	//    go = new GameObject("Map");
	//    go.transform.position = new Vector3(0, 0, 0);
	//    MeshFilter mf = go.AddComponent<MeshFilter>();
	//    MeshRenderer mr = go.AddComponent<MeshRenderer>();
	//    Mesh m = new Mesh();
	//    m.SetVertices(vertz);
	//    m.SetUVs(0, uvs);
	//    m.triangles = tris.ToArray();
	//    mf.sharedMesh = m;
	//    mr.material = feedmaterial;
	//    mf.sharedMesh.RecalculateNormals();
	//    return go;
	//}
}
