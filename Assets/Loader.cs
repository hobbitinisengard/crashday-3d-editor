using System.Collections.Generic;
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

	void Awake()
	{
		Consts.MissingTilesNames.Clear();
		if (Consts.Isloading)
		{
			InitializeTilePlacementArray(Consts.TRACK.Height, Consts.TRACK.Width);
			// Load tiles layout from TRACK to TilePlacementArray
			for (int z = 0; z < Consts.TRACK.Height; z++)
			{
				for (int x = 0; x < Consts.TRACK.Width; x++)
				{
					TrackTileSavable tile = Consts.TRACK.TrackTiles[Consts.TRACK.Height - z - 1][x];
					//  tiles bigger than 1x1 have funny max uint numbers around center block. We ignore them as well as grass fields (FieldId = 0)  
					if (tile.FieldId < Consts.TRACK.FieldFiles.Count && tile.FieldId != 0)
					{
						// without .cfl suffix
						string TileName = Consts.TRACK.FieldFiles[tile.FieldId].Substring(0, Consts.TRACK.FieldFiles[tile.FieldId].Length - 4);
						// ignore strange grass tiles
						if (TileName == "border1" || TileName == "border2")
							continue;
						// Don't load tiles that aren't in tile database
						if (!TileManager.TileListInfo.ContainsKey(TileName))
						{
							if (!Consts.MissingTilesNames.Contains(TileName))
								Consts.MissingTilesNames.Add(TileName);
							continue;
						}
						int Rotation = tile.Rotation * 90;
						bool Inversion = tile.IsMirrored == 0 ? false : true;
						//Inversed tiles rotate anti-clockwise so..
						if (Inversion && Rotation != 0)
							Rotation = 360 - Rotation;
						byte Height = tile.Height;
						Vector2Int dim = TileManager.GetRealDims(TileName, (Rotation == 90 || Rotation == 270) ? true : false);
						if (!Consts.LoadMirrored)
						{
							Consts.TilePlacementArray[z, x].Set(TileName, Rotation, Inversion, Height);
						}
						else
							Consts.TilePlacementArray[z, Consts.TRACK.Width - 1 - x - dim.x + 1].Set(
									TileName, 360 - Rotation, !Inversion, Height);
					}
				}
			}
			InitializeHeightArrays(Consts.TRACK.Height, Consts.TRACK.Width);
			LoadTerrain(Consts.LoadMirrored);
		}
		else
		{ // load new track
			Consts.TRACK = new TrackSavable((ushort)SliderWidth.val, (ushort)SliderHeight.val);
			InitializeHeightArrays(Consts.TRACK.Height, Consts.TRACK.Width);
			InitializeTilePlacementArray(SliderHeight.val, SliderWidth.val);
			Consts.Trackname = "Untitled";
		}

		if (Consts.LoadMirrored)
			Consts.Trackname += " (mirrored)";

		nazwa_toru.text = Consts.Trackname;
		CreateScatteredMeshColliders();

		if (Consts.Isloading)
			PlaceLoadedTilesOnMap();
		else
			Consts.Isloading = false;

		// Fills sliderCase HeightSlider with tabs. This includes custom tilesets.
		// -----------------------------------------------------------------
		// Create empty tileset tabs
		string[] Tilesets = TileManager.TileListInfo.Select(t => t.Value.TilesetName).Distinct().ToArray();
		Tilesets = Tilesets.Where(t => t != null).ToArray();
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
				if (t.Value.Model == null)
					continue;
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
		editorPanel.GetComponent<SliderCase>().SwitchToTileset(Consts.CHKPOINTS_STR);
	}
	private void InitializeHeightArrays(int Height, int Width)
	{
		// initialize height arrays
		Consts.former_heights = Enumerable.Repeat(0f, (4 * Height + 1) * (4 * Width + 1)).ToArray();
		Consts.current_heights = Enumerable.Repeat(0f, (4 * Height + 1) * (4 * Width + 1)).ToArray();
	}

	private void InitializeTilePlacementArray(int height, int width)
	{
		Consts.TilePlacementArray = new TilePlacement[height, width];
		for (int z = 0; z < height; z++)
		{
			for (int x = 0; x < width; x++)
			{
				Consts.TilePlacementArray[z, x] = new TilePlacement();
			}
		}
	}

	/// <summary>
	///Loads terrain from Consts.TRACK to current_heights and former_heights
	/// </summary>
	void LoadTerrain(bool LoadMirrored)
	{
		Consts.GravityValue = (int)(Consts.TRACK.Heightmap[0][0] * 5f);

		for (int z = 0; z <= 4 * Consts.TRACK.Height; z++)
		{
			for (int x = 0; x <= 4 * Consts.TRACK.Width; x++)
			{
				int i;
				if (LoadMirrored)
					i = 4 * Consts.TRACK.Width - x + z * (4 * Consts.TRACK.Width + 1);
				else
					i = x + z * (4 * Consts.TRACK.Width + 1);

				Consts.current_heights[i] = Consts.TRACK.Heightmap[4 * Consts.TRACK.Height - z][x] / 5f - Consts.TRACK.Heightmap[0][0] / 5f;
				Consts.former_heights[i] = Consts.current_heights[i];
			}
		}
	}
	/// <summary>
	/// Creates 5x5 vertices 1x1 grass
	/// </summary>
	void CreateScatteredMeshColliders()
	{
		Mesh basic = Resources.Load<Mesh>("rmcs/basic");
		for (int z = 0; z < Consts.TRACK.Height; z++)
		{
			for (int x = 0; x < Consts.TRACK.Width; x++)
			{
				GameObject element = new GameObject("Map " + x + " " + z);
				element.transform.position = new Vector3Int(4 * x, 0, 4 * z);
				MeshCollider mc = element.AddComponent<MeshCollider>();
				MeshFilter mf = element.AddComponent<MeshFilter>();
				MeshRenderer mr = element.AddComponent<MeshRenderer>();
				mr.material = thismaterial;
				mf.mesh = Instantiate(basic);
				mc.sharedMesh = Instantiate(basic);
				Consts.UpdateMapColliders(new List<GameObject> { element });
				element.layer = 8;
			}
		}
	}

	void PlaceLoadedTilesOnMap()
	{
		Stopwatch sw = Stopwatch.StartNew();
		int elements = 0;
		List<GameObject> to_update = new List<GameObject>();
		for (int z = 0; z < Consts.TRACK.Height; z++)
		{
			for (int x = 0; x < Consts.TRACK.Width; x++)
			{
				if (Consts.TilePlacementArray[z, x].Name == null)
					continue;
				Vector3Int TLpos = new Vector3Int(4 * x, 0, 4 * (z + 1));
				elements++;
				to_update.Add(editorPanel.GetComponent<Build>().PlaceTile(TLpos,
						Consts.TilePlacementArray[z, x].Name, Consts.TilePlacementArray[z, x].Rotation,
						Consts.TilePlacementArray[z, x].Inversion, Consts.TilePlacementArray[z, x].Height));
			}
		}
		// when PlaceTile tries to place a tile sticking out of map bounds (because of resizing), it returns null. Nulls have to be removed from to_update
		to_update.RemoveAll(go => go == null);
		Build.UpdateTiles(to_update);
		Build.current_rmc = null;
		Consts.Isloading = false;
		sw.Stop();
		if (sw.Elapsed.Seconds != 0)
			UnityEngine.Debug.Log("Loading time [elements/s]" + elements / Mathf.Round(sw.Elapsed.Seconds));
	}
}
