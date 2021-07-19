using SFB;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//Handles all variables essential for editor
public class DuVecInt
{
	/// <summary>
	/// BL Position
	/// </summary>
	public Vector2Int pos;
	public Vector2Int dims;
	public DuVecInt(Vector2Int POS, Vector2Int DIMZ)
	{
		pos = POS;
		dims = DIMZ;
	}
}

public class EditorMenu : MonoBehaviour
{
	private Material floor1;
	public EventSystem eventsystem;
	public GameObject save;
	public GameObject help;
	public GameObject MissingTilesPanel;
	public GameObject MissingTilesPanel_content;
	public GameObject editorPANEL;
	public GameObject formPANEL;
	public Toggle plusOn;
	public Dropdown GravityEffectDropdown;
	public InputField NameOfTrack; // Canvas/savePANEL/NameofTrack
	public Text upperPanel_t_version;
	/// <summary>
	/// tile name we are currently placing in building mode
	/// </summary>

	public static string tile_name = "NULL";
	private void Start()
	{
		upperPanel_t_version.text = Service.VERSION;
		floor1 = Resources.Load<Material>("floor1");
		if (Service.MissingTilesNames.Count > 0)
		{
			MissingTilesPanel.SetActive(true);
			MissingTilesPanel_content.GetComponent<Text>().text = string.Join("\n", Service.MissingTilesNames);
		}
	}
	private void OnEnable()
	{
		GravityEffectDropdown.value = Service.GravityValue / 1000;
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
			UpdateTileSelectedWithCursor();
		if (Input.GetKeyDown(KeyCode.F5))
		{
			ToggleSaveMenu();
		}
		if (!save.activeSelf)
		{
			if (Input.GetKeyDown(KeyCode.U))
			{
				Toggle_floor1Shader();
			}
			if (Input.GetKeyDown(KeyCode.H))
			{
				Toggle_help();
			}
			if (Input.GetMouseButtonDown(2))
			{//MMB przełącza teren/budowanie
				SwitchPanels();
			}
		}
	}
	void SwitchPanels()
	{
		editorPANEL.SetActive(!editorPANEL.activeSelf);
		formPANEL.SetActive(!formPANEL.activeSelf);
	}
	public void ToggleSaveMenu()
	{
		save.SetActive(!save.activeSelf);
	}
	private void Toggle_floor1Shader(Shader ForceShader = null)
	{
		if (ForceShader != null)
		{
			floor1.shader = ForceShader;
			return;
		}
		if (floor1.shader == Shader.Find("Mobile/Bumped Diffuse"))
			floor1.shader = Shader.Find("Transparent/Bumped Diffuse");
		else
			floor1.shader = Shader.Find("Mobile/Bumped Diffuse");
	}

	public void EditorToMenu()
	{
		Toggle_floor1Shader(Shader.Find("Mobile/Bumped Diffuse"));
		
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
	}
	void Awake()
	{
		NameOfTrack.text = GetComponent<Loader>().nazwa_toru.text;
	}

	public void SetMarkerDimsX(string val)
	{
		try
		{
			Service.MarkerBounds.x = int.Parse(val) > 5 ? int.Parse(val) : 5;
		}
		catch
		{

		}
	}
	public void SetMarkerDimsZ(string val)
	{
		try
		{
			Service.MarkerBounds.y = int.Parse(val) > 5 ? int.Parse(val) : 5;
		}
		catch
		{

		}
	}

	public void Toggle_help()
	{
		help.SetActive(!help.activeSelf);
	}

