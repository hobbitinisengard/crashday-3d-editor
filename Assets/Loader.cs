﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// 2nd script. First in editor scene.
/// <summary>
/// Takes care of creating and loading track
/// </summary>
public class Loader : MonoBehaviour
{
	public GameObject TilesetContainer;
	public GameObject TabTemplate;
	public GameObject Tile1x1Template;
	public TextAsset flatters;
	public Material thismaterial;
	/// <summary>
	/// Text in upperPanel
	/// </summary>
	public Text nazwa_toru;
	public GameObject editorPanel;

	public static float multiplier = 1;

	void Awake()
	{
		if (Service.Isloading)
		{
			Service.MissingTilesNames.Clear();
			InitializeTilePlacementArray(Service.TRACK.Height, Service.TRACK.Width);
			// Load tiles layout from TRACK to TilePlacementArray
			for (int z = 0; z < Service.TRACK.Height; z++)
			{
				for (int x = 0; x < Service.TRACK.Width; x++)
				{
					TrackTileSavable tile = Service.TRACK.TrackTiles[Service.TRACK.Height - z - 1][x];
					//  tiles bigger than 1x1 have funny max uint numbers around center block. We ignore them as well as grass fields (FieldId = 0)  
					if (tile.FieldId < Service.TRACK.FieldFiles.Count && tile.FieldId != 0)
					{
						// without .cfl suffix
						string TileName = Service.TRACK.FieldFiles[tile.FieldId].Substring(0, Service.TRACK.FieldFiles[tile.FieldId].Length - 4);
						// ignore strange grass tiles
						if (TileName == "border1" || TileName == "border2")
							continue;
						// Don't load tiles that aren't in tile database
						if (!TileManager.TileListInfo.ContainsKey(TileName))
						{
							if (!Service.MissingTilesNames.Contains(TileName))
								Service.MissingTilesNames.Add(TileName);
							continue;
						}
						int Rotation = tile.Rotation * 90;
						bool Inversion = tile.IsMirrored == 0 ? false : true;
						//Inversed tiles rotate anti-clockwise so..
						if (Inversion && Rotation != 0)
							Rotation = 360 - Rotation;
						byte Height = tile.Height;
						Vector2Int dim = TileManager.GetRealDims(TileName, (Rotation == 90 || Rotation == 270) ? true : false);
						if (!Service.LoadMirrored)
						{
							Service.TilePlacementArray[z, x].Set(TileName, Rotation, Inversion, Height);
						}
						else
							Service.TilePlacementArray[z, Service.TRACK.Width - 1 - x - dim.x + 1].Set(
									TileName, 360 - Rotation, !Inversion, Height);
					}
				}
			}
			InitializeHeightArrays(Service.TRACK.Height, Service.TRACK.Width);
			LoadTerrain(Service.LoadMirrored);
		}
		else
		{ // load new track
			Service.TRACK = new TrackSavable((ushort)SliderWidth.val, (ushort)SliderHeight.val);
			InitializeHeightArrays(Service.TRACK.Height, Service.TRACK.Width);
			InitializeTilePlacementArray(SliderHeight.val, SliderWidth.val);
			Service.UntitledString = "Untitled";
		}

		if (Service.LoadMirrored)
			Service.UntitledString += " (mirrored)";

		nazwa_toru.text = Service.UntitledString;
		CreateScatteredMeshColliders();

		if (Service.Isloading)
			PlaceLoadedTilesOnMap();
		else
			Service.Isloading = false;

		// Fills sliderCase HeightSlider with tabs. This includes custom tilesets.
		// -----------------------------------------------------------------
		// Create empty tileset tabs
		string[] Tilesets = TileManager.TileListInfo.Select(t => t.Value.TilesetName).Distinct().ToArray();
		editorPanel.GetComponent<SliderCase>().InitializeSlider(Tilesets);
		foreach (var tileset_name in Tilesets)
		{
			GameObject NewTab = Instantiate(TabTemplate, TilesetContainer.transform);
			// tabs' name are the same as tilesets' names
			NewTab.name = tileset_name;
		}
		// Populate created tabs with tiles
		foreach (var t in TileManager.TileListInfo)
		{
			try
			{
				GameObject NewTile = Instantiate(Tile1x1Template, TilesetContainer.transform.Find(t.Value.TilesetName).GetComponent<ScrollRect>().content);
				NewTile.transform.GetChild(0).GetComponent<Image>().sprite = Sprite.Create(t.Value.Icon,
						new Rect(Vector2.zero, new Vector2(t.Value.Icon.width, t.Value.Icon.height)), Vector2.zero);
				NewTile.transform.GetChild(0).localScale = new Vector3(0.92f, 0.92f, 1);
				NewTile.name = t.Key;
				NewTile.transform.GetChild(0).name = t.Key;
				NewTile.AddComponent<ShowTileName>();
				NewTile.SetActive(true);
			}
			catch
			{

			}
		}
		editorPanel.GetComponent<SliderCase>().SwitchToTileset(Service.CheckpointString);
	}
	private void InitializeHeightArrays(int Height, int Width)
	{
		// initialize height arrays
		Service.former_heights = Enumerable.Repeat(0f, (4 * Height + 1) * (4 * Width + 1)).ToArray();
		Service.current_heights = Enumerable.Repeat(0f, (4 * Height + 1) * (4 * Width + 1)).ToArray();
	}

