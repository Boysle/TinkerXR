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
    public class StaggeredConcentricDistribution : SceneDecorator.IDistribution
    {
        [SerializeField] public float stepSize;

        private const float regionRadius = 0.7071067811865475244f; //sqrt(2f*0.5f*0.5f)

        public void Distribute(SceneDecorator sceneDecorator, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration)
        {
            var circle0Pos = new Vector2(-1f, -1f);
            var circle1Pos = new Vector2(-1f, 1f);

            var offset = circle1Pos - circle0Pos;

            var distSq = offset.magnitude;
            var dist = Mathf.Sqrt(distSq);

            var circle0RegionDist = circle0Pos.sqrMagnitude;
            var circle1RegionDist = circle1Pos.sqrMagnitude;

            var circle0End = (int)Mathf.Floor((circle0RegionDist + regionRadius) / stepSize);
            var circle1End = (int)Mathf.Floor((circle1RegionDist + regionRadius) / stepSize);
            circle1RegionDist -= regionRadius;

            for (int circle0Step = (int)Mathf.Ceil((circle0RegionDist - regionRadius) / stepSize);
                 circle0Step < circle0End;
                 ++circle0Step)
            {
                var circle0Radius = stepSize * circle0Step;

                var circle1Start = (int)Mathf.Ceil(Mathf.Max(circle1RegionDist, dist - circle0Radius) / stepSize);
                for (var circle1Step = circle1Start; circle1Step < circle1End; ++circle1Step)
                {
                    var circle1Radius = stepSize * circle1Step;

                    //note that we stagger based on the opposing circle's step on purpose
                    var staggeredRadius0 = circle0Radius + ((circle1Step % 3) != 0 ? 0f : 0.15f * stepSize);
                    var staggeredRadius1 = circle1Radius + ((circle0Step % 3) != 0 ? 0f : 0.15f * stepSize);

                    if (dist > staggeredRadius0 + staggeredRadius1)
                    {
                        //the circles do not overlap
                        continue;
                    }

                    var staggeredRadius0Sq = staggeredRadius0 * staggeredRadius0;
                    var radicalOffset = (staggeredRadius0Sq - staggeredRadius1 * staggeredRadius1 + distSq) /
                                        (2f * dist);

                    var p0 = circle0Pos + radicalOffset * offset / dist;
                    var h = offset * Mathf.Sqrt(staggeredRadius0Sq - radicalOffset * radicalOffset) / dist;
                    Vector2 p1 = new(p0.x + h.y, p0.y - h.x);
                    p0 = new(p0.x - h.y, p0.y + h.x);

                    sceneDecorator.GenerateOn(p0, p0, sceneAnchor, sceneDecoration);
                    if (p1 != p0)
                    {
                        sceneDecorator.GenerateOn(p1, p1, sceneAnchor, sceneDecoration);
                    }
                }
            }
        }
    }
}
