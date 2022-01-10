using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DuVec3
{
	public Vector3 P1;
	public Vector3 P2;
	public DuVec3(Vector3 p1, Vector3 p2)
	{
		P1 = p1;
		P2 = p2;
	}
}
public enum SelectionState { NOSELECTION, SELECTING_VERTICES, MARKING_VERTICES, LMB_DOWN, WAITING4BL, WAITING4TR, SETTING_PARAMETERS, POINT_SELECTED }
public enum FormButton { none, linear, integral, jump, jumpend, flatter, to_slider, rounded, copy, infinity, amplify }
public enum MarkingPattern { rect, triangle, triangle_inv, diagonal }
/// <summary>
/// Hooked to ShapeMenu
/// </summary>
public class ShapeMenu : MonoBehaviour
{
	public Camera mainCamera;
	public GameObject FormPanel;
	public FormSlider formSlider;
	public Slider HeightSlider;
	public CopyPaste copyPaste;
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
	public Button Rounded;
	public Button Infinity;
	public Button AmplifyButton;
	public InputField Bounds_x;
	public InputField Bounds_y;
	// modifiers
	public Toggle KeepShape;
	public Toggle Connect;
	public Toggle SelectTR;
	public Toggle Symmetric;
	public Toggle Inversion_mode;
	public Toggle Addition_mode;
	public Toggle Exclusion_mode;
	/// <summary>
	/// Indicates type of selected shape in FormMenu 
	/// </summary>
	public static FormButton LastSelected = FormButton.none;
	public static SelectionState selectionState = SelectionState.NOSELECTION;
	public static MarkingPattern CurrentPattern = MarkingPattern.rect;
	/// <summary>
	/// list of selected tiles
	/// </summary>
	public static List<GameObject> selected_tiles = new List<GameObject>();
	public static Dictionary<int, GameObject> markings = new Dictionary<int, GameObject>();
	/// <summary>
	///Whether the user is currently selecting vertices with Q
	/// </summary>
	private static bool areal_selection = false;
	/// <summary>
	/// Bottom Left pointing vector set in waiting4BL state
	/// </summary>
	public static Vector3 BL;
	private Vector3Int TR;
	/// <summary>
	/// Half the number of steps when with symmetric modifier
	/// </summary>
	private int slope_length;
	private float slider_realheight;
	private int start_rounding = -1;
	private int end_rounding = -1;
	/// <summary>
	/// Where the user has pressed LMB in LMB_DOWN state
	/// </summary>
	private Vector3Int p1;
	/// <summary>
	/// The current position of the cursor in LMB_DOWN state
	/// </summary
	private Vector3Int p2;
	/// <summary>
	/// Former state of markings before being marked/unmarked
	/// </summary>
	private static Dictionary<int, string> BeforeMarking = new Dictionary<int, string>();
	struct ShapeShortcuts
	{
		public static Dictionary<KeyCode, FormButton> ShapeTypes { get; set; }
		public static Dictionary<KeyCode, Toggle> Modifiers { get; set; }
	}

