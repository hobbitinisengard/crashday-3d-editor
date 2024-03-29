﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum SubMode { Set, Avg, Amp }

public class ManualMode : MonoBehaviour
{
	Color32 Color_selected = new Color32(219, 203, 178, 255);
	public Slider HeightSlider;
	public Slider IntensitySlider;
	public Slider DistortionSlider;
	public Slider RadiusSlider;
	public Text SingleModeText;
	public Text SmoothModeText;
	public Text AmplifyModeText;
	/// <summary>
	/// Used for rectangular elevation (RMB)
	/// </summary>
	private static GameObject P1_marker;
	private static Vector3 P1 = new Vector3(-1, -1, -1);
	/// <summary>
	/// Center of the circumference of height change
	/// </summary>
	private Vector3 InitialPos;
	private float TargetDistortionValue;
	private Dictionary<int, float> TargetDistortionHeights = new Dictionary<int, float>();
	private Dictionary<int, float> TargetSetHeights = new Dictionary<int, float>();
	private SubMode submode = SubMode.Set;

	public void RemoveIndicator()
	{
		P1 = new Vector3(-1, -1, -1);
		if (P1_marker != null)
			Destroy(P1_marker);
	}

	public static bool IndicatorVisible()
    {
		return P1_marker != null;
    }

	public void SwitchSubMode(int mode)
	{
		submode = (SubMode)mode;
		SingleModeText.color = Color.white;
		SmoothModeText.color = Color.white;
		AmplifyModeText.color = Color.white;
		if (submode == SubMode.Set)
		{
			SingleModeText.color = Color_selected;
			transform.Find("s_Distortion").gameObject.SetActive(false);
		}
		else if (submode == SubMode.Avg)
		{
			SmoothModeText.color = Color_selected;
			transform.Find("s_Distortion").gameObject.SetActive(true);
		}
		else if (submode == SubMode.Amp)
		{
			AmplifyModeText.color = Color_selected;
			transform.Find("s_Distortion").gameObject.SetActive(false);
		}
	}

	void Start()
	{

	}

	public void OnDisable()
	{
		RemoveIndicator();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
			SwitchSubMode(0);
		else if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			SwitchSubMode(1);
			RemoveIndicator();
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			SwitchSubMode(2);
			RemoveIndicator();
		}
		if (!Input.GetKey(KeyCode.LeftControl) && !MouseInputUIBlocker.BlockedByUI && Consts.IsWithinMapBounds(Highlight.pos))
		{
			if (submode == SubMode.Set)
			{
				if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
					FreeElevation(); // single-action
				else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
					FreeElevation(); //auto-fire
				else if (Input.GetMouseButtonDown(1) && Highlight.over)
					RectangularElevation();
			}
			else if (submode == SubMode.Avg)
			{
				if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
					Smoothing(); // single-action
				else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
					Smoothing(); //auto-fire
				else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
					Distortion(); // single-action
				else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
					Distortion(); //auto-fire
			}
			else if (submode == SubMode.Amp)
			{
				if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
					Amplification(); // single-action
				else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
					Amplification(); //auto-fire
				if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
					Amplification(-1); // single-action
				else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
					Amplification(-1); //auto-fire
			}
		}
		if (Input.GetKeyDown(KeyCode.Tab))
			RadiusSlider.GetComponent<Slider>().value = 1;

