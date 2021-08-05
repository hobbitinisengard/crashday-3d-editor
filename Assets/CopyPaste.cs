using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum CopyState { empty, copying, non_empty }
public enum PastingMode { fixed_height, addition }
/// <summary>
/// works with ShapeMenu. Handles copy/paste functionality. 
/// </summary>
public class CopyPaste : MonoBehaviour
{
	public GameObject FormPanel;
	public GameObject FormMenu;
	public Button CopyButton;
	public Button EnterCopyPasteMenu;
	public Button PastingModeSwitch;
	public Material seethrough;
	/// <summary>
	/// Coordinates of copied vertices. Put here to live when switching form-build tabs
	/// </summary>
	private static List<Vector3> CopyClipboard = new List<Vector3>();
	private static List<GameObject> Markings = new List<GameObject>();
	private int SelectionRotationVal;

	private static CopyState copystate = CopyState.empty;
	private PastingMode pastingMode = PastingMode.fixed_height;
	private static float fixed_height;
	private Vector3 lastpos;

	public static bool IsEnabled()
	{
		return copystate == CopyState.copying;
	}
	private void OnDisable()
	{
		SwitchState(CopyState.non_empty);
	}
	private void OnEnable()
	{
		
	}
	private void Start()
	{
		CopyButton.onClick.AddListener(() => { ShapeMenu.LastSelected = FormButton.copy; });
		EnterCopyPasteMenu.onClick.AddListener(() => { SwitchState(CopyState.copying); });
		PastingModeSwitch.onClick.AddListener(SwitchPastingMode);

		FormPanel.GetComponent<Form>().HeightSlider.onValueChanged.AddListener((val) => { MousewheelWorks(val); });
	}
	void SwitchPastingMode()
	{
		if (pastingMode == PastingMode.fixed_height)
		{
			FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(false);
			pastingMode = PastingMode.addition;
			PastingModeSwitch.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Addition";
			UpdatePreview();
		}
		else if (pastingMode == PastingMode.addition)
		{
			FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(true);
			pastingMode = PastingMode.fixed_height;
			PastingModeSwitch.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Fixed height";
			UpdatePreview();
		}
	}
	void Update()
	{
		
		
		if (MouseInputUIBlocker.BlockedByUI)
			return;
		// ShapeMenu triggers CopySelectionToClipboard()
		if (Input.GetKeyDown(KeyCode.C))
			ShapeMenu.LastSelected = FormButton.copy;
		if (Input.GetKeyDown(KeyCode.V) && copystate == CopyState.non_empty)
			SwitchState(CopyState.copying);

		if (copystate == CopyState.copying)
		{
			if (pastingMode == PastingMode.fixed_height)
			{
				FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(true);
			}
			else if (pastingMode == PastingMode.addition)
				FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(false);

			if (Input.GetKeyDown(KeyCode.Tab))
				SwitchPastingMode();
			if (Input.GetMouseButtonDown(1))
				RotateClockwiseSelection();
			if (Input.GetMouseButtonDown(0))
				PasteSelectionOntoTerrain();
			if (Input.GetKeyDown(KeyCode.Q))
				InverseSelection();
			if (Input.GetKeyDown(KeyCode.I))
				InverseHeights();
			if (Input.GetKeyDown(KeyCode.Delete))
				SwitchState(CopyState.empty);
			if (Input.GetKeyDown(KeyCode.Escape))
				SwitchState(CopyState.non_empty);
			if (lastpos != Highlight.pos && copystate == CopyState.copying)
				UpdatePreview();
			if (copystate == CopyState.copying && FormMenu.activeSelf)
			{
				SwitchState(CopyState.non_empty);
			}
		}

	}
	internal void CopySelectionToClipboard()
	{
		CopyClipboard.Clear();
		// Save height for fixed height options
		fixed_height = ShapeMenu.BL.y;
		// reserve place for first marking = Vector3 zero.
		CopyClipboard.Add(Vector3.zero);
		foreach (var mrk in ShapeMenu.markings)
		{
			if (mrk.name == "on")
			{
				Vector3 pom = Consts.RoundVector3(mrk.transform.position - ShapeMenu.BL);
				if (pom.x == 0 && pom.z == 0)
					continue;
				else
					CopyClipboard.Add(pom);
			}
		}
		SelectionRotationVal = 0;

		SwitchState(CopyState.copying);
	}
	private void UpdatePreview()
	{
		if (MouseInputUIBlocker.BlockedByUI)
			return;
		RemoveCopyPoints();
		lastpos = Highlight.pos;
		if (Highlight.pos.x == -1)
			return;

		foreach (var mrk in CopyClipboard)
		{
			if (Consts.IsWithinMapBounds(Highlight.pos + mrk))
			{
				Vector3 pos = Highlight.pos + mrk;
				// height of preview depends on pasting mode
				if (pastingMode == PastingMode.fixed_height)
					pos.Set(pos.x, fixed_height + mrk.y, pos.z);

				GameObject znacznik = Consts.CreateMarking(seethrough, pos, false);
				Markings.Add(znacznik);
			}
		}
	}
	private void RemoveCopyPoints()
	{
		for (int i = 0; i < Markings.Count; i++)
			Destroy(Markings[i]);
		Markings.Clear();
	}
	private void SwitchState(CopyState cs)
	{
		copystate = cs;
		if (cs == CopyState.copying)
		{
			GetComponent<ShapeMenu>().StateSwitch(SelectionState.NOSELECTION);
			FormMenu.SetActive(false);
			PastingModeSwitch.gameObject.SetActive(true);
			EnterCopyPasteMenu.interactable = true;
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("TAB/LMB/RMB/M/I/ESC/DEL");
		}
		else
		{
			FormPanel.GetComponent<Form>().HeightSlider.gameObject.SetActive(true);
			FormPanel.GetComponent<Form>().FormSlider.GetComponent<FormSlider>().SwitchTextStatus("Shape forming");
			if (cs == CopyState.empty)
			{
				PastingModeSwitch.gameObject.SetActive(false);
				RemoveCopyPoints();
				CopyClipboard.Clear();
				EnterCopyPasteMenu.interactable = false;
			}
			else if (cs == CopyState.non_empty)
			{
				PastingModeSwitch.gameObject.SetActive(false);
				RemoveCopyPoints();
			}
		}
	}


