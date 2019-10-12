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
    upperPanel_t_version.text = Data.VERSION;
    floor1 = Resources.Load<Material>("floor1");
    if(Data.MissingTilesNames.Count > 0)
    {
      MissingTilesPanel.SetActive(true);
      MissingTilesPanel_content.GetComponent<Text>().text = string.Join("\n", Data.MissingTilesNames);
    }
  }
  void Update()
  {
    formToBuildButton.interactable = (formPANEL.activeSelf && !Terraining.isSelecting);
    if (Input.GetKeyDown(KeyCode.F5))
    {
      save.SetActive(!save.activeSelf);
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
        if (editorPANEL.gameObject.activeSelf)
        {
          BuildToFormMenu();
        }
        else
        {
          FormToBuildMenu();
        }
      }
    }
  }

  private void Toggle_floor1Shader(Shader ForceShader = null)
  {
    if(ForceShader != null)
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
    NameOfTrack.text = this.GetComponent<Loader>().nazwa_toru.text;

  }
  public void FormToBuildMenu()
  {
    if (!Terraining.isSelecting)
    {//form -> build tylko gdy nie jesteś w trakcie przeciągania
      Terraining.Del_znaczniki();
      Terraining.istilemanip = false;
      formPANEL.GetComponent<Terraining>().state_help_text.text = "Manual forming..";
      if (Terraining.indicator != null)
        Destroy(Terraining.indicator);
      editorPANEL.gameObject.SetActive(true);
      formPANEL.gameObject.SetActive(false);
    }
  }

  public void SetMarkerDimsX(string val)
  {

    Terraining.max_verts_visible_dim.x = int.Parse(val) > 5 ? int.Parse(val) : 5;
  }
  public void SetMarkerDimsZ(string val)
  {
    Terraining.max_verts_visible_dim.z = int.Parse(val) > 5 ? int.Parse(val) : 5;
  }
  public void BuildToFormMenu()
  {
    editorPANEL.gameObject.SetActive(false);
    formPANEL.gameObject.SetActive(true);
    if (Building.current_rmc != null)
    {
      if (!editorPANEL.GetComponent<Building>().LMBclicked || (editorPANEL.GetComponent<Building>().LMBclicked && !editorPANEL.GetComponent<Building>().AllowLMB))
        Building.DelLastPrefab();
      Building.nad_wczesniej = false;
    }
  }

  public void Toggle_help()
  {
    if (help.activeSelf)
      help.SetActive(false);
    else
      help.SetActive(true);
  }

  public static Bitmap ConvertTo24bpp(System.DrawingCore.Image img)
  {
    var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
    using (var gr = System.DrawingCore.Graphics.FromImage(bmp))
      gr.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
    return bmp;
  }


  public void SaveMenu_works()
  {
    string[] originalpath = StandaloneFileBrowser.OpenFolderPanel("Select folder to save this track in ..", MainMenu.LoadTrackPath(), false);
    string path = originalpath[0];
    if (path == "")
      return;
    MainMenu.SaveTrackPath(path);
    Data.UpperBarTrackName = NameOfTrack.text;
    GetComponent<Loader>().nazwa_toru.text = Data.UpperBarTrackName;

    // Save terrain
    for (int y = 0; y < 4 * Data.TRACK.Height + 1; y++)
    {
      for (int x = 0; x < 4 * Data.TRACK.Width + 1; x++)
      {
        int i = x + 4 * y * Data.TRACK.Width + y;
        Data.TRACK.Heightmap[4 * Data.TRACK.Height - y][x] = Loader.current_heights[i] * 5;
      }
    }

    // Prepare track tiles arrays
    for (int z = 0; z < Data.TRACK.Height; z++)
      for (int x = 0; x < Data.TRACK.Width; x++)
        Data.TRACK.TrackTiles[z][x].Set(0, 0, 0, 0);

    Data.TRACK.FieldFiles.Clear();
    Data.TRACK.FieldFiles.Add("field.cfl");
    Data.TRACK.FieldFilesNumber = 1;

    // Save tiles
    for (int z = 0; z < Data.TRACK.Height; z++)
    {
      for (int x = 0; x < Data.TRACK.Width; x++)
      {
        if (Data.TilePlacementArray[z, x].Name != null)
        {
          ushort fieldId = SetAndGetFieldId(Data.TilePlacementArray[z, x].Name);
          byte inwersja = (byte)(Data.TilePlacementArray[z, x].Inversion ? 1 : 0);
          byte rotacja = (byte)(Data.TilePlacementArray[z, x].Rotation / 90);
          Vector2Int dim = TileManager.GetRealDims(Data.TilePlacementArray[z, x].Name, (rotacja == 1 || rotacja == 3) ? true : false);
          if (inwersja == 1 && rotacja != 0)
            rotacja = (byte)(4 - rotacja);
          //Base part
          Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z + 1 - dim.y][x].Set(fieldId, rotacja, inwersja, 0);
          //Left Bottom
          if (dim.y == 2)
            Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x].Set(65471, rotacja, inwersja, 0);
          //Right top
          if (dim.x == 2)
            Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z + 1 - dim.y][x + 1].Set(65472, rotacja, inwersja, 0);
          //Right bottom
          if (dim.x == 2 && dim.y == 2)
            Data.TRACK.TrackTiles[Data.TRACK.Height - 1 - z][x + 1].Set(65470, rotacja, inwersja, 0);
        }
      }
    }
    MapParser.SaveMap(Data.TRACK, path + "\\" + Data.UpperBarTrackName + ".trk");
  }
  ushort SetAndGetFieldId(string name)
  {
    name += ".cfl";
    for (ushort i = 0; i < Data.TRACK.FieldFiles.Count; i++)
    {
      if (name == Data.TRACK.FieldFiles[i])
        return i;
    }
    Data.TRACK.FieldFiles.Add(name);
    Data.TRACK.FieldFilesNumber++;
    return (ushort)(Data.TRACK.FieldFilesNumber - 1);
  }

  public void Toggle_saveMenu()
  {
    if (!formPANEL.activeSelf)
    {
      if (save.activeSelf)
        save.SetActive(false);
      else
        save.SetActive(true);
    }
  }
}
