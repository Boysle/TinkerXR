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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// The <c>DestructibleMeshComponent</c> handles the segmentation and manipulation of a mesh to create a segmented version of the original.
    /// Used mainly to build destructible environments in conjunction with a global mesh. For mor details on its usage see <see cref="DestructibleGlobalMeshSpawner"/>
    /// It uses asynchronous tasks to perform the computations for mesh segmentation based on specified parameters and reserved areas.
    /// </summary>
    public class DestructibleMeshComponent : MonoBehaviour
    {
        /// <summary>
        /// Event triggered when a destructible mesh is successfully created.
        /// </summary>
        public UnityEvent<DestructibleMeshComponent> OnDestructibleMeshCreated;

        /// <summary>
        /// Function delegate that processes the segmentation result. It can be used to modify the segmentation results before they are instantiated.
        /// </summary>
        public Func<MeshSegmentationResult, MeshSegmentationResult> OnSegmentationCompleted;

        [SerializeField] private Material _destructibleMeshMaterial;

        [SerializeField] private float _reservedTop = -1.0f;
        [SerializeField] private float _reservedBottom = -1.0f;
        private Task<MeshSegmentationResult> _segmentationTask;
        private readonly List<GameObject> _segments = new();


        /// <summary>
        /// Gets or sets the material used for the mesh. This material is applied to the mesh segments that are created during the segmentation process.
        /// </summary>
        public Material GlobalMeshMaterial
        {
            get => _destructibleMeshMaterial;
            set => _destructibleMeshMaterial = value;
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

        /// <summary>
        /// Gets the game object associated to the reserved segment defined as the portion of the mesh that should be indestructible.
        /// Can be null if no segment has been reserved.
        /// </summary>
        public GameObject ReservedSegment { get; private set; }

        /// <summary>
        /// Represents a single mesh segment with position data, indices for mesh topology, UVs for texturing, and tangents for normal mapping.
        /// </summary>
        public struct MeshSegment
        {
            public Vector3[] positions;
            public int[] indices;
            public Vector2[] uv;
            public Vector4[] tangents;
        }

        /// <summary>
        /// Contains the results of a mesh segmentation operation, including a list of mesh segments and a specially reserved segment.
        /// </summary>
        public struct MeshSegmentationResult
        {
            public List<MeshSegment> segments;
            public MeshSegment reservedSegment;
        }


        /// <summary>
        /// Initiates the mesh segmentation process asynchronously based on the provided mesh positions, indices, and segmentation points. It uses native functions to perform the segmentation and updates the mesh accordingly.
        /// </summary>
        /// <param name="meshPositions">Array of mesh vertex positions.</param>
        /// <param name="meshIndices">Array of mesh indices.</param>
        /// <param name="segmentationPoints">Array of points used for segmenting the mesh.</param>
        public unsafe void SegmentMesh(Vector3[] meshPositions, uint[] meshIndices, Vector3[] segmentationPoints)
        {
            Vector3 reservedMin = new Vector3(ReservedBottom, -1.0f, -1.0f);
            Vector3 reservedMax = new Vector3(ReservedTop, -1.0f, -1.0f);
            _segmentationTask = Task.Run(() =>
            {
                MRUKNativeFuncs.MrukResult result = MRUKNativeFuncs.ComputeMeshSegmentation(
                    meshPositions, (uint)meshPositions.Length,
                    meshIndices, (uint)meshIndices.Length,
                    segmentationPoints, (uint)segmentationPoints.Length,
                    reservedMin, reservedMax,
                    out var meshSegments, out var numSegments, out var reservedSegment);
                if (result == MRUKNativeFuncs.MrukResult.Success)
                {
                    var segmentationResult = ProcessSegments(meshSegments, numSegments, reservedSegment);
                    // Free the allocated memory
                    MRUKNativeFuncs.FreeMeshSegmentation(meshSegments, numSegments, ref reservedSegment);
                    return segmentationResult;
                }

                // Free the allocated memory even in case of a failure
                MRUKNativeFuncs.FreeMeshSegmentation(meshSegments, numSegments, ref reservedSegment);
                throw new Exception(
                    $"Failed to segment the mesh: {MRUKNativeFuncs.MrukResult.ErrorInvalidArgs}");
            });
            _segmentationTask.ContinueWith(OnSegmentationTaskCompleted,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Returns the count of destructible mesh segments currently managed by this component.
        /// </summary>
        /// <returns>The number of child GameObjects representing mesh segments.</returns>
        public int GetDestructibleMeshSegmentsCount()
        {
            return transform.childCount;
        }

        /// <summary>
        /// Populates a provided container with GameObjects representing the destructible mesh segments.
        /// </summary>
        /// <remarks>The container will be cleared before being populated.</remarks>
        /// <typeparam name="T">The type of the list to populate, must implement IList<GameObject>.</typeparam>
        /// <param name="segments">The list to populate with mesh segment GameObjects.</param>
        public void GetDestructibleMeshSegments<T>(T segments) where T : IList<GameObject>
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments),
                    "Cannot populate the managed container with the global mesh segments as it was never initialized.");
            }

            if (segments.IsReadOnly)
            {
                throw new NotSupportedException("The segments collection is read-only and cannot be modified.");
            }
            segments.Clear();
            foreach (Transform segment in transform)
            {
                segments.Add(segment.gameObject);
            }
        }

        /// <summary>
        /// Populates an array with GameObjects representing the destructible mesh segments.
        /// </summary>
        /// <remarks>The array should be provided with enough space to hold all segments.
        /// see <see cref="GetDestructibleMeshSegmentsCount"/>.</remarks>
        /// <param name="segments">The array to populate with mesh segment GameObjects.</param>
        public void GetDestructibleMeshSegments(GameObject[] segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments),
                    "Cannot populate the array with the global mesh segments as it was never initialized.");
            }

            var index = 0;
            foreach (Transform segment in transform)
            {
                if (index >= segments.Length)
                {
                    throw new ArgumentException("The provided array does not have enough space to hold all segments.",
                        nameof(segments));
                }

                segments[index++] = segment.gameObject;
            }
        }

        /// <summary>
        /// Cleans up created mesh segments when the component is destroyed or reset.
        /// </summary>
        /// <param name="segment">The game object associated to the segment to be destroyed.</param>
        public void DestroySegment(GameObject segment)
        {
            GetDestructibleMeshSegments(_segments);
            if (!_segments.Contains(segment))
            {
                Debug.LogError(
                    "The segment that has been requested to be destroyed does not belong to the destructible mesh anymore." +
                    "This could be due to the segment being already been destroyed or it had its parent changed.");
                return;
            }

            if (segment == ReservedSegment)
            {
                Debug.LogWarning(
                    "The segment that has been requested to be destroyed is the reserved segment and it should not be destroyed." +
                    "In case the deletion is intended destroy the ReservedSegment game object directly, together with its mesh and material.");
                return;
            }
            if (segment.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.mesh != null)
            {
                Destroy(meshFilter.sharedMesh); // meshes are not automatically destroyed when their GO is destroyed
            }

            if (segment.TryGetComponent<MeshRenderer>(out var meshRenderer) && meshRenderer.material != null)
            {
                Destroy(meshRenderer.material); // same for materials
            }

            Destroy(segment);
        }

        /// <summary>
        /// Handles the completion of the segmentation task, processing the results and invoking relevant events.
        /// </summary>
        /// <param name="task">The task containing the segmentation results.</param>
        private void OnSegmentationTaskCompleted(Task<MeshSegmentationResult> task)
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                try
                {
                    // Invoke the event if it's not null and use the manipulated segments, otherwise use the original task result directly
                    var result = OnSegmentationCompleted?.Invoke(task.Result) ?? task.Result;
                    CreateDestructibleMesh(result);
                    OnDestructibleMeshCreated?.Invoke(this);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing segmentation results: {e.Message}");
                }
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Segmentation task failed: " + task.Exception?.InnerException?.Message);
            }
            else if (task.IsCanceled)
            {
                Debug.LogWarning("Segmentation task was canceled.");
            }
        }

        /// <summary>
        /// Processes the mesh segments obtained from the segmentation task. Converts native mesh data into Unity-compatible mesh segments.
        /// </summary>
        /// <param name="segments">Pointer to the array of mesh segments.</param>
        /// <param name="numSegments">Number of segments.</param>
        /// <param name="reservedSegment">The reserved segment data.</param>
        /// <returns>A <see cref="MeshSegmentationResult"/> containing the processed mesh segments.</returns>
        private unsafe MeshSegmentationResult ProcessSegments(MRUKNativeFuncs.MrukMesh3f* segments, uint numSegments,
            MRUKNativeFuncs.MrukMesh3f reservedSegment)
        {
            var result = new MeshSegmentationResult
            {
                segments = new List<MeshSegment>(),
                reservedSegment = new MeshSegment
                {
                    positions = new Vector3[reservedSegment.numVertices],
                    indices = new int[reservedSegment.numIndices]
                }
            };
            // Process segments
            for (uint i = 0; i < numSegments; i++)
            {
                var segment = new MeshSegment
                {
                    positions = new Vector3[segments[i].numVertices],
                    indices = new int[segments[i].numIndices]
                };
                // Copy vertices
                var verticesPtr = (IntPtr)segments[i].vertices;
                for (var j = 0; j < segments[i].numVertices; j++)
                {
                    var x = Marshal.PtrToStructure<float>(verticesPtr);
                    verticesPtr = IntPtr.Add(verticesPtr, sizeof(float));
                    var y = Marshal.PtrToStructure<float>(verticesPtr);
                    verticesPtr = IntPtr.Add(verticesPtr, sizeof(float));
                    var z = Marshal.PtrToStructure<float>(verticesPtr);
                    verticesPtr = IntPtr.Add(verticesPtr, sizeof(float));
                    segment.positions[j] = new Vector3(x, y, z);
                }

                // Copy indices
                var indicesPtr = (IntPtr)segments[i].indices;
                for (var k = 0; k < segments[i].numIndices; k++)
                {
                    segment.indices[k] = Marshal.ReadInt32(indicesPtr);
                    indicesPtr = IntPtr.Add(indicesPtr, sizeof(int));
                }

                result.segments.Add(segment);
            }

            // Process reserved segment similarly
            var reservedVerticesPtr = (IntPtr)reservedSegment.vertices;
            for (var j = 0; j < reservedSegment.numVertices; j++)
            {
                var x = Marshal.PtrToStructure<float>(reservedVerticesPtr);
                reservedVerticesPtr = IntPtr.Add(reservedVerticesPtr, sizeof(float));
                var y = Marshal.PtrToStructure<float>(reservedVerticesPtr);
                reservedVerticesPtr = IntPtr.Add(reservedVerticesPtr, sizeof(float));
                var z = Marshal.PtrToStructure<float>(reservedVerticesPtr);
                reservedVerticesPtr = IntPtr.Add(reservedVerticesPtr, sizeof(float));
                result.reservedSegment.positions[j] = new Vector3(x, y, z);
            }

            var reservedIndicesPtr = (IntPtr)reservedSegment.indices;
            for (var k = 0; k < reservedSegment.numIndices; k++)
            {
                result.reservedSegment.indices[k] = Marshal.ReadInt32(reservedIndicesPtr);
                reservedIndicesPtr = IntPtr.Add(reservedIndicesPtr, sizeof(int));
            }

            return result;
        }

        private void CreateDestructibleMesh(MeshSegmentationResult result)
        {
            foreach (var segment in result.segments)
            {
                CreateMeshSegment(segment.positions, segment.indices, uv: segment.uv, tangents: segment.tangents);
            }

            if (result.reservedSegment.indices.Length > 0)
            {
                ReservedSegment = CreateMeshSegment(result.reservedSegment.positions, result.reservedSegment.indices,
                    uv: result.reservedSegment.uv, tangents: result.reservedSegment.tangents, isReserved: true);
            }
        }

        private GameObject CreateMeshSegment(Vector3[] positions, int[] indices, Vector2[] uv = null,
            Vector4[] tangents = null, bool isReserved = false)
        {
            if (positions.Length == 0 || indices.Length == 0)
            {
                return null; // the native code does not filter out empty segments as it's cheaper to discard them here
            }
            var meshObject = new GameObject(isReserved ? "ReservedMeshSegment" : "DestructibleMeshSegment");
            meshObject.transform.SetParent(transform, false);
            var meshFilter = meshObject.AddComponent<MeshFilter>();
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshFilter.mesh.SetVertices(positions);
            meshFilter.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            meshFilter.mesh.SetTangents(tangents);
            meshFilter.mesh.SetUVs(0, uv);
            meshRenderer.material = GlobalMeshMaterial;
            return meshObject;
        }

        public void OnDestroy()
        {
            GetDestructibleMeshSegments(_segments);
            for (var i = _segments.Count - 1; i >= 0; i--)
            {
                if (_segments[i].TryGetComponent<MeshFilter>(out var mFilter) && mFilter.mesh != null)
                {
                    Destroy(mFilter.sharedMesh); // meshes are not automatically destroyed when their GO is destroyed
                }

                if (_segments[i].TryGetComponent<MeshRenderer>(out var mRenderer) && mRenderer.material != null)
                {
                    Destroy(mRenderer.material); // same for materials
                }

                Destroy(_segments[i]);
            }

            _segmentationTask.Dispose();
            _segmentationTask = null;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DestructibleMeshComponent))]
    public class DestructibleMeshComponentEditor : Editor
    {
        private int _numSegments = 0;
        private bool _initialized = false;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // Draws the default inspector
            var component = (DestructibleMeshComponent)target;
            if (GUILayout.Button("Debug Destructible Mesh"))
            {
                if (Application.isPlaying)
                {
                    try
                    {
                        SceneDebugger.DebugDestructibleMeshComponent(component);
                        Debug.Log("Debugging of destructible meshes completed.");
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Operation Not Available", e.Message, "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Operation Not Available",
                        "This operation is only available in Play Mode.", "OK");
                }
            }

            if (_initialized == false && component.transform.childCount != 0)
            {
                _numSegments = component.transform.childCount;
                _initialized = true;
            }

            EditorGUILayout.LabelField("Number of Segments", _numSegments.ToString());
        }
    }
#endif
}
