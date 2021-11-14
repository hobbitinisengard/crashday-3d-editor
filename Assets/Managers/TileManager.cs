using System;
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

public class TilesetListEntry
{
	public List<string> TileSections;
	public bool Enabled;
}

public static class TileManager
{
	public static Dictionary<string, TileListEntry> TileListInfo = new Dictionary<string, TileListEntry>(270);
	/// <summary>
    /// Custom tileset IDs and their tile sections for tileset manager menu
    /// </summary>
	public static Dictionary<string, TilesetListEntry> CustomTileSections;
	/// <summary>
	/// Keys - tile names; values - ID(s) of mod(s) overriding these tiles. 
	/// </summary>
	public static Dictionary<string, List<string>> DefaultTiles;
	public static List<MaterialListEntry> Materials;

	public static bool Loaded;
	public static string CdWorkshopPath = NavigateDirUp(IO.GetCrashdayPath(), 2) + "\\workshop\\content\\508980\\";
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

		CustomTileSections = new Dictionary<string, TilesetListEntry>();
		Materials = new List<MaterialListEntry>(30);
		DefaultTiles = new Dictionary<string, List<string>>(270);

		PackageManager.LoadDefaultCPKs();
		LoadDefaultTiles();
 
		string[] WorkshopModIds = File.ReadAllLines(Consts.tilesets_path).Distinct().ToArray();
		if (WorkshopModIds.Length == 1 && WorkshopModIds[0] == "")
			return;

