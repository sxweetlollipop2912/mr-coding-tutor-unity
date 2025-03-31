Shader "Custom/NewVideoRender360" {
	Properties {
		_Color("Main Color", Color) = (1, 1, 1, 1)
		_MainTex("Render Texture", 2D) = "white" {}
	}

	SubShader {
		Tags { 
			"LightMode" = "Always" 
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
		}
		// Tags { "RenderType" = "Opaque" }
		
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull front    // FLIP THE SURFACES
		
		LOD 100

		Pass {
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma glsl
			#pragma target 3.0

			#include "UnityCG.cginc"

			#define PI 3.141592653589793

			struct appdata {
				// float2 texcoord : TEXCOORD0;
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				// float4 vertex : SV_POSITION;
				// half2 texcoord : TEXCOORD0;
				float4 pos : SV_POSITION;
				float3 normal : TEXCOORD0;
			};

			sampler2D _MainTex;
			// float4 _MainTex_ST;

			inline float2 RadialCoords(float3 a_coords)
			{
				float3 a_coords_n = normalize(a_coords);
				float lon = atan2(a_coords_n.z, a_coords_n.x);
				float lat = acos(a_coords_n.y);
				float2 sphereCoords = float2(lon, lat) * (1.0 / PI);
				return float2(1 - (sphereCoords.x * 0.5 + 0.5), 1 - sphereCoords.y);
			}

			v2f vert(appdata v)
			{
				v2f o;
				// o.vertex = UnityObjectToClipPos(v.vertex);
				// v.texcoord.x = 1 - v.texcoord.x;
				// o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				o.pos = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;

				return o;
			}

			// fixed4 frag(v2f i) : SV_Target
			// {
			//  	fixed4 col = tex2D(_MainTex, i.texcoord);
			//  	return col;
			// }

			float4 frag(v2f IN) : COLOR
			{
				float2 equiUV = RadialCoords(IN.normal);
				return tex2D(_MainTex, equiUV);
			}

			ENDCG
		}
	}

	FallBack "VertexLit"
}