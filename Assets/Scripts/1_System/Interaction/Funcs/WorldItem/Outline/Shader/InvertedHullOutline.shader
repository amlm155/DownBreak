Shader "DownBreak/Interact/InvertedHullOutline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (1.15, 0.72, 0.18, 0.95)
        _OutlineWidth ("Outline Width Pixels", Range(0.5, 8.0)) = 2.5
        _GlowIntensity ("Glow Intensity", Range(0.5, 3.0)) = 1.35
        _PulseSpeed ("Pulse Speed", Range(0.0, 8.0)) = 2.2
        _PulseAmount ("Pulse Amount", Range(0.0, 0.2)) = 0.055
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
        }

        Pass
        {
            Name "InteractOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _GlowIntensity;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.viewDirectionWS = _WorldSpaceCameraPos.xyz - positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = normalize(input.viewDirectionWS);
                half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
                half rimPower = lerp(7.0h, 1.25h, saturate(_OutlineWidth / 8.0h));
                half rim = pow(fresnel, rimPower);
                half pulse = 1.0h + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                half highlightStrength = (0.32h + rim * 1.35h) * _GlowIntensity * pulse;
                half3 color = _OutlineColor.rgb * highlightStrength;
                return half4(color, _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