	public void SaveMenu_works()
	{
		string[] originalpath = StandaloneFileBrowser.OpenFolderPanel("Select folder to save this track in ..", MainMenu.LoadTrackPath(), false);
		string path = originalpath[0];
		if (path == "")
			return;
		MainMenu.SaveTrackPath(path);
		Service.Trackname = NameOfTrack.text;
		GetComponent<Loader>().nazwa_toru.text = Service.Trackname;
		// save currently set gravity value to static variable to remember last dropdown selection
		Service.GravityValue = GravityEffectDropdown.value * 1000 * ((plusOn.isOn == true) ? 1 : -1);

		// Save terrain
		for (int y = 0; y <= 4 * Service.TRACK.Height; y++)
		{
			for (int x = 0; x <= 4 * Service.TRACK.Width; x++)
			{
				int i = x + 4 * y * Service.TRACK.Width + y;
				Service.TRACK.Heightmap[4 * Service.TRACK.Height - y][x] = Service.current_heights[i] * 5 + Service.GravityValue / 5f;
				
			}
		}

		// Prepare track tiles arrays
		for (int z = 0; z < Service.TRACK.Height; z++)
			for (int x = 0; x < Service.TRACK.Width; x++)
				Service.TRACK.TrackTiles[z][x].Set(0, 0, 0, 0);

		Service.TRACK.FieldFiles.Clear();
		Service.TRACK.FieldFiles.Add("field.cfl");
		Service.TRACK.FieldFilesNumber = 1;
		List<QuarterData> QuartersToSave = new List<QuarterData>();
		// Save tiles
		for (int z = 0; z < Service.TRACK.Height; z++)
		{
			for (int x = 0; x < Service.TRACK.Width; x++)
			{
				if (Service.TilePlacementArray[z, x].Name != null)
				{
					ushort fieldId = SetAndGetFieldId(Service.TilePlacementArray[z, x].Name);
					byte mirror = (byte)(Service.TilePlacementArray[z, x].Inversion ? 1 : 0);
					byte rotation = (byte)(Service.TilePlacementArray[z, x].Rotation / 90);
					if (mirror == 1 && rotation != 0)
						rotation = (byte)(4 - rotation);
					byte height = Service.TilePlacementArray[z, x].Height;
					Vector2Int dim = TileManager.GetRealDims(Service.TilePlacementArray[z, x].Name, (rotation == 1 || rotation == 3) ? true : false);
					//Base part - Left Top
					Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z][x].Set(fieldId, rotation, mirror, height);
					//Left Bottom
					if (dim.y == 2)
						//Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z][x].Set(65471, rotacja, inwersja, height);
						QuartersToSave.Add(new QuarterData(Service.TRACK.Height - z, x, 1, rotation, mirror, height));
					////Right top
					if (dim.x == 2)
						//  Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z + 1 - dim.y][x + 1].Set(65472, rotacja, inwersja, height);
						QuartersToSave.Add(new QuarterData(Service.TRACK.Height - 1 - z, x + 1, 2, rotation, mirror, height));
					////Right bottom
					if (dim.x == 2 && dim.y == 2)
						//  Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z][x + 1].Set(65470, rotacja, inwersja, height);
						QuartersToSave.Add(new QuarterData(Service.TRACK.Height - z, x + 1, 0, rotation, mirror, height));
				}
			}
		}
		foreach (QuarterData q in QuartersToSave)
		{
			try
			{
				if (Service.TRACK.TrackTiles[q.Y][q.X].FieldId == 0)
					Service.TRACK.TrackTiles[q.Y][q.X].Set(q.ID == 0 ? (ushort)65470 : q.ID == 1 ? (ushort)65471 : (ushort)65472, q.rotation, q.mirror, q.height);
			}
			catch
			{
				Debug.LogWarning("Index out of range: Y,X=" + q.Y + " " + q.X + " ");
			}
		}
		MapParser.SaveMap(Service.TRACK, path + "\\" + Service.Trackname + ".trk");
	}
	ushort SetAndGetFieldId(string name)
	{
		name += ".cfl";
		for (ushort i = 0; i < Service.TRACK.FieldFiles.Count; i++)
		{
			if (name == Service.TRACK.FieldFiles[i])
				return i;
		}
		Service.TRACK.FieldFiles.Add(name);
		Service.TRACK.FieldFilesNumber++;
		return (ushort)(Service.TRACK.FieldFilesNumber - 1);
	}

	private struct QuarterData
	{
		public int Y;
		public int X;
		public byte ID;
		public byte rotation;
		public byte mirror;
		public byte height;

		public QuarterData(int v1, int v2, int v3, byte rotation, byte mirror, byte height)
		{
			this.Y = v1;
			this.X = v2;
			this.ID = (byte)v3;
			this.rotation = rotation;
			this.mirror = mirror;
			this.height = height;
		}
	}
	private void UpdateTileSelectedWithCursor()
	{
		bool cast = Physics.Raycast(Service.V(Highlight.pos), Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 9);
		if (cast)
		{
			var tile = hit.transform.gameObject;
			var tiles = Build.Get_surrounding_tiles(tile);
			tiles.Add(tile);
			Build.UpdateTiles(tiles);
		}
	}
}
