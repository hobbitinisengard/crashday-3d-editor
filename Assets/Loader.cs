using SFB;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.DrawingCore.Imaging;
using System.IO;
using UnityEngine;
//Provides every function needed to load data when Heightmap Editor is started
public class Element
{
  public string _nazwa;
  public byte _kategoria;
  public int _rotacja;
  public bool _inwersja;
  public List<int> t_verts;

  public void Set(string nazwa, int rotacja, bool inwersja, byte kategoria)
  {
    this._nazwa = nazwa;
    this._kategoria = kategoria;
    this._inwersja = inwersja;
    this._rotacja = rotacja;
  }
}
public class Kategoria
{
  public string _nazwa_tilesa;
  public byte _nr_kat;

  public Kategoria(string nazwa, byte nr_kat)
  {
    _nazwa_tilesa = nazwa;
    _nr_kat = nr_kat;
  }
}
public class Dimension
{
  public string _nazwa_tilesa;
  public Vector2Int _wymiary;
  public Dimension(string nazwa, Vector2Int wymiar)
  {
    _nazwa_tilesa = nazwa;
    _wymiary = wymiar;
  }
}
// 2. Skrypt
public class Loader : MonoBehaviour
{
  public GameObject ls; //loadscreen
  public GameObject MainMenu; //mainmenu
  public static List<Duint> loadedTilesPairsXZ = new List<Duint>();
  static string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
  public static List<Dimension> dims = new List<Dimension>(); //W mainmenu 

  /// <summary>
  /// Otwiera explorer, ucina (4) path, zapisuje go do pliku, ustawia BMP =  S.heightmapa, ustala S.boundingHeight, SliderHeight/Width, S.nazwa_trasy,
  /// ładuje tilesy do S.tiles i Loader.tilesPairs, pokazuje ekran ładowania i wywołuje PlayGame() po Find(MainMenu)
  /// </summary>
  public void OpenExplorer()
  {
    string[] sourcepath = StandaloneFileBrowser.OpenFilePanel("SELECT TRACK (TRK) ", LoadPath(), "trk", false);
    if (sourcepath.Length == 0)
      return;
    string path = sourcepath[0];
    if (path.Length > 0)
    {
      //Path doesnt have .trk; save it to path.txt
      path = path.Substring(0, path.Length - 4);
      SavePath(path);

      STATIC.TRACK = MapParser.ReadMap(path + ".trk");

      SliderWidth.val = STATIC.TRACK.Width;
      SliderHeight.val = STATIC.TRACK.Height;

      STATIC.Nazwa_trasy = path.Substring(path.LastIndexOf('\\') + 1);
      // 2.Layout
      Zaladuj_tilesy();

      // 3.Run editor

      STATIC.PlaygamePass = true;
      STATIC.Isloading = true;
      ls.SetActive(true);
      string nazwa = Mathf.CeilToInt(8 * UnityEngine.Random.value).ToString();
      ls.transform.Find(nazwa).gameObject.SetActive(true);

      MainMenu.GetComponent<MainMenu>().PlayGame();
      //Next script is Helper.Awake()

    }
  }
  public static void InitializeSTATICTiles(int width, int height)
  {
    STATIC.Tiles = new Element[width, height];
    for (int z = 0; z < height; z++)
    {
      for (int x = 0; x < width; x++)
      {
        STATIC.Tiles[x, z] = new Element();
      }
    }
  }
  /// <summary>
  /// LoadPath()+.txt ładuje do S.tiles i loadedTilesPairsXZ informacje.
  /// </summary>
  void Zaladuj_tilesy()
  {
    Loader.loadedTilesPairsXZ.Clear();
    InitializeSTATICTiles(SliderWidth.val, SliderHeight.val);
    for (int z = 0; z < SliderHeight.val; z++)
    {
      for (int x = 0; x < SliderWidth.val; x++)
      {
        if (STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].FieldId < STATIC.TRACK.FieldFiles.Count && STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].FieldId != 0)
        {
          string nazwa_tilesa = STATIC.TRACK.FieldFiles[STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].FieldId].Substring(0, STATIC.TRACK.FieldFiles[STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].FieldId].Length - 4);

