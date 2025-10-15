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

using UnityEngine;
using Meta.XR.MRUtilityKit;

namespace Meta.XR.MRUtilityKitSamples
{
    public sealed class KeyboardManager : MonoBehaviour
    {
        [SerializeField]
        GameObject _prefab;

        [SerializeField]
        OVRPassthroughLayer _passthroughLayer;

        public void OnTrackableAdded(MRUKTrackable trackable)
        {
            Debug.Log($"Detected new {trackable.TrackableType} with {trackable.name}");

            if (trackable.TrackableType != OVRAnchor.TrackableType.Keyboard)
            {
                // We only care about keyboards
                return;
            }

            // Instantiate the prefab
            var newGameObject = Instantiate(_prefab, trackable.transform);

            // Hook everything up
            var boundaryVisualizer = newGameObject.GetComponentInChildren<Bounded3DVisualizer>();
            if (boundaryVisualizer)
            {
                boundaryVisualizer.Initialize(_passthroughLayer, trackable);
            }
        }

        public void OnTrackableRemoved(MRUKTrackable trackable)
        {
            Debug.Log($"Removing GameObject '{trackable.name}'");
            Destroy(trackable.gameObject);
        }

        void Update()
        {
            // Toggle between full passthrough and surface-projected passthrough
            if (_passthroughLayer && OVRInput.GetDown(OVRInput.RawButton.A))
            {
                _passthroughLayer.enabled = false;

                switch (_passthroughLayer.projectionSurfaceType)
                {
                    case OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed:
                    {
                        _passthroughLayer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.UserDefined;
                        _passthroughLayer.overlayType = OVROverlay.OverlayType.Overlay;
                        Camera.main.clearFlags = CameraClearFlags.Skybox;
                        break;
                    }
                    case OVRPassthroughLayer.ProjectionSurfaceType.UserDefined:
                    {
                        _passthroughLayer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;
                        _passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;
                        Camera.main.clearFlags = CameraClearFlags.SolidColor;
                        break;
                    }
                }

                _passthroughLayer.enabled = true;
            }
        }
    }
}
