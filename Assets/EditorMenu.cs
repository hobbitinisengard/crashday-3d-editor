using SFB;
using System.Collections;
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
	/// <summary>
	/// Text in upperPanel
	/// </summary>
	public Text nazwa_toru;
	public InputField NameOfTrack; // Canvas/savePANEL/NameofTrack
	public Text upperPanel_t_version;
	public Text SAVED_TEXT; //for feedback when quick-saved
	/// <summary>
	/// tile name we are currently placing in building mode
	/// </summary>

	public static string tile_name = "NULL";
	private void Start()
	{
		upperPanel_t_version.text = Consts.VERSION;
		floor1 = Resources.Load<Material>("floor1");
		if (Consts.MissingTilesNames.Count > 0)
		{
			MissingTilesPanel.SetActive(true);
			MissingTilesPanel_content.GetComponent<Text>().text = string.Join("\n", Consts.MissingTilesNames);
		}
	}
	private void OnEnable()
	{
		GravityEffectDropdown.value = Consts.GravityValue / 1000;
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
			UpdateTileSelectedWithCursor();
		if(Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.S))
		{
			QuickSave();
		}
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
	void QuickSave()
	{ 
		if (Consts.LoadLastFolderPath() == System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments))
			ToggleSaveMenu();
		else
			SaveMenu_works(true);
	}
	IEnumerator DisplayMessage()
	{
		SAVED_TEXT.gameObject.SetActive(true);
		yield return new WaitForSeconds(2);
		SAVED_TEXT.gameObject.SetActive(false);
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

	public void SetMarkerDimsX(string val)
	{
		try
		{
			Consts.MarkerBounds.x = int.Parse(val) > 5 ? int.Parse(val) : 5;
		}
		catch
		{

		}
	}
	public void SetMarkerDimsZ(string val)
	{
		try
		{
			Consts.MarkerBounds.y = int.Parse(val) > 5 ? int.Parse(val) : 5;
		}
		catch
		{

		}
	}

	public void Toggle_help()
	{
		help.SetActive(!help.activeSelf);
	}

	public void SaveMenu_works(bool Quicksave = false)
	{
		//Debug.Log("SaveMenu:" + path);
		if (NameOfTrack.text == "")
			return;
		string path;

		if (Quicksave)
   path = Consts.LoadLastFolderPath();
		else
		{
			string[] originalpath = StandaloneFileBrowser.OpenFolderPanel("Select folder to save this track in ..", Consts.LoadLastFolderPath(), false);
			path = originalpath[0];
		}

		Consts.SaveLastFolderPath(path);

		path += "\\" + NameOfTrack.text + ".trk";

		Consts.Trackname = NameOfTrack.text;
		nazwa_toru.text = NameOfTrack.text;
		// save currently set gravity value to static variable to remember last dropdown selection
		Consts.GravityValue = GravityEffectDropdown.value * 1000 * ((plusOn.isOn == true) ? 1 : -1);

		// Save terrain
		for (int y = 0; y <= 4 * Consts.TRACK.Height; y++)
		{
			for (int x = 0; x <= 4 * Consts.TRACK.Width; x++)
			{
				int i = x + 4 * y * Consts.TRACK.Width + y;
				Consts.TRACK.Heightmap[4 * Consts.TRACK.Height - y][x] = Consts.current_heights[i] * 5 + Consts.GravityValue;
				
			}
		}

		// Prepare track tiles arrays
		for (int z = 0; z < Consts.TRACK.Height; z++)
			for (int x = 0; x < Consts.TRACK.Width; x++)
				Consts.TRACK.TrackTiles[z][x].Set(0, 0, 0, 0);

		Consts.TRACK.FieldFiles.Clear();
		Consts.TRACK.FieldFiles.Add("field.cfl");
		Consts.TRACK.FieldFilesNumber = 1;
		List<QuarterData> QuartersToSave = new List<QuarterData>();
		// Save tiles
		for (int z = 0; z < Consts.TRACK.Height; z++)
		{
			for (int x = 0; x < Consts.TRACK.Width; x++)
			{
				if (Consts.TilePlacementArray[z, x].Name != null)
				{
					ushort fieldId = SetAndGetFieldId(Consts.TilePlacementArray[z, x].Name);
					byte mirror = (byte)(Consts.TilePlacementArray[z, x].Inversion ? 1 : 0);
					byte rotation = (byte)(Consts.TilePlacementArray[z, x].Rotation / 90);
					if (mirror == 1 && rotation != 0)
						rotation = (byte)(4 - rotation);
					byte height = Consts.TilePlacementArray[z, x].Height;
					Vector2Int dim = TileManager.GetRealDims(Consts.TilePlacementArray[z, x].Name, (rotation == 1 || rotation == 3) ? true : false);
					//Base part - Left Top
					Consts.TRACK.TrackTiles[Consts.TRACK.Height - 1 - z][x].Set(fieldId, rotation, mirror, height);
					//Left Bottom
					if (dim.y == 2)
						//Consts.TRACK.TrackTiles[Consts.TRACK.Height - 1 - z][x].Set(65471, rotacja, inwersja, height);
						QuartersToSave.Add(new QuarterData(Consts.TRACK.Height - z, x, 1, rotation, mirror, height));
					//Right top
					if (dim.x == 2)
						//  Consts.TRACK.TrackTiles[Consts.TRACK.Height - 1 - z + 1 - dim.y][x + 1].Set(65472, rotacja, inwersja, height);
						QuartersToSave.Add(new QuarterData(Consts.TRACK.Height - 1 - z, x + 1, 2, rotation, mirror, height));
					//Right bottom
					if (dim.x == 2 && dim.y == 2)
						//  Consts.TRACK.TrackTiles[Consts.TRACK.Height - 1 - z][x + 1].Set(65470, rotacja, inwersja, height);
						QuartersToSave.Add(new QuarterData(Consts.TRACK.Height - z, x + 1, 0, rotation, mirror, height));
				}
			}
		}
		foreach (QuarterData q in QuartersToSave)
		{
			try
			{
				if (Consts.TRACK.TrackTiles[q.Y][q.X].FieldId == 0)
					Consts.TRACK.TrackTiles[q.Y][q.X].Set(q.ID == 0 ? (ushort)65470 : q.ID == 1 ? (ushort)65471 : (ushort)65472, q.rotation, q.mirror, q.height);
			}
			catch
			{
				Debug.LogWarning("Index out of range: Y,X=" + q.Y + " " + q.X + " ");
			}
		}
		Debug.Log("saveto:" + path);
		MapParser.SaveMap(Consts.TRACK, path);
		save.SetActive(false);
		StartCoroutine(DisplayMessage());
	}
	ushort SetAndGetFieldId(string name)
	{
		name += ".cfl";
		for (ushort i = 0; i < Consts.TRACK.FieldFiles.Count; i++)
		{
			if (name == Consts.TRACK.FieldFiles[i])
				return i;
		}
		Consts.TRACK.FieldFiles.Add(name);
		Consts.TRACK.FieldFilesNumber++;
		return (ushort)(Consts.TRACK.FieldFilesNumber - 1);
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
		bool cast = Physics.Raycast(ShapeMenu.V(Highlight.pos), Vector3.down, out RaycastHit hit, Consts.RAY_H, 1 << 9);
		if (cast)
		{
			var tile = hit.transform.gameObject;
			var tiles = Build.Get_surrounding_tiles(tile);
			tiles.Add(tile);
			Build.UpdateTiles(tiles);
		}
	}
}


