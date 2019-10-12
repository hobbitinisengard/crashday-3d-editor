using UnityEngine;
// Parses Cat files meoowwww
// and cfl files
public class Vegetation
{
  public string Name;
  public Vector3 Position;
  public bool AutoHeight;
  /// <summary>
  /// Simple vegetation file constructor
  /// </summary>
  /// <param name="name">name of veg file</param>
  /// <param name="x">Center x pos of bush</param>
  /// <param name="z">Center z pos of bush</param>
  /// <param name="y">auto or string with float number</param>
  public Vegetation(string name, float x, float z, string y)
  {
    Name = name;
    AutoHeight = (y == "Auto") ? true : false;
    if (y == "auto")
    {
      AutoHeight = true;
      Position = new Vector3(x, z, 0);
    }
    else
    {
      AutoHeight = false;
      Position = new Vector3(x, z, float.Parse(y, System.Globalization.CultureInfo.InvariantCulture));
    }
  }
}
