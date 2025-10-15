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

Shader "Meta/MRUK/Unlit/Wireframe" {
  Properties {
    _WireframeColor("WireframeColor", Color) = (1, 0, 0, 1)
    _Color("Color", Color) = (1, 1, 1, 1)
    _DistanceMultipler("DistanceMultiplier", Range(1, 5)) = 1
  }

  SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    Blend SrcAlpha OneMinusSrcAlpha

    Pass {
      CGPROGRAM
      #include "UnityCG.cginc"
      #pragma vertex vert
      #pragma fragment frag

      half4 _WireframeColor, _Color;
      float _LineThickness, _DistanceMultipler;

      struct appdata
      {
        float4 vertex : POSITION;
        float4 color : COLOR; // barycentric coords
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float3 vertexView : TEXCOORD0;
        float3 color: COLOR;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      v2f vert(appdata v)
      {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_OUTPUT(v2f, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.vertexView = UnityObjectToViewPos(v.vertex);
        o.color = v.color;
        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        // on edge, one or the coordinates is 0
        float closest = min(i.color.x, min(i.color.y, i.color.z));

        // use distance to make far away edges visible
        float distance = length(i.vertexView) * _DistanceMultipler * 0.02;
        float val = closest/distance;

        return lerp(_WireframeColor, _Color, clamp(val,0,1));
      }
      ENDCG
    }
  }
}
