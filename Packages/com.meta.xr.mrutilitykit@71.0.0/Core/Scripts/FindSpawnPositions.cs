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
using UnityEngine.Serialization;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Allows for fast generation of valid (inside the room, outside furniture bounds) random positions for content spawning.
    /// Optional method to pin directly to surfaces.
    /// </summary>
    [Feature(Feature.Scene)]
    public class FindSpawnPositions : MonoBehaviour
    {
        [Tooltip("When the scene data is loaded, this controls what room(s) the prefabs will spawn in.")]
        public MRUK.RoomFilter SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        [SerializeField, Tooltip("Prefab to be placed into the scene, or object in the scene to be moved around.")]
        public GameObject SpawnObject;

        [SerializeField, Tooltip("Number of SpawnObject(s) to place into the scene per room, only applies to Prefabs.")]
        public int SpawnAmount = 8;

        [SerializeField, Tooltip("Maximum number of times to attempt spawning/moving an object before giving up.")]
        public int MaxIterations = 1000;

        /// <summary>
        /// Defines possible locations where objects can be spawned.
        /// </summary>
        public enum SpawnLocation
        {
            Floating, // Spawn somewhere floating in the free space within the room
            AnySurface, // Spawn on any surface (i.e. a combination of all 3 options below)
            VerticalSurfaces, // Spawn only on vertical surfaces such as walls, windows, wall art, doors, etc...
            OnTopOfSurfaces, // Spawn on surfaces facing upwards such as ground, top of tables, beds, couches, etc...
            HangingDown // Spawn on surfaces facing downwards such as the ceiling
        }

        [FormerlySerializedAs("selectedSnapOption")]
        [SerializeField, Tooltip("Attach content to scene surfaces.")]
        public SpawnLocation SpawnLocations = SpawnLocation.Floating;

        [SerializeField, Tooltip("When using surface spawning, use this to filter which anchor labels should be included. Eg, spawn only on TABLE or OTHER.")]
        public MRUKAnchor.SceneLabels Labels = ~(MRUKAnchor.SceneLabels)0;

        [SerializeField, Tooltip("If enabled then the spawn position will be checked to make sure there is no overlap with physics colliders including themselves.")]
        public bool CheckOverlaps = true;

        [SerializeField, Tooltip("Required free space for the object (Set negative to auto-detect using GetPrefabBounds)")]
        public float OverrideBounds = -1; // default to auto-detect. This value represents the extents of the bounding box

        [FormerlySerializedAs("layerMask")]
        [SerializeField, Tooltip("Set the layer(s) for the physics bounding box checks, collisions will be avoided with these layers.")]
        public LayerMask LayerMask = -1;

        [SerializeField, Tooltip("The clearance distance required in front of the surface in order for it to be considered a valid spawn position")]
        public float SurfaceClearanceDistance = 0.1f;

        private void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadFindSpawnPositions).Send();
