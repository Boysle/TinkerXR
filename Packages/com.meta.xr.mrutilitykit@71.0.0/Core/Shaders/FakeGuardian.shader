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

Shader "Meta/MRUK/MixedReality/FakeGuardian" {
    Properties
    {
        _WallScale("Wall Scale", Vector) = (1,1,0,0)
        _GuardianFade("Fade Amount", Range(0 , 1)) = 0

        [Header(DepthTest)]
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0 //"Off"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpColor("Blend Color", Float) = 2 //"ReverseSubtract"
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha("Blend Alpha", Float) = 3 //"Min"
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2 //"Back"
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            ZWrite[_ZWrite]
            ZTest[_ZTest]
            Cull [_CullMode]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            float2 fresnel : TEXCOORD2;
            UNITY_VERTEX_OUTPUT_STEREO //Insert

            };

            uniform float2 _WallScale;
            float _GuardianFade;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); //Insert
            UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            float3 i = normalize(ObjSpaceViewDir(v.vertex));
            o.fresnel.x = step(0,dot(i, v.normal));
            o.fresnel.y = 1-step(0.5,abs(dot(v.normal, float3(0,1,0))));
                return o;
            }

               fixed4 frag(v2f i) : SV_Target {
                float xTile =  (abs(i.uv.x * 2 * _WallScale.x) % 1);
                float yTile = (abs(i.uv.y * 2 * _WallScale.y) % 1);
            float gridWidth = lerp(0.5, 0.48, saturate(_GuardianFade*4)) + 0.005; // minor offset to account for pixel precision
                float grid = step(gridWidth, abs(xTile - 0.5));
                grid = max(grid, step(gridWidth, abs(yTile - 0.5)));
                float cornerFade = lerp(0.35, 0.0, saturate(_GuardianFade-0.25)*2);
                float corners = step(cornerFade, abs(xTile - 0.5));
                corners *= step(cornerFade, abs(yTile - 0.5));
            grid *= corners;

                float camProximity = distance(i.worldPos, _WorldSpaceCameraPos);
                float redRing = step(0.5, 1-abs((saturate(camProximity) - 0.5) * 30));
            float ptHole = step(0.5, camProximity);
            float3 guardianBlue = lerp(float3(0, 0.5, 1), float3(0, 0.3, 0.6), saturate(camProximity * 0.5));
                float3 gridColor = lerp(float3(1, 0, 0), guardianBlue, smoothstep(0.5,0.6,saturate(camProximity)));
                float finalAlpha = saturate((grid * ptHole) + (_GuardianFade * 10 * redRing));
            clip(finalAlpha - 0.02);
                return float4(gridColor.r, gridColor.g, gridColor.b, finalAlpha);
            }
            ENDCG
        }

        // this renders Passthrough when poking your head outside of the Guardian
        Pass
        {
            ZTest Always
            //Cull Front
            BlendOp RevSub
            Blend Zero One, One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO //Insert
            };

            v2f vert(appdata v)
            {
                v2f o;
                                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float camProximity = distance(i.worldPos, _WorldSpaceCameraPos);
                float redRing = step(0.5, 1-abs((saturate(camProximity) - 0.5) * 30));
                float ptHole = 1-step(0.5, camProximity);
                clip(ptHole-0.5);
                return float4(0, 0, 0, ptHole);
            }
            ENDCG
        }
    }
}
