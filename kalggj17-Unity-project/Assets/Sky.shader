// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


Shader "Custom/Sky"
{

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", float) = 1.0
        _TopColor ("Top Color", color) = (0,0,1,1)
        _BottomColor ("Bottom Color", color) = (1,0,0,1)
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
            #include "UnityLightingCommon.cginc" // for _LightColor0

            struct input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float scale : FLOAT;
           		//float3 normal : TEXCOORD1;
           		float4 color : COLOR;
                float4 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 worldSpacePosition : TEXCOORD1;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 pos : TEXCOORD3;
                fixed4 color : COLOR;
                float4 worldNormal: NORMAL;
                float4 diff : COLOR1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            fixed4 _TopColor;
            fixed4 _BottomColor;
            
            v2f vert (input i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
                o.worldSpacePosition = mul(unity_ObjectToWorld, i.vertex);

                o.worldNormal = i.normal;

                o.worldNormal = float4(mul( unity_ObjectToWorld, i.normal ).xyz, 0.0);

                o.pos = UnityObjectToViewPos(i.vertex);
                o.color = i.color;

                half nl = max(0, dot(-o.worldNormal, _WorldSpaceLightPos0.xyz));
                // factor in the light color
                o.diff = (nl * _LightColor0) / 50000;


                return o;
            }
            
            fixed4 frag (v2f v) : SV_Target
            {
                fixed4 col = clamp(lerp(_TopColor, _BottomColor, clamp((v.uv.y * 3) - 1, 0, 1)),0,1);
                col += v.diff;
                return col;
            }
            ENDCG
        }
    }
}