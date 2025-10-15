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

Shader "Meta/MRUK/MixedReality/SpaceMapGradient"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex ("Gradient", 2D) = "white" {}
        _InsideColor ("Inside Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };
            sampler2D _MainTex;
            sampler2D _GradientTex;
            uniform float4x4 _SpaceMapProjectionViewMatrix;
            uniform fixed4 _InsideColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float4 clipPos = mul(_SpaceMapProjectionViewMatrix, i.worldPos);
                clipPos /= clipPos.w;
                float2 uv = clipPos.xy * 0.5 + 0.5;
                fixed4 col = tex2D(_MainTex, uv);
                if (length(col.rgb) < 0.01) { //pretty black, outside
                    return fixed4(0, 0, 0, 0); // return black
                }
                if(col.b > 0) {
                    return _InsideColor;
                }
                return tex2D(_GradientTex, 1-col.r);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
