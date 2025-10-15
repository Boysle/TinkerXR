/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

Shader "MixedReality/SceneMeshDefault" {
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Angle ("Effect Angle", Range (0, 90)) = 60
        [Enum(Off,0,On,1)] _SceneMeshZWrite("Self Occlude", Float) = 0 //"Off"
    }
    SubShader
    {
        PackageRequirements {"com.unity.render-pipelines.universal"}
        Pass
        {
            Tags { "Queue"="Transparent" "RenderQueue"="3001" "LightMode" = "UniversalForward"}
            LOD 100
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                half3 normal : NORMAL;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float4 _Color;
            float _Angle;
            Varyings vert (Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(input.vertex.xyz);
                o.worldPos = mul(unity_ObjectToWorld, input.vertex);
                o.normal = TransformObjectToWorldNormal(input.normal.xyz);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }
            half GetCoordFromPosition(float worldPos, half offset)
            {
                half coordValue = saturate(fmod(abs(worldPos),1));
                coordValue = abs((coordValue * 2)-1);
                return coordValue;
            }
            half4 frag ( Varyings i, float facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float backFace =  facing > 0 ? 1 : 0.1;
                // edging effect (vertical surfaces, horizontal surfaces except floor/ceiling)
                float edgeGradient = max(GetCoordFromPosition(i.uv.x, 0), GetCoordFromPosition(i.uv.y, 0));
                float stroke = step(0.99,edgeGradient);
                float glow = saturate((edgeGradient - 0.75)*4);
                half4 edgeEffect = _Color * (stroke + pow(glow,4) + 0.1);
                // ground effect (horizontal surfaces)
                float uGrid = GetCoordFromPosition(i.worldPos.x, 0);
                float vGrid = GetCoordFromPosition(i.worldPos.z, 0);
                float groundGradient = max(uGrid, vGrid);
                float uOffset = GetCoordFromPosition(i.worldPos.x, 0.5);
                float vOffset = GetCoordFromPosition(i.worldPos.z, 0.5);
                float gridOffset = min(uOffset, vOffset);
                float groundGrid = step(0.99, groundGradient) * step(0.8, gridOffset);
                float groundGlow = smoothstep(0.8, 0.99, groundGradient) * smoothstep(0.5, 1, gridOffset);
                half4 floorEffect = edgeEffect + _Color * groundGrid + _Color * (groundGlow * 0.25 + 0.2);
                // render the "floor" version only on horizontal surfaces
                float groundMask = acos(abs(dot(i.normal.xyz, float3(0,1,0))));
                groundMask = step((_Angle/90) * 3.14159265 * 0.5,groundMask);
                half4 finalEffect = lerp(floorEffect, edgeEffect, groundMask) * backFace;
                return finalEffect;
            }
            ENDHLSL
        }
    }
    /// BiRP
    SubShader
    {
        Pass
        {
            Tags { "Queue"="Transparent" }
            LOD 100
            Cull Off
            ZWrite [_SceneMeshZWrite]
            Blend One One
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 finalEffect = fixed4(0,0,0,0);
                return finalEffect;
            }
            ENDCG
        }

        Pass
        {
            Tags { "Queue"="Transparent" }
            LOD 100
            Cull Off
            ZWrite Off
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                half3 normal : NORMAL;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _Color;
            float _Angle;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed GetCoordFromPosition(float worldPos, fixed offset)
            {
                fixed coordValue = saturate(fmod(abs(worldPos),1));
                coordValue = abs((coordValue * 2)-1);
                return coordValue;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float PI = 3.14159265;
                float backFace =  facing > 0 ? 1 : 0.1;

                // edging effect (vertical surfaces, horizontal surfaces except floor/ceiling)
                float edgeGradient = max(GetCoordFromPosition(i.uv.x, 0), GetCoordFromPosition(i.uv.y, 0));
                float stroke = step(0.99,edgeGradient);
                float glow = saturate((edgeGradient - 0.75)*4);
                fixed4 edgeEffect = _Color * (stroke + pow(glow,4) + 0.1);

                // ground effect (horizontal surfaces)
                float uGrid = GetCoordFromPosition(i.worldPos.x, 0);
                float vGrid = GetCoordFromPosition(i.worldPos.z, 0);
                float groundGradient = max(uGrid, vGrid);

                float uOffset = GetCoordFromPosition(i.worldPos.x, 0.5);
                float vOffset = GetCoordFromPosition(i.worldPos.z, 0.5);
                float gridOffset = min(uOffset, vOffset);

                float groundGrid = step(0.99, groundGradient) * step(0.8, gridOffset);
                float groundGlow = smoothstep(0.8, 0.99, groundGradient) * smoothstep(0.5, 1, gridOffset);
                fixed4 floorEffect = edgeEffect + _Color * groundGrid + _Color * (groundGlow * 0.25 + 0.2);

                // render the "floor" version only on horizontal surfaces
                float groundMask = acos(abs(dot(i.normal.xyz, float3(0,1,0))));
                groundMask = step((_Angle/90) * PI * 0.5,groundMask);

                fixed4 finalEffect = lerp(floorEffect, edgeEffect, groundMask) * backFace;
                return finalEffect;
            }
        ENDCG
        }
    }
}
