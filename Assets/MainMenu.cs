using SFB;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// First launched script
// MainMenu.cs -> Loader.cs
// Loads mods, handles menu and loads .trk file pointed by player
public class MainMenu : MonoBehaviour
{
	public Text LoadingScreen_text_logo;
	public GameObject loadScreen;
	public GameObject LoadMenu;
	public GameObject ResizeMenu;
	public Text Resizemenu_Trackname;
	public Text Resizemenu_Size_str;
	public Text Resizemenu_Elements_str;
	public InputField ResizeMenu_Right;
	public InputField ResizeMenu_Left;
	public InputField ResizeMenu_Up;
	public InputField ResizeMenu_Down;
	public Toggle ResizeToggle;
	public Toggle MirroredToggle;
	public Button ManageTilesets_button;
	public ScrollRect ManageTilesets_ScrollView;
	/// <summary>Whether new track dimensions don't exceed TrackTileLimit</summary>
	public static bool CanCreateTrack = true;
	void Awake()
	{
		ResizeMenu_Right.text = "0";
		ResizeMenu_Left.text = "0";
		ResizeMenu_Up.text = "0";
		ResizeMenu_Down.text = "0";
		Service.LoadMirrored = false;
		Service.Isloading = false;
		// if we running this for the first time
		if (TileManager.TileListInfo.Count == 0)
		{
			TileManager.LoadTiles();
		}
		Populate_Manage_Tilesets_Menu();
	}
	private void Enable_manage_tiles_button()
	{
		ManageTilesets_button.transform.GetChild(0).GetComponent<Text>().color = Color.white;
		ManageTilesets_button.interactable = true;
	}
	private void Disable_manage_tiles_button()
	{
		ManageTilesets_button.transform.GetChild(0).GetComponent<Text>().color = Color.gray;
		ManageTilesets_button.interactable = false;
	}
	private void Populate_Manage_Tilesets_Menu()
	{
		// if there is any custom tileset (with workshopId)
		if (TileManager.TileListInfo.Where(t => t.Value.Custom_tileset_id != null).Any())
		{
			Enable_manage_tiles_button();

			GameObject manage_entry_template = ManageTilesets_ScrollView.content.transform.GetChild(0).gameObject;
			string[] mod_ids = TileManager.TileListInfo.Select(t => t.Value.Custom_tileset_id).Distinct().ToArray();
			foreach (var mod_id in mod_ids)
			{
				if (mod_id != null)
				{
					GameObject NewEntry = Instantiate(manage_entry_template, manage_entry_template.transform.parent);
					NewEntry.transform.Find("Id").GetComponent<Text>().text = mod_id;
					NewEntry.transform.Find("Sets").GetComponent<Text>().text = string.Join(", ", TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Value.TilesetName).Distinct().ToArray());

					// entry gameobject's name is workshopId
					NewEntry.name = mod_id;
					NewEntry.SetActive(true);
				}
			}
		}
	}
	public void RemoveJustUnzippedFolder(GameObject Id_GO)
	{
		string Mod_id = Id_GO.GetComponent<Text>().text;

		//remove custom tiles of this tileset
		string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == Mod_id).Select(t => t.Key).ToArray();
		foreach (var name in to_remove_names)
			TileManager.TileListInfo.Remove(name);

		//remove folder in moddata
		Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + Mod_id + "\\", true);

		// remove entry in menu
		DestroyImmediate(ManageTilesets_ScrollView.content.transform.Find(Mod_id).gameObject);

		if (ManageTilesets_ScrollView.content.childCount == 1) // if we only have empty invisible manage_entry_template remaining
		{
			Disable_manage_tiles_button();
		}

	}
	public void RemoveTileset(GameObject Id_GO)
	{
		string Mod_id = Id_GO.GetComponent<Text>().text;

		//remove custom tiles of this tileset
		string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == Mod_id).Select(t => t.Key).ToArray();
		foreach (var name in to_remove_names)
			TileManager.TileListInfo.Remove(name);

		//remove entry in tilesets.txt
		string[] lines_to_keep = File.ReadAllLines(Application.streamingAssetsPath + "\\tilesets.txt").Where(line => line != Mod_id).ToArray();
		File.WriteAllLines(Application.streamingAssetsPath + "\\tilesets.txt", lines_to_keep);

		//remove folder in moddata
		Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + Mod_id + "\\", true);

		// remove entry in menu
		DestroyImmediate(ManageTilesets_ScrollView.content.transform.Find(Mod_id).gameObject);

		if (ManageTilesets_ScrollView.content.childCount == 1) // if we only have empty invisible manage_entry_template remaining
		{
			// disable menu
			ManageTilesets_button.transform.GetChild(0).GetComponent<Text>().color = Color.gray;
			ManageTilesets_button.interactable = false;
		}
	}
	private void ChangeSceneToEditor()
	{
		if (CanCreateTrack)
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}
	}
	public void ToggleMirrored()
	{
		Service.LoadMirrored = MirroredToggle.isOn;
	}
	public void RemoveEntryAndContentFolder()
	{
		string contentpath = IO.GetCrashdayPath() + "\\data\\content\\";
		try
		{
			Directory.Delete(contentpath, true);
		}
		catch
		{ }
		IO.RemoveCrashdayPath();
		QuitGame();
	}
	public static void DeleteDirectory(string target_dir)
	{
		string[] files = Directory.GetFiles(target_dir);
		string[] dirs = Directory.GetDirectories(target_dir);

		foreach (string file in files)
		{
			File.SetAttributes(file, FileAttributes.Normal);
			File.Delete(file);
		}

		foreach (string dir in dirs)
		{
			DeleteDirectory(dir);
		}

		Directory.Delete(target_dir, false);
	}
	public void CreateNewTrack()
	{
		StartCoroutine("EnableLoadingScreen");
		ChangeSceneToEditor();
	}
	public void QuitGame()
	{
		Application.Quit();
	}
	public void LoadTrackToVariablesAndRunEditor()
	{
		string[] sourcepath = StandaloneFileBrowser.OpenFilePanel("Select track (.trk) ", LoadTrackPath(), "trk", false);
		if (sourcepath.Length == 0)
			return;
		else// player hasnt clicked 'cancel' button
		{
			string path = sourcepath[0];
			//Path can't have .trk suffix
			path = path.Substring(0, path.Length - 4);
			Service.Trackname = path.Substring(path.LastIndexOf('\\') + 1);
			SaveTrackPath(path);
			Service.TRACK = MapParser.ReadMap(path + ".trk");

			if (ResizeToggle.isOn)
			{
				LoadMenu.SetActive(false);
				ResizeMenu.SetActive(true);
				UpdateResizeMenu();
			}
			else
			{
				Service.Isloading = true;
				StartCoroutine("EnableLoadingScreen");
				ChangeSceneToEditor();
			}
		}
	}
	public void UpdateResizeMenu()
	{
		ResizeMenu_Right.text = ResizeMenu_Right.text == "" ? "0" : ResizeMenu_Right.text;
		ResizeMenu_Left.text = ResizeMenu_Left.text == "" ? "0" : ResizeMenu_Left.text;
		ResizeMenu_Up.text = ResizeMenu_Up.text == "" ? "0" : ResizeMenu_Up.text;
		ResizeMenu_Down.text = ResizeMenu_Down.text == "" ? "0" : ResizeMenu_Down.text;
		Resizemenu_Trackname.text = Service.Trackname;
		int newwidth = int.Parse(ResizeMenu_Right.text) + int.Parse(ResizeMenu_Left.text);
		int newheight = int.Parse(ResizeMenu_Up.text) + int.Parse(ResizeMenu_Down.text);
		Resizemenu_Size_str.text = "Size: " + (Service.TRACK.Width + newwidth) + " x " + (Service.TRACK.Height + newheight);
		Resizemenu_Elements_str.text = "Elements: " + (Service.TRACK.Width + newwidth) * (Service.TRACK.Height + newheight) + " / " + Service.MAX_ELEMENTS;
	}
	public void Resize_n_Load()
	{
		int newwidth = int.Parse(ResizeMenu_Right.text) + int.Parse(ResizeMenu_Left.text);
		int newheight = int.Parse(ResizeMenu_Up.text) + int.Parse(ResizeMenu_Down.text);
		if (Service.TRACK.Width + newwidth < 3 || Service.TRACK.Height + newheight < 3 || 
		 (Service.TRACK.Width + newwidth) * (Service.TRACK.Height + newheight) > Service.MAX_ELEMENTS)
			return;
		TrackSavable ResizedMap = new TrackSavable(Service.TRACK, int.Parse(ResizeMenu_Right.text), int.Parse(ResizeMenu_Left.text),
			int.Parse(ResizeMenu_Up.text), int.Parse(ResizeMenu_Down.text));
		
		Service.TRACK = ResizedMap;
		Service.Isloading = true;
		StartCoroutine("EnableLoadingScreen");
		ChangeSceneToEditor();
	}
	IEnumerator EnableLoadingScreen()
	{
		LoadingScreen_text_logo.text = "3D Editor " + Service.VERSION;
		string nazwa = Mathf.CeilToInt(8 * UnityEngine.Random.value).ToString();
		loadScreen.SetActive(true);
		loadScreen.transform.Find(nazwa).gameObject.SetActive(true);
		yield return null;
	}

	/// <summary>
	/// Saves latest path to StreamingAssets/Path.txt
	/// </summary>
	public static void SaveTrackPath(string path)
	{
		StreamWriter w = new StreamWriter(Application.dataPath + "/StreamingAssets/path.txt");
		w.WriteLine(path);
		w.Close();
	}
	/// <summary>
	/// Loads latest path from StreamingAssets/Path.txt
	/// </summary>
	/// <returns></returns>
	public static string LoadTrackPath()
	{
		StreamReader w = new StreamReader(Application.dataPath + "/StreamingAssets/path.txt");
		string LastTrackPath = w.ReadLine();
		w.Close();
		if (LastTrackPath == "")
			LastTrackPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
		return LastTrackPath;
	}
}