		// Unpack and load custom tilesets
		foreach (string Id in WorkshopModIds)
		{
			// A single "#" character before the tileset ID marks it as disabled, so we only need its tile sections.
			bool enabled = true;
			string newId = Id.Trim();
			if (Id.StartsWith("#"))
			{
				newId = Id.Remove(0, 1);
				enabled = false;
			}
			if (Directory.Exists(CdWorkshopPath + newId + "\\")) // Check if the ID is correct
			{
				PackageManager.LoadCPK(Directory.GetFiles(CdWorkshopPath + newId).First(), newId);
				LoadCustomTiles(newId, enabled);
			}
		}
	}

	/// <summary>
	/// Loads default tiles from the data folder.
	/// </summary>
	public static void LoadDefaultTiles(string filter = null)
	{
		List<string> cfls = ReadCflFiles(IO.GetCrashdayPath() + "\\data\\content\\tiles\\", null, filter);
		if (filter == null)
			// Loading default tiles for the first time
			foreach (string cfl in cfls)
				DefaultTiles.Add(cfl, new List<string>());

		ReadCatFiles(IO.GetCrashdayPath() + "\\data\\content\\editor\\", ref cfls, null, true, filter);
		foreach (string cfl in cfls)
		{
			// This tile doesn't exist in editor folder -> no category specified -> set tilesetkey to 0. ("default" tab)
			if (TileListInfo[cfl].IsCheckpoint)
				TileListInfo[cfl].TilesetName = Consts.CHKPOINTS_STR;
			else
				TileListInfo[cfl].TilesetName = Consts.DefaultTilesetName;
		}
		LoadFlatterInfo();
	}

	/// <summary>
	/// Loads custom tiles from the moddata folder.
	/// </summary>
	public static void LoadCustomTiles(string mod_id, bool enabled = true, string filter = null)
	{
		List<string> cfls = new List<string>();
		if (enabled)
		{
			cfls = ReadCflFiles(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\content\\tiles\\", mod_id, filter);
			if (filter == null)
				foreach (string cfl in cfls)
					if (DefaultTiles.ContainsKey(cfl))
						DefaultTiles[cfl].Add(mod_id);
		}
		ReadCatFiles(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\content\\editor\\", ref cfls, mod_id, enabled, filter);
		if (enabled)
			foreach (string cfl in cfls)
			{
				// This tile doesn't exist in editor folder -> no category specified -> set tilesetkey to 0. ("default" tab)
				if (TileListInfo[cfl].IsCheckpoint)
					TileListInfo[cfl].TilesetName = Consts.CHKPOINTS_STR;
				else
					TileListInfo[cfl].TilesetName = Consts.DefaultTilesetName;
			}
		LoadFlatterInfo();
	}

	/// <summary>
	/// Reloads tiles previously overridden by the specified mod.
	/// </summary>
	public static void UpdateSpecificTiles(string mod_being_disabled)
    {
		foreach (List<string> mods in DefaultTiles.Values)
			if (mods.Count() == 1 && mods[0] == mod_being_disabled)
			{
				LoadDefaultTiles(mod_being_disabled);
				break;
			}
		List<string> mods_to_reload = new List<string>();
		foreach (List<string> mods in DefaultTiles.Values)
		{
			Debug.Log(mods.Count());
			if (mods.Count() > 1 && mods[mods.Count() - 1] == mod_being_disabled) // another mod overrides this tile
				mods_to_reload.Add(mods[mods.Count() - 2]);
		}
		mods_to_reload = mods_to_reload.Distinct().ToList();
		foreach (string mod in mods_to_reload)
			LoadCustomTiles(mod, true, mod_being_disabled);

		foreach (string tile in DefaultTiles.Keys)
		{
			List<string> mods = DefaultTiles[tile];
			if (mods.Contains(mod_being_disabled))
				mods.Remove(mod_being_disabled);
		}
    }

	/// <summary>
	/// Loads all Cfl files using directory. Has to be run before ReadCatFiles.
	/// </summary>
	/// <param name="Dir"></param>
	private static List<string> ReadCflFiles(string Dir, string mod_id = null, string mod_to_filter_by = null)
	{
		List<string> names = new List<string>();
		string[] cfls = Directory.GetFiles(Dir, "*.cfl");
		foreach (var File in cfls)
		{
			string name = Path.GetFileNameWithoutExtension(File);
			if (mod_to_filter_by != null && DefaultTiles.ContainsKey(name))
			{
				List<string> mods = DefaultTiles[name];
				if (mods.Count() != 0 && mods[mods.Count - 1] != mod_to_filter_by)
					continue;
			}
			string[] cfl = System.IO.File.ReadAllLines(File);

			for (int j = 0; j < cfl.Length; j++)
				cfl[j] = IO.RemoveComment(cfl[j]);

			if (cfl[2].LastIndexOf('.') < 0)
				Debug.Log("modelName");
			string modelName = cfl[2].Remove(cfl[2].LastIndexOf('.')).ToLower();

			// field is just block of grass. It's listed in cfl file but isn't showed. Mica used this tile as CP (weird right?) so I had to take this into consideration
			if (modelName == "field" && mod_id == null || modelName == "border1" || modelName == "border2")
				continue;
			string[] size_str = Regex.Split(cfl[3], " ");
			Vector2Int size = new Vector2Int(int.Parse(size_str[0]), int.Parse(size_str[1]));

			bool IsCheckpoint = cfl[9] == "1" ? true : false;
			int offset = cfl[13].Contains("STOP") ? 1 : 0; // checkpoints have one additional parameter for checkpoint dimensions, which moves forward all other parameters by one line
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
			if (Directory.Exists(NavigateDirUp(Dir, 2) + "\\textures\\pictures\\tiles\\"))
			{
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
			}
			else
			{
				texture = Resources.Load<Texture2D>("flag");
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

			if (TileListInfo.ContainsKey(name))
				TileListInfo[name].Set(size, Restrictions, IsCheckpoint, model, ModelMaterials, texture, VegData.ToArray(), mod_id);
			else
				TileListInfo.Add(name, new TileListEntry(size, Restrictions, IsCheckpoint, model, ModelMaterials, texture, VegData.ToArray(), mod_id));

			names.Add(name);
		}
		return names;
	}

	/// <summary>
	/// Handles tile positioning on GUI as well as tilesets.
	/// </summary>
	private static void ReadCatFiles(string path, ref List<string> cfls_to_hide, string mod_id = null, bool enabled = true, string mod_to_filter_by = null)
	{
		// if no editor folder is present, return
		if (!Directory.GetParent(path).Exists)
			return;

		if (mod_id != null)
		{
			CustomTileSections[mod_id] = new TilesetListEntry { TileSections = new List<string>(), Enabled = enabled };
		}

		string[] Catfiles = Directory.GetFiles(path, "*.cat"); // get paths
		for (int i = 0; i < Catfiles.Length; i++)
			Catfiles[i] = File.ReadAllText(Catfiles[i]); // get text

		foreach (var bulk in Catfiles)
		{
			// delete tabs and split by newline characters
			string[] file = bulk.Replace("\t", "").Split(new string[] { "\r\n" }, System.StringSplitOptions.None);

			for (int k = 0; k < file.Length; k++)
				file[k] = IO.RemoveComment(file[k]);
			// editor doesn't support dynamic objects
			if (file[0] == "DYNAMICS")
				continue;

			int j = 4; //start reading .cat file from 4th line
			while (j < file.Length)
			{
				string setName = TranslateTilesetName(file[j]);
				//cfl file has unnecessary blank lines at the bottom. => End reading.
				if (setName == "")
					break;

				int InCategory = int.Parse(file[j + 1]);

				if (mod_id != null)
				{
					CustomTileSections[mod_id].TileSections.Add(setName);
					if (!enabled)
					{
						j += InCategory + 5;
						continue;
					}
				}
				for (int k = j; k < j + InCategory; k++)
				{
					string[] cat = Regex.Split(file[k + 3], " ");
					// example: RwToArenaFloor.p3d => rwtoarenafloor
					string name = cat[0].Remove(cat[0].IndexOf(".cfl")).ToLower();
					if (mod_to_filter_by != null && DefaultTiles.ContainsKey(name))
					{
						List<string> mods = DefaultTiles[name];
						if (mods.Count() != 0 && mods[mods.Count - 1] != mod_to_filter_by)
							continue;
					}
					string description = TranslateTileDescription(string.Join(" ", new ArraySegment<string>(cat, 1, cat.Length - 1).Where(x => !int.TryParse(x, out int i))));
					if (TileListInfo.ContainsKey(name))
						TileListInfo[name].Set(setName, description);
					if (cfls_to_hide.Contains(name))
						cfls_to_hide.Remove(name);
				}
				j += InCategory + 5;
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

			if (TileListInfo.ContainsKey(name))
				TileListInfo[name].FlatterPoints = flatters;
		}
	}

	private static string TranslateTilesetName(string name)
	{
		switch (name)
		{
			case "$ID trkdata/editor/fields.cat CategorySet1":
				return "Roads";
			case "$ID trkdata/editor/fields.cat CategorySet2":
				return "Country & Highway";
			case "$ID trkdata/editor/fields.cat CategorySet3":
				return "Dirt Roads";
			case "$ID trkdata/editor/fields.cat CategorySet4":
				return "Race Track";
			case "$ID trkdata/editor/fields.cat CategorySet5":
				return "Arena & Tribune";
			case "$ID trkdata/editor/fields.cat CategorySet6":
				return "Elevated";
			case "$ID trkdata/editor/fields.cat CategorySet7":
			case "$ID trkdata/editor/fields.cat CategorySet8":
				return "Tunnels";
			case "$ID trkdata/editor/fields.cat CategorySet9":
				return "Stunt Set";
			case "$ID trkdata/editor/fields.cat CategorySet10":
				return "Industrial";
			case "$ID trkdata/editor/fields.cat CategorySet11":
				return "Buildings";
			case "$ID trkdata/editor/fields.cat CategorySet12":
				return "Decoration";
			case "$ID trkdata/editor/cps.cat CategorySet1":
				return Consts.CHKPOINTS_STR;
			default:
				Regex regex = new Regex(@"\\\d?");
				name = regex.Replace(name, "").Replace("/", " ");
				return name;
		}
	}

	private static string TranslateTileDescription(string description)
	{
		switch (description)
		{
			case "$ID trkdata/editor/cps.cat chkarena.cfl.TrackPiece":
				return "CP On Tarmac";
			case "$ID trkdata/editor/cps.cat chkbrg.cfl.TrackPiece":
				return "CP On Elevated";
			case "$ID trkdata/editor/cps.cat chkcr.cfl.TrackPiece":
				return "CP On Country Road";
			case "$ID trkdata/editor/cps.cat chkdr.cfl.TrackPiece":
				return "CP On Dirt Road";
			case "$ID trkdata/editor/cps.cat chkfield.cfl.TrackPiece":
				return "CP On Grass";
			case "$ID trkdata/editor/cps.cat chkhw.cfl.TrackPiece":
				return "CP On Highway";
			case "$ID trkdata/editor/cps.cat chkind.cfl.TrackPiece":
				return "CP On Concrete";
			case "$ID trkdata/editor/cps.cat chkpoint.cfl.TrackPiece":
				return "CP On Road";
			case "$ID trkdata/editor/cps.cat chkpoint2.cfl.TrackPiece":
				return "CP Raised 1";
			case "$ID trkdata/editor/cps.cat chkpoint3.cfl.TrackPiece":
				return "CP Raised 2";
			case "$ID trkdata/editor/cps.cat chkpoint4.cfl.TrackPiece":
				return "CP Raised 3";
			case "$ID trkdata/editor/cps.cat chksubtun.cfl.TrackPiece":
				return "CP In Tunnel";
			case "$ID trkdata/editor/cps.cat rwcp.cfl.TrackPiece":
				return "CP On Race Track";
			case "$ID trkdata/editor/cps.cat rwstart.cfl.TrackPiece":
				return "Starting Grid";
			case "$ID trkdata/editor/fields.cat 4sjumper.cfl.TrackPiece":
				return "4-Side Jump";
			case "$ID trkdata/editor/fields.cat alley.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat cralley.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirtalley.cfl.TrackPiece":
				return "Tree Alley";
			case "$ID trkdata/editor/fields.cat arenacurve.cfl.TrackPiece":
				return "Curve";
			case "$ID trkdata/editor/fields.cat arenacurve2.cfl.TrackPiece":
				return "Inverse Curve";
			case "$ID trkdata/editor/fields.cat arenacurvec.cfl.TrackPiece":
				return "Curve Roofed";
			case "$ID trkdata/editor/fields.cat arenaend.cfl.TrackPiece":
				return "Ending";
			case "$ID trkdata/editor/fields.cat arenaentry.cfl.TrackPiece":
				return "Side Entry Road";
			case "$ID trkdata/editor/fields.cat arenaentryc.cfl.TrackPiece":
				return "Side Entry Roofed Road";
			case "$ID trkdata/editor/fields.cat arenafloor.cfl.TrackPiece":
				return "Tarmac Floor";
			case "$ID trkdata/editor/fields.cat arenapillar.cfl.TrackPiece":
				return "Roof";
			case "$ID trkdata/editor/fields.cat arenastr.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirtroad.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat rwstreet.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat street.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat streetb1.cfl.TrackPiece":
				return "Straight";
			case "$ID trkdata/editor/fields.cat arenastrc.cfl.TrackPiece":
				return "Straight Roofed";
			case "$ID trkdata/editor/fields.cat barreltrailer.cfl.TrackPiece":
				return "Trailer With Explosives";
			case "$ID trkdata/editor/fields.cat barricade1.cfl.TrackPiece":
				return "Wood/Concrete Barricade 1";
			case "$ID trkdata/editor/fields.cat barricade2.cfl.TrackPiece":
				return "Wood/Concrete Barricade 2";
			case "$ID trkdata/editor/fields.cat belt1.cfl.TrackPiece":
				return "Conveyor Belt";
			case "$ID trkdata/editor/fields.cat belt2.cfl.TrackPiece":
				return "Conveyor Belt Upwards";
			case "$ID trkdata/editor/fields.cat bigjump.cfl.TrackPiece":
				return "Wide Small Ramp Road";
			case "$ID trkdata/editor/fields.cat bigwave.cfl.TrackPiece":
				return "Bump-Up Jump Road";
			case "$ID trkdata/editor/fields.cat billboard.cfl.TrackPiece":
				return "Town Billboard";
			case "$ID trkdata/editor/fields.cat billboard2.cfl.TrackPiece":
				return "Racetrack Billboard";
			case "$ID trkdata/editor/fields.cat bldside.cfl.TrackPiece":
				return "Road Works";
			case "$ID trkdata/editor/fields.cat brgdamage.cfl.TrackPiece":
				return "Straight Destroyed";
			case "$ID trkdata/editor/fields.cat brgjump.cfl.TrackPiece":
				return "Straight Lifted Destroyed";
			case "$ID trkdata/editor/fields.cat brgjump2.cfl.TrackPiece":
				return "Straight Lifted";
			case "$ID trkdata/editor/fields.cat brgjump3.cfl.TrackPiece":
				return "Straight With Ramp";
			case "$ID trkdata/editor/fields.cat brgloop.cfl.TrackPiece":
				return "Huge Looping Road/Bridge";
			case "$ID trkdata/editor/fields.cat bridge1.cfl.TrackPiece":
				return "Straight Above Grass";
			case "$ID trkdata/editor/fields.cat bridge2.cfl.TrackPiece":
				return "Straight Above Road";
			case "$ID trkdata/editor/fields.cat bridge3.cfl.TrackPiece":
				return "Straight Above Highway";
			case "$ID trkdata/editor/fields.cat bridge4.cfl.TrackPiece":
				return "Straight Above Racetrack";
			case "$ID trkdata/editor/fields.cat bridge5.cfl.TrackPiece":
				return "Straight Above Country Road";
			case "$ID trkdata/editor/fields.cat bridge6.cfl.TrackPiece":
				return "Straight Above Dirt Road";
			case "$ID trkdata/editor/fields.cat buildpit.cfl.TrackPiece":
				return "Building Pit";
			case "$ID trkdata/editor/fields.cat camera1.cfl.TrackPiece":
				return "Camera 1m";
			case "$ID trkdata/editor/fields.cat camera2.cfl.TrackPiece":
				return "Camera 5m";
			case "$ID trkdata/editor/fields.cat camera3.cfl.TrackPiece":
				return "Camera 10m";
			case "$ID trkdata/editor/fields.cat church.cfl.TrackPiece":
				return "Church";
			case "$ID trkdata/editor/fields.cat concrete.cfl.TrackPiece":
				return "Concrete Floor";
			case "$ID trkdata/editor/fields.cat containers1.cfl.TrackPiece":
				return "Containers 1";
			case "$ID trkdata/editor/fields.cat containers2.cfl.TrackPiece":
				return "Containers 2";
			case "$ID trkdata/editor/fields.cat contcrane.cfl.TrackPiece":
				return "Container Crane";
			case "$ID trkdata/editor/fields.cat contjump.cfl.TrackPiece":
				return "Container Jump";
			case "$ID trkdata/editor/fields.cat coolingtower.cfl.TrackPiece":
				return "Cooling Tower";
			case "$ID trkdata/editor/fields.cat corkscrew.cfl.TrackPiece":
				return "Corkscrew Road";
			case "$ID trkdata/editor/fields.cat crane1.cfl.TrackPiece":
				return "Huge Crane";
			case "$ID trkdata/editor/fields.cat crane2.cfl.TrackPiece":
				return "Huge Crane Carrying Tube";
			case "$ID trkdata/editor/fields.cat crcrossing.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat crossing.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat crossing2.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat crossing3.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirtcrossing.cfl.TrackPiece":
				return "Intersection";
			case "$ID trkdata/editor/fields.cat crcrvdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat crvdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat drcrvdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat rwdiago1.cfl.TrackPiece":
				return "45 Deg Curve";
			case "$ID trkdata/editor/fields.cat crdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat drdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat rwdiago2.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat strdiag.cfl.TrackPiece":
				return "45 Deg Straight";
			case "$ID trkdata/editor/fields.cat crend.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirtend.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat hwend.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat rwend.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat strend.cfl.TrackPiece":
				return "Ending In Nowhere";
			case "$ID trkdata/editor/fields.cat crjump1.cfl.TrackPiece":
				return "Small Wooden Ramp Country Road";
			case "$ID trkdata/editor/fields.cat crjump2.cfl.TrackPiece":
				return "Broken Road Country Road";
			case "$ID trkdata/editor/fields.cat crjump3.cfl.TrackPiece":
				return "Huge Ramp Country Road";
			case "$ID trkdata/editor/fields.cat croil.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat rwoil.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat stroil.cfl.TrackPiece":
				return "Oil Slick";
			case "$ID trkdata/editor/fields.cat crtcrvdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat crtcrvdiag2.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat strdiag2.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat tcrvdiag.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat tcrvdiag2.cfl.TrackPiece":
				return "45 Deg T-Intersection";
			case "$ID trkdata/editor/fields.cat crtcurve.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirttcurve.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat tcurve.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat tcurve2.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat tcurve3.cfl.TrackPiece":
				return "T-Interesetion";
			case "$ID trkdata/editor/fields.cat crtcurvedirt.cfl.TrackPiece":
				return "T-Intersection Country Road";
			case "$ID trkdata/editor/fields.cat crtcurvestr.cfl.TrackPiece":
				return "T-Intersection Road";
			case "$ID trkdata/editor/fields.cat crtodirt.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat hwtocr.cfl.TrackPiece":
				return "Connection Country Road";
			case "$ID trkdata/editor/fields.cat curvbrg1.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat curve3.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat curveb1.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirtcurve2.cfl.TrackPiece":
				return "Big Turn";
			case "$ID trkdata/editor/fields.cat curvbrg2.cfl.TrackPiece":
				return "Curved Entry";
			case "$ID trkdata/editor/fields.cat curve1.cfl.TrackPiece":
				return "Tigth Turn 1";
			case "$ID trkdata/editor/fields.cat curve2.cfl.TrackPiece":
				return "Tight Turn 2";
			case "$ID trkdata/editor/fields.cat curve4.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat curveb2.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat dirtcurve3.cfl.TrackPiece":
				return "Big Sloped Turn";
			case "$ID trkdata/editor/fields.cat deadend.cfl.TrackPiece":
				return "Dead End";
			case "$ID trkdata/editor/fields.cat deathwall.cfl.TrackPiece":
				return "Suicide Wall";
			case "$ID trkdata/editor/fields.cat desbuild.cfl.TrackPiece":
				return "Destroued Building";
			case "$ID trkdata/editor/fields.cat diner.cfl.TrackPiece":
				return "Diner";
			case "$ID trkdata/editor/fields.cat dirt4sjumper.cfl.TrackPiece":
				return "Bump Intersection";
			case "$ID trkdata/editor/fields.cat dirtcurve1.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat rwcurve1.cfl.TrackPiece":
				return "Sharp Turn";
			case "$ID trkdata/editor/fields.cat dirtjump1.cfl.TrackPiece":
				return "Motocross Jump 1";
			case "$ID trkdata/editor/fields.cat dirtjump2.cfl.TrackPiece":
				return "Motocross Jump 2";
			case "$ID trkdata/editor/fields.cat dirtjump3.cfl.TrackPiece":
				return "Long Motocross Jump";
			case "$ID trkdata/editor/fields.cat dirtjump4.cfl.TrackPiece":
				return "Wooden Ramp";
			case "$ID trkdata/editor/fields.cat dirtroad2.cfl.TrackPiece":
				return "Straight 2";
			case "$ID trkdata/editor/fields.cat dirtsunken.cfl.TrackPiece":
				return "Sunken";
			case "$ID trkdata/editor/fields.cat dirttostr.cfl.TrackPiece":
			case "$ID trkdata/editor/fields.cat strtostrb.cfl.TrackPiece":
				return "Connection Road";
			case "$ID trkdata/editor/fields.cat dirttunnel.cfl.TrackPiece":
				return "Rocky Border";
			case "$ID trkdata/editor/fields.cat drivway1.cfl.TrackPiece":
				return "Straight-Up Piece 1 Road";
			case "$ID trkdata/editor/fields.cat drivway2.cfl.TrackPiece":
				return "Straight-Up Piece 2 Road";
			case "$ID trkdata/editor/fields.cat extreme.cfl.TrackPiece":
				return "The Big Sickness Road";
			case "$ID trkdata/editor/fields.cat factory.cfl.TrackPiece":
				return "Factory Building";
			case "$ID trkdata/editor/fields.cat farm1.cfl.TrackPiece":
				return "Farm 1";
			case "$ID trkdata/editor/fields.cat farm2.cfl.TrackPiece":
				return "Farm 2";
			case "$ID trkdata/editor/fields.cat fldlight.cfl.TrackPiece":
				return "Floodlight";
			case "$ID trkdata/editor/fields.cat fueldepot.cfl.TrackPiece":
				return "Fuel Deposit";
			case "$ID trkdata/editor/fields.cat fueltank.cfl.TrackPiece":
				return "Fuel Tanks";
			case "$ID trkdata/editor/fields.cat gasstation.cfl.TrackPiece":
				return "Gas Station";
			case "$ID trkdata/editor/fields.cat greenhouse.cfl.TrackPiece":
				return "Greenhouse";
			case "$ID trkdata/editor/fields.cat highway.cfl.TrackPiece":
				return "Straight 1 Direction";
			case "$ID trkdata/editor/fields.cat highway2.cfl.TrackPiece":
				return "Straight 2 Directions";
			case "$ID trkdata/editor/fields.cat holejump.cfl.TrackPiece":
				return "Pass-The-Hole Road";
			case "$ID trkdata/editor/fields.cat house1.cfl.TrackPiece":
				return "Residential House 1";
			case "$ID trkdata/editor/fields.cat house2.cfl.TrackPiece":
				return "Apartment House";
			case "$ID trkdata/editor/fields.cat house3.cfl.TrackPiece":
				return "Residential House 2";
			case "$ID trkdata/editor/fields.cat house4.cfl.TrackPiece":
				return "Tenement 1";
			case "$ID trkdata/editor/fields.cat house5.cfl.TrackPiece":
				return "Tenement 2";
			case "$ID trkdata/editor/fields.cat hwdamage.cfl.TrackPiece":
				return "Damaged Road";
			case "$ID trkdata/editor/fields.cat hwexit.cfl.TrackPiece":
				return "Exit Countryroad";
			case "$ID trkdata/editor/fields.cat hwjump.cfl.TrackPiece":
				return "Big Iron Ramp Highway";
			case "$ID trkdata/editor/fields.cat hwsign.cfl.TrackPiece":
				return "Road Sign 2";
			case "$ID trkdata/editor/fields.cat hwsign2.cfl.TrackPiece":
				return "Electronic Sign";
			case "$ID trkdata/editor/fields.cat hwsign3.cfl.TrackPiece":
				return "Road Sign 1";
			case "$ID trkdata/editor/fields.cat hwwall1.cfl.TrackPiece":
				return "Noise Protection Wall 2";
			case "$ID trkdata/editor/fields.cat hwwall2.cfl.TrackPiece":
				return "Noise Protection Wall 1";
			case "$ID trkdata/editor/fields.cat indfenc1.cfl.TrackPiece":
				return "Fence Line";
			case "$ID trkdata/editor/fields.cat indfenc2.cfl.TrackPiece":
				return "Fence Corner 1";
			case "$ID trkdata/editor/fields.cat indfenc3.cfl.TrackPiece":
				return "Fence Corner 2";
			case "$ID trkdata/editor/fields.cat indgate.cfl.TrackPiece":
				return "Registration Gate";
			case "$ID trkdata/editor/fields.cat logjump.cfl.TrackPiece":
				return "Stacked Logs Jump";
			case "$ID trkdata/editor/fields.cat looping.cfl.TrackPiece":
				return "Looping Road";
			case "$ID trkdata/editor/fields.cat looping2.cfl.TrackPiece":
				return "Huge Looping Road";
			case "$ID trkdata/editor/fields.cat market.cfl.TrackPiece":
				return "Super Market";
			case "$ID trkdata/editor/fields.cat mast.cfl.TrackPiece":
				return "Wooden Mast";
			case "$ID trkdata/editor/fields.cat mbslogo.cfl.TrackPiece":
				return "Moonbyte Logo";
			case "$ID trkdata/editor/fields.cat minijump.cfl.TrackPiece":
				return "Small Ramp Road";
			case "$ID trkdata/editor/fields.cat office.cfl.TrackPiece":
				return "Office Building";
			case "$ID trkdata/editor/fields.cat plot1.cfl.TrackPiece":
				return "Parking Lot 1";
			case "$ID trkdata/editor/fields.cat plot2.cfl.TrackPiece":
				return "Parling Lot 2";
			case "$ID trkdata/editor/fields.cat plot3.cfl.TrackPiece":
				return "Parking Lot";
			case "$ID trkdata/editor/fields.cat powerplant2.cfl.TrackPiece":
				return "Cooling Unit";
			case "$ID trkdata/editor/fields.cat powerpole.cfl.TrackPiece":
				return "Powerpole";
			case "$ID trkdata/editor/fields.cat powerpole2.cfl.TrackPiece":
				return "Destroyed Powerpole";
			case "$ID trkdata/editor/fields.cat qjump.cfl.TrackPiece":
				return "Corkscrew Jump Road";
			case "$ID trkdata/editor/fields.cat ringjump.cfl.TrackPiece":
				return "Ring Jump Road";
			case "$ID trkdata/editor/fields.cat rplogo.cfl.TrackPiece":
				return "Replay Studios Logo";
			case "$ID trkdata/editor/fields.cat rwbanner1.cfl.TrackPiece":
				return "Banner";
			case "$ID trkdata/editor/fields.cat rwbridge.cfl.TrackPiece":
				return "Racetrack Elevated";
			case "$ID trkdata/editor/fields.cat rwchic.cfl.TrackPiece":
				return "Difficult Chicane";
			case "$ID trkdata/editor/fields.cat rwchicl.cfl.TrackPiece":
				return "Easy Chicane";
			case "$ID trkdata/editor/fields.cat rwcurve2.cfl.TrackPiece":
				return "Wide Turn 1";
			case "$ID trkdata/editor/fields.cat rwcurve3.cfl.TrackPiece":
				return "Wide Turn 2";
			case "$ID trkdata/editor/fields.cat rwdiago.cfl.TrackPiece":
				return "Short Diagonal";
			case "$ID trkdata/editor/fields.cat rwdist50.cfl.TrackPiece":
				return "50m Distance Sign";
			case "$ID trkdata/editor/fields.cat rwdist100.cfl.TrackPiece":
				return "100m Distance Sign";
			case "$ID trkdata/editor/fields.cat rwdist200.cfl.TrackPiece":
				return "200m Distance Sign";
			case "$ID trkdata/editor/fields.cat rwoverrw.cfl.TrackPiece":
				return "Racetrack Above Racetrack";
			case "$ID trkdata/editor/fields.cat rwpit.cfl.TrackPiece":
				return "Pitlane";
			case "$ID trkdata/editor/fields.cat rwstrwl1.cfl.TrackPiece":
				return "Enclosed On Both Sides";
			case "$ID trkdata/editor/fields.cat rwstrwl2.cfl.TrackPiece":
				return "Enclosed On One Side";
			case "$ID trkdata/editor/fields.cat rwstrwl3.cfl.TrackPiece":
				return "Enclosed On Both Sides/Ending";
			case "$ID trkdata/editor/fields.cat rwstrwl4.cfl.TrackPiece":
				return "Enclosed On One Side/Ending";
			case "$ID trkdata/editor/fields.cat rwtoarena.cfl.TrackPiece":
				return "Connection Raceway Arena";
			case "$ID trkdata/editor/fields.cat rwtoarenafloor.cfl.TrackPiece":
				return "Connection Raceway Tarmac Floor";
			case "$ID trkdata/editor/fields.cat rwtostr.cfl.TrackPiece":
				return "Pitlane Entry";
			case "$ID trkdata/editor/fields.cat rwtostr2.cfl.TrackPiece":
				return "Chicane With Track Side Exit";
			case "$ID trkdata/editor/fields.cat rwtribune1.cfl.TrackPiece":
				return "Tribune Style 1";
			case "$ID trkdata/editor/fields.cat rwtribune1b.cfl.TrackPiece":
				return "Tribune Corner Style 1";
			case "$ID trkdata/editor/fields.cat rwtribune2.cfl.TrackPiece":
				return "Tribune Style 2";
			case "$ID trkdata/editor/fields.cat rwupwards.cfl.TrackPiece":
				return "Racetrack Elevated Entry";
			case "$ID trkdata/editor/fields.cat sgncurvl.cfl.TrackPiece":
				return "Sign Left";
			case "$ID trkdata/editor/fields.cat sgncurvr.cfl.TrackPiece":
				return "Sign Right";
			case "$ID trkdata/editor/fields.cat sgnstr.cfl.TrackPiece":
				return "Sign Ahead";
			case "$ID trkdata/editor/fields.cat smokestack.cfl.TrackPiece":
				return "Smokestack";
			case "$ID trkdata/editor/fields.cat spike.cfl.TrackPiece":
				return "The Spike Road";
			case "$ID trkdata/editor/fields.cat spike2.cfl.TrackPiece":
				return "The Extreme Spike";
			case "$ID trkdata/editor/fields.cat str2brg1.cfl.TrackPiece":
				return "Straight Entry";
			case "$ID trkdata/editor/fields.cat streetl.cfl.TrackPiece":
				return "Straight With Streetlight";
			case "$ID trkdata/editor/fields.cat stripes.cfl.TrackPiece":
				return "Colored Concrete Floor";
			case "$ID trkdata/editor/fields.cat subcurve.cfl.TrackPiece":
				return "Big Turn Narrow";
			case "$ID trkdata/editor/fields.cat subentry.cfl.TrackPiece":
				return "Entry Narrow";
			case "$ID trkdata/editor/fields.cat subtcurve.cfl.TrackPiece":
				return "T-Intersection Narrow";
			case "$ID trkdata/editor/fields.cat subtcurve2.cfl.TrackPiece":
				return "Curved T-Intersection Narrow";
			case "$ID trkdata/editor/fields.cat subtun.cfl.TrackPiece":
				return "Straight 1 Narrow";
			case "$ID trkdata/editor/fields.cat subtun2.cfl.TrackPiece":
				return "Straight 2 Narrow";
			case "$ID trkdata/editor/fields.cat subtunarena.cfl.TrackPiece":
				return "Arena Crossing Narrow";
			case "$ID trkdata/editor/fields.cat subtunbrg.cfl.TrackPiece":
				return "Bridge Crossing Narrow";
			case "$ID trkdata/editor/fields.cat subtuncr.cfl.TrackPiece":
				return "Country Road Crossing Narrow";
			case "$ID trkdata/editor/fields.cat subtuncross.cfl.TrackPiece":
				return "Intersection Narrow";
			case "$ID trkdata/editor/fields.cat subtundamage.cfl.TrackPiece":
				return "Damaged Entry Narrow";
			case "$ID trkdata/editor/fields.cat subtundeadend.cfl.TrackPiece":
				return "Dead End Narrow";
			case "$ID trkdata/editor/fields.cat subtundi.cfl.TrackPiece":
				return "Sirt Road Crossing Narrow";
			case "$ID trkdata/editor/fields.cat subtunhw.cfl.TrackPiece":
				return "Highway Crossing Narrow";
			case "$ID trkdata/editor/fields.cat subtunrw.cfl.TrackPiece":
				return "Raceway Crossing Narrow";
			case "$ID trkdata/editor/fields.cat subtunstr.cfl.TrackPiece":
				return "Road Crossing Narrow";
			case "$ID trkdata/editor/fields.cat tcurveb.cfl.TrackPiece":
				return "Big Intersection";
			case "$ID trkdata/editor/fields.cat tirerow.cfl.TrackPiece":
				return "Tire Row 1";
			case "$ID trkdata/editor/fields.cat tirerow2.cfl.TrackPiece":
				return "Tire Row 2";
			case "$ID trkdata/editor/fields.cat tree1.cfl.TrackPiece":
				return "Oak Trees";
			case "$ID trkdata/editor/fields.cat tree2.cfl.TrackPiece":
				return "Poplar Trees";
			case "$ID trkdata/editor/fields.cat tree3.cfl.TrackPiece":
				return "Huge Oak Tree";
			case "$ID trkdata/editor/fields.cat tree4.cfl.TrackPiece":
				return "Birch Trees";
			case "$ID trkdata/editor/fields.cat tree5.cfl.TrackPiece":
				return "Pine Trees";
			case "$ID trkdata/editor/fields.cat treerow1.cfl.TrackPiece":
				return "Long Forest Row";
			case "$ID trkdata/editor/fields.cat treerow2.cfl.TrackPiece":
				return "Forest End";
			case "$ID trkdata/editor/fields.cat treerow3.cfl.TrackPiece":
				return "Forest Edge";
			case "$ID trkdata/editor/fields.cat treerow4.cfl.TrackPiece":
				return "Short Forest Row";
			case "$ID trkdata/editor/fields.cat truck.cfl.TrackPiece":
				return "Truck With Container";
			case "$ID trkdata/editor/fields.cat tube1.cfl.TrackPiece":
				return "Tube Entry Road";
			case "$ID trkdata/editor/fields.cat tube2.cfl.TrackPiece":
				return "Tube Straight";
			case "$ID trkdata/editor/fields.cat tube3.cfl.TrackPiece":
				return "Damaged Tube With Wooden Ramp";
			case "$ID trkdata/editor/fields.cat warehouse1.cfl.TrackPiece":
				return "Warehouse Entry";
			case "$ID trkdata/editor/fields.cat warehouse2.cfl.TrackPiece":
				return "Warehouse";
			case "$ID trkdata/editor/fields.cat warehouse3.cfl.TrackPiece":
				return "Warehouse With Ramp";
			case "$ID trkdata/editor/fields.cat windgenerator.cfl.TrackPiece":
				return "Wind Generator";
			case "$ID trkdata/editor/fields.cat woodfence1.cfl.TrackPiece":
				return "Woodfence";
			case "$ID trkdata/editor/fields.cat woodfence2.cfl.TrackPiece":
				return "Woodfence corner";
			case "$ID trkdata/editor/fields.cat wsubcurve.cfl.TrackPiece":
				return "Sharp Turn Wide";
			case "$ID trkdata/editor/fields.cat wsubtcurve.cfl.TrackPiece":
				return "T-Intersection Wide";
			case "$ID trkdata/editor/fields.cat wsubtun.cfl.TrackPiece":
				return "Straight 1 Wide";
			case "$ID trkdata/editor/fields.cat wsubtun2.cfl.TrackPiece":
				return "Straight 2 Wide";
			case "$ID trkdata/editor/fields.cat wsubtuncross.cfl.TrackPiece":
				return "Intersection Wide";
			case "$ID trkdata/editor/fields.cat wsubtundamage.cfl.TrackPiece":
				return "Damaged Entry Wide";
			case "$ID trkdata/editor/fields.cat wsubtunsplit.cfl.TrackPiece":
				return "Split Wide";
			case "$ID trkdata/editor/fields.cat wsubtuntohw.cfl.TrackPiece":
				return "Connection Tunnel Highway Wide";
			case "$ID trkdata/editor/fields.cat wsubtuntosubtun.cfl.TrackPiece":
				return "Connection Wide Narrow";
			case "$ID trkdata/editor/fields.cat zebracross.cfl.TrackPiece":
				return "Crosswalk";
			default:
				Regex regex = new Regex(@"\\\d?");
				description = regex.Replace(description, "").Replace("/", " ");
				return description;
		}
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
