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
	public HashSet<int> t_verts;

	public TilePlacement()
	{

	}
	public TilePlacement(TilePlacement tilePlacement)
	{
		Name = tilePlacement.Name;
		Rotation = tilePlacement.Rotation;
		Inversion = tilePlacement.Inversion;
		Height = tilePlacement.Height;
	}

	public void Set(string nazwa, int rotacja, bool inwersja, byte Height)
	{
		this.Name = nazwa;
		this.Inversion = inwersja;
		this.Rotation = rotacja;
		this.Height = Height;
	}

}
