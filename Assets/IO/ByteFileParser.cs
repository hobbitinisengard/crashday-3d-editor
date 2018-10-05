using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ByteFileParser
{
	private int _readOffset;

	public List<byte> Data;
	public byte[] DataBytes;

	public ByteFileParser(List<byte> data)
    {
	    Data = data;
	    DataBytes = data.ToArray();
    }


	public void SetReadingOffest(int newOffset)
	{
		_readOffset = newOffset;
	}

	public void AddReadingOffset(int newOffest)
	{
		_readOffset += newOffest;
	}

	public void ResetData()
	{
		Data = new List<byte>();
		DataBytes = Data.ToArray();
	}
	
	//=======================
	//	READ/WRITE STUFF HERE
	//=======================

	/// <summary>
	/// Write amount of zero bytes
	/// </summary>
	/// <param name="amount">amount of zero bytes</param>
	public void WriteEmpty(int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			Data.Add(0);
		}
	}

	/// <summary>
	/// Write a string without null termination. This is used in CD format to check if file is not corrupted
	/// </summary>
	/// <param name="str">string to add</param>
	public void WriteFlag(string str)
	{
		Data.AddRange(Encoding.UTF8.GetBytes(str));
	}

	/// <summary>
	/// Write a string and null-terminate it.
	/// </summary>
	/// <param name="str"></param>
	public void WriteString(string str)
	{
		WriteFlag(str);
		//Data.AddRange(Encoding.UTF8.GetBytes("\0"));
		Data.Add(0);
	}

    public string ReadString()
    {
        for (var i = _readOffset; i < Data.Count; i++)
        {
            if (Data[i] == 0)
            {
                var oldOffset = _readOffset;
	            _readOffset = i + 1;
                return Encoding.UTF8.GetString(Data.ToArray(), oldOffset, i - oldOffset);
            }
        }
        return "";
    }


	public void WriteInt(int i)
	{
		Data.AddRange(BitConverter.GetBytes(i));
	}

    public int ReadInt()
    {
	    _readOffset += 4;
        return BitConverter.ToInt32(DataBytes, _readOffset - 4);
    }


	public void WriteUInt(uint i)
	{
		Data.AddRange(BitConverter.GetBytes(i));
	}

    public uint ReadUInt()
    {
	    _readOffset += 4;
        return BitConverter.ToUInt32(DataBytes, _readOffset - 4);
    }


	public void WriteUShort(ushort i)
	{
		Data.AddRange(BitConverter.GetBytes(i));
	}

    public ushort ReadUShort()
    {
	    _readOffset += 2;
        return BitConverter.ToUInt16(DataBytes, _readOffset - 2);
    }


	public void WriteShort(short i)
	{
		Data.AddRange(BitConverter.GetBytes(i));
	}

    public short ReadShort()
    {
	    _readOffset += 2;
        return BitConverter.ToInt16(DataBytes, _readOffset - 2);
    }


	public void WriteFloat(float i)
	{
		Data.AddRange(BitConverter.GetBytes(i));
	}

    public float ReadFloat()
    {
	    _readOffset += 4;
        return BitConverter.ToSingle(DataBytes, _readOffset - 4);
    }


	public void WriteVector3(Vector3 i)
	{
		WriteFloat(i.x);
		WriteFloat(i.y);
		WriteFloat(i.z);
	}

    public Vector3 ReadVector3()
    {
        return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
    }


	public void WriteByte(byte i)
	{
		Data.Add(i);
	}

    public byte ReadByte()
    {
	    _readOffset += 1;
        return Data[_readOffset-1];
    }


	public void WriteChar(char i)
	{
		Data.AddRange(BitConverter.GetBytes(i));
	}

    public char ReadChar()
    {
	    _readOffset += 1;
        return BitConverter.ToChar(DataBytes, _readOffset - 1);
    }
}
