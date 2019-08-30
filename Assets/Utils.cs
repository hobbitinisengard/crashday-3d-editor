using UnityEngine;
// Not mine code. Handles vertices marking in form mode.
public class Utils : MonoBehaviour
{

  static Texture2D _bluTexture;
  public static Texture2D BluTexture
  {
    get
    {
      if (_bluTexture == null)
      {
        _bluTexture = new Texture2D(1, 1);
        _bluTexture.SetPixel(0, 0, Color.cyan);
        _bluTexture.Apply();
      }

      return _bluTexture;
    }
  }

  public static void DrawScreenRect(Rect rect, Color color)
  {
    GUI.color = color;
    GUI.DrawTexture(rect, BluTexture);
    GUI.color = Color.white;
  }
  public static Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
  {
    // Move origin from bottom left to top left
    screenPosition1.y = Screen.height - screenPosition1.y;
    screenPosition2.y = Screen.height - screenPosition2.y;
    // Calculate corners
    Vector3 topLeft = Vector3.Min(screenPosition1, screenPosition2);
    Vector3 bottomRight = Vector3.Max(screenPosition1, screenPosition2);
    // Create Rect
    return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
  }
  public static Bounds GetViewportBounds(Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
  {
    var v1 = Camera.main.ScreenToViewportPoint(screenPosition1);
    var v2 = Camera.main.ScreenToViewportPoint(screenPosition2);
    var min = Vector3.Min(v1, v2);
    var max = Vector3.Max(v1, v2);
    min.z = camera.nearClipPlane;
    max.z = camera.farClipPlane;

    var bounds = new Bounds();
    bounds.SetMinMax(min, max);
    return bounds;
  }
}
