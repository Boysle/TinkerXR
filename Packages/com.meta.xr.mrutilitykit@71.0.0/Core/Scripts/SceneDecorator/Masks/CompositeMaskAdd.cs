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
    public class CompositeMaskAdd : Mask2D
    {
        [Serializable]
        public struct MaskLayer
        {
            public Mask mask;
            public float outputScale;
            public float outputLimitMin;
            public float outputLimitMax;
            public float outputOffset;

            public float SampleMask(Candidate c)
            {
                return mask.SampleMask(c, outputLimitMin, outputLimitMax, outputScale, outputOffset);
            }
        }

        [SerializeField]
        private MaskLayer[] maskLayers;

        public override float SampleMask(Candidate c)
        {
            var affineTransform = GenerateAffineTransform(offsetX, offsetY, rotation, scaleX, scaleY, shearX, shearY);
            var tuv = Float3X3.Multiply(affineTransform, new Vector3(c.localPos.x, c.localPos.y, 1f));
            tuv /= tuv.z;
            c.localPos = new Vector2(tuv.x, tuv.y);

            var value = 0f;
            foreach (var layer in maskLayers)
            {
                value += layer.SampleMask(c);
            }

            return value;
        }

        public override bool Check(Candidate c)
        {
            return true;
        }
    }
}
