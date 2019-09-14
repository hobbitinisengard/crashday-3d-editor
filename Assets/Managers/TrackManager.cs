using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

public class TrackManager : MonoBehaviour
{
    public GameObject TilePrefab;
    public Transform MapParentTransform;
    public List<List<Transform>> Tiles;
	public TrackSavable CurrentTrack;

	public enum TrackState
	{
		TrackEmpty,
		TrackStart,
		TrackLoaded
	};

	public TrackState CurrentTrackState;

    public static int TileSize = 20;

	public static int MaxMapSizeLimit = 8000;

	private TileManager _tm;
	private TerrainManager _terrainManager;

	void Awake()
	{
		_tm = GetComponent<TileManager>();
		_terrainManager = GetComponent<TerrainManager>();
	}

	/// <summary>
	/// Retruns the position of the base tile at the given point
	/// If a tile takes more than one block, on part of it will not be an actualy tile object.
	/// Use this to get the position of the main one
	/// </summary>
	/// <param name="pos">Get actual base position</param>
	/// <returns>Base position or the same pos, if tile is 1x1</returns>
	public IntVector2 GetBaseTilePosition(IntVector2 pos)
	{
		if (CurrentTrack.TrackTiles[pos.y][pos.x].FieldId == 65470)
		{
			return new IntVector2(pos.x-1, pos.y-1);
		}

		if (CurrentTrack.TrackTiles[pos.y][pos.x].FieldId == 65472)
		{
			return new IntVector2(pos.x-1, pos.y);
		}

		if (CurrentTrack.TrackTiles[pos.y][pos.x].FieldId == 65471)
		{
			return new IntVector2(pos.x, pos.y-1);
		}

		return pos;
	}

	/// <summary>
	/// Remove (set to atlas id 0) tile at this position and remove null tiles if our tile 
	/// has size bigger then 1x1
	/// </summary>
	/// <param name="pos">position of the tile to be removed</param>
	public void RemoveTileAt(IntVector2 pos)
	{
		if (CurrentTrackState == TrackState.TrackEmpty) return;

		//if we are trying to delete the extra part of a multi-tiled tile, delete the main part
		if (CurrentTrack.TrackTiles[pos.y][pos.x].FieldId == 65470)
		{
			RemoveTileAt(new IntVector2(pos.x-1, pos.y-1));
			return;
		}

		if (CurrentTrack.TrackTiles[pos.y][pos.x].FieldId == 65472)
		{
			RemoveTileAt(new IntVector2(pos.x-1, pos.y));
			return;
		}

		if (CurrentTrack.TrackTiles[pos.y][pos.x].FieldId == 65471)
		{
			RemoveTileAt(new IntVector2(pos.x, pos.y-1));
			return;
		}

		//if additional parts exists and are on the map, delete them
		if(pos.x + 1 < CurrentTrack.Width && pos.y + 1 < CurrentTrack.Height)
			if(CurrentTrack.TrackTiles[pos.y + 1][pos.x + 1].FieldId == 65470)
				SetTileByAtlasId(0, new IntVector2(pos.x+1, pos.y+1));

		if(pos.x + 1 < CurrentTrack.Width)
			if(CurrentTrack.TrackTiles[pos.y][pos.x + 1].FieldId == 65472)
				SetTileByAtlasId(0, new IntVector2(pos.x+1, pos.y));

		if(pos.y + 1 < CurrentTrack.Height)
			if(CurrentTrack.TrackTiles[pos.y + 1][pos.x].FieldId == 65471)
				SetTileByAtlasId(0, new IntVector2(pos.x, pos.y+1));

		//now reset the main part of the tile
		SetTileByAtlasId(0, pos);
	}

	public void UpdateTrackSize(int addLeft, int addRight, int addUp, int addDown)
	{
		//TODO:
		//Move DynamicObjects
		TrackSavable newTrack = new TrackSavable(CurrentTrack);

		newTrack.Width += (ushort) (addLeft + addRight);
		newTrack.Height += (ushort) (addUp + addDown);

		List<ushort> newCPs = new List<ushort>();

		for (int i = 0; i < CurrentTrack.CheckpointsNumber; i++)
		{
			int newPosX = CurrentTrack.Checkpoints[i] % CurrentTrack.Width;
			int newPosY = CurrentTrack.Checkpoints[i] / CurrentTrack.Height;

			newPosX += addLeft;
			newPosY += addUp;
			if(newPosX >= 0 && newPosX < newTrack.Width && newPosY >= 0 && newPosY < newTrack.Height)
				newCPs.Add((ushort)(newPosY * newTrack.Width + newPosX));
		}

		newTrack.CheckpointsNumber = (ushort)newCPs.Count;
		newTrack.Checkpoints = newCPs;


		newTrack.TrackTiles = new List<List<TrackTileSavable>>(newTrack.Height);

		for (int y = 0; y < newTrack.Height; y++)
		{
			newTrack.TrackTiles.Add(new List<TrackTileSavable>(newTrack.Width));
			for (int x = 0; x < newTrack.Width; x++)
			{
				TrackTileSavable tile;
				if (x - addLeft >= 0 && y - addUp >= 0 && x - addLeft < CurrentTrack.Width && y - addUp < CurrentTrack.Height)
				{
					tile = new TrackTileSavable(CurrentTrack.TrackTiles[y-addUp][x-addLeft]);
				}
				else
				{
					tile = new TrackTileSavable(0, 0, 0, 0);
				}

				newTrack.TrackTiles[y].Add(tile);
			}
		}

		newTrack.Heightmap = new List<List<float>>(newTrack.Height*4+1);

		for (int y = 0; y < newTrack.Height*4+1; y++)
		{
			newTrack.Heightmap.Add(new List<float>(newTrack.Width*4+1));
			for (int x = 0; x < newTrack.Width*4+1; x++)
			{
				if (x - addLeft*4 >= 0 && y - addUp*4 >= 0 && x - addLeft*4 < CurrentTrack.Width*4+1 && y - addUp*4 < CurrentTrack.Height*4+1)
				{
					newTrack.Heightmap[y].Add(CurrentTrack.Heightmap[y - addUp*4][x - addLeft*4]);
				}
				else
				{
					newTrack.Heightmap[y].Add(0.0f);
				}
			}
		}

		LoadTrack(newTrack);

		GetComponent<ToolManager>().OnMapSizeChange();
	}

