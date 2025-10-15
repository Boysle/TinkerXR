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
using UnityEngine.Events;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine.Serialization;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Manages the creation and updating of a navigation mesh (NavMesh) for scene navigation.
    /// This class handles dynamic NavMesh generation based on scene data, including rooms and anchors, and responds to changes in the scene.
    /// </summary>
    [Feature(Feature.Scene)]
    public class SceneNavigation : MonoBehaviour
    {
        [Tooltip("When the scene data is loaded, this controls what room(s) will be used when baking the NavMesh.")]
        public MRUK.RoomFilter BuildOnSceneLoaded = MRUK.RoomFilter.CurrentRoomOnly;

        [Tooltip("If enabled, updates on scene elements such as rooms and anchors will be handled by this class")]
        internal bool TrackUpdates = true;

        [Header("Custom Nav Mesh Settings")]
        [Tooltip("Used for specifying the type of geometry to collect when building a NavMesh")]
        public NavMeshCollectGeometry CollectGeometry = NavMeshCollectGeometry.PhysicsColliders;

        [Tooltip("Used for specifying the type of objects to include when building a NavMesh")]
        public CollectObjects CollectObjects = CollectObjects.Children;

        [Tooltip("The minimum distance to the walls where the navigation mesh can exist.")]
        public float AgentRadius = 0.2f;

        [Tooltip("How much vertical clearance space must exist.")]
        public float AgentHeight = 0.5f;

        [Tooltip("The height of discontinuities in the level the agent can climb over (i.e. steps and stairs).")]
        public float AgentClimb = 0.04f;

        [Tooltip("Maximum slope the agent can walk up.")]
        public float AgentMaxSlope = 5.5f;

        [Tooltip("The agents that will be assigned to the NavMesh generated with the scene data.")]
        public List<NavMeshAgent> Agents;

        [FormerlySerializedAs("SceneObjectsToInclude")]
        [Header("Scene Settings")]
        [Tooltip("The scene objects that will contribute to the creation of the NavMesh.")]
        public MRUKAnchor.SceneLabels NavigableSurfaces;

        [Tooltip("The scene objects that will carve a hole in the NavMesh.")]
        public MRUKAnchor.SceneLabels SceneObstacles;

        [Tooltip("A bitmask representing the layers to consider when selecting what that will be used for baking.")]
        public LayerMask Layers;

        /// <summary>
        /// Event triggered when the navigation mesh has been initialized.
        /// </summary>
        [field: SerializeField, FormerlySerializedAs(nameof(OnNavMeshInitialized)), Space(10)]
        public UnityEvent OnNavMeshInitialized
        {
            get;
            private set;
        } = new();

        /// <summary>
        /// Gets a dictionary mapping <see cref="MRUKAnchor"/> objects to their corresponding GameObjects that represent obstacles in the environment.
        /// </summary>
        public Dictionary<MRUKAnchor, GameObject> Obstacles
        {
            get;
            private set;
        } = new();

        /// <summary>
        /// Gets a dictionary mapping <see cref="MRUKAnchor"/> objects to their corresponding GameObjects that represent surfaces in the environment.
        /// </summary>
        public Dictionary<MRUKAnchor, GameObject> Surfaces
        {
            get;
            private set;
        } = new();

        private const float _minimumNavMeshSurfaceArea = 0;
        private NavMeshSurface _navMeshSurface;
        private const string _obstaclePrefix = "_obstacles";
        private Transform _obstaclesRoot;
        private Transform _surfacesRoot;
        private const string _surfacesPrefix = "_surfaces";
        private MRUKAnchor.SceneLabels _cachedNavigableSceneLabels;

        private void Awake()
        {
            _navMeshSurface = gameObject.GetComponent<NavMeshSurface>();
            _cachedNavigableSceneLabels = NavigableSurfaces;
        }

        private void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneNavigation).Send();