	void Start()
	{
		AmplifyButton.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.amplify  ); });
		    JumperEnd.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.jumpend  ); });
		     Infinity.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.infinity ); });
		     Integral.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.integral ); });
		     toslider.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.to_slider); });
		      Prostry.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.linear   ); });
		      Rounded.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.rounded  ); });
		      Flatten.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.flatter  ); });
		       Jumper.onClick.AddListener(delegate { ShapeTypeSwitch(FormButton.jump     ); });

		ShapeShortcuts.ShapeTypes = new Dictionary<KeyCode, FormButton>
		{
			{ KeyCode.Alpha1, FormButton.to_slider },
			{ KeyCode.Alpha2, FormButton.linear    },
			{ KeyCode.Alpha3, FormButton.jump      },
			{ KeyCode.Alpha4, FormButton.jumpend   },
			{ KeyCode.Alpha5, FormButton.integral  },
			{ KeyCode.Alpha6, FormButton.flatter   },
			{ KeyCode.Alpha7, FormButton.rounded   },
			{ KeyCode.K,      FormButton.amplify   }
		};
		ShapeShortcuts.Modifiers = new Dictionary<KeyCode, Toggle>
		{
			{ KeyCode.F1, KeepShape },
			{ KeyCode.F2, Connect   },
			{ KeyCode.F3, SelectTR  },
			{ KeyCode.F4, Symmetric }
		};

		Symmetric.onValueChanged.AddListener(delegate
		{
			if (Symmetric.isOn)
				slope_length /= 2;
			else
			{
				if ((BL.x < TR.x && BL.z >= TR.z) || (BL.x > TR.x && BL.z <= TR.z))
				{
					slope_length = (int)Mathf.Abs(BL.x - TR.x);
				}
				else
					slope_length = (int)Mathf.Abs(BL.z - TR.z);
			}
			if (selectionState == SelectionState.SETTING_PARAMETERS)
				StateSwitch(SelectionState.SETTING_PARAMETERS); // Update the maximum start and end rounding on the status bar
		});
	}
	void CheckNumericShortcuts()
	{
		foreach (KeyCode key in ShapeShortcuts.ShapeTypes.Keys)
			if (Input.GetKeyDown(key) && !Bounds_x.isFocused && !Bounds_y.isFocused)
			{
				ShapeTypeSwitch(ShapeShortcuts.ShapeTypes[key]);
				break;
			}
		foreach (KeyCode key in ShapeShortcuts.Modifiers.Keys)
			if (Input.GetKeyDown(key))
			{
				ShapeShortcuts.Modifiers[key].isOn = !ShapeShortcuts.Modifiers[key].isOn;
				break;
			}
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
			SelectionMarkingSwitch();
			if (selectionState == SelectionState.MARKING_VERTICES && Input.GetKeyUp(KeyCode.T))
				MarkingPatternSwitch();
			if (Input.GetKeyUp(KeyCode.R))
				MarkingModeSwitch();

			if (!MouseInputUIBlocker.BlockedByUI)
			{
				VertexSelectionState();
				VertexMarkingState();
				WaitingForParametersState();
				WaitingForTopRightState(); 
				WaitingForBottomLeftState();
				ApplyOperation();
			}
		}
	}
	void EnsureModifiersNAND()
	{
		if (Connect.isOn && KeepShape.isOn)
		{
			Connect.isOn = false;
			KeepShape.isOn = false;
		}
		else if (Connect.isOn && SelectTR.isOn)
		{
			Connect.isOn = false;
			SelectTR.isOn = false;
		}
	}
	public static Vector3 V(Vector3 v)
	{
		return new Vector3(v.x, Consts.MAX_H, v.z);
	}

	void SelectionMarkingSwitch()
	{
		if (selectionState == SelectionState.SELECTING_VERTICES && !Input.GetKey(KeyCode.LeftControl))
		{
			StateSwitch(SelectionState.MARKING_VERTICES);
		}
		else if (selectionState == SelectionState.MARKING_VERTICES && Input.GetKeyDown(KeyCode.LeftControl))
		{
			StateSwitch(SelectionState.SELECTING_VERTICES);
		}
	}
	void VertexSelectionState()
	{
		if (Input.GetMouseButtonUp(0) && Highlight.over)
		{
			if (selectionState == SelectionState.NOSELECTION)
				StateSwitch(SelectionState.SELECTING_VERTICES);
			if (selectionState == SelectionState.SELECTING_VERTICES)
			{
				if (Highlight.tile.layer == 8 && Physics.Raycast(V(Highlight.pos_float), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 9))
                {
					SpawnVertexBoxes(hit.transform.gameObject, selected_tiles.Count == 0 && Input.GetKey(KeyCode.Q), Input.GetKey(KeyCode.LeftShift));
				}
				else
					SpawnVertexBoxes(Highlight.tile, selected_tiles.Count == 0 && Input.GetKey(KeyCode.Q), Input.GetKey(KeyCode.LeftShift));
			}
		}
	}
	void VertexMarkingState()
	{
		if (selectionState == SelectionState.MARKING_VERTICES || selectionState == SelectionState.LMB_DOWN)
		{
			if (Input.GetMouseButtonDown(1) || Input.GetKey(KeyCode.Escape))
			{ // RMB  or ESC turns formMenu off
				StateSwitch(SelectionState.NOSELECTION);
			}
			else
				MarkVertices();
		}
	}

	void ApplyOperation()
	{
		if (selectionState == SelectionState.POINT_SELECTED)
		{
			// only ShapeMenu class handles selecting BL vertex which is essential for setting up vertices' orientation
			if (LastSelected == FormButton.copy)
				copyPaste.CopySelectionToClipboard();
			else
			{
				if (LastSelected == FormButton.amplify)
					Amplify();
				else if (LastSelected == FormButton.infinity)
					SetToInfinity();
				else if (LastSelected == FormButton.to_slider)
					FormMenu_toSlider();
				else
					ApplyFormingFunction();

				StateSwitch(SelectionState.MARKING_VERTICES);
			}
			LastSelected = FormButton.none;
			start_rounding = -1;
			end_rounding = -1;
		}
	}

	private void WaitingForBottomLeftState()
	{
		if (selectionState == SelectionState.WAITING4BL)
		{
			// to slider and infinity buttons don't require BL selection
			if (LastSelected == FormButton.infinity || LastSelected == FormButton.to_slider)
				StateSwitch(SelectionState.POINT_SELECTED);

			foreach (GameObject mrk in markings.Values)
			{
				mrk.GetComponent<BoxCollider>().enabled = true;
				mrk.layer = 11;
			}

			if (Input.GetMouseButtonUp(0)) //First click
			{
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11) && hit.transform.gameObject.name == "on")
				{
					BL = Vector3Int.RoundToInt(hit.point);
					BL.y = Consts.current_heights[Consts.PosToIndex(hit.point)];
					if (SelectTR.isOn)
						StateSwitch(SelectionState.WAITING4TR);
					else
					{
						SetShapeInfo(true);
						if (LastSelected == FormButton.rounded && slope_length > 1)
							StateSwitch(SelectionState.SETTING_PARAMETERS);
						else
							StateSwitch(SelectionState.POINT_SELECTED);
					}
				}
			}
			else if (Input.GetMouseButtonDown(1)) // Cancelling selection
			{
				StateSwitch(SelectionState.MARKING_VERTICES);
			}
		}
	}
	
	private void WaitingForTopRightState()
	{
		if (selectionState == SelectionState.WAITING4TR)
		{
			if (LastSelected == FormButton.amplify || LastSelected == FormButton.flatter || LastSelected == FormButton.copy)
				StateSwitch(SelectionState.POINT_SELECTED);

			if (Input.GetMouseButtonUp(0))
			{
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11))
				{
					TR = Vector3Int.RoundToInt(Highlight.pos);
					SetShapeInfo(false);
					if (LastSelected == FormButton.rounded && slope_length > 1)
						StateSwitch(SelectionState.SETTING_PARAMETERS);
					else
						StateSwitch(SelectionState.POINT_SELECTED);
				}
			}
			else if (Input.GetMouseButtonDown(1)) // Cancelling selection
			{
				StateSwitch(SelectionState.MARKING_VERTICES);
			}
		}
	}

	private void WaitingForParametersState()
	{
		if (selectionState == SelectionState.SETTING_PARAMETERS)
		{
			if (LastSelected != FormButton.rounded || slope_length < 2) // Only the manually-rounded slope requires additional parameters
				StateSwitch(SelectionState.POINT_SELECTED);

			if (Input.GetKeyUp(KeyCode.Return))
			{
				if (start_rounding == -1)
					start_rounding = (int)Mathf.Clamp(HeightSlider.value, 0, slope_length - 1);
				else
					end_rounding = (int)Mathf.Clamp(HeightSlider.value, 0, slope_length - 1 - start_rounding);

				if (start_rounding == slope_length - 1) // Skip end rounding selection if it can't be more than 0
					end_rounding = 0;

				if (end_rounding == -1)
					StateSwitch(SelectionState.SETTING_PARAMETERS);
				else
					StateSwitch(SelectionState.POINT_SELECTED);
			}

			else if (Input.GetMouseButtonDown(1)) // Cancelling selection
			{
				StateSwitch(SelectionState.MARKING_VERTICES);
			}
		}
	}

	/// <summary>
	/// TO SLIDER button logic
	/// </summary>
	void FormMenu_toSlider()
	{
		var surroundings = Build.Get_surrounding_tiles(markings.Values.ToList());
		
		float slider_value = Consts.SliderValue2RealHeight(HeightSlider.value);
		//Update terrain
		HashSet<int> indexes = new HashSet<int>();
		foreach (GameObject znacznik in markings.Values)
		{
			if (znacznik.name == "on")
			{
				int index = Consts.PosToIndex(znacznik.transform.position);
				indexes.Add(index);
				Vector3 v = Consts.IndexToPos(index);

				if (KeepShape.isOn)
				{
					Consts.current_heights[index] += slider_value;
				}
				else
				{
					Consts.current_heights[index] = slider_value;
				}

				znacznik.transform.Translate(0, Consts.current_heights[index] - v.y, 0);
				Consts.former_heights[index] = Consts.current_heights[index];
				UndoBuffer.Add(v, Consts.IndexToPos(index));
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
		if (LastSelected == FormButton.flatter)
		{
			if (selected_tiles.Count != 1 || !IsFlatter(selected_tiles[0].name))
				return;
		}
		else if (slider_realheight == BL.y && !Connect.isOn || slope_length == 0)
			return;

		start_rounding = Math.Min(start_rounding, slope_length - 1);
		end_rounding = Math.Min(end_rounding, slope_length - 1 - start_rounding);
		List<DuVec3> extremes = GetOpposingVertices();
		List<float> slope_points = new List<float>();

		if ((BL.x < TR.x && BL.z >= TR.z) || (BL.x > TR.x && BL.z <= TR.z))
		{
			for (int z = (int)BL.z; BL_aims4_TR(BL.z, TR.z, z); Go2High(BL.z, TR.z, ref z))
			{
				int extreme_index = (int)Mathf.Abs(z - BL.z);
				BL.y = extremes[extreme_index].P1.y;
				slider_realheight = extremes[extreme_index].P2.y;
				float heightdiff = extremes[extreme_index].P2.y - extremes[extreme_index].P1.y;
				int steps = (int)Mathf.Abs(BL.x - TR.x);
				int step = 0;
				if (Connect.isOn || z == (int)BL.z)
					slope_points = GetSlopePoints(heightdiff, steps);

				for (int x = (int)BL.x; BL_aims4_TR(BL.x, TR.x, x); Go2High(BL.x, TR.x, ref x))
				{
					SetMarkingPos(x, z, step, steps, heightdiff, slope_points);
					step++;
				}
			}
		}
		else
		{
			for (int x = (int)BL.x; BL_aims4_TR(BL.x, TR.x, x); Go2High(BL.x, TR.x, ref x))
			{
				int extreme_index = (int)Mathf.Abs(x - BL.x);
				BL.y = extremes[extreme_index].P1.y;
				slider_realheight = extremes[extreme_index].P2.y;
				float heightdiff = slider_realheight - BL.y;
				int steps = (int)Mathf.Abs(BL.z - TR.z);
				int step = 0;
				if (Connect.isOn || x == (int)BL.x)
					slope_points = GetSlopePoints(heightdiff, steps);

				for (int z = (int)BL.z; BL_aims4_TR(BL.z, TR.z, z); Go2High(BL.z, TR.z, ref z))
				{
					SetMarkingPos(x, z, step, steps, heightdiff, slope_points);
					step++;
				}
			}
		}
		Consts.UpdateMapColliders(markings.Values.ToList());
		var surroundings = Build.Get_surrounding_tiles(markings.Values.ToList());
		Build.UpdateTiles(surroundings);
		surroundings.Clear();
		UndoBuffer.ApplyOperation();

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
		List<DuVec3> GetOpposingVertices()
		{
			List<DuVec3> Extremes = new List<DuVec3>();
			if ((BL.x < TR.x && BL.z >= TR.z) || (BL.x > TR.x && BL.z <= TR.z))
			{   // extremes spreading along Z axis
				for (int z = (int)BL.z; BL_aims4_TR(BL.z, TR.z, z); Go2High(BL.z, TR.z, ref z))
				{
					Vector3 P1, P2;
					if (Connect.isOn)
					{
						P1 = new Vector3(BL.x, Consts.current_heights[Consts.PosToIndex((int)BL.x, z)], z);
						if (!Symmetric.isOn)
							P2 = new Vector3(TR.x, Consts.current_heights[Consts.PosToIndex(TR.x, z)], z);
						else
							P2 = new Vector3(((int)BL.x + TR.x) / 2, Consts.current_heights[Consts.PosToIndex(((int)BL.x + TR.x) / 2, z)], z);
					}
					else
                    {
						P1 = new Vector3(BL.x, BL.y, z);
						P2 = new Vector3(TR.x, slider_realheight, z);
					}
					Extremes.Add(new DuVec3(P1, P2));
				}
			}
			else
			{   // extremes spreading along X axis
				for (int x = (int)BL.x; BL_aims4_TR(BL.x, TR.x, x); Go2High(BL.x, TR.x, ref x))
				{
					Vector3 P1, P2;
					if (Connect.isOn)
					{
						P1 = new Vector3(x, Consts.current_heights[Consts.PosToIndex(x, (int)BL.z)], BL.z);
						if (!Symmetric.isOn)
							P2 = new Vector3(x, Consts.current_heights[Consts.PosToIndex(x, TR.z)], TR.z);
						else
							P2 = new Vector3(x, Consts.current_heights[Consts.PosToIndex(x, ((int)BL.z + TR.z) / 2)], ((int)BL.z + TR.z) / 2);
					}
					else
                    {
						P1 = new Vector3(x, BL.y, BL.z);
						P2 = new Vector3(x, slider_realheight, TR.z);
					}
					Extremes.Add(new DuVec3(P1, P2));
				}
			}
			return Extremes;
		}
	}
	void Amplify()
	{
		var surroundings = Build.Get_surrounding_tiles(markings.Values.ToList());
		float slider_value = HeightSlider.value;
		//Update terrain
		HashSet<int> indexes = new HashSet<int>();
		foreach (GameObject marking in markings.Values)
		{
			if (marking.name == "on")
			{
				int index = Consts.PosToIndex(marking.transform.position);
				indexes.Add(index);
				Vector3 v = Consts.IndexToPos(index);

				Consts.current_heights[index] = BL.y + slider_value * (Consts.current_heights[index] - BL.y);

				marking.transform.Translate(0, Consts.current_heights[index] - v.y, 0);
				Consts.former_heights[index] = Consts.current_heights[index];
				UndoBuffer.Add(v, Consts.IndexToPos(index));
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
		var surroundings = Build.Get_surrounding_tiles(markings.Values.ToList());
		if (surroundings.Count != 0)
		{
			surroundings.Clear();
			return;
		}

		//Update terrain
		HashSet<int> indexes = new HashSet<int>();
		foreach (GameObject znacznik in markings.Values)
		{
			if (znacznik.name == "on")
			{
				Vector3 v = znacznik.transform.position;
				int index = Consts.PosToIndex(v);
				indexes.Add(index);
				Consts.current_heights[index] = float.NaN;
				Consts.former_heights[index] = Consts.current_heights[index];
				UndoBuffer.Add(v, Consts.IndexToPos(index));
			}
		}
		UndoBuffer.ApplyOperation();
		Consts.UpdateMapColliders(indexes);
	}
	void SetShapeInfo(bool SetTR)
	{
		if (SetTR)
		{
			//We have bottom-left, now we're searching for upper-right (all relative to 'rotation' of selection)
			int lowX = int.MaxValue, hiX = int.MinValue, lowZ = int.MaxValue, hiZ = int.MinValue;
			foreach (GameObject znacznik in markings.Values)
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

		slider_realheight = Consts.SliderValue2RealHeight(HeightSlider.value);

		if ((BL.x < TR.x && BL.z >= TR.z) || (BL.x > TR.x && BL.z <= TR.z))
		{
			slope_length = (int)Mathf.Abs(BL.x - TR.x);
		}
		else
			slope_length = (int)Mathf.Abs(BL.z - TR.z);

		if (Symmetric.isOn)
			slope_length /= 2;
	}
	
	List<float> GetSlopePoints(float height, int steps)
    {
		switch (LastSelected)
        {
			case FormButton.jump:
				return Consts.Razorstep(height, steps, steps - 1, 0); // Doesn't need the symmetric modifier
			case FormButton.jumpend:
				return Consts.Razorstep(height, steps, 0, slope_length - 1, Symmetric.isOn);
			case FormButton.integral:
				return Consts.Razorstep(height, steps, (slope_length - 1) / 2, (slope_length - 1) / 2, Symmetric.isOn);
			case FormButton.rounded:
				return Consts.Razorstep(height, steps, start_rounding, end_rounding, Symmetric.isOn);
			default:
				return new List<float>();
		}
    }

	void SetMarkingPos(int x, int z, int step, int steps, float heightdiff, List<float> slope_points)
	{
		bool traf = Physics.Raycast(new Vector3(x, Consts.MAX_H, z), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 11);
		if (traf && hit.transform.gameObject.name == "on" && Consts.IsWithinMapBounds(x, z))
		{
			int index = Consts.PosToIndex(x, z);
			float Y;
			if (LastSelected == FormButton.flatter)
			{
				slope_points = TileManager.TileListInfo[selected_tiles[0].name].FlatterPoints.ToList();
				if (!Input.GetKey(KeyCode.Y))
					Y = BL.y - slope_points[step];
				else
				{
					heightdiff = slope_points[steps];
					Y = BL.y + heightdiff - slope_points[steps - step];
				}
			}
			else if (LastSelected == FormButton.linear)
			{
				Y = BL.y + (float)step / steps * heightdiff;
			}
			else
			{
				Y = BL.y + slope_points[step];
			}
			if (KeepShape.isOn)
				Y += Consts.current_heights[index] - BL.y;
			if (float.IsNaN(Y))
				return;
			Vector3 for_buffer = Consts.IndexToPos(index);
			Consts.former_heights[index] = Y;
			Consts.current_heights[index] = Consts.former_heights[index];
			UndoBuffer.Add(for_buffer, Consts.IndexToPos(index));
			GameObject znacznik = hit.transform.gameObject;
			znacznik.transform.Translate(0, Y - for_buffer.y, 0);
		}
	}
	private bool IsFlatter(string Name)
	{
		return TileManager.TileListInfo[Name].FlatterPoints != null;
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
			StateSwitch(SelectionState.LMB_DOWN);

			foreach (int index in markings.Keys)
				BeforeMarking.Add(index, markings[index].name);
			p1 = Vector3Int.RoundToInt(Highlight.pos); //Input.mousePosition;
			p2 = new Vector3Int(-1, -1, -1);
		}
		if (Input.GetMouseButton(0) && (Vector3Int.RoundToInt(Highlight.pos) != p2 || Input.GetKeyUp(KeyCode.T) || Input.GetKeyUp(KeyCode.R)))
		{
			foreach (int index in markings.Keys)
			{
				if (IsWithinMarkingBounds(Vector3Int.RoundToInt(Consts.IndexToPos(index))))
				{
					markings[index].name = BeforeMarking[index];
					markings[index].GetComponent<MeshRenderer>().sharedMaterial = BeforeMarking[index] == "on" ? red : white;
				}
			}
			if (Vector3Int.RoundToInt(Highlight.pos) != p2)
				p2 = Vector3Int.RoundToInt(Highlight.pos);
			
			if (Input.GetKeyUp(KeyCode.T))
				MarkingPatternSwitch();

			foreach (int index in markings.Keys)
			{
				if (IsWithinMarkingBounds(Vector3Int.RoundToInt(Consts.IndexToPos(index))))
				{
					if (markings[index].name == "Cube" && (Addition_mode.isOn || Inversion_mode.isOn))
					{
						markings[index].name = "on";
						markings[index].GetComponent<MeshRenderer>().sharedMaterial = red;
					}
					else if (markings[index].name == "on" && (Exclusion_mode.isOn || Inversion_mode.isOn))
					{
						markings[index].name = "Cube";
						markings[index].GetComponent<MeshRenderer>().sharedMaterial = white;
					}
				}
			}
		}
		if (Input.GetMouseButtonUp(0))
		{ // .. end of selection
			BeforeMarking.Clear();
			StateSwitch(SelectionState.MARKING_VERTICES);
		}
		if (selectionState == SelectionState.MARKING_VERTICES && LastSelected != FormButton.none)
			StateSwitch(SelectionState.WAITING4BL);

		void InverseSelection()
		{
			foreach (var z in markings.Values)
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

	//void OnGUI()
	//{
	//	if (selectionState == SelectionState.LMB_DOWN)
	//	{
	//		// Create a rect from both mouse positions
	//		Rect rect = Utils.GetScreenRect(p1, Input.mousePosition);
	//		Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
	//	}
	//}
	public bool IsWithinMarkingBounds(Vector3Int pos) //GameObject znacznik)
	{
		if (selectionState != SelectionState.LMB_DOWN)
			return false;
		//Camera camera = Camera.main;
		//Bounds viewportBounds = Utils.GetViewportBounds(camera, p1, Input.mousePosition);
		//return viewportBounds.Contains(camera.WorldToViewportPoint(gameObject.transform.position));
		if (CurrentPattern == MarkingPattern.rect)
		{
			int x_min, x_max, z_min, z_max;
			x_min = Math.Min(p1.x, p2.x);
			x_max = Math.Max(p1.x, p2.x);
			z_min = Math.Min(p1.z, p2.z);
			z_max = Math.Max(p1.z, p2.z);
			return pos.x >= x_min && pos.x <= x_max && pos.z >= z_min && pos.z <= z_max;
		}
		else if (CurrentPattern == MarkingPattern.triangle)
        {
			if (p2.z != p1.z && p2.x != p1.x)
			{
				if (p1.x < p2.x && p1.z < p2.z && (pos.x < p1.x || pos.z > p2.z))
					return false;
				else if (p1.x < p2.x && p1.z > p2.z && (pos.z > p1.z || pos.x > p2.x))
					return false;
				else if (p1.x > p2.x && p1.z < p2.z && (pos.z < p1.z || pos.x < p2.x))
					return false;
				else if (p1.x > p2.x && p1.z > p2.z && (pos.x > p1.x || pos.z < p2.z))
					return false;

				float k = (float)(p2.z - p1.z) / (float)(p2.x - p1.x);
				if (p2.x - p1.x > 0)
					return pos.z - p1.z >= k * (pos.x - p1.x);
				else
					return pos.z - p1.z <= k * (pos.x - p1.x);
			}
			else
            {
				int x_min, x_max, z_min, z_max;
				x_min = Math.Min(p1.x, p2.x);
				x_max = Math.Max(p1.x, p2.x);
				z_min = Math.Min(p1.z, p2.z);
				z_max = Math.Max(p1.z, p2.z);
				return pos.x >= x_min && pos.x <= x_max && pos.z >= z_min && pos.z <= z_max;
			}
		}
		else if (CurrentPattern == MarkingPattern.triangle_inv)
		{
			if (p2.z != p1.z && p2.x != p1.x)
			{
				if (p1.x < p2.x && p1.z < p2.z && (pos.x > p2.x || pos.z < p1.z))
					return false;
				else if (p1.x < p2.x && p1.z > p2.z && (pos.z < p2.z || pos.x < p1.x))
					return false;
				else if (p1.x > p2.x && p1.z < p2.z && (pos.z > p2.z || pos.x > p1.x))
					return false;
				else if (p1.x > p2.x && p1.z > p2.z && (pos.x < p2.x || pos.z > p1.z))
					return false;

				float k = (float)(p2.z - p1.z) / (float)(p2.x - p1.x);
				if (p2.x - p1.x > 0)
					return pos.z - p1.z <= k * (pos.x - p1.x);
				else
					return pos.z - p1.z >= k * (pos.x - p1.x);
			}
			else
			{
				int x_min, x_max, z_min, z_max;
				x_min = Math.Min(p1.x, p2.x);
				x_max = Math.Max(p1.x, p2.x);
				z_min = Math.Min(p1.z, p2.z);
				z_max = Math.Max(p1.z, p2.z);
				return pos.x >= x_min && pos.x <= x_max && pos.z >= z_min && pos.z <= z_max;
			}
		}
		else if (CurrentPattern == MarkingPattern.diagonal)
        {
			int xz_sum_min, xz_sum_max, xz_dif_min, xz_dif_max;
			xz_sum_min = Math.Min(p1.x + p1.z, p2.x + p2.z);
			xz_sum_max = Math.Max(p1.x + p1.z, p2.x + p2.z);
			xz_dif_min = Math.Min(p1.x - p1.z, p2.x - p2.z);
			xz_dif_max = Math.Max(p1.x - p1.z, p2.x - p2.z);
			return pos.x + pos.z >= xz_sum_min && pos.x + pos.z <= xz_sum_max
				&& pos.x - pos.z >= xz_dif_min && pos.x - pos.z <= xz_dif_max;
		}
		return false;
	}

	void SpawnVertexBoxes(GameObject tile, bool checkTerrain, bool ForceMapVertices)
	{
		if (selectionState == SelectionState.NOSELECTION || selectionState == SelectionState.SELECTING_VERTICES)
		{
			if (checkTerrain) // Selecting with Q
			{
				areal_selection = true;
			}
			if (areal_selection)
			{
				CreateMarkingsWithFixedBounds();
			}
			else if (!selected_tiles.Contains(tile))
			{
				if (ForceMapVertices)
					CreateNewSetOfMarkingsFromGrass();
				else
					CreateNewSetOfMarkingsFromTile();
			}
			else
				DeleteMarkings();
		}

		//local functions
		void CreateMarkingsWithFixedBounds()
		{
			Vector3 v = Highlight.pos;
			for (int z = (int)(v.z - Consts.MarkerBounds.y / 2); z <= v.z + Consts.MarkerBounds.y / 2; z++)
			{
				for (int x = (int)(v.x - Consts.MarkerBounds.x / 2); x <= v.x + Consts.MarkerBounds.x / 2; x++)
				{
					if (x >= 0 && x <= 4 * Consts.TRACK.Width && z >= 0 && z <= 4 * Consts.TRACK.Height
						&& !markings.ContainsKey(Consts.PosToIndex(x, z)))
					{
						GameObject znacznik = Consts.CreateMarking(white, Consts.IndexToPos(Consts.PosToIndex(x, z)));
						markings.Add(Consts.PosToIndex(x, z), znacznik);
					}
				}
			}
		}
		void CreateNewSetOfMarkingsFromGrass() // creates markings based on all markings over rmc of area
		{
			List<Vector3> vertices = GetAllVerticesOfTile();
			RaycastHit hit;
			foreach (var v in vertices)
			{
				if (!Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, out hit, Consts.RAY_H, 1 << 11)
					&& !Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, out hit, Consts.RAY_H, 1 << 12))
				{
					markings.Add(Consts.PosToIndex(v), Consts.CreateMarking(white, v));
				}
			}
			selected_tiles.Add(tile);
		}

		void CreateNewSetOfMarkingsFromTile()
		{
			List<Vector3> sensitive_vertices;
			if (tile.layer == 8)
			{
				sensitive_vertices = Build.Get_grass_vertices(tile);
			}
			else
			{
				sensitive_vertices = Build.Border_Vault.Get_sensitive_vertices(tile);
			}
			foreach (var pos in sensitive_vertices)
				if (!markings.ContainsKey(Consts.PosToIndex(pos)))
					markings.Add(Consts.PosToIndex(pos), Consts.CreateMarking(white, pos));
			selected_tiles.Add(tile);
		}

		void DeleteMarkings()
		{
			List<Vector3> vertices = GetAllVerticesOfTile();
			RaycastHit hit_mrk;
			foreach (var v in vertices)
			{
				if (Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, out hit_mrk, Consts.RAY_H, 1 << 11)
					|| Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, out hit_mrk, Consts.RAY_H, 1 << 12))
				{
					RaycastHit[] hit_tiles = Physics.RaycastAll(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, Consts.RAY_H, 1 << 8)
						.Concat(Physics.RaycastAll(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, Consts.RAY_H, 1 << 9)).ToArray();

					// Don't delete marking if it belongs to another selected tile
					bool shared = false;
					foreach (var h in hit_tiles)
						if (h.transform.gameObject != tile && selected_tiles.Contains(h.transform.gameObject))
							shared = true;

					if (!shared)
					{
						Destroy(hit_mrk.transform.gameObject);
						markings.Remove(Consts.PosToIndex(v));
					}
				}
			}
			selected_tiles.Remove(tile);
		}

		List<Vector3> GetAllVerticesOfTile()
		{
			Vector3 pos = tile.transform.position;
			pos.y = Consts.MAX_H;
			GameObject[] corresponding_grasses;
			if (tile.layer == 8)
				corresponding_grasses = new GameObject[] { tile };
			else
			{
				RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Consts.RAY_H, 1 << 8);
				corresponding_grasses = new GameObject[hits.Length];
				for (int i = 0; i < hits.Length; i++)
					corresponding_grasses[i] = hits[i].transform.gameObject;
			}
			List<Vector3> vertices = new List<Vector3>();
			foreach (var grass in corresponding_grasses)
			{
				Vector3[] sub_vertices = grass.GetComponent<MeshFilter>().mesh.vertices;
				foreach (var v in sub_vertices)
					if (!vertices.Contains(grass.transform.TransformPoint(v)))
						vertices.Add(grass.transform.TransformPoint(v));
			}
			return vertices;
		}
	}

	public void Del_znaczniki()
	{
		if (markings.Count != 0)
		{
			foreach (var mrk in markings.Values)
				Destroy(mrk);
			markings.Clear();
			selected_tiles.Clear();
			BeforeMarking.Clear();
		}
	}
	
	public void MarkingModeSwitch()
    {
		if (Inversion_mode.isOn)
			Addition_mode.isOn = true;
		else if (Addition_mode.isOn)
			Exclusion_mode.isOn = true;
		else if (Exclusion_mode.isOn)
			Inversion_mode.isOn = true;
    }

	private void MarkingPatternSwitch()
    {
		CurrentPattern = (MarkingPattern)(((int)CurrentPattern + 1) % Enum.GetNames(typeof(MarkingPattern)).Length);
    }

	private void ShapeTypeSwitch(FormButton new_type)
    {
		LastSelected = new_type;
		StateSwitch(SelectionState.WAITING4BL);
	}

	/// <summary>
	/// internal => visible also for every script attached to ShapeMenu
	/// </summary>
	internal void StateSwitch(SelectionState newstate)
	{
		selectionState = newstate;
		if (newstate == SelectionState.NOSELECTION)
		{
			areal_selection = false;
			Del_znaczniki();
			FormMenu.SetActive(false);
			formSlider.SwitchTextStatus("Shape forming");
		}
		else if (newstate == SelectionState.SELECTING_VERTICES)
		{
			FormMenu.SetActive(true);
			formSlider.SwitchTextStatus("Selecting vertices..");
		}
		else if (newstate == SelectionState.MARKING_VERTICES)
		{
			LastSelected = FormButton.none;
			start_rounding = -1;
			end_rounding = -1;
			formSlider.SwitchTextStatus("Marking vertices..");
		}
		else if (newstate == SelectionState.WAITING4BL)
		{
			if(LastSelected == FormButton.amplify)
				formSlider.SwitchTextStatus("Select h0 vertex..");
			else
				formSlider.SwitchTextStatus("Waiting for bottom-left vertex..");
		}
		else if (newstate == SelectionState.WAITING4TR)
		{
			formSlider.SwitchTextStatus("Waiting for top-right vertex..");
		}
		else if (newstate == SelectionState.SETTING_PARAMETERS)
		{
			if (start_rounding == -1)
				formSlider.SwitchTextStatus($"Set start rounding (0 - {slope_length - 1}) & Enter");
			else if (end_rounding == -1)
				formSlider.SwitchTextStatus($"Set end rounding (0 - {Math.Max(slope_length - 1 - start_rounding, 0)}) & Enter");
		}
	}
}
