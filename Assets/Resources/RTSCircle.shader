Shader "Custom/RTSCircle" {
	Properties{
			_CirclePosition("CirclePosition", Vector) = (0,0,0,0)
			_Length("Length", Float) = 2
			_Thickness("Thickness", Float) = 2
	}
		SubShader{
				Tags {
						"RenderType" = "Opaque"
				}
				LOD 200
				Pass {
						Name "FORWARD"
						Tags {
								"LightMode" = "ForwardBase"
						}


						CGPROGRAM
						#pragma vertex vert
						#pragma fragment frag
						#define UNITY_PASS_FORWARDBASE
						#include "UnityCG.cginc"
						#pragma multi_compile_fwdbase_fullshadows
						#pragma multi_compile_fog
						#pragma only_renderers d3d9 d3d11 glcore gles 
						#pragma target 3.0
						uniform float4 _CirclePosition;
						uniform float _Length;
						uniform float _Thickness;
						struct VertexInput {
								float4 vertex : POSITION;
						};
						struct VertexOutput {
								float4 pos : SV_POSITION;
								float4 posWorld : TEXCOORD0;
								UNITY_FOG_COORDS(1)
						};
						VertexOutput vert(VertexInput v) {
								VertexOutput o = (VertexOutput)0;
								o.posWorld = mul(unity_ObjectToWorld, v.vertex);
								o.pos = UnityObjectToClipPos(v.vertex);
								UNITY_TRANSFER_FOG(o,o.pos);
								return o;
						}
						float4 frag(VertexOutput i) : COLOR {
							////// Lighting:
							////// Emissive:
															float node_1386 = distance(i.posWorld.rgb,_CirclePosition.rgb);
															float node_8493 = (step((node_1386 - _Thickness),_Length) - step(node_1386,_Length));
															float3 emissive = float3(node_8493,node_8493,node_8493);
															float3 finalColor = emissive;
															fixed4 finalRGBA = fixed4(finalColor,1);
															UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
															return finalRGBA;
													}
													ENDCG
											}
	}
		FallBack "Diffuse"
														CustomEditor "ShaderForgeMaterialInspector"
}