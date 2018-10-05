using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class MapParser
{
    //reads map from file to Track object
    public static TrackSavable ReadMap(string path)
    {
        TrackSavable Track = new TrackSavable();
        List<byte> data = new List<byte>(File.ReadAllBytes(path));
        ByteFileParser io = new ByteFileParser(data);

        //ignore "CDTRK" string in the start of the file
        io.SetReadingOffest(5);

        //unused current time thing
        Track.CurrentTime = io.ReadInt();

        //author
        Track.Author = io.ReadString();

        //comment
        Track.Comment = io.ReadString();

        //skip unneeded block
        for (int n = 0; n < 20; n++)
        {
	        io.ReadInt();
	        io.ReadString();
        }

        //track style (race, derby, htf)
	    Track.Style = io.ReadByte();

        //ambience
        Track.Ambience = io.ReadString();

        //amount of files used on map
        Track.FieldFilesNumber = io.ReadUShort();

        //name of fields
        Track.FieldFiles = new List<string>(Track.FieldFilesNumber);
        for (int n = 0; n < Track.FieldFilesNumber; n++)
        {
            Track.FieldFiles.Add(io.ReadString());
        }

        //width and height in tiles
        Track.Width = io.ReadUShort();
        Track.Height = io.ReadUShort();

        Track.TrackTiles = new List<List<TrackTileSavable>>(Track.Height);

        for (int y = 0; y < Track.Height; y++)
        {
			Track.TrackTiles.Add(new List<TrackTileSavable>(Track.Width));
            for (int x = 0; x < Track.Width; x++)
            {
                TrackTileSavable newTile = new TrackTileSavable();
                newTile.FieldId = io.ReadUShort();
	            newTile.Rotation = io.ReadByte();
                newTile.IsMirrored = io.ReadByte();
                newTile.Height = io.ReadByte();

                Track.TrackTiles[y].Add(newTile);
            }
        }


        Track.DynamicObjectFilesNumber = io.ReadUShort();
        Track.DynamicObjectFiles = new List<string>(Track.DynamicObjectFilesNumber);
        for (int i = 0; i < Track.DynamicObjectFilesNumber; i++)
        {
            Track.DynamicObjectFiles.Add(io.ReadString());
        }

        Track.DynamicObjectsNumber = io.ReadUShort();
        Track.DynamicObjects = new List<DynamicObjectSavable>(Track.DynamicObjectsNumber);
        for (int i = 0; i < Track.DynamicObjectsNumber; i++)
        {
            DynamicObjectSavable newDynamicObject = new DynamicObjectSavable();
            newDynamicObject.ObjectId = io.ReadUShort();
            newDynamicObject.Position = io.ReadVector3();
            newDynamicObject.Rotation = io.ReadFloat();
            Track.DynamicObjects.Add(newDynamicObject);
        }


        Track.CheckpointsNumber = io.ReadUShort();
        Track.Checkpoints = new List<ushort>(Track.CheckpointsNumber);
        for (int i = 0; i < Track.CheckpointsNumber; i++)
        {
            Track.Checkpoints.Add(io.ReadUShort());
        }

        Track.Permission = io.ReadByte();

        Track.GroundBumpyness = io.ReadFloat();

        Track.Scenery = io.ReadByte();

        Track.Heightmap = new List<List<float>>();
        
        for (int y = 0; y < Track.Height * 4 + 1; y++)
        {
	        Track.Heightmap.Add(new List<float>());
            for (int x = 0; x < Track.Width * 4 + 1; x++)
            {
                Track.Heightmap[y].Add(io.ReadFloat());
            }
        }

        return Track;
    }

	public static void SaveMap(TrackSavable track, string path)
	{
		List<byte> bytes = new List<byte>();
		ByteFileParser io = new ByteFileParser(bytes);

		 //ignore "CDTRK" string in the start of the file
        io.WriteFlag("CDTRK");

        //unused current time thing
		io.WriteInt(track.CurrentTime);

        //author
        io.WriteString(track.Author);

        //comment
        io.WriteString(track.Comment);

        //skip unneeded block
        for (int n = 0; n < 20; n++)
        {
	        io.WriteInt(1);
	        io.WriteString("");
        }

        //track style (race, derby, htf)
	    io.WriteByte(track.Style);

        //ambience
        io.WriteString(track.Ambience);

        //amount of fileds used on map
       io.WriteUShort(track.FieldFilesNumber);

        //name of fields
        for (int n = 0; n < track.FieldFilesNumber; n++)
        {
            io.WriteString(track.FieldFiles[n]);
        }

        //width and height in tiles
		io.WriteUShort(track.Width);
		io.WriteUShort(track.Height);

        for (int y = 0; y < track.Height; y++)
        {
            for (int x = 0; x < track.Width; x++)
            {
	            io.WriteUShort(track.TrackTiles[y][x].FieldId);
	            io.WriteByte(track.TrackTiles[y][x].Rotation);
	            io.WriteByte(track.TrackTiles[y][x].IsMirrored);
	            io.WriteByte(track.TrackTiles[y][x].Height);
            }
        }

		io.WriteUShort(track.DynamicObjectFilesNumber);

        for (int i = 0; i < track.DynamicObjectFilesNumber; i++)
        {
	        io.WriteString(track.DynamicObjectFiles[i]);
        }

		io.WriteUShort(track.DynamicObjectsNumber);

        for (int i = 0; i < track.DynamicObjectsNumber; i++)
        {
	        io.WriteUShort(track.DynamicObjects[i].ObjectId);
	        io.WriteVector3(track.DynamicObjects[i].Position);
	        io.WriteFloat(track.DynamicObjects[i].Rotation);
        }


        io.WriteUShort(track.CheckpointsNumber);
        for (int i = 0; i < track.CheckpointsNumber; i++)
        {
	        io.WriteUShort(track.Checkpoints[i]);
        }

		io.WriteByte(track.Permission);

		io.WriteFloat(track.GroundBumpyness);

		io.WriteByte(track.Scenery);

        for (int y = 0; y < track.Height * 4 + 1; y++)
        {
            for (int x = 0; x < track.Width * 4 + 1; x++)
            {
                io.WriteFloat(track.Heightmap[y][x]);
            }
        }

		File.WriteAllBytes(path, io.Data.ToArray());
	}
}
