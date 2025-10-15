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

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Draws a visual representation of the scene boundaries similarly to what the guardian does at a system level
    /// to indicate the limit of the physical space defined by the user.
    /// </summary>
    [Feature(Feature.Scene)]
    public class RoomGuardian : MonoBehaviour
    {
        [Tooltip("Material to use for the Guardian effect")]
        public Material GuardianMaterial;

        [Tooltip("This is how far, in meters, the player must be form a surface for the Guardian to become visible (in other words, it blends `_GuardianFade` from 0 to 1). The position of the user is calculated as a point 0.2m above the ground. This is to catch tripping hazards, as well as walls.")]
        public float GuardianDistance = 1.0f;

        private void Start()
        {
            // required for passthrough blending to work properly
            OVRPlugin.eyeFovPremultipliedAlphaModeEnabled = false;
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadRoomGuardian).Send();
#endif
        }

        void Update()
        {
            if (GuardianMaterial == null)
            {
                return;
            }

            // get the closest distance of all the surfaces
            // should avoid raycasting if possible
            var room = MRUK.Instance?.GetCurrentRoom();
            if (!room)
            {
                return;
            }

            bool insideRoom = room.IsPositionInRoom(Camera.main.transform.position);

            // instead of using the head position, we actually want a position near the feet
            // (to catch short volumes like a bed, which are a tripping hazard)
            Vector3 testPosition = new Vector3(Camera.main.transform.position.x, 0.2f, Camera.main.transform.position.z);

            float closestDistance =
                room.TryGetClosestSurfacePosition(testPosition, out Vector3 closestPoint, out _, new LabelFilter(~(MRUKAnchor.SceneLabels.FLOOR | MRUKAnchor.SceneLabels.CEILING)));

            bool outsideVolume = !room.IsPositionInSceneVolume(testPosition);

            float guardianFade = insideRoom && outsideVolume ? Mathf.Clamp01(1 - (closestDistance / GuardianDistance)) : 1.0f;
            GuardianMaterial.SetFloat("_GuardianFade", guardianFade);

            Color lineColor = insideRoom ? Color.green : Color.red;
            Debug.DrawLine(testPosition, closestPoint, lineColor);
        }
    }
}
