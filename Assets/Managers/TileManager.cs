using System.Collections.Generic;
using System.IO;
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
	/// <summary>
	/// Returns real dimensions of rotated tile. If given tile is 1x2 and it's rotated by 90deg, this returns 2x1
	/// </summary>
	/// <param name="name"></param>
	/// <param name="IsRotated"></param>
	/// <returns></returns>
	public static Vector2Int GetRealDims(string name, bool IsRotated)
	{
		return new Vector2Int(IsRotated ? TileListInfo[name].Size.y : TileListInfo[name].Size.x, IsRotated ? TileListInfo[name].Size.x : TileListInfo[name].Size.y);
	}
	public static void LoadTiles()
	{
		if (Loaded) return;

		TileListInfo = new Dictionary<string, TileListEntry>(270);
		Materials = new List<MaterialListEntry>(30);

		// unpack default cpks
		PackageManager.LoadDefaultCPKs();

		// default cat and cfl files
		ReadCatFiles(IO.GetCrashdayPath() + "\\data\\content\\editor\\");
		ReadCflFiles(IO.GetCrashdayPath() + "\\data\\content\\tiles\\");

		// unpack every bin (zip) tileset if it's id exists in WorkshopModIds to unpacked crashday content folder
		string[] WorkshopModIds = File.ReadAllLines(Application.dataPath + "\\StreamingAssets\\tilesets.txt");

		if (WorkshopModIds.Length == 1 && WorkshopModIds[0] == "")
			return;

		string CdWorkshopPath = NavigateDirUp(IO.GetCrashdayPath(), 2) + "\\workshop\\content\\508980\\";
		//Unpack custom tilesets
		foreach (string Id in WorkshopModIds)
			PackageManager.LoadCPK(Directory.GetFiles(CdWorkshopPath + Id).First(), Id);

		// load custom tilesets
		foreach (string Id in WorkshopModIds)
		{
			ReadCatFiles(IO.GetCrashdayPath() + "\\moddata\\" + Id + "\\content\\editor\\");
			ReadCflFiles(IO.GetCrashdayPath() + "\\moddata\\" + Id + "\\content\\tiles\\", Id);
		}

		// load flatters
		LoadFlatterInfo();
	}
	/// <summary>
	/// Handles tile positioning on GUI as well as tilesets. Has to be run before ReadCflFiles
	/// </summary>
	private static void ReadCatFiles(string path)
	{
		// if no editor folder is present, return
		if (!Directory.GetParent(path).Exists)
			return;

		string[] Catfiles = Directory.GetFiles(path, "*.cat"); // get paths
		for (int i = 0; i < Catfiles.Length; i++)
			Catfiles[i] = File.ReadAllText(Catfiles[i]); // get text

		foreach (var bulk in Catfiles)
		{
			// delete tabs and split by newline characters
			string[] file = bulk.Replace("\t", "").Split(new string[] { "\r\n" }, System.StringSplitOptions.None);

			for (int j = 0; j < file.Length; j++)
				file[j] = IO.RemoveComment(file[j]);
			// editor doesn't support dynamic objects
			if (file[0] == "DYNAMICS")
				continue;
			{
				int j = 4; //start reading .cat file from 4th line
				while (j < file.Length)
				{
					string setName = TranslateTilesetName(file[j]);
					//cfl file has unnecessary blank lines at the bottom. => End reading.
					if (setName == "")
						break;

					int InCategory = int.Parse(file[j + 1]);

					for (int k = j; k < j + InCategory; k++)
					{
						string[] cat = Regex.Split(file[k + 3], " ");
						// example: RwToArenaFloor.p3d => rwtoarenafloor
						string name = cat[0].Remove(cat[0].IndexOf(".cfl")).ToLower();
						if (!TileListInfo.ContainsKey(name))
							TileListInfo.Add(name, new TileListEntry(setName));
					}
					j += InCategory + 5;
				}
			}
		}
	}



	private static void LoadFlatterInfo()
	{
		TextAsset Txt = Resources.Load("flatters") as TextAsset;
		string File = Txt.text;
		string[] lines = Regex.Split(File, "\n");
		foreach (var line in lines)
		{
			string[] brkLine = Regex.Split(line, " ");
			string name = brkLine[0];
			float[] flatters = new float[brkLine.Length - 1];
			for (int i = 0; i < flatters.Length; i++)
				flatters[i] = float.Parse(brkLine[i + 1], System.Globalization.CultureInfo.InvariantCulture);

			if (!TileListInfo.ContainsKey(name))
				TileListInfo.Add(name, new TileListEntry(flatters));
			else
				TileListInfo[name].FlatterPoints = flatters;
		}
	}
	/// <summary>
	/// Loads all Cfl files using directory
	/// </summary>
	/// <param name="Dir"></param>
	private static void ReadCflFiles(string Dir, string mod_id = null)
	{
		string[] cfls = System.IO.Directory.GetFiles(Dir, "*.cfl");
		foreach (var File in cfls)
		{
			string[] cfl = System.IO.File.ReadAllLines(File);
			string name = Path.GetFileNameWithoutExtension(File);
			for (int j = 0; j < cfl.Length; j++)
				cfl[j] = IO.RemoveComment(cfl[j]);

			string modelName = cfl[2].Remove(cfl[2].LastIndexOf('.')).ToLower();

			// field is just block of grass. It's listed in cfl file but isn't showed. Mica used this tile as CP (weird right?) so I had to take this into consideration
			if ((modelName == "field" && mod_id == null) || modelName == "border1" || modelName == "border2")
				continue;
			string[] size_str = Regex.Split(cfl[3], " ");
			Vector2Int size = new Vector2Int(int.Parse(size_str[0]), int.Parse(size_str[1]));

			bool IsCheckpoint = cfl[9] == "1" ? true : false;
			int offset = IsCheckpoint ? 1 : 0; // checkpoints have one additional parameter for checkpoint dimensions, which moves forward all other parameters by one line
			string Restrictions = Regex.Replace(cfl[12 + offset], " ", "");
			// remove STOP characters
			Restrictions = Restrictions.Remove(Restrictions.Length - 4);
			string Vegetation_str = cfl[16 + offset];

			List<Vegetation> VegData = new List<Vegetation>();
			if (Vegetation_str != "NO_VEGETATION" && Vegetation_str != "SAME_VEGETATION_AS")
			{
				int bushCount = int.Parse(Vegetation_str);
				for (int j = 0; j < bushCount; j++)
				{
					string VegName = cfl[19 + offset + 7 * j];
					VegName = VegName.Substring(0, VegName.IndexOf('.'));
					// don't load grass and small vegetation. We are interested only in big trees
					// TODO
					//if (!Consts.AllowedBushes.Contains(VegName))
					//  continue;
					//string[] xz = Regex.Split(cfl[21 + offset + 7 * j], " ");
					//float x = float.Parse(xz[0], System.Globalization.CultureInfo.InvariantCulture);
					//float z = float.Parse(xz[1], System.Globalization.CultureInfo.InvariantCulture);
					//string y = cfl[23 + offset + 7 * j];
					//VegData.Add(new Vegetation(VegName, x, z, y));
				}
			}
			// load icon of tile
			Texture2D texture = Texture2D.blackTexture;
			string[] files = Directory.GetFiles(NavigateDirUp(Dir, 2) + "\\textures\\pictures\\tiles\\", name + ".*");
			files = files.Where(f => f.Contains("dds") || f.Contains("tga")).ToArray();
			if (files.Length == 0)
			{
				string datapath = IO.GetCrashdayPath() + "\\data\\content\\textures\\pictures\\tiles\\" + name + ".tga";
				texture = TgaDecoder.LoadTGA(datapath);
			}
			else if (files[0].Contains(".tga")) // tga format
				texture = TgaDecoder.LoadTGA(files[0]);
			else
			{
				// file is in dds format
				string ddsFilePath = NavigateDirUp(Dir, 2) + "\\textures\\pictures\\tiles\\" + name + ".dds";
				byte[] bytes = System.IO.File.ReadAllBytes(ddsFilePath);
				texture = DDSDecoder.LoadTextureDXT(bytes, TextureFormat.DXT1);
			}

			string Model_path = NavigateDirUp(Dir, 2) + "\\models\\" + modelName + ".p3d";

			if (!System.IO.File.Exists(Model_path))
			{ // look for model in original files (used in mica's tiles)
				Model_path = IO.GetCrashdayPath() + "\\data\\content\\models\\" + modelName + ".p3d";
				if (!System.IO.File.Exists(Model_path))
				{
					Debug.LogWarning("No p3d for tile " + name);
					continue;
				}
			}

			P3DModel model = P3DParser.LoadFromFile(Model_path);
			List<Material> ModelMaterials = new List<Material>(model.P3DNumTextures);

			for (int j = 0; j < model.P3DNumTextures; j++)
			{
				MaterialListEntry m = Materials.FirstOrDefault(x => x.Name == model.P3DRenderInfo[j].TextureFile);
				if (m == default(MaterialListEntry))
				{
					m = new MaterialListEntry
					{
						Material = model.CreateMaterial(j, mod_id),
						Name = model.P3DRenderInfo[j].TextureFile
					};
					Materials.Add(m);
				}

				ModelMaterials.Add(m.Material);
			}

			if (!TileListInfo.ContainsKey(name))
			{ // This tile doesn't exist in editor folder -> no category specified -> set tilesetkey to 0. ("default" tab)
				TileListInfo.Add(name, new TileListEntry(size, Restrictions, IsCheckpoint, model, ModelMaterials, texture, VegData.ToArray(), mod_id));
				TileListInfo[name].TilesetName = Consts.DefaultTilesetName;
			}
			else
				TileListInfo[name].Set(size, Restrictions, IsCheckpoint, model, ModelMaterials, texture, VegData.ToArray(), mod_id);
			if (IsCheckpoint)
				TileListInfo[name].TilesetName = Consts.CHKPOINTS_STR;
		}

	}

	private static string TranslateTilesetName(string name)
	{
		if (name == "$ID trkdata/editor/fields.cat CategorySet1")
			return "Roads";
		if (name == "$ID trkdata/editor/fields.cat CategorySet2")
			return "Trunk roads";
		if (name == "$ID trkdata/editor/fields.cat CategorySet3")
			return "Unsealed roads";
		if (name == "$ID trkdata/editor/fields.cat CategorySet4")
			return "Racetrack";
		if (name == "$ID trkdata/editor/fields.cat CategorySet5")
			return "Arena";
		if (name == "$ID trkdata/editor/fields.cat CategorySet6")
			return "Elevated";
		if (name == "$ID trkdata/editor/fields.cat CategorySet7")
			return "Tunnels";
		if (name == "$ID trkdata/editor/fields.cat CategorySet8")
			return "Tunnels 2";
		if (name == "$ID trkdata/editor/fields.cat CategorySet9")
			return "Stunt Set";
		if (name == "$ID trkdata/editor/fields.cat CategorySet10")
			return "Industrial";
		if (name == "$ID trkdata/editor/fields.cat CategorySet11")
			return "Buildings";
		if (name == "$ID trkdata/editor/fields.cat CategorySet12")
			return "Nature";
		if (name == "$ID trkdata/editor/cps.cat CategorySet1")
			return Consts.CHKPOINTS_STR;
		name = name.Replace('/', ' ').Replace("\\", "");
		return name;
	}
	/// <summary>
	/// Navigates directory n levels up. Directory has to end with slashes
	/// </summary>
	/// <param name="dir">directory</param>
	/// <param name="n">number of levels</param>
	/// <returns></returns>
	private static string NavigateDirUp(string dir, int n)
	{
		for (int i = 0; i < n; i++)
			dir = Directory.GetParent(dir).ToString();
		return dir;
	}
}

