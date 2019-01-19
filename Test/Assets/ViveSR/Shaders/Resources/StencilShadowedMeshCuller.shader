Shader "ViveSR/MeshCuller, Shadowed, Stencil"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_StencilValue ("StencilRefValue", float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Compare", int) = 0	// disable
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry-2" "LightMode"="ForwardBase" }

		Stencil{
			Ref  [_StencilValue]
			Comp [_StencilComp]
		}

		GrabPass{ "_SeeThroughBGTex" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 cPos : TEXCOORD1;
				SHADOW_COORDS(2)				
			};

			sampler2D _MainTex;
			sampler2D _SeeThroughBGTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.cPos = o.pos.xyw;
				TRANSFER_SHADOW(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 screenPos = i.cPos.xy / i.cPos.z;
				screenPos.x = 0.5 + screenPos.x * 0.5;
				screenPos.y = 0.5 - screenPos.y * 0.5;
				fixed4 col = tex2D(_SeeThroughBGTex, screenPos);
				fixed shadow = SHADOW_ATTENUATION(i);
				col.rgb *= shadow;

				return col;
			}
			ENDCG
		}
	}

	FallBack "Standard"
}
