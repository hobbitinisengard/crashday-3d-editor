//Place onto the object you want to see things through i.e. window / hole 

Shader "Custom/CustomDepthShader" {
  SubShader {
    Tags {"Queue" = "Geometry+10" }
    Lighting Off
    ZTest LEqual
    ZWrite On
    ColorMask 0
    Pass {}
  }
}