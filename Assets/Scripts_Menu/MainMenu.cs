using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// First launched script
// MainMenu.cs -> Loader.cs
// Loads mods, handles menu and loads .trk file pointed by player
public class MainMenu : MonoBehaviour
{
	public GameObject LoadMenu;
	public Toggle ResizeToggle;
	public Toggle MirroredToggle;

	public GameObject ResizeMenu;
	public Text Resizemenu_Trackname;
	public Text Resizemenu_Size_str;
	public Text Resizemenu_Elements_str;
	public InputField ResizeMenu_Right;
	public InputField ResizeMenu_Left;
	public InputField ResizeMenu_Up;
	public InputField ResizeMenu_Down;

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
		try
		{
			Directory.Delete(IO.GetCrashdayPath() + "\\data\\content\\", true);
			Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\", true);
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
		Consts.TRACK = new TrackSavable(Consts.TRACK, int.Parse(ResizeMenu_Right.text), int.Parse(ResizeMenu_Left.text),
			int.Parse(ResizeMenu_Up.text), int.Parse(ResizeMenu_Down.text));

		Loader.Isloading = true;
		
		ChangeSceneToEditor();
	}
}
