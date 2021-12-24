using System.Xml.Serialization;

[System.Serializable]
public class TrackTileSavable
{
	[XmlAttribute("id")]
    public ushort FieldId;
	[XmlAttribute("rotation")]
    public byte Rotation;
	[XmlAttribute("mirrored")]
    public byte IsMirrored;
	[XmlAttribute("height")]
    public byte Height;
    public TrackTileSavable()
    { }

	public TrackTileSavable(TrackTileSavable old)
	{
		FieldId = old.FieldId;
		Rotation = old.Rotation;
		IsMirrored = old.IsMirrored;
		Height = old.Height;
	}

	public TrackTileSavable(ushort id, byte rotation, byte isMirrored, byte height)
	{
		FieldId = id;
		Rotation = rotation;
		IsMirrored = isMirrored;
		Height = height;
	}
    public void Set(ushort id, byte rotation, byte isMirrored, byte height)
    {
        this.FieldId = id;
        this.Rotation = rotation;
        this.IsMirrored = isMirrored;
        this.Height = height;
    }
}