	private void InitializeTilePlacementArray(int height, int width)
	{
		Service.TilePlacementArray = new TilePlacement[height, width];
		for (int z = 0; z < height; z++)
		{
			for (int x = 0; x < width; x++)
			{
				Service.TilePlacementArray[z, x] = new TilePlacement();
			}
		}
	}

	/// <summary>
	///Loads terrain from Service.TRACK to current_heights and former_heights
	/// </summary>
	void LoadTerrain(bool LoadMirrored)
	{
		Service.GravityValue = (int)(Service.TRACK.Heightmap[0][0] * 5f);

		for (int z = 0; z < 4 * Service.TRACK.Height + 1; z++)
		{
			for (int x = 0; x < 4 * Service.TRACK.Width + 1; x++)
			{
				int i;
				if (LoadMirrored)
					i = 4 * Service.TRACK.Width - x + z * (4 * Service.TRACK.Width + 1);
				else
					i = x + z * (4 * Service.TRACK.Width + 1);

				Service.current_heights[i] = multiplier * Service.TRACK.Heightmap[4 * Service.TRACK.Height - z][x] / 5f - Service.TRACK.Heightmap[0][0] / 5f;
				Service.former_heights[i] = Service.current_heights[i];
			}
		}
	}
	/// <summary>
	/// Creates 5x5 vertices 1x1 grass
	/// </summary>
	void CreateScatteredMeshColliders()
	{
		Mesh basic = Resources.Load<Mesh>("rmcs/basic");
		for (int z = 0; z < Service.TRACK.Height; z++)
		{
			for (int x = 0; x < Service.TRACK.Width; x++)
			{
				GameObject element = new GameObject("Map " + x + " " + z);
				element.transform.position = new Vector3Int(4 * x, 0, 4 * z);
				MeshCollider mc = element.AddComponent<MeshCollider>();
				MeshFilter mf = element.AddComponent<MeshFilter>();
				MeshRenderer mr = element.AddComponent<MeshRenderer>();
				mr.material = thismaterial;
				mf.mesh = Instantiate(basic);
				mc.sharedMesh = Instantiate(basic);
				Service.UpdateMapColliders(new List<GameObject> { element });
				element.layer = 8;
			}
		}
	}

	void PlaceLoadedTilesOnMap()
	{
		Stopwatch sw = Stopwatch.StartNew();
		int elements = 0;
		List<GameObject> to_update = new List<GameObject>();
		for (int z = 0; z < Service.TRACK.Height; z++)
		{
			for (int x = 0; x < Service.TRACK.Width; x++)
			{
				if (Service.TilePlacementArray[z, x].Name == null)
					continue;
				Vector3Int TLpos = new Vector3Int(4 * x, 0, 4 * (z + 1));
				elements++;
				to_update.Add(editorPanel.GetComponent<Build>().PlaceTile(TLpos,
						Service.TilePlacementArray[z, x].Name, Service.TilePlacementArray[z, x].Rotation,
						Service.TilePlacementArray[z, x].Inversion, Service.TilePlacementArray[z, x].Height));
			}
		}
		Build.UpdateTiles(to_update);
		Build.current_rmc = null;
		Service.Isloading = false;
		sw.Stop();
		if (sw.Elapsed.Seconds != 0)
			UnityEngine.Debug.Log("Loading time [elements/s]" + elements / sw.Elapsed.Seconds);
	}
}
