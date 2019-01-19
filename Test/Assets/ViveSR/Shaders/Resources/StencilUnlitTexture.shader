// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ViveSR/Unlit, Textured, Stencil"
{
	Properties
	{
		_MainTex ("Base Texture", 2D) = "white" {}
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", int) = 1										// default
		_StencilValue ("StencilRefValue", float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Compare", int) = 0	// disable

	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		ZTest Always
		ZWrite [_ZWrite]

		Stencil{
			Ref [_StencilValue]
			Comp[_StencilComp]
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;

			struct vInput
			{
				float4 pos : POSITION;			
				float2 uvCoord : TEXCOORD0;	
			};

			struct fInput
			{
				float4 pos : SV_POSITION;
				float2 uvCoord : TEXCOORD0;	
			};

			fInput vert (vInput vIn)
			{
				fInput vOut;			

				vOut.pos = UnityObjectToClipPos(vIn.pos);				
				vOut.uvCoord = vIn.uvCoord;

				return vOut;
			}
			
			float4 frag (fInput fIn) : SV_Target
			{
				return tex2D(_MainTex, fIn.uvCoord);
			}
			ENDCG
		}
	}
}
