﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// First submode of manual mode: Areal mode. Second one is Single mode
/// You switch between them with [Tab]
/// </summary>
public class ArealMode : MonoBehaviour
{
	private enum ArealModes { Immediate, Incremental, Amplify }
	private ArealModes CurrentMode;
	Color32 Color_selected = new Color32(219, 203, 178, 255);
	public Slider HeightSlider;
	public Slider IntensitySlider;
	public Slider DistortionSlider;
	public Slider RadiusSlider;
	public Button SingleModeButton;
	public Button SmoothModeButton;
	public Button AmplifyModeButton;
	private GameObject indicator;
	private int index;
	private Vector3 InitialPos;
	private Dictionary<int, float> TargetDistValues = new Dictionary<int, float>();
	private Dictionary<int, float> TargetSmoothValues = new Dictionary<int, float>();
	void RemoveIndicator()
	{
		index = 0;
		if (indicator != null)
			Destroy(indicator);
	}
	public void OnDisable()
	{
		RemoveIndicator();
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
			SwitchMode(0);
		else if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			SwitchMode(1);
			RemoveIndicator();
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3))
			SwitchMode(2);

		if (!Input.GetKey(KeyCode.LeftControl)) //if ctrl key wasn't pressed (height pickup)
		{
			if (CurrentMode == ArealModes.Immediate)
			{
				DistortionSlider.enabled = false;

				if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt))
					UndoBuffer.ApplyOperation();

				if (Input.GetKeyDown(KeyCode.Escape)) //ESC deletes white indicator in Make_Elevation()
				{
					RemoveIndicator();
				}
				if (!MouseInputUIBlocker.BlockedByUI)
				{
					if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
						Areal_vertex_manipulation(); // single-action
					else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
						Areal_vertex_manipulation(); //auto-fire
					else if (Input.GetMouseButtonDown(1) && Highlight.over)
						Make_areal_elevation();
				}
			}
			else if (CurrentMode == ArealModes.Incremental)
			{
				DistortionSlider.enabled = true;
				if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
				{
					if ((Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0)) && !Input.GetKey(KeyCode.LeftAlt))
						UndoBuffer.ApplyOperation();

					if (!MouseInputUIBlocker.BlockedByUI)
					{
						if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
							Areal_smoothing(); // single-action
						else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
							Areal_smoothing(); //auto-fire
						else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
							Areal_distortion(); // single-action
						else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
							Areal_distortion(); //auto-fire
					}
				}
			}
			else if (CurrentMode == ArealModes.Amplify)
			{
				DistortionSlider.enabled = false;
				if (!Input.GetKey(KeyCode.LeftControl)) //X ctrl_key_works()
				{
					if ((Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0)) && !Input.GetKey(KeyCode.LeftAlt))
						UndoBuffer.ApplyOperation();

					if (!MouseInputUIBlocker.BlockedByUI)
					{
						if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
							Areal_amp(); // single-action
						else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
							Areal_amp(); //auto-fire
						else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
							Areal_amp(-1); // single-action
						else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
							Areal_amp(-1); //auto-fire
					}
				}
			}
		}
	}
	// buttons use this function
	public void SwitchMode(float mode)
	{
		CurrentMode = (ArealModes)mode;
		SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		AmplifyModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		if (mode == 0)
			SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
		else if(mode == 1)
			SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
		else if (mode == 2)
			AmplifyModeButton.transform.GetChild(0).GetComponent<Text>().color = Color_selected;
	}
	private void Areal_distortion()
	{
		if (Service.IsWithinMapBounds(Highlight.pos))
		{
			if (Highlight.pos.x != InitialPos.x || Highlight.pos.z != InitialPos.z)
			{ // vertex -> vertex
				InitialPos = Highlight.pos;
				TargetDistValues.Clear();
			}
			// Highlight.pos is center vertex
			List<int> indexes = new List<int>();
			for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
			{
				for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
				{
					if (Service.IsWithinMapBounds(x, z))
					{
						int idx = Service.PosToIndex((int)x, (int)z);
						Vector3 currentpos = Service.IndexToPos(idx);
						float dist = Service.Distance(currentpos, Highlight.pos);
						if (dist > RadiusSlider.value)
							continue;
						Vector3 pos = Highlight.pos;
						float dist_val = Service.SliderValue2RealHeight(DistortionSlider.value);
						float h_val = Highlight.pos.y;//Service.SliderValue2RealHeight(HeightSlider.value);
						float TargetDistValue;
						if (TargetDistValues.ContainsKey(idx))
							TargetDistValue = TargetDistValues[idx];
						else
						{
							TargetDistValue = UnityEngine.Random.Range(h_val - dist_val, h_val + dist_val);
							TargetDistValues.Add(idx, TargetDistValue);
						}
						UndoBuffer.AddZnacznik((int)x, (int)z);
						Service.current_heights[idx] += (TargetDistValue - Service.current_heights[idx]) * IntensitySlider.value / 100f;
						Service.former_heights[idx] = Service.current_heights[idx];
						Service.UpdateMapColliders(new List<int> { idx });
						var tiles = Build.Get_surrounding_tiles(new List<int> { idx });
						Build.UpdateTiles(tiles);
						indexes.Add(idx);
					}
				}
			}
			Service.UpdateMapColliders(indexes);
			//Search for any tiles 
			RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Service.MAX_H, Highlight.pos.z),
																																										new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
																																											Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9);
			List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
			Build.UpdateTiles(hitsList);
		}
	}

	private void Areal_smoothing()
	{
		if (Service.IsWithinMapBounds(Highlight.pos))
		{
			// Highlight.pos is center vertex
			if (Highlight.pos.x != InitialPos.x || Highlight.pos.z != InitialPos.z)
			{ // vertex -> vertex
				InitialPos = Highlight.pos;
				TargetSmoothValues.Clear();
			}
			List<int> indexes = new List<int>();
			for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
			{
				for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
				{
					if (Service.IsWithinMapBounds(x, z))
					{
						int idx = Service.PosToIndex((int)x, (int)z);
						Vector3 currentpos = Service.IndexToPos(idx);
						float dist = Service.Distance(currentpos, Highlight.pos);
						if (dist > RadiusSlider.value)
							continue;
						UndoBuffer.AddZnacznik(currentpos);
						Vector3 pos = Highlight.pos;
						pos.y = Service.MAX_H;
						float avg;
						if (TargetSmoothValues.ContainsKey(idx))
							avg = TargetSmoothValues[idx];
						else
						{
							float height_sum = 0;
							for (int xx = -1; xx <= 1; xx++)
							{
								for (int zz = -1; zz <= 1; zz++)
								{
									if (xx == 0 && xx == zz)
										continue;
									pos.x = Highlight.pos.x + xx;
									pos.z = Highlight.pos.z + zz;
									if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 8))
										height_sum += hit.point.y;
								}
							}
							avg = height_sum / 8f;
							TargetSmoothValues.Add(idx, avg);
						}
						UndoBuffer.AddZnacznik((int)x, (int)z);
						Service.current_heights[idx] += (avg - Service.current_heights[idx]) * (IntensitySlider.value / 100f);
						Service.former_heights[idx] = Service.current_heights[idx];
						indexes.Add(idx);
					}
				}
			}
			Service.UpdateMapColliders(indexes);
			//Search for any tiles 
			RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Service.MAX_H, Highlight.pos.z),
																																										new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
																																											Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9);
			List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
			Build.UpdateTiles(hitsList);
		}
	}
	void Areal_amp(int ext_multiplier = 1)
	{
		if (Service.IsWithinMapBounds(Highlight.pos))
		{
			// Highlight.pos is center vertex
			List<int> indexes = new List<int>();
			for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
			{
				for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
				{
					if (Service.IsWithinMapBounds(x, z))
					{
						int idx = Service.PosToIndex((int)x, (int)z);
						Vector3 currentpos = Service.IndexToPos(idx);
						UndoBuffer.AddZnacznik(currentpos);
						Service.current_heights[idx] *= 1 + ext_multiplier * IntensitySlider.value / 100f;
						Service.former_heights[idx] = Service.current_heights[idx];
						indexes.Add(idx);
					}
				}
			}
			Service.UpdateMapColliders(indexes);
			//Search for any tiles 
			RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Service.MAX_H, Highlight.pos.z),
																																										new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
																																											Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9);
			List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
			Build.UpdateTiles(hitsList);
		}
	}
	void Areal_vertex_manipulation()
	{
		if (Service.IsWithinMapBounds(Highlight.pos))
		{
			// Highlight.pos is center vertex

			List<int> indexes = new List<int>();
			float MaxRadius = RadiusSlider.value * 1.41f;
			for (float z = Highlight.pos.z - RadiusSlider.value; z <= Highlight.pos.z + RadiusSlider.value; z++)
			{
				for (float x = Highlight.pos.x - RadiusSlider.value; x <= Highlight.pos.x + RadiusSlider.value; x++)
				{
					if (Service.IsWithinMapBounds(x, z))
					{
						int idx = Service.PosToIndex((int)x, (int)z);
						Vector3 currentpos = Service.IndexToPos(idx);
						float dist = Service.Distance(currentpos, Highlight.pos);
						if (dist > MaxRadius)
							continue;
						UndoBuffer.AddZnacznik(currentpos);
						float Hdiff = Service.SliderValue2RealHeight(HeightSlider.value) - Service.current_heights[idx];

						float fullpossibleheight = Hdiff * IntensitySlider.value / 100f;
						Service.former_heights[idx] += fullpossibleheight * Service.Smoothstep(0, 1, (MaxRadius - dist) / MaxRadius);
						Service.current_heights[idx] = Service.former_heights[idx];
						indexes.Add(idx);
					}
				}
			}
			Service.UpdateMapColliders(indexes);
			//Search for any tiles 
			RaycastHit[] hits = Physics.BoxCastAll(new Vector3(Highlight.pos.x, Service.MAX_H, Highlight.pos.z),
																																										new Vector3(RadiusSlider.value + 1, 1, RadiusSlider.value),
																																											Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9);
			List<GameObject> hitsList = hits.Select(hit => hit.transform.gameObject).ToList();
			Build.UpdateTiles(hitsList);
		}
	}
	void Make_areal_elevation()
	{
		if (index == 0)
		{
			// Get initial position and set znacznik there
			if (Service.IsWithinMapBounds(Highlight.pos))
			{
				index = (int)(Highlight.pos.x + 4 * Service.TRACK.Width * Highlight.pos.z + Highlight.pos.z);
				//Debug.Log("I1="+index+" "+m.vertices[index]+" pos="+highlight.pos);
				indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
				indicator.transform.localScale = new Vector3(.25f, 1, .25f);
				indicator.transform.position = Highlight.pos;
			}
		}
		else
		{
			// Time to get second position
			if (Service.IsWithinMapBounds(Highlight.pos))
			{
				int index2 = (int)(Highlight.pos.x + 4 * Service.TRACK.Width * Highlight.pos.z + Highlight.pos.z);
				Vector3Int a = Vector3Int.RoundToInt(Service.IndexToPos(index));
				Vector3Int b = Vector3Int.RoundToInt(Service.IndexToPos(index2));
				Vector3Int LD = new Vector3Int(Mathf.Min(a.x, b.x), 0, Mathf.Min(a.z, b.z));
				Vector3Int PG = new Vector3Int(Mathf.Max(a.x, b.x), 0, Mathf.Max(a.z, b.z));
				{
					List<int> indexes = new List<int>();
					int x_edge = PG.x - LD.x;
					int z_edge = PG.z - LD.z;
					for (int z = LD.z - (int)RadiusSlider.value; z <= PG.z + RadiusSlider.value; z++)
					{
						for (int x = LD.x - (int)RadiusSlider.value; x <= PG.x + RadiusSlider.value; x++)
						{
							if (Service.IsWithinMapBounds(x, z))
							{
								int idx = Service.PosToIndex(x, z);
								Vector3 currentpos = Service.IndexToPos(idx);
								if (x >= LD.x && x <= PG.x && z >= LD.z && z <= PG.z)
								{
									Service.former_heights[idx] = Service.SliderValue2RealHeight(HeightSlider.value);
								}
								else
								{
									Vector3 Closest = GetClosestPointOfEdgeOfSelection(currentpos, LD, PG);
									float dist = Service.Distance(currentpos, Closest);
									if (dist > RadiusSlider.value)
										continue;
									float Hdiff = Service.SliderValue2RealHeight(HeightSlider.value) - Service.current_heights[idx];
									Service.former_heights[idx] += Hdiff * Service.Smoothstep(0, 1, (RadiusSlider.value - dist) / RadiusSlider.value);
								}
								UndoBuffer.AddZnacznik(currentpos);
								Service.current_heights[idx] = Service.former_heights[idx];
								indexes.Add(idx);
							}
						}
					}
					Service.UpdateMapColliders(indexes);
				}
				Destroy(indicator);
				index = 0;
				RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Service.MAX_H, 0.5f * (a.z + b.z)),
						new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))),
						Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9); //Search for tiles
				Build.UpdateTiles(hits.Select(hit => hit.transform.gameObject).ToList());
				UndoBuffer.ApplyOperation();
			}
		}
	}
	private Vector3 GetClosestPointOfEdgeOfSelection(Vector3 v, Vector3 LD, Vector3 PG)
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
