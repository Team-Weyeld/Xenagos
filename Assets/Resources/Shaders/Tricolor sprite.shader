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
				float4 inputRaw = tex2D(_MainTex, i.uv);
				float3 input = inputRaw.xyz;
				float alpha = inputRaw.a;

				input[0] *= _Color0.a;
				input[1] *= _Color1.a;
				input[2] *= _Color2.a;

				float value = max(max(input[0], input[1]), input[2]);

				float3 color0 = _Color0.xyz * input[0];
				float3 color1 = _Color1.xyz * input[1];
				float3 color2 = _Color2.xyz * input[2];
				float total = input[0] + input[1] + input[2];
				
				float4 output = float4((color0 + color1 + color2) / total * value, alpha);
//				float4 output = float4((color0 + color1 + color2) * value, alpha);

				output *= _Tint;

				return output;
			}

			ENDCG
		}
	}
}
