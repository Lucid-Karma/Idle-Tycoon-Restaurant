Shader "Custom/UnlitScrollingTexture"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Float) = 1.0
        _ScrollDirection ("Scroll Direction", Vector) = (1, 0, 0, 0)
        // The themes' strips, kept here so a build carries them like _BaseMap (SeasonalDecor.Screens swaps one in;
        // loaded from Resources they drew black on the web)
        [NoScaleOffset] _Halloween ("Halloween strip", 2D) = "white" {}
        [NoScaleOffset] _Winter ("Winter strip", 2D) = "white" {}
        [NoScaleOffset] _Valentine ("Valentine strip", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Offset -1, -1   // a hair in front of the frame it sits in (phones' shallower depth buffers made them flicker)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float _ScrollSpeed;
            float4 _ScrollDirection;

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

            v2f vert (appdata v)
            {
                v2f o;
                // wrapped (the strip repeats): _Time grows all session, and on a phone's lower precision a large
                // offset turned the letters into flickering bits (user, 2026-10-28)
                float2 scrollOffset = frac(_ScrollDirection.xy * _ScrollSpeed * _Time.y);
                o.uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + scrollOffset;   // (tiling: SeasonalDecor.Screens)
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(tex2D(_BaseMap, i.uv).rgb, 1);   // (a solid panel)
            }
            ENDCG
        }
    }
}