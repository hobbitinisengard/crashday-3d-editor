using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public enum BorderType { Vertical, Horizontal };

public class Border
{
	public BorderType border_type;

	private byte _tiles_occupying = 0;

	public byte tiles_constraining
	{
		get { return _tiles_occupying; }
		set { _tiles_occupying = value; }
	}
	public Border(BorderType bt)
	{
		border_type = bt;
	}
}


//Handles BUILD mode.
public class Build : MonoBehaviour
{
	// "real mesh collider" - RMC - plane with vertices set in positions where tile can have its terrain vertices changed
	// "znacznik" - tag - white small box spawned only in FORM mode. Form mode uses some static functions from here.
	public GameObject editorPanel; //-> slidercase.cs
	public GameObject PrefabPoolObject;
	public Text CURRENTELEMENT; //name of currently selected element on top of the building menu
	public Text CURRENTROTATION;
	public Text CURRENTMIRROR;
	public Text BuildButtonText; // text 'build' in build button
	public Text MixingInfoText; // text for displaying H = mixingHeight and keypad enter
	public GameObject savePanel; // "save track scheme" menu
	public Material partiallytransparent;
	public Material transparent;
	public Material reddish;
	public static Border_vault Border_Vault = new Border_vault();
	bool LMBclicked = false;
	bool AllowLMB = false;
	/// <summary>obj_rmc = current RMC</summary>
	public static GameObject current_rmc;
	/// <summary>current tile or null</summary>
	public static string tile_name = "NULL";
	/// <summary>the last selected tile before it was reset to null</summary>
	public static string previous_tile_name = "NULL";
	/// <summary>current rotation of currently showed element</summary>
	public static int cum_rotation = 0;
	///<summary> current inversion of an element that is currently being shown</summary>
	public static bool inversion = false;
	public static bool enableMixing = false;
	/// <summary>Tile's own height in mixing mode</summary>
	public static byte MixingHeight = 0;
	/// <summary>former position of temporary placement of tile in build mode</summary>
	Vector3 last_trawa;
	public static bool over_b4 = Highlight.over;
	private bool IsEnteringKeypadValue;
	private static GameObject outlined_element;


	private void OnDisable()
	{
		ExitTileSelection();
		if (current_rmc != null)
		{
			if (!LMBclicked || (LMBclicked && !AllowLMB))
				DelLastPrefab();
			over_b4 = false;
		}
	}
	public static void Reset()
	{
		enableMixing = false;
		MixingHeight = 0;
		cum_rotation = 0;
		inversion = false;
		tile_name = "NULL";
		previous_tile_name = "NULL";
	}
	public static void ChangeCurrentTile(string name, bool visibility_enabled)
	{
		if (visibility_enabled)
			tile_name = name;
		else
			previous_tile_name = name;
	}
	void ExitTileSelection()
	{
		if (outlined_element)
		{
			outlined_element.GetComponent<MeshRenderer>().material = transparent;
			outlined_element = null;
		}
	}
	void XTileSelection()
	{
		Outline_underlying();
		DelLastPrefab();
		if (Input.GetMouseButtonDown(0))
			Del_selected_element();
	}
	void CTileSelection()
	{
		Outline_underlying();
		DelLastPrefab();
		if (Input.GetMouseButtonDown(0))
			PickUpTileUnderCursor();
	}
	void Outline_underlying()
	{
		RaycastHit[] hits = Physics.RaycastAll(new Vector3(Highlight.pos_float.x, Consts.MAX_H, Highlight.pos_float.z), Vector3.down, Consts.RAY_H, 1 << 9);
		if (hits.Length == 0)
			return;
		if (outlined_element)
		{
			if (Input.GetAxis("Mouse ScrollWheel") != 0)
			{
				for (int i = 0; i < hits.Length; i++)
				{
					if (hits[i].transform.gameObject.GetComponent<MeshRenderer>().material.shader.name == reddish.shader.name)
					{
						hits[i].transform.gameObject.GetComponent<MeshRenderer>().material = transparent;
						outlined_element = hits[(i + 1) % hits.Length].transform.gameObject;
						outlined_element.GetComponent<MeshRenderer>().material = reddish;
						return;
					}
				}
			}
		}
		else
		{
			hits[0].transform.gameObject.GetComponent<MeshRenderer>().material = reddish;
			outlined_element = hits[0].transform.gameObject;
		}
	}
	public static void Del_selected_element()
	{
		if (!outlined_element)
			return;

		Vector3Int pos = Vpos2tpos(outlined_element);
		if (TileManager.TileListInfo[outlined_element.name].IsCheckpoint)
		{
			Consts.TRACK.Checkpoints.Remove((ushort)(pos.x + (Consts.TRACK.Height - 1 - pos.z) * Consts.TRACK.Width));
			Consts.TRACK.CheckpointsNumber--;
		}

		Show_underlying_grass_tiles(outlined_element);
		List<GameObject> to_restore = Get_surrounding_tiles(outlined_element);
		Border_Vault.Remove_borders_of(outlined_element);
		DestroyImmediate(outlined_element);
		outlined_element = null;
		Consts.TilePlacementArray[pos.z, pos.x].Name = null;
		RecoverTerrain(Consts.TilePlacementArray[pos.z, pos.x].t_verts);
		UpdateTiles(to_restore);
	}
	void Update()
	{
		CURRENTELEMENT.text = ".cfl: " + tile_name;
		CURRENTROTATION.text = "Rot: " + cum_rotation.ToString();
		CURRENTMIRROR.text = "Inv: " + inversion.ToString();

		if (Input.GetKeyUp(KeyCode.M))
			SwitchMixingMode();
		if (Input.GetKeyUp(KeyCode.LeftAlt))
			ToggleVisibility();
		if (!MouseInputUIBlocker.BlockedByUI)
		{
			// Ctrl + RMB - picks up mixing height in mixing mode so rotation with ctrl is forbidden
			if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftControl))
				cum_rotation = (cum_rotation == 270) ? 0 : cum_rotation + 90;

