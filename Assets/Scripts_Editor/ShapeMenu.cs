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

/// <summary>
/// Handles movement through vertices in both diagonal and normal slopes.
/// </summary>
public class VertexInfo
{
	private readonly bool diagonal;
	private bool rotate;

	private float _x;
	public float x { get => _x; }

	private float _z;
	public float z { get => _z; }

	private int i_width;
	public int indicator_width
	{
		get => i_width;
		set
		{
			i_width = value;
			(_x, _z) = IndicatorsToPos(i_width, i_length, diagonal, rotate);
		}
	}

	private int i_length;
	public int indicator_length
	{
		get => i_length;
		set
		{
			i_length = value;
			(_x, _z) = IndicatorsToPos(i_width, i_length, diagonal, rotate);
		}
	}

	public VertexInfo(float x, float z, bool diagonal, bool rotate = false)
	{
		if ((long)x + (long)z > int.MaxValue || Math.Abs((long)z - (long)x) > int.MaxValue)
		{
			x /= 2;
			z /= 2;
		}
		_x = x;
		_z = z;
		this.diagonal = diagonal;
		this.rotate = rotate;
		UpdateIndicators();
	}

	public VertexInfo(bool diagonal, bool rotate)
    {
		this.diagonal = diagonal;
		this.rotate = rotate;
	}

	public void Set(int i_width, int i_length, bool rotate)
	{
		this.i_width = i_width;
		this.i_length = i_length;
		this.rotate = rotate;
		(_x, _z) = IndicatorsToPos(i_width, i_length, diagonal, rotate);
	}

	public void Set(int i_width, int i_length)
    {
		Set(i_width, i_length, rotate);
	}

	/// <summary>
	/// In non-diagonal slopes, the indicators are just the x and y coordinates, otherwise x - y and x + y.
	/// </summary>
	private void UpdateIndicators()
	{
		if (!diagonal)
		{
			i_width = Mathf.RoundToInt(rotate ? _z : _x);
			i_length = Mathf.RoundToInt(rotate ? _x : _z);
		}
		else
		{
			i_width = Mathf.RoundToInt(rotate ? _x + _z : _x - _z);
			i_length = Mathf.RoundToInt(rotate ? _x - _z : _x + _z);
		}
	}

	public static (float, float) IndicatorsToPos(int i_width, int i_length, bool diagonal, bool rotate)
    {
		if (!diagonal)
			return rotate ? (i_length, i_width) : (i_width, i_length);
		else
			return rotate ? ((i_width + i_length) / 2f, (i_width - i_length) / 2f)
				          : ((i_length + i_width) / 2f, (i_length - i_width) / 2f);
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
	public Toggle Diagonal;
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
	/// Needed for toggling diagonal mode in the setting_parameters state
	/// </summary>
	private static Vector3 initial_bl;
	private static Vector3 initial_tr;
	/// <summary>
	/// Bottom Left pointing vector set in waiting4BL state
	/// </summary>
	public static Vector3 BL;
	private static Vector3 TR;
	/// <summary>
	/// The slope goes along: false => Z axis, true => X axis (not considering the Diagonal modifier).
	/// </summary>
	private bool rotate;
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
			{ KeyCode.F3, Diagonal  },
			{ KeyCode.F4, Symmetric },
			{ KeyCode.F6, SelectTR  }
		};

