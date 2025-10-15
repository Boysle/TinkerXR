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

using Meta.XR.MRUtilityKit.Extensions;
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public class CookieMask : Mask2D
    {
        public enum SampleMode
        {
            NEAREST = 0x0,
            NEAREST_REPEAT = 0x1,
            NEAREST_REPEAT_MIRROR = 0x2,
            BILINEAR = 0x3,
            BILINEAR_REPEAT = 0x4,
            BILINEAR_REPEAT_MIRROR = 0x5
        }

        [SerializeField]
        public Texture2D cookie;

        [SerializeField]
        public SampleMode sampleMode;

        public override float SampleMask(Candidate c)
        {
            var affineTransform = GenerateAffineTransform(offsetX, offsetY, rotation, scaleX, scaleY, shearX, shearY);
            var tuv = Float3X3.Multiply(affineTransform, Vector3Extensions.FromVector2AndZ(c.localPos, 1f));
            tuv /= tuv.z;
            var uv = new Vector2(tuv.x, tuv.y);

            float value;
            switch (sampleMode)
            {
                default:
                case SampleMode.NEAREST:
                    uv *= new Vector2(cookie.width, cookie.height);
                    value = (tuv.x < 0f | tuv.x > 1f | tuv.y < 0f | tuv.y > 1f) ? 0f : cookie.GetPixel((int)uv.x, (int)uv.y).r;
                    break;
                case SampleMode.NEAREST_REPEAT:
                    uv = uv.Frac();
                    uv *= new Vector2(cookie.width, cookie.height);
                    value = cookie.GetPixel((int)uv.x, (int)uv.y).r;
                    break;
                case SampleMode.NEAREST_REPEAT_MIRROR:
                    uv = 2f * (uv - uv.Add(0.5f).Floor()).Abs();
                    uv *= new Vector2(cookie.width, cookie.height);
                    value = cookie.GetPixel((int)uv.x, (int)uv.y).r;
                    break;
                case SampleMode.BILINEAR:
                    value = (uv.x < 0f | uv.x > 1f | uv.y < 0f | uv.y > 1f) ? 0f : cookie.GetPixelBilinear(uv.x, uv.y).r;
                    break;
                case SampleMode.BILINEAR_REPEAT:
                    uv = uv.Frac();
                    value = cookie.GetPixelBilinear(uv.x, uv.y).r;
                    break;
                case SampleMode.BILINEAR_REPEAT_MIRROR:
                    uv = 2f * (uv - uv.Add(0.5f).Floor()).Abs();
                    value = cookie.GetPixelBilinear(uv.x, uv.y).r;
                    break;
            }

            return value;
        }

        public override bool Check(Candidate c)
        {
            return true;
        }
    }
}
