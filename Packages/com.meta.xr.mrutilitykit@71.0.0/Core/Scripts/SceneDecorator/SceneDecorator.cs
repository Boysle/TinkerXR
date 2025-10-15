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
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public class SceneDecorator : MonoBehaviour
    {
        public static readonly float PI = 3.14159f;

        public interface IDistribution
        {
            public void Distribute(SceneDecorator sceneDecorator, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration);
        }

        [SerializeField]
        public List<SceneDecoration> sceneDecorations;

        [SerializeField]
        public Collider[] customColliders;

        [SerializeField]
        public string[] customTargetTags;

        [SerializeField]
        public int recursionLimit = 3;

        private int _recursionDepth = 0;
        private SceneDecorator _parent = null;

        [Tooltip("When the scene data is loaded, this controls what room(s) the decorator will add decorations.")]
        public MRUK.RoomFilter DecorateOnStart = MRUK.RoomFilter.AllRooms;

        [Tooltip("If enabled, updates on scene elements such as rooms and anchors will be handled by this class")]
        internal bool TrackUpdates = true;

        private PoolManagerComponent _poolManagerComponent;
        private PoolManagerSingleton _poolManagerSingleton;

        private Dictionary<GameObject, MRUKAnchor> _spawnedDecorations = new Dictionary<GameObject, MRUKAnchor>();
        private void Start()
        {
            _poolManagerSingleton = gameObject.AddComponent<PoolManagerSingleton>();
            _poolManagerComponent = gameObject.AddComponent<PoolManagerComponent>();
            InitPools();

#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneDecoration).Send();
#endif
            if (MRUK.Instance is null)
            {
                return;
            }

            MRUK.Instance.SceneLoadedEvent.AddListener(() =>
            {
                if (DecorateOnStart == MRUK.RoomFilter.None)
                {
                    return;
                }

                switch (DecorateOnStart)
                {
                    case MRUK.RoomFilter.CurrentRoomOnly:
                        DecorateScene(MRUK.Instance.GetCurrentRoom(), _recursionDepth);
                        break;
                    case MRUK.RoomFilter.AllRooms:
                        DecorateScene(MRUK.Instance.Rooms, _recursionDepth);
                        break;
                }
            });

            if (MRUK.Instance.IsInitialized)
            {
                switch (DecorateOnStart)
                {
                    case MRUK.RoomFilter.CurrentRoomOnly:
                        DecorateScene(MRUK.Instance.GetCurrentRoom(), _recursionDepth);
                        break;
                    case MRUK.RoomFilter.AllRooms:
                        DecorateScene(MRUK.Instance.Rooms, _recursionDepth);
                        break;
                }
            }

            if (!TrackUpdates)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveRoomCreated);
            MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRoomRemoved);
        }

        private void OnDestroy()
        {
            if (MRUK.Instance == null)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.RemoveListener(ReceiveRoomCreated);
            MRUK.Instance.RoomRemovedEvent.RemoveListener(ReceiveRoomRemoved);
        }

        private void InitPools()
        {
            List<PoolManagerComponent.PoolDesc> defaultPools = new();
            foreach (var decoration in sceneDecorations)
            {
                foreach (var prim in decoration.decorationPrefabs)
                {
                    defaultPools.Add(new PoolManagerComponent.PoolDesc()
                    {
                        poolType = PoolManagerComponent.PoolDesc.PoolType.FIXED,
                        size = decoration.Poolsize,
                        primitive = prim,
                        callbackProviderOverride = null
                    });
                }
            }
            _poolManagerComponent.defaultPools = defaultPools.ToArray();
            _poolManagerComponent.InitDefaultPools();
        }

        private void OnEnable()
        {
            if (!MRUK.Instance)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveRoomCreated);
            MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRoomRemoved);
        }

        private void OnDisable()
        {
            if (!MRUK.Instance)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.RemoveListener(ReceiveRoomCreated);
            MRUK.Instance.RoomRemovedEvent.RemoveListener(ReceiveRoomRemoved);
        }

        private void ReceiveRoomRemoved(MRUKRoom room)
        {
            ClearDecorations(room);
            UnRegisterAnchorUpdates(room);
        }

        private void ReceiveRoomCreated(MRUKRoom room)
        {
            if (TrackUpdates && DecorateOnStart == MRUK.RoomFilter.AllRooms)
            {
                DecorateScene(room);
                RegisterAnchorUpdates(room);
            }
        }

        private void UnRegisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.RemoveListener(ReceiveAnchorCreated);
            room.AnchorRemovedEvent.RemoveListener(ReceiveAnchorRemoved);
            room.AnchorUpdatedEvent.RemoveListener(ReceiveAnchorUpdated);
        }

        private void ReceiveAnchorUpdated(MRUKAnchor anchor)
        {
            if (TrackUpdates)
            {
                ClearDecorations(anchor);
                Decorate(anchor);
            }
        }

        private void ReceiveAnchorRemoved(MRUKAnchor anchor)
        {
            ClearDecorations(anchor);
        }

        private void ReceiveAnchorCreated(MRUKAnchor anchor)
        {
            Decorate(anchor);
        }

        private void RegisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.AddListener(ReceiveAnchorCreated);
            room.AnchorRemovedEvent.AddListener(ReceiveAnchorRemoved);
            room.AnchorUpdatedEvent.AddListener(ReceiveAnchorUpdated);
        }
        private void ClearDecorations(MRUKAnchor anchor)
        {
            List<GameObject> decorationToRemove = new();
            foreach (var kv in _spawnedDecorations)
            {
                if (kv.Value != anchor)
                {
                    continue;
                }

                decorationToRemove.Add(kv.Key);
            }

            foreach (var decoration in decorationToRemove)
            {
                _poolManagerSingleton.Release(decoration);
            }
        }

        // when we receive an update, all decorations associated with this room
        // will be removed.
        // depending on your SpawnHierarchy, the decoration may gets removed
        // from another tool which also receives updates
        private void ClearDecorations(MRUKRoom room)
        {
            List<GameObject> decorationToRemove = new();
            foreach (var kv in _spawnedDecorations)
            {
                if (kv.Value.Room != room)
                {
                    continue;
                }

                decorationToRemove.Add(kv.Key);
            }

            foreach (var decoration in decorationToRemove)
            {
                _poolManagerSingleton.Release(decoration);
            }
        }

        public void ClearDecorations()
        {
            foreach (var decoration in _spawnedDecorations)
            {
                _poolManagerSingleton.Release(decoration.Key);
            }
        }
        private void DecorateScene(MRUKRoom room)
        {
            DecorateScene(room, 0);
        }

        public void DecorateScene()
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                DecorateScene(room, 0);
            }
        }

        private void DecorateScene(List<MRUKRoom> rooms, int recursionDepth)
        {
            foreach (var room in rooms)
            {
                DecorateScene(room, recursionDepth);
            }
        }

        private void DecorateScene(MRUKRoom room, int recursionDepth)
        {
            if (recursionDepth >= recursionLimit)
            {
                return;
            }

            foreach (SceneDecoration sceneDecoration in sceneDecorations)
            {
                Decorate(room, sceneDecoration);
            }
        }

        private void Decorate(MRUKAnchor anchor)
        {
            foreach (SceneDecoration sceneDecoration in sceneDecorations)
            {
                Decorate(anchor, sceneDecoration);
            }
        }

        private void Decorate(MRUKAnchor anchor, SceneDecoration sceneDecoration)
        {
            var labels = sceneDecoration.executeSceneLabels;
            if (anchor.Label == labels)
            {
                Distribute(anchor, sceneDecoration);
            }
        }

        private void Decorate(MRUKRoom room, SceneDecoration sceneDecoration)
        {
            var labels = sceneDecoration.executeSceneLabels;
            foreach (MRUKAnchor.SceneLabels flag in Enum.GetValues(typeof(MRUKAnchor.SceneLabels)))
            {
                if ((labels & flag) == 0)
                {
                    continue;
                }

                List<MRUKAnchor> anchors = GetAnchorsWithLabel(room, flag);

                if (anchors != null)
                {
                    foreach (MRUKAnchor sceneAnchor in anchors)
                    {
                        Distribute(sceneAnchor, sceneDecoration);
                    }
                }
            }
        }

        private void Distribute(MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration)
        {
            if (sceneDecoration.decorationPrefabs.Length == 0)
            {
                Debug.LogWarning($"No decoration prefab added to {sceneDecoration.name}");
                return;
            }

            if (_poolManagerComponent.defaultPools.Length == 0)
            {
                InitPools();
            }

            switch (sceneDecoration.distributionType)
            {
                case DistributionType.GRID:
                    sceneDecoration.gridDistribution.Distribute(this, sceneAnchor, sceneDecoration);
                    break;
                case DistributionType.SIMPLEX:
                    sceneDecoration.simplexDistribution.Distribute(this, sceneAnchor, sceneDecoration);
                    break;
                case DistributionType.STAGGERED_CONCENTRIC:
                    sceneDecoration.staggeredConcentricDistribution.Distribute(this, sceneAnchor, sceneDecoration);
                    break;
                default:
                case DistributionType.RANDOM:
                    sceneDecoration.randomDistribution.Distribute(this, sceneAnchor, sceneDecoration);
                    break;
            }
        }


        private static void TestCollider(Collider c, Vector3 worldPos, Vector3 rayDir, SceneDecoration sceneDecoration, ref RaycastHit closestHit)
        {
            rayDir = sceneDecoration.selectBehind ? -rayDir : rayDir;

            if (c.Raycast(new Ray(worldPos, rayDir),
                    out var hit,
                    float.PositiveInfinity) &&
                hit.distance < Mathf.Abs(closestHit.distance))
            {
                closestHit = hit;
                closestHit.distance = sceneDecoration.selectBehind ? -closestHit.distance : closestHit.distance;
                if (sceneDecoration.DrawDebugRaysAndImpactPoints)
                {
                    Debug.DrawLine(worldPos, closestHit.point, Color.magenta, 60 * 60);
                    Utilities.DrawWireSphere(worldPos, 0.05f, Color.cyan, 60 * 60);
                    Utilities.DrawWireSphere(closestHit.point, 0.05f, Color.blue, 60 * 60);
                }
            }
        }

        private static void TestPhysicsLayers(Vector3 worldPos, Vector3 rayDir, SceneDecoration sceneDecoration, ref RaycastHit closestHit)
        {
            rayDir = sceneDecoration.selectBehind ? -rayDir : rayDir;
            if (sceneDecoration.DrawDebugRaysAndImpactPoints)
            {
                Utilities.DrawWireSphere(worldPos, 0.05f, Color.cyan, 60 * 60);
            }

            if (Physics.Raycast(new Ray(worldPos, rayDir), out var hit, float.PositiveInfinity, sceneDecoration.targetPhysicsLayers)
                && hit.distance < Mathf.Abs(closestHit.distance))
            {
                closestHit = hit;
                closestHit.distance = sceneDecoration.selectBehind ? -closestHit.distance : closestHit.distance;
                if (sceneDecoration.DrawDebugRaysAndImpactPoints)
                {
                    Debug.DrawLine(worldPos, closestHit.point, Color.red, 60 * 60);
                    Utilities.DrawWireSphere(closestHit.point, 0.05f, Color.blue, 60 * 60);
                }
            }
        }

        private bool TestConstraints(SceneDecoration sceneDecoration, Candidate c)
        {
            foreach (var constraint in sceneDecoration.constraints)
            {
                if (!constraint.enabled)
                {
                    continue;
                }

                var modeCheck = constraint.modeCheck;
                var value = constraint.mask.SampleMask(c);
                var check = constraint.mask.Check(c);
                switch (modeCheck)
                {
                    case ConstraintModeCheck.Value:
                        if (value < constraint.min |
                            value > constraint.max)
                        {
                            return false;
                        }

                        break;
                    case ConstraintModeCheck.Bool:
                        if (!check)
                        {
                            return false;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return true;
        }

        private void ApplyModifiers(GameObject decorationGO, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration, Candidate candidate)
        {
            foreach (var m in sceneDecoration.modifiers)
            {
                if (m.enabled)
                {
                    m.ApplyModifier(decorationGO, sceneAnchor, sceneDecoration, candidate);
                }
            }
        }

        public void GenerateOn(Vector2 localPos, Vector2 localPosNormalized, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration)
        {
            Vector3 pos = Vector3.zero;
            switch (sceneDecoration.placement)
            {
                case Placement.SPHERICAL:
                    localPos *= new Vector2(2 * PI, PI);
                    float sTheta = Mathf.Sin(localPos.x);
                    pos = new(sTheta * Mathf.Cos(localPos.y),
                        sTheta * Mathf.Sin(localPos.y),
                        Mathf.Cos(localPos.x));
                    break;
                default:
                    pos = new Vector3(localPos.x, localPos.y, 0f) + sceneDecoration.rayOffset;
                    break;
            }

            GenerateAt(sceneAnchor.transform.TransformPoint(pos), localPos, localPosNormalized, sceneAnchor, sceneDecoration);
        }

        private void GenerateAt(Vector3 worldPos, Vector2 localPos, Vector2 localPosNormalized, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration)
        {
            var rayDir = sceneDecoration.placementDirection;
            switch (sceneDecoration.placement)
            {
                case Placement.LOCAL_PLANAR:
                    rayDir = sceneAnchor.transform.rotation * rayDir;
                    break;
                case Placement.SPHERICAL:
                    rayDir = (worldPos - sceneAnchor.transform.position).normalized;
                    break;
                case Placement.WORLD_PLANAR:
                default:
                    break;
            }

            var closestHit = new RaycastHit { distance = float.PositiveInfinity };
            var targets = sceneDecoration.targets;

            foreach (Target flag in Enum.GetValues(typeof(Target)))
            {
                if ((targets & flag) == 0)
                {
                    continue;
                }


                switch (flag)
                {
                    default:
                    case Target.GLOBAL_MESH:
                    case Target.RESERVED_MESH: //we do not support this yet in MRUK
                        if (sceneAnchor.Room.GlobalMeshAnchor != null)
                        {
                            var goGlobalMesh = sceneAnchor.Room.GlobalMeshAnchor.gameObject;
                            var colGlobalMesh = goGlobalMesh.GetComponentInChildren<MeshCollider>();
                            if (colGlobalMesh == null)
                            {
                                continue;
                            }

                            TestCollider(colGlobalMesh, worldPos, rayDir, sceneDecoration, ref closestHit);
                        }

                        break;
                    case Target.PHYSICS_LAYERS:
                        TestPhysicsLayers(worldPos, rayDir, sceneDecoration, ref closestHit);
                        break;
                    case Target.CUSTOM_COLLIDERS:
                        foreach (var c in customColliders)
                        {
                            TestCollider(c, worldPos, rayDir, sceneDecoration, ref closestHit);
                        }

                        break;
                    case Target.CUSTOM_TAGS:
                        foreach (var tag in customTargetTags)
                        {
                            if (sceneAnchor.gameObject.CompareTag(tag))
                            {
                                var tmpCollider = sceneAnchor.gameObject.GetComponentInChildren<MeshCollider>();
                                if (tmpCollider == null)
                                {
                                    continue;
                                }

                                TestCollider(tmpCollider, worldPos, rayDir, sceneDecoration, ref closestHit);
                            }
                        }

                        break;
                }
            }

            if (float.IsPositiveInfinity(closestHit.distance))
            {
                return;
            }

            var anchorDist = sceneAnchor.GetClosestSurfacePosition(closestHit.point, out var closestPosition);

            GameObject decorationGO = sceneDecoration.decorationPrefabs[Random.Range(0, sceneDecoration.decorationPrefabs.Length - 1)];
            Candidate candidate = new Candidate()
            {
                decorationPrefab = decorationGO,
                localPos = localPos,
                localPosNormalized = localPosNormalized,
                hit = closestHit,
                anchorDist = anchorDist,
                anchorCompDists = closestPosition,
                slope = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(closestHit.normal, -rayDir))
            };
            if (!TestConstraints(sceneDecoration, candidate))
            {
                return;
            }

            Transform parent;
            switch (sceneDecoration.spawnHierarchy)
            {
                default:
                case SpawnHierarchy.ROOT:
                    parent = null;
                    break;
                case SpawnHierarchy.SCENE_DECORATOR_CHILD:
                    parent = gameObject.transform;
                    break;
                case SpawnHierarchy.ANCHOR_CHILD:
                    parent = sceneAnchor.transform;
                    break;
                case SpawnHierarchy.TARGET_CHILD:
                    parent = closestHit.transform;
                    break;
                case SpawnHierarchy.TARGET_COLLIDER_CHILD:
                    parent = closestHit.collider == null ? null : closestHit.collider.transform;
                    break;
            }

            decorationGO = _poolManagerSingleton.Create(decorationGO, closestHit.point, Quaternion.identity, sceneAnchor, parent);

            if (decorationGO == null)
            {
                return;
            }
            //this is used to have a connection between active decoration object and MRUK anchor
            _spawnedDecorations[decorationGO] = sceneAnchor;

            if (sceneDecoration.lifetime > 0f)
            {
                Destroy(decorationGO, sceneDecoration.lifetime);
            }

            if (decorationGO.TryGetComponent(out SceneDecorator sceneDecorator))
            {
                sceneDecorator._parent = this;
                sceneDecorator._recursionDepth = _recursionDepth + 1;
            }

            if (parent != null &
                sceneDecoration.discardParentScaling)
            {
                var scale = decorationGO.transform.localScale;
                var lossyScale = parent.lossyScale;
                scale.x *= 1 / lossyScale.x;
                scale.y *= 1 / lossyScale.y;
                scale.z *= 1 / lossyScale.z;
                decorationGO.transform.localScale = scale;
            }

            ApplyModifiers(decorationGO, sceneAnchor, sceneDecoration, candidate);
        }

        private List<MRUKAnchor> GetAnchorsWithLabel(MRUKRoom room, MRUKAnchor.SceneLabels label)
        {
            var ret = new List<MRUKAnchor>();
            foreach (var anchor in room.Anchors)
            {
                if (anchor.Label == label)
                {
                    ret.Add(anchor);
                }
            }

            return ret;
        }
    }
}
