using SFB;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
//Handles all variables essential for editor
public class DuVecInt
{
  /// <summary>
  /// Pozycja LD
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

  public GameObject save;
  public GameObject help;
  public GameObject MissingTilesPanel;
  public GameObject MissingTilesPanel_content;
  public GameObject editorPANEL;
  public GameObject formPANEL;
  public Button formToBuildButton;
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
  void Update()
  {
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
        editorPANEL.SetActive(!editorPANEL.activeSelf);
        formPANEL.SetActive(!formPANEL.activeSelf);
      }
    }
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
    Debug.Log(SceneManager.GetActiveScene());
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
  }
  void Awake()
  {
    NameOfTrack.text = GetComponent<Loader>().nazwa_toru.text;
  }

  public void SetMarkerDimsX(string val)
  {

    Service.MarkerBounds.x = int.Parse(val) > 5 ? int.Parse(val) : 5;
  }
  public void SetMarkerDimsZ(string val)
  {
    Service.MarkerBounds.y = int.Parse(val) > 5 ? int.Parse(val) : 5;
  }

  public void Toggle_help()
  {
    if (help.activeSelf)
      help.SetActive(false);
    else
      help.SetActive(true);
  }


  public void SaveMenu_works()
  {
    string[] originalpath = StandaloneFileBrowser.OpenFolderPanel("Select folder to save this track in ..", MainMenu.LoadTrackPath(), false);
    string path = originalpath[0];
    if (path == "")
      return;
    MainMenu.SaveTrackPath(path);
    Service.UpperBarTrackName = NameOfTrack.text;
    GetComponent<Loader>().nazwa_toru.text = Service.UpperBarTrackName;

    // Save terrain
    for (int y = 0; y < 4 * Service.TRACK.Height + 1; y++)
    {
      for (int x = 0; x < 4 * Service.TRACK.Width + 1; x++)
      {
        int i = x + 4 * y * Service.TRACK.Width + y;
        Service.TRACK.Heightmap[4 * Service.TRACK.Height - y][x] = Service.current_heights[i] * 5;
      }
    }

    // Prepare track tiles arrays
    for (int z = 0; z < Service.TRACK.Height; z++)
      for (int x = 0; x < Service.TRACK.Width; x++)
        Service.TRACK.TrackTiles[z][x].Set(0, 0, 0, 0);

    Service.TRACK.FieldFiles.Clear();
    Service.TRACK.FieldFiles.Add("field.cfl");
    Service.TRACK.FieldFilesNumber = 1;

    // Save tiles
    for (int z = 0; z < Service.TRACK.Height; z++)
    {
      for (int x = 0; x < Service.TRACK.Width; x++)
      {
        if (Service.TilePlacementArray[z, x].Name != null)
        {
          ushort fieldId = SetAndGetFieldId(Service.TilePlacementArray[z, x].Name);
          byte inwersja = (byte)(Service.TilePlacementArray[z, x].Inversion ? 1 : 0);
          byte rotacja = (byte)(Service.TilePlacementArray[z, x].Rotation / 90);
          if (inwersja == 1 && rotacja != 0)
            rotacja = (byte)(4 - rotacja);
          Vector2Int dim = TileManager.GetRealDims(Service.TilePlacementArray[z, x].Name, (rotacja == 1 || rotacja == 3) ? true : false);
          //Base part
          Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z + 1 - dim.y][x].Set(fieldId, rotacja, inwersja, 0);
          //Left Bottom
          if (dim.y == 2)
            Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z][x].Set(65471, rotacja, inwersja, 0);
          //Right top
          if (dim.x == 2)
            Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z + 1 - dim.y][x + 1].Set(65472, rotacja, inwersja, 0);
          //Right bottom
          if (dim.x == 2 && dim.y == 2)
            Service.TRACK.TrackTiles[Service.TRACK.Height - 1 - z][x + 1].Set(65470, rotacja, inwersja, 0);
        }
      }
    }
    Service.TRACK.Comment = "Made with 3D editor " + Service.VERSION;
    MapParser.SaveMap(Service.TRACK, path + "\\" + Service.UpperBarTrackName + ".trk");
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
}
