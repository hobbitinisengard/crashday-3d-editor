using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
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
		Initialize_Documents_Folder();
		ResizeMenu_Right.text = "0";
		ResizeMenu_Left.text = "0";
		ResizeMenu_Up.text = "0";
		ResizeMenu_Down.text = "0";
		Consts.LoadMirrored = false;
		Loader.Isloading = false;
		// if we running this for the first time
		if (TileManager.TileListInfo.Count == 0)
		{
			TileManager.LoadTiles();
		}
		Populate_Manage_Tilesets_Menu();
	}
	void Initialize_Documents_Folder()
	{
		if (!Directory.Exists(Consts.documents_3deditor_path))
		{
			Directory.CreateDirectory(Consts.documents_3deditor_path);
			File.Create(Consts.path_path).Dispose();
			File.WriteAllLines(Consts.path_path, new string[] { Consts.documents_3deditor_path });
			File.Create(Consts.userdata_path).Dispose();
			File.WriteAllLines(Consts.userdata_path, new string[] { "0" });
			File.Create(Consts.tilesets_path).Dispose();
			
		}
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
		if (TileManager.CustomTileSections.Count > 0)
		{
			Enable_manage_tiles_button();

			GameObject manage_entry_template = ManageTilesets_ScrollView.content.transform.GetChild(0).gameObject;
			string[] mod_ids = TileManager.CustomTileSections.Keys.ToArray();

			for (int i = 0; i < mod_ids.Length; i++)
			{
				GameObject NewEntry = Instantiate(manage_entry_template, manage_entry_template.transform.parent);
				SwitchAppearance(NewEntry, mod_ids[i], TileManager.CustomTileSections[mod_ids[i]].Enabled);
				NewEntry.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					NewEntry.GetComponent<RectTransform>().anchoredPosition.x,
					i * -(NewEntry.GetComponent<RectTransform>().rect.height + 5) - 5);
				NewEntry.name = mod_ids[i];
				NewEntry.SetActive(true);
			}
		}
	}
	public void UpdateTileset(GameObject Id_GO)
	{
		string mod_id = Id_GO.GetComponent<Text>().text;
		bool enabled = TileManager.CustomTileSections[mod_id].Enabled;
		GameObject Entry = ManageTilesets_ScrollView.content.transform.Find(mod_id).gameObject;

		// Remove this tileset and its custom tiles from the database 
		string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
		foreach (var name in to_remove_names)
			TileManager.TileListInfo.Remove(name);
		TileManager.CustomTileSections.Remove(mod_id);

		// Remove folder in moddata
		Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\", true);

		// Unpack and load updated tileset
		PackageManager.LoadCPK(Directory.GetFiles(TileManager.CdWorkshopPath + mod_id).First(), mod_id);
		TileManager.ReadCatFiles(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\content\\editor\\", mod_id, enabled);
		if (enabled)
			TileManager.ReadCflFiles(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\content\\tiles\\", mod_id);

		// Update tile sections in the menu
		List<string> sections = TileManager.CustomTileSections[mod_id].TileSections;
		Entry.transform.Find("Sets").GetComponent<Text>().text = string.Join(", ", sections.ToArray());
		Entry.SetActive(false);
		Entry.SetActive(true);
	}

	public void RemoveTileset(GameObject Id_GO)
	{
		string mod_id = Id_GO.GetComponent<Text>().text;
		GameObject Entry = ManageTilesets_ScrollView.content.transform.Find(mod_id).gameObject;

		// Remove this tileset and its custom tiles from the database 
		string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
		foreach (var name in to_remove_names)
			TileManager.TileListInfo.Remove(name);
		TileManager.CustomTileSections.Remove(mod_id); ;

		// Remove entry in tilesets.txt
		string[] lines_to_keep = File.ReadAllLines(Consts.tilesets_path)
			.Where(line => line != mod_id && line != "#" + mod_id).ToArray();
		File.WriteAllLines(Consts.tilesets_path, lines_to_keep);

		// Remove folder in moddata
		Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\", true);

		// Update tileset menu
		for (int i = Entry.transform.GetSiblingIndex() + 1;
			i < ManageTilesets_ScrollView.content.transform.childCount; i++)
        {
			GameObject ientry = ManageTilesets_ScrollView.content.transform.GetChild(i).gameObject;
			ientry.GetComponent<RectTransform>().anchoredPosition = new Vector2(ientry.GetComponent<RectTransform>().anchoredPosition.x,
				ientry.GetComponent<RectTransform>().anchoredPosition.y + ientry.GetComponent<RectTransform>().rect.height + 5);
		}
		DestroyImmediate(Entry);

		// Disable the menu if we only have the invisible template remaining
		if (ManageTilesets_ScrollView.content.childCount == 1)
		{
			ManageTilesets_button.transform.GetChild(0).GetComponent<Text>().color = Color.gray;
			ManageTilesets_button.interactable = false;
		}
	}

	public void ToggleTileset(GameObject Id_GO)
    {
		string mod_id = Id_GO.GetComponent<Text>().text;
		string[] mod_ids = File.ReadAllLines(Consts.tilesets_path);
		bool enable = !TileManager.CustomTileSections[mod_id].Enabled;
		GameObject Entry = ManageTilesets_ScrollView.content.transform.Find(mod_id).gameObject;

		if (enable)
		{
			// Remove the prefix from the tileset ID
			mod_ids[Array.IndexOf(mod_ids, "#" + mod_id)] = mod_id;

			// Load the full tileset
			TileManager.ReadCatFiles(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\content\\editor\\", mod_id, true);
			TileManager.ReadCflFiles(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\content\\tiles\\", mod_id);
		}
		else
		{
			// Add the prefix to the tileset ID
			mod_ids[Array.IndexOf(mod_ids, mod_id)] = "#" + mod_id;

			// Remove custom tiles of this tileset
			string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
			foreach (var name in to_remove_names)
				TileManager.TileListInfo.Remove(name);
			TileManager.CustomTileSections[mod_id].Enabled = false;
		}

		SwitchAppearance(Entry, mod_id, enable);
		Entry.SetActive(false);
		Entry.SetActive(true);

		// Update tilesets.txt
		File.WriteAllLines(Consts.tilesets_path, mod_ids);
	}

	// TO DO:
	// Add tileset
	// Update all
	// Remove all
	// Enable/disable all

	private void SwitchAppearance(GameObject Entry, string mod_id, bool enable)
    {
		Entry.transform.Find("Id").GetComponent<Text>().text = mod_id;
		Entry.transform.Find("Sets").GetComponent<Text>().text = string.Join(", ", TileManager.CustomTileSections[mod_id].TileSections.ToArray());

		if (enable)
        {
			Entry.transform.Find("Id").GetComponent<Text>().color = new Color32(255, 255, 255, 255);
			Entry.transform.Find("Sets").GetComponent<Text>().color = new Color32(255, 255, 255, 255);
			Entry.transform.Find("button_toggle").gameObject.transform.Find("Text").GetComponent<Text>().text = "Disable";
		}
		else
        {
			Entry.transform.Find("Id").GetComponent<Text>().color = new Color32(160, 160, 160, 160);
			Entry.transform.Find("Sets").GetComponent<Text>().color = new Color32(160, 160, 160, 160);
			Entry.transform.Find("button_toggle").gameObject.transform.Find("Text").GetComponent<Text>().text = "Enable";
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
		Consts.LoadMirrored = MirroredToggle.isOn;
		if (!MirroredToggle.isOn)
        {
			MirroredToggle.transform.gameObject.SetActive(false);
			MirroredToggle.transform.gameObject.SetActive(true);
        }
	}
	public void ToggleResize()
    {
		if (!ResizeToggle.isOn)
        {
			ResizeToggle.transform.gameObject.SetActive(false);
			ResizeToggle.transform.gameObject.SetActive(true);
        }
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
	public void RemoveModdataFolder()
	{
		string contentpath = IO.GetCrashdayPath() + "\\moddata\\";
		try
		{
			Directory.Delete(contentpath, true);
		}
		catch
		{ }
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
		ChangeSceneToEditor();
	}
	public void QuitGame()
	{
		Application.Quit();
	}
	public void LoadTrackToVariablesAndRunEditor()
	{
		string[] sourcepath = StandaloneFileBrowser.OpenFilePanel("Select track (.trk) ", Consts.LoadLastFolderPath(), "trk", false);
		if (sourcepath.Length == 0)
			return;
		else// player hasnt clicked 'cancel' button
		{
			string path = sourcepath[0];
			Consts.TRACK = MapParser.ReadMap(path);
			Consts.Trackname = path.Substring(path.LastIndexOf('\\') + 1, path.Length - path.LastIndexOf('\\') - 5);
			path = path.Substring(0, path.LastIndexOf('\\'));
			Consts.SaveLastFolderPath(path);
			
			if (ResizeToggle.isOn)
			{
				LoadMenu.SetActive(false);
				ResizeMenu.SetActive(true);
				UpdateResizeMenu();
			}
			else
			{
				Loader.Isloading = true;
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
		Resizemenu_Trackname.text = Consts.Trackname;
		int newwidth = int.Parse(ResizeMenu_Right.text) + int.Parse(ResizeMenu_Left.text);
		int newheight = int.Parse(ResizeMenu_Up.text) + int.Parse(ResizeMenu_Down.text);
		Resizemenu_Size_str.text = "Size: " + (Consts.TRACK.Width + newwidth) + " x " + (Consts.TRACK.Height + newheight);
		Resizemenu_Elements_str.text = "Elements: " + (Consts.TRACK.Width + newwidth) * (Consts.TRACK.Height + newheight) + " / " + Consts.MAX_ELEMENTS;
	}
	public void Resize_n_Load()
	{
		int newwidth = int.Parse(ResizeMenu_Right.text) + int.Parse(ResizeMenu_Left.text);
		int newheight = int.Parse(ResizeMenu_Up.text) + int.Parse(ResizeMenu_Down.text);
		if (Consts.TRACK.Width + newwidth < 3 || Consts.TRACK.Height + newheight < 3 || 
		 (Consts.TRACK.Width + newwidth) * (Consts.TRACK.Height + newheight) > Consts.MAX_ELEMENTS)
			return;
		TrackSavable ResizedMap = new TrackSavable(Consts.TRACK, int.Parse(ResizeMenu_Right.text), int.Parse(ResizeMenu_Left.text),
			int.Parse(ResizeMenu_Up.text), int.Parse(ResizeMenu_Down.text));

		var style = Consts.TRACK.Style;
		var permission = Consts.TRACK.Permission;
		Consts.TRACK = ResizedMap;
		Consts.TRACK.Style = style;
		Consts.TRACK.Permission = permission;

		Loader.Isloading = true;
		
		ChangeSceneToEditor();
	}
}
