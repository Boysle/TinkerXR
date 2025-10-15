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
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// The <c>DestructibleGlobalMeshSpawner</c> class manages the spawning and lifecycle of destructible global meshes within the MRUK framework.
    /// It listens to room events and dynamically creates or removes destructible meshes as rooms are created or removed.
    /// For more details on room management, see <see cref="MRUKRoom"/>.
    /// </summary>
    public class DestructibleGlobalMeshSpawner : MonoBehaviour
    {
        [SerializeField] public MRUK.RoomFilter CreateOnRoomLoaded = MRUK.RoomFilter.CurrentRoomOnly;

        /// <summary>
        /// Event triggered when a destructible mesh is successfully created.
        /// </summary>
        public UnityEvent<DestructibleMeshComponent> OnDestructibleMeshCreated;

        /// <summary>
        /// Function delegate that processes the segmentation result. It can be used to modify the segmentation results before they are instantiated.
        /// </summary>
        public Func<DestructibleMeshComponent.MeshSegmentationResult, DestructibleMeshComponent.MeshSegmentationResult>
            OnSegmentationCompleted;

        [SerializeField] private bool _reserveSpace = false;
        [SerializeField] private Vector3 _reservedMin;
        [SerializeField] private Vector3 _reservedMax;
        [SerializeField] private Material _globalMeshMaterial;
        [SerializeField] private float _pointsPerUnitX = 1.0f;
        [SerializeField] private float _pointsPerUnitY = 1.0f;
        [SerializeField] private int _maxPointsCount = 256;
        [SerializeField] private float _reservedTop = 0f;
        [SerializeField] private float _reservedBottom = 0f;

        private readonly Dictionary<MRUKRoom, DestructibleGlobalMesh> _spawnedDestructibleMeshes = new();
        private const string _destructibleGlobalMeshObjectName = "DestructibleGlobalMesh";
        private static List<Vector3> _points = new List<Vector3>();

        /// <summary>
        /// Gets or sets whether to keep some reserved un-destructible space (defined in meters).
        /// </summary>
        public bool ReserveSpace
        {
            get => _reserveSpace;
            set => _reserveSpace = value;
        }

        /// <summary>
        /// Gets or sets the number of points per unit along the X-axis for the destructible mesh.
        /// This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// </summary>
        public float PointsPerUnitX
        {
            get => _pointsPerUnitX;
            set => _pointsPerUnitX = value;
        }

        /// <summary>
        /// Gets or serts the number of points per unit along the Y-axis for the destructible mesh.
        /// This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// </summary>
        public float PointsPerUnitY
        {
            get => _pointsPerUnitY;
            set => _pointsPerUnitY = value;
        }

        /// <summary>
        /// Gets or sets the maximum number of points that the destructible mesh can contain.
        /// The higher number of points the higher the impact on performance
        /// </summary>
        public int MaxPointsCount
        {
            get => _maxPointsCount;
            set => _maxPointsCount = value;
        }

        /// <summary>
        /// Gets or sets the material used for the mesh. This material is applied to the mesh segments that are created during the segmentation process.
        /// </summary>
        public Material GlobalMeshMaterial
        {
            get => _globalMeshMaterial;
            set => _globalMeshMaterial = value;
        }

        /// <summary>
        /// Gets or sets the reserved space at the top of the mesh. This space is not included in the destructible area, allowing for controlled segmentation.
        /// </summary>
        public float ReservedTop
        {
            get => _reservedTop;
            set => _reservedTop = value;
        }

        /// <summary>
        /// Gets or sets the reserved space at the bottom of the mesh. TThis space is not included in the destructible area, allowing for controlled segmentation.
        /// </summary>
        public float ReservedBottom
        {
            get => _reservedBottom;
            set => _reservedBottom = value;
        }

        void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadDestructibleGlobalMeshSpawner).Send();
