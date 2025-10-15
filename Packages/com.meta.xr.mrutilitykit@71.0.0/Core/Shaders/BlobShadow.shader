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

Shader "Meta/MRUK/Projector/BlobShadow" {
	Properties {
		_ShadowTex ("Cookie", 2D) = "gray" {}
		_FalloffTex ("FallOff", 2D) = "white" {}
		_ShadowIntensity ("Intensity", Range (0, 1)) = 0.8
		_Color ("Color", Color) = (0,0,0,1)
	}
	Subshader {
		Tags {"RenderType"="Opaque"}
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha, One One
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			struct vertex_out {
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				float4 pos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;

			vertex_out vert (appdata v)
			{
				vertex_out o;
				UNITY_SETUP_INSTANCE_ID(v);
			    UNITY_INITIALIZE_OUTPUT(vertex_out, o);
			    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uvShadow = mul (unity_Projector, v.vertex);
				o.uvFalloff = mul (unity_ProjectorClip, v.vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			sampler2D _ShadowTex;
			sampler2D _FalloffTex;
			half _ShadowIntensity;
			fixed4 _Color;

			fixed4 frag (vertex_out i) : SV_Target
			{
				fixed4 texS = tex2Dproj (_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
				fixed4 texF = tex2Dproj (_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
				fixed4 res = lerp(fixed4(0,0,0,0), fixed4(_Color.r,_Color.g,_Color.b, texS.a), texF.a * _ShadowIntensity);

				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(1,1,1,1));
				return res;
			}
			ENDCG
		}
	}
}
