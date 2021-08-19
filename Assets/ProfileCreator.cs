using PathCreation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

enum ProfileState { idle, first_clicked, profileApplied, preview_visible }
/// <summary>
/// Hooked in Profiles Menu. Responsible for Profiles functionality
/// </summary>
public class ProfileCreator : MonoBehaviour
{
	public Toggle tClosedPath;
	public Slider sCurviness;
	public GameObject FormPanel;
	private VertexPath[] paths;
	private GameObject RoadMesh;
	private List<List<GameObject>> Profiles = new List<List<GameObject>>();
	public Material white;
	public Material red;
	private ProfileState state = ProfileState.idle;

	private void OnEnable()
	{
		FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(false);
	}
	private void OnDisable()
	{
		RemovePreview();
		state = ProfileState.idle;
		FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(true);
	}
	private void SwitchTextStatus(string text)
	{
		FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus(text);
	}
	private void SwitchTextStatus(ProfileState s)
	{
		state = s;
		if (s == ProfileState.idle)
			SwitchTextStatus("Ready..");
		else if (s == ProfileState.first_clicked)
			SwitchTextStatus("*Selecting*");
		else if (s == ProfileState.profileApplied)
			SwitchTextStatus("RMB/backspace");
		else if (s == ProfileState.preview_visible)
			SwitchTextStatus("Enter/Backspace/Del");
	}
	private void Update()
	{
		SelectingProfile();

		if (Input.GetMouseButtonDown(1))
			AcceptProfile();
		if (Input.GetKeyDown(KeyCode.Backspace) && state != ProfileState.preview_visible)
			RemoveProfile();
		if (Input.GetKeyDown(KeyCode.Return) && (state == ProfileState.profileApplied || state == ProfileState.idle))
			GeneratePreview();
		else if (Input.GetKeyDown(KeyCode.Return) && state == ProfileState.preview_visible)
			ApplyProfile();
		if (Input.GetKeyDown(KeyCode.Backspace) && state == ProfileState.preview_visible)
			RemovePreview(false);
		if (Input.GetKeyDown(KeyCode.Delete) && state == ProfileState.preview_visible)
			RemovePreview(true);
	}
	void AcceptProfile()
	{
		if (!IsNewProfileValid())
			return;
		SwitchTextStatus(ProfileState.idle);
	}
	void RemoveProfile()
	{
		if (Profiles.Count == 0)
			return;
		for (int i = 0; i < Profiles.Last().Count; i++) // delete markings of current profile
			Destroy(Profiles.Last()[i]);
		Profiles.Remove(Profiles.Last());
		SwitchTextStatus(ProfileState.idle);
	}

	void SelectingProfile()
	{
		if (!MouseInputUIBlocker.BlockedByUI)
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (!Consts.IsWithinMapBounds(Highlight.pos))
					return;
				if (Input.GetKey(KeyCode.LeftControl)) // add single vertex
				{
					if (state == ProfileState.idle || state == ProfileState.first_clicked)
					{ // first or middle marking
						if (state == ProfileState.idle)
						{
							GameObject znacznik = Consts.CreateMarking(red);
							Profiles.Add(new List<GameObject>() { znacznik }); //add first marking to list
						}
						else if (state == ProfileState.first_clicked)
						{
							GameObject znacznik = Consts.CreateMarking(white);
							Profiles.Last().Add(znacznik); //add middle marking
						}
						SwitchTextStatus(ProfileState.first_clicked);
					}
				}
				else if (state == ProfileState.profileApplied) // standard multiple selection
				{ // redo first marking
					GameObject znacznik = Consts.CreateMarking(white);
					for (int i = 0; i < Profiles.Last().Count; i++) // delete markings of current profile
						Destroy(Profiles.Last()[i]);
					Profiles.Last().Clear();
					znacznik.GetComponent<MeshRenderer>().material = red;
					Profiles.Last().Add(znacznik); // add to current profile 
					SwitchTextStatus(ProfileState.first_clicked);
				}
				else if (state == ProfileState.idle)
				{ // first marking
					GameObject znacznik = Consts.CreateMarking(red);
					Profiles.Add(new List<GameObject>() { znacznik }); //add first marking to list
					SwitchTextStatus(ProfileState.first_clicked);
				}
				else if (state == ProfileState.first_clicked)
				{ // second marking 
					Vector3[] RemainingPos = GetRemainingPoints(Profiles.Last().Last().transform.position, Highlight.pos);
					foreach (var pos in RemainingPos)
						Profiles.Last().Add(Consts.CreateMarking(white, pos));

					SwitchTextStatus(ProfileState.profileApplied);
				}

			}
		}
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
		if (Profiles.Count == 0)
			return true;

		return Profiles.Last().Count == Profiles[0].Count ? true : false;
	}
	public void ApplyProfile()
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
					UndoBuffer.Add(for_buffer, Consts.IndexToPos(idx));
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		//Search for any tiles
		var surr = Build.Get_surrounding_tiles(indexes);
		Build.UpdateTiles(surr);
		UndoBuffer.next_operation = true;
		RemovePreview(false);
	}

	public void RemovePreview(bool ClearPts = true)
	{
		if (RoadMesh)
		{
			Destroy(RoadMesh);
			RoadMesh = null;
		}
		if (ClearPts)
			ClearPtsLists();
		SwitchTextStatus(ProfileState.idle);
	}
	public void ClearPtsLists()
	{
		foreach (var ProfileList in Profiles)
		{
			for (int i = 0; i < ProfileList.Count; i++)
				Destroy(ProfileList[i]);
		}
		Profiles.Clear();
	}
	public void GeneratePreview()
	{
		if (Profiles.Count < 3 || Profiles[0].Count < 2)
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
				verts.AddRange(path.localPoints);
			else
			{
				for (int i = 0; i < mostvertices; i++)
					verts.Add(path.GetPointAtTime(i / (mostvertices - 1f), EndOfPathInstruction.Stop));
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

		SwitchTextStatus(ProfileState.preview_visible);
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
