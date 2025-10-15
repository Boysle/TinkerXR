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

using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public class ColliderMask : Mask
    {
        [SerializeField] private int MaxCheckColliders = 10;
        [SerializeField] private bool IgnoreFloorCollision = true;
        [SerializeField] private bool IgnoreGlobalMeshCollision = true;

        public override float SampleMask(Candidate c)
        {
            return 0;
        }

        public override bool Check(Candidate c)
        {
            var prefabCollider = c.decorationPrefab.GetComponentInChildren<Collider>();
            if (prefabCollider == null)
            {
                return true;
            }

            var colliders = new Collider[MaxCheckColliders];
            if (prefabCollider is BoxCollider boxCollider)
            {
                var size = Physics.OverlapBoxNonAlloc(c.hit.point, boxCollider.size / 2
                    , colliders);
                return CheckColliderHitsForMRUK(colliders, size);
            }

            if (prefabCollider is MeshCollider meshCollider)
            {
                BoxCollider tempBoxCollider = c.decorationPrefab.AddComponent<BoxCollider>();
                tempBoxCollider.center
                    = meshCollider.bounds.center - c.decorationPrefab.transform.position; // Convert to local space
                tempBoxCollider.size = meshCollider.bounds.size;
                var size = Physics.OverlapBoxNonAlloc(c.hit.point, tempBoxCollider.size / 2
                    , colliders);

                Destroy(tempBoxCollider);
                return CheckColliderHitsForMRUK(colliders, size);
            }

            if (prefabCollider is CapsuleCollider capsuleCollider)
            {
                var size = Physics.OverlapCapsuleNonAlloc(capsuleCollider.transform.position
                    , c.hit.point + Vector3.up * capsuleCollider.height, capsuleCollider.radius
                    , colliders);
                return CheckColliderHitsForMRUK(colliders, size);
            }

            if (prefabCollider is SphereCollider sphereCollider)
            {
                var size = Physics.OverlapSphereNonAlloc(c.hit.point, sphereCollider.radius
                    , colliders);
                return CheckColliderHitsForMRUK(colliders, size);
            }

            return false;
        }

        /// Returns true if there's no HIT with another gameobject,
        /// or hits with Floor/GlobalMesh which we can ignore
        private bool CheckColliderHitsForMRUK(Collider[] colliders, int size)
        {
            var ret = true;
            for (var i = 0; i < MaxCheckColliders; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                if (colliders[i].gameObject.GetComponentsInParent<MRUKAnchor>().Length != 0)
                {
                    var mrukAnchor = colliders[i].gameObject.GetComponentsInParent<MRUKAnchor>()[0];
                    if (mrukAnchor.Label == MRUKAnchor.SceneLabels.FLOOR)
                    {
                        ret = IgnoreFloorCollision;
                    }

                    if (mrukAnchor.Label == MRUKAnchor.SceneLabels.GLOBAL_MESH)
                    {
                        ret = IgnoreGlobalMeshCollision;
                    }
                }
                else
                {
                    ret = false; //we hit another object
                    break;
                }
            }

            return ret;
        }
    }
}
