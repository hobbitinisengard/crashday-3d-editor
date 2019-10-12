using System.Collections.Generic;
using UnityEngine;

public class P3DParser
{
  public static P3DModel LoadFromFile(string path)
  {
    List<byte> data = new List<byte>(System.IO.File.ReadAllBytes(path));
    return LoadFromBytes(data);
  }

  public static P3DModel LoadFromBytes(List<byte> data)
  {
    P3DModel model = new P3DModel();
    ByteFileParser io = new ByteFileParser(data);
    //skip 'p3d' text and version, cause i am a bad guy
    io.SetReadingOffest(4);

    model.P3DLength = io.ReadFloat();
    model.P3DHeight = io.ReadFloat();
    model.P3DDepth = io.ReadFloat();

    //skip 'tex' text
    io.AddReadingOffset(3);
    //4 reserved bytes
    io.AddReadingOffset(4);

    model.P3DNumTextures = io.ReadByte();

    model.P3DRenderInfo = new P3DModel.RenderInfo[model.P3DNumTextures];
    for (int i = 0; i < model.P3DNumTextures; i++)
    {
      P3DModel.RenderInfo newRenderInfo = new P3DModel.RenderInfo();
      newRenderInfo.TextureFile = io.ReadString().ToLower();
      model.P3DRenderInfo[i] = newRenderInfo;
    }

    //skip lights test
    io.AddReadingOffset(6);
    //4 reserved bytes
    io.AddReadingOffset(4);

    model.P3DNumLights = io.ReadShort();
    model.P3DLights = new P3DModel.P3DLight[model.P3DNumLights];
    for (int i = 0; i < model.P3DNumLights; i++)
    {
      P3DModel.P3DLight newP3DLight = new P3DModel.P3DLight();

      newP3DLight.Name = io.ReadString();

      newP3DLight.Pos = io.ReadVector3();

      newP3DLight.Range = io.ReadFloat();
      newP3DLight.Color = io.ReadInt();

      newP3DLight.ShowCorona = io.ReadByte();
      newP3DLight.ShowLensFlares = io.ReadByte();
      newP3DLight.LightUpEnvironment = io.ReadByte();
      model.P3DLights[i] = newP3DLight;
    }

    //skip meshes test
    io.AddReadingOffset(6);
    //4 reserved bytes
    io.AddReadingOffset(4);

    model.P3DNumMeshes = io.ReadShort();
    model.P3DMeshes = new P3DModel.P3DMesh[model.P3DNumMeshes];
    for (int m = 0; m < model.P3DNumMeshes; m++)
    {
      //skip submesh test
      io.AddReadingOffset(7);
      //4 reserved bytes
      io.AddReadingOffset(4);

      P3DModel.P3DMesh newP3DMesh = new P3DModel.P3DMesh(model.P3DNumTextures);

      newP3DMesh.Name = io.ReadString();

      newP3DMesh.Flags = io.ReadUInt();
      newP3DMesh.LocalPos = io.ReadVector3();

      newP3DMesh.Length = io.ReadFloat();
      newP3DMesh.Height = io.ReadFloat();
      newP3DMesh.Depth = io.ReadFloat();

      for (int i = 0; i < model.P3DNumTextures; i++)
      {
        P3DModel.TextureInfo newTextureInfo = new P3DModel.TextureInfo();

        newTextureInfo.TextureStart = io.ReadShort();
        newTextureInfo.NumFlat = io.ReadShort();
        newTextureInfo.NumFlatMetal = io.ReadShort();
        newTextureInfo.NumGouraud = io.ReadShort();
        newTextureInfo.NumGouraudMetal = io.ReadShort();
        newTextureInfo.NumGouraudMetalEnv = io.ReadShort();
        newTextureInfo.NumShining = io.ReadShort();
        newP3DMesh.TextureInfos[i] = newTextureInfo;
      }


      newP3DMesh.NumVertices = io.ReadShort();
      newP3DMesh.Vertex = new Vector3[newP3DMesh.NumVertices];
      for (int i = 0; i < newP3DMesh.NumVertices; i++)
      {
        newP3DMesh.Vertex[i] = io.ReadVector3();
      }


      newP3DMesh.NumPolys = io.ReadShort();
      newP3DMesh.Poly = new P3DModel.P3DTexPolygon[newP3DMesh.NumPolys];
      for (int i = 0; i < newP3DMesh.NumPolys; i++)
      {
        P3DModel.P3DTexPolygon newP3DTexPolygon = new P3DModel.P3DTexPolygon();

        newP3DTexPolygon.P1 = io.ReadShort();
        newP3DTexPolygon.U1 = io.ReadFloat();
        newP3DTexPolygon.V1 = io.ReadFloat();

        newP3DTexPolygon.P2 = io.ReadShort();
        newP3DTexPolygon.U2 = io.ReadFloat();
        newP3DTexPolygon.V2 = io.ReadFloat();

        newP3DTexPolygon.P3 = io.ReadShort();
        newP3DTexPolygon.U3 = io.ReadFloat();
        newP3DTexPolygon.V3 = io.ReadFloat();

        newP3DMesh.Poly[i] = newP3DTexPolygon;
      }


      for (int i = 0; i < model.P3DNumTextures; i++)
      {
        short PolyInTex = newP3DMesh.TextureInfos[i].TextureStart;

        for (int n = 0; n < newP3DMesh.TextureInfos[i].NumFlat; n++)
        {
          newP3DMesh.Poly[PolyInTex + n].Material = P3DModel.P3DMaterial.Flat;
          newP3DMesh.Poly[PolyInTex + n].Texture = model.P3DRenderInfo[i].TextureFile;
        }
        PolyInTex += newP3DMesh.TextureInfos[i].NumFlat;


        for (int n = 0; n < newP3DMesh.TextureInfos[i].NumFlatMetal; n++)
        {
          newP3DMesh.Poly[PolyInTex + n].Material = P3DModel.P3DMaterial.FlatMetal;
          newP3DMesh.Poly[PolyInTex + n].Texture = model.P3DRenderInfo[i].TextureFile;
        }
        PolyInTex += newP3DMesh.TextureInfos[i].NumFlatMetal;

        for (int n = 0; n < newP3DMesh.TextureInfos[i].NumGouraud; n++)
        {
          newP3DMesh.Poly[PolyInTex + n].Material = P3DModel.P3DMaterial.Gouraud;
          newP3DMesh.Poly[PolyInTex + n].Texture = model.P3DRenderInfo[i].TextureFile;
        }
        PolyInTex += newP3DMesh.TextureInfos[i].NumGouraud;

        for (int n = 0; n < newP3DMesh.TextureInfos[i].NumGouraudMetal; n++)
        {
          newP3DMesh.Poly[PolyInTex + n].Material = P3DModel.P3DMaterial.GouraudMetal;
          newP3DMesh.Poly[PolyInTex + n].Texture = model.P3DRenderInfo[i].TextureFile;
        }
        PolyInTex += newP3DMesh.TextureInfos[i].NumGouraudMetal;

        for (int n = 0; n < newP3DMesh.TextureInfos[i].NumGouraudMetalEnv; n++)
        {
          newP3DMesh.Poly[PolyInTex + n].Material = P3DModel.P3DMaterial.GouraudMetalEnv;
          newP3DMesh.Poly[PolyInTex + n].Texture = model.P3DRenderInfo[i].TextureFile;
        }
        PolyInTex += newP3DMesh.TextureInfos[i].NumGouraudMetalEnv;

        for (int n = 0; n < newP3DMesh.TextureInfos[i].NumShining; n++)
        {
          newP3DMesh.Poly[PolyInTex + n].Material = P3DModel.P3DMaterial.Shining;
          newP3DMesh.Poly[PolyInTex + n].Texture = model.P3DRenderInfo[i].TextureFile;
        }
      }
      model.P3DMeshes[m] = newP3DMesh;
    }

    //skip user test
    io.AddReadingOffset(4);
    //4 reserved bytes
    io.AddReadingOffset(4);

    model.P3DUserDataSize = io.ReadInt();

    if (model.P3DUserDataSize != 0)
    {
      model.P3DUserDataPtr = model.P3DUserDataSize.ToString();
    }
    else
    {
      model.P3DUserDataPtr = "";
    }
    return model;
  }
}
