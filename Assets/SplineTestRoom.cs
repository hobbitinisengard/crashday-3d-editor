using PathCreation;
using System.Collections.Generic;
using UnityEngine;

public class SplineTestRoom : MonoBehaviour
{
  public Material feedmaterial;
  /// <summary>
  ///  Initial vec
  /// </summary>
  private List<Vector3> Pts1 = new List<Vector3>();
  private List<Vector3> Pts2 = new List<Vector3>();
  private List<Vector3> Pts3 = new List<Vector3>();
  private List<VertexPath> paths = new List<VertexPath>();
  void Start()
  {
    Pts1.Add(new Vector3(0, 5, 0));
    Pts1.Add(new Vector3(1, 4, 0));
    Pts1.Add(new Vector3(2, 2, 0));
    Pts1.Add(new Vector3(3, 1, 0));

    Pts3.Add(new Vector3(8, 5, 8));
    Pts3.Add(new Vector3(8, 4, 7));
    Pts3.Add(new Vector3(8, 2, 6));
    Pts3.Add(new Vector3(8, 1, 5));

    Pts2.Add(new Vector3(2, 5, 6));
    Pts2.Add(new Vector3(3, 4, 5));
    Pts2.Add(new Vector3(4, 2, 4));
    Pts2.Add(new Vector3(5, 1, 3));

    for (int i = 0; i < Pts1.Count; i++)
    {
      VertexPath p = GeneratePath(new Vector3[] { Pts1[i], Pts2[i], Pts3[i] });
      paths.Add(p);
    }
    // poglądowe
    foreach (var path in paths)
    {
      Color c = new Color(Random.value, Random.value, Random.value);
      foreach (var point in path.localPoints)
      {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        MeshRenderer r = sphere.GetComponent<MeshRenderer>();
        r.material.color = c;
        sphere.transform.position = point;
        sphere.transform.localScale /= 8f;
      }
    }
    // find longest path (with most vertices)
    int mostvertices = 0;
    foreach (var path in paths)
    {
      if (mostvertices < path.localPoints.Length)
        mostvertices = path.localPoints.Length;
    }
    // create vertex cloud
    List<Vector3> verts = new List<Vector3>(mostvertices * paths.Count);
    foreach (var path in paths)
    {
      if (mostvertices == path.localPoints.Length)
        verts.AddRange(path.localPoints);
      else
      {
        for (int i = 0; i < mostvertices; i++)
          verts.Add(transform.InverseTransformPoint(path.GetPointAtTime(i / (mostvertices - 1f), EndOfPathInstruction.Stop)));
      }
    }
    GameObject go = new GameObject("Profile mesh");
    go.transform.position = new Vector3(0, 0, 0);
    MeshFilter mf = go.AddComponent<MeshFilter>();
    MeshRenderer mr = go.AddComponent<MeshRenderer>();
    Mesh mesh = CreateProfileMesh(verts, paths.Count, mostvertices);
    
    mf.sharedMesh = mesh;
  }
  Mesh CreateProfileMesh(List<Vector3> vertz, int pnx, int pny)
  {
    List<int> tris = new List<int>();
    // one side
    for (int y = 0; y < pnx - 1; y++)
    {
      for (int i = 0; i < pny - 1; i++)
      {
        // front
        tris.Add(y * pny + i); 
        tris.Add(y * pny + i + 1);
        tris.Add(y * pny + i + pny); 
        
        tris.Add(y * pny + i + 1); 
        tris.Add(y * pny + i + pny + 1); 
        tris.Add(y * pny + i + pny); 

        // back
        tris.Add(y * pny + i + pny); 
        tris.Add(y * pny + i + pny + 1); 
        tris.Add(y * pny + i); 

        tris.Add(y * pny + i + pny + 1); 
        tris.Add(y * pny + i + 1); 
        tris.Add(y * pny + i); 
      }
    }
    Mesh m = new Mesh();
    m.SetVertices(vertz);
    m.triangles = tris.ToArray();
    return m;
  }
  VertexPath GeneratePath(Vector3[] points)
  {
    BezierPath bezierPath = new BezierPath(points, false, PathSpace.xyz);
    // Then create a vertex path from the bezier path, to be used for movement etc
    return new VertexPath(bezierPath, transform, 0.5f);
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
