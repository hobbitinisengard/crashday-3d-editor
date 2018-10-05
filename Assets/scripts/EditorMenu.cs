﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using SFB;
using System.Text.RegularExpressions;

public class Pzero {
	public string nazwa;
	public float pos;

    public Pzero(string NAZWA, float POS)
    {
        nazwa = NAZWA;
        pos = POS;
    }
}
public class Flatter
{
    public string nazwa;
    public List<float> heights;

    public Flatter(string NAZWA, List<float> LIST)
    {
        nazwa = NAZWA;
        heights = LIST;
    }
}
public class Custom
{
    public string nazwa;
    public string nazwa_rmc;

    public Custom(string NAZWA, string NAZWA_RMC)
    {
        nazwa = NAZWA;
        nazwa_rmc = NAZWA_RMC;
    }
}
public class Krzaczor
{
    public string nazwa;
    public List<int> indexy;

    public Krzaczor(string NAZWA, List<int> LIST)
    {
        nazwa = NAZWA;
        indexy = LIST;
    }
}

public class EditorMenu : MonoBehaviour {
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
    public static string nazwa_tilesa = "street"; //potrzebna do częstego zmieniania trybów - żeby zapamiętywać klocek
    public static List<Pzero> pzeros = new List<Pzero>(); // Tablica względnych poziomów 0 dla każdego elementu 267
    public static List<Flatter> flatter = new List<Flatter>(); // Tablica elementów dla które można "spłaszczyć" 36
    public static List<Custom> customs = new List<Custom>(); // Tablica elementów dla których trzeba dać niestandardowy rmc (nazwa_rmc) 118
    public static List<Kategoria> kategorie = new List<Kategoria>();
    public static string[] krzaczory = new string[] { "tree1", "tree2", "tree3", "tree4", "tree5", "streetl", "alley", "cralley", "dirtalley", "zebracross", "curveb2", "plot1", "plot2", "plot3"};
    void Update()
    {
        formToBuildButton.interactable = (formPANEL.activeSelf && !Terenowanie.isSelecting);
        if (Input.GetKeyDown(KeyCode.Alpha1))
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
    public void EditorToMenu() {
        if (floor1.shader == DepthShader)
            floor1.shader = defaultShader;
        Debug.Log(SceneManager.GetActiveScene());
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
    void Awake() {
        NameOfTrack.text = this.GetComponent<Helper>().nazwa_toru.text;
        {
            string[] lines = Regex.Split(STATIC.pzeros.text, "\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tab = lines[i].Split(' ');
                pzeros.Add(new Pzero(tab[0], float.Parse(tab[1])));
            }
        }
        {
            string[] lines = Regex.Split(STATIC.flatters.text, "\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tab = lines[i].Split(' ');
                flatter.Add(new Flatter(tab[0], new List<float>()));
                for (int j = 1; j < tab.Length; j++)
                    flatter[flatter.Count - 1].heights.Add(float.Parse(tab[j]));
            }
        }
        {
            string[] lines = Regex.Split(STATIC.rmcs.text, "\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tab = lines[i].Split(' ');
                customs.Add(new Custom(tab[0], tab[1]));
            }
        }
    }
    public void FormToBuildMenu()
    {
        if (!Terenowanie.isSelecting)
        {//form -> build tylko gdy nie jesteś w trakcie przeciągania
            Terenowanie.del_znaczniki();
            Terenowanie.istilemanip = false;
            formPANEL.GetComponent<Terenowanie>().state_help_text.text = "Manual forming..";
            if (Terenowanie.indicator != null)
                Destroy(Terenowanie.indicator);
            editorPANEL.gameObject.SetActive(true);
            formPANEL.gameObject.SetActive(false);
        }
    }
    public void BuildToFormMenu()
    {
        editorPANEL.gameObject.SetActive(false);
        formPANEL.gameObject.SetActive(true);
        if (Budowanie.obj_rmc != null)
        {
            if(!editorPANEL.GetComponent<Budowanie>().LMBclicked || (editorPANEL.GetComponent<Budowanie>().LMBclicked && !editorPANEL.GetComponent<Budowanie>().AllowLMB))
                Budowanie.DelLastPrefab();
            Budowanie.nad_wczesniej = false;
        }
    }
    
	public void toggle_help(){
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
        Loader.SavePath(path);
        STATIC.nazwa_trasy = NameOfTrack.text;
        GetComponent<Helper>().nazwa_toru.text = STATIC.nazwa_trasy;
        //SaveTerrainToBMP(path); (redundant)
        //SaveLayoutToTxt_n_Png(path); (redundant)
        SaveTerrain();
        PrepareTrackTilesArrays();
        SaveTiles();
        MapParser.SaveMap(STATIC.TRACK, path+"\\"+STATIC.nazwa_trasy+".trk");
    }
    void PrepareTrackTilesArrays()
    {
        //for(int k=0; k<5; k++)
        //{
        //    STATIC.TRACK.Height++;
        //    for (int i = 0; i < 4; i++)
        //    {
        //        STATIC.TRACK.Heightmap.Add(new List<float>());
        //        for (int x = 0; x < 4 * SliderWidth.val + 1; x++)
        //        {
        //            STATIC.TRACK.Heightmap[STATIC.TRACK.Heightmap.Count - 1].Add(0);
        //        }
        //    }

        //    STATIC.TRACK.TrackTiles.Add(new List<TrackTileSavable>());
        //    for (int i = 0; i < SliderWidth.val; i++)
        //    {
        //        STATIC.TRACK.TrackTiles[STATIC.TRACK.TrackTiles.Count - 1].Add(new TrackTileSavable());
        //    }
        //}
        for(int z=0; z<SliderHeight.val; z++)
            for(int x=0; x<SliderWidth.val; x++)
                STATIC.TRACK.TrackTiles[z][x].Set(0, 0, 0, 0);
        
        STATIC.TRACK.FieldFiles.Clear();
        STATIC.TRACK.FieldFiles.Add("field.cfl");
        STATIC.TRACK.FieldFilesNumber = 1;
    }
    void SaveTiles()
    {
        for (int z = 0; z < SliderHeight.val; z++)
        {
            for (int x = 0; x < SliderWidth.val; x++)
            {
                if (STATIC.tiles[x, z]._nazwa != null)
                {
                    ushort fieldId = SetAndGetFieldId(STATIC.tiles[x, z]._nazwa);
                    byte inwersja = (byte)(STATIC.tiles[x, z]._inwersja ? 1 : 0);
                    byte rotacja = (byte)(STATIC.tiles[x, z]._rotacja/90);
                    Vector2Int dim = Loader.GetTileDimensions(STATIC.tiles[x, z]._nazwa, (rotacja == 1 || rotacja == 3) ? true : false);
                    if (inwersja == 1 && rotacja != 0)
                        rotacja = (byte)(4 - rotacja);
                    //Base part
                    STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z + 1 - dim.y][x].Set(fieldId, rotacja, inwersja, 0);
                    //Left Bottom
                    if(dim.y == 2)
                        STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x].Set(65471, rotacja, inwersja, 0);
                    //Right top
                    if(dim.x == 2)
                        STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z + 1 - dim.y][x + 1].Set(65472, rotacja, inwersja, 0);     
                    //Right bottom
                    if(dim.x == 2 && dim.y == 2)
                        STATIC.TRACK.TrackTiles[SliderHeight.val - 1 - z][x+1].Set(65470, rotacja, inwersja, 0);
                }
            }
        }
    }
    ushort SetAndGetFieldId(string nazwa_tilesa)
    {
        nazwa_tilesa = nazwa_tilesa + ".cfl";
        for(ushort i=0; i< STATIC.TRACK.FieldFiles.Count; i++)
        {
            if (nazwa_tilesa == STATIC.TRACK.FieldFiles[i])
                return i;
        }
        STATIC.TRACK.FieldFiles.Add(nazwa_tilesa);
        STATIC.TRACK.FieldFilesNumber++;
        return (ushort)(STATIC.TRACK.FieldFilesNumber - 1);
    }
    void SaveTerrain()
    {
        for (int y = 0; y < 4 * SliderHeight.val + 1; y++)
        {
            for (int x = 0; x < 4 * SliderWidth.val + 1; x++)
            {
                int i = x + 4 * y * SliderWidth.val + y;
                STATIC.TRACK.Heightmap[4 * SliderHeight.val - y][x]  = Helper.current_heights[i] * 5f;
            }
        }
    }

    //void SaveLayoutToTxt_n_Png(string path)
    //{
    //    //Zapisz widok do PNG i zapisz layout do txt
    //    Texture2D widok = new Texture2D(63 * SliderWidth.val + 1, 63 * SliderHeight.val + 1);
    //    widok = Greenify(widok);
    //    StreamWriter w = new StreamWriter(path + "\\" + STATIC.nazwa_trasy + ".txt", false);
    //    for (int y = 0; y < SliderHeight.val; y++)
    //    {
    //        for (int x = 0; x < SliderWidth.val; x++)
    //        {
    //            if (STATIC.tiles[x, y]._nazwa != null)
    //            {
    //                //Debug.Log();
    //                Element el = STATIC.tiles[x, y];
    //                w.WriteLine(x + " " + y + " " + el._nazwa + " " + el._kategoria + " " + el._rotacja + " " + el._inwersja);
    //                Texture2D tiles = Instantiate(Resources.Load<Texture2D>("Tiles/" + STATIC.tiles[x, y]._nazwa));
    //                if (el._inwersja)
    //                    tiles = InverseImage(tiles);
    //                if (el._rotacja != 0)
    //                    tiles = RotateImage(tiles, el._rotacja);
    //                for (int j = 0; j < tiles.height; j++) // Nałóż obrazek
    //                    for (int i = 0; i < tiles.width; i++)
    //                        widok.SetPixel(63 * x + 1 + i, 63 * y + 1 + j, tiles.GetPixel(i, j));
    //            }
    //        }
    //    }
    //    w.Close();
    //    widok.Apply();
    //    SaveTextureAsPNG(widok, path + "\\" + STATIC.nazwa_trasy + "_layout.png");
    //}
    //void SaveTerrainToBMP(string path)
    //{
    //    Bitmap heightmapa = new Bitmap(4 * SliderWidth.val + 1, 4 * SliderHeight.val + 1);
    //    {
    //        int i = 0;
    //        for (int y = 0; y < 4 * SliderHeight.val + 1; y++)
    //        {
    //            for (int x = 0; x < 4 * SliderWidth.val + 1; x++)
    //            {
    //                heightmapa.SetPixel(x, 4 * SliderHeight.val - y, System.DrawingCore.Color.FromArgb(Terenowanie.RealHeight2SliderValue(Helper.current_heights[i]), Terenowanie.RealHeight2SliderValue(Helper.current_heights[i]), Terenowanie.RealHeight2SliderValue(Helper.current_heights[i])));
    //                i++;
    //            }
    //        }
    //    }
    //    heightmapa = ConvertTo24bpp(heightmapa);
    //    //EncoderParameters parameters = new EncoderParameters(0);
    //    //parameters.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);
    //    heightmapa.Save(path + "\\" + STATIC.nazwa_trasy + ".bmp", ImageFormat.Bmp);
    //}

    //Texture2D Greenify(Texture2D image)
    //{
    //    UnityEngine.Color[] cArr = image.GetPixels();
    //    UnityEngine.Color fillColor = UnityEngine.Color.gray;
    //    for(int i=0; i<cArr.Length; i++)
    //    {
    //        cArr[i] = fillColor;
    //    }
    //    image.SetPixels(cArr);
    //    image.Apply();
    //    return image;
    //}
    //Texture2D RotateImage(Texture2D image, int rotacja)
    //{
    //    Texture2D to_return;
    //    if (rotacja == 90 || rotacja == 270)
    //        to_return = new Texture2D(image.height, image.width);
    //    else
    //        to_return = new Texture2D(image.width, image.height);
    //    for(int y=0; y<to_return.height; y++)
    //    {
    //        for(int x=0; x<to_return.width; x++)
    //        {
    //            if (rotacja == 90)
    //                to_return.SetPixel(x, y, image.GetPixel(image.width - y, x));
    //            else if (rotacja == 180)
    //                to_return.SetPixel(x, y, image.GetPixel(image.width - x, image.height - y));
    //            else if (rotacja == 270)
    //                to_return.SetPixel(x, y, image.GetPixel(y, image.height - x));
    //        }
    //    }
    //    to_return.Apply();
    //    return to_return;
    //}
    //Texture2D InverseImage(Texture2D tiles)
    //{
    //    Texture2D to_return = new Texture2D(tiles.width, tiles.height);
    //    for (int j = 0; j < tiles.height; j++)
    //    {
    //        for (int i = 0; i <= tiles.width/2; i++)
    //        {

    //            UnityEngine.Color lewy = tiles.GetPixel(i, j);
    //            to_return.SetPixel(i, j, tiles.GetPixel(tiles.width - i, j));
    //            to_return.SetPixel(tiles.width - i, j, lewy);
    //        }
    //    }
    //    to_return.Apply();
    //    return to_return;
    //}

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
    //public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    //{
    //    byte[] _bytes = _texture.EncodeToPNG();
    //    System.IO.File.WriteAllBytes(_fullPath, _bytes);
    //    //Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    //}
}
