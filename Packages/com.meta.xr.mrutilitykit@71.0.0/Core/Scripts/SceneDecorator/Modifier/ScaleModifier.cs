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

using System;
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public class ScaleModifier : Modifier
    {
        [Serializable]
        public struct AxisParameters
        {
            [SerializeField]
            public Mask mask;

            [SerializeField]
            public float limitMin;

            [SerializeField]
            public float limitMax;

            [SerializeField]
            public float scale;

            [SerializeField]
            public float offset;
        }

        [SerializeField]
        AxisParameters x = new() { limitMin = float.NegativeInfinity, limitMax = float.PositiveInfinity, scale = 1f };

        [SerializeField]
        AxisParameters y = new() { limitMin = float.NegativeInfinity, limitMax = float.PositiveInfinity, scale = 1f };

        [SerializeField]
        AxisParameters z = new() { limitMin = float.NegativeInfinity, limitMax = float.PositiveInfinity, scale = 1f };

        public override void ApplyModifier(GameObject decorationGO, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration, Candidate candidate)
        {
            var scale = decorationGO.transform.localScale;
            scale.x *= x.mask.SampleMask(candidate, x.limitMin, x.limitMax, x.scale, x.offset);
            scale.y *= y.mask.SampleMask(candidate, y.limitMin, y.limitMax, y.scale, y.offset);
            scale.z *= z.mask.SampleMask(candidate, z.limitMin, z.limitMax, z.scale, z.offset);
            decorationGO.transform.localScale = scale;
        }
    }
}
