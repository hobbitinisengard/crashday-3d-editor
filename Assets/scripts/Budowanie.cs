﻿using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
public class Budowanie : MonoBehaviour {
    public Slider editorSlider;
    public Text CURRENTELEMENT;
    public GameObject savePanel;

    public static List<GameObject> praobiekty = new List<GameObject>();
    /// <summary>
    /// Lista trawek ukrywanych w PlacePrefab -> hide_8(), zapisywana w save_current_prefab();
    /// </summary>
	public bool LMBclicked = false;
    public bool AllowLMB = false;
    /// <summary>
    /// bieżący RMC
    /// </summary>
	public static GameObject obj_rmc;
    /// <summary>
    /// bieżący tiles
    /// </summary>
	GameObject prefab;
    /// <summary>
    /// wcześniejsze położenie Lewego Dolnego (LD) rogu trawy
    /// </summary>
	Vector3 last_trawa;
	public static bool nad_wczesniej = Highlight.nad;
    /// <summary>
    /// całkowita rotacja bieżącego elementu
    /// </summary>
	int cum_rotation = 0;
    /// <summary>
    /// bieżąca inwersja
    /// </summary>
    bool inversion = false;
    
    void Awake () {
		for(int i=3; i<this.transform.childCount; i++)
        {
            for (int j = 0; j < this.transform.GetChild(i).childCount; j++)
                this.transform.GetChild(i).GetChild(j).gameObject.AddComponent<Poka_nazwe_tilesa>();
        }
    }
	void Update() {
        if (!savePanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(1))
                cum_rotation = (cum_rotation == 270) ? 0 : cum_rotation + 90;

            CURRENTELEMENT.text = EditorMenu.nazwa_tilesa;
            if(Input.GetKeyDown(KeyCode.Q))
                InverseState(); // Q włącza Inwersję
            if(Input.GetKey(KeyCode.X))
                XButtonState(); // X nie pozwala na postawienie tilesa;
            if (Input.GetKeyDown(KeyCode.LeftAlt))
                SwitchToNULL();

            if (EditorMenu.nazwa_tilesa != "NULL" && !Input.GetKey(KeyCode.Space) && !FlyCamera.over_UI)
            {
                if (!Highlight.nad)
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
                if (Highlight.nad)
                { //Jeśli kursor wskazuje na trawkę
                    if (!nad_wczesniej)
                    {
                        //Debug.Log("Było przejście na trawkę z nulla");
                        PlacePrefab(Highlight.t, EditorMenu.nazwa_tilesa, cum_rotation, inversion);
                        last_trawa = Highlight.t;
                        nad_wczesniej = true;
                    }
                    else if (last_trawa.x != Highlight.t.x || last_trawa.z != Highlight.t.z)
                    {
                        //Debug.Log("Przejście z trawy na trawę");
                        if (!LMBclicked)//Jeżeli nie postawiliśmy klocka
                            DelLastPrefab();
                        PlacePrefab(Highlight.t, EditorMenu.nazwa_tilesa, cum_rotation, inversion);
                        last_trawa = Highlight.t;
                        LMBclicked = false;
                    }

                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
                    { // Zmień podgląd
                        podnies_tilesa();
                    }
                    else if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.X) && AllowLMB)
                    {//Połóż element na trawce
                        LMBclicked = true;
                        Save_tile_properties(EditorMenu.nazwa_tilesa, inversion, cum_rotation, new Vector3Int(Highlight.t.x / 4, 0, Highlight.t.z / 4), SliderCase.last_value);
                    }
                    if (Input.GetMouseButtonDown(1) && Highlight.nad && !LMBclicked)
                    {//Obrót Prawym Przyciskiem Myszy
                     //Debug.Log("Obrót");
                        DelLastPrefab();
                        PlacePrefab(Highlight.t, EditorMenu.nazwa_tilesa, cum_rotation, inversion);
                        nad_wczesniej = true;
                    }
                }
            }
            if (Input.GetKey(KeyCode.Space) && nad_wczesniej)
            {//Fruwająca kamera w czasie stawiania klocka
                if (!LMBclicked)
                    DelLastPrefab();
                else
                    LMBclicked = false;
                nad_wczesniej = false;
            }
            if (nad_wczesniej && FlyCamera.over_UI)
            {//Myszka na UI w czasie stawiania klocka
                if (!LMBclicked)
                    DelLastPrefab();
                else
                    LMBclicked = false;
                nad_wczesniej = false;
            }
        }
	}
    void SwitchToNULL()
    {
        if (!LMBclicked)
            DelLastPrefab();
        else
            LMBclicked = false;
        nad_wczesniej = false;
        EditorMenu.nazwa_tilesa = "NULL";// Wyłącza podgląd
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
            del_underlying_element();
    }
    void podnies_tilesa(){
		RaycastHit hit;
        Vector3Int v = Highlight.pos;
        v.y = Terenowanie.maxHeight+1;
		bool traf = Physics.Raycast(v,Vector3.down,out hit,Terenowanie.rayHeight,1<<9);
		if (traf) {
            Vector3Int LD = GetLDpos(hit.transform.gameObject);
            EditorMenu.nazwa_tilesa = GetRMCname(hit.transform.gameObject);
			editorSlider.GetComponent<SliderCase>().hideCase (editorSlider.value);
            editorSlider.value = STATIC.tiles[LD.x/4, LD.z/4]._kategoria;
            editorSlider.GetComponent<SliderCase>().showCase (editorSlider.value);
            editorSlider.GetComponent<SliderCase>().changeText (editorSlider.value);
		}
	}
    public static bool isCheckpoint(string nazwa_tilesa)
    {
        if (nazwa_tilesa.Contains("chk") || nazwa_tilesa == "rwstart" || nazwa_tilesa == "skijumpcp")
            return true;

        return false;
    }
	public static void del_underlying_element(){
		RaycastHit hit;
		bool traf = Physics.Raycast(new Vector3(Highlight.pos.x,Terenowanie.maxHeight+1,Highlight.pos.z),Vector3.down, out hit, Terenowanie.rayHeight, 1 << 9);
		if (traf && hit.transform.gameObject != obj_rmc) {
            Vector3Int pos = vpos2epos(hit.transform.gameObject);

            if (isCheckpoint(STATIC.tiles[pos.x, pos.z]._nazwa))
            {
                STATIC.TRACK.Checkpoints.Remove((ushort)(pos.x * (SliderHeight.val - 1 - pos.y)));
                STATIC.TRACK.CheckpointsNumber--;
            }

            Unhide_trawkas(hit.transform.position);
            List <GameObject> to_restore = get_surrounding_tiles (hit.transform.gameObject);
			DestroyImmediate (hit.transform.gameObject);
            STATIC.tiles[pos.x, pos.z]._nazwa = null;
            Przywroc_teren(STATIC.tiles[pos.x, pos.z].t_verts);
            UpdateTiles(to_restore);
		}
	}
	static void Unhide_trawkas(Vector3 pos)
    {
        pos.y = Terenowanie.maxHeight+1;
        RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.down, Terenowanie.rayHeight, 1 << 8);
        foreach (RaycastHit hit in hits)
            hit.transform.gameObject.GetComponent<MeshRenderer>().enabled = true;
    }
	public static Vector3Int vpos2epos (GameObject rmc){
		Vector3Int to_return = new Vector3Int();
        Vector3 dim = getTileDims(rmc);
        to_return.x = (int)((rmc.transform.position.x - 2 - 2 * (dim.x - 1)) / 4f);
        to_return.z = Mathf.RoundToInt((rmc.transform.position.z - 2 - 2 * (dim.z - 1)) / 4f);		
		return to_return;
	}
	public static void DelLastPrefab(){
		//Usuwam podgląd
		if (obj_rmc != null) {
            Unhide_trawkas(obj_rmc.transform.position);
            Vector3Int pos = vpos2epos(obj_rmc);
            List <GameObject> surroundings = get_surrounding_tiles(obj_rmc);
            DestroyImmediate (obj_rmc);
            Przywroc_teren(STATIC.tiles[pos.x, pos.z].t_verts);
            UpdateTiles(surroundings);
        }
	}
    public static List<int> GetRmcIndices(GameObject rmc)
    {
        List<int> to_return = new List<int>();
        Vector3Int LD = GetLDpos(rmc);
        Vector3Int tileDims = getTileDims(obj_rmc);
        for(int z=0; z<=4*tileDims.z; z++)
        {
            for(int x=0; x<=4*tileDims.x; x++)
            {
                to_return.Add(LD.x + x + 4 * SliderWidth.val * (LD.z + z) + LD.z + z);
            }
        }
        return to_return;
    }
	/// <summary>
	/// Zwraca tablicę GOs stykających się z obiektem o współrzędnych VERTEXOWYCH LDx, LDz
    /// Można też dać Mapę. Wtedy trzeba również dać tablicę znaczników.
	/// </summary>
	public static List<GameObject> get_surrounding_tiles(GameObject rmc_o, List<GameObject> znaczniki = null){
        if (rmc_o != null)
        {
            rmc_o.layer = 10;
            
            List<GameObject> to_return = new List<GameObject>();
            RaycastHit[] hits = Physics.BoxCastAll(rmc_o.transform.position, rmc_o.GetComponent<MeshFilter>().mesh.bounds.size * 0.55f, Vector3.down, Quaternion.identity, Terenowanie.rayHeight, 1 << 9);
            foreach (RaycastHit hit in hits)
                if(hit.transform.gameObject != rmc_o)
                    to_return.Add(hit.transform.gameObject);
            rmc_o.layer = 9;
            return to_return;
        }
        else // Znajdź elementy na mapie
        {
            List<GameObject> to_return = new List<GameObject>();
            foreach(GameObject znacznik in znaczniki)
            {
                Vector3 pos = znacznik.transform.position;
                pos.y = Terenowanie.maxHeight+1;
                RaycastHit[] hits = Physics.SphereCastAll(pos, 0.1f, Vector3.down, Terenowanie.rayHeight, 1 << 9);
                foreach (RaycastHit hit in hits)
                    if (!to_return.Contains(hit.transform.gameObject))
                        to_return.Add(hit.transform.gameObject);
            }
            return to_return;
        }
	}
    /// <summary>
    /// Zwraca vertexowe LD
    /// </summary>
	public static Vector3Int GetLDpos(GameObject rmc_o){
		Vector3Int to_return = new Vector3Int();
		Vector3 el_pos = rmc_o.transform.position;
        Vector3 dim = getTileDims(rmc_o);
        to_return.x = Mathf.RoundToInt(el_pos.x - 2 - 2 * (dim.x - 1));
        to_return.z = Mathf.RoundToInt(el_pos.z - 2 - 2 * (dim.z - 1));

        if (to_return.z % 4 != 0 || to_return.z % 4 != 0)
        {
            Debug.LogError("Źle ustawiony LD=" + to_return);
        }
        return to_return;
	}
    public static GameObject FindChildWhoseTagContains(GameObject parent, string tag)
    {
        Transform t = parent.transform;

        for (int i = 0; i < t.childCount; i++)
        {
            if (t.GetChild(i).gameObject.tag.Contains(tag))
            {
                return t.GetChild(i).gameObject;
            }

        }

        return null;
    }
    /// <summary>
    /// Tylko rmc. Zwraca wymiary elementu biorąc pod uwagę jego rotację 
    /// </summary>
	public static Vector3Int getTileDims(GameObject rmc_o) {
        GameObject prefab = FindChildWhoseTagContains(rmc_o, "x");
        Vector3Int to_return = new Vector3Int (int.Parse (prefab.tag.Substring (0, 1)), 0, int.Parse (prefab.tag.Substring (2, 1)));
        if (Mathf.RoundToInt(rmc_o.transform.rotation.eulerAngles.y) == 90 || Mathf.RoundToInt(rmc_o.transform.rotation.eulerAngles.y) == 270)
        {
            int pom = to_return.x;
            to_return.x = to_return.z;
            to_return.z = pom;
        }
        return to_return;
    }

	//Indexy vertexów terenu które zostały zmienione
	static void Save_tile_properties(string nazwa, bool inwersja, int rotacja, Vector3Int p, byte kategoria){
        STATIC.tiles [p.x, p.z]._inwersja = inwersja;
		STATIC.tiles [p.x, p.z]._nazwa = nazwa;
        STATIC.tiles [p.x, p.z]._rotacja = rotacja;
        STATIC.tiles[p.x, p.z]._kategoria = kategoria;
        if (nazwa.Contains("chk") && !STATIC.TRACK.Checkpoints.Contains((ushort)(p.x + (SliderHeight.val - 1 - p.z) * SliderWidth.val )))
        {
            STATIC.TRACK.Checkpoints.Add((ushort)(p.x + (SliderHeight.val - 1 - p.z) * SliderWidth.val));
            STATIC.TRACK.CheckpointsNumber++;
        }
        
    }
    /// <summary>
    /// Przywróć teren sprzed 'dorównywania'. Element, którego teren przywracamy musi być już usunięty!
    /// </summary>
    static void Przywroc_teren(List<int> indexes){
        if (indexes == null || indexes.Count == 0)
            return;
        RaycastHit hit;
        for (int i = 0; i < indexes.Count; i++)
        {
            Vector3 v = Helper.IndexToPos(indexes[i]);
            v.y = Terenowanie.maxHeight+1;
            bool traf = Physics.SphereCast(v, 0.005f, Vector3.down, out hit, Terenowanie.rayHeight, 1<<9);
            if (traf)
            {
                Helper.former_heights[indexes[i]] = hit.point.y;
                //Debug.DrawLine(v, new Vector3(v.x, -5, v.z), Color.green, 5);
            }
            else
            {
                //Debug.DrawLine(v, new Vector3(v.x, -5, v.z), Color.yellow, 5);
            }
                
        }
        Terenowanie.UpdateMapColliders(indexes, true);
    }

	public static GameObject znajdzRMC(float wymx, float wymz, string nazwa){
        for(int i=0; i<EditorMenu.customs.Count; i++)
        {   //Przeszukaj w customowych
            if(EditorMenu.customs[i].nazwa == nazwa)
                return Resources.Load("rmcs/" + EditorMenu.customs[i].nazwa_rmc) as GameObject;
        }
        //Jak nie ma to zwróć domyślny
        return Resources.Load("rmcs/default_" + wymx.ToString() + "x" + wymz.ToString()) as GameObject;
	}
    /// <summary>
    /// Pozycja środka tilesa. Realne wymiary tilesa. Sprawdza czy element nie wystaje poza granice mapy
    /// </summary>
	static bool CzyWolne(Vector3Int pos, Vector3Int tileDims){
        pos.y = Terenowanie.maxHeight+1;
        if (pos.z <= 0 || pos.z >= 4 * SliderHeight.val || pos.x <= 0 || pos.x >= 4 * SliderWidth.val)
            return false;
        RaycastHit[] hits = Physics.BoxCastAll(pos, new Vector3(4*tileDims.x*0.4f, 1, 4*tileDims.z*0.4f), Vector3.down, Quaternion.identity, Terenowanie.rayHeight, 1 << 9);
        return (hits.Length == 0) ? true : false;
    }

	static bool sprawdz_pozycje(int offsetx, int offsetz){
		RaycastHit hit;
		Vector3 v = new Vector3 (Highlight.t.x + 2f + 4*offsetx, Terenowanie.maxHeight+1, Highlight.t.z + 2f + 4*offsetz);
        //Vector3 x = new Vector3(v.x, -5, v.z);
        //Debug.DrawLine(v, x, Color.yellow, 500);
		bool traf = Physics.Raycast (v, Vector3.down, out hit, Terenowanie.rayHeight, 1<<9 | 1<<8);
        return (traf && hit.transform.gameObject.layer == 8) ? true : false;
	}

	//Przeszukuje tablicę typu 'Pzero'
	public static float getPzero(string nazwa){
		//Debug.Log (nazwa);
		for (int i = 0; i < EditorMenu.pzeros.Count; i++) {
			if (EditorMenu.pzeros [i].nazwa == nazwa) {
				//Debug.Log (nazwa + " " + EditorMenu.pzeros [i].pos);
				return EditorMenu.pzeros [i].pos;
			}
		}
		return -1;
	}

    public static string GetRMCname(GameObject rmc)
    {
         return FindChildWhoseTagContains(rmc,"x").name.Substring(0, FindChildWhoseTagContains(rmc, "x").name.IndexOf('('));
    }

	/// <summary>
    /// Ponownie kładzie elementy. Jeżeli elementy są stawiane na płaszczyźnie pnącej się w dół up = false
    /// </summary>
	public static void UpdateTiles(List<GameObject> rmcs, List<GameObject> znaczniki = null)
    {
        //1 faza. Ustawienie rmców
        foreach (GameObject rmc_o in rmcs)
        {
            rmc_o.layer = 9;
            Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
            Mesh rmc_mc = rmc_o.GetComponent<MeshCollider>().sharedMesh;
            //Aktualizuj RMC
            Vector3[] verts = rmc.vertices;
            for (int index = 0; index < rmc.vertices.Length; index++)
            {
                Vector3Int v = Vector3Int.RoundToInt(rmc_o.transform.TransformPoint(rmc.vertices[index]));
                verts[index].y = Helper.current_heights[v.x + 4 * v.z * SliderWidth.val + v.z];
            }
            rmc.vertices = verts;
            rmc.RecalculateBounds();
            rmc.RecalculateNormals();
            rmc_mc = null;
            rmc_mc = rmc;
            rmc_o.SetActive(false);
            rmc_o.SetActive(true);
        }
        //2.Dorównanie czułych vertexów rmctów jeżeli pod lub nad jest inny rmc.
        foreach (GameObject rmc_o in rmcs)
        {
            rmc_o.layer = 10;
            Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
            Mesh rmc_mc = rmc_o.GetComponent<MeshCollider>().sharedMesh;

            //Dorównaj RMC i od razu przypisz tablicę current_heights
            dorownaj_rmc2rmc(rmc_o);
            Vector3Int pos = vpos2epos(rmc_o);
            bool inwersja = STATIC.tiles[pos.x, pos.z]._inwersja;
            int rotacja = STATIC.tiles[pos.x, pos.z]._rotacja;
            GameObject prefab = FindChildWhoseTagContains(rmc_o, "x");
            //pobierz oryginalne wymiary
            Vector3Int tileDims = getTileDims(rmc_o);
            //usunięcie starego prefaba i zastąpienie go płaskim nowym. Prefab i dm są w kolejności
            Vector3 old_pos = prefab.transform.position;
            string prefab_name = prefab.name.Substring(0, prefab.name.IndexOf('('));
            DestroyImmediate(prefab);
            prefab = Instantiate(GetPrefab(prefab_name) as GameObject, old_pos, Quaternion.Euler(0, 180 + rotacja, 0), rmc_o.transform);
            //prefab.transform.rotation = rotate_q;
            prefab.transform.localScale *= 0.2f;
            bool anythingchanged = false;
            Vector3Int LDpos = GetLDpos(rmc_o);
            //Debug.Log("LDpos po =" + LDpos.x + " " + LDpos.z);
            for (int z = 0; z <= 4 * tileDims.z; z++)
            {
                for (int x = 0; x <= 4 * tileDims.x; x++)
                {
                    if (x != 0 && z != 0 && x != 4 * tileDims.x && z != 4 * tileDims.z)
                    {
                        schowaj_8(x, z, LDpos, ref anythingchanged); // środek
                    }
                    else
                    {
                        dorownaj_obrzeza(x, z, LDpos, ref anythingchanged); //obrzeża
                    }
                }
            }
            Terenowanie.UpdateMapColliders(rmc_o.transform.position, tileDims);
            List<Mesh> meshes = GetPrefabMeshList(inwersja, prefab);
            Tiles_to_RMC_Cast(prefab_name, ref meshes, ref prefab, inwersja);
            rmc_o.layer = 9;
        }
    }

    public static void InverseMesh(Mesh mesh)
    {
        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
            verts[i] = new Vector3(-verts[i].x, verts[i].y, verts[i].z);
        mesh.vertices = verts;

        for(int i=0; i< mesh.subMeshCount; i++) // Każdemu materiałowi trzeba przypisać tablicę trójkątów
        {
            int [] trgs = mesh.GetTriangles(i);
            mesh.SetTriangles(trgs.Reverse().ToArray(), i);
        }

    }
    /// <summary>
    /// Można wywołać tylko w edytorze. Inaczej gameobjecty w tabeli będą usunięte i w tabeli będą nule
    /// </summary>
    /// <param name="nazwa_tilesa"></param>
    /// <returns></returns>
    public static GameObject GetPrefab(string nazwa_tilesa)
    {
        //for (int i = 0; i < praobiekty.Count; i++)
        //    if (praobiekty[i].name == nazwa_tilesa)
        //        return praobiekty[i];

        //praobiekty.Add(Resources.Load("prefabs/" + nazwa_tilesa) as GameObject);
        //return praobiekty[praobiekty.Count - 1];
        return Resources.Load("prefabs/" + nazwa_tilesa) as GameObject;
    }


    public static byte GetTileCategory(string nazwa_tilesa)
    {
        for(int i=0; i<EditorMenu.kategorie.Count; i++)
        {
            if (EditorMenu.kategorie[i]._nazwa_tilesa == nazwa_tilesa)
                return EditorMenu.kategorie[i]._nr_kat;
        }
        return 1; // Zwróć kategorię town roads jeżeli tego elementu nie ma w pliku
    }
    public GameObject PlacePrefab(Vector3Int LDpos, string nazwa_tilesa, int cum_rotation, bool inwersja = false){
        AllowLMB = false;
        if (Input.GetKey (KeyCode.X))
			return null;
        GameObject prefab_PRE = GetPrefab(nazwa_tilesa);
        if (prefab_PRE == null)
            return null;
		Quaternion rotate_q = Quaternion.Euler (new Vector3 (0, 180+cum_rotation, 0));
        //Pobierz oryginalne wymiary
		Vector3Int tileDims = new Vector3Int (int.Parse(prefab_PRE.tag.Substring(0,1)), 0, int.Parse(prefab_PRE.tag.Substring(2,1))); //wymiary bieżącego tilesa [vertexy]
        GameObject rmc_PRE = znajdzRMC(tileDims.x, tileDims.z, nazwa_tilesa);
        //Ustanów wymiary realnymi
        if (cum_rotation == 90 || cum_rotation == 270)
        {
            int pom = tileDims.x;
            tileDims.x = tileDims.z;
            tileDims.z = pom;
        }
        Vector3Int rmcPlacement = new Vector3Int(LDpos.x + 2 + 2 * (tileDims.x - 1), 0, LDpos.z + 2 + 2 * (tileDims.z - 1));

        if (!STATIC.isloading)
        {
            if (!CzyWolne(rmcPlacement, tileDims))
            {
                obj_rmc = null;
                return null;
            }
        }
        AllowLMB = true;
        //______________________
        //POŁÓŻ RMC NA TRAWCE
        //----------------------
        obj_rmc = GameObject.Instantiate(rmc_PRE, rmcPlacement, rotate_q);
        if (inwersja)
            InverseMesh(obj_rmc.GetComponent<MeshFilter>().mesh);

        if (!STATIC.isloading)
            obj_rmc.name = "RMC";
        else
            obj_rmc.name = STATIC.tiles[LDpos.x / 4, LDpos.z / 4]._kategoria.ToString();

        Mesh rmc = obj_rmc.GetComponent<MeshFilter> ().mesh;
		obj_rmc.layer = 10;

		rmc.MarkDynamic ();

		Vector3[] verts = rmc.vertices;
		//RMC dostaje mesh collider (dla tilesa)
		MeshCollider rmc_mc = obj_rmc.AddComponent<MeshCollider> ();

		//Dopasuj RMC
		for(int index=0; index<rmc.vertices.Length; index++) {
			Vector3Int v = Vector3Int.RoundToInt(obj_rmc.transform.TransformPoint(rmc.vertices[index]));
			verts [index].y = Helper.current_heights[v.x + 4*v.z*SliderWidth.val+v.z];
		}

		//dopasowanie rmc do terenu
		rmc.vertices = verts;
		rmc.RecalculateBounds ();
		rmc.RecalculateNormals ();
		rmc_mc.sharedMesh = null;
		rmc_mc.sharedMesh = rmc;
		obj_rmc.GetComponent<MeshRenderer> ().enabled = false;
        if(!STATIC.isloading)
        {
            List<GameObject> rmcsToUpdate = new List<GameObject>();
            bool anythingchanged = false;
            //_______________________
            //Dorównaj sąsiadujące trawki do rmc
            //---------------------------
            for (int z = 0; z <= 4 * tileDims.z; z++)
            {
                for (int x = 0; x <= 4 * tileDims.x; x++)
                {
                    if (z != 0 && z != 4 * tileDims.z && x != 0 && x != 4 * tileDims.x)
                    {
                        schowaj_8(x, z, LDpos, ref anythingchanged); // środek
                    }
                    else
                    {
                      rmcsToUpdate = dorownaj_obrzeza(x, z, LDpos, ref anythingchanged, rmcsToUpdate); //obrzeża
                    }
                }
            }
            if (anythingchanged)
            {
                Terenowanie.UpdateMapColliders(obj_rmc.transform.position, tileDims);
            }
                
            if (rmcsToUpdate != null)
            {
                UpdateTiles(rmcsToUpdate);
            }
        }
        //_________________________
        //POSTAW TILESA
        //-------------------------
        prefab = GameObject.Instantiate<GameObject> (prefab_PRE,obj_rmc.transform);
        prefab.transform.rotation = Quaternion.Euler(new Vector3(0, 180+cum_rotation, 0));
		prefab.transform.localScale *= 0.2f;
        prefab.transform.position = rmcPlacement;
        

        List<Mesh> meshes = GetPrefabMeshList(inwersja, prefab);
        Tiles_to_RMC_Cast(prefab_PRE.name, ref meshes, ref prefab, inwersja);

        obj_rmc.layer = 9;
        STATIC.tiles[LDpos.x / 4, LDpos.z / 4].t_verts = GetRmcIndices(obj_rmc);

        if (STATIC.isloading)
        {
            Save_tile_properties(nazwa_tilesa, inwersja, cum_rotation, new Vector3Int(LDpos.x/4, 0, LDpos.z/4), GetTileCategory(nazwa_tilesa));
            return obj_rmc;
        }
            
        else
            return null;


        //Do tworzenia płaszczaków
        //prefab.layer = 12;
        //prefab.AddComponent<MeshCollider>();
        //prefab.GetComponent<MeshCollider>().sharedMesh = null;
        //if(prefab.transform.childCount != 0)
        //    prefab.GetComponent<MeshCollider>().sharedMesh = prefab.transform.Find("main").GetComponent<MeshFilter>().mesh;
        //else
        //    prefab.GetComponent<MeshCollider>().sharedMesh = prefab.GetComponent<MeshFilter>().mesh;
    }

    /// <summary>
    /// Lista meshów. \-/ dziecka prefaba zaznacz MarkDynamic(), zainwersjuj, dodaj do listy. Jeśli nie ma dzieci, to prefab dodaj do listy meshów
    /// </summary>
    static List<Mesh> GetPrefabMeshList(bool inwersja, GameObject prefab)
    {
        List<Mesh> meshes = new List<Mesh>();
        if (prefab.transform.childCount != 0)
        {
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                if (prefab.transform.GetChild(i).tag != "krzaczor" && prefab.transform.GetChild(i).GetComponent<MeshRenderer>().enabled)
                {
                    prefab.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh.MarkDynamic();
                    if (inwersja)
                        InverseMesh(prefab.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh);
                    meshes.Add(prefab.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh);
                }
            }
        }
        else
        {
            prefab.GetComponent<MeshFilter>().mesh.MarkDynamic();
            if (inwersja)
                InverseMesh(prefab.GetComponent<MeshFilter>().mesh);
            meshes.Add(prefab.GetComponent<MeshFilter>().mesh);
        }
        return meshes;
    }

    /// <summary>
    /// getPzero dla nazwa_tilesa, \-/ z meshes ray na 10. Jeśli nie trafił to na 8, jak nie trafił to podstawową wysokością jest Bounding height
    /// </summary>
    static void Tiles_to_RMC_Cast(string nazwa_tilesa, ref List<Mesh> meshes, ref GameObject prefab, bool inwersja)
    {
        float pzero = getPzero(nazwa_tilesa);
        // Raycast tiles(H) \ rmc
        foreach (Mesh mesh in meshes)
        {
            RaycastHit hit;
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 v = prefab.transform.TransformPoint(mesh.vertices[i]);
                if(Physics.Raycast(new Vector3(v.x, Terenowanie.maxHeight + 1, v.z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 10))
                { // own rmc
                    verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
                }
                else
                if (Physics.SphereCast(new Vector3(v.x, Terenowanie.maxHeight + 1, v.z), 0.005f, Vector3.down, out hit, Terenowanie.rayHeight, 1 << 10))
                { // due to the fact rotation in unity is stored in quaternions using floats you won't always hit mesh collider with one-dimensional raycasts. 
                    verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
                }
                else
                { // Poza własnym rmc i obcym rmc, na mapę.
                    if (Physics.SphereCast(new Vector3(v.x, Terenowanie.minHeight - 1, v.z), 0.2f, Vector3.up, out hit, Terenowanie.rayHeight, 1 << 9 | 1 << 8))
                        verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, hit.point.y + v.y - pzero, v.z));
                    else // Poza mapą, wysokość obrzeża
                        verts[i] = prefab.transform.InverseTransformPoint(new Vector3(v.x, Helper.current_heights[0] + v.y - pzero, v.z));
                }
            }
            mesh.vertices = verts;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
        UpdateKrzaczory(ref prefab, inwersja);
    }

    /// <summary>
    /// Dorównuje pozycję krzaczora do najniższego vertexa jego pnia.
    /// Jeżeli prefab.name należy do grupy krzaczorów, \-/ elementu z tagiem krzaczor (kolejność x od L do P; y od dołu do góry globalnie), ustawia mu wysokość
    /// vertexa mesha main z predefiniowanej listy intów EditorMenu.Krzaczory
    /// </summary>
    public static void UpdateKrzaczory(ref GameObject prefab, bool inwersja)
    {
        RaycastHit hit;
        for(int i=0; i<EditorMenu.krzaczory.Length; i++)
        {
            if(prefab.name.Substring(0, prefab.name.IndexOf('(')) == EditorMenu.krzaczory[i]) //Ten element ma krzaczory
            {
                int index_krzaczora = 0;
                for (int j = 0; j < prefab.transform.childCount; j++)
                {
                    if (prefab.transform.GetChild(j).tag == "krzaczor")
                    {
                        Vector3 v = prefab.transform.GetChild(j).localPosition;
                        v.x = (inwersja) ? - v.x : v.x;
                        v = prefab.transform.TransformPoint(v);
                        v.y = Terenowanie.maxHeight+1;
                        Physics.Raycast(v, Vector3.down, out hit, Terenowanie.rayHeight, 1 << 10);
                        prefab.transform.GetChild(j).position = new Vector3(v.x, hit.point.y, v.z);
                        index_krzaczora++;
                    }
                }
                return; //Misja wykonana. Krzaczory na prawidłowych pozycjach
            }
        }
        // Ten element nie ma krzaczorów
    }
    /// <summary>
    /// Zwraca mesh main prefaba lub mesh jego meshfiltera
    /// </summary>
    public static Mesh GetMainMesh(ref GameObject prefab)
    {
        if (prefab.transform.childCount != 0)
        {
            for (int i = 0; i < prefab.transform.childCount; i++)
                if (prefab.transform.GetChild(i).name == "main")
                    return prefab.transform.GetChild(i).GetComponent<MeshFilter>().mesh;

            return null; // <-- Tutaj raczej nigdy nie dojdzie
        }
        else
            return prefab.GetComponent<MeshFilter>().mesh;
    }
    /// <summary>
    /// Wyłącza widoczność trawki leżącej pod elementem. Uaktualnia tabelę current_heights. Layer musi być 10.
    /// </summary>
    public static void schowaj_8(int x, int z, Vector3Int LDpos, ref bool anythingchanged){
        RaycastHit hit;
		x += LDpos.x;
		z += LDpos.z; //Mamy x,y są teraz globalne
		int index = x + 4 * z * SliderWidth.val + z;
        Vector3 v = new Vector3 (x, Terenowanie.minHeight-1, z);
        if(Physics.Raycast(v, Vector3.up, out hit, Terenowanie.rayHeight, 1 << 10) && Mathf.Abs(Helper.current_heights[index] - hit.point.y) > 0.01f)
        {
            Helper.current_heights[index] = hit.point.y;
            anythingchanged = true;
        }
            
        RaycastHit[] hits = Physics.SphereCastAll(v, 0.01f, Vector3.up, Terenowanie.rayHeight, 1 << 8);
        foreach (RaycastHit h in hits)
            h.transform.gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
    /// <summary>
    /// Ustawia wartość wysokości terenu w tym miejscu do wysokości vertexa obiektu layer=10
    /// </summary>
    public static List<GameObject> dorownaj_obrzeza(int x, int z, Vector3Int LDpos, ref bool anythingchanged, List<GameObject> toUpdate = null) {
        RaycastHit hit;
        x += LDpos.x;
        z += LDpos.z;
        Vector3Int v = new Vector3Int(x, Terenowanie.maxHeight+1, z);
        int index = (x + 4 * z * SliderWidth.val + z);
        if (Physics.SphereCast(v, 0.005f, Vector3.down, out hit, Terenowanie.rayHeight, 1 << 10) && Mathf.Abs(Helper.current_heights[index] - hit.point.y) > 0.1f)
        {
            anythingchanged = true;
            Helper.current_heights[index] = hit.point.y;
            
        }
        if(toUpdate != null)
        {
            RaycastHit[] hits_to_update = Physics.SphereCastAll(v, 0.1f, Vector3.down, Terenowanie.rayHeight, 1 << 9);
            foreach(RaycastHit h in hits_to_update)
            {
                if (!toUpdate.Contains(h.transform.gameObject))
                    toUpdate.Add(h.transform.gameObject);
            }
            return toUpdate;
        }
        return null;
	}
    /// <summary>
    /// Dorównanie na wspólnych ścianach rmctów do siebie
    /// </summary>
    public static void dorownaj_rmc2rmc(GameObject rmc_o)
    {
        Mesh rmc_mc = rmc_o.GetComponent<MeshCollider>().sharedMesh;
        RaycastHit hit, sgnHit;
        Mesh rmc = rmc_o.GetComponent<MeshFilter>().mesh;
        Vector3[] verts = rmc.vertices;
        for (int index = 0; index < verts.Length; index++)
        {
            Vector3Int v = Vector3Int.RoundToInt(rmc_o.transform.TransformPoint(rmc.vertices[index]));
            if(Physics.Raycast(new Vector3(v.x, Terenowanie.maxHeight + 1, v.z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 9))
            {
                //Mamy znaczniki i tutaj jest punkt zmienionej wysokości (czerwony kwadracik)
                if (Physics.SphereCast(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), 0.005f, Vector3.down, out sgnHit, Terenowanie.rayHeight, 1 << 11) && sgnHit.transform.name == "on")
                {
                    //Sprawdź czy rmc layer=9 ma tutaj vertexa.
                    {
                        bool rmc9matuvertexa = false;
                        foreach (Vector3 vo in hit.transform.gameObject.GetComponent<MeshFilter>().mesh.vertices)
                        {
                            Vector3Int vert = Vector3Int.RoundToInt(hit.transform.gameObject.transform.TransformPoint(vo));
                            if (vert.x + 4 * vert.z * SliderWidth.val + vert.z == v.x + 4 * v.z * SliderWidth.val + v.z)
                            {
                                rmc9matuvertexa = true;
                                break;
                            }
                        }
                        if (rmc9matuvertexa)
                        {
                            //Debug.DrawLine(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.red, Terenowanie.rayHeight);
                            verts[index].y = Helper.current_heights[v.x + 4 * v.z * SliderWidth.val + v.z];
                        } else
                        {
                            //Debug.DrawLine(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.gray, Terenowanie.rayHeight);
                            verts[index].y = hit.point.y;
                            //Helper.current_heights[v.x + 4 * v.z * SliderWidth.val + v.z] = hit.point.y;
                        }
                    }

                }
                else // Normalne dorównanko
                {
                    verts[index].y = hit.point.y;
                    //Helper.current_heights[v.x + 4 * v.z * SliderWidth.val + v.z] = hit.point.y;
                }

            }
        }
        rmc.vertices = verts;
        rmc.RecalculateBounds();
        rmc.RecalculateNormals();
        rmc_mc = null;
        rmc_mc = rmc_o.GetComponent<MeshFilter>().mesh;
        rmc_o.SetActive(false);
        rmc_o.SetActive(true);
    }

    //	public static void SaveMesh (Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
    //		string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
    //		if (string.IsNullOrEmpty(path)) return;
    //
    //		path = FileUtil.GetProjectRelativePath(path);
    //
    //		Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
    //
    //		if (optimizeMesh)
    //			MeshUtility.Optimize(meshToSave);
    //
    //		AssetDatabase.CreateAsset(meshToSave, path);
    //		AssetDatabase.SaveAssets();
    //	}
    //
}


