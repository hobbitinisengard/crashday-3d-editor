using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingleMode : MonoBehaviour
{
	private ManualSubMode CurrentMode;
	Color32 Color_selected = new Color32(219, 203, 178, 255);
	public GameObject ArealMenu;
	public Slider HeightSlider;
	public Slider IntensitySlider;
	public Slider DistortionSlider;
	public Button SingleModeButton;
	public Button SmoothModeButton;
	public Button AmpModeButton;

	private GameObject indicator;
	private int index;
	private float TargetDistValue = 0;
	private Vector3 InitialPos;

	public void OnDisable()
	{
		index = 0;
		if (indicator != null)
			Destroy(indicator);
	}
	// buttons use this function
	public void SwitchMode(float mode)
	{
		CurrentMode = (ManualSubMode)mode;
		SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		AmpModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		if (mode == 0)
			SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
		else if(mode == 1)
			SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
		else if (mode == 2)
			AmpModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
			SwitchMode(0);
		else if (Input.GetKeyDown(KeyCode.Alpha2))
			SwitchMode(1);
		else if (Input.GetKeyDown(KeyCode.Alpha3))
			SwitchMode(2);
		if (CurrentMode == ManualSubMode.Set)
		{
			IntensitySlider.transform.parent.gameObject.SetActive(false);
			DistortionSlider.transform.parent.gameObject.SetActive(false);
			if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
			{
				if (Input.GetMouseButtonUp(0))
					UndoBuffer.ApplyOperation();

				if (Input.GetKeyDown(KeyCode.Escape)) //ESC toggles off Make_Elevation()
				{
					index = 0;
					if (indicator != null)
						Destroy(indicator);
				}
				if (!MouseInputUIBlocker.BlockedByUI)
				{
					if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
						Single_vertex_manipulation(); // single-action
					else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
						Single_vertex_manipulation(); //auto-fire
					else if (Input.GetMouseButtonDown(1) && Highlight.over)
						Make_elevation();
				}
			}
		}
		else if (CurrentMode == ManualSubMode.Avg)
		{

			IntensitySlider.transform.parent.gameObject.SetActive(true);
			DistortionSlider.transform.parent.gameObject.SetActive(true);
			if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
			{
				if ((Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0)) && !Input.GetKey(KeyCode.LeftAlt))
					UndoBuffer.ApplyOperation();

				if (!MouseInputUIBlocker.BlockedByUI)
				{
					if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
						Single_smoothing(); // single-action
					else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
						Single_smoothing(); //auto-fire
					else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
						Single_distortion(); // single-action
					else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
						Single_distortion(); //auto-fire
				}
			}
		}
		else if (CurrentMode == ManualSubMode.Amp)
		{

			IntensitySlider.transform.parent.gameObject.SetActive(true);
			DistortionSlider.transform.parent.gameObject.SetActive(false);
			if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
			{
				if ((Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0)) && !Input.GetKey(KeyCode.LeftAlt))
					UndoBuffer.ApplyOperation();

				if (!MouseInputUIBlocker.BlockedByUI)
				{
					if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
						Single_amp(); // single-action
					else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
						Single_amp(); //auto-fire
				}
			}
		}
	}
	void Single_amp()
	{
		if (Highlight.over && Consts.IsWithinMapBounds(Highlight.pos))
		{
			Vector3 v = Highlight.pos;
			int index = Consts.PosToIndex(v);
			UndoBuffer.Add(Consts.IndexToPos(index));
			RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Consts.MAX_H, v.z), 0.5f, Vector3.down, Consts.RAY_H, 1 << 9);
			List<GameObject> to_update = new List<GameObject>();
			foreach (RaycastHit hit in hits)
				to_update.Add(hit.transform.gameObject);
			float heightdiff = Consts.current_heights[index] - Consts.SliderValue2RealHeight(HeightSlider.value);
					
			if (to_update.Count > 0)
			{
				if (AreListedObjectsHavingRMCVertexHere(to_update, index))
				{
					Consts.current_heights[index] += heightdiff * IntensitySlider.value/100f;
					//Helper.current_heights[index] = Helper.former_heights[index];
					Consts.UpdateMapColliders(new HashSet<int> { index });
					Build.UpdateTiles(to_update);
				}
			}
			else
			{
				Consts.former_heights[index] += heightdiff * IntensitySlider.value/100f;
				Consts.current_heights[index] = Consts.former_heights[index];
				Consts.UpdateMapColliders(new HashSet<int> { index });
			}
		}
	}
	void Single_distortion()
	{

		if (Consts.IsWithinMapBounds(Highlight.pos))
		{
			float dist_val = Consts.SliderValue2RealHeight(DistortionSlider.value);
			float h_val = Highlight.pos.y;//Consts.SliderValue2RealHeight(HeightSlider.value);

			if (Highlight.pos.x != InitialPos.x || Highlight.pos.z != InitialPos.z)
			{ // vertex -> vertex
				InitialPos = Highlight.pos;
				TargetDistValue = Random.Range(h_val - dist_val, h_val + dist_val);
			}
			int idx = Consts.PosToIndex(Highlight.pos);
			UndoBuffer.Add(Highlight.pos);
			Consts.current_heights[idx] += (TargetDistValue - Consts.current_heights[idx]) * IntensitySlider.value / 100f;
			Consts.former_heights[idx] = Consts.current_heights[idx];
			Consts.UpdateMapColliders(new HashSet<int> { idx });
			var tiles = Build.Get_surrounding_tiles(new HashSet<int> { idx });
			Build.UpdateTiles(tiles);
		}
	}
	void Single_smoothing()
	{
		if (Consts.IsWithinMapBounds(Highlight.pos))
		{
			Vector3 pos = Highlight.pos;
			pos.y = Consts.MAX_H;

			float height_sum = 0;
			for (int x = -1; x <= 1; x++)
			{
				for (int z = -1; z <= 1; z++)
				{
					if (x == 0 && x == z)
						continue;
					pos.x = Highlight.pos.x + x;
					pos.z = Highlight.pos.z + z;
					if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 8))
						height_sum += hit.point.y;
				}
			}
			float avg = height_sum / 8f;
			int idx = Consts.PosToIndex(Highlight.pos);
			UndoBuffer.Add(Highlight.pos);
			Consts.former_heights[idx] += (avg - Consts.former_heights[idx]) * IntensitySlider.value / 100f;
			Consts.current_heights[idx] = Consts.former_heights[idx];
			Consts.UpdateMapColliders(new HashSet<int> { idx });
			var tiles = Build.Get_surrounding_tiles(new HashSet<int> { idx });
			Build.UpdateTiles(tiles);
		}
	}
	/// <summary>
	/// Handles quick rectangular selection in first form mode with RMB
	/// </summary>
	void Make_elevation()
	{
		if (index == 0)
		{
			// Get initial position and set znacznik there
			if (Consts.IsWithinMapBounds(Highlight.pos))
			{
				index = (int)(Highlight.pos.x + 4 * Consts.TRACK.Width * Highlight.pos.z + Highlight.pos.z);
				//Debug.Log("I1="+index+" "+m.vertices[index]+" pos="+highlight.pos);
				indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
				indicator.transform.localScale = new Vector3(.25f, 1, .25f);
				indicator.transform.position = Highlight.pos;
			}
		}
		else
		{
			// Time to get second position
			if (Consts.IsWithinMapBounds(Highlight.pos))
			{
				int index2 = (int)(Highlight.pos.x + 4 * Consts.TRACK.Width * Highlight.pos.z + Highlight.pos.z);
				//Debug.Log("I2="+index2+" "+m.vertices[index]+" pos="+highlight.pos);
				Vector3Int a = Vector3Int.RoundToInt(Consts.IndexToPos(index));
				Vector3Int b = Vector3Int.RoundToInt(Consts.IndexToPos(index2));
				{
					HashSet<int> indexes = new HashSet<int>();
					for (int z = Mathf.Min(a.z, b.z); z <= Mathf.Max(a.z, b.z); z++)
					{
						for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
						{
							int idx = x + 4 * z * Consts.TRACK.Width + z;
							UndoBuffer.Add(Consts.IndexToPos(idx));
							Consts.former_heights[idx] = Consts.SliderValue2RealHeight(HeightSlider.value);
							Consts.current_heights[idx] = Consts.former_heights[idx];
							indexes.Add(idx);
						}
					}
					Consts.UpdateMapColliders(indexes);
				}
				Destroy(indicator);
				index = 0;
				RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Consts.MAX_H, 0.5f * (a.z + b.z)), new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))), Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9); //Szukaj jakiegokolwiek tilesa w zaznaczeniu
				List<GameObject> to_update = new List<GameObject>();
				foreach (RaycastHit hit in hits)
					to_update.Add(hit.transform.gameObject);

				Build.UpdateTiles(to_update);
				UndoBuffer.ApplyOperation();
			}
		}
	}
	/// <summary>
	/// Handles quick sculpting mode
	/// </summary>
	void Single_vertex_manipulation()
	{
		if (Highlight.over && Consts.IsWithinMapBounds(Highlight.pos))
		{
			Vector3 v = Highlight.pos;
			int index = Consts.PosToIndex(v);
			UndoBuffer.Add(Consts.IndexToPos(index));
			RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Consts.MAX_H, v.z), 0.5f, Vector3.down, Consts.RAY_H, 1 << 9);
			List<GameObject> to_update = new List<GameObject>();
			foreach (RaycastHit hit in hits)
				to_update.Add(hit.transform.gameObject);

			if (to_update.Count > 0)
			{
				if (AreListedObjectsHavingRMCVertexHere(to_update, index))
				{
					Consts.current_heights[index] = Consts.SliderValue2RealHeight(HeightSlider.value);
					Consts.former_heights[index] = Consts.current_heights[index];
					Consts.UpdateMapColliders(new HashSet<int> { index });
					Build.UpdateTiles(to_update);
				}
			}
			else
			{
				Consts.former_heights[index] = Consts.SliderValue2RealHeight(HeightSlider.value);
				Consts.current_heights[index] = Consts.former_heights[index];
				Consts.UpdateMapColliders(new HashSet<int> { index });
			}
		}
	}
	bool AreListedObjectsHavingRMCVertexHere(List<GameObject> to_update, int index)
	{
		foreach (GameObject rmc in to_update)
		{
			bool found_matching = false;
			foreach (Vector3 v in rmc.GetComponent<MeshCollider>().sharedMesh.vertices)
			{
				Vector3Int V = Vector3Int.RoundToInt(rmc.transform.TransformPoint(v));
				if (Consts.PosToIndex(V) == index)
				{
					found_matching = true;
					break;
				}
			}
			if (!found_matching)
				return false;
		}
		return true;
	}
}
