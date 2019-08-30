using SFB;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
  public Material floor1;
  public Shader DepthShader;
  public Shader defaultShader;
  public GameObject save;
  public GameObject help;
  public GameObject options;
  public GameObject editorPANEL;
  public GameObject formPANEL;
  public Button formToBuildButton;
  public InputField NameOfTrack; // Canvas/savePANEL/NameofTrack
  public static string tile_name = "street"; //potrzebna do częstego zmieniania trybów - żeby zapamiętywać klocek

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
        toggle_map();
      }
      if (Input.GetKeyDown(KeyCode.H))
      {
        toggle_help();
      }
      if (Input.GetKeyDown(KeyCode.O))
      {
        toggle_options();
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

  private void toggle_map()
  {
    if (floor1.shader != DepthShader)
      floor1.shader = DepthShader;
    else
      floor1.shader = defaultShader;
  }
  public void EditorToMenu()
  {
    if (floor1.shader == DepthShader)
      floor1.shader = defaultShader;
    Debug.Log(SceneManager.GetActiveScene());
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
  }
  void Awake()
  {
    NameOfTrack.text = this.GetComponent<Helper>().nazwa_toru.text;

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
    if (Building.obj_rmc != null)
    {
      if (!editorPANEL.GetComponent<Building>().LMBclicked || (editorPANEL.GetComponent<Building>().LMBclicked && !editorPANEL.GetComponent<Building>().AllowLMB))
        Building.DelLastPrefab();
      Building.nad_wczesniej = false;
    }
  }

  public void toggle_help()
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
    string[] originalpath = StandaloneFileBrowser.OpenFolderPanel("Select folder to save this track in ..", Loader.LoadPath(), false);
    string path = originalpath[0];
    if (path == "")
      return;
    MainMenu.SaveTrackPath(path);
    Data.UpperBarTrackName = NameOfTrack.text;
    GetComponent<Helper>().nazwa_toru.text = Data.UpperBarTrackName;

    // Save terrain
    for (int y = 0; y < 4 * SliderHeight.val + 1; y++)
    {
      for (int x = 0; x < 4 * SliderWidth.val + 1; x++)
      {
        int i = x + 4 * y * SliderWidth.val + y;
        Data.TRACK.Heightmap[4 * SliderHeight.val - y][x] = Helper.current_heights[i] * 5f;
      }
    }

    // Prepare track tiles arrays
    for (int z = 0; z < SliderHeight.val; z++)
      for (int x = 0; x < SliderWidth.val; x++)
        Data.TRACK.TrackTiles[z][x].Set(0, 0, 0, 0);

    Data.TRACK.FieldFiles.Clear();
    Data.TRACK.FieldFiles.Add("field.cfl");
    Data.TRACK.FieldFilesNumber = 1;

    // Save tiles
    for (int z = 0; z < SliderHeight.val; z++)
    {
      for (int x = 0; x < SliderWidth.val; x++)
      {
        if (Data.TilePlacementArray[x, z].Name != null)
        {
          ushort fieldId = SetAndGetFieldId(Data.TilePlacementArray[x, z].Name);
          byte inwersja = (byte)(Data.TilePlacementArray[x, z].Inversion ? 1 : 0);
          byte rotacja = (byte)(Data.TilePlacementArray[x, z].Rotation / 90);
          Vector2Int dim = Helper.GetTileDimensions(Data.TilePlacementArray[x, z].Name, (rotacja == 1 || rotacja == 3) ? true : false);
          if (inwersja == 1 && rotacja != 0)
            rotacja = (byte)(4 - rotacja);
          //Base part
          Data.TRACK.TrackTiles[SliderHeight.val - 1 - z + 1 - dim.y][x].Set(fieldId, rotacja, inwersja, 0);
          //Left Bottom
          if (dim.y == 2)
            Data.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].Set(65471, rotacja, inwersja, 0);
          //Right top
          if (dim.x == 2)
            Data.TRACK.TrackTiles[SliderHeight.val - 1 - z + 1 - dim.y][x + 1].Set(65472, rotacja, inwersja, 0);
          //Right bottom
          if (dim.x == 2 && dim.y == 2)
            Data.TRACK.TrackTiles[SliderHeight.val - 1 - z][x + 1].Set(65470, rotacja, inwersja, 0);
        }
      }
    }


    MapParser.SaveMap(Data.TRACK, path + "\\" + Data.UpperBarTrackName + ".trk");
  }
  void PrepareTrackTilesArrays()
  {
    
  }
  void SaveTiles()
  {
    
  }
  ushort SetAndGetFieldId(string nazwa_tilesa)
  {
    nazwa_tilesa = nazwa_tilesa + ".cfl";
    for (ushort i = 0; i < Data.TRACK.FieldFiles.Count; i++)
    {
      if (nazwa_tilesa == Data.TRACK.FieldFiles[i])
        return i;
    }
    Data.TRACK.FieldFiles.Add(nazwa_tilesa);
    Data.TRACK.FieldFilesNumber++;
    return (ushort)(Data.TRACK.FieldFilesNumber - 1);
  }
  void SaveTerrain()
  {
    
  }

  public void toggle_options()
  {
    if (options.activeSelf)
      options.SetActive(false);
    else
      options.SetActive(true);
  }

  public void toggle_saveMenu()
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
