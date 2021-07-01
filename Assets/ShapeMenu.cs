using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public enum SelectionState { NOSELECTION, VERTICES_EMERGED, SELECTING_VERTICES, SELECTING_NOW, WAITING4LD, BL_SELECTED }
public enum FormButton { none, linear, integral, jump, jumpend, flatter, to_slider, copy, infinity }
/// <summary>
/// Hooked to ShapeMenu
/// </summary>
public class ShapeMenu : MonoBehaviour
{
	public Camera mainCamera;
	public GameObject FormPanel;
	/// <summary>
	/// child - formMenu with all the below listed buttons
	/// </summary>
	public GameObject FormMenu;


	public Material transp;
	public Material red;
	public Material white;
	public Button toslider;
	public Button Flatten;
	public Button Jumper;
	public Button Prostry;
	public Button JumperEnd;
	public Button Integral;
	public Button Infinity;
	// modifiers
	public Toggle KeepShape;
	public Toggle Connect;
	/// <summary>
	/// Indicates type of selected shape in FormMenu 
	/// </summary>
	public static FormButton LastSelected = FormButton.none;
	public static SelectionState selectionState = SelectionState.NOSELECTION;
	int index = 0;
	public static List<GameObject> surroundings = new List<GameObject>();
	/// <summary>
	/// list of selected tiles
	/// </summary>
	public static List<GameObject> selected_tiles = new List<GameObject>();
	public static List<GameObject> markings = new List<GameObject>();
	/// <summary>
	/// Bottom Left pointing vector set in waiting4LD state
	/// </summary>
	public static Vector3 BL;
	private Vector3 mousePosition1;
	void Start()
	{
		toslider.onClick.AddListener(() => LastSelected = FormButton.to_slider);
		Prostry.onClick.AddListener(() => LastSelected = FormButton.linear);
		Integral.onClick.AddListener(() => LastSelected = FormButton.integral);
		Jumper.onClick.AddListener(() => LastSelected = FormButton.jump);
		JumperEnd.onClick.AddListener(() => LastSelected = FormButton.jumpend);
		Flatten.onClick.AddListener(() => LastSelected = FormButton.flatter);
		Infinity.onClick.AddListener(() => LastSelected = FormButton.infinity);
	}
	void CheckNumericShortuts()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
			LastSelected = FormButton.jump;
		else if (Input.GetKeyDown(KeyCode.Alpha2))
			LastSelected = FormButton.linear;
		else if (Input.GetKeyDown(KeyCode.Alpha3))
			LastSelected = FormButton.jumpend;
		else if (Input.GetKeyDown(KeyCode.Alpha4))
			LastSelected = FormButton.integral;
		else if (Input.GetKeyDown(KeyCode.Alpha5))
			LastSelected = FormButton.to_slider;
		else if (Input.GetKeyDown(KeyCode.Alpha6))
			LastSelected = FormButton.flatter;
		else if (Input.GetKeyDown(KeyCode.F1))
			KeepShape.isOn = !KeepShape.isOn;
		else if (Input.GetKeyDown(KeyCode.F2))
			Connect.isOn = !Connect.isOn;
	}
	public void OnDisable()
	{
		StateSwitch(SelectionState.NOSELECTION);
	}
	private void Update()
	{
		EnsureModifiersNAND();
		if (!CopyPaste.isEnabled())
		{
			CheckNumericShortuts();
			if (!MouseInputUIBlocker.BlockedByUI)
			{
				if (Input.GetMouseButtonUp(0))
				{
					UpdateCurrent();
					SpawnVertexBoxes(Input.GetKey(KeyCode.Q));
				}
				VerticesVisibleState(); // wait for left ctrl release
				VertexSelectionState(); //selecting vertices state
				Waiting4_Bottom_Left_state(); //  waiting for bottom left vertex state
				SelectShape(); // apply selected shape
			}
		}

	}
	void VerticesVisibleState()
	{

		if (selectionState == SelectionState.VERTICES_EMERGED)
			if (!Input.GetKey(KeyCode.LeftControl))
			{
				StateSwitch(SelectionState.SELECTING_VERTICES);
			}
				
	}
	void EnsureModifiersNAND()
	{
		if (Connect.isOn && KeepShape.isOn == Connect.isOn)
		{
			Connect.isOn = false;
			KeepShape.isOn = false;
		}
	}
	void UpdateCurrent()
	{
		if (selectionState == SelectionState.NOSELECTION || selectionState == SelectionState.VERTICES_EMERGED)
		{
			if (Physics.Raycast(new Vector3(Highlight.pos.x, Service.maxHeight, Highlight.pos.z),
				Vector3.down, out RaycastHit hit, Service.rayHeight, 1 << 9)
				&& hit.transform.gameObject.layer == 9
				&& !selected_tiles.Contains(hit.transform.gameObject))
			{
				selected_tiles.Add(hit.transform.gameObject);
			}
		}

	}
	public bool IsAnyZnacznikMarked()
	{
		return markings.Any(marking => marking.name == "on");
		//foreach (var z in markings)
		//{
		//	if (z.name == "on")
		//		return true;
		//}
		//return false;
	}
	/// <summary>
	/// a) Given list of map colliders => \-/ MapCollider \-/ vertex of that mc update its position (using former_heights)
	/// b) Given list of znaczniki(tags) => List of indexes of vertices, \-/ tag save its position to list, then run overload using list of int 
	/// </summary>

	float FindHighestY(List<GameObject> znaczniki)
	{
		float highest = -20;
		foreach (GameObject znacznik in znaczniki)
			if (znacznik.name == "on" && highest < znacznik.transform.position.y)
				highest = znacznik.transform.position.y;
		return highest;
	}
	void VertexSelectionState()
	{
		if (selectionState == SelectionState.SELECTING_VERTICES || selectionState == SelectionState.SELECTING_NOW)
		{
			if (Input.GetMouseButtonDown(1) || Input.GetKey(KeyCode.Escape))
			{ // RMB  or ESC turns formMenu off
				StateSwitch(SelectionState.NOSELECTION);
			}
			else
				MarkVertices();
		}
	}

	void SelectShape()
	{
		if (selectionState == SelectionState.BL_SELECTED)
		{
			// only ShapeMenu class handles selecting BL vertex which is essential for setting up vertices' orientation
			if (LastSelected == FormButton.copy)
				FormPanel.GetComponent<Form>().ShapeMenu.GetComponent<CopyPaste>().CopySelectionToClipboard();
			else
			{
				ApplyFormingFunction();
				StateSwitch(SelectionState.SELECTING_VERTICES);
			}
			LastSelected = FormButton.none;
		}
	}


	private void Waiting4_Bottom_Left_state()
	{
		if (selectionState == SelectionState.WAITING4LD)
		{
			// to slider and infinity buttons don't require BL selection
			if (LastSelected == FormButton.to_slider || LastSelected == FormButton.infinity)
				StateSwitch(SelectionState.BL_SELECTED);

			foreach (GameObject mrk in markings)
			{
				mrk.GetComponent<BoxCollider>().enabled = true;
				mrk.layer = 11;
			}

			if (Input.GetMouseButtonUp(0)) //First click
			{
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Service.maxHeight, Highlight.pos.z), Vector3.down, out RaycastHit hit, Service.rayHeight, 1 << 11) && hit.transform.gameObject.name == "on")
				{
					BL = Vector3Int.RoundToInt(hit.point);
					BL.y = hit.transform.gameObject.transform.position.y;
					StateSwitch(SelectionState.BL_SELECTED);
				}
			}
			else if (Input.GetMouseButtonDown(1)) // Cancelling selection
			{
				StateSwitch(SelectionState.SELECTING_VERTICES);
				LastSelected = FormButton.none;
			}

		}
	}
	/// <summary>
	/// TO SLIDER button logic
	/// </summary>
	void FormMenu_toSlider()
	{
		surroundings = Build.Get_surrounding_tiles(markings);
		float elevateby = 0;
		float slider_realheight = Service.SliderValue2RealHeight(FormPanel.GetComponent<Form>().HeightSlider.value);
		//Update terrain
		List<int> indexes = new List<int>();
		foreach (GameObject znacznik in markings)
		{
			if (znacznik.name == "on")
			{
				Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
				int index = Service.PosToIndex(v);
				UndoBuffer.AddZnacznik(Service.IndexToPos(index));
				indexes.Add(index);

				if (KeepShape.isOn)
				{
					elevateby = slider_realheight - markings[0].transform.position.y;
					Service.current_heights[index] += elevateby;
				}

				else
					Service.current_heights[index] = slider_realheight;

				znacznik.transform.position = new Vector3(znacznik.transform.position.x, Service.current_heights[index], znacznik.transform.position.z);
				Service.former_heights[index] = Service.current_heights[index];
			}
		}
		UndoBuffer.ApplyOperation();
		Service.UpdateMapColliders(indexes);

		Build.UpdateTiles(surroundings);
		surroundings.Clear();

	}
	/// <summary>
	/// TO Infinity button logic
	/// </summary>
	void SetToInfinity()
	{
		// NaNs grass break tiles
		surroundings = Build.Get_surrounding_tiles(markings);
		if (surroundings.Count != 0)
		{
			surroundings.Clear();
			return;
		}

		//Update terrain
		List<int> indexes = new List<int>();
		foreach (GameObject znacznik in markings)
		{
			if (znacznik.name == "on")
			{
				Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
				int index = Service.PosToIndex(v);
				UndoBuffer.AddZnacznik(Service.IndexToPos(index));
				indexes.Add(index);
				Service.current_heights[index] = float.NaN;
				Service.former_heights[index] = Service.current_heights[index];
			}
		}
		UndoBuffer.ApplyOperation();
		Service.UpdateMapColliders(indexes);
	}

	void Go2High(float low, float high, ref int x)
	{
		x = (low < high) ? x + 1 : x - 1;
	}
	/// <summary>
	/// helper function ensuring that:
	/// x goes from bottom left pos (considering rotation of selection; see: bottom-left vertex) to (upper)-right pos
	/// </summary>
	bool Ld_aims4_pg(float ld, float pg, int x)
	{
		return (ld < pg) ? x <= pg : x >= pg;
	}

	/// <summary>
	/// Returns List of vertices contains their global position (x or z, depending on bottom-left vertex) and height  
	/// </summary>
	public List<DuVec3> GetOpposingVerticesForConnect(Vector3 LD, Vector3Int PG)
	{
		List<DuVec3> Extremes = new List<DuVec3>();
		if ((LD.x < PG.x && LD.z > PG.z) || (LD.x > PG.x && LD.z < PG.z))
		{ // equal heights along Z axis ||||
			for (int z = (int)LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
			{
				Vector3 P1 = new Vector3(float.MaxValue, 0, 0);
				Vector3 P2 = new Vector3(float.MinValue, 0, 0);
				foreach (var mrk in markings)
				{
					if (mrk.name == "on" && mrk.transform.position.z == z)
					{
						if (mrk.transform.position.x < P1.x)
							P1 = mrk.transform.position;
						if (mrk.transform.position.x > P2.x)
							P2 = mrk.transform.position;
					}
				}
				if (P1.x == LD.x)
					Extremes.Add(new DuVec3(new Vector3(P1.x, P1.y, P1.z), new Vector3(P2.x, P2.y, P2.z)));
				else
					Extremes.Add(new DuVec3(new Vector3(P2.x, P2.y, P2.z), new Vector3(P1.x, P1.y, P1.z)));
			}
		}
		else
		{
			//equal heights along X axis _---
			//string going = (LD.x < PG.x && LD.z < PG.z) ? "forward" : "back";
			for (int x = (int)LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
			{
				Vector3 P1 = new Vector3(0, 0, float.MaxValue);
				Vector3 P2 = new Vector3(0, 0, float.MinValue);
				foreach (var mrk in markings)
				{
					if (mrk.name == "on" && mrk.transform.position.x == x)
					{
						if (mrk.transform.position.z < P1.z)
							P1 = mrk.transform.position;
						if (mrk.transform.position.z > P2.z)
							P2 = mrk.transform.position;
					}
				}
				if (P1.z == LD.z)
					Extremes.Add(new DuVec3(new Vector3(P1.x, P1.y, P1.z), new Vector3(P2.x, P2.y, P2.z)));
				else
					Extremes.Add(new DuVec3(new Vector3(P2.x, P2.y, P2.z), new Vector3(P1.x, P1.y, P1.z)));
			}
		}
		return Extremes;
	}
	/// <summary>
	/// Handles placing more complicated shapes.
	/// </summary>
	void ApplyFormingFunction()
	{
		RaycastHit hit;
		if (LastSelected == FormButton.to_slider)
		{
			FormMenu_toSlider();
			return;
		}
		if (LastSelected == FormButton.infinity)
		{
			SetToInfinity();
			return;
		}
		//Flatter check
		if (LastSelected == FormButton.flatter)
		{
			if (selected_tiles.Count != 1 || !IsFlatter(selected_tiles[0].name))
				return;
		}
		surroundings = Build.Get_surrounding_tiles(markings);

		if (selectionState == SelectionState.BL_SELECTED)
		{
			//We have bottom-left, now we're searching for upper-right (all relative to 'rotation' of selection)
			Vector3Int PG = new Vector3Int();
			int lowX = int.MaxValue, hiX = int.MinValue, lowZ = int.MaxValue, hiZ = int.MinValue;
			foreach (GameObject znacznik in markings)
			{
				if (znacznik.name == "on")
				{
					if (lowX > znacznik.transform.position.x)
						lowX = Mathf.RoundToInt(znacznik.transform.position.x);
					if (hiX < znacznik.transform.position.x)
						hiX = Mathf.RoundToInt(znacznik.transform.position.x);

					if (lowZ > znacznik.transform.position.z)
						lowZ = Mathf.RoundToInt(znacznik.transform.position.z);
					if (hiZ < znacznik.transform.position.z)
						hiZ = Mathf.RoundToInt(znacznik.transform.position.z);
				}
			}
			// for vertices on tiles
			if (selected_tiles.Count > 0)
			{
				if (BL.x < hiX)
				{
					if (BL.z < hiZ)
					{
						PG.Set(hiX, 0, hiZ);
					}
					else
						PG.Set(hiX, 0, lowZ);
				}
				else
				{
					if (BL.z < hiZ)
					{
						PG.Set(lowX, 0, hiZ);
					}
					else
						PG.Set(lowX, 0, lowZ);
				}
			}
			else
			{
				// for vertices of grass
				Vector3 center = new Vector3(BL.x, BL.y, BL.z);
				bool D = Physics.BoxCast(center, new Vector3(1e-3f, Service.maxHeight, 1e-3f), Vector3.back, out RaycastHit Dhit, Quaternion.identity, 1, 1 << 11);
				bool L = Physics.BoxCast(center, new Vector3(1e-3f, Service.maxHeight, 1e-3f), Vector3.left, out RaycastHit Lhit, Quaternion.identity, 1, 1 << 11);
				if (D && Dhit.transform.name != "on")
					D = false;
				if (L && Lhit.transform.name != "on")
					L = false;
				if (D)
				{
					if (L)
					{
						BL.Set(hiX, BL.y, hiZ);
						PG.Set(lowX, 0, lowZ);
					}
					else
					{
						BL.Set(lowX, BL.y, hiZ);
						PG.Set(hiX, 0, lowZ);
					}
				}
				else
				{
					if (L)
					{
						PG.Set(lowX, 0, hiZ);
						BL.Set(hiX, BL.y, lowZ);
					}
					else
					{
						PG.Set(hiX, 0, hiZ);
						BL.Set(lowX, BL.y, lowZ);
					}
				}
			}
			List<DuVec3> extremes = new List<DuVec3>();
			float slider_realheight = Service.SliderValue2RealHeight(FormPanel.GetComponent<Form>().HeightSlider.value);
			float heightdiff = slider_realheight - BL.y;
			if (Connect.isOn)
				extremes = GetOpposingVerticesForConnect(BL, PG);
			if ((BL.x < PG.x && BL.z >= PG.z) || (BL.x > PG.x && BL.z <= PG.z))
			{ // equal heights along Z axis ||||
				float steps = Mathf.Abs(BL.x - PG.x);
				int step = 0;
				if (steps != 0 && (heightdiff != 0 || LastSelected == FormButton.flatter))
				{
					for (int x = (int)BL.x; Ld_aims4_pg(BL.x, PG.x, x); Go2High(BL.x, PG.x, ref x))
					{
						for (int z = (int)BL.z; Ld_aims4_pg(BL.z, PG.z, z); Go2High(BL.z, PG.z, ref z))
						{
							// check for elements 
							if (Connect.isOn)
							{
								int ext_index = (int)Mathf.Abs(z - BL.z);
								heightdiff = extremes[ext_index].P2.y - extremes[ext_index].P1.y;
								steps = Mathf.Abs(extremes[ext_index].P1.x - extremes[ext_index].P2.x);
								slider_realheight = extremes[ext_index].P2.y;
								BL.y = extremes[ext_index].P1.y;
							}
							bool traf = Physics.Raycast(new Vector3(x, Service.maxHeight, z), Vector3.down, out hit, Service.rayHeight, 1 << 11);
							index = Service.PosToIndex(x, z);
							UndoBuffer.AddZnacznik(Service.IndexToPos(index));
							Vector3 vertpos = Service.IndexToPos(index);
							if (traf && hit.transform.gameObject.name == "on" && Service.IsWithinMapBounds(vertpos))
							{
								float old_Y = vertpos.y; // tylko do keepshape
								if (LastSelected == FormButton.linear)
									vertpos.y = BL.y + step / steps * heightdiff;
								else if (LastSelected == FormButton.integral)
									vertpos.y = BL.y + Service.Smoothstep(BL.y, slider_realheight, BL.y + step / steps * heightdiff) * heightdiff;
								else if (LastSelected == FormButton.jump)
									vertpos.y = BL.y + 2 * Service.Smoothstep(BL.y, slider_realheight, BL.y + 0.5f * step / steps * heightdiff) * heightdiff;
								else if (LastSelected == FormButton.jumpend)
									vertpos.y = BL.y + 2 * (Service.Smoothstep(BL.y, slider_realheight, BL.y + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
								else if (LastSelected == FormButton.flatter)
									vertpos.y = BL.y - TileManager.TileListInfo[selected_tiles[0].name].FlatterPoints[step];

								if (KeepShape.isOn)
									vertpos.y += old_Y - BL.y;
								if (float.IsNaN(vertpos.y))
									return;
								Service.former_heights[index] = vertpos.y;
								Service.current_heights[index] = Service.former_heights[index];
								GameObject znacznik = hit.transform.gameObject;
								znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);
							}
						}
						step += 1;
					}
				}
			}
			else
			{ // equal heights along X axis _-_-
				float steps = Mathf.Abs(BL.z - PG.z);
				//Debug.Log("steps = " + steps);
				int step = 0;
				if (steps != 0 && (heightdiff != 0 || LastSelected == FormButton.flatter))
				{
					for (int z = (int)BL.z; Ld_aims4_pg(BL.z, PG.z, z); Go2High(BL.z, PG.z, ref z))
					{
						for (int x = (int)BL.x; Ld_aims4_pg(BL.x, PG.x, x); Go2High(BL.x, PG.x, ref x))
						{
							if (Connect.isOn)
							{
								int ext_index = (int)Mathf.Abs(x - BL.x);
								heightdiff = extremes[ext_index].P2.y - extremes[ext_index].P1.y;
								steps = Mathf.Abs(extremes[ext_index].P1.z - extremes[ext_index].P2.z);
								slider_realheight = extremes[ext_index].P2.y;
								BL.y = extremes[ext_index].P1.y;
							}
							//Debug.DrawLine(new Vector3(x, Terenowanie.Service.maxHeight+1, z), new Vector3(x, -5, z), Color.green, 60);
							bool traf = Physics.Raycast(new Vector3(x, Service.maxHeight, z), Vector3.down, out hit, Service.rayHeight, 1 << 11);
							index = Service.PosToIndex(x, z);
							UndoBuffer.AddZnacznik(Service.IndexToPos(index));
							Vector3 vertpos = Service.IndexToPos(index);
							if (traf && hit.transform.gameObject.name == "on" && Service.IsWithinMapBounds(vertpos))
							{
								//Debug.DrawRay(new Vector3(x, Terenowanie.Service.maxHeight+1, z), Vector3.down, Color.blue, 40);

								float old_Y = vertpos.y; // tylko do keepshape
								if (LastSelected == FormButton.linear)
									vertpos.y = BL.y + step / steps * heightdiff;
								else if (LastSelected == FormButton.integral)
									vertpos.y = BL.y + Service.Smoothstep(BL.y, slider_realheight, BL.y + step / steps * heightdiff) * heightdiff;
								else if (LastSelected == FormButton.jump)
									vertpos.y = BL.y + 2 * Service.Smoothstep(BL.y, slider_realheight, BL.y + 0.5f * step / steps * heightdiff) * heightdiff;
								else if (LastSelected == FormButton.jumpend)
									vertpos.y = BL.y + 2 * (Service.Smoothstep(BL.y, slider_realheight, BL.y + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
								else if (LastSelected == FormButton.flatter)
									vertpos.y = BL.y - TileManager.TileListInfo[selected_tiles[0].name].FlatterPoints[step];
								if (KeepShape.isOn)
									vertpos.y += old_Y - BL.y;
								if (vertpos.y == Mathf.Infinity)
									return;
								Service.former_heights[index] = vertpos.y;
								Service.current_heights[index] = Service.former_heights[index];
								GameObject znacznik = hit.transform.gameObject;
								znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);
							}
						}
						step += 1;
					}
				}
			}
			Service.UpdateMapColliders(markings);
			//if (current != null)
			//  Build.UpdateTiles(new List<GameObject> { current });
			Build.UpdateTiles(surroundings);
			surroundings.Clear();
			UndoBuffer.ApplyOperation();
		}
	}

	private bool IsFlatter(string Name)
	{
		return TileManager.TileListInfo[Name].FlatterPoints.Length != 0 ? true : false;
	}


	public bool IsMarkingVisible(GameObject mrk)
	{
		Ray r = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(mrk.transform.position));
		if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, Service.rayHeight, 1 << 11 | 1 << 8))
		{
			float hitDistance = Vector3.Distance(hit.transform.position, mainCamera.transform.position);
			float realDistance = Vector3.Distance(mrk.transform.position, mainCamera.transform.position);
			if (hit.transform.gameObject.layer == 11 && Mathf.Abs(hitDistance - realDistance) < 0.1f)
				return true;
			else
				return false;
		}
		else
			return false;
	}
	void MarkVertices()
	{
		if (Input.GetKeyUp(KeyCode.LeftAlt))
			InverseSelection();
		if (Input.GetMouseButtonDown(0))
		{ // Beginning of selection..
			StateSwitch(SelectionState.SELECTING_NOW);
			mousePosition1 = Input.mousePosition;
		}
		if (Input.GetMouseButtonUp(0))
		{ // .. end of selection
			foreach (GameObject znacznik in markings)
			{
				if (IsWithinSelectionBounds(znacznik) && IsMarkingVisible(znacznik))
				{
					if (znacznik.GetComponent<MeshRenderer>().sharedMaterial == white)
					{
						znacznik.name = "on";
						znacznik.GetComponent<MeshRenderer>().sharedMaterial = red;
					}
					else
					{
						znacznik.name = "off";
						znacznik.GetComponent<MeshRenderer>().sharedMaterial = white;
					}

				}
			}
			StateSwitch(SelectionState.SELECTING_VERTICES);
		}
		if (selectionState == SelectionState.SELECTING_VERTICES && LastSelected != FormButton.none)
			StateSwitch(SelectionState.WAITING4LD);

		void InverseSelection()
		{
			foreach (var z in markings)
			{
				if (z.name == "on")
				{
					z.name = "Cube";
					z.GetComponent<MeshRenderer>().sharedMaterial = white;
				}
				else
				{
					z.name = "on";
					z.GetComponent<MeshRenderer>().sharedMaterial = red;
				}
			}
		}
	}

	

	void OnGUI()
	{
		if (selectionState == SelectionState.SELECTING_NOW)
		{
			// Create a rect from both mouse positions
			Rect rect = Utils.GetScreenRect(mousePosition1, Input.mousePosition);
			Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
		}
	}
	public bool IsWithinSelectionBounds(GameObject gameObject)
	{
		if (selectionState != SelectionState.SELECTING_NOW)
			return false;
		Camera camera = Camera.main;
		Bounds viewportBounds = Utils.GetViewportBounds(camera, mousePosition1, Input.mousePosition);
		return viewportBounds.Contains(camera.WorldToViewportPoint(gameObject.transform.position));
	}
	void SpawnVertexBoxes(bool checkTerrain)
	{

		if (!Highlight.over)
			return;
		if (selectionState == SelectionState.NOSELECTION || selectionState == SelectionState.VERTICES_EMERGED)
		{
			if (checkTerrain) // Working with terrain vertices
				CreateTerrainMarkings();
			else if (selected_tiles.Count == 0)
				CreateTerrainMarkings();
			else
				CreateNewSetOfMarkingsFromTile();
		}

		//local functions
		void CreateTerrainMarkings()
		{
			Vector3 v = Highlight.pos;
			for (int z = (int)(v.z - Service.MarkerBounds.y / 2); z <= v.z + Service.MarkerBounds.y / 2; z++)
			{
				for (int x = (int)(v.x - Service.MarkerBounds.x / 2); x <= v.x + Service.MarkerBounds.x / 2; x++)
				{
					if (Service.IsWithinMapBounds(x, z))
					{
						GameObject znacznik = Service.CreateMarking(white, Service.IndexToPos(Service.PosToIndex(x, z)));
						markings.Add(znacznik);
					}
				}
			}
			StateSwitch(SelectionState.SELECTING_VERTICES);
		}
		void CreateNewSetOfMarkingsFromTile() // creates markings based on all markings over rmc of area
		{
			var Last = selected_tiles[selected_tiles.Count - 1];
			Vector3 v = Last.transform.position;
			v.y = Service.maxHeight;
			var hits = Physics.RaycastAll(v, Vector3.down, Service.rayHeight, 1 << 8);
			foreach(var hit in hits)
			{
				GameObject map = hit.transform.gameObject;
				Mesh rmc = map.GetComponent<MeshFilter>().mesh;
				// if last sensitive vertex point isn't the same as last marking -> add new points
				Vector3 SomePoint = map.transform.TransformPoint(rmc.vertices[rmc.vertices.Length - 1]);

				if (markings.Count == 0 || SomePoint != markings[markings.Count - 1].transform.position)
				{
					for (int i = 0; i < rmc.vertexCount; i++)
					{
						SomePoint = map.transform.TransformPoint(rmc.vertices[i]);
						if (!Physics.Raycast(new Vector3(SomePoint.x, Service.maxHeight, SomePoint.z), Vector3.down, Service.rayHeight, 1 << 11))
							markings.Add(Service.CreateMarking(white, map.transform.TransformPoint(rmc.vertices[i])));
					}
				}
			}
			
			StateSwitch(SelectionState.VERTICES_EMERGED);
		}
		//void CreateNewSetOfMarkingsFromTile() // creates markings based on RMC (sensitive vertices)
		//{
		//	// add new points from tile
		//	var Last = selected_tiles[selected_tiles.Count - 1];
		//	Mesh rmc = Last.GetComponent<MeshFilter>().mesh;
		//	// if last sensitive vertex point isn't the same as last marking -> add new points
		//	Vector3 SomePoint = Last.transform.TransformPoint(rmc.vertices[rmc.vertices.Length - 1]);

		//	if (markings.Count == 0|| SomePoint != markings[markings.Count - 1].transform.position)
		//	{
		//		for (int i = 0; i < rmc.vertexCount; i++)
		//		{
		//			SomePoint = Last.transform.TransformPoint(rmc.vertices[i]);
		//			if (!Physics.Raycast(new Vector3(SomePoint.x, Service.maxHeight, SomePoint.z), Vector3.down, Service.rayHeight, 1 << 11))
		//				markings.Add(Service.CreateMarking(white, Last.transform.TransformPoint(rmc.vertices[i])));
		//		}
		//	}
		//	StateSwitch(SelectionState.VERTICES_EMERGED);
		//}
	}

	public void Del_znaczniki()
	{
		if (markings.Count != 0)
		{
			foreach (var mrk in markings)
				Destroy(mrk);
			markings.Clear();
			selected_tiles.Clear();
		}

	}
	/// <summary>
	/// internal => visible also for every script attached to ShapeMenu
	/// </summary>
	internal void StateSwitch(SelectionState newstate)
	{
		selectionState = newstate;
		if (newstate == SelectionState.NOSELECTION)
		{
			Del_znaczniki();
			FormMenu.SetActive(false);
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Shape forming");
		}
		else if (newstate == SelectionState.VERTICES_EMERGED)
		{
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Tiles selection");
		}
		else if (newstate == SelectionState.SELECTING_VERTICES)
		{
			FormMenu.SetActive(true);
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Marking vertices");
		}
		else if (newstate == SelectionState.WAITING4LD)
		{
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Waiting for bottom-left vertex..");
		}
		else if (newstate == SelectionState.BL_SELECTED)
		{
		}
	}
}
