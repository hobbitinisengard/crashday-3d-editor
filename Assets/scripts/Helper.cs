using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
// 3. Skrypt. Pierwszy w edytorze
public class Helper :MonoBehaviour 
{
	public Material thismaterial;
    /// <summary>
    /// Text w upperPanel
    /// </summary>
    public Text nazwa_toru;
    public GameObject editorPanel;
    public static float[] former_heights;
    public static float[] current_heights;
    public static float multiplier = 1;
    int fx = 0, fz = 0;

    void Awake(){
        former_heights = Enumerable.Repeat(0f, (4 * SliderHeight.val + 1) * (4 * SliderWidth.val + 1)).ToArray();
        current_heights = Enumerable.Repeat(0f, (4 * SliderHeight.val + 1) * (4 * SliderWidth.val + 1)).ToArray();

        if (!STATIC.isloading)
        {
            STATIC.TRACK = new TrackSavable((ushort)SliderWidth.val, (ushort)SliderHeight.val);
            STATIC.nazwa_trasy = "Untitled";
            Loader.InitializeSTATICTiles(SliderWidth.val, SliderHeight.val);
        }
        else
            LoadTerrain();

        Terenowanie.minHeight = -2000;
        Terenowanie.maxHeight = 2000;
        Terenowanie.rayHeight = Terenowanie.maxHeight - Terenowanie.minHeight + 5;
        nazwa_toru.text = STATIC.nazwa_trasy;
        CreateScatteredMeshColliders();
    }
    void Start()
    {
        if (STATIC.isloading && Loader.loadedTilesPairsXZ.Count != 0)
            LoadTiles();
        else
            STATIC.isloading = false;
        // Koniec ładowania
    }
    /// <summary>
    /// Ładuje realne wysokości ze STATIC heightmapa i STATIC boundingHeight do current i former heights
    /// </summary>
    void LoadTerrain()
    {
        for (int y = 0; y < 4 * SliderHeight.val + 1; y++)
        {
            for (int x = 0; x < 4 * SliderWidth.val + 1; x++)
            {
                int i = x + 4 * y * SliderWidth.val + y;
                current_heights[i] = multiplier * STATIC.TRACK.Heightmap[4*SliderHeight.val - y][x]/5f;
                former_heights[i] = current_heights[i];
            }
        }
        
    }
    /// <summary>
    /// Tworzy części mapy 5x5, dodaje mesh collider i filter, nakłada materiał i ustawia teren.
    /// </summary>
    void CreateScatteredMeshColliders()
    {
        Mesh basic = Resources.Load<Mesh>("rmcs/basic");
        for (int z=0; z<SliderHeight.val; z++)
        {
            for(int x=0; x<SliderWidth.val; x++)
            {
                GameObject element = new GameObject("Map " + x + " " + z);
                element.transform.position = new Vector3Int(4 * x, 0, 4 * z);
                MeshCollider mc = element.AddComponent<MeshCollider>();
                MeshFilter mf = element.AddComponent<MeshFilter>();
                MeshRenderer mr = element.AddComponent<MeshRenderer>();
                mr.material = thismaterial;
                mf.mesh = Instantiate(basic);
                mc.sharedMesh = Instantiate(basic);
                Terenowanie.UpdateMapColliders(new List<GameObject> { element });
                element.layer = 8;
            }
        }   
    }

    void LoadTiles()
    {
        List<GameObject> to_update = new List<GameObject>();
        foreach(Duint pair in Loader.loadedTilesPairsXZ)
        {
            fx = pair.x;
            fz = pair.z;
            to_update.Add(editorPanel.GetComponent<Budowanie>().PlacePrefab(new Vector3Int(4 * fx, 0, 4 * fz), STATIC.tiles[fx, fz]._nazwa, STATIC.tiles[fx, fz]._rotacja, STATIC.tiles[fx, fz]._inwersja));
        }
        STATIC.isloading = false;
        Budowanie.UpdateTiles(to_update);
        to_update.Clear();
    }
    
    /// <summary>
    /// Zwraca globalne położenie vertexa. Ustawia Y z current_heights
    /// </summary>
    public static Vector3 IndexToPos(int index)
    {
        int x = index % (4*SliderWidth.val+1);
        Vector3 to_return = new Vector3(x, current_heights[index], (index-x) / (4 * SliderWidth.val + 1));
        return to_return;
    }

    //static GameObject CreatePlane(int pnx, int pny, Material feedmaterial)
    //{
    //    List<Vector3> vertz = new List<Vector3>();
    //    List<Vector2> uvs = new List<Vector2>();
    //    List<int> tris = new List<int>();
    //    //Tworzę vertexy i uv
    //    for (int y = 0; y < pny; y++)
    //    {
    //        for (int x = 0; x < pnx; x++)
    //        {
    //            vertz.Add(new Vector3(x, 0, y));
    //            uvs.Add(new Vector2(x, y));
    //        }
    //    }
    //    //Tworzę tris
    //    for (int y = 0; y < pny - 1; y++)
    //    {
    //        for (int i = 0; i < pnx - 1; i++)
    //        {
    //            tris.Add(y * pnx + i);
    //            tris.Add(y * pnx + i + pnx);
    //            tris.Add(y * pnx + i + 1);

    //            tris.Add(y * pnx + i + pnx);
    //            tris.Add(y * pnx + i + pnx + 1);
    //            tris.Add(y * pnx + i + 1);
    //        }
    //    }
    //    go = new GameObject("Map");
    //    go.transform.position = new Vector3(0, 0, 0);
    //    MeshFilter mf = go.AddComponent<MeshFilter>();
    //    MeshRenderer mr = go.AddComponent<MeshRenderer>();
    //    Mesh m = new Mesh();
    //    m.SetVertices(vertz);
    //    m.SetUVs(0, uvs);
    //    m.triangles = tris.ToArray();
    //    mf.sharedMesh = m;
    //    mr.material = feedmaterial;
    //    mf.sharedMesh.RecalculateNormals();
    //    return go;
    //}

}
