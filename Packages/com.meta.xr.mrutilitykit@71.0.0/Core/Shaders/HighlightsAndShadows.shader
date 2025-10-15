// Copyright(c) Meta Platforms, Inc. and affiliates.
// All rights reserved.
//
// Licensed under the Oculus SDK License Agreement (the "License");
// you may not use the Oculus SDK except in compliance with the License,
// which is provided at the time of installation or download, or which
// otherwise accompanies this software in either electronic or hard copy form.
//
// You may obtain a copy of the License at
//
// https://developer.oculus.com/licenses/oculussdk/
//
// Unless required by applicable law or agreed to in writing, the Oculus SDK
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "Meta/MRUK/Scene/HighlightsAndShadows"
{
    Properties
    {
        _ShadowIntensity ("Shadow Intensity", Range (0, 1)) = 0.8
        _HighLightAttenuation ("Highlight Attenuation", Range (0, 1)) = 0.8
        _HighlightOpacity("Highlight Opacity", Range (0, 1)) = 0.2
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal"
        }
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "Queue"="Transparent"
        }
        Pass
        {

            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend One OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex ShadowReceiverVertex
            #pragma fragment ShadowReceiverFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _HighLightAttenuation;
            float _ShadowIntensity;
            float _HighlightOpacity;

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowReceiverVertex(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalize(mul(unity_ObjectToWorld, float4(input.normal, 0.0)).xyz);
                return output;
            }

            half4 ShadowReceiverFragment(const Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 color = half3(0, 0, 0);
                half mainLightShadowAttenuation;

                // Main light shadows.
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = input.positionWS;
                const float4 shadowCoord = GetShadowCoord(vertexInput);
                mainLightShadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                half alpha = (1 - mainLightShadowAttenuation) * _ShadowIntensity;

                //Additional lights highlights.
                float lightAlpha = 0;
                for (int i = 0; i < GetAdditionalLightsCount(); i++) {
                    Light light = GetAdditionalLight(i, input.positionWS, float4(0, 0, 0, 0));
                    float ndtol = saturate(dot(light.direction, input.normalWS));
                    lightAlpha = light.distanceAttenuation * ndtol * _HighLightAttenuation * light.shadowAttenuation;
                    color += light.color * lightAlpha * (1-alpha);
                }
                return half4(color, alpha + (lightAlpha * _HighlightOpacity));
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
        }

        //Accumulate point light contribution
        Pass
        {
            Name "PointLight Contribution"
            Tags
            {
                "LightMode" = "ForwardAdd"
            }
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            uniform float _ShadowIntensity;
            uniform float _HighLightAttenuation;
            uniform float _HighlightOpacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            struct Light {
                half3 direction;
                fixed4 color;
                float distanceAttenuation;
            };

            Light getLight(v2f i) {
                Light light;
                float3 dir;

                #if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
                    dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                #else
                    dir = _WorldSpaceLightPos0.xyz;
                #endif

                light.direction = dir;
                light.color = _LightColor0;
                UNITY_LIGHT_ATTENUATION(attenuation, 0, i.worldPos);
                light.distanceAttenuation = attenuation;
                return light;
            }

            fixed4 frag(v2f i) : COLOR {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                Light light = getLight(i);
                float ndtol = max(0.0, dot(i.normal, light.direction));
                float lightContribution = light.distanceAttenuation * _HighLightAttenuation * ndtol * light.color.w;
                float4 color = light.color * lightContribution;
                float alpha = lightContribution * _HighlightOpacity;
                return fixed4(color.r, color.g, color.b, alpha);
            }
            ENDCG
        }

        //Apply shadow attenuation for the main directionalLight
        Pass
        {
            Name "DirectionalShadows"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            uniform float _ShadowIntensity;
            uniform float _DepthCheckBias;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : COLOR {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float attenuation = UNITY_SHADOW_ATTENUATION(i, i.worldPos);
                float3 lightDirection = _WorldSpaceLightPos0.xyz;
                float ndtol = dot(i.normal, lightDirection);
                int directionCheck = step(0,ndtol);
                float alpha = (1 - attenuation) * _ShadowIntensity * directionCheck;
                return fixed4(0, 0, 0, alpha);
            }
            ENDCG
        }

        // Cast shadows
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma target 2.0

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma skip_variants SHADOWS_SOFT
            #pragma multi_compile_shadowcaster

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }
    }
    Fallback "Off"
}
