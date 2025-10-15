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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meta.XR.Util;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Provides debugging tools for visualizing and interacting with the scene data.
    /// </summary>
    [Feature(Feature.Scene)]
    public class SceneDebugger : MonoBehaviour
    {
        [Tooltip("Material used for visual helpers in debugging")]
        public Material visualHelperMaterial;

        [Tooltip("Visualize anchors")] public bool ShowDebugAnchors;

        [Tooltip("On start, place the canvas in front of the user")]
        public bool MoveCanvasInFrontOfCamera = true;

        [Tooltip("When false, use the interaction system already present in the scene")]
        public bool SetupInteractions;

        [Tooltip(" Text field for displaying logs")]
        public TextMeshProUGUI logs;

        [Tooltip("Dropdown to select what surface types to debug")]
        public TMP_Dropdown surfaceTypeDropdown;

        [Tooltip("Dropdown to select whether to export the global mesh with the scene JSON")]
        public TMP_Dropdown exportGlobalMeshJSONDropdown;

        [Tooltip("Dropdown to select what positioning methods to debug")]
        public TMP_Dropdown positioningMethodDropdown;

        [Tooltip("Text field for displaying room details")]
        public TextMeshProUGUI RoomDetails;

        [Tooltip("List of navigable tabs representing sub menus accessible from the top of the debug menu")]
        public List<Image> Tabs = new();

        [Tooltip("List of canvas groups for different menus")]
        public List<CanvasGroup> Menus = new();

        [Tooltip("Helper for ray interactions")]
        public OVRRayHelper RayHelper;

        [Tooltip("Input module for handling VR input")]
        public OVRInputModule InputModule;

        [Tooltip("Raycaster for handling ray interactions")]
        public OVRRaycaster Raycaster;

        [Tooltip("Gaze pointer for VR interactions")]
        public OVRGazePointer GazePointer;

        private readonly Color _foregroundColor = new(0.2039f, 0.2549f, 0.2941f, 1f);
        private readonly Color _backgroundColor = new(0.11176f, 0.1568f, 0.1843f, 1f);
        private readonly int _srcBlend = Shader.PropertyToID("_SrcBlend");
        private readonly int _dstBlend = Shader.PropertyToID("_DstBlend");
        private readonly int _zWrite = Shader.PropertyToID("_ZWrite");
        private readonly int _cull = Shader.PropertyToID("_Cull");
        private readonly int _color = Shader.PropertyToID("_Color");
        private readonly List<GameObject> _debugAnchors = new();
        private OVRCameraRig _cameraRig;

        // For visual debugging of the room
        private GameObject _debugCube;
        private GameObject _debugSphere;
        private GameObject _debugNormal;
        private GameObject _navMeshViz;
        private GameObject _debugAnchor;
        private bool _previousShowDebugAnchors;
        private Mesh _debugCheckerMesh;
        private EffectMesh _globalMeshEffectMesh;
        private MRUKAnchor _previousShownDebugAnchor;
        private MRUKAnchor _globalMeshAnchor;
        private NavMeshTriangulation _navMeshTriangulation;
        private Action _debugAction;
        private Canvas _canvas;
        private const float _spawnDistanceFromCamera = 0.75f;
        private SpaceMapGPU _spaceMapGPU;


        private void Awake()
        {
            _cameraRig = FindObjectOfType<OVRCameraRig>();
            _canvas = GetComponentInChildren<Canvas>();
            if (SetupInteractions)
            {
                SetupInteractionDependencies();
            }
        }

        private void Start()
        {
            MRUK.Instance?.RegisterSceneLoadedCallback(OnSceneLoaded);
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneDebugger).Send();
#endif
            _globalMeshEffectMesh = GetGlobalMeshEffectMesh();
            _spaceMapGPU = GetSpaceMapGPU();
            if (MoveCanvasInFrontOfCamera)
            {
                StartCoroutine(SnapCanvasInFrontOfCamera());
            }

            if (_spaceMapGPU == null) //no space map present in level
            {
                var toolSpaceMapGPU = Menus[0].transform.FindChildRecursive("SpaceMapGPU");
                toolSpaceMapGPU.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            _debugAction?.Invoke();

            // Toggle the anchors debug visuals
            if (ShowDebugAnchors != _previousShowDebugAnchors)
            {
                if (ShowDebugAnchors)
                {
                    foreach (var room in MRUK.Instance.Rooms)
                    {
                        foreach (var anchor in room.Anchors)
                        {
                            GameObject anchorVisual = GenerateDebugAnchor(anchor);
                            _debugAnchors.Add(anchorVisual);
                        }
                    }
                }
                else
                {
                    foreach (var anchorVisual in _debugAnchors)
                    {
                        Destroy(anchorVisual.gameObject);
                    }

                    _previousShowDebugAnchors = ShowDebugAnchors;
                }
            }

            if (OVRInput.GetDown(OVRInput.RawButton.Start))
            {
                ToggleMenu(!_canvas.gameObject.activeInHierarchy);
            }

            Billboard();
        }

        private void OnDisable()
        {
            _debugAction = null;
        }

        private void OnSceneLoaded()
        {
            CreateDebugPrimitives();
        }

        private void SetupInteractionDependencies()
        {
            if (!_cameraRig)
            {
                return;
            }

            GazePointer.rayTransform = _cameraRig.centerEyeAnchor;
            InputModule.rayTransform = _cameraRig.rightControllerAnchor;
            Raycaster.pointer = _cameraRig.rightControllerAnchor.gameObject;
            if (_cameraRig.GetComponentsInChildren<OVRRayHelper>(false).Length > 0)
            {
                return;
            }

            var rightControllerHelper =
                _cameraRig.rightControllerAnchor.GetComponentInChildren<OVRControllerHelper>();
            if (rightControllerHelper)
            {
                rightControllerHelper.RayHelper =
                    Instantiate(RayHelper, Vector3.zero, Quaternion.identity, rightControllerHelper.transform);
                rightControllerHelper.RayHelper.gameObject.SetActive(true);
            }

            var leftControllerHelper =
                _cameraRig.leftControllerAnchor.GetComponentInChildren<OVRControllerHelper>();
            if (leftControllerHelper)
            {
                leftControllerHelper.RayHelper =
                    Instantiate(RayHelper, Vector3.zero, Quaternion.identity, leftControllerHelper.transform);
                leftControllerHelper.RayHelper.gameObject.SetActive(true);
            }

            var hands = _cameraRig.GetComponentsInChildren<OVRHand>();
            foreach (var hand in hands)
            {
                hand.RayHelper =
                    Instantiate(RayHelper, Vector3.zero, Quaternion.identity, _cameraRig.trackingSpace);
                hand.RayHelper.gameObject.SetActive(true);
            }
        }

        private Ray GetControllerRay()
        {
            Vector3 rayOrigin;
            Vector3 rayDirection;
            if (OVRInput.activeControllerType == OVRInput.Controller.Touch
                || OVRInput.activeControllerType == OVRInput.Controller.RTouch)
            {
                rayOrigin = _cameraRig.rightHandOnControllerAnchor.position;
                rayDirection = _cameraRig.rightHandOnControllerAnchor.forward;
            }
            else if (OVRInput.activeControllerType == OVRInput.Controller.LTouch)
            {
                rayOrigin = _cameraRig.leftHandOnControllerAnchor.position;
                rayDirection = _cameraRig.leftHandOnControllerAnchor.forward;
            }
            else // hands
            {
                var rightHand = _cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>();
                // can be null if running in Editor with Meta Linq app and the headset is put off
                if (rightHand != null)
                {
                    rayOrigin = rightHand.PointerPose.position;
                    rayDirection = rightHand.PointerPose.forward;
                }
                else
                {
                    rayOrigin = _cameraRig.centerEyeAnchor.position;
                    rayDirection = _cameraRig.centerEyeAnchor.forward;
                }
            }

            return new Ray(rayOrigin, rayDirection);
        }

        /// <summary>
        /// Shows information about the rooms loaded.
        /// </summary>
        /// <param name="isOn">If set to true, room details will be shown.</param>
        public void ShowRoomDetailsDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction += ShowRoomDetails;
                }
                else
                {
                    _debugAction -= ShowRoomDetails;
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ShowRoomDetailsDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Highlights the room's key wall, defined as the longest wall in the room which has no other room points behind it
        /// </summary>
        /// <param name="isOn">If set to true, the key wall will be highlighted.</param>
        public void GetKeyWallDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    var wallScale = Vector2.zero;
                    var keyWall = MRUK.Instance?.GetCurrentRoom()?.GetKeyWall(out wallScale);
                    if (keyWall != null)
                    {
                        var anchorCenter = keyWall.GetAnchorCenter();
                        if (_debugCube != null)
                        {
                            _debugCube.transform.localScale = new Vector3(wallScale.x, wallScale.y, 0.05f);
                            _debugCube.transform.localPosition = anchorCenter;

                            _debugCube.transform.localRotation = keyWall.transform.localRotation;
                        }
                    }

                    SetLogsText("\n[{0}]\nSize: {1}",
                        nameof(GetKeyWallDebugger),
                        wallScale
                    );
                }

                if (_debugCube != null)
                {
                    _debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetKeyWallDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        ///  Highlights the anchor with the largest available surface area.
        /// </summary>
        /// <param name="isOn">If set to true, the largest surface will be highlighted.</param>
        public void GetLargestSurfaceDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    var surfaceType = MRUKAnchor.SceneLabels.TABLE; // using table as the default value
                    if (surfaceTypeDropdown)
                    {
                        surfaceType = Utilities.StringLabelToEnum(surfaceTypeDropdown.options[surfaceTypeDropdown.value].text.ToUpper());
                    }

                    var largestSurface = MRUK.Instance?.GetCurrentRoom()?.FindLargestSurface(surfaceType);
                    if (largestSurface != null)
                    {
                        if (_debugCube != null)
                        {
                            Vector3 anchorSize = largestSurface.VolumeBounds.HasValue
                                ? largestSurface.VolumeBounds.Value.size
                                : new Vector3(0, 0, 0.01f);
                            if (largestSurface.PlaneRect.HasValue)
                            {
                                anchorSize = new Vector3(largestSurface.PlaneRect.Value.x,
                                    largestSurface.PlaneRect.Value.y, 0.01f);
                            }

                            _debugCube.transform.localScale = anchorSize;
                            _debugCube.transform.localPosition = largestSurface.transform.position;
                            _debugCube.transform.localRotation = largestSurface.transform.rotation;
                        }

                        SetLogsText("\n[{0}]\nAnchor: {1}\nType: {2}",
                            nameof(GetLargestSurfaceDebugger),
                            largestSurface.name,
                            largestSurface.Label
                        );
                    }
                    else
                    {
                        SetLogsText("\n[{0}]\n No surface of type {1} found.",
                            nameof(GetLargestSurfaceDebugger),
                            surfaceType
                        );
                    }
                }
                else
                {
                    _debugAction = null;
                }

                if (_debugCube != null)
                {
                    _debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetLargestSurfaceDebugger),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Highlights the best-suggested seat, for something like remote caller placement.
        /// </summary>
        /// <param name="isOn">If set to true, the closest seat pose will be highlighted.</param>
        public void GetClosestSeatPoseDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        MRUKAnchor seat = null;
                        var seatPose = new Pose();
                        var ray = GetControllerRay();
                        MRUK.Instance?.GetCurrentRoom()?.TryGetClosestSeatPose(ray, out seatPose, out seat);
                        if (seat)
                        {
                            if (_debugCube != null)
                            {
                                _debugCube.transform.localRotation = seat.transform.localRotation;
                                _debugCube.transform.position = seatPose.position;
                                _debugCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            }

                            SetLogsText("\n[{0}]\nSeat: {1}\nPosition: {2}\nDistance: {3}",
                                nameof(GetClosestSeatPoseDebugger),
                                seat.name,
                                seatPose.position,
                                Vector3.Distance(seatPose.position, ray.origin).ToString("0.##")
                            );
                        }
                        else
                        {
                            SetLogsText("\n[{0}]\n No seat found in the scene.",
                                nameof(GetClosestSeatPoseDebugger)
                            );
                        }
                    };
                }
                else
                {
                    _debugAction = null;
                }

                if (_debugCube != null)
                {
                    _debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetClosestSeatPoseDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Highlights the closest position on a SceneAPI surface.
        /// </summary>
        /// <param name="isOn">If set to true, the closest surface position will be highlighted.</param>
        public void GetClosestSurfacePositionDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        var origin = GetControllerRay().origin;
                        var surfacePosition = Vector3.zero;
                        var normal = Vector3.up;
                        MRUKAnchor closestAnchor = null;
                        MRUK.Instance?.GetCurrentRoom()
                            ?.TryGetClosestSurfacePosition(origin, out surfacePosition, out closestAnchor, out normal);
                        ShowHitNormal(surfacePosition, normal);

                        if (closestAnchor != null)
                        {
                            SetLogsText("\n[{0}]\nAnchor: {1}\nSurface Position: {2}\nDistance: {3}",
                                nameof(GetClosestSurfacePositionDebugger),
                                closestAnchor.name,
                                surfacePosition,
                                Vector3.Distance(origin, surfacePosition).ToString("0.##")
                            );
                        }
                    };
                }
                else
                {
                    _debugAction = null;
                }

                if (_debugNormal != null)
                {
                    _debugNormal.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetClosestSurfacePositionDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Highlights the best suggested transform to place a widget on a surface.
        /// </summary>
        /// <param name="isOn">If set to true, the best pose from raycast will be highlighted.</param>
        public void GetBestPoseFromRaycastDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        var ray = GetControllerRay();
                        MRUKAnchor sceneAnchor = null;
                        var positioningMethod = MRUK.PositioningMethod.DEFAULT;
                        if (positioningMethodDropdown)
                        {
                            positioningMethod = (MRUK.PositioningMethod)positioningMethodDropdown.value;
                        }

                        var bestPose = MRUK.Instance?.GetCurrentRoom()?.GetBestPoseFromRaycast(ray, Mathf.Infinity,
                            new LabelFilter(), out sceneAnchor, positioningMethod);
                        if (bestPose.HasValue && sceneAnchor && _debugCube)
                        {
                            _debugCube.transform.position = bestPose.Value.position;
                            _debugCube.transform.rotation = bestPose.Value.rotation;
                            _debugCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            SetLogsText("\n[{0}]\nAnchor: {1}\nPose Position: {2}\nPose Rotation: {3}",
                                nameof(GetBestPoseFromRaycastDebugger),
                                sceneAnchor.name,
                                bestPose.Value.position,
                                bestPose.Value.rotation
                            );
                        }
                    };
                }
                else
                {
                    _debugAction = null;
                }

                if (_debugCube != null)
                {
                    _debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetBestPoseFromRaycastDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Casts a ray cast forward from the right controller position and draws the normal of the first Scene API object hit.
        /// </summary>
        /// <param name="isOn">If set to true, the raycast hits with scene objects will be visualized.</param>
        public void RayCastDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        var ray = GetControllerRay();
                        var hit = new RaycastHit();
                        MRUKAnchor anchorHit = null;
                        MRUK.Instance?.GetCurrentRoom()?.Raycast(ray, Mathf.Infinity, out hit, out anchorHit);
                        ShowHitNormal(hit.point, hit.normal);
                        if (anchorHit != null)
                        {
                            SetLogsText("\n[{0}]\nAnchor: {1}\nHit point: {2}\nHit normal: {3}\n",
                                nameof(RayCastDebugger),
                                anchorHit.name,
                                hit.point,
                                hit.normal
                            );
                        }
                    };
                }
                else
                {
                    _debugAction = null;
                }

                if (_debugNormal != null)
                {
                    _debugNormal.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(RayCastDebugger),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Moves the debug sphere to the controller position and colors it in green if its position is in the room,
        /// red otherwise.
        /// </summary>
        /// <param name="isOn">If set to true, the position of the debug sphere will be checked.</param>
        public void IsPositionInRoomDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        var ray = GetControllerRay();
                        if (_debugSphere != null)
                        {
                            var isInRoom = MRUK.Instance?.GetCurrentRoom()
                                ?.IsPositionInRoom(_debugSphere.transform.position);
                            _debugSphere.transform.position = ray.GetPoint(0.2f); // add some offset
                            _debugSphere.GetComponent<Renderer>().material.color =
                                isInRoom.HasValue && isInRoom.Value ? Color.green : Color.red;
                            SetLogsText("\n[{0}]\nPosition: {1}\nIs inside the Room: {2}\n",
                                nameof(IsPositionInRoomDebugger),
                                _debugSphere.transform.position,
                                isInRoom
                            );
                        }
                    };
                }

                if (_debugSphere != null)
                {
                    _debugSphere.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(IsPositionInRoomDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Shows the debug anchor visualization mode for the anchor being pointed at.
        /// </summary>
        /// <param name="isOn">If set to true, debug anchors will be visualized.</param>
        public void ShowDebugAnchorsDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        var ray = GetControllerRay();
                        var hit = new RaycastHit();
                        MRUKAnchor anchorHit = null;
                        MRUK.Instance?.GetCurrentRoom()?.Raycast(ray, Mathf.Infinity, out hit, out anchorHit);
                        if (_previousShownDebugAnchor != anchorHit && anchorHit != null)
                        {
                            Destroy(_debugAnchor);
                            _debugAnchor = GenerateDebugAnchor(anchorHit);
                            _previousShownDebugAnchor = anchorHit;
                        }

                        ShowHitNormal(hit.point, hit.normal);
                        SetLogsText("\n[{0}]\nHit point: {1}\nHit normal: {2}\n",
                            nameof(ShowDebugAnchorsDebugger),
                            hit.point,
                            hit.normal
                        );
                    };
                }
                else
                {
                    _debugAction = null;
                    Destroy(_debugAnchor);
                    _debugAnchor = null;
                }

                if (_debugNormal != null)
                {
                    _debugNormal.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ShowDebugAnchorsDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Displays the global mesh anchor if one is found in the scene.
        /// </summary>
        /// <param name="isOn">If set to true, the global mesh will be displayed.</param>
        public void DisplayGlobalMesh(bool isOn)
        {
            try
            {
                var filter = new LabelFilter(MRUKAnchor.SceneLabels.GLOBAL_MESH);
                if (MRUK.Instance && MRUK.Instance.GetCurrentRoom() && !_globalMeshAnchor)
                {
                    _globalMeshAnchor = MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor;
                }

                if (!_globalMeshAnchor)
                {
                    SetLogsText("\n[{0}]\nNo global mesh anchor found in the scene.\n",
                        nameof(DisplayGlobalMesh)
                    );
                    return;
                }

                if (isOn)
                {
                    if (!_globalMeshEffectMesh)
                    {
                        _globalMeshEffectMesh =
                            new GameObject("_globalMeshViz", typeof(EffectMesh)).GetComponent<EffectMesh>();
                        _globalMeshEffectMesh.Labels = MRUKAnchor.SceneLabels.GLOBAL_MESH;
                        if (visualHelperMaterial)
                        {
                            _globalMeshEffectMesh.MeshMaterial = visualHelperMaterial;
                        }

                        _globalMeshEffectMesh.CreateMesh();
                    }
                    else
                    {
                        _globalMeshEffectMesh.ToggleEffectMeshVisibility(true, filter, visualHelperMaterial);
                    }
                }
                else
                {
                    if (!_globalMeshEffectMesh)
                    {
                        return;
                    }

                    _globalMeshEffectMesh.ToggleEffectMeshVisibility(false, filter, _globalMeshEffectMesh.MeshMaterial);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(DisplayGlobalMesh),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Toggles the global mesh anchor's collision.
        /// </summary>
        /// <param name="isOn">If set to true, collisions for the global mesh anchor will be enabled </param>
        public void ToggleGlobalMeshCollisions(bool isOn)
        {
            try
            {
                var filter = new LabelFilter(MRUKAnchor.SceneLabels.GLOBAL_MESH);
                if (MRUK.Instance && MRUK.Instance.GetCurrentRoom() && !_globalMeshAnchor)
                {
                    _globalMeshAnchor = MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor;
                }

                if (!_globalMeshAnchor)
                {
                    SetLogsText("\n[{0}]\nNo global mesh anchor found in the scene.\n",
                        nameof(ToggleGlobalMeshCollisions)
                    );
                    return;
                }

                if (isOn)
                {
                    if (!_globalMeshEffectMesh)
                    {
                        _globalMeshEffectMesh =
                            new GameObject("_globalMeshViz", typeof(EffectMesh)).GetComponent<EffectMesh>();
                        _globalMeshEffectMesh.Labels = MRUKAnchor.SceneLabels.GLOBAL_MESH;
                        if (visualHelperMaterial)
                        {
                            _globalMeshEffectMesh.MeshMaterial = visualHelperMaterial;
                        }

                        _globalMeshEffectMesh.HideMesh = true;
                        _globalMeshEffectMesh.CreateMesh();
                    }

                    _globalMeshEffectMesh.AddColliders();
                }
                else
                {
                    if (!_globalMeshEffectMesh)
                    {
                        return;
                    }

                    _globalMeshEffectMesh.DestroyColliders(filter);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ToggleGlobalMeshCollisions),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Exports the current scene data to a JSON file if the specified condition is met.
        /// </summary>
        /// <param name="isOn">If set to true, the scene data will be exported to JSON.</param>
        public void ExportJSON(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    bool exportGlobalMesh = true;
                    if (exportGlobalMeshJSONDropdown)
                    {
                        exportGlobalMesh = exportGlobalMeshJSONDropdown.options[exportGlobalMeshJSONDropdown.value].text.ToLower() == "true";
                    }

                    var scene = SerializationHelpers.Serialize(SerializationHelpers.CoordinateSystem.Unity, exportGlobalMesh);
                    var filename = $"MRUK_Export_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json";
                    var path = Path.Combine(Application.persistentDataPath, filename);
                    File.WriteAllText(path, scene);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ExportJSON),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Debugging method to color each segment with a unique color.
        /// </summary>
        /// <param name="destructibleMeshComponent">The <see cref="DestructibleMeshComponent"/> to be debugged.</param>
        public static void DebugDestructibleMeshComponent(DestructibleMeshComponent destructibleMeshComponent)
        {
            if (destructibleMeshComponent == null)
            {
                throw new Exception("Can not debug a null DestructibleMeshComponent.");
            }

            var segments = new List<GameObject>();

            destructibleMeshComponent.GetDestructibleMeshSegments(segments);
            foreach (var segment in segments)
            {
                // Create a new material with a random color for each segment
                var newMaterial = new Material(Shader.Find("Standard"))
                {
                    color = UnityEngine.Random.ColorHSV()
                };
                if (segment.TryGetComponent<MeshRenderer>(out var renderer))
                {
                    renderer.material = newMaterial;
                }
            }
        }

        /// <summary>
        /// Displays a texture in the right info panel about your spacemap
        /// </summary>
        /// <param name="isOn">No action needed as the SpaceMap does not need additional logic </param>
        public void DisplaySpaceMap(bool isOn)
        {
            //we do not need to call additional logic, this is just for completeness
        }

        /// <summary>
        /// Displays the nav mesh, if present.
        /// </summary>
        /// <param name="isOn">If set to true, the navigation mesh will be displayed, if present</param>
        public void DisplayNavMesh(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    _debugAction = () =>
                    {
                        var triangulation = NavMesh.CalculateTriangulation();
                        if (triangulation.areas.Length == 0 && _navMeshTriangulation.Equals(triangulation))
                        {
                            return;
                        }

                        MeshRenderer navMeshRenderer;
                        MeshFilter navMeshFilter;
                        if (!_navMeshViz)
                        {
                            _navMeshViz = new GameObject("_navMeshViz");
                            navMeshRenderer = _navMeshViz.AddComponent<MeshRenderer>();
                            navMeshFilter = _navMeshViz.AddComponent<MeshFilter>();
                        }
                        else
                        {
                            navMeshRenderer = _navMeshViz.GetComponent<MeshRenderer>();
                            navMeshFilter = _navMeshViz.GetComponent<MeshFilter>();
                            DestroyImmediate(navMeshFilter.mesh);
                            navMeshFilter.mesh = null;
                        }

                        var navMesh = new Mesh()
                        {
                            indexFormat = triangulation.indices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
                        };

                        navMesh.SetVertices(triangulation.vertices);
                        navMesh.SetIndices(triangulation.indices, MeshTopology.Triangles, 0);
                        navMeshRenderer.material = visualHelperMaterial;
                        navMeshRenderer.material.color = Color.cyan;
                        navMeshFilter.mesh = navMesh;
                        _navMeshTriangulation = triangulation;
                    };
                }
                else
                {
                    DestroyImmediate(_navMeshViz);
                    _debugAction = null;
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(DisplayNavMesh),
                    e.Message,
                    e.StackTrace
                );
            }
        }


        private EffectMesh GetGlobalMeshEffectMesh()
        {
            var effectMeshes = FindObjectsByType<EffectMesh>(FindObjectsSortMode.None);
            foreach (var effectMesh in effectMeshes)
            {
                if ((effectMesh.Labels & MRUKAnchor.SceneLabels.GLOBAL_MESH) != 0)
                {
                    return effectMesh;
                }
            }

            return null;
        }

        private SpaceMapGPU GetSpaceMapGPU()
        {
            var spaceMaps = FindObjectsByType<SpaceMapGPU>(FindObjectsSortMode.None);
            return spaceMaps.Length > 0 ? spaceMaps[0] : null;
        }

        private void ShowRoomDetails()
        {
            var currentRoomName = MRUK.Instance?.GetCurrentRoom().name ?? "N/A";
            var numRooms = MRUK.Instance?.Rooms.Count ?? 0;
            RoomDetails.text = string.Format("\n[{0}]\nNumber of rooms: {1}\nCurrent room: {2}",
                nameof(ShowRoomDetailsDebugger), numRooms, currentRoomName);
        }


        /// <summary>
        ///     Creates an object to help visually debugging a specific anchor.
        /// </summary>
        private GameObject GenerateDebugAnchor(MRUKAnchor anchor)
        {
            var debugPlanePrefab = CreateDebugPrefabSource(true);
            var debugVolumePrefab = CreateDebugPrefabSource(false);

            Vector3 anchorScale;
            if (anchor.VolumeBounds.HasValue)
            {
                // Volumes
                _debugAnchor = CloneObject(debugVolumePrefab, anchor.transform);
                anchorScale = anchor.VolumeBounds.Value.size;
            }
            else
            {
                // Quads
                _debugAnchor = CloneObject(debugPlanePrefab, anchor.transform);
                anchorScale = Vector3.zero;
                if (anchor.PlaneRect != null)
                {
                    var quadScale = anchor.PlaneRect.Value.size;
                    anchorScale = new Vector3(quadScale.x, quadScale.y, 1.0f);
                }
            }

            ScaleChildren(_debugAnchor.transform, anchorScale);
            _debugAnchor.transform.parent = null;
            _debugAnchor.SetActive(true);

            Destroy(debugPlanePrefab);
            Destroy(debugVolumePrefab);

            return _debugAnchor;
        }

        private GameObject CloneObject(GameObject prefabObj, Transform refObject)
        {
            var newObj = Instantiate(prefabObj);
            newObj.name = "Debug_" + refObject.name;
            newObj.transform.position = refObject.position;
            newObj.transform.rotation = refObject.rotation;

            return newObj;
        }

        private void ScaleChildren(Transform parent, Vector3 localScale)
        {
            foreach (Transform child in parent)
            {
                child.localScale = localScale;
            }
        }

        /// <summary>
        ///     By creating our reference PLANE and VOLUME prefabs in code, we can avoid linking them via Inspector.
        /// </summary>
        private GameObject CreateDebugPrefabSource(bool isPlane)
        {
            var prefabName = isPlane ? "PlanePrefab" : "VolumePrefab";
            var prefabObject = new GameObject(prefabName);

            var meshParent = new GameObject("MeshParent");
            meshParent.transform.SetParent(prefabObject.transform);
            meshParent.SetActive(false);

            var prefabMesh = isPlane
                ? GameObject.CreatePrimitive(PrimitiveType.Quad)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabMesh.name = "Mesh";
            prefabMesh.transform.SetParent(meshParent.transform);
            if (isPlane)
            // Unity quad's normal doesn't align with transform's Z-forward
            {
                prefabMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            // Anchor cubes don't have a center pivot
            {
                prefabMesh.transform.localPosition = new Vector3(0, 0, -0.5f);
            }

            SetMaterialProperties(prefabMesh.GetComponent<MeshRenderer>());
            DestroyImmediate(prefabMesh.GetComponent<Collider>());

            var prefabPivot = new GameObject("Pivot");
            prefabPivot.transform.SetParent(prefabObject.transform);

            CreateGridPattern(prefabPivot.transform, Vector3.zero, Quaternion.identity);
            if (!isPlane)
            {
                CreateGridPattern(prefabPivot.transform, new Vector3(0, 0, -1), Quaternion.Euler(180, 0, 0));
                CreateGridPattern(prefabPivot.transform, new Vector3(0, -0.5f, -0.5f), Quaternion.Euler(90, 0, 0));
                CreateGridPattern(prefabPivot.transform, new Vector3(0, 0.5f, -0.5f), Quaternion.Euler(-90, 0, 0));
                CreateGridPattern(prefabPivot.transform, new Vector3(-0.5f, 0, -0.5f), Quaternion.Euler(0, -90, 90));
                CreateGridPattern(prefabPivot.transform, new Vector3(0.5f, 0, -0.5f), Quaternion.Euler(180, -90, 90));
            }

            return prefabObject;
        }

        private void SetMaterialProperties(MeshRenderer refMesh)
        {
            refMesh.material.SetColor(_color, new Color(0.5f, 0.9f, 1.0f, 0.75f));
            refMesh.material.SetOverrideTag("RenderType", "Transparent");
            refMesh.material.SetInt(_srcBlend, (int)BlendMode.SrcAlpha);
            refMesh.material.SetInt(_dstBlend, (int)BlendMode.One);
            refMesh.material.SetInt(_zWrite, 0);
            refMesh.material.SetInt(_cull, 2); // "Back"
            refMesh.material.DisableKeyword("_ALPHATEST_ON");
            refMesh.material.EnableKeyword("_ALPHABLEND_ON");
            refMesh.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            refMesh.material.renderQueue = (int)RenderQueue.Transparent;
        }

        // The grid pattern on each anchor is actually a mesh, to avoid a texture
        private void CreateGridPattern(Transform parentTransform, Vector3 localOffset, Quaternion localRotation)
        {
            var newGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            newGameObject.name = "Checker";
            newGameObject.transform.SetParent(parentTransform, false);
            newGameObject.transform.localPosition = localOffset;
            newGameObject.transform.localRotation = localRotation;
            DestroyImmediate(newGameObject.GetComponent<Collider>());

            // offset the debug grid the smallest amount to avoid z-fighting
            const float NORMAL_OFFSET = 0.001f;

            // the mesh is used on every prefab, but only needs to be created once
            var vertCounter = 0;
            if (_debugCheckerMesh == null)
            {
                _debugCheckerMesh = new Mesh();
                const int gridWidth = 10;
                var cellWidth = 1.0f / gridWidth;
                var xPos = -0.5f;
                var yPos = -0.5f;

                var totalTiles = gridWidth * gridWidth / 2;
                var totalVertices = totalTiles * 4;
                var totalIndices = totalTiles * 6;

                var MeshVertices = new Vector3[totalVertices];
                var MeshUVs = new Vector2[totalVertices];
                var MeshColors = new Color32[totalVertices];
                var MeshNormals = new Vector3[totalVertices];
                var MeshTangents = new Vector4[totalVertices];
                var MeshTriangles = new int[totalIndices];

                var indexCounter = 0;
                var quadCounter = 0;

                for (var x = 0; x < gridWidth; x++)
                {
                    var createQuad = x % 2 == 0;
                    for (var y = 0; y < gridWidth; y++)
                    {
                        if (createQuad)
                        {
                            for (var V = 0; V < 4; V++)
                            {
                                var localVertPos = new Vector3(xPos, yPos + y * cellWidth, NORMAL_OFFSET);
                                switch (V)
                                {
                                    case 1:
                                        localVertPos += new Vector3(0, cellWidth, 0);
                                        break;
                                    case 2:
                                        localVertPos += new Vector3(cellWidth, cellWidth, 0);
                                        break;
                                    case 3:
                                        localVertPos += new Vector3(cellWidth, 0, 0);
                                        break;
                                }

                                MeshVertices[vertCounter] = localVertPos;
                                MeshUVs[vertCounter] = Vector2.zero;
                                MeshColors[vertCounter] = Color.black;
                                MeshNormals[vertCounter] = Vector3.forward;
                                MeshTangents[vertCounter] = Vector3.right;

                                vertCounter++;
                            }

                            var baseCount = quadCounter * 4;
                            MeshTriangles[indexCounter++] = baseCount;
                            MeshTriangles[indexCounter++] = baseCount + 2;
                            MeshTriangles[indexCounter++] = baseCount + 1;
                            MeshTriangles[indexCounter++] = baseCount;
                            MeshTriangles[indexCounter++] = baseCount + 3;
                            MeshTriangles[indexCounter++] = baseCount + 2;

                            quadCounter++;
                        }

                        createQuad = !createQuad;
                    }

                    xPos += cellWidth;
                }

                _debugCheckerMesh.Clear();
                _debugCheckerMesh.name = "CheckerMesh";
                _debugCheckerMesh.vertices = MeshVertices;
                _debugCheckerMesh.uv = MeshUVs;
                _debugCheckerMesh.colors32 = MeshColors;
                _debugCheckerMesh.triangles = MeshTriangles;
                _debugCheckerMesh.normals = MeshNormals;
                _debugCheckerMesh.tangents = MeshTangents;
                _debugCheckerMesh.RecalculateNormals();
                _debugCheckerMesh.RecalculateTangents();
            }

            newGameObject.GetComponent<MeshFilter>().mesh = _debugCheckerMesh;

            var material = newGameObject.GetComponent<MeshRenderer>().material;
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt(_srcBlend, (int)BlendMode.SrcAlpha);
            material.SetInt(_dstBlend, (int)BlendMode.One);
            material.SetInt(_zWrite, 0);
            material.SetInt(_cull, 2); // "Back"
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>
        ///     Creates the debug primitives for visual debugging purposes and to avoid inspector linking.
        /// </summary>
        private void CreateDebugPrimitives()
        {
            _debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _debugCube.name = "SceneDebugger_Cube";
            _debugCube.GetComponent<Renderer>().material.color = Color.green;
            _debugCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            _debugCube.GetComponent<Collider>().enabled = false;
            _debugCube.SetActive(false);

            _debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _debugSphere.name = "SceneDebugger_Sphere";
            _debugSphere.GetComponent<Renderer>().material.color = Color.green;
            _debugSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            _debugSphere.GetComponent<Collider>().enabled = false;
            _debugSphere.SetActive(false);

            _debugNormal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _debugNormal.name = "SceneDebugger_Normal";
            _debugNormal.GetComponent<Renderer>().material.color = Color.green;
            _debugNormal.transform.localScale = new Vector3(0.02f, 0.1f, 0.02f);
            _debugNormal.GetComponent<Collider>().enabled = false;
            _debugNormal.SetActive(false);
        }

        /// <summary>
        ///     Convenience method to show the normal of a hit collision.
        /// </summary>
        /// <param name="position">Position of the location to render</param>
        /// <param name="normal">Normal of the hit</param>
        private void ShowHitNormal(Vector3 position, Vector3 normal)
        {
            if (_debugNormal != null && position != Vector3.zero && normal != Vector3.zero)
            {
                _debugNormal.SetActive(true);
                _debugNormal.transform.rotation = Quaternion.FromToRotation(-Vector3.up, normal);
                _debugNormal.transform.position =
                    position + -_debugNormal.transform.up * _debugNormal.transform.localScale.y;
            }
            else
            {
                _debugNormal.SetActive(false);
            }
        }

        private void SetLogsText(string logsText, params object[] args)
        {
            if (logs)
            {
                logs.text = string.Format(logsText, args);
            }
        }

        /// <summary>
        /// Activates the selected tab by changing its color and sets all other tabs to the background color.
        /// </summary>
        /// <param name="selectedTab">The tab image to activate.</param>
        public void ActivateTab(Image selectedTab)
        {
            foreach (var tab in Tabs)
            {
                tab.color = _backgroundColor;
            }

            selectedTab.color = _foregroundColor;
        }

        /// <summary>
        /// Activates the specified menu by enabling its canvas group and disables all other menus.
        /// </summary>
        /// <param name="menuToActivate">The canvas group of the menu to activate.</param>
        public void ActivateMenu(CanvasGroup menuToActivate)
        {
            foreach (var menu in Menus)
            {
                ToggleCanvasGroup(menu, false);
            }

            ToggleCanvasGroup(menuToActivate, true);
        }


        private void ToggleCanvasGroup(CanvasGroup canvasGroup, bool shouldShow)
        {
            canvasGroup.interactable = shouldShow;
            canvasGroup.alpha = shouldShow ? 1f : 0f;
            canvasGroup.blocksRaycasts = shouldShow;
        }

        private void Billboard()
        {
            if (!_canvas)
            {
                return;
            }

            var direction = _canvas.transform.position - _cameraRig.centerEyeAnchor.transform.position;
            var rotation = Quaternion.LookRotation(direction);
            _canvas.transform.rotation = rotation;
        }

        private void ToggleMenu(bool active)
        {
            if (!_canvas)
            {
                return;
            }

            _canvas.gameObject.SetActive(active);
            StartCoroutine(SnapCanvasInFrontOfCamera());
        }

        private IEnumerator SnapCanvasInFrontOfCamera()
        {
            yield return new WaitUntil(
                () => _cameraRig && _cameraRig.centerEyeAnchor.transform.position != Vector3.zero);
            transform.position = _cameraRig.centerEyeAnchor.transform.position +
                                 _cameraRig.centerEyeAnchor.transform.forward * _spawnDistanceFromCamera;
        }
    }
}
