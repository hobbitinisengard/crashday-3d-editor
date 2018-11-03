using UnityEngine;
using UnityEngine.UI;
public class VertexHeight : MonoBehaviour
{
    public Material partiallytransparent;
    static public float vertexheight_value;
    /// <summary>
    /// shows current height value next to slider in form mode
    /// </summary>
	public void ShowValue(float val)
    {
        GetComponent<Text>().text = val.ToString();
        vertexheight_value = val;
    }
    /// <summary>
    /// Displays transparent cuboid for 2 secs.
    /// </summary>
    /// <param name="v"></param>
	public void SliderPreview(float v)
    {
        float y = Terenowanie.SliderValue2RealHeight(v);
        GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(preview.GetComponent<BoxCollider>());
        preview.GetComponent<MeshRenderer>().material = partiallytransparent;
        preview.transform.localScale = new Vector3(3f, 0.05f, 3);
        preview.transform.position = new Vector3(2 + Highlight.t.x, y, 2 + Highlight.t.z);
        Destroy(preview, 2);
    }

}