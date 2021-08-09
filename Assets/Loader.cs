using System.Collections;
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
	public GameObject MainCanvas;
	public GameObject TilesetContainer;
	public GameObject TabTemplate;
	public GameObject Tile1x1Template;
	public GameObject MainCamera;

	public Text LoadingScreen_text_logo;
	public Text LoadingScreen_progressbar;
	public GameObject loadScreen;

	public TextAsset flatters;
	public Material thismaterial;
	
	public GameObject editorPanel;
	public static bool Isloading = false;
	void InitializeCone()
	{
		Consts.Cone = GameObject.Find("cone");
		Consts.Cone = Consts.Cone.transform.GetChild(0).gameObject;
		Consts.Cone.layer = 13;
	}
	void EnableLoadingScreen()
	{
		LoadingScreen_text_logo.text = "3D editor " + Consts.VERSION;
		string nazwa = Mathf.RoundToInt(7 * UnityEngine.Random.value).ToString();
		loadScreen.SetActive(true);
		loadScreen.transform.Find(nazwa).gameObject.SetActive(true);

	}
	void DisableLoadingScreen()
	{
		loadScreen.SetActive(false);
		Isloading = false;
	}
	void Awake()
	{
		InitializeCone();
		EnableLoadingScreen();
		StartCoroutine(LoadTrackCoroutine());
	}
	IEnumerator LoadTrackCoroutine()
	{
		Consts.MissingTilesNames.Clear();

		yield return null;
		if (Isloading)
		{ // load track
			PopulateTilePlacementArray();
			InitializeHeightArrays(Consts.TRACK.Height, Consts.TRACK.Width);
			LoadTerrain(Consts.LoadMirrored);
		}
		else
		{ // generate track
			Consts.TRACK = new TrackSavable((ushort)SliderWidth.val, (ushort)SliderHeight.val);
			InitializeHeightArrays(Consts.TRACK.Height, Consts.TRACK.Width);
			InitializeTilePlacementArray(SliderHeight.val, SliderWidth.val);
			Consts.Trackname = "Untitled";
		}

		if (Consts.LoadMirrored)
			Consts.Trackname += " (mirrored)";

		MainCanvas.GetComponent<EditorMenu>().nazwa_toru.text = Consts.Trackname;
		MainCanvas.GetComponent<EditorMenu>().NameOfTrack.text = Consts.Trackname;
		CreateScatteredMeshColliders();

		if (Isloading)
			StartCoroutine(PlaceLoadedTilesOnMap());
		else
		{
			DisableLoadingScreen();
			MainCamera.SetActive(true);
		}

		CreateTilesetMenu();

		editorPanel.GetComponent<SliderCase>().SwitchToTileset(Consts.CHKPOINTS_STR);
	}
	void PopulateTilePlacementArray()
	{
		InitializeTilePlacementArray(Consts.TRACK.Height, Consts.TRACK.Width);
		// Load tiles layout from TRACK to TilePlacementArray
		for (int z = 0; z < Consts.TRACK.Height; z++)
		{
			for (int x = 0; x < Consts.TRACK.Width; x++)
			{
				TrackTileSavable tile = Consts.TRACK.TrackTiles[Consts.TRACK.Height - z - 1][x];
				//  tiles bigger than 1x1 have funny max uint numbers around center block. We ignore them as well as grass fields (FieldId = 0)  
				if (tile.FieldId < Consts.TRACK.FieldFiles.Count)
				{
					// without .cfl suffix
					string TileName = Consts.TRACK.FieldFiles[tile.FieldId].Substring(0, Consts.TRACK.FieldFiles[tile.FieldId].Length - 4);
					// ignore strange grass tiles
					if (TileName == "border1" || TileName == "border2" || TileName == "field")
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
	}
	void CreateTilesetMenu()
	{
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
		Consts.GravityValue = (int)(Consts.TRACK.Heightmap[0][0]);

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

	IEnumerator PlaceLoadedTilesOnMap()
	{
		List<GameObject> to_update = new List<GameObject>();
	
		LoadingScreen_progressbar.text = "Applying restrictions.. ";
		yield return null;
			for (int z = 0; z < Consts.TRACK.Height; z++)
			{
				for (int x = 0; x < Consts.TRACK.Width; x++)
				{
					if (Consts.TilePlacementArray[z, x].Name == null)
						continue;
					Vector3Int TLpos = new Vector3Int(4 * x, 0, 4 * (z + 1));

					to_update.Add(editorPanel.GetComponent<Build>().PlaceTile(TLpos,
							Consts.TilePlacementArray[z, x].Name, Consts.TilePlacementArray[z, x].Rotation,
							Consts.TilePlacementArray[z, x].Inversion, Consts.TilePlacementArray[z, x].Height));
				}
			}
		LoadingScreen_progressbar.text = "Placing tiles..";
		yield return null;
		// when PlaceTile tries to place a tile sticking out of map bounds (because of resizing), it returns null. Nulls have to be removed from to_update
		to_update.RemoveAll(go => go == null);
		Stopwatch sw = Stopwatch.StartNew();
		Stopwatch tw = Stopwatch.StartNew();
		for (int i = 0; i < to_update.Count; i++)
		{
			Build.UpdateTiles(new List<GameObject> { to_update[i] });
			if (sw.Elapsed.Seconds > 4)
			{
				LoadingScreen_progressbar.text = "Placing tiles.. " + (100 * i / to_update.Count).ToString() + " %";
				sw.Restart();
				yield return null;
			}
		}
		UnityEngine.Debug.Log("Loading time/s:" + to_update.Count / (float)tw.Elapsed.Seconds);
		Build.current_rmc = null;
		DisableLoadingScreen();
		MainCamera.SetActive(true);
	}
}