	public void RotateClockwiseSelection()
	{
		if (CopyClipboard.Count == 0)
			return;
		SelectionRotationVal = SelectionRotationVal == 270 ? 0 : SelectionRotationVal + 90;
		for (int i = 1; i < CopyClipboard.Count; i++)
		{
			CopyClipboard[i] = Consts.RotatePointAroundPivot(CopyClipboard[i], CopyClipboard[0], new Vector3(0, 90, 0));
		}
		UpdatePreview();
	}

	
	public void InverseHeights()
	{
		for (int i = 0; i < CopyClipboard.Count; i++)
		{
			CopyClipboard[i] = new Vector3(CopyClipboard[i].x, -CopyClipboard[i].y, CopyClipboard[i].z);
		}
		UpdatePreview();
	}
	/// <summary>
	/// Mirrors selection always along Z axis
	/// </summary>
	public void InverseSelection()
	{
		if (CopyClipboard.Count < 2)
			return;

		for (int i = 0; i < CopyClipboard.Count; i++)
		{
			CopyClipboard[i] = Vector3.Scale(CopyClipboard[i], new Vector3(-1, 1, 1));
		}
		UpdatePreview();
	}

	private void PasteSelectionOntoTerrain()
	{
		if (CopyClipboard.Count == 0)
			return;

		UndoBuffer.Add(CopyClipboard);
		//Indexes of vertices for UpdateMapColliders()
		HashSet<int> indexes = new HashSet<int>();
		foreach (var mrk in CopyClipboard)
		{
			if (Consts.IsWithinMapBounds(Highlight.pos + mrk))
			{
				Vector3 pom = Highlight.pos + mrk;
				// Update arrays of vertex heights
				int newindex = Consts.PosToIndex(pom);
				indexes.Add(newindex);
				UndoBuffer.Add((int)pom.x, (int)pom.z);
				if (pastingMode == PastingMode.fixed_height)
				{
					Consts.current_heights[newindex] = fixed_height + mrk.y;
					Consts.former_heights[newindex] = fixed_height + mrk.y;
				}
				else if (pastingMode == PastingMode.addition)
				{
					Consts.current_heights[newindex] = pom.y;
					Consts.former_heights[newindex] = pom.y;
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		UndoBuffer.ApplyOperation();
		Build.UpdateTiles(Build.Get_surrounding_tiles(Markings, true));
	}
	void MousewheelWorks(float sliderval)
	{
		if(copystate == CopyState.copying)
		{
			fixed_height = Consts.SliderValue2RealHeight(sliderval);
			UpdatePreview();
		}
	}
}
