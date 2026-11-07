Shader "Wind/Experiments/HierarchyWind"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.28,0.58,0.22,1)
        [NoScaleOffset] _HWData("Pivot Tip Weight Data", 2D) = "black" {}
        _HWRoot("Root Pivot Object Space", Vector) = (0,0,0,0)
        _HWBakeMin("Bake Bounds Min", Vector) = (0,0,0,0)
        _HWBakeSize("Bake Bounds Size", Vector) = (1,1,1,0)
        [HideInInspector] _HWResponse("Response", Vector) = (0,0,0,0)
        [HideInInspector] _HWDelayed("Delayed", Vector) = (0,0,0,0)
        [HideInInspector] _HWAngles("Angles", Vector) = (18,24,0.15,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "HierarchyWindDeform.hlsl"
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 p = input.positionOS.xyz;
            float3 n = input.normalOS;
            HWDeform(p,n,input.color,input.vertexID);
            output.positionWS = TransformObjectToWorld(p);
            output.normalWS = TransformObjectToWorldNormal(n);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            return output;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            float3 n = normalize(input.normalWS);
            Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half3 diffuse = light.color * saturate(dot(n,light.direction)) *
                            light.distanceAttenuation * light.shadowAttenuation;
            return half4(_BaseColor.rgb * (SampleSH(n) + diffuse), _BaseColor.a);
        }
        half4 DepthFrag(Varyings input) : SV_Target { return 0; }
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings ShadowVert(Attributes input)
            {
                Varyings output = Vert(input);
                float3 lightDirection = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDirection = normalize(_LightPosition-output.positionWS);
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS,
                    normalize(output.normalWS),lightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                return output;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    Fallback Off
}
