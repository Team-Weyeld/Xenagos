Shader "Tricolor sprite"
{
	Properties
	{
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		_Tint ("Tint", Color) = (1,1,1,1)
		_Color0 ("Color 1", Color) = (1,0,0,1)
		_Color1 ("Color 2", Color) = (0,1,0,1)
		_Color2 ("Color 3", Color) = (0,0,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Tint;
			float4 _Color0;
			float4 _Color1;
			float4 _Color2;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 input = tex2D(_MainTex, i.uv);

				float4 resultColor = float4(0, 0, 0, 0);
				resultColor = lerp(resultColor, _Color0, input[0] * _Color0.a);
				resultColor = lerp(resultColor, _Color1, input[1] * _Color1.a);
				resultColor = lerp(resultColor, _Color2, input[2] * _Color2.a);
				resultColor.a = input.a;

				float4 output = resultColor * _Tint;

				return output;
			}

			ENDCG
		}
	}
}
