Shader "Custom/GlowingScrollingObject_URP"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.0
        _ScrollSpeed ("Scroll Speed", float) = 1.0
        _ScrollDirection ("Scroll Direction", Vector) = (1, 0, 0, 0)
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };
            
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseColor;
            float4 _GlowColor;
            float _GlowIntensity;
            float _ScrollSpeed;
            float4 _ScrollDirection;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                float2 scrollOffset = _ScrollDirection.xy * _Time.y * _ScrollSpeed;
                o.uv = v.uv + scrollOffset;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float4 glowEffect = _GlowColor * _GlowIntensity;
                return baseTex + glowEffect;
            }
            
            ENDHLSL
        }
    }
}
