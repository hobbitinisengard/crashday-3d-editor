using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Terenowanie : MonoBehaviour {
	public GameObject FormMenu;
    public GameObject savePanel;
    public Text state_help_text;
	public Slider slider;
    public Material transp;
	public Material red;
	public Button toslider; //ustawia zaznaczone vertexy do wartości slidera
	public Button Flatten;
    public Button Jumper; // Łagodne doprowadzenie do prostrego
	public Button Prostry; // kształt /
    public Button JumperEnd; //Łagodne zakończenie prostrego
	public Button Integral; // kształt całki
    public Toggle KeepShape; //Tryb kształtów
    public Text HelperInputField; // Pomocniczy tekst do wpisywania konkretnej wartości
    /// <summary>
    /// przechowuje bieżące wysokości wszystkich wertexów. Podczas stawiania tilesów trawki są dorównywane, co zaburza teren. Gdy tiles jest usuwany, to wysokość danego vertexa jest przywracana z tej tablicy
    /// </summary>
	public static GameObject indicator;
	public static List<GameObject> znaczniki = new List<GameObject> ();
	public static List<GameObject> surroundings = new List<GameObject> ();
    public static int minHeight;
    public static int maxHeight;
    public static int rayHeight;
	int index = 0; //Indexy do meshow dla vertexa
    int max_verts_visible_dim = 60; // Maksymalna liczba widocznych znaczników vertexów terenu w trybie 2.
    float slider_realheight;
	GameObject current; // Ostatnio zaznaczony element

    public static bool firstFormingMode = true; //  Klawiszem F przestawiamy jej wartość logiczną => sterujemy trybem ustawiania
    public static bool istilemanip = false;
	public static bool isSelecting = false; //Tryb zaznaczania vertexów w trybie manipulacji tilesa
    bool waiting4LD = false; //Po kliknięciu kształtu, czekamy na 1 klik - zaznaczenie LewegoDolnego rogu
    bool waiting4LDpassed = false; // Przypisywana w stanie waiting4LD. Jesteśmy po 1ym kliku
    bool is_entering_keypad_value = false; // Przypisywana podczas pomocniczego wpisywania wysokości na klawiaturze - numericenter();
    int menucontrol = 1;
	string last_form_button;
	Vector3Int LD;
    float LDH; // wartość tylko dla wysokości LD.  Żeby nie mieszać intów z floatami
	Vector3 mousePosition1;
    RaycastHit hit;
    string buffer;
    void Awake(){
        toslider.onClick.AddListener(() => { if (KeepShape.isOn)
                                                last_form_button = "to_slider";
                                             else
                                                FormMenu_toSlider();
                                            });
		Prostry.onClick.AddListener (() => last_form_button = "prostry");
        Integral.onClick.AddListener(() => last_form_button = "integral");
        Jumper.onClick.AddListener(() => last_form_button = "jumper");
        JumperEnd.onClick.AddListener(() => last_form_button = "jumperend");
        Flatten.onClick.AddListener(() => last_form_button = "flatter");
        state_help_text.text = "Manual forming..";
	}

    void Update () {
        if (!savePanel.activeSelf)
        {
            Numericenter();
            mousewheelcheck();
            SetFormingMode();
            ctrl_key_works();
            istilemanip_state(); //dokonujemy zaznaczenia i (PPM anuluje zaznaczenie)
            waiting4LD_state(); //  mamy LD
            selectShape();
            menucontrol = control();
            //Poziomowanie vertexa. Obsługa nakładających się vertexów
            if (menucontrol == 1)
            {
                //Caps ON
                if (!Input.GetKey(KeyCode.LeftControl)) //jeżeli nie było ctrl_key_works()
                {
                    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0) && index == 0)
                        single_vertex_manipulation(); // single-action :)
                    else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0) && index == 0)
                        single_vertex_manipulation(); //auto-fire >:)
                    else if (Input.GetMouseButtonDown(1) && Highlight.nad)
                        make_elevation();
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {//Klawisz ESC wyłącza aktywność make_elevation
                        index = 0;
                        if (indicator != null)
                            Destroy(indicator);
                    }
                }
            }
            else if (menucontrol == 2)
            {
                //Caps OFF - form Menu
                if (indicator != null)
                {
                    Destroy(indicator);
                    index = 0;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    czule_vertexy(Input.GetKey(KeyCode.Q));
                }
            }
        }
		
	}
    private void Hide_text_helper()
    {
        slider.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
        HelperInputField.text = "";
        is_entering_keypad_value = false;
    }
    private void Numericenter()
    {
        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            try
            {
                slider.value = float.Parse(HelperInputField.text);
                Hide_text_helper();
            }
            catch { }
        }
        KeyCode[] keyCodes = {KeyCode.Keypad0,KeyCode.Keypad1,KeyCode.Keypad2,KeyCode.Keypad3,KeyCode.Keypad4,KeyCode.Keypad5,KeyCode.Keypad6,KeyCode.Keypad7,KeyCode.Keypad8,KeyCode.Keypad9, KeyCode.KeypadPeriod, KeyCode.KeypadMinus};
        for(int i = 0; i<keyCodes.Length; i++){
            if(Input.GetKeyDown(keyCodes[i])){
                slider.transform.GetChild(1).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                if (i < 10)
                    HelperInputField.text += i.ToString();
                else if (i == 10)
                    HelperInputField.text += ",";
                else if (i == 11)
                    HelperInputField.text += "-";
                is_entering_keypad_value = true;
                return;   
            }
        }
    }

    /// <summary>
    /// a) Jeżeli lista MapColliderów => \-/ MapCollidera \-/ vertexa tegoż collidera uaktualnij pozycję (z former_heights) i zaaplikuj.
    /// b) Jeżeli lista znaczników => Lista indexów, \-/ znacznika zapisz jego pozycję do listy, Wywołaj przeładowanie dla intów.
    /// NewUpdate == Saveto Former
    /// </summary>
    public static void UpdateMapColliders(List <GameObject> mcs, bool przywrocenie_terenu = false)
    {
        if (mcs[0].layer == 11) // Argumentami znaczniki
        {
            List<int> indexes = new List<int>();
            foreach(GameObject znacznik in mcs)
            {
                Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
                indexes.Add(v.x + 4 * v.z * SliderWidth.val + v.z);
            }
            UpdateMapColliders(indexes, przywrocenie_terenu);

        } else //Argumentami MapCollidery
        {
            foreach (GameObject mc in mcs)
            {
                Vector3[] verts = mc.GetComponent<MeshCollider>().sharedMesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3Int v = Vector3Int.RoundToInt(mc.transform.TransformPoint(verts[i]));
                    if (przywrocenie_terenu)
                    {    
                        verts[i].y = Helper.former_heights[v.x + 4 * v.z * SliderWidth.val + v.z];
                        Helper.current_heights[v.x + 4 * v.z * SliderWidth.val + v.z] = Helper.former_heights[v.x + 4 * v.z * SliderWidth.val + v.z];
                    }
                    else
                        verts[i].y = Helper.current_heights[v.x + 4 * v.z * SliderWidth.val + v.z];
                }
                mc.GetComponent<MeshCollider>().sharedMesh.vertices = verts;
                mc.GetComponent<MeshCollider>().sharedMesh.RecalculateBounds();
                mc.GetComponent<MeshCollider>().sharedMesh.RecalculateNormals();
                mc.GetComponent<MeshFilter>().mesh = mc.GetComponent<MeshCollider>().sharedMesh;
                mc.GetComponent<MeshCollider>().enabled = false;
                mc.GetComponent<MeshCollider>().enabled = true;
            }
        }
    }
    /// <summary>
    /// Lista mapcolliderów, \-/ pozycji indexu rzuć raycast L=8, jeśli hita nie ma na liście, dodaj go. Wywołaj przeładowanie dla Gameobjectów.
    /// </summary>
    public static void UpdateMapColliders(List<int> indexes, bool przywrocenie_terenu = false)
    {
        List<GameObject> mcs = new List<GameObject>();
        foreach (int i in indexes)
        {
            Vector3Int v = Vector3Int.RoundToInt(Helper.IndexToPos(i));
            v.y = Terenowanie.maxHeight+1;
            RaycastHit[] hits = Physics.SphereCastAll(v,0.002f, Vector3.down, Terenowanie.rayHeight, 1 << 8);
            foreach (RaycastHit hit in hits)
                if (!mcs.Contains(hit.transform.gameObject))
                {
                    mcs.Add(hit.transform.gameObject);
                }
                   
        }
        UpdateMapColliders(mcs, przywrocenie_terenu);
    }
    public static void UpdateMapColliders(Vector3 rmc_pos, Vector3Int tileDims, bool przywrocenie_terenu = false)
    {
        rmc_pos.y = Terenowanie.maxHeight+1;
        RaycastHit[] hits = Physics.BoxCastAll(rmc_pos, new Vector3(4 * tileDims.x * 0.6f, 1, 4 * tileDims.z * 0.6f), Vector3.down, Quaternion.identity, Terenowanie.rayHeight, 1 << 8);
        List<GameObject> mcs = new List<GameObject>();
        foreach (RaycastHit hit in hits)
        {
            mcs.Add(hit.transform.gameObject);
        }
        UpdateMapColliders(mcs, przywrocenie_terenu);
    }

    float FindLowestY(List <GameObject> znaczniki)
    {
        float lowest = 40;
        foreach (GameObject znacznik in znaczniki)
            if (znacznik.name == "on" && lowest > znacznik.transform.position.y)
                lowest = znacznik.transform.position.y;
        return lowest;
    }
    float FindHighestY(List<GameObject> znaczniki)
    {
        float highest = -20;
        foreach (GameObject znacznik in znaczniki)
            if (znacznik.name == "on" && highest < znacznik.transform.position.y)
                highest = znacznik.transform.position.y;
        return highest;
    }
    void SetFormingMode()
    {
        if(!firstFormingMode && Input.GetKeyDown(KeyCode.F)) // Mamy tryb 2 a chcemy na 1
        {
            waiting4LD = false;
            isSelecting = false;
            istilemanip = false;
            del_znaczniki();
            state_help_text.text = "Manual forming..";
            firstFormingMode = true;
        } else if(firstFormingMode && Input.GetKeyDown(KeyCode.F)) // Mamy 1 a chcemy na 2
        {
            firstFormingMode = false;
            state_help_text.text = "Shape forming..";
        }
    }

    void istilemanip_state(){
        if (!waiting4LD)
        {
            if (istilemanip)
            {
                if (Input.GetMouseButtonDown(1))
                { // PPM wyłącza formMenu
                    istilemanip = false;
                    del_znaczniki();
                    state_help_text.text = "Shape forming..";
                }
                else
                    zaznacz_vertexy_tilesa();
            }
        }
	}

    void selectShape()
    {
        if (waiting4LDpassed)
        {
            state_help_text.text = "Shape forming..";
            if (last_form_button != "")
            {
                if (last_form_button == "to_slider")
                    FormMenu_toSlider();
                else
                    apply_fancy_shape();

                waiting4LDpassed = false;
                last_form_button = null;
                KeepShape.isOn = false;
            }
        }
        
    }

	void waiting4LD_state(){
		if (waiting4LD && !waiting4LDpassed) {
            state_help_text.text = "Waiting for bottom-left vertex..";
			foreach (GameObject znacznik in znaczniki) {
				znacznik.GetComponent<BoxCollider> ().enabled = true;
				znacznik.layer = 11;
            }

            if (Input.GetMouseButtonDown(0)) //Pierwszy klik
            {
                if (Physics.Raycast(new Vector3(Highlight.pos.x, 100, Highlight.pos.z), Vector3.down, out hit, Terenowanie.maxHeight+1, 1 << 11) && hit.transform.gameObject.name == "on")
                {
                    LD = Vector3Int.RoundToInt(hit.transform.position);
                    LDH = hit.transform.position.y;
                    waiting4LDpassed = true;
                    waiting4LD = false;

                }
            }
            else if (Input.GetMouseButtonDown(1)) // Anulowanie zaznaczenia
            {
                waiting4LD = false;
                istilemanip = true;
                last_form_button = null;
                state_help_text.text = "Marking vertices..";

            }
               
		}
	}
	void mousewheelcheck(){
        if (Input.GetAxis("Mouse ScrollWheel") != 0 && is_entering_keypad_value)
        {
            Hide_text_helper();
        }
		if (Input.GetKey (KeyCode.LeftShift)) {
			if (Input.GetAxis ("Mouse ScrollWheel") > 0 && slider.value < 1000) {
				slider.value += 10;
			} else if (Input.GetAxis ("Mouse ScrollWheel") < 0 && slider.value > -1000) {
				slider.value -= 10;
			}
		} else if (Input.GetKey(KeyCode.LeftAlt))
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < 1000)
            {
                slider.value += 0.25f;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > -1000)
            {
                slider.value -= 0.25f;
            }
        }
        else if (Input.GetAxis ("Mouse ScrollWheel") > 0 && slider.value < 1000) {
			slider.value += 1;
		} else if (Input.GetAxis ("Mouse ScrollWheel") < 0 && slider.value > -1000) {
			slider.value -= 1;
		}
        slider_realheight = SliderValue2RealHeight(slider.value);
	}
    //bool Check_keepShape_validity(float LDy, List <GameObject> znaczniki, float sliderheight)
    //{
    //    float riseby = sliderheight - LDy;
    //    float maxheight = -Terenowanie.rayHeight;
    //        foreach (GameObject znacznik in znaczniki)
    //        {
    //            if (maxheight < znacznik.transform.position.y)
    //                maxheight = znacznik.transform.position.y;
    //        }
    //    return (maxheight + riseby > 9.921875 || maxheight + riseby < -10) ? false : true;
    //}
    public static float SliderValue2RealHeight(float sliderval)
    {
        return sliderval/5f;//(maxHeight - minHeight) * (sliderval / 255f - 1);
    }
    public static float RealHeight2SliderValue(float realheight)
    {
        return 5*realheight;//(byte)Mathf.RoundToInt(255*(realheight/(maxHeight-minHeight) + 1));
    }

    void FormMenu_toSlider(){
        surroundings = Budowanie.get_surrounding_tiles(null, znaczniki);
        float elevateby = 0;
        float slider_realheight = SliderValue2RealHeight(slider.value);
        if (KeepShape.isOn)
            elevateby = slider_realheight - LDH;
		if (istilemanip) {
            //Aktualizuj teren
            List<int> indexes = new List<int>();
			foreach (GameObject znacznik in znaczniki) {
				if (znacznik.name == "on") {
					Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
					int index = v.x + 4 * v.z * SliderWidth.val + v.z;
                    indexes.Add(index);
                    if (KeepShape.isOn)
                        Helper.current_heights[index] += elevateby;
                    else
                        Helper.current_heights[index] = slider_realheight;
                    
                    znacznik.transform.position = new Vector3 (znacznik.transform.position.x, Helper.current_heights[index], znacznik.transform.position.z);
                    Helper.former_heights[index] = Helper.current_heights[index];
				}
			}
            UpdateMapColliders(indexes);

            if (current != null)
                surroundings.Add(current);
            Budowanie.UpdateTiles(surroundings);   
            surroundings.Clear();
        } else
			Debug.LogError ("istilemanip = false");

        KeepShape.isOn = false;
        last_form_button = "";
    }
	float pom(int ld, int pg, ref int x){
		return (ld < pg) ? x++ : x--;
	}
    bool pom2(int ld, int pg, int x)
    {
        return (ld < pg) ? x <= pg : x >= pg;
    }
    int isFlatter(string nazwa)
    {
        for(int i=0; i<EditorMenu.flatter.Count; i++)
        {
            if (nazwa == EditorMenu.flatter[i].nazwa)
                return i;
        }
        return -1;
    }
	void apply_fancy_shape(){
        //Flatter check
        int flatter_index = -1;
        
        if (last_form_button == "flatter")
        {
            if (current == null)
                return;
            flatter_index = isFlatter(Budowanie.GetRMCname(current));
            if (flatter_index == -1)
                return;
        }
        surroundings = Budowanie.get_surrounding_tiles(null, znaczniki);
        int index = 0;
		if (waiting4LDpassed) {
            //Mamy pozycję LD, szukamy PG
            Vector3Int PG = findPG(LD);
            //Debug.Log("PG=" + PG);
            //Aktualizuj teren
            float heightdiff = slider_realheight - LDH;
            if (KeepShape.isOn)
                heightdiff -= FindHighestY(znaczniki) - LDH;
            if ((LD.x < PG.x && LD.z > PG.z) || (LD.x > PG.x && LD.z < PG.z))
            { // równe powierzchnie po Z ||||
                float steps = Mathf.Abs(LD.x - PG.x);
                int step = 0;
                if(steps != 0 && (heightdiff != 0 || last_form_button == "flatter"))
                {
                    for (int x = LD.x; pom2(LD.x, PG.x, x); pom(LD.x, PG.x, ref x))
                    {
                        for (int z = LD.z; pom2(LD.z, PG.z, z); pom(LD.z, PG.z, ref z))
                        {
                            
                            bool traf = Physics.Raycast(new Vector3(x, Terenowanie.maxHeight+1, z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 11);
                            index = x + 4 * z * SliderWidth.val + z;
                            Vector3 vertpos = Helper.IndexToPos(index);
                            if (traf && hit.transform.gameObject.name == "on" && IsWithinMapBounds(vertpos))
                            {
                                float old_Y = vertpos.y; // tylko do keepshape
                                if (last_form_button == "prostry")
                                    vertpos.y = LDH + step / steps * heightdiff;
                                else if (last_form_button == "integral")
                                    vertpos.y = LDH + Smootherstep(LDH, slider_realheight, LDH + step / steps * heightdiff) * heightdiff;
                                else if (last_form_button == "jumper")
                                    vertpos.y = LDH + 2 * Smootherstep(LDH, slider_realheight, LDH + 0.5f * step / steps * heightdiff) * heightdiff;
                                else if (last_form_button == "jumperend")
                                    vertpos.y = LDH + 2 * (Smootherstep(LDH, slider_realheight, LDH + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
                                else if (last_form_button == "flatter")
                                    vertpos.y = LDH - EditorMenu.flatter[flatter_index].heights[step];
                                if(KeepShape.isOn)
                                    vertpos.y += old_Y - LDH;
                                Helper.former_heights[index] = vertpos.y;
                                Helper.current_heights[index] = Helper.former_heights[index];
                                GameObject znacznik = hit.transform.gameObject;
                                znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);

                            }

                        }
                        //Debug.Log(x + " "+ LDH +" "+ slider_realheight + " "+ LDH + step / steps * heightdiff + "HEIGHT="+ verts[index].y);
                        step += 1;
                    }
                }
                
            }
            else
            { // |.'| równe powierzchnie po X _-_-
                float steps = Mathf.Abs(LD.z - PG.z);
                //Debug.Log("steps = " + steps);
                int step = 0;
                if(steps != 0 && (heightdiff != 0 || last_form_button == "flatter"))
                {
                    for (int z = LD.z; pom2(LD.z, PG.z, z); pom(LD.z, PG.z, ref z))
                    {
                        
                        for (int x = LD.x; pom2(LD.x, PG.x, x); pom(LD.x, PG.x, ref x))
                        {
                            //Debug.DrawLine(new Vector3(x, Terenowanie.maxHeight+1, z), new Vector3(x, -5, z), Color.green, 60);
                            bool traf = Physics.Raycast(new Vector3(x, Terenowanie.maxHeight+1, z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 11);
                            index = x + 4 * z * SliderWidth.val + z;
                            Vector3 vertpos = Helper.IndexToPos(index);
                            if (traf && hit.transform.gameObject.name == "on" && IsWithinMapBounds(vertpos))
                            {
                                //Debug.DrawRay(new Vector3(x, Terenowanie.maxHeight+1, z), Vector3.down, Color.blue, 40);
                                
                                float old_Y = vertpos.y; // tylko do keepshape
                                if (last_form_button == "prostry")
                                    vertpos.y = LDH + step / steps * heightdiff;
                                else if (last_form_button == "integral")
                                    vertpos.y = LDH + Smootherstep(LDH, slider_realheight, LDH + step / steps * heightdiff) * heightdiff;
                                else if (last_form_button == "jumper")
                                    vertpos.y = LDH + 2 * Smootherstep(LDH, slider_realheight, LDH + 0.5f * step / steps * heightdiff) * heightdiff;
                                else if (last_form_button == "jumperend")
                                    vertpos.y = LDH + 2*(Smootherstep(LDH, slider_realheight, LDH + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
                                else if (last_form_button == "flatter")
                                    vertpos.y = LDH - EditorMenu.flatter[flatter_index].heights[step];
                                if (KeepShape.isOn)
                                    vertpos.y += old_Y - LDH;
                                Helper.former_heights[index] = vertpos.y;
                                Helper.current_heights[index] = Helper.former_heights[index];
                                GameObject znacznik = hit.transform.gameObject;
                                znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);
                            }
                        }
                        //Debug.Log(step / steps);
                        step += 1;
                    }
                } 
            }
            //foreach(GameObject znacznik in znaczniki)
            //{
            //    znacznik.GetComponent<MeshRenderer>().sharedMaterial = transp;
            //    znacznik.name = "off";
            //}
            UpdateMapColliders(znaczniki);
            if (current != null)
                Budowanie.UpdateTiles(new List<GameObject> { current });
            Budowanie.UpdateTiles(surroundings, znaczniki);
            surroundings.Clear();
        }
        
    }

    bool IsWithinMapBounds(Vector3 v)
    {
        return (v.x > 0 && v.x < 4 * SliderWidth.val + 1 && v.z > 0 && v.z < 4 * SliderHeight.val + 1) ? true : false;
    }
    float Smootherstep(float edge0, float edge1, float x)
    {
        if (edge1 - edge0 == 0)
            return 0;
        // Scale to 0 - 1
        x = (x - edge0) / (edge1 - edge0);
        // 
        return  (x * x * x * (x * (x * 6f - 15f) + 10f));
    }
    Vector3Int findPG(Vector3 LD){
        //Debug.Log("LD=" + LD);
		int lowX = 999999, hiX = -1, lowZ = 999999, hiZ = -1;
		foreach (GameObject znacznik in znaczniki) {
            if(znacznik.name == "on")
            {
                if (lowX > znacznik.transform.position.x)
                    lowX = Mathf.RoundToInt(znacznik.transform.position.x);
                if (hiX < znacznik.transform.position.x)
                    hiX = Mathf.RoundToInt(znacznik.transform.position.x);

                if (lowZ > znacznik.transform.position.z)
                    lowZ = Mathf.RoundToInt(znacznik.transform.position.z);
                if (hiZ < znacznik.transform.position.z)
                    hiZ = Mathf.RoundToInt(znacznik.transform.position.z);
            }
			
		}
        //Debug.Log("lowX"+lowX+ ", hiX " + hiX+ ", lowZ " + lowZ+ ", hiZ " + hiZ);
		if (lowX < LD.x) { 
			if (lowZ < LD.z) { 
				return new Vector3Int (lowX, 0, lowZ);
			} else {
				return new Vector3Int (lowX, 0, hiZ);
			}
		} else { // lowX = LD.x
            //Debug.Log("lowX = LD.x");
			if (lowZ < LD.z) { 
				return new Vector3Int (hiX, 0, lowZ);
			} else {
				return new Vector3Int (hiX, 0, hiZ);
			}
		}
	}

	void zaznacz_vertexy_tilesa(){
        state_help_text.text = "Marking vertices..";
		//Zaznaczanie vertexów tylko w trybie manipulacji tilesa
		if (Input.GetMouseButtonDown (0)) { // Rozpoczęcie zaznaczania..
			isSelecting = true;
			mousePosition1 = Input.mousePosition;
		}
		if (Input.GetMouseButtonUp (0)) { // ..i zakończenie.
			foreach (GameObject znacznik in znaczniki) {
				if (IsWithinSelectionBounds (znacznik)) {
					//Debug.Log (znacznik.transform.position);
					if (znacznik.GetComponent<MeshRenderer> ().sharedMaterial == transp) {
                        znacznik.name = "on";
						znacznik.GetComponent<MeshRenderer> ().sharedMaterial = red;
					} else {
                        znacznik.name = "off";
                        znacznik.GetComponent<MeshRenderer> ().sharedMaterial = transp;
					}
				}
			}
            isSelecting = false;
        }
        if (last_form_button != null && isSelecting == false)
            waiting4LD = true;
    }
	void OnGUI(){
		if( isSelecting )
		{
			// Create a rect from both mouse positions
			Rect rect = Utils.GetScreenRect( mousePosition1, Input.mousePosition );
			Utils.DrawScreenRect( rect, new Color( 0.8f, 0.8f, 0.95f, 0.25f ) );
		}
	}
	public bool IsWithinSelectionBounds( GameObject gameObject )
	{
		if(!isSelecting)
			return false;
		Camera camera = Camera.main;
		Bounds viewportBounds = Utils.GetViewportBounds( camera, mousePosition1, Input.mousePosition );
		return viewportBounds.Contains(camera.WorldToViewportPoint(gameObject.transform.position));
	}
	void czule_vertexy(bool checkTerrain){
        if (!Highlight.nad)
            return;
		FormMenu.gameObject.SetActive (true);
        if(current != null && !checkTerrain)
        {
            Mesh rmc = current.GetComponent<MeshFilter>().mesh;
            if (znaczniki.Count != 0)
            {
                if (znaczniki[0].name == current.name + "_mrk")
                {
                    //Żądanie identycznego ustawienia wskaźników => nic nie rób
                    return;
                }
                else
                {
                    del_znaczniki();
                }
            }
            for (int i = 0; i < rmc.vertexCount; i++)
            {
                GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
                znacznik.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                znacznik.transform.position = current.transform.TransformPoint(rmc.vertices[i]);
                znacznik.GetComponent<MeshRenderer>().material = transp;
                znacznik.GetComponent<BoxCollider>().enabled = false;
                if (i == 0)
                    znacznik.name = current.name + "_mrk";
                znaczniki.Add(znacznik);
            }
        }
        else // Mamy mapę. Liczba vertexów jest ograniczona dla wydajności =  max_verts_visible_dim
        {
            Vector3Int v = Highlight.pos;
            if (znaczniki.Count != 0)
            {
                if (znaczniki[0].name == "first")//Żądanie identycznego ustawienia wskaźników => nic nie rób
                    return;
                else
                    del_znaczniki();
            }

            for(int z = v.z - max_verts_visible_dim/2; z<=v.z + max_verts_visible_dim/2; z++)
            {
                for(int x = v.x - max_verts_visible_dim / 2; x <= v.x + max_verts_visible_dim / 2; x++)
                {
                    if(IsWithinMapBounds(new Vector3(x,0,z)))//Nie zmieniamy vertexów na obrzeżach i poza mapą
                    {
                        GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        znacznik.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                        int index = x + 4 * z * SliderWidth.val + z;
                        znacznik.transform.position = Helper.IndexToPos(index);
                        znacznik.GetComponent<MeshRenderer>().material = transp;
                        znacznik.GetComponent<BoxCollider>().enabled = false;
                        
                        if (znaczniki.Count == 0)
                            znacznik.name = "first";
                        znaczniki.Add(znacznik);
                    }
                }
            }
        }
		
		istilemanip = true;
	}
	public static void del_znaczniki(){
        if(znaczniki.Count != 0)
        {
            for (int i = 0; i < znaczniki.Count; i++)
            {
                Destroy(znaczniki[i]);
            }
            znaczniki.Clear();
            GameObject.Find("e_formPANEL").GetComponent<Terenowanie>().FormMenu.gameObject.SetActive(false);
        }
		
	}
	int control(){
		// 0 - żaden z trybów; 1 - manual; 2-menuform;
		if (istilemanip && !waiting4LDpassed)
			return 0;
		if (firstFormingMode) {
            current = null;
			return 1;
		} else if (Physics.Raycast (new Vector3 (Highlight.pos.x, Terenowanie.maxHeight+1, Highlight.pos.z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 9)) {
            if (hit.transform.gameObject.layer == 9)
            {
                current = hit.transform.gameObject;
                return 2;
            }
        } else
        {
            current = null;
            return 2;
        }
        return 0;
	}
	void make_elevation(){
		if (index == 0) {
			//Ustal początkową pozycję i ustaw tam znacznik
			if(IsWithinMapBounds(Highlight.pos)){
			index = Highlight.pos.x + 4*SliderWidth.val*Highlight.pos.z + Highlight.pos.z;
				//Debug.Log("I1="+index+" "+m.vertices[index]+" pos="+highlight.pos);
				indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                indicator.transform.localScale = new Vector3(.25f, 1, .25f);
				indicator.transform.position = Highlight.pos;
			}
		} else {
			//Pozycja początkowa ustalona.
			if (IsWithinMapBounds(Highlight.pos)) {
                int index2 = Highlight.pos.x + 4 * SliderWidth.val * Highlight.pos.z + Highlight.pos.z;
                //Debug.Log("I2="+index2+" "+m.vertices[index]+" pos="+highlight.pos);
                Vector3Int a = Vector3Int.RoundToInt(Helper.IndexToPos(index));
                Vector3Int b = Vector3Int.RoundToInt(Helper.IndexToPos(index2));
                {
                    List<int> indexes = new List<int>();
                    for (int z = Mathf.Min(a.z, b.z); z <= Mathf.Max(a.z, b.z); z++)
                    {
                        for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
                        {
                            int idx = x + 4 * z * SliderWidth.val + z;
                            Helper.former_heights[idx] = SliderValue2RealHeight(slider.value);
                            Helper.current_heights[idx] = Helper.former_heights[idx];
                            indexes.Add(idx);
                        }
                    }
                    UpdateMapColliders(indexes); // Uproscic     \\\
                }
				Destroy (indicator);
				index = 0;
				RaycastHit[] hits  = Physics.BoxCastAll(new Vector3(0.5f*(a.x+b.x),Terenowanie.maxHeight+1,0.5f*(a.z+b.z)), new Vector3(0.5f*Mathf.Abs(a.x-b.x),1f,0.5f*(Mathf.Abs(a.z-b.z))), Vector3.down,Quaternion.identity,Terenowanie.rayHeight,1<<9); //Szukaj jakiegokolwiek tilesa w zaznaczeniu
                List<GameObject> to_update = new List<GameObject>();
                foreach (RaycastHit hit in hits)
                {
                    to_update.Add(hit.transform.gameObject);
                }
                Budowanie.UpdateTiles(to_update);
            }
		}
	}
    static float GetLowestRMCPoint(GameObject rmc_o)
    {
        float to_return = Terenowanie.maxHeight+1;
        foreach(Vector3 vert in rmc_o.GetComponent<MeshCollider>().sharedMesh.vertices)
        {
            if (vert.y < to_return)
                to_return = vert.y;
        }
        return to_return;
    }
	void ctrl_key_works(){
        
        if (Input.GetKey (KeyCode.LeftControl) && Highlight.nad && !FlyCamera.over_UI && !Input.GetKey (KeyCode.LeftAlt)) {
            if (is_entering_keypad_value)
                Hide_text_helper();
            
            Vector3Int v = Highlight.pos;
			int index = v.x + 4 * v.z * SliderWidth.val + v.z;
            slider.value = RealHeight2SliderValue(Helper.current_heights[index]);
		}
	}

	void single_vertex_manipulation() {
        if (Highlight.nad && !FlyCamera.over_UI && IsWithinMapBounds(Highlight.pos))
        {
            Vector3Int v = Highlight.pos;
            //Debug.DrawLine(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.red, 5);
            RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), 0.5f, Vector3.down, Terenowanie.rayHeight, 1 << 9);
            List<GameObject> to_update = new List<GameObject>();
            foreach (RaycastHit hit in hits)
                to_update.Add(hit.transform.gameObject);
            int index = v.x + 4 * v.z * SliderWidth.val + v.z;
            if(to_update.Count != 0)
            {
                if (AreListedObjectsHaveRMCVertexHere(to_update, index))
                {
                    Helper.current_heights[index] = SliderValue2RealHeight(slider.value);
                    //Helper.current_heights[index] = Helper.former_heights[index];
                    UpdateMapColliders(new List<int> { index });
                    Budowanie.UpdateTiles(to_update);
                }
            }
            else
            {
                Helper.former_heights[index] = SliderValue2RealHeight(slider.value);
                Helper.current_heights[index] = Helper.former_heights[index];
                UpdateMapColliders(new List<int> { index });
            }
        }
    }

    bool AreListedObjectsHaveRMCVertexHere(List<GameObject> to_update, int index)
    {
        foreach(GameObject rmc in to_update)
        {
            bool found_matching = false;
            foreach(Vector3 v in rmc.GetComponent<MeshCollider>().sharedMesh.vertices)
            {
                Vector3Int V = Vector3Int.RoundToInt(rmc.transform.TransformPoint(v));
                if (V.x + 4 * V.z * SliderWidth.val + V.z == index)
                {
                    found_matching = true;
                    break;
                }
            }
            if (!found_matching)
                return false;
        }
        return true;
    }

    bool Distance(Vector3 v1, Vector3 v2){ // do porównywania punktów 1 na drugim
		return (Mathf.Abs (v1.x - v2.x) < 0.02 && Mathf.Abs (v1.z - v2.z) < 0.02) ? true : false;
	}
}