#endif
            if (MRUK.Instance && SpawnOnStart != MRUK.RoomFilter.None)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(() =>
                {
                    switch (SpawnOnStart)
                    {
                        case MRUK.RoomFilter.AllRooms:
                            StartSpawn();
                            break;
                        case MRUK.RoomFilter.CurrentRoomOnly:
                            StartSpawn(MRUK.Instance.GetCurrentRoom());
                            break;
                    }
                });
            }
        }

        /// <summary>
        /// Starts the spawning process for all rooms.
        /// </summary>
        public void StartSpawn()
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                StartSpawn(room);
            }
        }

        /// <summary>
        /// Starts the spawning process for a specific room.
        /// </summary>
        /// <param name="room">The room to spawn objects in.</param>
        public void StartSpawn(MRUKRoom room)
        {
            var prefabBounds = Utilities.GetPrefabBounds(SpawnObject);
            float minRadius = 0.0f;
            const float clearanceDistance = 0.01f;
            float baseOffset = -prefabBounds?.min.y ?? 0.0f;
            float centerOffset = prefabBounds?.center.y ?? 0.0f;
            Bounds adjustedBounds = new();

            if (prefabBounds.HasValue)
            {
                minRadius = Mathf.Min(-prefabBounds.Value.min.x, -prefabBounds.Value.min.z, prefabBounds.Value.max.x, prefabBounds.Value.max.z);
                if (minRadius < 0f)
                {
                    minRadius = 0f;
                }

                var min = prefabBounds.Value.min;
                var max = prefabBounds.Value.max;
                min.y += clearanceDistance;
                if (max.y < min.y)
                {
                    max.y = min.y;
                }

                adjustedBounds.SetMinMax(min, max);
                if (OverrideBounds > 0)
                {
                    Vector3 center = new Vector3(0f, clearanceDistance, 0f);
                    Vector3 size = new Vector3(OverrideBounds * 2f, clearanceDistance * 2f, OverrideBounds * 2f); // OverrideBounds represents the extents, not the size
                    adjustedBounds = new Bounds(center, size);
                }
            }

            for (int i = 0; i < SpawnAmount; ++i)
            {
                bool foundValidSpawnPosition = false;
                for (int j = 0; j < MaxIterations; ++j)
                {
                    Vector3 spawnPosition = Vector3.zero;
                    Vector3 spawnNormal = Vector3.zero;
                    if (SpawnLocations == SpawnLocation.Floating)
                    {
                        var randomPos = room.GenerateRandomPositionInRoom(minRadius, true);
                        if (!randomPos.HasValue)
                        {
                            break;
                        }

                        spawnPosition = randomPos.Value;
                    }
                    else
                    {
                        MRUK.SurfaceType surfaceType = 0;
                        switch (SpawnLocations)
                        {
                            case SpawnLocation.AnySurface:
                                surfaceType |= MRUK.SurfaceType.FACING_UP;
                                surfaceType |= MRUK.SurfaceType.VERTICAL;
                                surfaceType |= MRUK.SurfaceType.FACING_DOWN;
                                break;
                            case SpawnLocation.VerticalSurfaces:
                                surfaceType |= MRUK.SurfaceType.VERTICAL;
                                break;
                            case SpawnLocation.OnTopOfSurfaces:
                                surfaceType |= MRUK.SurfaceType.FACING_UP;
                                break;
                            case SpawnLocation.HangingDown:
                                surfaceType |= MRUK.SurfaceType.FACING_DOWN;
                                break;
                        }

                        if (room.GenerateRandomPositionOnSurface(surfaceType, minRadius, new LabelFilter(Labels), out var pos, out var normal))
                        {
                            spawnPosition = pos + normal * baseOffset;
                            spawnNormal = normal;
                            var center = spawnPosition + normal * centerOffset;
                            // In some cases, surfaces may protrude through walls and end up outside the room
                            // check to make sure the center of the prefab will spawn inside the room
                            if (!room.IsPositionInRoom(center))
                            {
                                continue;
                            }

                            // Ensure the center of the prefab will not spawn inside a scene volume
                            if (room.IsPositionInSceneVolume(center))
                            {
                                continue;
                            }

                            // Also make sure there is nothing close to the surface that would obstruct it
                            if (room.Raycast(new Ray(pos, normal), SurfaceClearanceDistance, out _))
                            {
                                continue;
                            }
                        }
                    }

                    Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);
                    if (CheckOverlaps && prefabBounds.HasValue)
                    {
                        if (Physics.CheckBox(spawnPosition + spawnRotation * adjustedBounds.center, adjustedBounds.extents, spawnRotation, LayerMask, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                    }

                    foundValidSpawnPosition = true;

                    if (SpawnObject.gameObject.scene.path == null)
                    {
                        Instantiate(SpawnObject, spawnPosition, spawnRotation, transform);
                    }
                    else
                    {
                        SpawnObject.transform.position = spawnPosition;
                        SpawnObject.transform.rotation = spawnRotation;
                        return; // ignore SpawnAmount once we have a successful move of existing object in the scene
                    }

                    break;
                }

                if (!foundValidSpawnPosition)
                {
                    Debug.LogWarning($"Failed to find valid spawn position after {MaxIterations} iterations. Only spawned {i} prefabs instead of {SpawnAmount}.");
                    break;
                }
            }
        }
    }
}
