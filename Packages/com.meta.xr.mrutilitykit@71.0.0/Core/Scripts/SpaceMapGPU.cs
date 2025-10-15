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
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Manages the creation and updating of a <see cref="SpaceMap"/> using GPU resources. This class is designed to handle
    /// dynamic environments, providing real-time updates to the spatial map.
    /// </summary>
    public class SpaceMapGPU : MonoBehaviour
    {
        /// <summary>
        /// Event triggered when the space map is initially created
        /// </summary>
        [field: SerializeField]
        public UnityEvent SpaceMapCreatedEvent
        {
            get;
            private set;
        } = new();

        /// <summary>
        /// Event triggered when the space map is updated.
        /// </summary>
        [field: SerializeField]
        public UnityEvent SpaceMapUpdatedEvent
        {
            get;
            private set;
        } = new();

        [Tooltip("When the scene data is loaded, this controls what room(s) the prefabs will spawn in.")]
        [Header("Scene and Room Settings")]
        public MRUK.RoomFilter CreateOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        [Tooltip("If enabled, updates on scene elements such as rooms and anchors will be handled by this class")]
        internal bool TrackUpdates = true;

        [Space]
        [Header("Textures")]
        [SerializeField]
        [Tooltip("Use this dimension for SpaceMap in X and Y")]
        public int TextureDimension = 512;

        [Tooltip("Colorize the SpaceMap with this Gradient")]
        public Gradient MapGradient = new();

        [Space]
        [Header("SpaceMap Settings")]
        [SerializeField]
        private Material gradientMaterial;

        [SerializeField] private ComputeShader CSSpaceMap;

        [Tooltip("All objects will be rendered into this Layer. Best practise is to define an own Layer for SpaceMap")]
        [SerializeField]
        public LayerMask layerMask;

        private MRUKAnchor.SceneLabels FloorWallLabel = MRUKAnchor.SceneLabels.FLOOR;

        [Tooltip("Those Labels will be taken into account when running the SpaceMap")]
        [SerializeField]
        private MRUKAnchor.SceneLabels SceneObjectLabels;

        [Tooltip("Set a color for the inside of an Object")]
        [SerializeField]
        private Color InsideObjectColor;

        [Tooltip("Add this to the border of the capture Camera")]
        [SerializeField]
        private float CameraCaptureBorderBuffer = 0.5f;

        [Space]
        [Header("SpaceMap Debug Settings")]
        [SerializeField]
        [Tooltip("This setting affects your performance. If enabled, the TextureMap will be filled with the SpaceMap")]
        private bool CreateOutputTexture;

        [Tooltip("The Spacemap will be rendered into this Texture.")]
        [SerializeField]
        internal Texture2D OutputTexture;

        [Tooltip("Add here a debug plane")]
        [SerializeField]
        private GameObject DebugPlane;

        [SerializeField] private bool ShowDebugPlane;

        private Color colorFloorWall = Color.red;
        private Color colorSceneObjects = Color.green;
        private Color colorVirtualObjects = Color.blue;

        private Camera _captureCamera;
        private readonly float _cameraDistance = 10f;

        private RenderTexture[] _RTextures;

        private EffectMesh _effectMeshFloor;
        private EffectMesh _effectMeshObjects;

        private const string OculusUnlitShader = "Oculus/Unlit";

        private Texture2D _gradientTexture;

        private int _csSpaceMapKernel;
        private int _csFillSpaceMapKernel;
        private int _csPrepareSpaceMapKernel;

        internal bool Dirty => _isDirty;
        private bool _isDirty = false;

        private const string SHADER_GLOBAL_SPACEMAPCAMERAMATRIX = "_SpaceMapProjectionViewMatrix";

        [SerializeField]
        private RenderTexture RenderTexture;


        private static readonly int
            WidthID = Shader.PropertyToID("Width"),
            HeightID = Shader.PropertyToID("Height"),
            ColorFloorWallID = Shader.PropertyToID("ColorFloorWall"),
            ColorSceneObjectsID = Shader.PropertyToID("ColorSceneObjects"),
            ColorVirtualObjectsID = Shader.PropertyToID("ColorVirtualObjects"),
            StepID = Shader.PropertyToID("Step"),
            SourceID = Shader.PropertyToID("Source"),
            ResultID = Shader.PropertyToID("Result");

        /// <summary>
        /// Initiates the space mapping process based on the specified room filter. This method sets up the necessary components
        /// and configurations to generate the space map, including updating textures, initializing effect meshes, setting up the capture camera,
        /// and applying materials.
        /// </summary>
        /// <param name="roomFilter">The <see cref="MRUK.RoomFilter"/> that determines which rooms are included in the space map,
        /// influencing how the space map is generated.</param>
        public async void StartSpaceMap(MRUK.RoomFilter roomFilter)
        {
            _isDirty = true;
            await InitUpdateGradientTexture();
            InitEffectMesh(roomFilter);
            InitializeCaptureCamera(roomFilter);
            ApplyMaterial();

            SpaceMapCreatedEvent.Invoke();
        }

        private void Start()
        {
            _RTextures = new RenderTexture[2];

            //kernels for compute shader
            _csSpaceMapKernel = CSSpaceMap.FindKernel("SpaceMap");
            _csFillSpaceMapKernel = CSSpaceMap.FindKernel("FillSpaceMap");
            _csPrepareSpaceMapKernel = CSSpaceMap.FindKernel("PrepareSpaceMap");

            if (MRUK.Instance is null)
            {
                return;
            }

            _isDirty = true;

            MRUK.Instance.RegisterSceneLoadedCallback(() =>
            {
                if (CreateOnStart == MRUK.RoomFilter.None)
                {
                    return;
                }

                StartSpaceMap(CreateOnStart);
            });

            if (!TrackUpdates)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
            MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
        }

        private void OnDestroy()
        {
            if (MRUK.Instance == null)
            {
                return;
            }

            MRUK.Instance.RoomCreatedEvent.RemoveListener(ReceiveCreatedRoom);
            MRUK.Instance.RoomRemovedEvent.RemoveListener(ReceiveRemovedRoom);
        }

        private void ReceiveCreatedRoom(MRUKRoom room)
        {
            //only create the effect mesh when we track room updates
            if (TrackUpdates &&
                CreateOnStart == MRUK.RoomFilter.AllRooms)
            {
                if (_effectMeshFloor == null || _effectMeshObjects == null)
                {
                    _effectMeshFloor = InitEffectMeshComponent(_effectMeshFloor, colorFloorWall, FloorWallLabel);
                    _effectMeshObjects = InitEffectMeshComponent(_effectMeshObjects, colorSceneObjects, SceneObjectLabels);
                }
                _effectMeshFloor.CreateMesh(room);
                _effectMeshObjects.CreateMesh(room);

                RegisterAnchorUpdates(room);
                _isDirty = true;
            }
        }

        private void ReceiveRemovedRoom(MRUKRoom room)
        {
            _effectMeshFloor.DestroyMesh(room);
            _effectMeshObjects.DestroyMesh(room);
            UnregisterAnchorUpdates(room);
            _isDirty = true;
        }

        private void UnregisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.RemoveListener(ReceiveAnchorCreatedEvent);
            room.AnchorRemovedEvent.RemoveListener(ReceiveAnchorRemovedCallback);
            room.AnchorUpdatedEvent.RemoveListener(ReceiveAnchorUpdatedCallback);
        }

        private void RegisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.AddListener(ReceiveAnchorCreatedEvent);
            room.AnchorRemovedEvent.AddListener(ReceiveAnchorRemovedCallback);
            room.AnchorUpdatedEvent.AddListener(ReceiveAnchorUpdatedCallback);
        }

        private void ReceiveAnchorUpdatedCallback(MRUKAnchor anchor)
        {
            // only update the anchor when we track updates
            if (!TrackUpdates)
            {
                return;
            }
            CreateEffectMesh(anchor);
            DestroyEffectMesh(anchor);
            _isDirty = true;
        }

        private void ReceiveAnchorRemovedCallback(MRUKAnchor anchor)
        {
            // there is no check on ```TrackUpdates``` when removing an anchor.
            DestroyEffectMesh(anchor);
            _isDirty = true;
        }

        private void ReceiveAnchorCreatedEvent(MRUKAnchor anchor)
        {
            // only create the anchor when we track updates
            if (!TrackUpdates)
            {
                return;
            }
            CreateEffectMesh(anchor);
            _isDirty = true;
        }

        private void DestroyEffectMesh(MRUKAnchor anchor)
        {
            if (anchor.Label == MRUKAnchor.SceneLabels.FLOOR)
            {
                _effectMeshFloor.DestroyMesh(anchor);

            }
            else
            {
                _effectMeshObjects.DestroyMesh(anchor);
            }
        }

        private void CreateEffectMesh(MRUKAnchor anchor)
        {
            if (_effectMeshFloor == null || _effectMeshObjects == null)
            {
                _effectMeshFloor = InitEffectMeshComponent(_effectMeshFloor, colorFloorWall, FloorWallLabel);
                _effectMeshObjects = InitEffectMeshComponent(_effectMeshObjects, colorSceneObjects, SceneObjectLabels);
            }

            if (anchor.Label == MRUKAnchor.SceneLabels.FLOOR)
            {
                _effectMeshFloor.CreateEffectMesh(anchor);

            }
            else
            {
                _effectMeshObjects.CreateEffectMesh(anchor);
            }
        }


        private void Update()
        {
            if (_captureCamera != null)
            {
                Shader.SetGlobalMatrix(SHADER_GLOBAL_SPACEMAPCAMERAMATRIX, _captureCamera.projectionMatrix * _captureCamera.worldToCameraMatrix);
            }

            if (DebugPlane != null && DebugPlane.activeSelf != ShowDebugPlane)
            {
                DebugPlane.SetActive(ShowDebugPlane);
            }
        }

        /// <summary>
        /// Color clamps to edge color if worldPosition is off-grid.
        /// getBilinear blends the color between pixels.
        /// </summary>
        /// <param name="worldPosition">The world position to sample the color from.</param>
        /// <returns>The color at the specified world position. Returns black if the capture camera is not initialized.</returns>
        public Color GetColorAtPosition(Vector3 worldPosition)
        {
            if (_captureCamera == null)
            {
                return Color.black;
            }

            var worldToScreenPoint = _captureCamera.WorldToScreenPoint(worldPosition);

            var xPixel = worldToScreenPoint.x / _captureCamera.pixelWidth;
            var yPixel = worldToScreenPoint.y / _captureCamera.pixelHeight;

            var rawColor = OutputTexture.GetPixelBilinear(xPixel, yPixel);

            return rawColor.b > 0 ? InsideObjectColor : MapGradient.Evaluate(1 - rawColor.r);
        }

        private void InitEffectMesh(MRUK.RoomFilter roomFilter)
        {
            _effectMeshFloor = InitEffectMeshComponent(_effectMeshFloor, colorFloorWall, FloorWallLabel);
            _effectMeshObjects = InitEffectMeshComponent(_effectMeshObjects, colorSceneObjects, SceneObjectLabels);
            switch (roomFilter)
            {
                case MRUK.RoomFilter.CurrentRoomOnly:
                    _effectMeshFloor.CreateMesh(MRUK.Instance.GetCurrentRoom());
                    _effectMeshObjects.CreateMesh(MRUK.Instance.GetCurrentRoom());
                    break;
                case MRUK.RoomFilter.AllRooms:
                    _effectMeshFloor.CreateMesh();
                    _effectMeshObjects.CreateMesh();
                    break;
            }
        }

        private EffectMesh InitEffectMeshComponent(EffectMesh effectMesh, Color color, MRUKAnchor.SceneLabels labels)
        {
            if (effectMesh == null)
            {
                effectMesh = gameObject.AddComponent<EffectMesh>();
                effectMesh.MeshMaterial = new Material(Shader.Find(OculusUnlitShader)) { color = color };
                effectMesh.HideMesh = false;
                effectMesh.Labels = labels;
                effectMesh.Layer = LayerToInt();
                effectMesh.CastShadow = false;
                effectMesh.TrackUpdates = true;
            }

            return effectMesh;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!_isDirty)
            {
                return;
            }

            InitUpdateRT();
            if (_RTextures[0] == null)
            {
                return; //initialize phase
            }

            CSSpaceMap.SetInt(WidthID, (int)TextureDimension);
            CSSpaceMap.SetInt(HeightID, (int)TextureDimension);
            CSSpaceMap.SetVector(ColorFloorWallID, colorFloorWall);
            CSSpaceMap.SetVector(ColorSceneObjectsID, colorSceneObjects);
            CSSpaceMap.SetVector(ColorVirtualObjectsID, colorVirtualObjects);

            var threadGroupsX = Mathf.CeilToInt(TextureDimension / 8.0f);
            var threadGroupsY = Mathf.CeilToInt(TextureDimension / 8.0f);

            CSSpaceMap.SetTexture(_csPrepareSpaceMapKernel, SourceID, source);
            CSSpaceMap.SetTexture(_csPrepareSpaceMapKernel, ResultID, _RTextures[0]);
            CSSpaceMap.Dispatch(_csPrepareSpaceMapKernel, threadGroupsX, threadGroupsY, 1);

            var stepAmount = (int)Mathf.Log(TextureDimension, 2);

            int sourceIndex = 0, resultIndex = 0;

            for (var i = 0; i < stepAmount; i++)
            {
                var step = (int)Mathf.Pow(2, stepAmount - i - 1);

                sourceIndex = i % 2;
                resultIndex = (i + 1) % 2;

                CSSpaceMap.SetInt(StepID, step);
                CSSpaceMap.SetTexture(_csSpaceMapKernel, SourceID, _RTextures[sourceIndex]);
                CSSpaceMap.SetTexture(_csSpaceMapKernel, ResultID, _RTextures[resultIndex]);
                CSSpaceMap.Dispatch(_csSpaceMapKernel, threadGroupsX, threadGroupsY, 1);
            }

            //swap indexes to get the correct one for source again
            CSSpaceMap.SetTexture(_csFillSpaceMapKernel, SourceID, _RTextures[resultIndex]);
            CSSpaceMap.SetTexture(_csFillSpaceMapKernel, ResultID, _RTextures[sourceIndex]);
            CSSpaceMap.Dispatch(_csFillSpaceMapKernel, threadGroupsX, threadGroupsY, 1);

            if (CreateOutputTexture)
            {
                Graphics.Blit(_RTextures[sourceIndex], destination);

                RenderTexture.active = destination;
                OutputTexture.ReadPixels(new Rect(0, 0, TextureDimension, TextureDimension), 0, 0);
                OutputTexture.Apply();
            }

            gradientMaterial.SetTexture("_MainTex", _RTextures[sourceIndex]);
            SpaceMapUpdatedEvent.Invoke();
            _isDirty = false;
        }

        private void InitUpdateRT()
        {
            var wh = TextureDimension;

            if (_RTextures[0] == null || _RTextures[0].width != wh || _RTextures[0].height != wh)
            {
                TryReleaseRT(_RTextures[0]);
                TryReleaseRT(_RTextures[1]);
                _RTextures[0] = CreateNewRenderTexture(wh);
                _RTextures[1] = CreateNewRenderTexture(wh);
            }

            var tmpRT = RenderTexture.active;
            RenderTexture.active = _RTextures[0];
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = tmpRT;
        }

        private static RenderTexture CreateNewRenderTexture(int wh)
        {
            var rt = new RenderTexture(wh, wh, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
            rt.Create();
            return rt;
        }

        private static void TryReleaseRT(RenderTexture renderTexture)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
        }

        private void ApplyMaterial()
        {
            gradientMaterial.SetTexture("_GradientTex", _gradientTexture);
            gradientMaterial.SetColor("_InsideColor", InsideObjectColor);
            if (DebugPlane != null)
            {
                DebugPlane.GetComponent<Renderer>().material = gradientMaterial;
            }
        }

        private async Task InitUpdateGradientTexture()
        {
            if (_gradientTexture == null)
            {
                _gradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            }

            for (var i = 0; i <= _gradientTexture.width; i++)
            {
                var t = i / (_gradientTexture.width - 1f);
                _gradientTexture.SetPixel(i, 0, MapGradient.Evaluate(t));
            }

            SynchronizationContext unityContext = SynchronizationContext.Current;
            await Task.Run(() =>
            {
                unityContext.Post(_ =>
                {
                    _gradientTexture.Apply();
                }, null);
            });
        }

        private void InitializeCaptureCamera(MRUK.RoomFilter roomFilter)
        {
            if (_captureCamera == null)
            {
                _captureCamera = gameObject.AddComponent<Camera>();
            }

            _captureCamera.orthographic = true;
            _captureCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _captureCamera.targetDisplay = 7;
            _captureCamera.cullingMask = layerMask;

            _captureCamera.targetTexture = RenderTexture;

            var bb = GetBoudingBoxByFilter(roomFilter);
            transform.position = CalculateCameraPosition(bb);
            _captureCamera.orthographicSize = CalculateOrthographicSize(bb);
        }

        private Rect GetBoudingBoxByFilter(MRUK.RoomFilter roomFilter)
        {
            HashSet<Transform> targets = new();
            switch (roomFilter)
            {
                case MRUK.RoomFilter.CurrentRoomOnly:
                    foreach (var anchor in MRUK.Instance.GetCurrentRoom().Anchors)
                    {
                        if (anchor.HasAnyLabel(SceneObjectLabels))
                        {
                            targets.Add(anchor.transform);
                        }
                    }

                    break;
                case MRUK.RoomFilter.AllRooms:
                    foreach (var room in MRUK.Instance.Rooms)
                    {
                        foreach (var anchor in room.Anchors)
                        {
                            if (anchor.HasAnyLabel(SceneObjectLabels))
                            {
                                targets.Add(anchor.transform);
                            }
                        }
                    }

                    break;
            }

            var minX = Mathf.Infinity;
            var maxX = Mathf.NegativeInfinity;
            var minZ = Mathf.Infinity;
            var maxZ = Mathf.NegativeInfinity;
            foreach (Transform target in targets)
            {
                Vector3 position = target.position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minZ = Mathf.Min(minZ, position.z);
                maxZ = Mathf.Max(maxZ, position.z);
            }

            if (DebugPlane != null)
            {
                var sizeX = (maxX - minX + 2 * CameraCaptureBorderBuffer) / 10f;
                var sizeZ = (maxZ - minZ + 2 * CameraCaptureBorderBuffer) / 10f;

                var centerX = (minX + maxX) / 2;
                var centerZ = (minZ + maxZ) / 2;

                DebugPlane.transform.localScale = new Vector3(sizeX, 1, sizeZ);
                DebugPlane.transform.position = new Vector3(centerX, DebugPlane.transform.position.y, centerZ);
            }

            return Rect.MinMaxRect(minX - CameraCaptureBorderBuffer, maxZ + CameraCaptureBorderBuffer,
                maxX + CameraCaptureBorderBuffer, minZ - CameraCaptureBorderBuffer);
        }

        private Vector3 CalculateCameraPosition(Rect boundingBox)
        {
            return new Vector3(boundingBox.center.x, float.IsNaN(_cameraDistance) ? 0 : _cameraDistance, boundingBox.center.y);
        }

        private float CalculateOrthographicSize(Rect boundingBox)
        {
            return Mathf.Max(Mathf.Abs(boundingBox.width), Mathf.Abs(boundingBox.height)) / 2f;
        }

        private int LayerToInt()
        {
            var layerNumber = 0;
            var layer = layerMask.value;
            while (layer > 0)
            {
                layer >>= 1;
                layerNumber++;
            }

            return layerNumber - 1;
        }
    }
}
