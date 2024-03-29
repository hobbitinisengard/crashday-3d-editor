﻿using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class DynamicObjectSavable
{
    //uint16_t objid, vector3 pos, float rotation
	[XmlAttribute("id")]
    public ushort ObjectId;
	[XmlAttribute("position")]
    public Vector3 Position;
	[XmlAttribute("rotation")]
    public float Rotation;

    public DynamicObjectSavable()
    { }

    public DynamicObjectSavable(DynamicObjectSavable old)
    {
        ObjectId = old.ObjectId;
        Position = old.Position;
        Rotation = old.Rotation;
    }
}
