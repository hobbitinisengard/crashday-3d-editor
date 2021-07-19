﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
//Handles BUILD mode.

public class Build : MonoBehaviour
{
	private static readonly float spherecast_radius = 5e-6f;
	// "real mesh collider" - RMC - plane with vertices set in positions where tile can have its terrain vertices changed
	// "znacznik" - tag - white small box spawned only in FORM mode. Form mode uses some static functions from here.
	public GameObject editorPanel; //-> slidercase.cs
	public Text CURRENTELEMENT; //name of currently selected element on top of the building menu
	public Text CURRENTROTATION;
	public Text CURRENTMIRROR;
	public Text BuildButtonText; // text 'build' in build button
	public Text MixingInfoText; // text for displaying H = mixingHeight and keypad enter
	public GameObject savePanel; // "save track scheme" menu
	public Material partiallytransparent;

	bool LMBclicked = false;
	bool AllowLMB = false;
	/// <summary>obj_rmc = current RMC</summary>
	public static GameObject current_rmc;
	public static bool enableMixing = false;
	public static byte MixingHeight = 0;
	/// <summary>current tile</summary>
	/// <summary>former position of temporary placement of tile in build mode</summary>
	Vector3 last_trawa;
	public static bool nad_wczesniej = Highlight.over;
	/// <summary>current rotation of currently showed element</summary>
	int cum_rotation = 0;
	///<summary> current inversion of an element that is currently being shown</summary>
	bool inversion = false;
	private bool IsEnteringKeypadValue;

	private void OnDisable()
	{
		if (current_rmc != null)
		{
			if (!LMBclicked || (LMBclicked && !AllowLMB))
				DelLastPrefab();
			nad_wczesniej = false;
		}
	}
	void Update()
	{
		if (!MouseInputUIBlocker.BlockedByUI)
		{
			// Ctrl + RMB - picks up mixing height in mixing mode so rotation with ctrl is forbidden
			if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftControl))
				cum_rotation = (cum_rotation == 270) ? 0 : cum_rotation + 90;

			CURRENTELEMENT.text = EditorMenu.tile_name;
			CURRENTROTATION.text = cum_rotation.ToString();
			CURRENTMIRROR.text = inversion.ToString();
			if (Input.GetKeyUp(KeyCode.M))
				SwitchMixingMode();
			if (enableMixing)
				CtrlWithMousewheelWorks();
			if (Input.GetKeyDown(KeyCode.Q))
				InverseState(); // Q enabled inversion
			if (Input.GetKey(KeyCode.X))
				XButtonState(); // X won't let PlacePrefab work
			if (Input.GetKey(KeyCode.LeftAlt))
				SwitchToNULL();

			if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
			{
				PickUpTileUnderCursor();// Pick up tile under cursor
				return;
			}
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