	/// <summary>
	/// Sets the tile on the given position to the tile with given atlasId and reloads it world model
	/// ! This will not reset additional parts for multi-tiled tiles!
	/// </summary>
	/// <param name="atlasId">Id in the atlas of the tile you want. Usually 0 - field</param>
	/// <param name="position">Position of the tile to be reset</param>
	public void SetTileByAtlasId(ushort atlasId, IntVector2 position)
	{
		if (CurrentTrackState == TrackState.TrackEmpty) return;

		CurrentTrack.TrackTiles[position.y][position.x] = new TrackTileSavable(atlasId,0,0,0);

		UpdateTileAt(position.x, position.y);
	}

	/// <summary>
	/// Apply a tile object on the map
	/// </summary>
	/// <param name="tile">The tile object which will be applied</param>
	public void SetTile(Tile tile)
	{
		if (CurrentTrackState == TrackState.TrackEmpty) return;

		//find the index of the tile in our atlas. If the tile is new and not present in the atlas, add it to the atlas
		int index = CurrentTrack.FieldFiles.FindIndex(entry=>entry == tile.FieldName);
		if(index == -1)
		{
			CurrentTrack.FieldFiles.Add(tile.FieldName);
			index = CurrentTrack.FieldFilesNumber;
			CurrentTrack.FieldFilesNumber += 1;
		}

		//get the correct tile id and set it to the new tile object
		tile._trackTileSavable.FieldId = Convert.ToUInt16(index);
		CurrentTrack.TrackTiles[tile.GridPosition.y][tile.GridPosition.x] = new TrackTileSavable(tile._trackTileSavable);

		//multi-tiled tiles need right slave-tile id's so CD will not crash
		//this is probably done for easier working when we are trying to do something with slave-tiles
		if (tile.Size.y == 2 && tile.Size.x == 2)
		{
			if (tile.GridPosition.x + 1 < CurrentTrack.Width && tile.GridPosition.y + 1 < CurrentTrack.Height)
			{
				SetTileByAtlasId(65470, new IntVector2(tile.GridPosition.x + 1, tile.GridPosition.y + 1));
				UpdateTileAt(tile.GridPosition.x + 1, tile.GridPosition.y + 1);
			}

			if (tile.GridPosition.x + 1 < CurrentTrack.Width)
			{
				SetTileByAtlasId(65472, new IntVector2(tile.GridPosition.x + 1, tile.GridPosition.y));
				UpdateTileAt(tile.GridPosition.x + 1, tile.GridPosition.y);
			}

			if (tile.GridPosition.y + 1 < CurrentTrack.Height)
			{
				SetTileByAtlasId(65471, new IntVector2(tile.GridPosition.x, tile.GridPosition.y + 1));
				UpdateTileAt(tile.GridPosition.x, tile.GridPosition.y + 1);
			}
		}
		else if ((tile.Size.y == 2 && tile._trackTileSavable.Rotation%2 == 1) || 
		         tile.Size.x == 2 && tile._trackTileSavable.Rotation%2 == 0)
		{
			if (tile.GridPosition.x + 1 < CurrentTrack.Width)
			{
				SetTileByAtlasId(65472, new IntVector2(tile.GridPosition.x + 1, tile.GridPosition.y));
				UpdateTileAt(tile.GridPosition.x + 1, tile.GridPosition.y);
			}
		}
		else if(tile.Size.x + tile.Size.y == 3)
		{
			if (tile.GridPosition.y + 1 < CurrentTrack.Height)
			{
				SetTileByAtlasId(65471, new IntVector2(tile.GridPosition.x, tile.GridPosition.y + 1));
				UpdateTileAt(tile.GridPosition.x, tile.GridPosition.y + 1);
			}
		}
		

		UpdateTileAt(tile.GridPosition.x, tile.GridPosition.y);
	}

