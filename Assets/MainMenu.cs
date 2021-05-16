using SFB;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
// First launched script
// MainMenu.cs -> Loader.cs
// Loads mods, handles menu and loads .trk file pointed by player
public class MainMenu : MonoBehaviour
{
  public Text LoadingScreen_text_logo;
  public GameObject loadScreen;
  public Button ManageTilesets_button;
  public ScrollRect ManageTilesets_ScrollView;
  /// <summary>Whether new track dimensions don't exceed TrackTileLimit</summary>
  public static bool CanCreateTrack = true;
  void Awake()
  {
    Service.LoadMirrored = false;
    Service.Isloading = false;
    // if we running this for the first time
    if(TileManager.TileListInfo.Count == 0)
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
        if(mod_id != null)
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
      // disable menu
      ManageTilesets_button.transform.GetChild(0).GetComponent<Text>().color = Color.gray;
      ManageTilesets_button.interactable = false;
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

    if(ManageTilesets_ScrollView.content.childCount == 1) // if we only have empty invisible manage_entry_template remaining
    {
      // disable menu
      ManageTilesets_button.transform.GetChild(0).GetComponent<Text>().color = Color.gray;
      ManageTilesets_button.interactable = false;
    }
  }
  public void Toggle_mirror(GameObject checkmark)
  {
    Service.LoadMirrored =checkmark.activeSelf;
  }
  private void ChangeSceneToEditor()
  {
    if (CanCreateTrack)
    {
      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
    {}
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
      Service.UpperBarTrackName = path.Substring(path.LastIndexOf('\\') + 1);
      SaveTrackPath(path);
      Service.TRACK = MapParser.ReadMap(path + ".trk");

      //Set isLoading flag to true
      Service.Isloading = true;
      StartCoroutine("EnableLoadingScreen");

      ChangeSceneToEditor();
    }
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
