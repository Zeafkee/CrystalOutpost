Shader "CrystalOutpost/Toon"
{
    Properties
    {
        _Anim ("Anim (phase, flash, attack, -)", Vector) = (0, 0, 0, 0)
        _EmissionBoost ("Emission Boost", Float) = 1.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float3 _CO_LightDir;
            float3 _CO_LightColor;
            float3 _CO_Ambient;

            CBUFFER_START(UnityPerMaterial)
                float _EmissionBoost;
            #ifndef UNITY_INSTANCING_ENABLED
                float4 _Anim;
            #endif
            CBUFFER_END

            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(PerInstance)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _Anim)
                UNITY_INSTANCING_BUFFER_END(PerInstance)
                #define ANIM UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Anim)
            #else
                #define ANIM _Anim
            #endif

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 limb       : TEXCOORD0;
                float3 pivot      : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 color      : TEXCOORD0;
                float  flash      : TEXCOORD1;
            };

            float3 RotateX(float3 v, float angle)
            {
                float s, c;
                sincos(angle, s, c);
                return float3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float4 anim = ANIM;
                float phase = anim.x * 5.0;

                float isLeg = step(0.5, input.limb.x) * step(input.limb.x, 1.5);
                float isArm = step(1.5, input.limb.x) * step(input.limb.x, 2.5);
                float isHeld = step(2.5, input.limb.x);
                float side = input.limb.y;

                float legAngle = sin(phase + (side > 0.0 ? 0.0 : 3.14159)) * 0.75;
                float armAngle = sin(phase + (side > 0.0 ? 3.14159 : 0.0)) * 0.45;
                armAngle -= anim.z * (side > 0.0 ? 1.9 : 0.6);

                float angle = isLeg * legAngle + isArm * armAngle;
                float3 position = input.pivot + RotateX(input.positionOS - input.pivot, angle);
                float3 normal = RotateX(input.normalOS, angle);

                position.z -= isHeld * anim.z * 0.16;
                position.y += isHeld * sin(phase * 2.0) * 0.015 * step(0.001, anim.x);

                position.y += abs(sin(phase)) * 0.05 * step(0.001, anim.x);

                float3 positionWS = TransformObjectToWorld(position);
                float3 normalWS = TransformObjectToWorldNormal(normal);

                float hasLight = step(0.0001, dot(_CO_LightColor, float3(1.0, 1.0, 1.0)));
                float3 lightDir = lerp(normalize(float3(0.35, 0.85, -0.4)), _CO_LightDir, hasLight);
                float3 lightColor = lerp(float3(0.95, 0.9, 1.0), _CO_LightColor, hasLight);
                float3 ambient = lerp(float3(0.32, 0.3, 0.45), _CO_Ambient, hasLight);

                float halfLambert = dot(normalWS, lightDir) * 0.5 + 0.5;
                float3 lit = input.color.rgb * (ambient + lightColor * halfLambert * halfLambert);
                float3 emission = input.color.rgb * input.color.a * _EmissionBoost;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.color = lit + emission;
                output.flash = anim.y;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 color = lerp(input.color, half3(1.0, 1.0, 1.0), input.flash * 0.85);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
