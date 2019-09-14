using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class MaterialListEntry
{
  public string Name;
  public Material Material;
}


public static class TileManager
{
  public static Dictionary<string, TileListEntry> TileListInfo = new Dictionary<string, TileListEntry>(270);

  public static List<MaterialListEntry> Materials;

  public static bool Loaded;

  public static void LoadTiles()
  {
    if (Loaded) return;

    TileListInfo = new Dictionary<string, TileListEntry>(270);
    Materials = new List<MaterialListEntry>(30);

    // unpack default cpks
    PackageManager.LoadDefaultCPKs();

    // load default cat and cfl files
    ReadCatFiles("defcps.cat");
    ReadCatFiles("defflds.cat");
    ReadCflFiles(IO.GetCrashdayPath() + "/data/content/tiles/");

    // then unpack every bin (zip) tileset if it's id exists in WorkshopModIds to editor's StreamingAssets
    string[] WorkshopModIds = System.IO.File.ReadAllLines(Application.dataPath + "/StreamingAssets/tilesets.txt");
    string CdWorkshopPath = System.IO.Directory.GetParent(System.IO.Directory.GetParent(IO.GetCrashdayPath()).ToString()).ToString() + "workshop/content/508980/";

    foreach (string Id in WorkshopModIds)
    {
      string[] binfile = System.IO.Directory.GetFiles(CdWorkshopPath + Id);
      ZipManager.ExtractZipFile(binfile[0], Application.dataPath + "/StreamingAssets/" + Id);

      // load mod cat and cfl files
      ReadCatFiles(Application.dataPath + "/StreamingAssets/" + Id + "/content/editor/");
      ReadCflFiles(Application.dataPath + "/StreamingAssets/" + Id + "/content/tiles/");
    }

    

    // load flatters
    LoadFlatterInfo();
  }
  public static void ReadCatFiles(string dir)
  {
    if (System.IO.Directory.Exists(dir))
    {
      string[] files = System.IO.Directory.GetFiles(dir);
      for (int i = 0; i < files.Length; i++)
      {
        string[] file = System.IO.File.ReadAllLines(files[i]);

        for (int j = 0; j < file.Length; j++)
          file[j] = IO.RemoveComment(file[j]);
        if (file[0] == "DYNAMICS")
          continue;

        {
          int j = 4;
          while (j < file.Length)
          {
            string setName = file[j];
            int InCategory = int.Parse(file[j + 1]);
            bool AutoPositioning = bool.Parse(file[j + 2]);
            if (!AutoPositioning)
              break;

            for (int k = j; k < j + InCategory; k++)
            {
              string[] cat = Regex.Split(file[k + 3], " ");

              string name = cat[0].Remove(cat[0].Length - 3);
              Vector3Int editorpos = new Vector3Int(int.Parse(cat[1]), int.Parse(cat[2]), int.Parse(cat[3]));


              if (TileManager.TileListInfo[name] == null)
                TileManager.TileListInfo.Add(name, new TileListEntry(editorpos));
              else
                TileManager.TileListInfo[name].EditorPlacement = editorpos;
            }
            j += 2;
          }
        }

      }
    }
    else
    {
      Debug.Log("No editor folder or wrong path. This mod doesn't have cat files: " + dir);
    }
  }
  public static void LoadFlatterInfo()
  {
    TextAsset Txt = Resources.Load("rmcs.txt") as TextAsset;
    string File = Txt.text;
    string[] lines = Regex.Split(File, "\r\n");
    foreach (var line in lines)
    {
      string[] brkLine = Regex.Split(line, " ");
      string name = brkLine[0];
      float[] flatters = new float[brkLine.Length - 1];
      for (int i = 0; i < flatters.Length; i++)
        flatters[i] = float.Parse(brkLine[i + 1]);

      if (TileManager.TileListInfo[name] == null)
        TileManager.TileListInfo.Add(name, new TileListEntry(flatters));
      else
        TileManager.TileListInfo[name].FlatterPoints = flatters;
    }
  }
  public static void ReadCflFiles(string dir)
  {
    if (System.IO.Directory.Exists(dir))
    {
      string[] files = System.IO.Directory.GetFiles(dir);
      for (int i = 0; i < files.Length; i++)
      {
        string[] cfl = System.IO.File.ReadAllLines(files[i]);

        for (int j = 0; j < cfl.Length; j++)
          cfl[j] = IO.RemoveComment(cfl[j]);

        string name = cfl[2].Split('.')[0];

        string[] size_str = Regex.Split(cfl[0], " ");
        IntVector2 size = new IntVector2(int.Parse(size_str[0]), int.Parse(size_str[1]));

        string Restrictions = Regex.Replace(cfl[12], " ", "");
        // remove STOP characters
        Restrictions.Remove(Restrictions.Length - 3);

        bool IsCheckpoint = bool.Parse(cfl[9]);

        string Vegetation_str = cfl[16];

        List<Vegetation> VegData = new List<Vegetation>();
        if (Vegetation_str != "NO_VEGETATION")
        {
          int bushCount = int.Parse(Vegetation_str);
          for (int j = 0; j < bushCount; j++)
          {
            string vegName = cfl[19 + 7 * j];
            string[] xz = Regex.Split(cfl[20 + 7 * j], " ");
            float x = float.Parse(xz[0]);
            float z = float.Parse(xz[1]);
            string y = cfl[23 + 7 * j];
            VegData.Add(new Vegetation(name, x, z, y));
          }
        }
        Texture texTga = TGAParser.LoadTGA(System.IO.Directory.GetParent(dir).ToString() + "textures/pictures/tiles/" + name + ".tga");
        P3DModel model = P3DParser.LoadFromFile(System.IO.Directory.GetParent(dir).ToString() + "models/" + name + ".p3d");

        List<Material> ModelMaterials = new List<Material>(model.P3DNumTextures);

        for (int j = 0; j < model.P3DNumTextures; j++)
        {
          MaterialListEntry m = TileManager.Materials.FirstOrDefault(x => x.Name == model.P3DRenderInfo[j].TextureFile);
          if (m == default(MaterialListEntry))
          {
            m = new MaterialListEntry();
            m.Material = model.CreateMaterial(j);
            m.Name = model.P3DRenderInfo[j].TextureFile;
            TileManager.Materials.Add(m);
          }

          ModelMaterials.Add(m.Material);
        }

        if (TileManager.TileListInfo[name] == null)
          TileManager.TileListInfo.Add(name, new TileListEntry(size, RMCManager.GetRMCName(Restrictions, size), IsCheckpoint, model, ModelMaterials, texTga, VegData.ToArray()));
        else
          TileManager.TileListInfo[name].Set(size, RMCManager.GetRMCName(Restrictions, size), IsCheckpoint, model, ModelMaterials, texTga, VegData.ToArray());
      }
    }
    else
    {
      Debug.Log("No tiles folder. Probably wrong path: " + dir);
    }
  }
}