	/// <summary>
	/// Update world model of the given tile in world space
	/// </summary>
	/// <param name="pos">Position of the tile to be updated</param>
	public void UpdateTileAt(IntVector2 pos)
	{
		UpdateTileAt(pos.x, pos.y);
	}

	/// <summary>
	/// Update world model of the given tile in world space
	/// </summary>
	/// <param name="x">x pos of the tile</param>
	/// <param name="y">y pos of the tile</param>
	public void UpdateTileAt(int x, int y)
	{
		IntVector2 p = GetBaseTilePosition(new IntVector2(x, y));

		x = p.x;
		y = p.y;

		if (CurrentTrack.TrackTiles[y][x].FieldId < CurrentTrack.FieldFilesNumber)
		{
			int index = _tm.TileListInfo.FindIndex(entry=>entry.Name == CurrentTrack.FieldFiles[CurrentTrack.TrackTiles [y][x].FieldId]);

			if (index == -1)
			{
				Debug.LogError("Given tile id not found in the tile atlas!");

				Tiles[y][x].name = x + ":" + y + " ";
				Tiles[y][x].GetComponent<MeshFilter>().mesh.Clear();
				Tiles[y][x].GetComponent<Tile>().SetupTile(CurrentTrack.TrackTiles [y][x], new IntVector2(1,1), new IntVector2(x, y), _terrainManager, "");

				return;
			}

			//load our model in to the memory
			_tm.LoadModelForTileId(index);

			//The tile will be moved by the SetTile function later. The best moment to calcualte height is now.
			Tiles[y][x].position = new Vector3(0, _tm.TileListInfo[index].Model.P3DMeshes[0].Height / 2, 0);

			Tiles[y][x].name = x + ":" + y + " " + _tm.TileListInfo[index].Name;

			//set the model and textures for the tile
			Mesh m = _tm.TileListInfo[index].Model.CreateMesh();
			Tiles[y][x].GetComponent<MeshFilter>().mesh = m;
			Tiles[y][x].GetComponent<Renderer>().materials = _tm.TileListInfo[index].Materials.ToArray();

			Tiles[y][x].GetComponent<Tile>().SetupTile(CurrentTrack.TrackTiles [y][x], _tm.TileListInfo[index].Size, new IntVector2(x, y), _terrainManager, _tm.TileListInfo[index].Name);
			Tiles[y][x].GetComponent<Tile>().SetOriginalVertices(m.vertices);
			Tiles[y][x].GetComponent<Tile>().ApplyTerrain();
		}
		else
		{
			Tiles[y][x].name = x + ":" + y + " ";
			Tiles[y][x].GetComponent<MeshFilter>().mesh.Clear();
			Tiles[y][x].GetComponent<Tile>().SetupTile(CurrentTrack.TrackTiles [y][x], new IntVector2(1,1), new IntVector2(x, y), _terrainManager, "");
		}
	}

	/// <summary>
	/// Updates the terrain mesh corresponding to currentTrack
	/// </summary>
	public void UpdateTerrain()
	{
		for(int y = 0; y < CurrentTrack.Height; y++)
			for(int x = 0; x < CurrentTrack.Height; x++)
				UpdateTerrainAt(x, y);
	}

	/// <summary>
	/// Update terrain for the given tile
	/// </summary>
	/// <param name="pos">Position of the tile</param>
	public void UpdateTerrainAt(IntVector2 pos)
	{
		UpdateTerrainAt(pos.x, pos.y);
	}

	/// <summary>
	/// Update terrain for the given tile
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void UpdateTerrainAt(int x, int y)
	{
		IntVector2 p = GetBaseTilePosition(new IntVector2(x,y));
		Tiles[p.y][p.x].GetComponent<Tile>().ApplyTerrain();
	}

	public void LoadTrack()
	{
		LoadTrack(new TrackSavable());
		CurrentTrackState = TrackState.TrackStart;
	}

    public void LoadTrack(TrackSavable track)
    {
	    CurrentTrackState = TrackState.TrackEmpty;
	    CurrentTrack = track;

		//clear tiles left from the old loaded track
        for (int i = 0; i < MapParentTransform.childCount; i++)
        {
            Destroy(MapParentTransform.GetChild(i).gameObject);
        }

		GetComponent<TerrainManager>().GenerateTerrain();

		Tiles = new List<List<Transform>>(track.Height);
			
        for (int y = 0; y < track.Height; y++)
        {
			Tiles.Add(new List<Transform>(track.Width));
            for (int x = 0; x < track.Width; x++)
            {
	            GameObject newTile = (GameObject) Instantiate(TilePrefab, Vector3.zero, Quaternion.identity);

	            newTile.name = x + ":" + y + " ";
	            newTile.transform.SetParent(MapParentTransform);
	            newTile.AddComponent<Tile>();

	            Tiles[y].Add(newTile.transform);

				UpdateTileAt(x, y);
            }
        }

	    GetComponent<ToolManager>().OnMapSizeChange();

		Camera.main.transform.position = new Vector3(CurrentTrack.Width*10, 80, CurrentTrack.Height*-10);

	    CurrentTrackState = TrackState.TrackLoaded;
    }
}
