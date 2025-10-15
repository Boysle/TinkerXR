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
using Random = UnityEngine.Random;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Serializable]
    [Feature(Feature.Scene)]
    public class RandomDistribution : SceneDecorator.IDistribution
    {
        [SerializeField]
        [Tooltip("How many entries to generate per unit (1m)")]
        private float numPerUnit = 10f;

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

            var numToGenerate = Mathf.Max((int)Mathf.Ceil(anchorScale.x * anchorScale.y * numPerUnit), 1);
            for (; numToGenerate > 0; --numToGenerate)
            {
                var rnd_x = Random.value;
                var rnd_y = Random.value;

                sceneDecorator.GenerateOn(new Vector2(rnd_x * anchorScale.x - anchorScale.x / 2, rnd_y * anchorScale.y - anchorScale.y / 2),
                    new Vector2(rnd_x, rnd_y),
                    sceneAnchor,
                    sceneDecoration);
            }
        }
    }
}
