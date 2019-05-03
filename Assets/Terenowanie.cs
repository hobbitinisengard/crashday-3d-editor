using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DuVec3
{
    public Vector3 P1;
    public Vector3 P2;
    public DuVec3(Vector3 p1, Vector3 p2)
    {
        P1 = p1;
        P2 = p2;
    }
}
//handles terrain forming (FORM mode)
public class Terenowanie : MonoBehaviour
{
    public GameObject FormMenu;
    public GameObject savePanel;
    public GameObject CopyText;
    public Text state_help_text;
    public Slider slider;
    public Material transp;
    public Material red;
    public Button toslider; //button TO SLIDER
    public Button hill; //creates hill with smooth by ...
    public byte hillDim = 0; //  .. hillDim edges
    public Button Flatten;
    public Button Jumper;
    public Button Prostry;
    public Button JumperEnd;
    public Button Integral;
    public Button CopyButton;
    public Button InverseButton;
    public Button RotateButton;
    public Toggle KeepShape;
    public Toggle Connect;
    public Text HelperInputField; // Text for setting height by numpad
    public Text SelectionRotation; // Helper text for showing current rotation
    private int SelectionRotationVal = 0;
    public static GameObject indicator;
    public static List<GameObject> znaczniki = new List<GameObject>();
    public static List<GameObject> surroundings = new List<GameObject>();

    public static int minHeight;
    public static int maxHeight;
    public static int rayHeight;
    int index = 0; //Indexy do meshow dla vertexa
    public static Vector3Int max_verts_visible_dim = new Vector3Int(60, 0, 60); // Vector3 of visible vertices in second form mode
    float slider_realheight;
    GameObject current; // Ostatnio zaznaczony element

    public static bool firstFormingMode = true; //  F - toggles forming mode
    public static bool istilemanip = false;
    public static bool isSelecting = false; //Selecting vertices mode (white <-> red)
    bool waiting4LD = false; //After selecting shape, state of waiting for bottom-left vertex
    bool waiting4LDpassed = false; // state of execution of shape after waiting for bottom-left vertex
    bool is_entering_keypad_value = false; // used in numericenter();
    int menucontrol = 1; // 0=do nothing 1=firstFormingMode 2=second forming mode
    string last_form_button;
    Vector3Int LD;
    float LDH; // auxiliary value for height of bottom-left vertex
    Vector3 mousePosition1;
    string buffer;
    void Awake()
    {
        toslider.onClick.AddListener(() =>
        {
            if (KeepShape.isOn)
                last_form_button = "to_slider";
            else
                FormMenu_toSlider();
        });
        hill.onClick.AddListener(FormMenu_Hill);
        Prostry.onClick.AddListener(() => last_form_button = "prostry");
        Integral.onClick.AddListener(() => last_form_button = "integral");
        Jumper.onClick.AddListener(() => last_form_button = "jumper");
        JumperEnd.onClick.AddListener(() => last_form_button = "jumperend");
        Flatten.onClick.AddListener(() => last_form_button = "flatter");
        CopyButton.onClick.AddListener(() => last_form_button = "copy");
        RotateButton.onClick.AddListener(RotateClockwiseSelection);
        InverseButton.onClick.AddListener(InverseSelection);
        state_help_text.text = "Manual forming..";
    }