		Symmetric.onValueChanged.AddListener(delegate { UpdateShapeInfo(); });
		Diagonal.onValueChanged.AddListener(delegate { UpdateShapeInfo(); });
	}
	void UpdateShapeInfo()
	{
		if (selectionState == SelectionState.SETTING_PARAMETERS)
		{
			SetShapeInfo();
			StateSwitch(SelectionState.SETTING_PARAMETERS); // Update the maximum start and end rounding on the status bar
		}
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
				ApplyTerrainOperation();
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
			if (selected_tiles.Count > 0)
				StateSwitch(SelectionState.MARKING_VERTICES);
			else
				StateSwitch(SelectionState.NOSELECTION);
		}
		else if (selectionState == SelectionState.MARKING_VERTICES && Input.GetKeyDown(KeyCode.LeftControl) && !areal_selection)
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

				if (!Input.GetKey(KeyCode.LeftControl))
					StateSwitch(SelectionState.MARKING_VERTICES);
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
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z), Vector3.down, Consts.RAY_H, 1 << 11))
				{
					BL = Highlight.pos;
					BL.y = Consts.current_heights[Consts.PosToIndex(Highlight.pos)];
					initial_bl = BL;
					if (SelectTR.isOn)
						StateSwitch(SelectionState.WAITING4TR);
					else
					{
						slider_realheight = Consts.SliderValue2RealHeight(HeightSlider.value);
						SetShapeInfo();
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
				if (Physics.Raycast(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z),
									Vector3.down, Consts.RAY_H, 1 << 11))
				{
					TR = Highlight.pos;
					initial_tr = TR;
					slider_realheight = Consts.SliderValue2RealHeight(HeightSlider.value);
					SetShapeInfo();
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

	void ApplyTerrainOperation()
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
				UndoBuffer.AddVertexPair(v, Consts.IndexToPos(index));
			}
		}
		UndoBuffer.ApplyTerrainOperation();
		Consts.UpdateMapColliders(indexes);

		Build.UpdateTiles(surroundings);
		surroundings.Clear();
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
				UndoBuffer.AddVertexPair(v, Consts.IndexToPos(index));
			}
		}
		UndoBuffer.ApplyTerrainOperation();
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
				UndoBuffer.AddVertexPair(v, Consts.IndexToPos(index));
			}
		}
		UndoBuffer.ApplyTerrainOperation();
		Consts.UpdateMapColliders(indexes);
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
		else if (slider_realheight == ShapeMenu.BL.y && !Connect.isOn || slope_length == 0)
			return;

		start_rounding = Math.Min(start_rounding, slope_length - 1);
		end_rounding = Math.Min(end_rounding, slope_length - 1 - start_rounding);

		VertexInfo BL = new VertexInfo(ShapeMenu.BL.x, ShapeMenu.BL.z, Diagonal.isOn, rotate);
		VertexInfo TR = new VertexInfo(ShapeMenu.TR.x, ShapeMenu.TR.z, Diagonal.isOn, rotate);
		VertexInfo current = new VertexInfo(Diagonal.isOn, rotate);

		int steps = Math.Abs(TR.indicator_length - BL.indicator_length);
		var extremes = GetOpposingHeights();
		List<float> slope_points = new List<float>();

		for (current.indicator_width = BL.indicator_width; WithinSlopeBounds(false); MoveTowardTR(false))
		{
			int index = Math.Abs(current.indicator_width - BL.indicator_width);
			ShapeMenu.BL.y = extremes[index].y1;
			float heightdiff = extremes[index].y2 - extremes[index].y1;
			int step;
			if (Connect.isOn || current.indicator_width == BL.indicator_width)
				slope_points = GetSlopePoints(heightdiff, steps);

			for (current.indicator_length = StartIndicator(); WithinSlopeBounds(true); MoveTowardTR(true))
			{
				step = Math.Abs(current.indicator_length - BL.indicator_length);
				SetMarkingPos((int)current.x, (int)current.z, step, steps, heightdiff, slope_points);
			}
		}
		Consts.UpdateMapColliders(markings.Values.ToList());
		var surroundings = Build.Get_surrounding_tiles(markings.Values.ToList());
		Build.UpdateTiles(surroundings);
		surroundings.Clear();
		UndoBuffer.ApplyTerrainOperation();

		int StartIndicator()
        {
			if (Diagonal.isOn && Math.Abs(current.indicator_width % 2) != Math.Abs(BL.indicator_length % 2))
			{   // The start step alternates between 0 and 1 in diagonal slopes
				return BL.indicator_length + (BL.indicator_length < TR.indicator_length ? 1 : -1);
			}
			else
				return BL.indicator_length;
		}

		bool WithinSlopeBounds(bool lengthwise)
		{
			if (lengthwise)
				return BL.indicator_length <= current.indicator_length && current.indicator_length <= TR.indicator_length
					|| BL.indicator_length >= current.indicator_length && current.indicator_length >= TR.indicator_length;
			else
				return BL.indicator_width <= current.indicator_width && current.indicator_width <= TR.indicator_width
					|| BL.indicator_width >= current.indicator_width && current.indicator_width >= TR.indicator_width;
		}

		void MoveTowardTR(bool lengthwise)
		{
			if (lengthwise)
				// The next diagonal step shifts the indicator by 2 (one by x and one by y)
				current.indicator_length += (BL.indicator_length < TR.indicator_length ? 1 : -1) * (Diagonal.isOn ? 2 : 1);
			else
				current.indicator_width += BL.indicator_width < TR.indicator_width ? 1 : -1;
		}

		/// <summary>
		/// Returns the list of start and end heights for each lengthwise row of the slope.
		/// If Connect is off, the values for all rows are the heights of BL and TR respectively.
		/// </summary>
		List<(float y1, float y2)> GetOpposingHeights()
		{
			List<(float, float)> Extremes = new List<(float, float)>();
			float y1, y2;

			for (current.indicator_width = BL.indicator_width; WithinSlopeBounds(false); MoveTowardTR(false))
			{
				if (Connect.isOn)
				{
					current.indicator_length = StartIndicator();
					int dir = BL.indicator_length < TR.indicator_length ? 1 : -1;
					float x2, z2;
					if (!Symmetric.isOn)
						(x2, z2) = VertexInfo.IndicatorsToPos(current.indicator_width, current.indicator_length + steps * dir,
							                                  Diagonal.isOn, rotate);
					else
						(x2, z2) = VertexInfo.IndicatorsToPos(current.indicator_width, (current.indicator_length + steps * dir / 2),
															  Diagonal.isOn, rotate);
					y1 = Consts.current_heights[Consts.PosToIndex((int)current.x, (int)current.z)];
					y2 = Consts.current_heights[Consts.PosToIndex((int)x2, (int)z2)];
				}
				else
				{
					y1 = ShapeMenu.BL.y;
					y2 = slider_realheight;
				}
				Extremes.Add((y1, y2));
			}
			return Extremes;
		}
	}

	void SetShapeInfo()
	{
		// w and l (the indicators) are different from x and z with the Diagonal modifier
		VertexInfo low_bounds = new VertexInfo(int.MaxValue, int.MaxValue / 2, Diagonal.isOn, rotate);
		VertexInfo high_bounds = new VertexInfo(int.MinValue, int.MinValue / 2, Diagonal.isOn, rotate);
		foreach (GameObject znacznik in markings.Values)
		{
			if (znacznik.name == "on")
			{
				Vector3 pos = znacznik.transform.position;
				VertexInfo v = new VertexInfo(pos.x, pos.z, Diagonal.isOn);

				if (low_bounds.indicator_width > v.indicator_width)
					low_bounds.indicator_width = v.indicator_width;
				if (high_bounds.indicator_width < v.indicator_width)
					high_bounds.indicator_width = v.indicator_width;

				if (low_bounds.indicator_length > v.indicator_length)
					low_bounds.indicator_length = v.indicator_length;
				if (high_bounds.indicator_length < v.indicator_length)
					high_bounds.indicator_length = v.indicator_length;
			}
		}

		// Determiming BL and TR having the start (and end) row
		VertexInfo BL = new VertexInfo(initial_bl.x, initial_bl.z, Diagonal.isOn, false);
		VertexInfo TR = new VertexInfo(initial_tr.x, initial_tr.z, Diagonal.isOn, false);
		bool width_1 = low_bounds.indicator_width == high_bounds.indicator_width;
		bool length_1 = low_bounds.indicator_length == high_bounds.indicator_length;

		if (BL.indicator_length <= low_bounds.indicator_length && !length_1 &&
			(BL.indicator_width < high_bounds.indicator_width || width_1 && BL.indicator_width == high_bounds.indicator_width))
		{
			rotate = false;
			if (LastSelected != FormButton.copy)
				BL.Set(low_bounds.indicator_width, BL.indicator_length, rotate);
			TR.Set(high_bounds.indicator_width, SelectTR.isOn ? TR.indicator_length : high_bounds.indicator_length, rotate);
		}
		else if (BL.indicator_width <= low_bounds.indicator_width && !width_1 &&
			(BL.indicator_length > low_bounds.indicator_length || length_1 && BL.indicator_length == low_bounds.indicator_length))
		{
			rotate = true;
			if (LastSelected != FormButton.copy)
				BL.Set(high_bounds.indicator_length, BL.indicator_width, rotate);
			TR.Set(low_bounds.indicator_length, SelectTR.isOn ? TR.indicator_width : high_bounds.indicator_width, rotate);
		}
		else if (BL.indicator_length >= high_bounds.indicator_length && !length_1 &&
			(BL.indicator_width > low_bounds.indicator_width || width_1 && BL.indicator_width == low_bounds.indicator_width))
		{
			rotate = false;
			if (LastSelected != FormButton.copy)
				BL.Set(high_bounds.indicator_width, BL.indicator_length, rotate);
			TR.Set(low_bounds.indicator_width, SelectTR.isOn ? TR.indicator_length : low_bounds.indicator_length, rotate);
		}
		else if (BL.indicator_width >= high_bounds.indicator_width && !width_1 &&
			(BL.indicator_length < high_bounds.indicator_length || length_1 && BL.indicator_length == high_bounds.indicator_length))
		{
			rotate = true;
			if (LastSelected != FormButton.copy)
				BL.Set(low_bounds.indicator_length, BL.indicator_width, rotate);
			TR.Set(high_bounds.indicator_length, SelectTR.isOn ? TR.indicator_width : low_bounds.indicator_width, rotate);
		}
		else
		{
			BL.Set(0, 0);
			TR.Set(0, 0);
		}
		ShapeMenu.BL.Set(BL.x, ShapeMenu.BL.y, BL.z);
		ShapeMenu.TR.Set(TR.x, ShapeMenu.TR.y, TR.z);

		slope_length = Math.Abs(TR.indicator_length - BL.indicator_length);
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
					steps = slope_points.Count - 1;
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
			UndoBuffer.AddVertexPair(for_buffer, Consts.IndexToPos(index));
			GameObject znacznik = hit.transform.gameObject;
			znacznik.transform.Translate(0, Y - for_buffer.y, 0);
		}
	}
	private bool IsFlatter(string Name)
	{
		return TileManager.TileListInfo.ContainsKey(Name) && TileManager.TileListInfo[Name].FlatterPoints != null;
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
			rotate = false;
			formSlider.SwitchTextStatus("Marking vertices..");
		}
		else if (newstate == SelectionState.WAITING4BL)
		{
			if (LastSelected == FormButton.amplify)
				formSlider.SwitchTextStatus("Select h0 vertex..");
			else
				formSlider.SwitchTextStatus("Waiting for start row..");
		}
		else if (newstate == SelectionState.WAITING4TR)
		{
			formSlider.SwitchTextStatus("Waiting for end row..");
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
