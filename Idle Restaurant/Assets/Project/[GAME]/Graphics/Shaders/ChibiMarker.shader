// A flat colour that is drawn on top of everything, ignoring the depth buffer.
// For things that point at the game from outside it: the first-shift tutorial's arrow. A normal 3D arrow
// hangs in the kitchen like any other object, and the kitchen is full of things that hide it - the range
// hood above the stove swallowed it whenever it pointed at the pan. This one cannot be hidden.
// Back faces are culled, so a closed convex shape (the arrowhead) needs no depth sorting of its own.
// Two materials make an outlined arrow: a dark copy slightly larger drawn first (render queue 3999), the
// bright one over it (4000).
Shader "Chibi/Marker"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 0.8, 0.25, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Overlay" }

        Pass
        {
            Name "Marker"
            Tags { "LightMode" = "UniversalForward" }
            ZTest Always
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
