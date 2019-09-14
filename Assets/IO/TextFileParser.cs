using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextFileParser
{
	private int _currentReadLine;

	private string[] _strings;

	public TextFileParser(string[] strings)
	{
		_currentReadLine = 0;
		_strings = strings;
	}

	/// <summary>
	/// Skip amount of lines from reading.
	/// 0 = no skip
	/// Can be negative
	/// </summary>
	/// <param name="amount"></param>
	public void Skip(int amount = 1)
	{
		_currentReadLine += amount;
	}

	public float ReadFloat()
	{
		float f;
		float.TryParse(IO.RemoveComment(_strings[_currentReadLine]), out f);
		_currentReadLine += 1;
		return f;
	}

	public string ReadString()
	{
		_currentReadLine += 1;
		return IO.RemoveComment(_strings[_currentReadLine-1]);
	}

	public int ReadInt()
	{
		int i;
		int.TryParse(IO.RemoveComment(_strings[_currentReadLine]), out i);
		_currentReadLine += 1;
		return i;
	}

	public Vector2 ReadVector2()
	{
		string[] s = IO.RemoveComment(_strings[_currentReadLine]).Split();
		_currentReadLine += 1;
		float x, y;
		float.TryParse(s[0], out x);
		float.TryParse(s[1], out y);
		return new Vector2(x, y);
	}

	public Vector3 ReadVector3()
	{
		string[] s = IO.RemoveComment(_strings[_currentReadLine]).Split();
		_currentReadLine += 1;
		float x, y, z;
		float.TryParse(s[0], out x);
		float.TryParse(s[1], out y);
		float.TryParse(s[2], out z);
		return new Vector3(x, y, z);
	}

	public Color ReadColor()
	{
		string[] s = IO.RemoveComment(_strings[_currentReadLine]).Split();
		_currentReadLine += 1;
		int r, g, b;
		int.TryParse(s[0], out r);
		int.TryParse(s[1], out g);
		int.TryParse(s[2], out b);
		return new Color(r/255.0f, g/255.0f, b/255.0f, 1.0f);
	}

}
