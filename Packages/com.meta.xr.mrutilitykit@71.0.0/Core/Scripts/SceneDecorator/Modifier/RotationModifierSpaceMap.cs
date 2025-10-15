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

using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public class RotationModifierSpaceMap : Modifier
    {
        [SerializeField]
        public Color RotateToColor = Color.black;

        [SerializeField]
        public float Radius = 0.02f;

        public override void ApplyModifier(GameObject decorationGO, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration, Candidate candidate)
        {
            var spaceMap = FindObjectOfType<SpaceMap>();
            if (spaceMap == null)
            {
                return;
            }

            var colors = new List<Color>();
            var angles = new List<float>();
            for (int i = 0; i < 12; i++)
            {
                var angle = 2 * SceneDecorator.PI / 12 * i; //numbers around the clock
                var point2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
                var point = candidate.hit.point + new Vector3(point2D.x, 0, point2D.y);
                Color color = spaceMap.GetColorAtPosition(point);
                colors.Add(color);
                angles.Add(angle);
            }

            var closestDistance = ColorDistance(RotateToColor, colors[0]);
            var closestAngle = new List<float>() { angles[0] };
            for (int i = 0; i < colors.Count; i++)
            {
                float distance = ColorDistance(RotateToColor, colors[i]);
                if (distance <= closestDistance) //we can have many positive hits, therefore we make a random candidate in the end
                {
                    closestDistance = distance;
                    closestAngle.Add(angles[i]);
                }
            }

            var idx = UnityEngine.Random.Range(0, closestAngle.Count);
            decorationGO.transform.rotation = Quaternion.Euler(0, -1 * closestAngle[idx] * Mathf.Rad2Deg, 0);
        }

        private float ColorDistance(Color a, Color b)
        {
            return Mathf.Sqrt(Mathf.Pow(a.r - b.r, 2) + Mathf.Pow(a.g - b.g, 2) + Mathf.Pow(a.b - b.b, 2));
        }
    }
}
