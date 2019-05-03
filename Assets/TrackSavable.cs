using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class TrackSavable
{
	[XmlAttribute("time")]
    public int CurrentTime;         //unused
	[XmlAttribute("author")]
    public string Author;
	[XmlAttribute("comment")]
    public string Comment;
    /*
     * 5times
     * 
     * int32 Best2MinStuntScore 
     * char* Best2MinStuntScorePlayers (NULL terminated string) 
     * int32 Best3MinStuntScore 
     * char* Best3MinStuntScorePlayers (NULL terminated string) 
     * int32 Best5MinStuntScore 
     * char* Best5MinStuntScorePlayers (NULL terminated string) 
     * int32 BestRacingLapTime 
     * char* BestRacingLapTimePlayers (NULL terminated string)
     */
	[XmlAttribute("style")]
    public byte Style;
	[XmlAttribute("ambience")]
    public string Ambience;

	[XmlAttribute("fieldFilesNum")]
    public ushort FieldFilesNumber;
	[XmlAttribute("fieldFiles")]
    public List<string> FieldFiles;
	[XmlAttribute("width")]
    public ushort Width;
	[XmlAttribute("height")]
    public ushort Height;
	[XmlAttribute("trackTiles")]
    public List<List<TrackTileSavable>> TrackTiles;

	[XmlAttribute("dynobjsfilesnum")]
    public ushort DynamicObjectFilesNumber;
	[XmlAttribute("dynobjsfiles")]
    public List<string> DynamicObjectFiles;
	[XmlAttribute("dynobjsnum")]
    public ushort DynamicObjectsNumber;
	[XmlAttribute("dynobjs")]
    public List<DynamicObjectSavable> DynamicObjects;

	[XmlAttribute("checkpointnumber")]
    public ushort CheckpointsNumber;
	[XmlAttribute("checkpoints")]
    public List<ushort> Checkpoints;
	[XmlAttribute("permission")]
    public byte Permission;
	[XmlAttribute("bumpyness")]
    public float GroundBumpyness;
	[XmlAttribute("scenery")]
    public byte Scenery;
	[XmlAttribute("heightmap")]
    public List<List<float>> Heightmap;


	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="old">Old track to copy parameters from</param>
	public TrackSavable(TrackSavable old)
	{
		Author = old.Author;
		Comment = old.Comment;
		Style = old.Style;
		Ambience = old.Ambience;

		FieldFilesNumber = old.FieldFilesNumber;
		FieldFiles = new List<string>(FieldFilesNumber);
		foreach (var ff in old.FieldFiles)
			FieldFiles.Add(ff);

		Height = old.Height;
		Width = old.Width;

		TrackTiles = new List<List<TrackTileSavable>>(Height);

		for (int y = 0; y <  Height; y++)
		{
			TrackTiles.Add(new List<TrackTileSavable>(Width));
			for (int x = 0; x <  Width; x++)
			{
				TrackTiles[y].Add(new TrackTileSavable(old.TrackTiles[y][x]));
			}
		}

		DynamicObjectFilesNumber = old.DynamicObjectFilesNumber;
		DynamicObjectFiles = new List<string>(DynamicObjectFilesNumber);
		foreach (var dof in old.DynamicObjectFiles)
			DynamicObjectFiles.Add(dof);

		DynamicObjectsNumber = old.DynamicObjectsNumber;
		DynamicObjects = new List<DynamicObjectSavable>(DynamicObjectsNumber);
		foreach (var d in old.DynamicObjects)
			DynamicObjects.Add(d);

		CheckpointsNumber = old.CheckpointsNumber;
		Checkpoints = new List<ushort>(CheckpointsNumber);
		foreach (var cp in old.Checkpoints)
			Checkpoints.Add(cp);

		Permission = old.Permission;
		GroundBumpyness = old.GroundBumpyness;
		Scenery = old.Scenery;

		Heightmap = new List<List<float>>(Height*4+1);

		for (int y = 0; y <  Height*4 + 1; y++)
		{
			Heightmap.Add(new List<float>(Width*4+1));
			for (int x = 0; x <  Width*4 + 1; x++)
			{
				Heightmap[y].Add(old.Heightmap[y][x]);
			}
		}
	}

	/// <summary>
	/// Default constructor
	/// </summary>
	public TrackSavable()
	{
		Author = "";
		Comment = "";
		Style = 0;
		Ambience = "day.amb";

		FieldFilesNumber = 2;
		FieldFiles = new List<string>(2)
		{
			"field.cfl",
			"chkpoint.cfl"
		};

		Height = 5;
		Width = 5;

		TrackTiles = new List<List<TrackTileSavable>>(5);

		for (int y = 0; y <  Height; y++)
		{
			TrackTiles.Add(new List<TrackTileSavable>(5));
			for (int x = 0; x <  Width; x++)
			{
				TrackTileSavable tile = new TrackTileSavable(0,0,0,0);
				if(x == 2 && y == 2)
					tile = new TrackTileSavable(1,0,0,0);
				TrackTiles[y].Add(tile);
			}
		}

		DynamicObjectFilesNumber = 0;
		DynamicObjectFiles = new List<string>();

		DynamicObjectsNumber = 0;
		DynamicObjects = new List<DynamicObjectSavable>();

		CheckpointsNumber = 1;
		Checkpoints = new List<ushort>
		{
			12
		};

		Permission = 0;
		GroundBumpyness = 1.0f;
		Scenery = 0;

		Heightmap = new List<List<float>>(21);

		for (int y = 0; y <  Height*4 + 1; y++)
		{
			Heightmap.Add(new List<float>(21));
			for (int x = 0; x <  Width*4 + 1; x++)
			{
				Heightmap[y].Add(0.0f);
			}
		}
	}

    public TrackSavable(ushort newwidth, ushort newheight)
    {
        Author = "";
        Comment = "";
        Style = 0;
        Ambience = "day.amb";

        FieldFilesNumber = 1;
        FieldFiles = new List<string>()
        {
            "field.cfl",
        };

        Height = newheight;
        Width = newwidth;

        TrackTiles = new List<List<TrackTileSavable>>(Height);

        for (int y = 0; y < Height; y++)
        {
            TrackTiles.Add(new List<TrackTileSavable>(Width));
            for (int x = 0; x < Width; x++)
                TrackTiles[y].Add(new TrackTileSavable(0, 0, 0, 0));
        }

        DynamicObjectFilesNumber = 0;
        DynamicObjectFiles = new List<string>();

        DynamicObjectsNumber = 0;
        DynamicObjects = new List<DynamicObjectSavable>();

        CheckpointsNumber = 0;
        Checkpoints = new List<ushort>();

        Permission = 0;
        GroundBumpyness = 1.0f;
        Scenery = 0;

        Heightmap = new List<List<float>>(4 * Height + 1);

        for (int y = 0; y < Height * 4 + 1; y++)
        {
            Heightmap.Add(new List<float>(4 * Width + 1));
            for (int x = 0; x < Width * 4 + 1; x++)
            {
                Heightmap[y].Add(0);
            }
        }
    }
}
