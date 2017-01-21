// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", float) = 1.0
        _FogColor ("Fog Color", color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float scale : FLOAT;
           		float3 normal : TEXCOORD1;
           		float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 worldSpacePosition : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal: TEXCOORD2;
                float3 pos : TEXCOORD3;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            fixed4 _FogColor;
            
            v2f vert (input i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
                o.worldSpacePosition = mul(unity_ObjectToWorld, i.vertex);
                o.normal = mul( float4( i.normal, 0.0 ), unity_WorldToObject ).xyz;
                o.pos = UnityObjectToViewPos(i.vertex);
                o.color = i.color;
                return o;
            }
            
            fixed4 frag (v2f v) : SV_Target
            {
                // sample the texture
                fixed2 lutUV = fixed2(v.worldSpacePosition.g / _Scale, v.color.r);
                fixed4 col = tex2D(_MainTex, lutUV);
                //fixed4 col = v.worldSpacePosition.g;
                // apply fog
                //UNITY_APPLY_FOG(v.fogCoord, col);
                float fog = (length(v.pos)/300)-0.1f;
                col = lerp(col, _FogColor, fog);
                //col = fixed4(v.normal.r, v.normal.g, v.normal.b, col.a);
                return col;
            }
            ENDCG
        }
    }
}