			if (EditorMenu.tile_name != "NULL" && !Input.GetKey(KeyCode.Space))
			{
				if (!Highlight.over)
				{
					if (nad_wczesniej)
					{
						//Debug.Log("Było przejście na nulla z trawki");
						if (!LMBclicked && AllowLMB)
							DelLastPrefab();
						else
							LMBclicked = false;
					}
					nad_wczesniej = false;
				}
				else
				{ //If cursor points on map
					if (!nad_wczesniej)
					{
						//Debug.Log("cursor: void -> terrain");
						PlaceTile(Highlight.TL, EditorMenu.tile_name, cum_rotation, inversion);
						last_trawa = Highlight.TL;
						nad_wczesniej = true;
					}
					else if (last_trawa.x != Highlight.TL.x || last_trawa.z != Highlight.TL.z)
					{
						//Debug.Log("cursor: terrain chunk -> terrain chunk");
						if (!LMBclicked)//If element hasn't been placed
							DelLastPrefab();
						PlaceTile(Highlight.TL, EditorMenu.tile_name, cum_rotation, inversion);
						last_trawa = Highlight.TL;
						LMBclicked = false;
					}
					else if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.X) && AllowLMB)
					{//Place currently showed tile on terrain
						LMBclicked = true;
						Save_tile_properties(EditorMenu.tile_name, inversion, cum_rotation,
								new Vector3Int(Highlight.TL.x / 4, 0, Highlight.TL.z / 4 - 1), enableMixing ? MixingHeight : (byte)0);
					}
					if (Input.GetMouseButtonDown(1) && Highlight.over && !LMBclicked)
					{//Rotation with RMB
						DelLastPrefab();
						PlaceTile(Highlight.TL, EditorMenu.tile_name, cum_rotation, inversion);
						nad_wczesniej = true;
					}
				}
			}
			else if (Input.GetKey(KeyCode.Space) && !LMBclicked)
			{//Delete currently showed element when moving camera with spacebar
				DelLastPrefab();
				nad_wczesniej = false;
			}
		}
		else if (nad_wczesniej)
		{//Delete currently showed element when moving cursor over menu
			if (!LMBclicked)
				DelLastPrefab();
			else
				LMBclicked = false;
			nad_wczesniej = false;
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
		}
		KeyCode[] keyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9 };
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
			MixingHeightPreview();
		}
	}
	/// <summary>Displays transparent cuboid for 2 secs.</summary>
	public void MixingHeightPreview()
	{
		StartCoroutine(DisplayMessageFor(MixingHeight.ToString(), 2));
		GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Destroy(preview.GetComponent<BoxCollider>());
		preview.GetComponent<MeshRenderer>().material = partiallytransparent;
		preview.transform.localScale = new Vector3(3f, 0.05f, 3);
		preview.transform.position = new Vector3(2 + Highlight.TL.x, MixingHeight / 5f, Highlight.TL.z - 2);
		Destroy(preview, 2);
	}
	/// <summary>
	/// Toggles off tile preview 
	/// </summary>
	void SwitchToNULL()
	{
		if (!LMBclicked)
			DelLastPrefab();
		else
			LMBclicked = false;
		nad_wczesniej = false;
		EditorMenu.tile_name = "NULL";
	}
	void InverseState()
	{
		inversion = !inversion;
		DelLastPrefab();
		nad_wczesniej = false;
	}
	void XButtonState()
	{
		DelLastPrefab();
		nad_wczesniej = false;
		if (Input.GetMouseButtonDown(0))
			Del_underlying_element();
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
	}
	public static void DelLastPrefab()
	{
		if (current_rmc != null)
		{
			Vector3Int pos = Vpos2tpos(current_rmc);
			if (Service.TilePlacementArray[pos.z, pos.x].Name != null)
				return;
			Unhide_trawkas(current_rmc);
			List<GameObject> surroundings = Get_surrounding_tiles(current_rmc);
			DestroyImmediate(current_rmc);
			int[] t_verts = Service.TilePlacementArray[pos.z, pos.x].t_verts;
			if (t_verts != null)
				RecoverTerrain(t_verts.ToList());
			UpdateTiles(surroundings);
		}
	}
	void PickUpTileHeightUnderCursor()
	{
		MixingHeight = Service.TilePlacementArray[Highlight.TL.z / 4 - 1, Highlight.TL.x / 4].Height;
	}
	void PickUpTileUnderCursor()
	{
		Vector3 v = Highlight.pos;
		v.y = Service.MAX_H;
		bool traf = Physics.Raycast(v, Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 9);
		if (traf)
		{
			EditorMenu.tile_name = hit.transform.name;
			editorPanel.GetComponent<SliderCase>().SwitchToTileset(TileManager.TileListInfo[EditorMenu.tile_name].TilesetName);
		}
	}
	public static bool IsCheckpoint(string nazwa_tilesa)
	{
		return TileManager.TileListInfo[nazwa_tilesa].IsCheckpoint;
	}
	// Shows only trawkas that are not occupied by tiles (which could be mixed)
	static void Unhide_trawkas(GameObject rmc)
	{
		rmc.layer = 10;
		Vector3 pos = rmc.transform.position;
		pos.y = Service.MAX_H;
		RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Service.RAY_H, 1 << 8);
		foreach (RaycastHit hit in hits)
		{
			pos = hit.transform.position;
			pos.x += 2;
			pos.z += 2;
			pos.y = Service.MAX_H;
			if (!Physics.Raycast(pos, Vector3.down, Service.RAY_H, 1 << 9))
				hit.transform.gameObject.GetComponent<MeshRenderer>().enabled = true;
		}
		rmc.layer = 9;
	}
	static void Hide_trawkas(Vector3 pos)
	{
		pos.y = Service.MAX_H;
		RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Service.RAY_H, 1 << 8);
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
	public static int[] GetRmcIndices(GameObject rmc)
	{
		List<int> to_return = new List<int>();
		Vector3Int TL = GetTLPos(rmc);
		Vector3Int tileDims = GetRealTileDims(current_rmc);
		for (int z = 0; z <= 4 * tileDims.z; z++)
		{
			for (int x = 0; x <= 4 * tileDims.x; x++)
			{
				to_return.Add(Service.PosToIndex(TL.x + x, TL.z - z));
			}
		}
		return to_return.ToArray();
	}
	public static List<GameObject> Get_surrounding_tiles(List<int> indexes)
	{
		List<GameObject> to_return = new List<GameObject>();
		foreach (int index in indexes)
		{
			Vector3 pos = Service.IndexToPos(index);
			pos.y = Service.MAX_H;
			RaycastHit[] hits = Physics.SphereCastAll(pos, 0.1f, Vector3.down, Service.RAY_H, 1 << 9);
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
			pos.y = Service.MAX_H;
			RaycastHit[] hits = Physics.SphereCastAll(pos, .1f, Vector3.down, Service.RAY_H, 1 << 9);
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
		rmc_o.layer = 10;
		List<GameObject> to_return = new List<GameObject>();
		Vector3 extents = rmc_o.GetComponent<MeshFilter>().mesh.bounds.extents;
		extents.y = 1; // bounding box for boxcast must have non-zero height
		Vector3 v = rmc_o.transform.position;
		v.y = Service.MAX_H;
		RaycastHit[] hits = Physics.BoxCastAll(v, extents, Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9);
		foreach (RaycastHit hit in hits)
			to_return.Add(hit.transform.gameObject);
		rmc_o.layer = 9;
		return to_return;
	}
	/// <summary>
	/// Returns top left point of tile in global coords
	/// </summary>
	public static Vector3Int GetTLPos(GameObject rmc_o)
	{
		Vector3Int to_return = new Vector3Int();
		Vector3 el_pos = rmc_o.transform.position;
		Vector3 dim = GetRealTileDims(rmc_o);
		to_return.x = Mathf.RoundToInt(el_pos.x - 2 - 2 * (dim.x - 1));
		to_return.z = Mathf.RoundToInt(el_pos.z + 2 + 2 * (dim.z - 1));

		if (to_return.z % 4 != 0 || to_return.z % 4 != 0)
		{
			Debug.LogError("Wrong position of BL=" + to_return);
		}
		return to_return;
	}
	/// <summary>
	/// Returns real dimensions of objects (e.g. 2x1) taking rotation into consideration
	/// </summary>
	public static Vector3Int GetRealTileDims(GameObject rmc_o)
	{
		bool isRotated = (Mathf.RoundToInt(rmc_o.transform.rotation.eulerAngles.y) == 90 || Mathf.RoundToInt(rmc_o.transform.rotation.eulerAngles.y) == 270) ? true : false;
		Vector2Int dimVec = TileManager.GetRealDims(rmc_o.name, isRotated);
		return new Vector3Int(dimVec.x, 0, dimVec.y);
	}
	static void Save_tile_properties(string nazwa, bool inwersja, int rotacja, Vector3Int arraypos, byte Height = 0)
	{
		Service.TilePlacementArray[arraypos.z, arraypos.x].Set(nazwa, rotacja, inwersja, Height);

		if (TileManager.TileListInfo[nazwa].IsCheckpoint
			&& !Service.TRACK.Checkpoints.Contains((ushort)(arraypos.x + (Service.TRACK.Height - 1 - arraypos.z) * Service.TRACK.Width)))
		{
			Service.TRACK.Checkpoints.Add((ushort)(arraypos.x + (Service.TRACK.Height - 1 - arraypos.z) * Service.TRACK.Width));
			Service.TRACK.CheckpointsNumber++;
		}
	}
	/// <summary>
	/// Recover terrain before "matching" terrain up. Tile whom terrain is recovered has to be already destroyed!
	/// </summary>
	static void RecoverTerrain(List<int> indexes)
	{
		if (indexes == null || indexes.Count == 0)
			return;
		for (int i = 0; i < indexes.Count; i++)
		{
			Vector3 v = Service.IndexToPos(indexes[i]);
			if (v.x % 4 == 0 && v.z % 4 == 0)
				continue;
			v.y = Service.MAX_H;
			bool traf = Physics.SphereCast(v, 0.005f, Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 9);
			if (traf)
			{
				//Service.former_heights[indexes[i]] = hit.point.y;
			}
		}
		Service.UpdateMapColliders(indexes, true);
	}

	/// <summary>
	/// Center of tile. real dimensions of tile.
	/// Checks if tile isn't sticking out of map boundaries
	/// </summary>
	static bool IsTherePlace4Tile(Vector3Int pos, Vector3Int dims)
	{

		pos.y = Service.MAX_H;
		// out of grass
		if (pos.z <= 0 || pos.z >= 4 * Service.TRACK.Height || pos.x <= 0 || pos.x >= 4 * Service.TRACK.Width)
			return false;
		// quarter of tile sticking out of bounds
		if ((dims.z == 2 && pos.z < 4) || (dims.x == 2 && pos.x == 4 * Service.TRACK.Width))
			return false;
		// other tiles block
		if (Physics.BoxCast(pos, new Vector3(4 * dims.x * 0.4f, 1, 4 * dims.z * 0.4f), Vector3.down, Quaternion.identity, Service.RAY_H, 1 << 9))
			return false;
		else
			return true;
	}
	//Looks over 'Pzero' height by tile name
	public static float GetPzero(string nazwa)
	{
		try
		{
			return -TileManager.TileListInfo[nazwa].Model.P3DHeight / (2f * 5f);
		}
		catch
		{
			Debug.LogError(nazwa);
			return 0;
		}
	}

	/// <summary>
	/// Places given tiles again onto terrain. (This function usually runs after changing terrain)
	/// </summary>
	public static void UpdateTiles(List<GameObject> rmcs)
	{
		//1. Updating only vertices of every RMC in list.
		foreach (GameObject rmc_o in rmcs)
		{
			rmc_o.layer = 9;
			//Update RMC
			Vector3[] verts = GetMeshVerts(rmc_o);
			for (int i = 0; i < verts.Length; i++)
			{
				Vector3Int v = Vector3Int.RoundToInt(rmc_o.transform.TransformPoint(verts[i]));
				verts[i].y = Service.current_heights[Service.PosToIndex(v)];
			}
			UpdateMeshes(rmc_o, verts);
		}
		//2. Matching edge of every rmc up if under or above given vertex already is another vertex (of another tile)
		foreach (GameObject rmc_o in rmcs)
		{
			rmc_o.layer = 10;
			// Match RMC up and take care of current_heights table
			Match_rmc2rmc(rmc_o);
			Vector3Int pos = Vpos2tpos(rmc_o);
			bool Mirrored = Service.TilePlacementArray[pos.z, pos.x].Inversion;
			int Rotation = Service.TilePlacementArray[pos.z, pos.x].Rotation;
			byte Height = Service.TilePlacementArray[pos.z, pos.x].Height;
			//Delete old prefab and replace it with plain new
			if (rmc_o.transform.childCount != 0)
				DestroyImmediate(rmc_o.transform.GetChild(0).gameObject);
			GameObject Prefab = GetPrefab(rmc_o.name, rmc_o.transform, Rotation);

			Vector3Int tileDims = GetRealTileDims(rmc_o);
			Vector3Int TLpos = GetTLPos(rmc_o);

			Hide_trawkas(rmc_o.transform.position);
			for (int z = 0; z <= 4 * tileDims.z; z++)
			{
				for (int x = 0; x <= 4 * tileDims.x; x++)
				{
					if (x == 0 || z == 0 || x == 4 * tileDims.x || z == 4 * tileDims.z)
					{
						Match_boundaries(x, -z, TLpos);
					}
					else
					{
						Hide_Inside(x, -z, TLpos);
					}
				}
			}
			Service.UpdateMapColliders(rmc_o.transform.position, tileDims);
			GetPrefabMesh(Mirrored, Prefab);
			Tiles_to_RMC_Cast(Prefab, Mirrored, Height);
			rmc_o.layer = 9;

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
	/// Instantiates rmc with no correct placing and no prefab. Rest of the work is delegated to UpdateTiles function
	/// </summary>
	public GameObject PlaceTile(Vector3Int TLpos, string name, int cum_rotation, bool mirrored = false, byte Height = 0)
	{
		// Placing tile with LMB cannot be accepted if X is pressed or there's no place 4 tile 
		AllowLMB = false;
		// Don't allow placing tile in delete mode
		if (Input.GetKey(KeyCode.X))
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

		if (enableMixing || Service.Isloading)
		{
			if (!IsTherePlaceForQuarter(TLpos, rmcPlacement, tileDims))
			{
				current_rmc = null;
				if (Service.Isloading)
					Service.TilePlacementArray[TLpos.z / 4 - 1, TLpos.x / 4].Name = null;
				return null;
			}
		}
		else if (!IsTherePlace4Tile(rmcPlacement, tileDims))
		{
			if (Service.Isloading)
				Service.TilePlacementArray[TLpos.z / 4 - 1, TLpos.x / 4].Name = null;
			current_rmc = null;
			return null;
		}


		AllowLMB = true;

		// Instantiate RMC
		current_rmc = GetRMC(name, cum_rotation, mirrored, rmcPlacement);
		current_rmc.name = name;
		current_rmc.GetComponent<MeshRenderer>().enabled = false;
		//Update RMC
		Vector3[] verts = GetMeshVerts(current_rmc);
		for (int index = 0; index < verts.Length; index++)
		{
			Vector3Int v = Vector3Int.RoundToInt(current_rmc.transform.TransformPoint(verts[index]));
			try
			{
				verts[index].y = Service.current_heights[Service.PosToIndex(v)];
			}
			catch
			{ // rmc out of bounds
				verts[index].y = 0;
			}
		}
		if (verts.Any(v => float.IsNaN(v.y)))
		{
			Destroy(current_rmc);
			current_rmc = null;
			return null;
		}
		UpdateMeshes(current_rmc, verts);

		MeshCollider rmc_mc = current_rmc.GetComponent<MeshCollider>();

		Vector3Int pos = Vpos2tpos(current_rmc);
		Service.TilePlacementArray[pos.z, pos.x].t_verts = GetRmcIndices(current_rmc);
		current_rmc.layer = 10;
		Hide_trawkas(current_rmc.transform.position);
		for (int z = 0; z <= 4 * tileDims.z; z++)
		{
			for (int x = 0; x <= 4 * tileDims.x; x++)
			{
				if (x == 0 || z == 0 || x == 4 * tileDims.x || z == 4 * tileDims.z)
				{
					Match_boundaries(x, -z, TLpos); // borders
				}
				else
				{
					Hide_Inside(x, -z, TLpos);
				}
			}
		}
		if (!Service.Isloading)
		{
			GameObject Prefab = GetPrefab(current_rmc.name, current_rmc.transform, cum_rotation);
			GetPrefabMesh(mirrored, Prefab);

			UpdateTiles(Get_surrounding_tiles(current_rmc));
			Service.UpdateMapColliders(current_rmc.transform.position, tileDims);
			Tiles_to_RMC_Cast(Prefab, mirrored, MixingHeight);
			return null;
		}
		else
		{
			Save_tile_properties(name, mirrored, cum_rotation, new Vector3Int(TLpos.x / 4, 0, TLpos.z / 4 - 1), Height);
			current_rmc.layer = 9;
			return current_rmc;
		}
	}
	bool IsTherePlaceForQuarter(Vector3Int TLpos, Vector3Int rmc_pos, Vector3Int dims)
	{
		// out of grass
		if (rmc_pos.z <= 0 || rmc_pos.z >= 4 * Service.TRACK.Height || rmc_pos.x <= 0 || rmc_pos.x >= 4 * Service.TRACK.Width)
			return false;
		// quarter of tile sticking out of bounds
		if ((dims.z == 2 && rmc_pos.z < 4) || (dims.x == 2 && rmc_pos.x == 4 * Service.TRACK.Width))
			return false;
		if (Service.Isloading)
			return true;
		TLpos.x /= 4;
		TLpos.z /= 4;
		TLpos.z--; //get BL not TL for array check
		if (Service.TilePlacementArray[TLpos.z, TLpos.x].Name == null)
			return true;
		else
			return false;
	}

	public static void Del_underlying_element()
	{
		bool traf = Physics.Raycast(new Vector3(Highlight.pos.x, Service.MAX_H, Highlight.pos.z), Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 9);
		if (traf)
		{
			Vector3Int pos = Vpos2tpos(hit.transform.gameObject);
			if (TileManager.TileListInfo[hit.transform.gameObject.name].IsCheckpoint)
			{
				Service.TRACK.Checkpoints.Remove((ushort)(pos.x + (Service.TRACK.Height - 1 - pos.z) * Service.TRACK.Width));
				Service.TRACK.CheckpointsNumber--;
			}

			Unhide_trawkas(hit.transform.gameObject);
			List<GameObject> to_restore = Get_surrounding_tiles(hit.transform.gameObject);
			DestroyImmediate(hit.transform.gameObject);
			Service.TilePlacementArray[pos.z, pos.x].Name = null;
			RecoverTerrain(Service.TilePlacementArray[pos.z, pos.x].t_verts.ToList());
			UpdateTiles(to_restore);
		}
	}
	private GameObject GetRMC(string tilename, int rotation, bool is_mirrored, Vector3 rmcPlacement)
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
				else if(RMCname.Contains("H2"))
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
		return Instantiate(Get_RMC_Containing(RMCname), rmcPlacement, rotate_q);
	}
	string NormalizeRMCname(string RMCname)
	{ //RMCname position: 
		char x = RMCname[0];
		char z = RMCname[2];
		while (x == '1' && RMCname.Contains("V2"))
			RMCname = RMCname.Replace("V2", "");
		while (z == '1' && RMCname.Contains("H2"))
			RMCname = RMCname.Replace("H2", "");

		return RMCname;
	}
	GameObject Get_RMC_Containing(string RMCname)
	{
		RMCname = NormalizeRMCname(RMCname);
		// V1H1H2 => string(V1) string(H1) string(H2)
		string[] restr = RMCname.Substring(3).SplitBy(2).ToArray();
		string outname;
		try
		{
			outname = Service.RMC_NAMES.Where(name => name.Contains(RMCname.Substring(0, 3))
		&& restr.All(r => name.Contains(r))
		&& name.Length == 3 + restr.Length * 2).First();
		}
		catch
		{
			Debug.LogWarning(RMCname);
			return null;
		}
		return Resources.Load<GameObject>("rmcs/" + outname);
	}
	static void GetPrefabMesh(bool mirrored, GameObject prefab)
	{
		prefab.GetComponent<MeshFilter>().mesh.MarkDynamic();
		if (mirrored)
			InverseMesh(prefab.GetComponent<MeshFilter>().mesh);
	}

	/// <summary>
	/// getPzero for prefab, mark rmc with layer 10. If raycast wasn't successful then try layer 8 (grass), if still wasn't successful, then set height of 0
	/// Raycast logic for placing tile onto RMC. Used in PlacePrefab and UpdateTiles
	/// </summary>
	static void Tiles_to_RMC_Cast(GameObject prefab, bool inwersja, byte Height = 0)
	{
		Mesh mesh = prefab.GetComponent<MeshFilter>().mesh;
		float pzero = GetPzero(prefab.name);
		// Raycast tiles(H) \ rmc
		Vector3[] verts = mesh.vertices;
		for (int i = 0; i < mesh.vertices.Length; i++)
		{
			Vector3 v = prefab.transform.TransformPoint(mesh.vertices[i]);
			if (Physics.Raycast(new Vector3(v.x, Service.MAX_H, v.z), Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 10))
			{
				verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
			}
			else if (Physics.SphereCast(new Vector3(v.x, Service.MAX_H, v.z), spherecast_radius, Vector3.down, out hit, Service.RAY_H, 1 << 10))
			{ // due to the fact rotation in unity is stored in quaternions using floats you won't always hit mesh collider with one-dimensional raycasts. 
				verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
			}
			else
			{ // when tile vertex is out of its dimensions (eg crane), cast on foreign rmc or map
				if (Physics.SphereCast(new Vector3(v.x, Service.MIN_H - 1, v.z), spherecast_radius, Vector3.up, out hit, Service.RAY_H, 1 << 9 | 1 << 8))
					verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
				else // out of map boundaries: height of closest edge
					verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, Service.current_heights[0] + v.y - pzero, v.z));
			}
			mesh.vertices = verts;
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			prefab.SetActive(false);
			prefab.SetActive(true);
			prefab.transform.position = new Vector3(prefab.transform.position.x, Height / 5f, prefab.transform.position.z);
		}
	}
	/// <summary>
	///Returns mesh "main" of tile or if tile doesn't have it, mesh of its meshfilter
	/// </summary>
	public static Mesh GetMainMesh(ref GameObject prefab)
	{
		if (prefab.transform.childCount != 0)
		{
			for (int i = 0; i < prefab.transform.childCount; i++)
				if (prefab.transform.GetChild(i).name == "main")
					return prefab.transform.GetChild(i).GetComponent<MeshFilter>().mesh;

			return null; // <-- hopefully Never going to end up here
		}
		else
			return prefab.GetComponent<MeshFilter>().mesh;
	}
	/// <summary>
	/// Toggles off visibility of terrain chunk laying under tile of bottom-left x,z. 
	/// Updates current_heights array. Layer of RMC has to be 10.
	/// </summary>
	public static void Hide_Inside(int x, int z, Vector3Int TLpos)
	{
		x += TLpos.x;
		z = TLpos.z + z; //x,y are global
		if (!Service.IsWithinMapBounds(x, z))
			return;
		int index = Service.PosToIndex(x, z);
		Vector3 v = new Vector3(x, Service.MIN_H - 1, z);
		if (Physics.Raycast(v, Vector3.up, out RaycastHit hit, Service.RAY_H, 1 << 10))
		{
			Service.current_heights[index] = hit.point.y;
		}
	}
	/// <summary>
	/// Matches up height of terrain to height of vertex of current RMC (layer = 10)
	/// </summary>
	public static void Match_boundaries(int x, int z, Vector3Int TLpos)
	{
		if (x % 4 == 0 && z % 4 == 0)
			return;
		x += TLpos.x;
		z = TLpos.z + z;
		Vector3Int v = new Vector3Int(x, Service.MAX_H, z);
		if (!Service.IsWithinMapBounds(x, z))
			return;
		int index = Service.PosToIndex(x, z);
		if (index == -1)
			return;
		if (Physics.Raycast(v, Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 10))
		//&& Mathf.Abs(hit.point.y - Service.current_heights[index]) > 0.1)
		{
			Service.current_heights[index] = hit.point.y;
		}
		else
		{ // For Quaternion rotation (90deg) numerical accuracy
			if (Physics.SphereCast(v, spherecast_radius, Vector3.down, out hit, Service.RAY_H + 1, 1 << 10))
			{
				Service.current_heights[index] = hit.point.y;
			}
		}
		return;
	}
	/// <summary>
	/// Matches vertices of RMCs one to another. rmc_o layer = 10
	/// </summary>
	public static void Match_rmc2rmc(GameObject rmc_o)
	{
		Vector3[] verts = GetMeshVerts(rmc_o);
		for (int i = 0; i < verts.Length; i++)
		{
			Vector3Int v = Vector3Int.RoundToInt(rmc_o.transform.TransformPoint(verts[i]));
			v.y = Service.MAX_H;
			if (Physics.SphereCast(v, spherecast_radius, Vector3.down, out RaycastHit hit, Service.RAY_H, 1 << 9))
			{
				verts[i].y = hit.point.y;
				Service.current_heights[Service.PosToIndex(v)] = hit.point.y;
			}
		}
		UpdateMeshes(rmc_o, verts);
	}
	static Vector3[] GetMeshVerts(GameObject rmc_o)
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