#endif
            if (MRUK.Instance is null)
            {
                return;
            }

            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoadedEvent);

            if (!TrackUpdates)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
            MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
            MRUK.Instance.RoomUpdatedEvent.AddListener(ReceiveUpdatedRoom);
        }

        private void OnSceneLoadedEvent()
        {
            switch (BuildOnSceneLoaded)
            {
                case MRUK.RoomFilter.CurrentRoomOnly:
                    BuildSceneNavMeshForRoom(MRUK.Instance.GetCurrentRoom());
                    break;
                case MRUK.RoomFilter.AllRooms:
                    BuildSceneNavMesh();
                    break;
                case MRUK.RoomFilter.None:
                default:
                    break;
            }
        }

        private void ReceiveCreatedRoom(MRUKRoom room)
        {
            if (!TrackUpdates)
            {
                return;
            }

            BuildSceneNavMeshForRoom(room);
        }

        private void ReceiveUpdatedRoom(MRUKRoom room)
        {
            if (!TrackUpdates)
            {
                return;
            }

            RemoveNavMeshData();
            BuildSceneNavMeshForRoom(room);
        }

        private void ReceiveRemovedRoom(MRUKRoom room)
        {
            if (!TrackUpdates)
            {
                return;
            }

            RemoveNavMeshData();
        }


        /// <summary>
        ///     Toggles the use of global mesh for navigation.
        /// </summary>
        /// <param name="useGlobalMesh">Whether to use the global mesh to build the NavMesh</param>
        /// <param name="agentTypeID">
        ///     The agent type ID to use for creating the scene nav mesh,
        ///     if not specified, a new agent will be created.
        /// </param>
        public void ToggleGlobalMeshNavigation(bool useGlobalMesh, int agentTypeID = -1)
        {
            if (useGlobalMesh && MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor == null)
            {
                Debug.LogWarning("[MRUK] No Global Mesh anchor was found in the scene.");
                return;
            }

            if (useGlobalMesh)
            {
                _cachedNavigableSceneLabels = NavigableSurfaces;
                NavigableSurfaces = MRUKAnchor.SceneLabels.GLOBAL_MESH;
            }
            else
            {
                NavigableSurfaces = _cachedNavigableSceneLabels;
            }

            if (agentTypeID == -1)
            {
                BuildSceneNavMesh();
            }
            else
            {
                BuildSceneNavMeshFromExistingAgent(agentTypeID);
            }
        }


        /// <summary>
        ///     Creates a navigation mesh for the entire scene.
        /// </summary>
        public void BuildSceneNavMesh()
        {
            // Use all of the rooms
            BuildSceneNavMeshForRoom();
        }

        /// <summary>
        ///     Creates a navigation mesh for the scene.
        /// </summary>
        /// <param name="room">
        ///     Optional parameter for the MRUKRoom to create the NavMesh for.
        ///     If not provided, obstacles will be created for all rooms.
        /// </param>
        /// <remarks>
        ///     This method creates a navigation mesh by collecting geometry from the scene,
        ///     building the navigation mesh data, and adding it to the NavMesh.
        ///     Currently Unity does not allow the creation of custom NavMeshAgents at runtime.
        ///     It also assigns the created navigation mesh to all NavMeshAgents in the scene.
        /// </remarks>
        public void BuildSceneNavMeshForRoom(MRUKRoom room = null)
        {
            CreateNavMeshSurface(); // in case no NavMeshSurface component was found, create a new one
            RemoveNavMeshData(); // clean up previous data
            var navMeshBounds = ResizeNavMeshFromRoomBounds(ref _navMeshSurface); // resize the nav mesh to fit the room
            var navMeshBuildSettings = CreateNavMeshBuildSettings(AgentRadius, AgentHeight, AgentMaxSlope, AgentClimb);
            if (!ValidateBuildSettings(navMeshBuildSettings, navMeshBounds))
            {
                return;
            }

            CreateObstacles(room);
            var data = CreateNavMeshData(navMeshBounds, navMeshBuildSettings, room);
            _navMeshSurface.navMeshData = data;
            _navMeshSurface.agentTypeID = navMeshBuildSettings.agentTypeID;
            _navMeshSurface.AddData();

            InitializeNavMesh(navMeshBuildSettings.agentTypeID);
        }

        /// <summary>
        ///     Creates a navigation mesh from an existing NavMeshAgent.
        /// </summary>
        /// <param name="agentIndex">The index of the NavMeshAgent to create the navigation mesh from.</param>
        public void BuildSceneNavMeshFromExistingAgent(int agentIndex)
        {
            CreateNavMeshSurface();
            RemoveNavMeshData(); // clean up previous data
            var navMeshBuildSettings = NavMesh.GetSettingsByIndex(agentIndex);
            _navMeshSurface.agentTypeID = navMeshBuildSettings.agentTypeID;
            CreateObstacles();
            CreateNavigableSurfaces();
            _navMeshSurface.BuildNavMesh();
            InitializeNavMesh(navMeshBuildSettings.agentTypeID);
        }

        /// <summary>
        ///     Creates a new NavMeshBuildSettings object with the specified agent properties.
        /// </summary>
        /// <param name="agentRadius">The minimum distance to the walls where the navigation mesh can exist.</param>
        /// <param name="agentHeight">How much vertical clearance space must exist.</param>
        /// <param name="agentMaxSlope">Maximum slope the agent can walk up.</param>
        /// <param name="agentClimb">The height of discontinuities in the level the agent can climb over (i.e. steps and stairs).</param>
        /// <returns>A new NavMeshBuildSettings object with the specified agent properties.</returns>
        public NavMeshBuildSettings CreateNavMeshBuildSettings(float agentRadius, float agentHeight,
            float agentMaxSlope, float agentClimb)
        {
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = agentRadius;
            settings.agentHeight = agentHeight;
            settings.agentSlope = agentMaxSlope;
            settings.agentClimb = agentClimb;
            return settings;
        }

        /// <summary>
        ///     Creates a NavMeshSurface component and sets its properties.
        /// </summary>
        public void CreateNavMeshSurface()
        {
            _navMeshSurface = GetComponent<NavMeshSurface>();
            if (!_navMeshSurface)
            {
                _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }

            ResizeNavMeshFromRoomBounds(ref _navMeshSurface);
            _navMeshSurface.collectObjects = CollectObjects;
            _navMeshSurface.useGeometry = CollectGeometry;
            _navMeshSurface.layerMask = Layers;
        }

        /// <summary>
        ///     Removes the NavMeshData from the NavMeshSurface component.
        /// </summary>
        public void RemoveNavMeshData()
        {
            if (!_navMeshSurface)
            {
                return;
            }

            _navMeshSurface.navMeshData = null;
            _navMeshSurface.RemoveData();

            if (Obstacles != null)
            {
                ClearObstacles();
            }

            if (Surfaces != null)
            {
                ClearSurfaces();
            }
        }


        /// <summary>
        ///     Resizes the NavMeshSurface to fit the room bounds.
        /// </summary>
        /// <param name="surface">The NavMeshSurface to resize.</param>
        /// <param name="room">The room bounds to use. Default is the current room.</param>
        /// <returns>The bounds of the resized NavMeshSurface.</returns>
        public Bounds ResizeNavMeshFromRoomBounds(ref NavMeshSurface surface, MRUKRoom room = null)
        {
            if (room == null)
            {
                room = MRUK.Instance.GetCurrentRoom();
            }

            var mapBounds = room.GetRoomBounds();
            var mapCenter = new Vector3(mapBounds.center.x, mapBounds.min.y, mapBounds.center.z);
            surface.center = mapCenter;

            var mapScale = new Vector3(mapBounds.size.x, 2f, mapBounds.size.z);
            surface.size = mapScale;
            var bounds = new Bounds(surface.center, Abs(surface.size));
            return bounds;
        }

        /// <summary>
        ///     Initializes the navigation mesh with the given agent type ID.
        /// </summary>
        /// <param name="agentTypeID">The agent type ID to initialize the navigation mesh with.</param>
        private void InitializeNavMesh(int agentTypeID)
        {
            if (_navMeshSurface.navMeshData.sourceBounds.extents.x *
                _navMeshSurface.navMeshData.sourceBounds.extents.z >
                _minimumNavMeshSurfaceArea)
            {
                if (Agents != null)
                {
                    foreach (var agent in Agents)
                    {
                        agent.agentTypeID = agentTypeID;
                    }
                }

                OnNavMeshInitialized?.Invoke();
            }
            else
            {
                Debug.LogWarning("Failed to generate a nav mesh, this may be because the room is too small" +
                                 " or the AgentType settings are to strict");
            }
        }

        /// <summary>
        ///     Creates NavMeshData for the given bounds and build settings.
        /// </summary>
        /// <param name="navMeshBounds">The bounds for the NavMesh.</param>
        /// <param name="navMeshBuildSettings">The build settings for the NavMesh.</param>
        /// <param name="room">The room for which creating the NavMesh.</param>
        /// <returns>The created NavMeshData.</returns>
        private NavMeshData CreateNavMeshData(Bounds navMeshBounds, NavMeshBuildSettings navMeshBuildSettings,
            MRUKRoom room = null)
        {
            List<NavMeshBuildSource> sources = new();
            if (_navMeshSurface.collectObjects == CollectObjects.Volume)
            {
                NavMeshBuilder.CollectSources(navMeshBounds, _navMeshSurface.layerMask,
                    _navMeshSurface.useGeometry, 0, new List<NavMeshBuildMarkup>(), sources);
            }
            else
            {
                CreateNavigableSurfaces(room);
                NavMeshBuilder.CollectSources(transform, _navMeshSurface.layerMask,
                    _navMeshSurface.useGeometry, 0, new List<NavMeshBuildMarkup>(), sources);
            }

            var data = NavMeshBuilder.BuildNavMeshData(navMeshBuildSettings, sources, navMeshBounds,
                transform.position, transform.rotation);
            return data;
        }

        /// <summary>
        ///     Creates obstacles for the given MRUKRoom or all rooms if no room is specified.
        /// </summary>
        /// <param name="room">
        ///     Optional parameter for the MRUKRoom to create the obstacles for.
        ///     If not provided, obstacles will be created for all rooms.
        /// </param>
        public void CreateObstacles(MRUKRoom room = null)
        {
            var rooms = new List<MRUKRoom>();
            if (room)
            {
                rooms.Add(room);
            }
            else
            {
                rooms = MRUK.Instance.Rooms;
            }

            if (_obstaclesRoot == null)
            {
                _obstaclesRoot = new GameObject(_obstaclePrefix).transform;
            }

            _obstaclesRoot.transform.SetParent(transform);
            foreach (var _room in rooms)
            {
                var sceneAnchors = _room.Anchors;
                foreach (var anchor in sceneAnchors)
                {
                    CreateObstacle(anchor);
                }
            }
        }

        /// <summary>
        ///     Creates a NavMeshObstacle for the given MRUKAnchor.
        /// </summary>
        /// <param name="anchor">The MRUKAnchor to create the obstacle for.</param>
        /// <param name="shouldCarve">Optional parameter that determines whether the obstacle should carve the NavMesh. Default is true.</param>
        /// <param name="carveOnlyStationary">
        ///     Optional parameter that determines whether the obstacle should only carve
        ///     the NavMesh when stationary. Default is true.
        /// </param>
        /// <param name="carvingTimeToStationary">
        ///     Optional parameter that sets the time in seconds an obstacle must be
        ///     stationary before it starts carving the NavMesh. Default is 0.2f.
        /// </param>
        /// <param name="carvingMoveThreshold">
        ///     Optional parameter that sets the minimum world space distance the
        ///     obstacle must move before it is considered moving. Default is 0.2f.
        /// </param>
        public void CreateObstacle(MRUKAnchor anchor, bool shouldCarve = true, bool carveOnlyStationary = true,
            float carvingTimeToStationary = 0.2f, float carvingMoveThreshold = 0.2f)
        {
            Vector3 obstacleSize, obstacleCenter;
            if (!anchor || !anchor.HasAnyLabel(SceneObstacles))
            {
                return;
            }

            if (Obstacles.ContainsKey(anchor))
            {
                Debug.LogWarning("Anchor already associated with an obstacle from this SceneNavigation");
                return;
            }

            if (anchor.VolumeBounds != null)
            {
                obstacleSize = anchor.VolumeBounds.Value.size;
                obstacleCenter = anchor.VolumeBounds.Value.center;
            }
            else if (anchor.PlaneRect != null)
            {
                obstacleSize = anchor.PlaneRect.Value.size;
                obstacleCenter = anchor.PlaneRect.Value.center;
            }
            else
            {
                // the anchor cannot be an obstacle
                return;
            }

            var obstacleGO = new GameObject($"{_obstaclePrefix}_{anchor.name}");
            obstacleGO.transform.SetParent(_obstaclesRoot.transform);
            var obstacle = obstacleGO.AddComponent<NavMeshObstacle>();
            obstacle.carving = shouldCarve;
            obstacle.carveOnlyStationary = carveOnlyStationary;
            obstacle.carvingTimeToStationary = carvingTimeToStationary;
            obstacle.carvingMoveThreshold = carvingMoveThreshold;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.transform.position = anchor.transform.position;
            obstacle.transform.rotation = anchor.transform.rotation;
            obstacle.size = obstacleSize;
            obstacle.center = obstacleCenter;
            Obstacles.Add(anchor, obstacleGO);
        }


        /// <summary>
        ///     Creates navigable surfaces for the given MRUKRoom or all rooms if no room is specified.
        /// </summary>
        /// <param name="room">
        ///     Optional parameter for the MRUKRoom to create the navigable surfaces for.
        ///     If not provided, navigable surfaces will be created for all rooms.
        /// </param>
        /// <remarks>
        ///     Creating surfaces will not automatically build a new NavMesh. When changing surfaces at run time,
        ///     always use <see cref="BuildSceneNavMesh" /> method
        /// </remarks>
        public void CreateNavigableSurfaces(MRUKRoom room = null)
        {
            var rooms = new List<MRUKRoom>();
            if (room)
            {
                rooms.Add(room);
            }
            else
            {
                rooms = MRUK.Instance.Rooms;
            }

            if (_surfacesRoot == null)
            {
                _surfacesRoot = new GameObject(_surfacesPrefix).transform;
            }

            _surfacesRoot.transform.SetParent(transform);
            foreach (var _room in rooms)
            {
                var sceneAnchors = _room.Anchors;
                foreach (var anchor in sceneAnchors)
                {
                    CreateNavigableSurface(anchor);
                }
            }

            _navMeshSurface.collectObjects = CollectObjects.Children;
        }

        /// <summary>
        ///     Creates a navigable surface for the given MRUKAnchor.
        /// </summary>
        /// <param name="anchor">The MRUKAnchor to create the navigable surface for.</param>
        private async void CreateNavigableSurface(MRUKAnchor anchor)
        {
            Vector3 surfaceSize, surfaceCenter;
            if (!anchor || !anchor.HasAnyLabel(NavigableSurfaces))
            {
                return;
            }

            var surfaceGO = new GameObject($"{_surfacesPrefix}_{anchor.name}");
            surfaceGO.transform.SetParent(_surfacesRoot.transform);
            surfaceGO.gameObject.layer = GetFirstLayerFromLayerMask(Layers);
            if (!anchor || !anchor.HasAnyLabel(NavigableSurfaces))
            {
                return;
            }

            if (Surfaces.ContainsKey(anchor))
            {
                Debug.LogWarning("Anchor already associated with an obstacle from this SceneNavigation");
                return;
            }

            if (anchor.VolumeBounds != null)
            {
                surfaceSize = anchor.VolumeBounds.Value.size;
                surfaceCenter = anchor.VolumeBounds.Value.center;
            }
            else if (anchor.PlaneRect != null)
            {
                surfaceSize = anchor.PlaneRect.Value.size;
                surfaceCenter = anchor.PlaneRect.Value.center;
            }
            else
            {
                // global mesh
                if (anchor.GlobalMesh == null)
                {
                    anchor.Anchor.TryGetComponent(out OVRLocatable locatable);
                    await locatable.SetEnabledAsync(true);

                    if (!locatable.TryGetSceneAnchorPose(out var pose))
                    {
                        return;
                    }

                    var pos = pose.ComputeWorldPosition(MRUK.Instance._cameraRig.trackingSpace);
                    var rot = pose.ComputeWorldRotation(MRUK.Instance._cameraRig.trackingSpace);
                    if (!pos.HasValue || !rot.HasValue)
                    {
                        return;
                    }

                    anchor.transform.SetPositionAndRotation(pos.Value, rot.Value);
                    anchor.GlobalMesh = anchor.LoadGlobalMeshTriangles();
                }

                var trimesh = anchor.GlobalMesh;

                var meshCollider = surfaceGO.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = trimesh;
                meshCollider.transform.position = anchor.transform.position;
                meshCollider.transform.rotation = anchor.transform.rotation;
                Surfaces.Add(anchor, surfaceGO);
                return;
            }

            var surfaceCollider = surfaceGO.AddComponent<BoxCollider>();
            surfaceCollider.transform.position = anchor.transform.position;
            surfaceCollider.transform.rotation = anchor.transform.rotation;
            surfaceCollider.size = surfaceSize;
            surfaceCollider.center = surfaceCenter;

            Surfaces.Add(anchor, surfaceGO);
        }

        /// <summary>
        ///     Clears all obstacles from the Obstacles dictionary associated with the given MRUKRoom.
        ///     If no room is specified, all obstacles are cleared.
        /// </summary>
        /// <param name="room">
        ///     Optional parameter for the MRUKRoom to clear the obstacles for.
        ///     If not provided, all obstacles will be cleared.
        /// </param>
        public void ClearObstacles(MRUKRoom room = null)
        {
            List<MRUKAnchor> obstaclesToRemove = new();
            foreach (var kv in Obstacles)
            {
                if (room != null && kv.Key.Room != room)
                {
                    continue;
                }

                DestroyImmediate(kv.Value);
                obstaclesToRemove.Add(kv.Key);
            }

            foreach (var anchor in obstaclesToRemove)
            {
                Obstacles.Remove(anchor);
            }
        }

        /// <summary>
        ///     Clears the obstacle associated with the given MRUKAnchor from the Obstacles dictionary.
        /// </summary>
        /// <param name="anchor">The MRUKAnchor whose associated obstacle should be cleared.</param>
        public void ClearObstacle(MRUKAnchor anchor)
        {
            if (!Obstacles.ContainsKey(anchor))
            {
                return;
            }

            DestroyImmediate(Obstacles[anchor]);
            Obstacles.Remove(anchor);
        }

        /// <summary>
        ///     Clears all surfaces from the Obstacles dictionary associated with the given MRUKRoom.
        ///     If no room is specified, all obstacles are cleared.
        /// </summary>
        /// <param name="room">
        ///     Optional parameter for the MRUKRoom to clear the surfaces for.
        ///     If not provided, all obstacles will be cleared.
        /// </param>
        /// <remarks>
        ///     Removing surfaces will not automatically build a new NavMesh. When changing surfaces at run time,
        ///     always use <see cref="BuildSceneNavMesh" /> method
        /// </remarks>
        private void ClearSurfaces(MRUKRoom room = null)
        {
            List<MRUKAnchor> surfacesToRemove = new();
            foreach (var kv in Surfaces)
            {
                if (room != null && kv.Key.Room != room)
                {
                    continue;
                }

                DestroyImmediate(kv.Value);
                surfacesToRemove.Add(kv.Key);
            }

            foreach (var anchor in surfacesToRemove)
            {
                Surfaces.Remove(anchor);
            }
        }

        /// <summary>
        ///     Clears the surface associated with the given MRUKAnchor from the Obstacles dictionary.
        /// </summary>
        /// <param name="anchor">The MRUKAnchor whose associated obstacle should be cleared.</param>
        public void ClearSurface(MRUKAnchor anchor)
        {
            if (!Surfaces.ContainsKey(anchor))
            {
                return;
            }

            DestroyImmediate(Surfaces[anchor]);
            Surfaces.Remove(anchor);
        }

        /// <summary>
        ///     Gets the first layer included in the given LayerMask.
        /// </summary>
        /// <param name="layerMask">The LayerMask to get the first layer from.</param>
        /// <returns>Returns the first layer included in the LayerMask.</returns>
        public static int GetFirstLayerFromLayerMask(LayerMask layerMask)
        {
            var layer = 0;
            for (var i = 0; i < 32; i++) // max number of layers in Unity
            {
                if (((1 << i) & layerMask) == 0)
                {
                    continue;
                }

                layer = i;
                break;
            }

            return layer;
        }

        /// <summary>
        ///     Validates the provided NavMeshBuildSettings against the provided NavMeshBounds.
        /// </summary>
        /// <param name="navMeshBuildSettings">The NavMeshBuildSettings to validate.</param>
        /// <param name="navMeshBounds">The Bounds to validate the NavMeshBuildSettings against.</param>
        /// <returns>Returns true if the NavMeshBuildSettings are valid, false otherwise.</returns>
        public static bool ValidateBuildSettings(NavMeshBuildSettings navMeshBuildSettings, Bounds navMeshBounds)
        {
            var report = navMeshBuildSettings.ValidationReport(navMeshBounds);
            if (report.Length <= 0)
            {
                return true;
            }

            var warning = "Some NavMeshBuildSettings constraints were violated:\n";
            foreach (var violation in report)
            {
                warning += "- " + violation + "\n";
            }

            Debug.LogWarning(warning);
            return false;
        }

        static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        private void OnDestroy()
        {
            OnNavMeshInitialized.RemoveAllListeners();
            if (!MRUK.Instance)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.RemoveListener(ReceiveCreatedRoom);
            MRUK.Instance.RoomRemovedEvent.RemoveListener(ReceiveRemovedRoom);
            MRUK.Instance.RoomUpdatedEvent.RemoveListener(ReceiveUpdatedRoom);
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoadedEvent);
        }
    }
}
