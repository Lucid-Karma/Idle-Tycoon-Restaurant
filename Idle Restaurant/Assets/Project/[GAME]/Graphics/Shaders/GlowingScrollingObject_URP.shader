Shader "Custom/UnlitScrollingTexture"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Float) = 1.0
        _ScrollDirection ("Scroll Direction", Vector) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

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
                float2 scrollOffset = _ScrollDirection.xy * _Time.y * _ScrollSpeed;
                o.uv = v.uv + scrollOffset;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_BaseMap, i.uv);
            }
            ENDCG
        }
    }
}