#endif
            MRUK.Instance.RegisterSceneLoadedCallback(() =>
            {
                if (CreateOnRoomLoaded == MRUK.RoomFilter.None)
                {
                    return;
                }

                switch (CreateOnRoomLoaded)
                {
                    case MRUK.RoomFilter.CurrentRoomOnly:
                        AddDestructibleGlobalMesh(MRUK.Instance.GetCurrentRoom());
                        break;
                    case MRUK.RoomFilter.AllRooms:
                        AddDestructibleGlobalMesh(null);
                        break;
                    case MRUK.RoomFilter.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
            MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
        }

        /// <summary>
        /// Adds a destructible global mesh to all rooms. This method is typically called when the <c>SpawnOnStart</c> setting includes all rooms.
        /// </summary>
        private void AddDestructibleGlobalMesh()
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                if (!room.GlobalMeshAnchor)
                {
                    Debug.LogWarning(
                        $"Can not find a global mesh anchor, skipping the destructible mesh creation for this room");
                    continue;
                }
                AddDestructibleGlobalMesh(room);
            }
        }

        /// <summary>
        /// Adds a destructible global mesh to a specific room. This method checks for existing meshes in the room and creates a new one if none exists.
        /// </summary>
        /// <param name="room">The room to which the mesh will be added.</param>
        /// <param name="includeFloor">Whether to include the floor surface in the destructible global mesh generated.</param>
        /// <param name="includeCeiling">Whether to include the ceiling surface in the destructible global mesh generated..</param>
        /// <param name="includeWalls">Whether to include the walls surfaces in the destructible global mesh generated.</param>
        /// <returns>The destructible mesh created for the room.</returns>
        public DestructibleGlobalMesh AddDestructibleGlobalMesh(MRUKRoom room, bool includeFloor = true,
            bool includeCeiling = true, bool includeWalls = true)
        {
            if (_spawnedDestructibleMeshes.ContainsKey(room))
            {
                throw new Exception("Cannot add a destructible mesh to this room as it already contains one.");
            }

            if (!room.GlobalMeshAnchor)
            {
                throw new Exception(
                    "A destructible mesh can not be created for this room as it does not contain a global mesh anchor.");
            }
            var destructibleGlobalMeshGO = new GameObject(_destructibleGlobalMeshObjectName);
            destructibleGlobalMeshGO.transform.SetParent(room.GlobalMeshAnchor.transform, false);
            var dMesh = destructibleGlobalMeshGO.AddComponent<DestructibleMeshComponent>();
            dMesh.GlobalMeshMaterial = _globalMeshMaterial;
            if (_reserveSpace == false)
            {
                ReservedBottom = ReservedTop = -1;
            }
            dMesh.ReservedBottom = ReservedBottom;
            dMesh.ReservedTop = ReservedTop;
            dMesh.OnDestructibleMeshCreated = OnDestructibleMeshCreated;
            dMesh.OnSegmentationCompleted = OnSegmentationCompleted;
            var destructibleGlobalMesh = new DestructibleGlobalMesh
            {
                MaxPointsCount = _maxPointsCount,
                PointsPerUnitX = _pointsPerUnitX,
                PointsPerUnitY = _pointsPerUnitY,
                DestructibleMeshComponent = dMesh
            };
            CreateDestructibleGlobalMesh(destructibleGlobalMesh, room);
            _spawnedDestructibleMeshes.Add(room, destructibleGlobalMesh);
            return destructibleGlobalMesh;
        }

        /// <summary>
        /// Creates a destructible mesh within a specified room. If no room is provided, it defaults to the current room.
        /// This method handles the mesh creation by calculating segmentation points and starts the segmentation process.
        /// </summary>
        /// <param name="destructibleGlobalMesh">The destructible global mesh to create.</param>
        /// <param name="room">The room where the mesh will be created. If null, the current room is used.</param>
        private static void CreateDestructibleGlobalMesh(DestructibleGlobalMesh destructibleGlobalMesh,
            MRUKRoom room = null)
        {
            if (!room)
            {
                room = MRUK.Instance.GetCurrentRoom();
            }

            if (!room)
            {
                throw new Exception("Could not find a room for the destructible mesh");
            }

            if (!room.GlobalMeshAnchor || !room.GlobalMeshAnchor.GlobalMesh)
            {
                throw new Exception("Could not load the mesh associated with the global mesh anchor of the room");
            }

            var meshPositions = room.GlobalMeshAnchor.GlobalMesh.vertices;
            var meshIndices = room.GlobalMeshAnchor.GlobalMesh.triangles;
            var segmentationPointsWS = ComputeRoomBoxGrid(room, destructibleGlobalMesh.MaxPointsCount,
                destructibleGlobalMesh.PointsPerUnitX, destructibleGlobalMesh.PointsPerUnitY);

            var meshIndicesUint = Array.ConvertAll(meshIndices, Convert.ToUInt32);
            var segmentationPointsLS = new Vector3[segmentationPointsWS.Length];
            for (var i = 0; i < segmentationPointsWS.Length; i++)
            {
                segmentationPointsLS[i] = room.transform.InverseTransformPoint(segmentationPointsWS[i]);
            }

            destructibleGlobalMesh.DestructibleMeshComponent.SegmentMesh(meshPositions, meshIndicesUint,
                segmentationPointsLS);
        }

        /// <summary>
        /// Attempts to find a destructible mesh associated with a specific room. If found, the method returns true and provides the mesh via an out parameter.
        /// </summary>
        /// <param name="room">The room for which to find the destructible mesh.</param>
        /// <param name="destructibleGlobalMesh">Out parameter that will hold the destructible mesh if found.</param>
        /// <returns>False if the mesh is not found (i.e., default value is returned), otherwise true.</returns>
        public bool TryGetDestructibleMeshForRoom(MRUKRoom room, out DestructibleGlobalMesh destructibleGlobalMesh)
        {
            destructibleGlobalMesh = _spawnedDestructibleMeshes.GetValueOrDefault(room);
            return destructibleGlobalMesh != default(DestructibleGlobalMesh);
        }

        /// <summary>
        /// Removes a destructible global mesh from the specified room. If no room is specified, it defaults to the current room.
        /// </summary>
        /// <param name="room">The room from which the mesh will be removed. If null, the current room is used.</param>
        public void RemoveDestructibleGlobalMesh(MRUKRoom room = null)
        {
            if (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            {
                throw new Exception(
                    "Can not remove a destructible global mesh when MRUK instance has not been initialized.");
            }
            if (room == null)
            {
                room = MRUK.Instance.GetCurrentRoom();
            }

            if (TryGetDestructibleMeshForRoom(room, out var destructibleGlobalMesh))
            {
                Destroy(destructibleGlobalMesh.DestructibleMeshComponent.gameObject);
                _spawnedDestructibleMeshes.Remove(room);
            }
        }

        private void ReceiveCreatedRoom(MRUKRoom room)
        {
            if (CreateOnRoomLoaded == MRUK.RoomFilter.CurrentRoomOnly && _spawnedDestructibleMeshes.Count > 0)
            {
                return;
            }

            AddDestructibleGlobalMesh(room);
        }

        private void ReceiveRemovedRoom(MRUKRoom room)
        {
            if (room == null)
            {
                throw new Exception("Received a Room Removed event but the room is null.");
            }

            RemoveDestructibleGlobalMesh(room);
        }

        private static Vector3[] ComputeRoomBoxGrid(MRUKRoom room, int maxPointsCount, float pointsPerUnitX = 1.0f,
            float pointPerUnitY = 1.0f)
        {
            _points.Clear();
            foreach (MRUKAnchor wall in room.WallAnchors)
            {
                var points = GeneratePoints(wall.transform.position, wall.transform.rotation,
                    wall.PlaneRect, pointsPerUnitX, pointPerUnitY);
                _points.AddRange(points);
            }

            var ceilingHeight = room.CeilingAnchor.transform.position.y - room.FloorAnchor.transform.position.y;
            var planesCount = Mathf.Max(Mathf.Ceil(pointPerUnitY * ceilingHeight), 1);
            var spaceBetweenPlanes = ceilingHeight / planesCount;
            for (var i = 0; i < planesCount; i++)
            {
                var planePosition = new Vector3(room.CeilingAnchor.transform.position.x,
                    room.CeilingAnchor.transform.position.y - (spaceBetweenPlanes * i),
                    room.CeilingAnchor.transform.position.z);
                var insidePoints = GeneratePoints(planePosition, room.CeilingAnchor.transform.rotation,
                    room.CeilingAnchor.PlaneRect, pointsPerUnitX, pointPerUnitY);
                _points.AddRange(insidePoints);
            }

            var ceilingPoints = GeneratePoints(room.CeilingAnchor.transform.position,
                room.CeilingAnchor.transform.rotation, room.CeilingAnchor.PlaneRect, pointsPerUnitX, pointPerUnitY);
            _points.AddRange(ceilingPoints);

            var floorPoints = GeneratePoints(room.FloorAnchor.transform.position, room.FloorAnchor.transform.rotation,
                room.FloorAnchor.PlaneRect, pointsPerUnitX, pointPerUnitY);
            _points.AddRange(floorPoints);

            if (_points.Count > maxPointsCount)
            {
                Shuffle(_points);
                _points.RemoveRange(maxPointsCount, _points.Count - maxPointsCount);
            }

            return _points.ToArray();
        }

        private static List<Vector3> GeneratePoints(Vector3 position, Quaternion rotation, Rect? planeBounds,
            float pointsPerUnitX, float pointsPerUnitY)
        {
            if (!planeBounds.HasValue)
            {
                throw new Exception("Failed to generate points as the given plane has no bounds.");
            }

            var planeSize = new Vector3(planeBounds.Value.size.x, planeBounds.Value.size.y, 0);
            var planeBottomLeft = position - rotation * new Vector3(planeSize.x * 0.5f, planeSize.y * 0.5f);

            var pointsX = Mathf.Max(Mathf.Ceil(pointsPerUnitX * planeSize.x), 1);
            var pointsY = Mathf.Max(Mathf.Ceil(pointsPerUnitY * planeSize.y), 1);

            var stride = new Vector2(planeSize.x / (pointsX + 1), planeSize.y / (pointsY + 1));

            var points = OVRObjectPool.List<Vector3>();
            for (var y = 0; y < pointsY; y++)
            {
                for (var x = 0; x < pointsX; x++)
                {
                    var dx = (x + 1) * stride.x;
                    var dy = (y + 1) * stride.y;
                    var point = planeBottomLeft + rotation * new Vector3(dx, dy);
                    points.Add(point);
                }
            }

            return points;
        }

        private static void Shuffle<T>(List<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }

    /// <summary>
    /// The <c>DestructibleGlobalMesh</c> struct represents a destructible global mesh within a specific room.
    /// It includes functionality to create and manage the mesh based on room-specific parameters and global settings.
    /// </summary>
    public struct DestructibleGlobalMesh
    {
        /// <summary>
        /// The <see cref="MRUtilityKit.DestructibleMeshComponent"/> associated with this global mesh. This component handles the actual mesh manipulation, including segmentation and rendering, based on the parameters provided.
        /// </summary>
        public DestructibleMeshComponent DestructibleMeshComponent;

        /// <summary>
        ///The maximum number of points that the destructible mesh can contain. The higher number of points the higher the impact on performance.
        /// </summary>
        public int MaxPointsCount;

        /// <summary>
        /// Specifies the number of points per unit along the X-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance..
        /// </summary>
        public float PointsPerUnitX;

        /// <summary>
        /// Specifies the number of points per unit along the Y-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// </summary>
        public float PointsPerUnitY;

        private bool Equals(DestructibleGlobalMesh other)
        {
            return DestructibleMeshComponent == other.DestructibleMeshComponent &&
                   Equals(MaxPointsCount, other.MaxPointsCount) &&
                   Mathf.Approximately(PointsPerUnitX, other.PointsPerUnitX) &&
                   Mathf.Approximately(PointsPerUnitY, other.PointsPerUnitY);
        }

        // @cond
        public override bool Equals(object obj)
        {
            return obj is DestructibleGlobalMesh other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DestructibleMeshComponent, MaxPointsCount, PointsPerUnitX, PointsPerUnitY);
        }

        public static bool operator ==(DestructibleGlobalMesh left, DestructibleGlobalMesh right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DestructibleGlobalMesh left, DestructibleGlobalMesh right)
        {
            return !left.Equals(right);
        }
        // @endcond
    }
}
