using System.Collections.Generic;
using System.IO;
using UnityEngine;
// Isn't used in editor.
public class Creatermcpos : MonoBehaviour
{
  RaycastHit hit;
  void Start()
  {
    List<string> nazwa_elementu = new List<string>();
    List<float> pZero = new List<float>();
    Object[] prefabs = Resources.LoadAll("prefabs/");

    //Object [] prefabs = new Object[1];
    //prefabs[0] = Resources.Load("prefabs/arenaend");
    foreach (GameObject prefab in prefabs)
    {
      Mesh mesh;
      GameObject tiles = Instantiate(prefab);
      tiles.transform.localScale *= 0.2f;
      if (prefab.transform.childCount != 0)
      {
        mesh = prefab.transform.Find("main").GetComponent<MeshFilter>().sharedMesh;
      }
      else
      {
        mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
      }

      tiles.AddComponent<MeshCollider>();
      tiles.GetComponent<MeshCollider>().sharedMesh = null;
      tiles.GetComponent<MeshCollider>().sharedMesh = mesh;
      tiles.transform.localPosition = new Vector3(2 + 2 * (int.Parse(prefab.tag.Substring(0, 1)) - 1), 0, 2 + 2 * (int.Parse(prefab.tag.Substring(2, 1)) - 1));
      bool traf = Physics.Raycast(new Vector3(0.01f, 15, 0.01f), Vector3.down, out hit, 30);
      if (!traf)
        Debug.LogError("Błąd!");

      nazwa_elementu.Add(prefab.name);
      pZero.Add(hit.point.y);
      DestroyImmediate(tiles);

      //Debug.Log(hit.point.y);
      //Debug.DrawLine (new Vector3 (0.01f, Terenowanie.minHeight, 0.01f), new Vector3(0.01f,hit.point.y,0.01f), Color.green, 50);
    }
    StreamWriter w = new StreamWriter("Assets/Resources/pzeros.txt");
    for (int i = 0; i < pZero.Count; i++)
    {
      w.WriteLine(nazwa_elementu[i] + " " + pZero[i]);
    }
    w.Close();
    //		foreach (GameObject prefab in prefabs) {
    //			List<Vector3> vertz = new List<Vector3>();
    //			GameObject newrmc = new GameObject (prefab.name + "_rmc");
    //			newrmc.AddComponent<MeshFilter> ();
    //			Mesh newrmcMesh = new Mesh ();
    //			Mesh mesh;
    //			RaycastHit hit;
    //			GameObject pow = new GameObject();
    //			GameObject tile = GameObject.Instantiate (prefab);
    //			tile.AddComponent<MeshFilter> ();
    //
    //			Vector3 tileDims = new Vector3 (int.Parse (tile.tag.Substring (0, 1)), 0, int.Parse (tile.tag.Substring (2, 1)));
    //			if (tileDims.x == 1) {
    //				if (tileDims.z == 1) {
    //					pow = GameObject.Instantiate (Resources.Load ("jxj") as GameObject);
    //					pow.AddComponent<MeshCollider> ();
    //					pow.transform.localPosition = new Vector3 (0, 30, 0);
    //				} else if (tileDims.z == 2) {
    //					pow = GameObject.Instantiate (Resources.Load ("jxd") as GameObject);
    //					pow.AddComponent<MeshCollider> ();
    //					pow.transform.localPosition = new Vector3 (0, 30, 0);
    //				}
    //			} else if (tileDims.x == 2) {
    //				if (tileDims.z == 1) {
    //					pow = GameObject.Instantiate (Resources.Load ("dxj") as GameObject);
    //					pow.AddComponent<MeshCollider> ();
    //					pow.transform.localPosition = new Vector3 (0, 30, 0);
    //				} else if (tileDims.z == 2) {
    //					pow = GameObject.Instantiate (Resources.Load ("dxd") as GameObject);
    //					pow.AddComponent<MeshCollider> ();
    //					pow.transform.localPosition = new Vector3 (0, 30, 0);
    //				}
    //			}
    //			if (tile.transform.childCount != 0) {
    //				mesh = tile.transform.Find ("main").GetComponent<MeshFilter> ().mesh;
    //			} else {
    //				mesh = tile.GetComponent<MeshFilter> ().mesh;
    //			}
    //			Vector3[] verts = mesh.vertices;
    //			//Debug.Log (verts.Length);
    //			foreach (Vector3 ver  in verts) {
    //				Vector3 vert = tile.transform.TransformPoint (ver);
    //				vert.y -= 5f;
    //				Mesh pow_m = new Mesh ();
    //				bool traf = Physics.SphereCast(vert,0.01f, Vector3.down, out hit, 100);
    //				Debug.Log ("Ray=" + vert);
    //				if (traf) {
    //					pow_m = hit.transform.GetComponent<MeshFilter> ().mesh;
    //
    //					//Hit triangle
    //					int[] tri = new int[3] {
    //						pow_m.triangles [hit.triangleIndex * 3 + 0],
    //						pow_m.triangles [hit.triangleIndex * 3 + 1],
    //						pow_m.triangles [hit.triangleIndex * 3 + 2]    
    //					};
    //					// Znajdź vertex najbliższy hit pointowi
    //					for (int i = 0; i < 3; i++) {
    //						bool dist = Distance (hit.transform.TransformPoint (pow_m.vertices [tri [i]]), hit.point);
    //						//Debug.Log (hit.transform.TransformPoint (pow_m.vertices [tri [i]]) + " " + hit.point);
    //						if (dist) {
    //							vertz.Add(new Vector3(Mathf.RoundToInt(pow_m.vertices [tri [i]].x), Mathf.RoundToInt(pow_m.vertices [tri [i]].y), Mathf.RoundToInt(pow_m.vertices [tri [i]].z)));
    //							//Debug.DrawRay (pow_m.vertices [tri [i]], Vector3.down, Color.green, 50);
    //							//Debug.Log ("traf. pos="+pow_m.vertices [tri [i]]);
    //						} else if (i == 2) {
    //							//Debug.LogError ("Dystans");
    //						}
    //					}
    //				} else {
    //					//Debug.LogError ("Nie trafił.");
    //					Debug.DrawRay (vert, Vector3.down, Color.red, 50);
    //				}
    //			}
    //			newrmcMesh.SetVertices (vertz);
    //			newrmc.GetComponent<MeshFilter> ().sharedMesh = newrmcMesh;
    //		
    //			//IExportOptions options = new ExportModelSettingsSerialize();
    //			//options. //= ExportSettings.ExportFormat.Binary;
    //			//ModelExporter.ExportObject ("Assets/"+prefab.name + "_rmc.fbx",newrmc, options);
    //			DestroyImmediate (tile);
    //			DestroyImmediate (pow);
    //		}
  }

  bool Distance(Vector3 v1, Vector3 v2)
  { // do porównywania punktów 1 na drugim
    return (Mathf.Abs(v1.x - v2.x) < 0.02 && Mathf.Abs(v1.z - v2.z) < 0.02) ? true : false;
  }

}