          int rotacja = STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].Rotation * 90;
          bool inwersja = STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].IsMirrored == 0 ? false : true;
          if (inwersja && rotacja != 0)
            rotacja = 360 - rotacja;

          Vector2Int dim = GetTileDimensions(nazwa_tilesa, (rotacja == 90 || rotacja == 270) ? true : false);
          STATIC.Tiles[x, z - dim.y + 1].Set(nazwa_tilesa, rotacja, inwersja, Budowanie.GetTileCategory(nazwa_tilesa));
          loadedTilesPairsXZ.Add(new Duint(x, z - dim.y + 1));
        }
      }
    }
  }
  public static Vector2Int GetTileDimensions(string nazwa_tilesa, bool swap = false)
  {
    for (int i = 0; i < dims.Count; i++)
    {
      if (nazwa_tilesa == dims[i]._nazwa_tilesa)
      {
        if (swap)
          return new Vector2Int(dims[i]._wymiary.y, dims[i]._wymiary.x);
        else
          return dims[i]._wymiary;
      }
    }
    return new Vector2Int();
  }
  /// <summary>
  /// Zapisuje informacje do pliku w Assets/Resources/path.txt
  /// </summary>
  public static void SavePath(string path)
  {
    StreamWriter w = new StreamWriter(Application.dataPath + "/StreamingAssets/path.txt");
    w.WriteLine(path);
    w.Close();
  }
  public static string LoadPath()
  {
    StreamReader w = new StreamReader(Application.dataPath + "/StreamingAssets/path.txt");
    string to_return = w.ReadLine();
    w.Close();
    if (to_return == "")
      to_return = docs;
    return to_return;
  }

  /// <summary>
  /// Ustala średnią wysokość z pixeli na obrzeżach
  /// </summary>
  byte GetBoundingHeight(Bitmap track)
  {
    int count = 0;
    float AverageHeight = 0;
    for (int z = 0; z < track.Height; z++)
    {
      if (z == 0 || z == track.Height - 1)
      {
        for (int x = 0; x < track.Width; x++)
        {
          AverageHeight += track.GetPixel(x, z).R;
          count++;
        }
      }
      else
      {
        AverageHeight += track.GetPixel(0, z).R;
        AverageHeight += track.GetPixel(track.Width - 1, z).R;
        count += 2;
      }
    }
    return (byte)Mathf.RoundToInt(AverageHeight / count); //0 - 255
  }
  /// <summary>
  /// Zmienia wymiary mapy tak, by łączna suma pixeli była mniejsza lub równa 2000
  /// </summary>
  Bitmap SetHeightmapUp(Bitmap H)
  {
    int newWidth = (H.Width - 1) / 4, newHeight = (H.Height - 1) / 4;
    if (H.Width * H.Height > 2000) // Kontrola całkowitej ilości pixelów
    {
      float Ratio = 1f * H.Width / H.Height;
      newHeight = Mathf.FloorToInt(Mathf.Sqrt(2000 / Ratio));
      newWidth = Mathf.FloorToInt(Ratio * newHeight);
    }
    return Resize(H, 4 * newWidth + 1, 4 * newHeight + 1);
  }

  Bitmap Resize(Image image, int width, int height)
  {
    var destRect = new Rectangle(0, 0, width, height);
    var destImage = new Bitmap(width, height);

    destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

    using (var graphics = System.DrawingCore.Graphics.FromImage(destImage))
    {
      graphics.CompositingMode = CompositingMode.SourceCopy;
      graphics.CompositingQuality = CompositingQuality.HighQuality;
      graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
      graphics.SmoothingMode = SmoothingMode.HighQuality;
      graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

      using (var wrapMode = new ImageAttributes())
      {
        wrapMode.SetWrapMode(System.DrawingCore.Drawing2D.WrapMode.TileFlipXY);
        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
      }
    }

    return destImage;
  }

}