			if (enableMixing)
				CtrlWithMousewheelWorks();
			if (Input.GetKeyDown(KeyCode.Q))
				InverseState(); // Q enabled inversion
			if (Input.GetKey(KeyCode.X))
				XTileSelection(); // X won't let PlacePrefab work
			if (Input.GetKey(KeyCode.C))
				CTileSelection(); // Choose the tile to be picked up with C
			if (Input.GetKeyUp(KeyCode.X) || Input.GetKeyUp(KeyCode.C))
				ExitTileSelection();

			if (enableMixing && !IsEnteringKeypadValue && Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(1))
			{// Pick up mixing height with RMB + ctrl
				PickUpTileHeightUnderCursor();
				StartCoroutine(DisplayMessageFor(MixingHeight.ToString(), 2));
				return;
			}
			else if (enableMixing)
			{ // check for numeric enter in mixing mode
				Numericenter();
			}

			if (!Input.GetKey(KeyCode.Space))
			{
				if (!Highlight.over)
				{
					if (over_b4)
					{
						//Debug.Log("Było przejście na nulla z trawki");
						if (!LMBclicked && AllowLMB)
							DelLastPrefab();
						else
							LMBclicked = false;
					}
					over_b4 = false;
				}
				else
				{ //If cursor points on map
					if (!over_b4)
					{
						//Debug.Log("cursor: void -> terrain");
						PlaceTile(Highlight.TL, tile_name, cum_rotation, inversion);
						last_trawa = Highlight.TL;
						over_b4 = true;
					}
					else if (last_trawa.x != Highlight.TL.x || last_trawa.z != Highlight.TL.z)
					{
						//Debug.Log("cursor: terrain chunk -> terrain chunk");
						if (!LMBclicked)//If element hasn't been placed
							DelLastPrefab();
						ExitTileSelection();
						PlaceTile(Highlight.TL, tile_name, cum_rotation, inversion);
						last_trawa = Highlight.TL;
						LMBclicked = false;
					}
					else if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.X) && !Input.GetKey(KeyCode.C) && AllowLMB)
					{//Place currently showed tile on terrain
						LMBclicked = true;
						Save_tile_properties(current_rmc, tile_name, inversion, cum_rotation,
								new Vector3Int(Highlight.TL.x / 4, 0, Highlight.TL.z / 4 - 1), enableMixing ? MixingHeight : (byte)0);
					}
					if (Input.GetMouseButtonDown(1) && Highlight.over && !LMBclicked)
					{//Rotation with RMB
						DelLastPrefab();
						PlaceTile(Highlight.TL, tile_name, cum_rotation, inversion);
						over_b4 = true;
					}
				}
			}
			else if (Input.GetKey(KeyCode.Space) && !LMBclicked)
			{//Delete currently showed element when moving camera with spacebar
				DelLastPrefab();
				over_b4 = false;
			}
		}
		else if (over_b4)
		{//Delete currently showed element when moving cursor over menu
			if (!LMBclicked)
				DelLastPrefab();
			else
				LMBclicked = false;
			over_b4 = false;
		}
	}

	/// <summary>
	/// Handles setting sliderheight with keypad
	/// </summary>
	private void Numericenter()
	{
		if (Input.GetKeyDown(KeyCode.KeypadMultiply))
		{
			try
			{
				MixingHeight = byte.Parse(MixingInfoText.text, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch
			{
				MixingHeight = 0;
			}
			IsEnteringKeypadValue = false;
			MixingInfoText.text = "";
			MixingInfoText.gameObject.SetActive(false);

			if (!Input.GetKey(KeyCode.Space) && Highlight.over)
			{
				if (tile_name == "NULL")
				{
					MixingHeightPreview();
				}
				else
				{
					DelLastPrefab();
					PlaceTile(Highlight.TL, tile_name, cum_rotation, inversion);
				}
			}
			StartCoroutine(DisplayMessageFor(MixingHeight.ToString(), 2));
		}
		KeyCode[] keyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
			KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9 };
		for (int i = 0; i < keyCodes.Length; i++)
		{
			if (Input.GetKeyDown(keyCodes[i]))
			{
				IsEnteringKeypadValue = true;
				MixingInfoText.gameObject.SetActive(true);
				MixingInfoText.text += i.ToString();
				return;
			}
		}
	}
	IEnumerator DisplayMessageFor(string message, float delay)
	{
		MixingInfoText.text = message;
		MixingInfoText.gameObject.SetActive(true);
		yield return new WaitForSeconds(delay);
		MixingInfoText.text = "";
		MixingInfoText.gameObject.SetActive(false);
	}
	void CtrlWithMousewheelWorks()
	{
		if (Input.GetAxis("Mouse ScrollWheel") != 0 && Input.GetKey(KeyCode.LeftControl))
		{
			if (Input.GetAxis("Mouse ScrollWheel") > 0)
			{
				if (MixingHeight < 255)
					MixingHeight += 1;
				else
					return;
			}
			else
			{
				if (MixingHeight > 0)
					MixingHeight -= 1;
				else
					return;
			}
			if (!Input.GetKey(KeyCode.Space) && Highlight.over)
			{
				if (tile_name == "NULL")
				{
					MixingHeightPreview();
				}
				else
				{
					DelLastPrefab();
					PlaceTile(Highlight.TL, tile_name, cum_rotation, inversion);
				}
			}
			StartCoroutine(DisplayMessageFor(MixingHeight.ToString(), 2));
		}
	}
	/// <summary>Displays transparent cuboid for 2 secs.</summary>
	public void MixingHeightPreview()
	{
		GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Destroy(preview.GetComponent<BoxCollider>());
		preview.GetComponent<MeshRenderer>().material = partiallytransparent;
		preview.transform.localScale = new Vector3(3f, 0.05f, 3);
		preview.transform.position = new Vector3(2 + Highlight.TL.x, MixingHeight / 5f, Highlight.TL.z - 2);
		Destroy(preview, 2);
	}
	/// <summary>
	/// Toggles tile preview 
	/// </summary>
	void ToggleVisibility()
	{
		if (!MouseInputUIBlocker.BlockedByUI)
		{
			if (!LMBclicked)
			{
				if (tile_name == "NULL")
				{
					if (!Input.GetKey(KeyCode.Space) && Highlight.over)
					{
						PlaceTile(Highlight.TL, previous_tile_name, cum_rotation, inversion);
					}
				}
				else
				{
					DelLastPrefab();
					over_b4 = false;
				}
			}
			else
				LMBclicked = false;
		}

		if (tile_name == "NULL")
		{
			tile_name = previous_tile_name;
		}
		else
		{
			previous_tile_name = tile_name;
			tile_name = "NULL";
		}
	}

	void InverseState()
	{
		inversion = !inversion;
		DelLastPrefab();
		over_b4 = false;
	}

	void SwitchMixingMode()
	{
		enableMixing = !enableMixing;
		if (enableMixing)
		{
			BuildButtonText.color = new Color32(39, 255, 0, 255);
		}
		else
		{
			BuildButtonText.color = new Color32(255, 161, 54, 255);
		}
		if (!MouseInputUIBlocker.BlockedByUI && !Input.GetKey(KeyCode.Space) && Highlight.over)
		{
			DelLastPrefab();
			PlaceTile(Highlight.TL, tile_name, cum_rotation, inversion);
		}
	}
	public static void DelLastPrefab()
	{
		if (current_rmc != null)
		{
			Vector3Int pos = Vpos2tpos(current_rmc);
			if (Consts.TilePlacementArray[pos.z, pos.x].Name != null)
				return;
			Show_underlying_grass_tiles(current_rmc);
			List<GameObject> surroundings = Get_surrounding_tiles(current_rmc);
			Border_Vault.Remove_borders_of(current_rmc);
			DestroyImmediate(current_rmc);
			var t_verts = Consts.TilePlacementArray[pos.z, pos.x].t_verts;
			if (t_verts != null)
				RecoverTerrain(t_verts);
			UpdateTiles(surroundings);
		}
	}
	void PickUpTileHeightUnderCursor()
	{
		MixingHeight = Consts.TilePlacementArray[Highlight.TL.z / 4 - 1, Highlight.TL.x / 4].Height;
	}
	void PickUpTileUnderCursor()
	{
		if (outlined_element != null)
		{
			if (tile_name == "NULL")
				previous_tile_name = outlined_element.name;
			else
				tile_name = outlined_element.name;
			var pos = Vpos2tpos(outlined_element);
			inversion = Consts.TilePlacementArray[pos.z, pos.x].Inversion;
			cum_rotation = Consts.TilePlacementArray[pos.z, pos.x].Rotation;
			if (tile_name == "NULL")
				editorPanel.GetComponent<SliderCase>().SwitchToTileset(TileManager.TileListInfo[previous_tile_name].TilesetName);
			else
				editorPanel.GetComponent<SliderCase>().SwitchToTileset(TileManager.TileListInfo[tile_name].TilesetName);
		}
	}
	public static bool IsCheckpoint(string nazwa_tilesa)
	{
		return TileManager.TileListInfo[nazwa_tilesa].IsCheckpoint;
	}
	// Shows only trawkas that are not occupied by tiles (which could be mixed)
	static void Show_underlying_grass_tiles(GameObject rmc)
	{
		rmc.layer = 10;
		Vector3 pos = rmc.transform.position;
		pos.y = Consts.MAX_H;
		RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Consts.RAY_H, 1 << 8);
		foreach (RaycastHit hit in hits)
		{
			pos = hit.transform.position;
			pos.x += 2;
			pos.z += 2;
			pos.y = Consts.MAX_H;
			if (!Physics.Raycast(pos, Vector3.down, Consts.RAY_H, 1 << 9))
				hit.transform.gameObject.GetComponent<MeshRenderer>().enabled = true;
		}
		rmc.layer = 9;
	}
	static void Hide_underlying_grass(Vector3 pos)
	{
		pos.y = Consts.MAX_H;
		RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Consts.RAY_H, 1 << 8);
		foreach (RaycastHit hit in hits)
			hit.transform.gameObject.GetComponent<MeshRenderer>().enabled = false;
	}
	/// <summary>
	/// Converts vertex pos to tile pos
	/// </summary>
	/// <param name="rmc"></param>
	/// <returns></returns>
	public static Vector3Int Vpos2tpos(GameObject rmc)
	{
		Vector3Int to_return = new Vector3Int();
		Vector3Int dim = GetRealTileDims(rmc);
		to_return.x = ((int)rmc.transform.position.x - 2 - 2 * (dim.x - 1)) / 4;
		to_return.z = ((int)rmc.transform.position.z - 2 + 4 * (dim.z - 1)) / 4;
		return to_return;
	}
	/// <summary>
	/// Returns real dimensions of objects (e.g. 2x1) taking rotation into consideration
	/// </summary>
	public static Vector3Int GetRealTileDims(GameObject rmc_o)
	{
		Vector3Int extents = Vector3Int.RoundToInt(rmc_o.GetComponent<MeshFilter>().mesh.bounds.extents / 2f);
		if (Mathf.Round(rmc_o.transform.rotation.eulerAngles.y) == 90
			|| Mathf.Round(rmc_o.transform.rotation.eulerAngles.y) == 270)
			return new Vector3Int(extents.z, 0, extents.x);
		else
			return extents;
	}
	static void Save_tile_properties(GameObject rmc, string name, bool mirror, int rotation, Vector3Int arraypos, byte Height = 0)
	{
		Consts.TilePlacementArray[arraypos.z, arraypos.x].Set(name, rotation, mirror, Height);

		if (TileManager.TileListInfo[name].IsCheckpoint
			&& !Consts.TRACK.Checkpoints.Contains((ushort)(arraypos.x + (Consts.TRACK.Height - 1 - arraypos.z) * Consts.TRACK.Width)))
		{
			Consts.TRACK.Checkpoints.Add((ushort)(arraypos.x + (Consts.TRACK.Height - 1 - arraypos.z) * Consts.TRACK.Width));
			Consts.TRACK.CheckpointsNumber++;
		}


	}
	public static HashSet<int> GetRmcIndices(GameObject rmc)
	{
		HashSet<int> to_return = new HashSet<int>();
		Vector3Int TL = GetTLPos(rmc);
		Vector3Int tileDims = GetRealTileDims(rmc);
		for (int z = 0; z <= 4 * tileDims.z; z++)
		{
			for (int x = 0; x <= 4 * tileDims.x; x++)
			{
				to_return.Add(Consts.PosToIndex(TL.x + x, TL.z - z));
			}
		}
		return to_return;
	}
	public static List<Vector3> Get_grass_vertices(GameObject grass)
	{
		List<Vector3> to_return = new List<Vector3>();
		Vector3[] verts = grass.GetComponent<MeshFilter>().mesh.vertices;
		foreach (var pos in verts)
		{
			to_return.Add(grass.transform.TransformPoint(pos));
		}
		return to_return;
	}
	public static List<GameObject> Get_surrounding_tiles(HashSet<int> indexes)
	{
		List<GameObject> to_return = new List<GameObject>();
		foreach (int index in indexes)
		{
			Vector3 pos = Consts.IndexToPos(index);
			pos.y = Consts.MAX_H;
			RaycastHit[] hits = Physics.SphereCastAll(pos, 0.1f, Vector3.down, Consts.RAY_H, 1 << 9);
			foreach (RaycastHit hit in hits)
				if (!to_return.Contains(hit.transform.gameObject))
					to_return.Add(hit.transform.gameObject);
		}
		return to_return;
	}

	/// <summary>
	/// Returns array of tiles being placed on terrain occupied by "znaczniki"
	/// </summary>
	/// <param name="markings"></param>
	/// <returns></returns>
	public static List<GameObject> Get_surrounding_tiles(List<GameObject> markings, bool AllMarkingsAreValid = false)
	{
		List<GameObject> to_return = new List<GameObject>();
		foreach (GameObject znacznik in markings)
		{
			if (!AllMarkingsAreValid)
			{
				if (znacznik.name != "on")
					continue;
			}

			Vector3 pos = znacznik.transform.position;
			pos.y = Consts.MAX_H;
			RaycastHit[] hits = Physics.SphereCastAll(pos, .1f, Vector3.down, Consts.RAY_H, 1 << 9);
			foreach (RaycastHit hit in hits)
				if (!to_return.Contains(hit.transform.gameObject))
					to_return.Add(hit.transform.gameObject);
		}
		return to_return;
	}
	/// <summary>
	/// Returns array of tiles being around object rmc_o.
	/// </summary>
	public static List<GameObject> Get_surrounding_tiles(GameObject rmc_o)
	{
		int orig_layer = rmc_o.layer;
		rmc_o.layer = 10;
		List<GameObject> to_return = new List<GameObject>();
		Vector3Int tile_dims = GetRealTileDims(rmc_o);
		tile_dims.y = 1; // bounding box for boxcast must have non-zero height
		Vector3 v = rmc_o.transform.position;
		v.y = Consts.MAX_H;
		RaycastHit[] hits = Physics.BoxCastAll(v, tile_dims * 2, Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9);
		foreach (RaycastHit hit in hits)
			to_return.Add(hit.transform.gameObject);
		rmc_o.layer = orig_layer;
		return to_return;
	}
	/// <summary>
	/// Returns top left point of tile in global coords
	/// </summary>
	public static Vector3Int GetTLPos(GameObject rmc_o)
	{
		Vector3Int to_return = new Vector3Int();
		Vector3 center = rmc_o.transform.position;
		Vector3 dim = GetRealTileDims(rmc_o);
		to_return.x = Mathf.RoundToInt(center.x - 2 - 2 * (dim.x - 1));
		to_return.z = Mathf.RoundToInt(center.z + 2 + 2 * (dim.z - 1));

		if (to_return.z % 4 != 0 || to_return.z % 4 != 0)
		{
			Debug.LogError("Wrong position of BL=" + to_return);
		}
		return to_return;
	}

	/// <summary>
	/// Recover terrain before "matching" terrain up. Tile whom terrain is recovered has to be already destroyed!
	/// </summary>
	static void RecoverTerrain(HashSet<int> indexes)
	{
		if (indexes == null || indexes.Count == 0)
			return;
		List<int> indexes_to_remove = new List<int>();
		foreach (var index in indexes)
		{
			Vector3 v = Consts.IndexToPos(index);
			if (v.x % 4 == 0 && v.z % 4 == 0)
				continue;
			v.y = Consts.MAX_H;
			var trafs = Physics.SphereCastAll(v, 0.005f, Vector3.down, Consts.RAY_H, 1 << 9);
			if (trafs.Count() >= 2)
			{
				indexes_to_remove.Add(index);
				//Consts.former_heights[indexes[i]] = hit.point.y;
			}
		}
		//foreach (var index_to_remove in indexes_to_remove)
		//	indexes.Remove(index_to_remove);

		Consts.UpdateMapColliders(indexes, true);
	}

	/// <summary>
	/// Center of tile. real dimensions of tile.
	/// Checks if tile isn't sticking out of map boundaries
	/// </summary>
	static bool IsTherePlace4Tile(Vector3Int pos, Vector3Int dims)
	{

		pos.y = Consts.MAX_H;
		// out of grass
		if (pos.z <= 0 || pos.z >= 4 * Consts.TRACK.Height || pos.x <= 0 || pos.x >= 4 * Consts.TRACK.Width)
			return false;
		// quarter of tile sticking out of bounds
		if ((dims.z == 2 && pos.z < 4) || (dims.x == 2 && pos.x == 4 * Consts.TRACK.Width))
			return false;
		// other tiles block
		if (Physics.BoxCast(pos, new Vector3(4 * dims.x * 0.4f, 1, 4 * dims.z * 0.4f), Vector3.down, Quaternion.identity, Consts.RAY_H, 1 << 9))
			return false;
		else
			return true;
	}
	public static float GetPzero(string tilename)
	{
		try
		{
			return -TileManager.TileListInfo[tilename].Model.P3DHeight / (2f * 5f);
		}
		catch
		{
			Debug.LogError(tilename);
			return 0;
		}
	}
	/// <summary>
	/// Places given tiles again onto terrain. (This function usually runs after changing terrain)
	/// </summary>
	public static void UpdateTiles(List<GameObject> rmcs)
	{
		for(int i=0; Loader.Isloading ? i < 1 : i < 2; i++)
		{
			foreach (GameObject rmc_o in rmcs)
			{
				rmc_o.layer = 9;
				// Match RMC up and take care of current_heights table
				Calculate_All_RMC_points(rmc_o);
				//Match_rmc2rmc(rmc_o);
				Vector3Int pos = Vpos2tpos(rmc_o);
				//Delete old prefab and replace it with plain new
				if (rmc_o.transform.childCount != 0)
					DestroyImmediate(rmc_o.transform.GetChild(0).gameObject);

				Vector3Int tileDims = GetRealTileDims(rmc_o);

				Hide_underlying_grass(rmc_o.transform.position);

				Consts.UpdateMapColliders(rmc_o.transform.position, tileDims);
				bool Mirrored = Consts.TilePlacementArray[pos.z, pos.x].Inversion;
				int Rotation = Consts.TilePlacementArray[pos.z, pos.x].Rotation;
				byte Height = Consts.TilePlacementArray[pos.z, pos.x].Height;
				GameObject Prefab = GetPrefab(rmc_o.name, rmc_o.transform, Rotation);
				GetPrefabMesh(Mirrored, Prefab);
				Tile_to_RMC_Cast(Prefab, rmc_o, Height);
				rmc_o.layer = 9;
			}
		}
	}

	public static void InverseMesh(Mesh mesh)
	{
		Vector3[] verts = mesh.vertices;
		for (int i = 0; i < verts.Length; i++)
			verts[i] = new Vector3(-verts[i].x, verts[i].y, verts[i].z);
		mesh.vertices = verts;

		for (int i = 0; i < mesh.subMeshCount; i++) // Every material has to be assigned with triangle array
		{
			int[] trgs = mesh.GetTriangles(i);
			mesh.SetTriangles(trgs.Reverse().ToArray(), i);
		}
	}
	public static GameObject GetPrefab(string TileName, Transform parent, int rotation)
	{
		//set the model and textures for the tile
		GameObject Prefab = new GameObject(TileName);
		Mesh m = TileManager.TileListInfo[TileName].Model.CreateMesh();
		var mf = Prefab.AddComponent<MeshFilter>();
		mf.mesh = m;
		var mr = Prefab.AddComponent<MeshRenderer>();
		mr.materials = TileManager.TileListInfo[TileName].Materials.ToArray();
		Prefab.transform.SetParent(parent, false);
		Prefab.transform.localScale /= 5f;
		Prefab.transform.rotation = Quaternion.Euler(new Vector3(0, rotation, 0));
		return Prefab;
	}
	/// <summary>
	/// You need to add borders to vault before calling this
	/// </summary>
	static bool Calculate_All_RMC_points(GameObject rmc)
	{
		// suppose MeshVerts returns nxm unconstrained mesh
		Vector3[] verts = GetMeshVerts(rmc);

		Quarter[] tile_quarters = Quarter.Generate_Quarters(rmc);

		for (int index = 0; index < verts.Length; index++)
		{
			Vector3Int v = Vector3Int.RoundToInt(rmc.transform.TransformPoint(verts[index]));

			if (!Consts.IsWithinMapBounds(v))
				continue;
			// find a quarter that given vertex belongs to and get information about restriction pattern
			Quarter quarter = tile_quarters.Aggregate(
				(minItem, nextItem) => Consts.Distance(minItem.pos, v) < Consts.Distance(nextItem.pos, v) ? minItem : nextItem);

			if (quarter.qt.Unrestricted())
			{
				verts[index].y = Consts.current_heights[Consts.PosToIndex(v)];
			}
			else if (quarter.qt.Both_restricted())
			{
				if (quarter.qt.All_restricted() && quarter.original_grid.Count == 4)
				{
					verts[index].y = Razor_both_restricted_formula(v);
					Consts.current_heights[Consts.PosToIndex(v)] = verts[index].y;
				}
				else
				{
					if (Consts.Lies_on_restricted_border(v,BorderType.Horizontal, quarter))
						verts[index].y = Calculate_horizontal_height(v);
					else if(Consts.Lies_on_restricted_border(v, BorderType.Vertical, quarter))
						verts[index].y = Calculate_vertical_height(v);
					else
						verts[index].y = Consts.current_heights[Consts.PosToIndex(v)];
				}
			}
			else if(quarter.qt.Horizontal_restricted())
			{
				if (!quarter.original_grid.Contains(Consts.PosToIndex(v)) || Consts.Lies_on_restricted_border(v, BorderType.Horizontal, quarter))
				{
					verts[index].y = Calculate_horizontal_height(v);
				}
				else
					verts[index].y = Consts.current_heights[Consts.PosToIndex(v)];
				Consts.current_heights[Consts.PosToIndex(v)] = verts[index].y;
			}
			else if(quarter.qt.Vertical_restricted())
			{
				if (!quarter.original_grid.Contains(Consts.PosToIndex(v)) || Consts.Lies_on_restricted_border(v, BorderType.Vertical, quarter))
					verts[index].y = Calculate_vertical_height(v);
				else
					verts[index].y = Consts.current_heights[Consts.PosToIndex(v)];

				Consts.current_heights[Consts.PosToIndex(v)] = verts[index].y;
			}

			if (float.IsNaN(verts[index].y))
			{
				return false;
			}
		}
		UpdateMeshes(rmc, verts);
		return true;
	}
	static float Calculate_vertical_height(Vector3Int v)
	{
		float h1 = Consts.current_heights[Consts.PosToIndex(v.x, v.z - (v.z % 4))];
		float h2 = Consts.current_heights[Consts.PosToIndex(v.x, v.z + (4 - (v.z % 4)))];
		return Mathf.Lerp(h1, h2, v.z % 4f / 4f);
	}
	static float Calculate_horizontal_height(Vector3Int v)
	{
		float h1 = Consts.current_heights[Consts.PosToIndex(v.x - (v.x % 4), v.z)];
		float h2 = Consts.current_heights[Consts.PosToIndex(v.x + (4 - (v.x % 4)), v.z)];
		return Mathf.Lerp(h1, h2, v.x % 4f / 4f);
	}
	static float Razor_both_restricted_formula(Vector3Int v)
	{
		float h1 = Consts.current_heights[Consts.PosToIndex(new Vector3(4 * (v.x / 4), v.y, 4 * (v.z / 4)))]; // BL
		float h2 = Consts.current_heights[Consts.PosToIndex(new Vector3(4 * (v.x / 4), v.y, 4 + 4 * (v.z / 4)))]; // TL
		float h3 = Consts.current_heights[Consts.PosToIndex(new Vector3(4 + 4 * (v.x / 4), v.y, 4 * (v.z / 4)))]; // BR
		float h4 = Consts.current_heights[Consts.PosToIndex(new Vector3(4 + 4 * (v.x / 4), v.y, 4 + 4 * (v.z / 4)))]; // TR
		float n = v.x % 4;
		float m = v.z % 4;
		float calculated_h = h1 + n / 4 * (h3 - h1) + m / 4 * ((h2 + n / 4 * (h4 - h2)) - (h1 + n / 4 * (h3 - h1)));
		return calculated_h;
	}
	/// <summary>
	/// Instantiates rmc with no correct placing and no prefab. Rest of the work is delegated to UpdateTiles function
	/// </summary>
	public GameObject PlaceTile(Vector3Int TLpos, string name, int cum_rotation, bool mirrored = false, byte Height = 0)
	{
		// Placing tile with LMB cannot be accepted if X is pressed or there's no place 4 tile 
		AllowLMB = false;
		// Don't allow placing tile in delete or pickup mode
		if (Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.C))
			return null;
		if (name == "NULL")
			return null;
		//Get real dims of tile
		Vector3Int tileDims = new Vector3Int(TileManager.TileListInfo[name].Size.x, 0, TileManager.TileListInfo[name].Size.y);
		if (cum_rotation == 90 || cum_rotation == 270)
		{
			int pom = tileDims.x;
			tileDims.x = tileDims.z;
			tileDims.z = pom;
		}
		Vector3Int rmcPlacement = new Vector3Int(TLpos.x + 2 + 2 * (tileDims.x - 1), 0, TLpos.z - 2 - 2 * (tileDims.z - 1));

		if (rmcPlacement.z < 0)
			return null;

		if (enableMixing || Loader.Isloading)
		{
			if (!IsTherePlaceForQuarter(TLpos, rmcPlacement, tileDims))
			{
				current_rmc = null;
				if (Loader.Isloading)
					Consts.TilePlacementArray[TLpos.z / 4 - 1, TLpos.x / 4].Name = null;
				return null;
			}
		}
		else if (!IsTherePlace4Tile(rmcPlacement, tileDims))
		{
			if (Loader.Isloading)
				Consts.TilePlacementArray[TLpos.z / 4 - 1, TLpos.x / 4].Name = null;
			current_rmc = null;
			return null;
		}
		
		AllowLMB = true;
		// Instantiate RMC
		current_rmc = GetRMC_and_Set_Restriction_info(name, cum_rotation, mirrored, rmcPlacement);
		current_rmc.name = name;
		Border_Vault.Add_borders_of(current_rmc);
		//Update RMC
		if (!Calculate_All_RMC_points(current_rmc))
		{
			Border_Vault.Remove_borders_of(current_rmc);
			Destroy(current_rmc);
			return null;
		}

		Vector3Int pos = Vpos2tpos(current_rmc);
		Consts.TilePlacementArray[pos.z, pos.x].t_verts = GetRmcIndices(current_rmc);
		Hide_underlying_grass(current_rmc.transform.position);
		
		if (!Loader.Isloading)
		{
			GameObject Prefab = GetPrefab(current_rmc.name, current_rmc.transform, cum_rotation);
			GetPrefabMesh(mirrored, Prefab);

			UpdateTiles(Get_surrounding_tiles(current_rmc));
			Consts.UpdateMapColliders(current_rmc.transform.position, tileDims);
			Tile_to_RMC_Cast(Prefab, current_rmc, MixingHeight);

			return null;
		}
		else
		{
			Save_tile_properties(current_rmc, name, mirrored, cum_rotation, new Vector3Int(TLpos.x / 4, 0, TLpos.z / 4 - 1), Height);
			return current_rmc;
		}
	}
	bool IsTherePlaceForQuarter(Vector3Int TLpos, Vector3Int rmc_pos, Vector3Int dims)
	{
		// out of grass
		if (rmc_pos.z <= 0 || rmc_pos.z >= 4 * Consts.TRACK.Height || rmc_pos.x <= 0 || rmc_pos.x >= 4 * Consts.TRACK.Width)
			return false;
		// quarter of tile sticking out of bounds
		if ((dims.z == 2 && rmc_pos.z < 4) || (dims.x == 2 && rmc_pos.x == 4 * Consts.TRACK.Width))
			return false;
		if (Loader.Isloading)
			return true;
		TLpos.x /= 4;
		TLpos.z /= 4;
		TLpos.z--; //get BL not TL for array check
		if (Consts.TilePlacementArray[TLpos.z, TLpos.x].Name == null)
			return true;
		else
			return false;
	}


	private GameObject GetRMC_and_Set_Restriction_info(string tilename, int rotation, bool is_mirrored, Vector3 rmcPlacement)
	{// ((unity loads models with -x axis))
		string RMCname = TileManager.TileListInfo[tilename].RMCname;
		Quaternion rotate_q = Quaternion.Euler(new Vector3(0, rotation, 0));
		char x = RMCname[0];
		char z = RMCname[2];

		if (x == '1' && z == '1')
		{
			//nothing
		}
		else if (x == '1' && z == '2')
		{
			if (is_mirrored)
			{
				rotate_q = Quaternion.Euler(new Vector3(0, -rotation, 0));
				if (rotation == 90 || rotation == 270)
				{
					// V1 = H2
					if (RMCname.Contains("H2") && !RMCname.Contains("V1"))
						RMCname += "V1";
					else if (!RMCname.Contains("H2") && RMCname.Contains("V1"))
						RMCname = RMCname.Replace("V1", "");
				}
			}
		}
		else if (x == '2' && z == '1')
		{
			if (is_mirrored)
			{
				rotate_q = Quaternion.Euler(new Vector3(0, -rotation, 0));
				if (rotation == 0 || rotation == 180)
				{
					// H1 = H2
					if (RMCname.Contains("H2") && !RMCname.Contains("H1"))
						RMCname += "H1";
					else if (!RMCname.Contains("H2") && RMCname.Contains("H1"))
						RMCname = RMCname.Replace("H1", "");
				}
			}
		}
		else // 2x2
		{
			if (is_mirrored)
			{
				// mirrored 2x2 tiles somehow invert restrictions H1 with H2
				// switch H1 with H2
				if (RMCname.Contains("H1"))
					RMCname = RMCname.Replace("H2", "Hx").Replace("H1", "H2").Replace("Hx", "H1");
				else if (RMCname.Contains("H2"))
					RMCname = RMCname.Replace("H2", "H1");
				if (rotation == 90)
				{
					// ("switched" H2) = V2 -> H1 = V2
					if (RMCname.Contains("V2") && !RMCname.Contains("H1"))
						RMCname += "H1";
					else if (!RMCname.Contains("V2") && RMCname.Contains("H1"))
						RMCname = RMCname.Replace("H1", "");
				}
			}
			else
			{
				if (rotation == 270)
				{
					// H2 = V2
					if (RMCname.Contains("V2") && !RMCname.Contains("H2"))
						RMCname += "H2";
					else if (!RMCname.Contains("V2") && RMCname.Contains("H2"))
						RMCname = RMCname.Replace("H2", "");
				}
			}
		}
		//Debug.Log(RMCname + " " + rotation + " " + is_mirrored);
		RMCname = NormalizeRMCname(RMCname);
		var rmc = Instantiate(Get_RMC_Containing(RMCname), rmcPlacement, rotate_q);
		BorderInfo.CreateComponent(rmc, RMCname);
		rmc.GetComponent<MeshRenderer>().material = transparent;
		return rmc;
	}
	string NormalizeRMCname(string RMCname)
	{ //RMCname position: 
		char x = RMCname[0];
		char z = RMCname[2];
		RMCname = RMCname.Replace("H1H1", "H1");
		while (x == '1' && RMCname.Contains("V2"))
			RMCname = RMCname.Replace("V2", "");
		while (z == '1' && RMCname.Contains("H2"))
			RMCname = RMCname.Replace("H2", "");

		return RMCname;
	}
	GameObject Get_RMC_Containing(string RMCname)
	{
		// V1H1H2 => string(V1) string(H1) string(H2)
		//	var restr = RMCname.Substring(3).SplitBy(2).OrderBy(s => s).ToArray();
		//string outname = RMCname.Substring(0, 3) + String.Join("", restr);
		string outname = RMCname.Substring(0, 3);
		GameObject rmc = Resources.Load<GameObject>("rmcs/" + outname);
		//if (rmc == null)
		//	rmc = Resources.Load<GameObject>("rmcs/" + RMCname.Substring(0, 3));
		return rmc;
	}
	static void GetPrefabMesh(bool mirrored, GameObject prefab)
	{
		prefab.GetComponent<MeshFilter>().mesh.MarkDynamic();
		if (mirrored)
			InverseMesh(prefab.GetComponent<MeshFilter>().mesh);
	}

	static void Tile_to_RMC_Cast(GameObject prefab, GameObject rmc, byte Height = 0)
	{
		rmc.layer = 10;
		Mesh mesh = prefab.GetComponent<MeshFilter>().mesh;
		Vector3[] verts = mesh.vertices;
		float pzero = GetPzero(prefab.name);
		// Raycast tiles(H) \ rmc

		for (int i = 0; i < verts.Length; i++)
		{
			RaycastHit hit;
			Vector3 v = prefab.transform.TransformPoint(verts[i]);
			if (Physics.Raycast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, out hit, Consts.RAY_H, 1 << 10))
			{
				verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
			}
			else if (Conecast(new Vector3(v.x, Consts.MAX_H, v.z), Vector3.down, out hit, 10))
			{
				verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
			}
			else if (Physics.SphereCast(new Vector3(v.x, Consts.MAX_H, v.z), .005f, Vector3.down, out hit, Consts.RAY_H, 1 << 10))
			{ // due to the fact rotation in unity is stored in quaternions using floats you won't always hit mesh collider with one-dimensional raycasts. 
				verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
			}
			else
			{
				if (Physics.SphereCast(new Vector3(v.x, Consts.MAX_H, v.z), .005f, Vector3.down, out hit, Consts.RAY_H, 1 << 8))
					verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
				else // out of map boundaries: height of closest edge
					verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, Consts.current_heights[0] + v.y - pzero, v.z));
			}
		}

		mesh.vertices = verts;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		prefab.SetActive(false);
		prefab.SetActive(true);
		prefab.transform.position = new Vector3(prefab.transform.position.x, Height / 5f, prefab.transform.position.z);
		rmc.layer = 9;

	}
	public static bool Conecast(Vector3 global_pos, Vector3 Direction, out RaycastHit hit, int layer)
	{
		Consts.Cone.transform.position = global_pos;
		var hits = Consts.Cone.GetComponent<Rigidbody>().SweepTestAll(Direction, Mathf.Infinity);
		if (hits.Length == 0)
		{
			hit = default;
			return false;
		}
		try
		{
			hit = hits.Where(h => h.transform.gameObject.layer == layer).First();
		}
		catch
		{
			hit = default;
			return false;
		}
		return true;
	}
	public static Vector3[] GetMeshVerts(GameObject rmc_o)
	{
		return rmc_o.GetComponent<MeshFilter>().mesh.vertices;
	}
	static void UpdateMeshes(GameObject rmc_o, Vector3[] newverts)
	{
		Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
		Mesh rmc_mc;
		if (rmc_o.GetComponent<MeshCollider>() != null)
			rmc_mc = rmc_o.GetComponent<MeshCollider>().sharedMesh;
		else
			rmc_mc = rmc_o.AddComponent<MeshCollider>().sharedMesh;

		rmc.vertices = newverts;
		rmc.RecalculateBounds();
		rmc.RecalculateNormals();
		rmc_mc = null;
		rmc_mc = rmc;
		rmc_o.SetActive(false);
		rmc_o.SetActive(true);
	}
}