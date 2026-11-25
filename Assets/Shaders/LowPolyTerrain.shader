// Flat-shaded terrain: every triangle gets one solid color, picked from its slope and height.
// Steep faces are rock, high faces get snow, the lowlands are mossy grass with black ash fields.
Shader "JetGame/Low Poly Terrain"
{
    Properties
    {
        _GrassColor ("Grass", Color) = (0.3, 0.4, 0.2, 1)
        _AshColor ("Ash", Color) = (0.15, 0.15, 0.15, 1)
        _RockColor ("Rock", Color) = (0.21, 0.21, 0.22, 1)
        _SnowColor ("Snow", Color) = (0.88, 0.9, 0.92, 1)
        _SnowHeight ("Snow Height", Float) = 1500
        _RockHeight ("Rock Height", Float) = 900
        _RockSlope ("Rock When Normal Y Below", Range(0, 1)) = 0.8
        _SnowSlope ("Snow When Normal Y Above", Range(0, 1)) = 0.55
        _FaceVariation ("Face Color Variation", Range(0, 0.3)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-100" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GrassColor, _AshColor, _RockColor, _SnowColor;
                float _SnowHeight, _RockHeight, _RockSlope, _SnowSlope, _FaceVariation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                nointerpolation float3 cornerWS : TEXCOORD1; // one corner of the triangle, the same for the whole face
                float fogFactor : TEXCOORD2;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = smoothstep(0, 1, frac(p));
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                Varyings output;
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.cornerWS = positions.positionWS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Face normal from the screen-space derivatives: flat per triangle. Terrain always faces up.
                float3 normal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                normal *= sign(normal.y);

                float2 xz = input.cornerWS.xz;
                float height = input.cornerWS.y;
                float patches = ValueNoise(xz / 3000) * 0.7 + ValueNoise(xz / 600) * 0.3;
                float ragged = (ValueNoise(xz / 400 + 50) - 0.5) * 400;

                half3 color = patches > 0.62 ? _AshColor.rgb : _GrassColor.rgb;
                if (normal.y < _RockSlope || height + ragged > _RockHeight) color = _RockColor.rgb;
                if (height + ragged > _SnowHeight && normal.y > _SnowSlope) color = _SnowColor.rgb;
                color *= 1 + (Hash(xz * 0.01) - 0.5) * 2 * _FaceVariation;

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = sun.color * saturate(dot(normal, sun.direction)) * sun.shadowAttenuation + SampleSH(normal);
                return half4(MixFog(color * light, input.fogFactor), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
