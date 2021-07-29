using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum SelectionState { NOSELECTION, VERTICES_EMERGED, SELECTING_VERTICES, SELECTING_NOW, WAITING4LD, WAITING4TR, POINT_SELECTED }
public enum FormButton { none, linear, integral, jump, jumpend, flatter, to_slider, copy, infinity,amplify,}
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
	public Button AmplifyButton;
	// modifiers
	public Toggle KeepShape;
	public Toggle Connect;
	public Toggle SelectTR;
	/// <summary>
	/// Indicates type of selected shape in FormMenu 
	/// </summary>
	public static FormButton LastSelected = FormButton.none;
	public static SelectionState selectionState = SelectionState.NOSELECTION;
	int index = 0;
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
	private Vector3Int TR;
	private Vector3Int TRcut;

	void Start()
	{
		toslider.onClick.AddListener(() => LastSelected = FormButton.to_slider);
		Prostry.onClick.AddListener(() => LastSelected = FormButton.linear);
		Integral.onClick.AddListener(() => LastSelected = FormButton.integral);
		Jumper.onClick.AddListener(() => LastSelected = FormButton.jump);
		JumperEnd.onClick.AddListener(() => LastSelected = FormButton.jumpend);
		Flatten.onClick.AddListener(() => LastSelected = FormButton.flatter);
		Infinity.onClick.AddListener(() => LastSelected = FormButton.infinity);
		AmplifyButton.onClick.AddListener(() => LastSelected = FormButton.amplify);
	}
	void CheckNumericShortcuts()
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
		else if (Input.GetKeyDown(KeyCode.K))
			LastSelected = FormButton.amplify;
		else if (Input.GetKeyDown(KeyCode.F1))
			KeepShape.isOn = !KeepShape.isOn;
		else if (Input.GetKeyDown(KeyCode.F2))
			Connect.isOn = !Connect.isOn;
		else if (Input.GetKeyDown(KeyCode.F3))
			SelectTR.isOn = !SelectTR.isOn;
	}
	public void OnDisable()
	{
		StateSwitch(SelectionState.NOSELECTION);
	}
	private void Update()
	{
		EnsureModifiersNAND();
		if (!CopyPaste.IsEnabled())
		{
			CheckNumericShortcuts();
			if (!MouseInputUIBlocker.BlockedByUI)
			{
				if (Input.GetMouseButtonUp(0))
				{
					UpdateCurrent();
					SpawnVertexBoxes(Input.GetKey(KeyCode.Q), Input.GetKey(KeyCode.LeftShift));
				}
				VerticesVisibleState(); // wait for left ctrl release
				VertexSelectionState(); 
				Waiting4_Top_Right_state();
				Waiting4_Bottom_Left_state();
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
		else if (Connect.isOn && SelectTR.isOn == Connect.isOn)
		{
			Connect.isOn = false;
			KeepShape.isOn = false;
		}
	}
	public static Vector3 V(Vector3 v)
	{
		return new Vector3(v.x, Consts.MAX_H, v.z);
	}
	void UpdateCurrent()
	{
		if (selectionState == SelectionState.NOSELECTION || selectionState == SelectionState.VERTICES_EMERGED)
		{
			RaycastHit hit;
			if (Physics.Raycast(V(Highlight.pos_float), Vector3.down, out hit, Consts.RAY_H, 1 << 9)
				|| Physics.Raycast(V(Highlight.pos_float), Vector3.down, out hit, Consts.RAY_H, 1 << 8))
			{
					if (!selected_tiles.Contains(hit.transform.gameObject))
						selected_tiles.Add(hit.transform.gameObject);
			}
		}
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
		if (selectionState == SelectionState.POINT_SELECTED)
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
			if (LastSelected == FormButton.infinity || LastSelected == FormButton.to_slider)
				StateSwitch(SelectionState.POINT_SELECTED);

			foreach (GameObject mrk in markings)
			{
				mrk.GetComponent<BoxCollider>().enabled = true;
				mrk.layer = 11;
			}

			if (Input.GetMouseButtonUp(0)) //First click
			{
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11) && hit.transform.gameObject.name == "on")
				{
					BL = Vector3Int.RoundToInt(hit.point);
					BL.y = hit.transform.gameObject.transform.position.y;
					if (SelectTR.isOn)
						StateSwitch(SelectionState.WAITING4TR);
					else
						StateSwitch(SelectionState.POINT_SELECTED);
				}
			}
			else if (Input.GetMouseButtonDown(1)) // Cancelling selection
			{
				StateSwitch(SelectionState.SELECTING_VERTICES);
				LastSelected = FormButton.none;
			}

		}
	}
	
	private void Waiting4_Top_Right_state()
	{
		if (selectionState == SelectionState.WAITING4TR)
		{
			if (LastSelected == FormButton.amplify || LastSelected == FormButton.flatter || LastSelected == FormButton.copy)
				StateSwitch(SelectionState.POINT_SELECTED);

			if (Input.GetMouseButtonUp(0))
			{
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11))
				{
					TRcut = Vector3Int.RoundToInt(Highlight.pos);
					StateSwitch(SelectionState.POINT_SELECTED);
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
		var surroundings = Build.Get_surrounding_tiles(markings);
		
		float slider_value = Consts.SliderValue2RealHeight(FormPanel.GetComponent<Form>().HeightSlider.value);
		//Update terrain
		HashSet<int> indexes = new HashSet<int>();
		foreach (GameObject znacznik in markings)
		{
			if (znacznik.name == "on")
			{
				Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
				int index = Consts.PosToIndex(v);
				UndoBuffer.Add(Consts.IndexToPos(index));
				indexes.Add(index);

				if (KeepShape.isOn)
				{
					Consts.current_heights[index] += slider_value;
				}
				else
					Consts.current_heights[index] = slider_value;

				znacznik.transform.position = new Vector3(znacznik.transform.position.x, Consts.current_heights[index], znacznik.transform.position.z);
				Consts.former_heights[index] = Consts.current_heights[index];
			}
		}
		UndoBuffer.ApplyOperation();
		Consts.UpdateMapColliders(indexes);

		Build.UpdateTiles(surroundings);
		surroundings.Clear();

	}

	/// <summary>
	/// Handles placing more complicated shapes.
	/// </summary>
	void ApplyFormingFunction()
	{
		if (LastSelected == FormButton.amplify)
		{
			Amplify();
			return;
		}
		if (LastSelected == FormButton.infinity)
		{
			SetToInfinity();
			return;
		}
		if (LastSelected == FormButton.to_slider)
		{
			FormMenu_toSlider();
			return;
		}
		//Flatter check
		if (LastSelected == FormButton.flatter)
		{
			if (selected_tiles.Count != 1 || !IsFlatter(selected_tiles[0].name))
				return;
		}

		float slider_realheight = Consts.SliderValue2RealHeight(FormPanel.GetComponent<Form>().HeightSlider.value);
		float heightdiff = slider_realheight - BL.y;

		if (heightdiff == 0 && !Connect.isOn)
			return;

		var surroundings = Build.Get_surrounding_tiles(markings);

		if (selectionState == SelectionState.POINT_SELECTED)
		{
			Set_rotated_BL_and_TR();
			List<DuVec3> extremes = new List<DuVec3>();
			if (Connect.isOn)
				extremes = GetOpposingVerticesForConnect(BL, TR);
			if ((BL.x < TR.x && BL.z >= TR.z) || (BL.x > TR.x && BL.z <= TR.z))
			{ // equal heights along Z axis ||||
				int steps = (int)Mathf.Abs(BL.x - TR.x);
				int step = 0;
				if (steps != 0 && (heightdiff != 0 || LastSelected == FormButton.flatter || Connect.isOn))
				{
					for (int x = (int)BL.x; BL_aims4_TR(BL.x, TR.x, x); Go2High(BL.x, TR.x, ref x))
					{
						for (int z = (int)BL.z; BL_aims4_TR(BL.z, TR.z, z); Go2High(BL.z, TR.z, ref z))
						{		
							// check for elements 
							if (Connect.isOn)
							{
								int ext_index = (int)Mathf.Abs(z - BL.z);
								heightdiff = extremes[ext_index].P2.y - extremes[ext_index].P1.y;
								steps = (int)Mathf.Abs(extremes[ext_index].P1.x - extremes[ext_index].P2.x);
								slider_realheight = extremes[ext_index].P2.y;
								BL.y = extremes[ext_index].P1.y;
							}
							SetMarkingPos(x, z, step, steps, slider_realheight, heightdiff, ref extremes);
							if (SelectTR.isOn && TRcut.x == x && TRcut.z == z)
								goto endloop;
						}
						step += 1;
					}
				}
			}
			else
			{ // equal heights along X axis _-_-
				int steps = (int)Mathf.Abs(BL.z - TR.z);
				//Debug.Log("steps = " + steps);
				int step = 0;
				if (steps != 0 && (heightdiff != 0 || LastSelected == FormButton.flatter || Connect.isOn))
				{
					for (int z = (int)BL.z; BL_aims4_TR(BL.z, TR.z, z); Go2High(BL.z, TR.z, ref z))
					{
						for (int x = (int)BL.x; BL_aims4_TR(BL.x, TR.x, x); Go2High(BL.x, TR.x, ref x))
						{
							// check for elements 
							if (Connect.isOn)
							{
								int ext_index = (int)Mathf.Abs(x - BL.x);
								heightdiff = extremes[ext_index].P2.y - extremes[ext_index].P1.y;
								steps = (int)Mathf.Abs(extremes[ext_index].P1.z - extremes[ext_index].P2.z);
								slider_realheight = extremes[ext_index].P2.y;
								BL.y = extremes[ext_index].P1.y;
							}
							SetMarkingPos(x, z, step, steps, slider_realheight, heightdiff, ref extremes);
							if (SelectTR.isOn && TRcut.x == x && TRcut.z == z)
								goto endloop;
						}
						step += 1;
					}
				}
			}

			endloop:
			Consts.UpdateMapColliders(markings);
			//if (current != null)
			//  Build.UpdateTiles(new List<GameObject> { current });
			Build.UpdateTiles(surroundings);
			surroundings.Clear();
			UndoBuffer.ApplyOperation();
		}

		// LOCAL FUNCTIONS
		void Go2High(float low, float high, ref int x)
		{
			x = (low < high) ? x + 1 : x - 1;
		}
		/// <summary>
		/// helper function ensuring that:
		/// x goes from bottom left pos (considering rotation of selection; see: bottom-left vertex) to (upper)-right pos
		/// </summary>
		bool BL_aims4_TR(float ld, float pg, int x)
		{
			return (ld < pg) ? x <= pg : x >= pg;
		}
		/// <summary>
		/// Returns List of vertices contains their global position (x or z, depending on bottom-left vertex) and height  
		/// </summary>
		List<DuVec3> GetOpposingVerticesForConnect(Vector3 LD, Vector3Int PG)
		{
			List<DuVec3> Extremes = new List<DuVec3>();
			if ((LD.x < PG.x && LD.z > PG.z) || (LD.x > PG.x && LD.z < PG.z))
			{ // equal heights along Z axis ||||
				for (int z = (int)LD.z; BL_aims4_TR(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
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
						Extremes.Add(new DuVec3(P1, P2));
					else
						Extremes.Add(new DuVec3(P2, P1));
				}
			}
			else
			{
				//equal heights along X axis _---
				//string going = (LD.x < PG.x && LD.z < PG.z) ? "forward" : "back";
				for (int x = (int)LD.x; BL_aims4_TR(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
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
						Extremes.Add(new DuVec3(P1, P2));
					else
						Extremes.Add(new DuVec3(P2, P1));
				}
			}
			return Extremes;
		}
		
	}
	void Amplify()
	{
		var surroundings = Build.Get_surrounding_tiles(markings);
		float slider_value = FormPanel.GetComponent<Form>().HeightSlider.value;
		//Update terrain
		HashSet<int> indexes = new HashSet<int>();
		foreach (GameObject marking in markings)
		{
			if (marking.name == "on")
			{
				Vector3Int v = Vector3Int.RoundToInt(marking.transform.position);
				int index = Consts.PosToIndex(v);
				UndoBuffer.Add(Consts.IndexToPos(index));
				indexes.Add(index);

				Consts.current_heights[index] = BL.y + slider_value * (Consts.current_heights[index] - BL.y);

				marking.transform.position = new Vector3(marking.transform.position.x, Consts.current_heights[index], marking.transform.position.z);
				Consts.former_heights[index] = Consts.current_heights[index];
			}
		}
		UndoBuffer.ApplyOperation();
		Consts.UpdateMapColliders(indexes);
		Build.UpdateTiles(surroundings);
	}
	/// <summary>
	/// TO Infinity button logic
	/// </summary>
	void SetToInfinity()
	{
		// NaNs grass break tiles
		var surroundings = Build.Get_surrounding_tiles(markings);
		if (surroundings.Count != 0)
		{
			surroundings.Clear();
			return;
		}

		//Update terrain
		HashSet<int> indexes = new HashSet<int>();
		foreach (GameObject znacznik in markings)
		{
			if (znacznik.name == "on")
			{
				Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
				int index = Consts.PosToIndex(v);
				UndoBuffer.Add(Consts.IndexToPos(index));
				indexes.Add(index);
				Consts.current_heights[index] = float.NaN;
				Consts.former_heights[index] = Consts.current_heights[index];
			}
		}
		UndoBuffer.ApplyOperation();
		Consts.UpdateMapColliders(indexes);
	}
	void Set_rotated_BL_and_TR()
	{
		//We have bottom-left, now we're searching for upper-right (all relative to 'rotation' of selection)
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
					TR.Set(hiX, 0, hiZ);
				}
				else
					TR.Set(hiX, 0, lowZ);
			}
			else
			{
				if (BL.z < hiZ)
				{
					TR.Set(lowX, 0, hiZ);
				}
				else
					TR.Set(lowX, 0, lowZ);
			}
		}
		else
		{
			// for vertices of grass
			Vector3 center = new Vector3(BL.x, BL.y, BL.z);
			bool D = Physics.BoxCast(center, new Vector3(1e-3f, Consts.MAX_H, 1e-3f), Vector3.back, out RaycastHit Dhit, Quaternion.identity, 1, 1 << 11);
			bool L = Physics.BoxCast(center, new Vector3(1e-3f, Consts.MAX_H, 1e-3f), Vector3.left, out RaycastHit Lhit, Quaternion.identity, 1, 1 << 11);
			if (D && Dhit.transform.name != "on")
				D = false;
			if (L && Lhit.transform.name != "on")
				L = false;
			if (D)
			{
				if (L)
				{
					BL.Set(hiX, BL.y, hiZ);
					TR.Set(lowX, 0, lowZ);
				}
				else
				{
					BL.Set(lowX, BL.y, hiZ);
					TR.Set(hiX, 0, lowZ);
				}
			}
			else
			{
				if (L)
				{
					TR.Set(lowX, 0, hiZ);
					BL.Set(hiX, BL.y, lowZ);
				}
				else
				{
					TR.Set(hiX, 0, hiZ);
					BL.Set(lowX, BL.y, lowZ);
				}
			}
		}
	}

	void SetMarkingPos(int x, int z, int step, int steps, float slider_realheight, float heightdiff, ref List<DuVec3> extremes)
	{
		bool traf = Physics.Raycast(new Vector3(x, Consts.MAX_H, z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11);
		if (traf && hit.transform.gameObject.name == "on" && Consts.IsWithinMapBounds(x, z))
		{

			index = Consts.PosToIndex(x, z);
 
			UndoBuffer.Add(Consts.IndexToPos(index));
			float old_Y = Consts.current_heights[index]; // tylko do keepshape
			float Y = old_Y;
			if (LastSelected == FormButton.linear)
				Y = BL.y + (float)step / steps * heightdiff;
			else if (LastSelected == FormButton.integral)
			{
				if (2 * step <= steps)
				{
					Y = BL.y + step * (step + 1) * heightdiff / (Mathf.Ceil(steps / 2f) * (Mathf.Ceil(steps / 2f) + 1)
					+ Mathf.Floor(steps / 2f) * (Mathf.Floor(steps / 2f) + 1));
				}
				else
				{
					Y = slider_realheight - (steps - step) * (steps - step + 1) * heightdiff / (Mathf.Ceil(steps / 2f) * (Mathf.Ceil(steps / 2f) + 1)
					+ Mathf.Floor(steps / 2f) * (Mathf.Floor(steps / 2f) + 1));
				}
			}
			else if (LastSelected == FormButton.jump)
				Y = BL.y + step * (step + 1) * heightdiff / (steps * (steps + 1));//Y = BL.y + 2 * Consts.Smoothstep(BL.y, slider_realheight, BL.y + 0.5f * step / steps * heightdiff) * heightdiff;
			else if (LastSelected == FormButton.jumpend)
				Y = slider_realheight - (steps - step) * (steps - step + 1) * heightdiff / (steps * (steps + 1));//Y = BL.y + 2 * (Consts.Smoothstep(BL.y, slider_realheight, BL.y + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
			else if (LastSelected == FormButton.flatter)
				Y = BL.y - TileManager.TileListInfo[selected_tiles[0].name].FlatterPoints[step];
			if (KeepShape.isOn)
				Y += old_Y - BL.y;
			if (float.IsNaN(Y))
				return;
			Consts.former_heights[index] = Y;
			Consts.current_heights[index] = Consts.former_heights[index];
			GameObject znacznik = hit.transform.gameObject;
			znacznik.transform.position = new Vector3(znacznik.transform.position.x, Y, znacznik.transform.position.z);
		}
	}
	private bool IsFlatter(string Name)
	{
		return TileManager.TileListInfo[Name].FlatterPoints.Length != 0;
	}
	public bool IsMarkingVisible(GameObject mrk)
	{
		Ray r = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(mrk.transform.position));
		if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, Consts.RAY_H, 1 << 11 | 1 << 8))
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
		if (Input.GetKeyUp(KeyCode.E))
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
	void SpawnVertexBoxes(bool checkTerrain, bool ForceMapVertices)
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
				CreateNewSetOfMarkingsFromTile(ForceMapVertices);
		}

		//local functions
		void CreateTerrainMarkings()
		{
			Vector3 v = Highlight.pos;
			for (int z = (int)(v.z - Consts.MarkerBounds.y / 2); z <= v.z + Consts.MarkerBounds.y / 2; z++)
			{
				for (int x = (int)(v.x - Consts.MarkerBounds.x / 2); x <= v.x + Consts.MarkerBounds.x / 2; x++)
				{
					if (Consts.IsWithinMapBounds(x, z))
					{
						GameObject znacznik = Consts.CreateMarking(white, Consts.IndexToPos(Consts.PosToIndex(x, z)));
						markings.Add(znacznik);
					}
				}
			}
			StateSwitch(SelectionState.SELECTING_VERTICES);
		}
		void CreateNewSetOfMarkingsFromTile(bool ForceMapVerticesForTile = false) // creates markings based on all markings over rmc of area
		{
			if (ForceMapVerticesForTile)
			{ // force map vertices for this tile
				var Last = selected_tiles[selected_tiles.Count - 1];
				Vector3 v = Last.transform.position;
				v.y = Consts.MAX_H;
				var corresponding_grasses = Physics.RaycastAll(v, Vector3.down, Consts.RAY_H, 1 << 8);
				foreach (var grass in corresponding_grasses)
				{
					GameObject grass_tile = grass.transform.gameObject;
					Mesh rmc = grass_tile.GetComponent<MeshFilter>().mesh;

					for (int i = 0; i < rmc.vertexCount; i++)
					{
						v = grass_tile.transform.TransformPoint(rmc.vertices[i]);
						if (!Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, Consts.RAY_H, 1 << 11))
							markings.Add(Consts.CreateMarking(white, grass_tile.transform.TransformPoint(rmc.vertices[i])));
					}
				}
			}
			else
			{ // normal tile selection
				var Last = selected_tiles[selected_tiles.Count - 1];
				Mesh rmc = Last.GetComponent<MeshFilter>().mesh;
				// add new points
				for (int i = 0; i < rmc.vertexCount; i++)
				{
					Vector3 v = Last.transform.TransformPoint(rmc.vertices[i]);
					if (!Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, Consts.RAY_H, 1 << 11))
						markings.Add(Consts.CreateMarking(white, Last.transform.TransformPoint(rmc.vertices[i])));
				}
				
			}
			StateSwitch(SelectionState.VERTICES_EMERGED);
		}
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
			if(LastSelected == FormButton.amplify)
				FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Select h0 vertex ..");
			else
				FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Waiting for bottom-left vertex..");
		}
		else if (newstate == SelectionState.WAITING4TR)
		{
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Waiting for top-right vertex..");
		}
		else if (newstate == SelectionState.POINT_SELECTED)
		{
		}
	}
}
