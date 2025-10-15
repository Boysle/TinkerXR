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

 // NOTE: for translucent passthrough (e.g. this shader), you MUST have on some script in your scene:
 // OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
Shader "Meta/MRUK/MixedReality/PassthroughReveal" {
    Properties
    {
        _PassthroughAmount ("Passthrough Amount", Range (0, 1)) = 1
        _PassthroughMask ("Passthrough Mask (optional)", 2D) = "white" {}
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0 //"Off"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
    }
        SubShader
        {
            Tags { "Queue" = "Overlay" }

            Pass
            {
                ZTest[_ZTest]
                ZWrite[_ZWrite]
                BlendOp Add, RevSub
                Blend SrcAlpha One, One One

                CGPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag
                        // required for VPOS things
                        #pragma target 3.0

                        #include "UnityCG.cginc"

                        // note: no SV_POSITION in this struct
            struct v2f {
                float2 uv : TEXCOORD0;
            };

            v2f vert (
                float4 vertex : POSITION, // vertex position input
                float2 uv : TEXCOORD0, // texture coordinate input
                out float4 outpos : SV_POSITION // clip space position output
                )
            {
                v2f o;
                o.uv = uv;
                outpos = UnityObjectToClipPos(vertex);
                return o;
            }

            float _PassthroughAmount;
            sampler2D _PassthroughMask;

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
            {
                // screenPos.xy will contain pixel integer coordinates.
                screenPos.xy = floor(screenPos.xy * 0.02) * 0.5;
                // the grid is only used in editor to represent passthrough
                float grid = frac(screenPos.r + screenPos.g) + 0.5;
                fixed mask = tex2D (_PassthroughMask, i.uv).r;
                fixed alpha = _PassthroughAmount * mask;

                #if SHADER_API_GLES3
                // this is designed to work specifically with this shader's RGB BlendMode
                // without an external C# script dependency, it can be tricky to branch a shader based on device
                // using the precompiler (#if, #endif, etc.), we can get pretty close behavior, but they don't all work (such as UNITY_ANDROID)
                // NOTE: this may age poorly (code rot) like anything related to shaders
                grid = 0;
                #endif

                fixed4 rgba = fixed4(grid, grid, grid, alpha);
                return rgba;
            }
            ENDCG
        }
    }
}