    void Update()
    {
        if (!savePanel.activeSelf)
        {
            ManageCopyPasteVertices();
            Numericenter();
            mousewheelcheck();
            SetFormingMode();
            Ctrl_key_works();
            istilemanip_state(); //selecting vertices. (PPM disables it)
            waiting4LD_state(); //  to get bottom-left vertex
            selectShape();
            menucontrol = Control();
            if (menucontrol == 1)
            {
                if (!Input.GetKey(KeyCode.LeftControl)) //jeżeli nie było ctrl_key_works()
                {
                    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0) && index == 0)
                        Single_vertex_manipulation(); // single-action :)
                    else if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0) && index == 0)
                        Single_vertex_manipulation(); //auto-fire >:)
                    else if (Input.GetMouseButtonDown(1) && Highlight.nad)
                        Make_elevation();
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {//ESC toggles off Make_Elevation()
                        index = 0;
                        if (indicator != null)
                            Destroy(indicator);
                    }
                }
            }
            else if (menucontrol == 2)
            {
                // form Menu
                if (indicator != null)
                {
                    Destroy(indicator);
                    index = 0;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    HandleVertexBoxes(Input.GetKey(KeyCode.Q));
                }
            }
        }

    }

    public void Update_HillDim(string val)
    {
        hillDim = byte.Parse(val);
    }
    private void Hide_text_helper()
    {
        slider.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
        HelperInputField.text = "";
        is_entering_keypad_value = false;
    }
    public void ManageCopyPasteVertices()
    {
        if (Input.GetKeyDown(KeyCode.C) && IsAnyZnacznikMarked())
            last_form_button = "copy";
        if (Input.GetKeyDown(KeyCode.R))
            RotateClockwiseSelection();
        if (Input.GetKeyDown(KeyCode.V))
            PasteSelectionOntoTerrain();
        if (Input.GetKeyDown(KeyCode.M))
            InverseSelection();
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            CopyText.SetActive(false);
            Helper.CopyClipboard.Clear();
        }
    }
    public bool IsAnyZnacznikMarked()
    {
        foreach (var z in znaczniki)
        {
            if (z.name == "on")
                return true;
        }
        return false;
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
        KeyCode[] keyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.KeypadPeriod, KeyCode.KeypadMinus };
        for (int i = 0; i < keyCodes.Length; i++)
        {
            if (Input.GetKeyDown(keyCodes[i]))
            {
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
    /// a) Given list of map colliders => \-/ MapCollider \-/ vertex of that mc update its position (using former_heights)
    /// b) Given list of znaczniki(tags) => List of indexes of vertices, \-/ tag save its position to list, then run overload using list of int 
    /// </summary>
    public static void UpdateMapColliders(List<GameObject> mcs, bool IsRecoveringTerrain = false)
    {
        if (mcs[0].layer == 11) // Argumentami znaczniki
        {
            List<int> indexes = new List<int>();
            foreach (GameObject znacznik in mcs)
            {
                Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
                indexes.Add(v.x + 4 * v.z * SliderWidth.val + v.z);
            }
            UpdateMapColliders(indexes, IsRecoveringTerrain);

        }
        else //Argumentami MapCollidery
        {
            foreach (GameObject mc in mcs)
            {
                Vector3[] verts = mc.GetComponent<MeshCollider>().sharedMesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3Int v = Vector3Int.RoundToInt(mc.transform.TransformPoint(verts[i]));
                    if (IsRecoveringTerrain)
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
    /// List of map colliders, \-/ position from index cast ray (layer=8). If hit isn't on list, add it. Run overload for gameObjects.
    /// </summary>
    public static void UpdateMapColliders(List<int> indexes, bool przywrocenie_terenu = false)
    {
        if (indexes.Count == 0)
            return;
        List<GameObject> mcs = new List<GameObject>();
        foreach (int i in indexes)
        {
            Vector3Int v = Vector3Int.RoundToInt(Helper.IndexToPos(i));
            v.y = Terenowanie.maxHeight + 1;
            RaycastHit[] hits = Physics.SphereCastAll(v, 0.002f, Vector3.down, Terenowanie.rayHeight, 1 << 8);
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
        rmc_pos.y = Terenowanie.maxHeight + 1;
        RaycastHit[] hits = Physics.BoxCastAll(rmc_pos, new Vector3(4 * tileDims.x * 0.6f, 1, 4 * tileDims.z * 0.6f), Vector3.down, Quaternion.identity, Terenowanie.rayHeight, 1 << 8);
        List<GameObject> mcs = new List<GameObject>();
        foreach (RaycastHit hit in hits)
        {
            mcs.Add(hit.transform.gameObject);
        }
        UpdateMapColliders(mcs, przywrocenie_terenu);
    }

    float FindLowestY(List<GameObject> znaczniki)
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
        if (!firstFormingMode && Input.GetKeyDown(KeyCode.F)) // Going from 2nd mode to 1st
        {
            waiting4LD = false;
            isSelecting = false;
            istilemanip = false;
            Del_znaczniki();
            state_help_text.text = "Manual forming..";
            firstFormingMode = true;
        }
        else if (firstFormingMode && Input.GetKeyDown(KeyCode.F)) //Going from 1st mode to 2nd
        {
            firstFormingMode = false;
            state_help_text.text = "Shape forming..";
        }
    }

    void istilemanip_state()
    {
        if (!waiting4LD)
        {
            if (istilemanip)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetKey(KeyCode.Escape))
                { // RMB  or ESC turns formMenu off
                    istilemanip = false;
                    Del_znaczniki();
                    state_help_text.text = "Shape forming..";
                }
                else
                    Zaznacz_vertexy_tilesa();
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
                else if (last_form_button == "copy")
                    CopySelectionToClipboard();
                else
                    apply_fancy_shape();

                waiting4LDpassed = false;
                last_form_button = null;
                KeepShape.isOn = false;
            }
        }
    }
    public void CopySelectionToClipboard()
    {
        RaycastHit hit;
        Physics.Raycast(new Vector3(LD.x, LDH + 1, LD.z), Vector3.down, out hit, rayHeight, 1 << 11);
        Helper.CopyClipboard.Clear();
        Helper.CopyClipboard.Add(Vector3.zero);
        foreach (var mrk in znaczniki)
        {
            if (mrk.name == "on")
            {
                Vector3 pom = mrk.transform.position - hit.transform.position;
                if (pom == Vector3.zero)
                    continue;
                pom.y = mrk.transform.position.y;
                Helper.CopyClipboard.Add(pom);
            }
        }
        SelectionRotationVal = 0;
        CopyText.GetComponent<Text>().text = SelectionRotationVal.ToString();
        CopyText.SetActive(true);
    }

    public void RotateClockwiseSelection()
    {
        if (Helper.CopyClipboard.Count == 0)
            return;
        SelectionRotationVal = SelectionRotationVal == 270 ? 0 : SelectionRotationVal + 90;
        for (int i = 1; i < Helper.CopyClipboard.Count; i++)
        {
            Helper.CopyClipboard[i] = RotatePointAroundPivot(Helper.CopyClipboard[i], Helper.CopyClipboard[0], new Vector3(0, 90, 0));
        }
        CopyText.GetComponent<Text>().text = SelectionRotationVal.ToString();
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return new Vector3(Mathf.RoundToInt(point.x), point.y, Mathf.RoundToInt(point.z)); // return it
    }
    /// <summary>
    /// Mirrors selection always along Z axis
    /// </summary>
    public void InverseSelection()
    {
        if (Helper.CopyClipboard.Count < 2)
            return;
    
        for(int i=0; i<Helper.CopyClipboard.Count; i++)
        {
            Helper.CopyClipboard[i] = Vector3.Scale(Helper.CopyClipboard[i], new Vector3(-1,1,1));
        }
        CopyText.GetComponent<Text>().text = "Inversed.";
        Invoke("RefreshCopyText", 1);
    }
    /// <summary>
    /// used to change text after certain time with Invoke() method
    /// </summary>
    public void RefreshCopyText()
    {
        CopyText.GetComponent<Text>().text = SelectionRotationVal.ToString();
    }
    public void PasteSelectionOntoTerrain()
    {
        if (Helper.CopyClipboard.Count == 0)
            return;

        //Indexes of vertices for UpdateMapColliders()
        List<int> indexes = new List<int>();

        // List of tiles lying onto vertices that are now being pasted
        List<GameObject> to_update = new List<GameObject>();

        foreach (var mrk in Helper.CopyClipboard)
        {
            if (IsWithinMapBounds(Highlight.pos + mrk))
            {
                // Update arrays of vertex heights
                indexes.Add(Helper.PosToIndex(Highlight.pos + mrk));
                Helper.current_heights[indexes[indexes.Count - 1]] = mrk.y;
                Helper.former_heights[indexes[indexes.Count - 1]] = mrk.y;

                Vector3 pom = Highlight.pos + mrk;

                // Mark pasted vertices
                GameObject zn = MarkAndReturnZnacznik(pom);
                if (zn != null)
                    zn.transform.position = new Vector3(zn.transform.position.x, mrk.y, zn.transform.position.z);

                // Look for tiles lying here
                {
                    RaycastHit tile;
                    pom.y = maxHeight;
                    if (Physics.SphereCast(pom, 0.1f, Vector3.down, out tile, rayHeight, 1 << 9) && !to_update.Contains(tile.transform.gameObject))
                        to_update.Add(tile.transform.gameObject);
                }
            }
        }
        UpdateMapColliders(indexes);
        Budowanie.UpdateTiles(to_update);
    }

    void waiting4LD_state()
    {
        RaycastHit hit;
        if (waiting4LD && !waiting4LDpassed)
        {
            state_help_text.text = "Waiting for bottom-left vertex..";
            foreach (GameObject znacznik in znaczniki)
            {
                znacznik.GetComponent<BoxCollider>().enabled = true;
                znacznik.layer = 11;
            }

            if (Input.GetMouseButtonDown(0)) //Pierwszy klik
            {
                if (Physics.Raycast(new Vector3(Highlight.pos.x, 100, Highlight.pos.z), Vector3.down, out hit, Terenowanie.maxHeight + 1, 1 << 11) && hit.transform.gameObject.name == "on")
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
    void mousewheelcheck()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0 && is_entering_keypad_value)
        {
            Hide_text_helper();
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < 2000)
            {
                slider.value += 10;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > -2000)
            {
                slider.value -= 10;
            }
        }
        else if (Input.GetKey(KeyCode.LeftAlt))
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < 2000)
            {
                slider.value += 0.25f;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > -2000)
            {
                slider.value -= 0.25f;
            }
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < 2000)
        {
            slider.value += 1;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > -2000)
        {
            slider.value -= 1;
        }
        slider_realheight = SliderValue2RealHeight(slider.value);
    }
    public static float SliderValue2RealHeight(float sliderval)
    {
        return sliderval / 5f;
    }
    public static float RealHeight2SliderValue(float realheight)
    {
        return 5 * realheight;
    }
    /// <summary>
    /// TO SLIDER button logic
    /// </summary>
    void FormMenu_toSlider()
    {
        surroundings = Budowanie.get_surrounding_tiles(null, znaczniki);
        float elevateby = 0;
        float slider_realheight = SliderValue2RealHeight(slider.value);
        if (KeepShape.isOn)
            elevateby = slider_realheight - LDH;
        if (istilemanip)
        {
            //Aktualizuj teren
            List<int> indexes = new List<int>();
            foreach (GameObject znacznik in znaczniki)
            {
                if (znacznik.name == "on")
                {
                    Vector3Int v = Vector3Int.RoundToInt(znacznik.transform.position);
                    int index = v.x + 4 * v.z * SliderWidth.val + v.z;
                    indexes.Add(index);
                    if (KeepShape.isOn)
                        Helper.current_heights[index] += elevateby;
                    else
                        Helper.current_heights[index] = slider_realheight;

                    znacznik.transform.position = new Vector3(znacznik.transform.position.x, Helper.current_heights[index], znacznik.transform.position.z);
                    Helper.former_heights[index] = Helper.current_heights[index];
                }
            }
            UpdateMapColliders(indexes);

            if (current != null)
                surroundings.Add(current);
            Budowanie.UpdateTiles(surroundings);
            surroundings.Clear();
        }
        else
            Debug.LogError("istilemanip = false");

        KeepShape.isOn = false;
        last_form_button = "";
    }
    /// <summary>
    /// Smooth terrain button logic
    /// </summary>
    void FormMenu_Hill()
    {
        if (hillDim <= 0)
            return;
        surroundings = Budowanie.get_surrounding_tiles(null, znaczniki);
        if (istilemanip)
        {
            List<int> indexes = MarkIsolines(GetMarkedZnaczniki());
            UpdateMapColliders(indexes);
            if (current != null)
                surroundings.Add(current);
            Budowanie.UpdateTiles(surroundings);
            surroundings.Clear();
        }
        else
            Debug.LogError("istilemanip = false");

        KeepShape.isOn = false;
    }
    /// <summary>
    /// Returns list of red znaczniki(tags)
    /// </summary>
    /// <returns></returns>
    private List<Vector3> GetMarkedZnaczniki()
    {
        List<Vector3> marked = new List<Vector3>();
        foreach (var z in znaczniki)
        {
            if (z.name == "on")
                marked.Add(new Vector3(z.transform.position.x, 0, z.transform.position.z));
        }
        return marked;
    }

    private bool IsThisZnacznikMarked(Vector3 z_pos)
    {
        RaycastHit hit;
        z_pos.y = maxHeight;
        if (Physics.SphereCast(z_pos, 0.1f, Vector3.down, out hit, rayHeight, 1 << 11) && hit.transform.name == "on")
            return true;
        return false;
    }
    /// <summary>
    /// Goes around red znaczniki creating isolines of height (set by smoothstep). Every znacznik in isoline get red color (selected).
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    private List<int> MarkIsolines(List<Vector3> top)
    {
        List<int> indexes = new List<int>();
        List<Vector3> setznaczniki = new List<Vector3>();
        Vector2Int[] moves = new Vector2Int[] { new Vector2Int(0, 1),
                                          new Vector2Int(1,1), 
        /*moves relative to global*/      new Vector2Int(1, 0),
                                          new Vector2Int(1, -1),
                                          new Vector2Int(0, -1),
                                          new Vector2Int(-1, -1),
                                          new Vector2Int(-1, 0),
                                          new Vector2Int(-1, 1),
        };
        Vector3 LDpos = Get_LD();
        Vector3 v = new Vector3(LDpos.x, 0, LDpos.z);
        v.z--;
        if (v.z < 1) // provides there is space to begin isolines
            return null;
        int index_of_last_move = 0;
        //Going anti-clockwise and marking isolines
        for (byte j = 1; j <= hillDim; j++)
        {
            do
            {
                byte lookups = 0;
                //Look for move that can be done
                for (int i = index_of_last_move; i < index_of_last_move + moves.Length; i++)
                {
                    lookups++;
                    if (i > moves.Length - 1)
                        i = 0;
                    GameObject znacznik = MarkAndReturnZnacznik(new Vector3(v.x + moves[i].x, 0, v.z + moves[i].y));
                    if (znacznik != null)
                    {
                        v.Set(v.x + moves[i].x, 0, v.z + moves[i].y);
                        if (IsWithinMapBounds(v))
                        {
                            SetVertexInIsoline(v, j, ref top, ref znacznik);
                            indexes.Add(Helper.PosToIndex(v));
                            setznaczniki.Add(v);
                            index_of_last_move = i;
                            break;
                        }
                        else
                        { // return vertices that already 've been found
                            return indexes;
                        }
                    }
                    if (lookups == moves.Length)
                    { // havent found way - dead end - have to go back
                        int licznik = 1;
                        while (true)
                        {
                            v = setznaczniki[setznaczniki.Count - licznik];
                            v.y = 2000;
                            RaycastHit[] hits = Physics.SphereCastAll(v, 1.1f, Vector3.down, rayHeight, 1 << 11);
                            bool found = false;
                            foreach (var hit in hits)
                            {
                                if (hit.transform.name != "on")
                                    found = true;
                            }
                            if (found)
                                break;
                            licznik++;
                        }
                    }
                }
                lookups = 0;
                if (index_of_last_move < 2)
                    index_of_last_move = moves.Length - 1 - index_of_last_move;
                else
                    index_of_last_move -= 2;
            } while (!(v.x == LDpos.x && v.z == LDpos.z - j));
            v.z--;
            setznaczniki.Clear();
        }
        return indexes;
    }
    /// <summary>
    /// Searches for znacznik in given pos. If found znacznik isn't marked, f. marks it and returns it.
    /// </summary>
    private GameObject MarkAndReturnZnacznik(Vector3 z_pos)
    {
        RaycastHit hit;
        z_pos.y = maxHeight;
        if (Physics.Raycast(z_pos, Vector3.down, out hit, rayHeight, 1 << 11))
        {
            if (hit.transform.name == "on")
                return hit.transform.gameObject;
            else
            {
                hit.transform.name = "on";
                hit.transform.GetComponent<MeshRenderer>().material = red;
                return hit.transform.gameObject;
            }
        }
        return null;
    }
    /// <summary>
    /// Handles setting proper height for vertex in isoline
    /// </summary>
    private void SetVertexInIsoline(Vector3 cur, byte izo_nr, ref List<Vector3> top, ref GameObject znacznik)
    {
        Vector3Int p_max = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        foreach (Vector3 p in top)
        {
            Vector3Int point = Vector3Int.RoundToInt(new Vector3(p.x, 0, p.z));
            if (Vector3.Distance(cur, point) < Vector3.Distance(cur, p_max))
            {
                p_max.Set(Mathf.RoundToInt(point.x), 0, Mathf.RoundToInt(point.z));
            }
        }
        float d_max = Distance(cur, p_max);

        Vector3 bisector = (cur - p_max);
        bisector.Normalize();
        // pozycja vertexa o wysokości względnej 0, to przybliżony do siatki punkt przesunięcia cur o wektor bisector
        // o komplementarnej do hillDim+1 długości
        Vector3 p_min = cur + bisector * (hillDim - izo_nr + 1);
        float d_min = Distance(cur, p_min);
        try
        {
            float max = Helper.current_heights[Helper.PosToIndex(p_max)];
            float min = Helper.current_heights[Helper.PosToIndex(p_min)];
            float heightdiff = max - min;
            float newheight = min + Smootherstep(min, max, min + (1f - d_max / (d_max + d_min)) * heightdiff) * heightdiff;
            //float newheight = min + Smootherstep(min, max, min + (1f - (float)izo_nr / (hillDim + 1)) * heightdiff) * heightdiff;
            Helper.current_heights[Helper.PosToIndex(cur)] = newheight;
            Helper.former_heights[Helper.PosToIndex(cur)] = newheight;
            znacznik.transform.position = new Vector3(znacznik.transform.position.x, newheight, znacznik.transform.position.z);
        }
        catch
        {
        }
    }
    //private int GetCorrAngle(Vector2 step)
    //{
    //    if (step.x == 1 && step.y == 0)
    //        return 0;
    //    else if (step.x == 1 && step.y == 1)
    //        return -45;
    //    else if (step.x == 0 && step.y == 1)
    //        return -90;
    //    else if (step.x == -1 && step.y == 1)
    //        return -135;
    //    else if (step.x == -1 && step.y == 0)
    //        return 180;
    //    else if (step.x == -1 && step.y == -1)
    //        return 135;
    //    else if (step.x == 0 && step.y == -1)
    //        return 90;
    //    else if (step.x == 1 && step.y == -1)
    //        return 45;
    //    else
    //        return -1;
    //}

    /// <summary>
    /// Returns most outthrust to bottom-left highlighted vertex of selection
    /// </summary>
    private Vector3Int Get_LD()
    {
        foreach (var znacznik in znaczniki)
        {
            if (znacznik.name == "on")
                return Vector3Int.RoundToInt(znacznik.transform.position);
        }
        return new Vector3Int();
    }
    /// <summary>
    /// Used for dynamic for loops
    /// </summary>
    /// <param name="i"></param>
    /// <param name="condition"></param>
    private void Incdec(ref int i, bool condition)
    {
        if (condition)
            i++;
        else
            i--;
    }
    private DuVecInt GetDimensionsOfZnaczniki(ref List<GameObject> znaczniki)
    {
        int minx = int.MaxValue, maxx = 0, minz = int.MaxValue, maxz = 0;
        foreach (GameObject znacznik in znaczniki)
        {
            if (znacznik.name == "on")
            {
                Vector3Int pos = Vector3Int.RoundToInt(znacznik.transform.position);
                if (minx > pos.x)
                    minx = pos.x;
                if (minz > pos.z)
                    minz = pos.z;
                if (maxx < pos.x)
                    maxx = pos.x;
                if (maxz < pos.z)
                    maxz = pos.z;
            }
        }
        return new DuVecInt(new Vector2Int(minx, minz), new Vector2Int(maxx - minx + 1, maxz - minz + 1));
    }
    /// <summary>
    /// Returns x++ if low < high; else returns x--
    /// </summary>
    float Go2High(int low, int high, ref int x)
    {
        return (low < high) ? x++ : x--;
    }
    /// <summary>
    /// helper function ensuring that:
    /// x goes from bottom left pos (considering rotation of selection; see: bottom-left vertex) to (upper)-right pos
    /// </summary>
    bool Ld_aims4_pg(int ld, int pg, int x)
    {
        return (ld < pg) ? x <= pg : x >= pg;
    }
    int IsFlatter(string nazwa)
    {
        for (int i = 0; i < EditorMenu.flatter.Count; i++)
        {
            if (nazwa == EditorMenu.flatter[i].nazwa)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns List of vertices contains their global position (x or z, depending on bottom-left v-x) and height  
    /// var going shows where u need to go (in global coords) to get into next height-level line
    /// </summary>
    public List<DuVec3> GetOpposingVerticesForConnect(Vector3Int LD, Vector3Int PG)
    {
        List<DuVec3> Extremes = new List<DuVec3>();
        if ((LD.x < PG.x && LD.z > PG.z) || (LD.x > PG.x && LD.z < PG.z))
        { // equal heights along Z axis ||||
            //string going = (LD.x < PG.x && LD.z > PG.z) ? "right" : "left";

            for (int z = LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
            {
                Vector3 P1 = new Vector3(float.MaxValue, 0, 0);
                Vector3 P2 = new Vector3(float.MinValue, 0, 0);
                foreach (var mrk in znaczniki)
                {
                    if (mrk.name == "on" && mrk.transform.position.z == z)
                    {
                        if (mrk.transform.position.x < P1.x)
                            P1 = mrk.transform.position;
                        if (mrk.transform.position.x > P2.x)
                            P2 = mrk.transform.position;
                    }
                }
                if (P1.x == LD.x)
                    Extremes.Add(new DuVec3(new Vector3(P1.x, P1.y, P1.z), new Vector3(P2.x, P2.y, P2.z)));
                else
                    Extremes.Add(new DuVec3(new Vector3(P2.x, P2.y, P2.z), new Vector3(P1.x, P1.y, P1.z)));
                {
                    //bool traf = Physics.Raycast(new Vector3(LD.x, Terenowanie.maxHeight + 1, LD.z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 11);
                    //if (traf && hit.transform.gameObject.name == "on")
                    //{
                    //    //Get outlying extreme position
                    //    RaycastHit[] verts = Physics.BoxCastAll(hit.transform.position, new Vector3(1, rayHeight, 0.4f), going == "left" ? Vector3.left : Vector3.right, Quaternion.identity, Mathf.Infinity, 1 << 11);
                    //    for (int i = verts.Length - 1; i > 0; i--)
                    //    {
                    //        if (verts[i].transform.name == "on")
                    //        {
                    //            Extremes[Extremes.Count - 1].y2 = verts[i].transform.position.y;
                    //            break;
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    bool foundLine = false;
                    //    for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x)) // going Z, (X loop)
                    //    {
                    //        //Check for any markups in this Z line
                    //        traf = Physics.Raycast(new Vector3(x, hit.transform.position.y, z), going == "right" ? Vector3.back : Vector3.forward, out hit, Terenowanie.rayHeight, 1 << 11);
                    //        if (traf)
                    //        {
                    //            LD.x = x;
                    //            z = (int)hit.transform.position.z;
                    //            if (hit.transform.gameObject.name == "on")
                    //            {
                    //                z = (int)Go2High(PG.z, LD.z, ref z);
                    //                foundLine = true;
                    //                break;
                    //            }
                    //        }
                    //    }
                    //    if (!foundLine)
                    //        break;
                    //}\
                }

            }
        }
        else
        {
            //equal heights along X axis _---
            //string going = (LD.x < PG.x && LD.z < PG.z) ? "forward" : "back";
            for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
            {
                Vector3 P1 = new Vector3(0, 0, float.MaxValue);
                Vector3 P2 = new Vector3(0, 0, float.MinValue);
                foreach (var mrk in znaczniki)
                {
                    if (mrk.name == "on" && mrk.transform.position.x == x)
                    {
                        if (mrk.transform.position.z < P1.z)
                            P1 = mrk.transform.position;
                        if (mrk.transform.position.z > P2.z)
                            P2 = mrk.transform.position;
                    }
                }
                if (P1.z == LD.z)
                    Extremes.Add(new DuVec3(new Vector3(P1.x, P1.y, P1.z), new Vector3(P2.x, P2.y, P2.z)));
                else
                    Extremes.Add(new DuVec3(new Vector3(P2.x, P2.y, P2.z), new Vector3(P1.x, P1.y, P1.z)));
                {
                    //bool traf = Physics.Raycast(new Vector3(x, Terenowanie.maxHeight + 1, LD.z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 11);
                    //if (traf && hit.transform.gameObject.name == "on")
                    //{
                    //    //Add base position vertex to list
                    //    Extremes.Add(new DuVec3(x, hit.transform.position.y, -1));

                    //    // Get the most outlying marked vertex in given 'line'
                    //    RaycastHit[] verts = Physics.RaycastAll(hit.transform.position, going == "forward" ? Vector3.forward : Vector3.back, Terenowanie.rayHeight, 1 << 11);
                    //    for (int i = verts.Length - 1; i > 0; i--)
                    //    { 
                    //        if (verts[i].transform.name == "on")
                    //        {
                    //            Extremes[Extremes.Count - 1].y2 = verts[i].transform.position.y;
                    //            break;
                    //        }
                    //    }
                    //} else
                    //{
                    //    bool foundLine = false;
                    //    for(int z=LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z)) // going X, (Z loop)
                    //    {
                    //        traf = Physics.Raycast(new Vector3(x, hit.transform.position.y, z), going == "forward" ? Vector3.right : Vector3.left, out hit, Terenowanie.rayHeight, 1 << 11);
                    //        if (traf)
                    //        {
                    //            LD.z = z;

                    //            x = (int)hit.transform.position.x;
                    //            x = (int)Go2High(PG.x, LD.x, ref x);

                    //            foundLine = true;
                    //            break;
                    //        }
                    //    }
                    //    if (!foundLine)
                    //        break;
                    //}\
                }
            }
        }
        return Extremes;
    }
    /// <summary>
    /// Handles placing more complicated shapes.
    /// </summary>
    void apply_fancy_shape()
    {
        RaycastHit hit;
        //Flatter check
        int flatter_index = -1;
        if (last_form_button == "flatter")
        {
            if (current == null)
                return;
            flatter_index = IsFlatter(Budowanie.GetRMCname(current));
            if (flatter_index == -1)
                return;
        }
        surroundings = Budowanie.get_surrounding_tiles(null, znaczniki);
        int index = 0;
        if (waiting4LDpassed)
        {
            //We have bottom-left, now we're searching for upper-right (all relative to 'rotation' of selection)
            Vector3Int PG = FindPG(LD);
            List<DuVec3> extremes = new List<DuVec3>();
            float heightdiff = slider_realheight - LDH;
            if (KeepShape.isOn)
                heightdiff -= FindHighestY(znaczniki) - LDH;
            if (Connect.isOn)
                extremes = GetOpposingVerticesForConnect(LD, PG);
            if ((LD.x < PG.x && LD.z >= PG.z) || (LD.x > PG.x && LD.z <= PG.z))
            { // equal heights along Z axis ||||
                float steps = Mathf.Abs(LD.x - PG.x);
                int step = 0;
                if (steps != 0 && (heightdiff != 0 || last_form_button == "flatter"))
                {
                    for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
                    {
                        for (int z = LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
                        {
                            if (Connect.isOn)
                            {
                                heightdiff = extremes[Mathf.Abs(z - LD.z)].P2.y - extremes[Mathf.Abs(z - LD.z)].P1.y;
                                steps = Mathf.Abs(extremes[Mathf.Abs(z - LD.z)].P1.x - extremes[Mathf.Abs(z - LD.z)].P2.x);
                                slider_realheight = extremes[Mathf.Abs(z - LD.z)].P2.y;
                                LDH = extremes[Mathf.Abs(z - LD.z)].P1.y;
                            }
                            bool traf = Physics.Raycast(new Vector3(x, Terenowanie.maxHeight + 1, z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 11);
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
                                if (KeepShape.isOn)
                                    vertpos.y += old_Y - LDH;
                                Helper.former_heights[index] = vertpos.y;
                                Helper.current_heights[index] = Helper.former_heights[index];
                                GameObject znacznik = hit.transform.gameObject;
                                znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);

                            }
                            //if (Connect.isOn)
                            //    step += 1;
                        }
                        //Debug.Log(x + " "+ LDH +" "+ slider_realheight + " "+ LDH + step / steps * heightdiff + "HEIGHT="+ verts[index].y);
                        //if (!Connect.isOn)
                        step += 1;
                    }

                }

            }
            else
            { // equal heights along X axis _-_-
                float steps = Mathf.Abs(LD.z - PG.z);
                //Debug.Log("steps = " + steps);
                int step = 0;
                if (steps != 0 && (heightdiff != 0 || last_form_button == "flatter"))
                {
                    for (int z = LD.z; Ld_aims4_pg(LD.z, PG.z, z); Go2High(LD.z, PG.z, ref z))
                    {
                        for (int x = LD.x; Ld_aims4_pg(LD.x, PG.x, x); Go2High(LD.x, PG.x, ref x))
                        {
                            if (Connect.isOn)
                            {
                                heightdiff = extremes[Mathf.Abs(x - LD.x)].P2.y - extremes[Mathf.Abs(x - LD.x)].P1.y;
                                steps = Mathf.Abs(extremes[Mathf.Abs(x - LD.x)].P1.z - extremes[Mathf.Abs(x - LD.x)].P2.z);
                                slider_realheight = extremes[Mathf.Abs(x - LD.x)].P2.y;
                                LDH = extremes[Mathf.Abs(x - LD.x)].P1.y;
                            }
                            //Debug.DrawLine(new Vector3(x, Terenowanie.maxHeight+1, z), new Vector3(x, -5, z), Color.green, 60);
                            bool traf = Physics.Raycast(new Vector3(x, Terenowanie.maxHeight + 1, z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 11);
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
                                    vertpos.y = LDH + 2 * (Smootherstep(LDH, slider_realheight, LDH + (0.5f * step / steps + 0.5f) * heightdiff) - 0.5f) * heightdiff;
                                else if (last_form_button == "flatter")
                                    vertpos.y = LDH - EditorMenu.flatter[flatter_index].heights[step];
                                if (KeepShape.isOn)
                                    vertpos.y += old_Y - LDH;
                                Helper.former_heights[index] = vertpos.y;
                                Helper.current_heights[index] = Helper.former_heights[index];
                                GameObject znacznik = hit.transform.gameObject;
                                znacznik.transform.position = new Vector3(znacznik.transform.position.x, vertpos.y, znacznik.transform.position.z);
                            }
                            //if (Connect.isOn)
                            //    step += 1;
                        }
                        //if (!Connect.isOn)
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
        return (v.x > 0 && v.x < 4 * SliderWidth.val && v.z > 0 && v.z < 4 * SliderHeight.val) ? true : false;
    }
    bool IsWithinMapBounds(int x, int z)
    {
        return (x > 0 && x < 4 * SliderWidth.val && z > 0 && z < 4 * SliderHeight.val) ? true : false;
    }
    float Smootherstep(float edge0, float edge1, float x)
    {
        if (edge1 - edge0 == 0)
            return 0;
        // Scale to 0 - 1
        x = (x - edge0) / (edge1 - edge0);
        // 
        return (x * x * x * (x * (x * 6f - 15f) + 10f));
    }
    Vector3Int FindPG(Vector3 LD)
    {
        //Debug.Log("LD=" + LD);
        int lowX = 999999, hiX = -99999, lowZ = 999999, hiZ = -999999;
        foreach (GameObject znacznik in znaczniki)
        {
            if (znacznik.name == "on")
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
        if (lowX < LD.x)
        {
            if (lowZ < LD.z)
            {
                return new Vector3Int(lowX, 0, lowZ);
            }
            else
            {
                return new Vector3Int(lowX, 0, hiZ);
            }
        }
        else
        { // lowX = LD.x
          //Debug.Log("lowX = LD.x");
            if (lowZ < LD.z)
            {
                return new Vector3Int(hiX, 0, lowZ);
            }
            else
            {
                return new Vector3Int(hiX, 0, hiZ);
            }
        }
    }

    void Zaznacz_vertexy_tilesa()
    {
        state_help_text.text = "Marking vertices..";
        //Zaznaczanie vertexów tylko w trybie manipulacji tilesa
        if (Input.GetMouseButtonDown(0))
        { // Rozpoczęcie zaznaczania..
            isSelecting = true;
            mousePosition1 = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        { // ..i zakończenie.
            foreach (GameObject znacznik in znaczniki)
            {
                if (IsWithinSelectionBounds(znacznik))
                {
                    //Debug.Log (znacznik.transform.position);
                    if (znacznik.GetComponent<MeshRenderer>().sharedMaterial == transp)
                    {
                        znacznik.name = "on";
                        znacznik.GetComponent<MeshRenderer>().sharedMaterial = red;
                    }
                    else
                    {
                        znacznik.name = "off";
                        znacznik.GetComponent<MeshRenderer>().sharedMaterial = transp;
                    }
                }
            }
            isSelecting = false;
        }
        if (last_form_button != null && last_form_button != "" && isSelecting == false)
            waiting4LD = true;
    }
    void OnGUI()
    {
        if (isSelecting)
        {
            // Create a rect from both mouse positions
            Rect rect = Utils.GetScreenRect(mousePosition1, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
        }
    }
    public bool IsWithinSelectionBounds(GameObject gameObject)
    {
        if (!isSelecting)
            return false;
        Camera camera = Camera.main;
        Bounds viewportBounds = Utils.GetViewportBounds(camera, mousePosition1, Input.mousePosition);
        return viewportBounds.Contains(camera.WorldToViewportPoint(gameObject.transform.position));
    }

    void HandleVertexBoxes(bool checkTerrain)
    {
        if (!Highlight.nad)
            return;
        FormMenu.gameObject.SetActive(true);
        if (current != null && !checkTerrain)
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
                    Del_znaczniki();
                }
            }
            for (int i = 0; i < rmc.vertexCount; i++)
            {
                GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
                znacznik.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                znacznik.transform.position = current.transform.TransformPoint(rmc.vertices[i]);
                znacznik.GetComponent<MeshRenderer>().material = transp;
                znacznik.GetComponent<BoxCollider>().enabled = true;
                znacznik.layer = 11;
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
                    Del_znaczniki();
            }

            for (int z = v.z - max_verts_visible_dim.z / 2; z <= v.z + max_verts_visible_dim.z / 2; z++)
            {
                for (int x = v.x - max_verts_visible_dim.x / 2; x <= v.x + max_verts_visible_dim.x / 2; x++)
                {
                    if (IsWithinMapBounds(x, z))//Nie zmieniamy vertexów na obrzeżach i poza mapą
                    {
                        GameObject znacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        znacznik.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                        int index = x + 4 * z * SliderWidth.val + z;
                        znacznik.transform.position = Helper.IndexToPos(index);
                        znacznik.GetComponent<MeshRenderer>().material = transp;
                        znacznik.GetComponent<BoxCollider>().enabled = true;
                        znacznik.layer = 11;
                        if (znaczniki.Count == 0)
                            znacznik.name = "first";
                        znaczniki.Add(znacznik);
                    }
                }
            }
        }

        istilemanip = true;
    }
    public static void Del_znaczniki()
    {
        if (znaczniki.Count != 0)
        {
            for (int i = 0; i < znaczniki.Count; i++)
            {
                Destroy(znaczniki[i]);
            }
            znaczniki.Clear();
            GameObject.Find("e_formPANEL").GetComponent<Terenowanie>().FormMenu.gameObject.SetActive(false);
        }

    }
    /// <summary>
    /// handles simple and advanced terrain forming
    /// </summary>
    /// <returns></returns>
    int Control()
    {
        RaycastHit hit;
        if (istilemanip && !waiting4LDpassed)
            return 0;
        if (firstFormingMode)
        {
            current = null;
            return 1;
        }
        else if (Physics.Raycast(new Vector3(Highlight.pos.x, Terenowanie.maxHeight + 1, Highlight.pos.z), Vector3.down, out hit, Terenowanie.rayHeight, 1 << 9))
        {
            if (hit.transform.gameObject.layer == 9)
            {
                current = hit.transform.gameObject;
                return 2;
            }
        }
        else
        {
            current = null;
            return 2;
        }
        return 0;
    }
    /// <summary>
    /// Handles quick rectangular selection in first form mode with RMB
    /// </summary>
    void Make_elevation()
    {
        if (index == 0)
        {
            //Ustal początkową pozycję i ustaw tam znacznik
            if (IsWithinMapBounds(Highlight.pos))
            {
                index = Highlight.pos.x + 4 * SliderWidth.val * Highlight.pos.z + Highlight.pos.z;
                //Debug.Log("I1="+index+" "+m.vertices[index]+" pos="+highlight.pos);
                indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                indicator.transform.localScale = new Vector3(.25f, 1, .25f);
                indicator.transform.position = Highlight.pos;
            }
        }
        else
        {
            //Pozycja początkowa ustalona.
            if (IsWithinMapBounds(Highlight.pos))
            {
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
                Destroy(indicator);
                index = 0;
                RaycastHit[] hits = Physics.BoxCastAll(new Vector3(0.5f * (a.x + b.x), Terenowanie.maxHeight + 1, 0.5f * (a.z + b.z)), new Vector3(0.5f * Mathf.Abs(a.x - b.x), 1f, 0.5f * (Mathf.Abs(a.z - b.z))), Vector3.down, Quaternion.identity, Terenowanie.rayHeight, 1 << 9); //Szukaj jakiegokolwiek tilesa w zaznaczeniu
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
        float to_return = Terenowanie.maxHeight + 1;
        foreach (Vector3 vert in rmc_o.GetComponent<MeshCollider>().sharedMesh.vertices)
        {
            if (vert.y < to_return)
                to_return = vert.y;
        }
        return to_return;
    }
    void Ctrl_key_works()
    {

        if (Input.GetKey(KeyCode.LeftControl) && Highlight.nad && !FlyCamera.over_UI && !Input.GetKey(KeyCode.LeftAlt))
        {
            if (is_entering_keypad_value)
                Hide_text_helper();

            Vector3Int v = Highlight.pos;
            int index = v.x + 4 * v.z * SliderWidth.val + v.z;
            slider.value = RealHeight2SliderValue(Helper.current_heights[index]);
        }
    }
    /// <summary>
    /// Handles quick sculpting mode
    /// </summary>
    void Single_vertex_manipulation()
    {
        if (Highlight.nad && !FlyCamera.over_UI && IsWithinMapBounds(Highlight.pos))
        {
            Vector3Int v = Highlight.pos;
            //Debug.DrawLine(new Vector3(v.x, Terenowanie.maxHeight+1, v.z), new Vector3(v.x, 0, v.z), Color.red, 5);
            RaycastHit[] hits = Physics.SphereCastAll(new Vector3(v.x, Terenowanie.maxHeight + 1, v.z), 0.5f, Vector3.down, Terenowanie.rayHeight, 1 << 9);
            List<GameObject> to_update = new List<GameObject>();
            foreach (RaycastHit hit in hits)
                to_update.Add(hit.transform.gameObject);
            int index = v.x + 4 * v.z * SliderWidth.val + v.z;
            if (to_update.Count != 0)
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
        foreach (GameObject rmc in to_update)
        {
            bool found_matching = false;
            foreach (Vector3 v in rmc.GetComponent<MeshCollider>().sharedMesh.vertices)
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
    /// <summary>
    /// Distance on 2D map between 3D points
    /// </summary>
    float Distance(Vector3 v1, Vector3 v2)
    {
        return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z));
    }
}

