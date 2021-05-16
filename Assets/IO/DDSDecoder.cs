using UnityEngine;
using UnityEditor;
using System;

public class DDSDecoder : ScriptableObject
{
  public static Texture2D LoadTextureDXT(byte[] ddsBytes, TextureFormat textureFormat)
  {
    if (textureFormat != TextureFormat.DXT1 && textureFormat != TextureFormat.DXT5)
      throw new Exception("Invalid TextureFormat. Only DXT1 and DXT5 formats are supported by this method.");

    byte ddsSizeCheck = ddsBytes[4];
    if (ddsSizeCheck != 124)
      throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

    int height = ddsBytes[13] * 256 + ddsBytes[12];
    int width = ddsBytes[17] * 256 + ddsBytes[16];

    int DDS_HEADER_SIZE = 128;
    byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
    Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

    Texture2D texture = new Texture2D(width, height, textureFormat, false);
    texture.LoadRawTextureData(dxtBytes);
    texture.Apply();

    return (texture);
  }
}