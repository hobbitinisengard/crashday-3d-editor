using SFB;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
// First launched script
// MainMenu.cs -> Helper.cs

// Loads mods, handles menu and loads .trk file pointed by player
public class MainMenu : MonoBehaviour
{
  public GameObject loadScreen;

  /// <summary>Whether new track dimensions don't exceed STATIC.TrackTileLimit</summary>
  public bool CanCreateTrack = true;

  void Awake()
  {
    Data.Isloading = false;
  }
  private void Start()
  {
    // load default tiles and information about them from .cfl files in crashday folder
    string cdPath = IO.GetCrashdayPath();
    TileManager.LoadTiles();
  }
  public void PlayGame()
  {
    if (CanCreateTrack)
    {
      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
  }
  public void QuitGame()
  {
    Application.Quit();
  }
  public void LoadTrackToVariablesAndRunEditor()
  {
    string[] sourcepath = StandaloneFileBrowser.OpenFilePanel("Select track (.trk) ", LoadTrackPath(), "trk", false);
    string path = sourcepath[0];
    if (path.Length > 0) // player hasnt clicked 'cancel' button
    {
      //Path can't have .trk suffix
      path = path.Substring(0, path.Length - 4);

      SaveTrackPath(path);

      Data.TRACK = MapParser.ReadMap(path + ".trk");

      Data.UpperBarTrackName = path.Substring(path.LastIndexOf('\\') + 1);

      // Allocate memory for TilePlacementArray
      Data.TilePlacementArray = new TilePlacement[Data.TRACK.Width, Data.TRACK.Height];
      for (int z = 0; z < Data.TRACK.Height; z++)
      {
        for (int x = 0; x < Data.TRACK.Width; x++)
        {
          Data.TilePlacementArray[x, z] = new TilePlacement();
        }
      }

      // Load tiles layout from TRACK to TilePlacementArray
      for (int z = 0; z < Data.TRACK.Height; z++)
      {
        for (int x = 0; x < Data.TRACK.Width; x++)
        {
          //  tiles bigger than 1x1 have funny max uint numbers around center block. We ignore them as well as empty grass fields (FieldId = 0)  
          if (Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x].FieldId < Data.TRACK.FieldFiles.Count && Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x].FieldId != 0)
          {
            // assignment for clarity
            TrackTileSavable tile = Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x];

            // without .cfl suffix
            string TileName = Data.TRACK.FieldFiles[tile.FieldId].Substring(0, Data.TRACK.FieldFiles[tile.FieldId].Length - 4);

            int Rotation = tile.Rotation * 90;
            bool Inversion = tile.IsMirrored == 0 ? false : true;
            if (Inversion && Rotation != 0)
              Rotation = 360 - Rotation;

            Vector2Int dim = GetTileDimensions(TileName, (Rotation == 90 || Rotation == 270) ? true : false);
            // given tile isn't modded, else don't place it
            if (dim.x != -1 && dim.y != -1)
            {
              Data.TilePlacementArray[x, z - dim.y + 1].Set(TileName, Rotation, Inversion);
              //loadedTilesPairsXZ.Add(new Vector2Int(x, z - dim.y + 1));
            }
            else
            {
              Data.TilePlacementArray[x, z - dim.y + 1].Set(TileName, Rotation, Inversion, 255);
            }
          }
        }
      }

      // 3.Run editor
      Data.Isloading = true;
      string nazwa = Mathf.CeilToInt(8 * UnityEngine.Random.value).ToString();
      loadScreen.transform.Find(nazwa).gameObject.SetActive(true);

      PlayGame();
      //Next script is Helper.Awake()

    }
  }
  /// <summary>
  /// LoadPath()+.txt loads to S.tiles and  loadedTilesPairsXZ info.
  /// </summary>
  void LoadTilesToList()
  {

  }
  /// <summary>
  /// Returns dimensions of tile. If specified tile hasn't been found, returns -1, -1 vector2Int.
  /// </summary>
  public static Vector2Int GetTileDimensions(string nazwa_tilesa, bool swap = false)
  {
    for (int i = 0; i < dims.Count; i++)
    {
      if (nazwa_tilesa == dims[i].TileName)
      {
        if (swap)
          return new Vector2Int(dims[i].DimVector.y, dims[i].DimVector.x);
        else
          return dims[i].DimVector;
      }
    }
    return new Vector2Int(-1, -1);
  }
  /// <summary>
  /// Zapisuje informacje do pliku w Assets/Resources/path.txt
  /// </summary>
  public static void SaveTrackPath(string path)
  {
    StreamWriter w = new StreamWriter(Application.dataPath + "/StreamingAssets/path.txt");
    w.WriteLine(path);
    w.Close();
  }
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
