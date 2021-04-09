using System;
using System.Collections.Generic;
///<summary>Info about element placed on the map during mapping</summary>
public class TilePlacement
{
  public string Name;
  public int Rotation;
  public bool Inversion;
  public byte Height;
  /// <summary>
  /// terrain vertices = list of vertices' indexes that this element contains
  /// </summary>
  public int [] t_verts;
  public void Set(string nazwa, int rotacja, bool inwersja, byte Height)
  {
    this.Name = nazwa;
    this.Inversion = inwersja;
    this.Rotation = rotacja;
    this.Height = Height;
  }

}