		if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0))
		{
			UndoBuffer.ApplyTerrainOperation();
			InitialPos = new Vector3(-1, -1, -1);
		}
		if (Input.GetKeyDown(KeyCode.Escape)) //ESC deletes white indicator in Make_Elevation()
			RemoveIndicator();
	}

	private void FreeElevation()
	{
		// Highlight.pos is center vertex
		if (Highlight.pos.x != InitialPos.x || Highlight.pos.z != InitialPos.z)
		{ // vertex -> vertex
			InitialPos = Highlight.pos;
			TargetSetHeights.Clear();
		}
		HashSet<int> indexes = new HashSet<int>();
		for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
		{
			for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
			{
				if (Consts.IsWithinMapBounds(x, z))
				{
					int idx = Consts.PosToIndex((int)x, (int)z);
					Vector3 currentpos = Consts.IndexToPos(idx);
					float dist = Consts.Distance(currentpos, Highlight.pos);
					if (Mathf.Round(dist) > RadiusSlider.value - 1)
						continue;

					float TargetSetHeight;
					if (TargetSetHeights.ContainsKey(idx))
						TargetSetHeight = TargetSetHeights[idx];
					else
					{
						float Hdiff = Consts.SliderValue2RealHeight(HeightSlider.value) - currentpos.y;
						TargetSetHeight = currentpos.y + Hdiff
										  * Consts.Smoothstep(0, 1, (RadiusSlider.value - dist) / RadiusSlider.value);
						TargetSetHeights.Add(idx, TargetSetHeight);
					}
					Consts.former_heights[idx] += (TargetSetHeight - currentpos.y)
												  * Mathf.Pow(IntensitySlider.value * 5, 2) / 10000;
					Consts.current_heights[idx] = Consts.former_heights[idx];
					UndoBuffer.AddVertexPair(currentpos, Consts.IndexToPos(idx));
					indexes.Add(idx);
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		//Search for any tiles 
		RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z),
							new Vector3(RadiusSlider.value + .5f, 1, RadiusSlider.value + .5f),
								Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9);
		List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
		Build.UpdateTiles(hitsList);
	}

	private void RectangularElevation()
	{
		if (P1.x == -1)
		{
			// Get initial position and set znacznik there
			P1 = Highlight.pos;
			P1_marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
			P1_marker.transform.localScale = new Vector3(.25f, 1, .25f);
			P1_marker.transform.position = Highlight.pos;
		}
		else
		{
			// Time to get second position
			Vector3 P2 = Highlight.pos;
			Vector3 BL = new Vector3(Mathf.Min(P1.x, P2.x), 0, Mathf.Min(P1.z, P2.z));
			Vector3 TR = new Vector3(Mathf.Max(P1.x, P2.x), 0, Mathf.Max(P1.z, P2.z));
			HashSet<int> indexes = new HashSet<int>();
			for (float z = BL.z - (int)RadiusSlider.value; z <= TR.z + RadiusSlider.value; z++)
			{
				for (float x = BL.x - (int)RadiusSlider.value; x <= TR.x + RadiusSlider.value; x++)
				{
					if (Consts.IsWithinMapBounds(x, z))
					{
						int idx = Consts.PosToIndex((int)x, (int)z);
						Vector3 currentpos = Consts.IndexToPos(idx);
						if (x >= BL.x && x <= TR.x && z >= BL.z && z <= TR.z)
						{
							Consts.former_heights[idx] = Consts.SliderValue2RealHeight(HeightSlider.value);
						}
						else
						{
							Vector3 Closest = GetClosestEdgeVertex(currentpos, BL, TR);
							float dist = Consts.Distance(currentpos, Closest);
							if (Mathf.Round(dist) > RadiusSlider.value - 1)
								continue;
							float Hdiff = Consts.SliderValue2RealHeight(HeightSlider.value) - Consts.current_heights[idx];
							Consts.former_heights[idx] += Hdiff * Consts.Smoothstep(0, 1, (RadiusSlider.value - dist) / RadiusSlider.value);
						}
						Consts.current_heights[idx] = Consts.former_heights[idx];
						UndoBuffer.AddVertexPair(currentpos, Consts.IndexToPos(idx));
						indexes.Add(idx);
					}
				}
			}
			Consts.UpdateMapColliders(indexes);
			Vector3 center = new Vector3(0.5f * (P1.x + P2.x), Consts.MAX_H, 0.5f * (P1.z + P2.z));
			Vector3 bounds = new Vector3(0.5f * Mathf.Abs(P1.x - P2.x) + RadiusSlider.value + .5f, 1f,
										 0.5f * Mathf.Abs(P1.z - P2.z) + RadiusSlider.value + .5f);
			RaycastHit[] hits = Physics.BoxCastAll(center, bounds, Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9);
			Build.UpdateTiles(hits.Select(hit => hit.transform.gameObject).ToList());
			UndoBuffer.ApplyTerrainOperation();
			RemoveIndicator();
		}

		Vector3 GetClosestEdgeVertex(Vector3 v, Vector3 LD, Vector3 PG)
		{
			Vector3 result = new Vector3();
			if (v.x < LD.x)
				result.x = LD.x;
			else if (v.x > PG.x)
				result.x = PG.x;
			else
				result.x = v.x;

			if (v.z < LD.z)
				result.z = LD.z;
			else if (v.z > PG.z)
				result.z = PG.z;
			else
				result.z = v.z;
			return result;
		}
	}

	private void Smoothing()
	{
		HashSet<int> indexes = new HashSet<int>();
		for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
		{
			for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
			{
				if (Consts.IsWithinMapBounds(x, z))
				{
					int idx = Consts.PosToIndex((int)x, (int)z);
					Vector3 currentpos = Consts.IndexToPos(idx);
					float dist = Consts.Distance(currentpos, Highlight.pos);
					if (Mathf.Round(dist) > RadiusSlider.value - 1)
						continue;

					float height_sum = 0;
					for (float xx = currentpos.x - 1; xx <= currentpos.x + 1; xx++)
					{
						for (float zz = currentpos.z - 1; zz <= currentpos.z + 1; zz++)
						{
							if (xx == currentpos.x && zz == currentpos.z)
								continue;
							Vector3 neighbor_pos = new Vector3(xx, 0, zz);
							height_sum += Consts.current_heights[Consts.PosToIndex(neighbor_pos)];
						}
					}
					float avg = height_sum / 8f;

					Vector3 for_buffer = Consts.IndexToPos(idx);
					Consts.current_heights[idx] += (avg - Consts.current_heights[idx])
												   * Mathf.Pow(IntensitySlider.value * 5, 2) / 10000;
					Consts.former_heights[idx] = Consts.current_heights[idx];
					UndoBuffer.AddVertexPair(for_buffer, Consts.IndexToPos(idx));
					indexes.Add(idx);
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		//Search for any tiles 
		RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z),
							new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
							Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9);
		List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
		Build.UpdateTiles(hitsList);
	}

	private void Distortion()
	{
		float distort_val = Consts.SliderValue2RealHeight(DistortionSlider.value);
		if (Highlight.pos.x != InitialPos.x || Highlight.pos.z != InitialPos.z)
		{ // vertex -> vertex
			InitialPos = Highlight.pos;
			TargetDistortionValue = UnityEngine.Random.Range(-distort_val, distort_val);
			TargetDistortionHeights.Clear();
		}
		// Highlight.pos is center vertex
		HashSet<int> indexes = new HashSet<int>();
		for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
		{
			for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
			{
				if (Consts.IsWithinMapBounds(x, z))
				{
					int idx = Consts.PosToIndex((int)x, (int)z);
					Vector3 currentpos = Consts.IndexToPos(idx);
					float distance = Consts.Distance(currentpos, Highlight.pos);
					if (Mathf.Round(distance) > RadiusSlider.value - 1)
						continue;

					float TargetDistortionHeight;
					if (TargetDistortionHeights.ContainsKey(idx))
						TargetDistortionHeight = TargetDistortionHeights[idx];
					else
					{
						TargetDistortionHeight = currentpos.y + TargetDistortionValue
												* Consts.Smoothstep(0, 1, (RadiusSlider.value - distance) / RadiusSlider.value);
						TargetDistortionHeights.Add(idx, TargetDistortionHeight);
					}
					Vector3 for_buffer = Consts.IndexToPos(idx);

					Consts.current_heights[idx] += (TargetDistortionHeight - currentpos.y)
												   * Mathf.Pow(IntensitySlider.value * 5, 2) / 10000;
					Consts.former_heights[idx] = Consts.current_heights[idx];

					UndoBuffer.AddVertexPair(for_buffer, Consts.IndexToPos(idx));
					Consts.UpdateMapColliders(new HashSet<int> { idx });
					var tiles = Build.Get_surrounding_tiles(new HashSet<int> { idx });
					Build.UpdateTiles(tiles);
					indexes.Add(idx);
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		//Search for any tiles 
		RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z),
							new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
							Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9);
		List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
		Build.UpdateTiles(hitsList);
	}

	private void Amplification(int dir = 1)
	{
		// Highlight.pos is center vertex
		HashSet<int> indexes = new HashSet<int>();
		for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
		{
			for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
			{
				if (Consts.IsWithinMapBounds(x, z))
				{
					int idx = Consts.PosToIndex((int)x, (int)z);
					float heightdiff = Consts.current_heights[idx] - Consts.SliderValue2RealHeight(HeightSlider.value);
					float dist = Consts.Distance(Consts.IndexToPos(idx), Highlight.pos);
					if (Mathf.Round(dist) > RadiusSlider.value - 1)
						continue;
					Vector3 for_buffer = Consts.IndexToPos(idx);
					Consts.former_heights[idx] += dir * heightdiff * Mathf.Pow(IntensitySlider.value * 2, 2) / 20000
												  * Consts.Smoothstep(0, 1, (RadiusSlider.value - dist) / RadiusSlider.value);
					Consts.current_heights[idx] = Consts.former_heights[idx];
					UndoBuffer.AddVertexPair(for_buffer, Consts.IndexToPos(idx));
					indexes.Add(idx);
				}
			}
		}
		Consts.UpdateMapColliders(indexes);
		//Search for any tiles 
		RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Consts.MAX_H, Highlight.pos.z),
							new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
							Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9);
		List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
		Build.UpdateTiles(hitsList);
	}
}
