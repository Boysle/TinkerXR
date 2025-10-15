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
    [Serializable]
    [Feature(Feature.Scene)]
    public class GridDistribution : SceneDecorator.IDistribution
    {
        [SerializeField] private float spacingX = 1f;
        [SerializeField] private float spacingY = 1f;

        public void Distribute(SceneDecorator sceneDecorator, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration)
        {
            Vector3 anchorScale = Vector3.one;
            if (sceneAnchor.PlaneRect.HasValue)
            {
                anchorScale = sceneAnchor.PlaneRect.HasValue
                    ? new(sceneAnchor.PlaneRect.Value.width, sceneAnchor.PlaneRect.Value.height, 1)
                    : Vector3.one;
            }

            if (sceneAnchor.VolumeBounds.HasValue)
            {
                anchorScale = sceneAnchor.VolumeBounds?.size ?? Vector3.one;
            }

            Vector2 res = new Vector2(
                Mathf.Max(Mathf.Ceil(anchorScale.x / spacingX), 1),
                Mathf.Max(Mathf.Ceil(anchorScale.y / spacingY), 1));

            Vector2 step = anchorScale / res;
            for (int x = 0; x < res.x; ++x)
            {
                for (int y = 0; y < res.y; ++y)
                {
                    var pos = new Vector2((new Vector2(x, y) * step).x - anchorScale.x / 2,
                        (new Vector2(x, y) * step).y - anchorScale.y / 2);
                    var posNormalized = new Vector2((new Vector2(x, y) * step).x / anchorScale.x,
                        (new Vector2(x, y) * step).y / anchorScale.y);
                    sceneDecorator.GenerateOn(pos, posNormalized, sceneAnchor, sceneDecoration);
                }
            }
        }
    }
}
