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
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    ///     The GridSliceResizer is a versatile tool designed to maintain the proportions of
    ///     specific areas of 3D meshes while allowing others to stretch during scaling.
    ///     The concept of the GridSliceResizer is similar to the popular 9-Slice-Scaling technique
    ///     used in 2D graphics, which keeps the borders of sprites unstretched while the inner rectangle is
    ///     stretched. In essence, the GridSliceResizer is a 27-Slice-Scaler for 3D meshes.
    ///     The component operates by dividing the bounding box of a 3D mesh into 27 cuboids, as illustrated below.
    ///     Not all cuboids are visible in this picture. Only the once that are front facing:
    ///     +-----+-----------+-----+
    ///     /_____/___________/_____/|
    ///     /_____/___________/_____/||
    ///     /     /           /     /|||
    ///     +-----+-----------+-----+ |||
    ///     |  A  |     B     |  C  |/|||
    ///     +-----+-----------+-----+ |||
    ///     |     |           |     | |||
    ///     |  D  |     E     |  F  | |||
    ///     |     |           |     |/||/
    ///     +-----+-----------+-----+ |/
    ///     |  G  |     H     |  I  | /
    ///     +-----+-----+-----+-----+
    ///     The scaling behaviour is as follows (assuming all other faces of the bounding box are divided as the
    ///     front facing one):
    ///     Center Cuboid (E): Vertices within this cuboid stretch on two axes (Y, Z).
    ///     Corner Cuboids (A, C, G, I): These cuboids do not stretch on any axis.
    ///     Middle Cuboids (B, H): These cuboids stretch horizontally but not vertically.
    ///     Middle Cuboids (D, F): These cuboids stretch vertically but not horizontally.
    ///     The slicing areas are defined by the PivotOffset and BorderXNegative, BorderXPositive, etc.
    ///     These border values range from 0 to 1 and extend from the mesh's pivot (which may be offset by PivotOffset)
    ///     to the maximum or minimum of the bounding box's axis.
    ///     If all borders are set to 1, the mesh will stretch like a regular mesh during scaling. If set to 0, no stretching
    ///     will occur. Typically, you'll want the pivot in the middle of the mesh and the borders set to around 0.8.
    ///     You can visualize the borders and pivot either in the editor, in the prefab mode and during play.
    ///     This component is only compatible with meshes that have read/write access enabled.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class GridSliceResizer : MonoBehaviour
    {
        /// <summary>
        /// Defines the method of scaling to be applied to different sections of the mesh.
        /// </summary>
        public enum Method
        {
            SLICE,
            SLICE_WITH_ASYMMETRICAL_BORDER,
            SCALE
        }

        /// <summary>
        ///     This parameter determines whether the center part of the object should be scaled.
        ///     If set to false, the center vertices will remain stationary. This is particularly useful when
        ///     you want to maintain the proportions of certain geometrical features in the center part, such
        ///     as a doorknob. By keeping the center vertices in place, you can avoid unwanted stretching effects,
        ///     resulting in a more visually appealing outcome.
        ///     However, it's important to note that for a convincing visual effect, the texture applied to the object
        ///     should also not stretch. If you encounter issues with texture stretching, consider adding a loop cut.
        ///     This can help maintain the texture's proportions and prevent it from distorting.
        /// </summary>
        [Flags]
        public enum StretchCenterAxis
        {
            X = 1 << 0,
            Y = 1 << 1,
            Z = 1 << 2
        }


        [Tooltip(
            "Represents the offset from the pivot point of the mesh. This offset is used to adjust the origin of scaling operations.")]
        public Vector3 PivotOffset;

        [Tooltip("Specifies the proportion of the mesh along the positive X-axis that is protected from scaling.")]

        [Space(15)] public Method ScalingX;

        [Tooltip("Specifies the proportion of the mesh along the negative X-axis that is protected from scaling.")]
        [Range(0, 1f)] public float BorderXNegative;


        [Tooltip("Specifies the proportion of the mesh along the positive X-axis that is protected from scaling.")]
        [Range(0, 1f)] public float BorderXPositive;

        [Tooltip(" Defines the scaling method to be applied along the Y-axis of the mesh.")]
        [Space(15)] public Method ScalingY;

        [Tooltip("Specifies the proportion of the mesh along the negative Y-axis that is protected from scaling.")]
        [Range(0, 1f)] public float BorderYNegative;

        [Tooltip("Specifies the proportion of the mesh along the positive Y-axis that is protected from scaling.")]
        [Range(0, 1f)] public float BorderYPositive;

        [Tooltip("Defines the scaling method to be applied along the Z-axis of the mesh.")]
        [Space(15)] public Method ScalingZ;

        [Tooltip("Specifies the proportion of the mesh along the negative Z-axis that is protected from scaling.")]
        [Range(0, 1f)] public float BorderZNegative;

        [Tooltip("Specifies the proportion of the mesh along the positive Z-axis that is protected from scaling.")]
        [Range(0, 1f)] public float BorderZPositive;

        [Tooltip("Specifies which axes should allow the center part of the object to stretch." +
                 "This setting is used to control the stretching behavior of the central section of the mesh" +
                 " allowing for selective stretching along specified axes.")]
        public StretchCenterAxis StretchCenter = 0;

        [Tooltip(
            "Indicates whether the resizer should update the mesh in play mode." +
            "When set to true, the mesh will continue to be updated based on the scaling settings during runtime." +
            "This can be useful for dynamic scaling effects but may impact performance if used excessively.")]
        public bool UpdateInPlayMode = true;

        [Tooltip(
            "The original mesh before any modifications. This mesh is used as the baseline for all scaling operations")]
        public Mesh OriginalMesh;

        private readonly Color[] _axisGizmosColors =
        {
            new(1, 0, 0, 0.5f), // red - X
            new(0, 1, 0, 0.5f), // green - Y
            new(0, 0, 1, 0.5f) // blue - Z
        };

        private float _cachedBorderXNegative;
        private float _cachedBorderXPositive;
        private float _cachedBorderYNegative;
        private float _cachedBorderYPositive;
        private float _cachedBorderZNegative;
        private float _cachedBorderZPositive;
        private const float _minBorderSize = 0.01f;
        private MeshFilter _meshFilter;
        private Vector3 _currentSize;
        private Bounds _boundingBox; // the bounding box of the mesh to resize
        private Bounds _scaledBoundingBox; // the bounding box of the mesh to resize scaled by the size
        private Matrix4x4 _pivotTransform = Matrix4x4.identity;
        private Matrix4x4 _scaledInvPivotTransform = Matrix4x4.identity;
        private Mesh _currentMesh;
        private MeshCollider _meshCollider;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (OriginalMesh != null)
            {
                _currentMesh = OriginalMesh;
                _meshFilter.sharedMesh = OriginalMesh;
            }
            else
            {
                _currentMesh = OriginalMesh = _meshFilter.sharedMesh;
            }

            _currentSize = OriginalMesh.bounds.size;
            _cachedBorderXNegative = BorderXNegative;
            _cachedBorderYNegative = BorderYNegative;
            _cachedBorderZNegative = BorderZNegative;
            _cachedBorderXPositive = BorderXPositive;
            _cachedBorderYPositive = BorderYPositive;
            _cachedBorderZPositive = BorderZPositive;
            _meshCollider = GetComponent<MeshCollider>();
        }

        private void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadGridSliceResizer).Send();
#endif
        }

        public void Update()
        {
            if ((Application.isPlaying && !UpdateInPlayMode)
                || !_meshFilter
                || !ShouldResize())
            {
                return;
            }

            var resizedMesh = ProcessVertices();
            resizedMesh.RecalculateBounds();
            resizedMesh.RecalculateNormals();
            resizedMesh.RecalculateTangents();
            resizedMesh.Optimize();

            _meshFilter.sharedMesh = resizedMesh;
            _currentSize = transform.lossyScale;
            UpdateCachedValues();
            if (!_meshCollider)
            {
                TryGetComponent(out _meshCollider);
                if (!_meshCollider)
                {
                    return;
                }
            }

            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = resizedMesh;
        }

        private void OnDestroy()
        {
            if (OriginalMesh)
            {
                _meshFilter.mesh = OriginalMesh;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw the pivot position
            Gizmos.color = Color.red;
            var lineSize = OriginalMesh.bounds.max.x / 10;
            var localPivot = transform.TransformPoint(PivotOffset);
            var startX = localPivot + Vector3.left * lineSize * 0.5f;
            var startY = localPivot + Vector3.down * lineSize * 0.5f;
            var startZ = localPivot + Vector3.back * lineSize * 0.5f;

            Gizmos.DrawRay(startX, Vector3.right * lineSize);
            Gizmos.DrawRay(startY, Vector3.up * lineSize);
            Gizmos.DrawRay(startZ, Vector3.forward * lineSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (_meshFilter == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Method[] scaling = { ScalingX, ScalingY, ScalingZ };
            float[] negativeBorders = { BorderXNegative, BorderYNegative, BorderZNegative };
            float[] positiveBorders = { BorderXPositive, BorderYPositive, BorderZPositive };
            var size = transform.lossyScale;
            var sizeInv = new Vector3(1 / size.x, 1 / size.y,
                1 / size.z);
            var originalScaledBoundsMin = Vector3.Scale(OriginalMesh.bounds.min, sizeInv);
            var originalScaledBoundsMax = Vector3.Scale(OriginalMesh.bounds.max, sizeInv);
            var originalScaledBounds = new Bounds(
                (originalScaledBoundsMin + originalScaledBoundsMax) * 0.5f,
                originalScaledBoundsMax - originalScaledBoundsMin
            );
            for (var i = 0; i <= 2; i++)
            {
                Gizmos.color = _axisGizmosColors[i];
                DrawBorderCubeGizmo(scaling[i], negativeBorders[i], positiveBorders[i], i);
            }

            Gizmos.color = new Color(0, 1, 1f, 0.5f);
            Gizmos.DrawWireCube(_boundingBox.center, _boundingBox.size);
        }

        /// <summary>
        ///     Processes the vertices of the given mesh, applying scaling and border adjustments.
        /// </summary>
        /// <remarks>
        ///     This function is expensive, as it access and modifies each vertex of the mesh.
        ///     Avoid calling it every frame.
        /// </remarks>
        /// <returns>
        ///     A new Mesh with processed vertices.
        /// </returns>
        public Mesh ProcessVertices()
        {
            var originalMesh = _currentMesh;
            var newSize = transform.lossyScale;
            // Clamp the border to avoid zero values (epsilon causes some issues)
            BorderXNegative = Mathf.Max(BorderXNegative, _minBorderSize);
            BorderYNegative = Mathf.Max(BorderYNegative, _minBorderSize);
            BorderZNegative = Mathf.Max(BorderZNegative, _minBorderSize);
            BorderXPositive = Mathf.Max(BorderXPositive, _minBorderSize);
            BorderYPositive = Mathf.Max(BorderYPositive, _minBorderSize);
            BorderZPositive = Mathf.Max(BorderZPositive, _minBorderSize);

            _pivotTransform.SetColumn(3, -PivotOffset);
            _scaledInvPivotTransform.SetColumn(3, Vector3.Scale(newSize, PivotOffset));
            var pivotBoundingBoxMin =
                _pivotTransform.MultiplyPoint3x4(Vector3.Min(originalMesh.bounds.min, PivotOffset));
            var pivotBoundingBoxMax =
                _pivotTransform.MultiplyPoint3x4(Vector3.Max(originalMesh.bounds.max, PivotOffset));

            // The bounding box of the mesh to resize scaled including the pivot point
            // This may be a bigger box as _scaledBoundingBox in case the pivot is outside of the scaled bounding box
            _boundingBox = new Bounds(
                (pivotBoundingBoxMin + pivotBoundingBoxMax) * 0.5f,
                pivotBoundingBoxMax - pivotBoundingBoxMin
            );
            _scaledBoundingBox = ScaleBounds(_boundingBox, transform.lossyScale);

            var lossyScaleInv = new Vector3(1.0f / newSize.x, 1.0f / newSize.y, 1.0f / newSize.z);
            var bordersPos = new Vector3(BorderXPositive, BorderYPositive, BorderZPositive);
            var bordersNeg = new Vector3(BorderXNegative, BorderYNegative, BorderZNegative);
            var scalingModes = new[] { ScalingX, ScalingY, ScalingZ };
            var borderPosPosition = Vector3.zero;
            var borderNegPosition = Vector3.zero;
            var stubPos = Vector3.zero;
            var stubNeg = Vector3.zero;
            var innerMax = Vector3.zero;
            var innerMin = Vector3.zero;
            var innerScaledMax = Vector3.zero;
            var innerScaledMin = Vector3.zero;
            var innerScaleRatioMax = Vector3.zero;
            var innerScaledRatioMin = Vector3.zero;
            var downscaleMax = Vector3.zero;
            var downscaleMin = Vector3.zero;

            var stretchCenter = new[]
            {
                (StretchCenter & StretchCenterAxis.X) != 0,
                (StretchCenter & StretchCenterAxis.Y) != 0,
                (StretchCenter & StretchCenterAxis.Z) != 0
            };

            for (var axis = 0; axis < 3; axis++)
            {
                switch (scalingModes[axis])
                {
                    case Method.SCALE:
                        stretchCenter[axis] = true; // override with the default scaling behavior
                        bordersNeg[axis] = bordersPos[axis] = 1; // everything should stretch
                        break;
                    case Method.SLICE:
                        bordersPos[axis] = bordersNeg[axis]; // symmetrical border
                        break;
                    case Method.SLICE_WITH_ASYMMETRICAL_BORDER:
                    default:
                        break;
                }

                var boundingBoxMax = _boundingBox.max[axis];
                var boundingBoxMin = _boundingBox.min[axis];

                // Locations of the border slices in local space
                borderPosPosition[axis] = boundingBoxMax - (1.0f - bordersPos[axis]) * Mathf.Abs(boundingBoxMax);
                borderNegPosition[axis] = boundingBoxMin + (1.0f - bordersNeg[axis]) * Mathf.Abs(boundingBoxMin);

                // Distance from the Border[Pos|Neg]LS to the outer maximum/minimum of the BBox
                stubPos[axis] = Mathf.Abs(boundingBoxMax - borderPosPosition[axis]);
                stubNeg[axis] = Mathf.Abs(boundingBoxMin - borderNegPosition[axis]);

                // The inner bounding box that should be stretched in all directions
                innerMax[axis] = boundingBoxMax - stubPos[axis];
                innerMin[axis] = boundingBoxMin + stubNeg[axis];

                // The expected bounding box of the inner bounding box when its scaled up by the size.
                // Max may be negative and Min may be positive in case the stubs are greater than
                // the scaled down bounding box and therefore don't fit the scaled bounding box.
                // This case gets treated special down below.
                innerScaledMax[axis] = _scaledBoundingBox.max[axis] - stubPos[axis];
                innerScaledMin[axis] = _scaledBoundingBox.min[axis] + stubNeg[axis];

                // The ratio between the inner bounding box and the scaled bounding box
                innerScaleRatioMax[axis] = Mathf.Max(0.0f, innerScaledMax[axis] / innerMax[axis]);
                innerScaledRatioMin[axis] = Mathf.Max(0.0f, innerScaledMin[axis] / innerMin[axis]);

                // The ratio to use for downscaling in case it's needed.
                // When Downscale[Min/Max] needs to be applied the temporary bounding box is
                // Max == StubPos, Min == StubNeg. Therefore get the ratio between it and the
                // expected scaled down bounding box to calculate the scale that needs
                // to be applied
                downscaleMax[axis] = _scaledBoundingBox.max[axis] / stubPos[axis];
                downscaleMin[axis] = _scaledBoundingBox.min[axis] / stubNeg[axis];
            }

            var resizedVertices = originalMesh.vertices;
            var transformedVertices = new Vector3[resizedVertices.Length];

            // pre-process to pre calculate if vertices need to be downscaled
            var scaleCenter = new[] { false, false, false };
            for (var i = 0; i < resizedVertices.Length; i++)
            {
                var transformedVertex = transformedVertices[i] = _pivotTransform.MultiplyPoint3x4(resizedVertices[i]);
                for (var axis = 0; axis < 3; axis++)
                {
                    if (0f <= transformedVertex[axis] && transformedVertex[axis] <= borderPosPosition[axis] &&
                        transformedVertex[axis] > innerScaledMax[axis])
                    {
                        scaleCenter[axis] = true;
                    }
                    else if (borderNegPosition[axis] <= transformedVertex[axis] && transformedVertex[axis] <= 0f &&
                             transformedVertex[axis] < innerScaledMin[axis])
                    {
                        scaleCenter[axis] = true;
                    }
                }
            }

            for (var i = 0; i < transformedVertices.Length; i++)
            {
                var newVertPosition = transformedVertices[i];
                for (var axis = 0; axis < 3; axis++)
                {
                    if (bordersNeg[axis] == 0.0f || bordersPos[axis] == 0.0f)
                    {
                        continue;
                    }

                    if (0.0f <= newVertPosition[axis] && newVertPosition[axis] <= borderPosPosition[axis] &&
                        (stretchCenter[axis] || scaleCenter[axis]))
                    {
                        // Vertex is inside the inner distance and should be stretched
                        newVertPosition[axis] *= innerScaleRatioMax[axis];
                    }
                    else if (borderNegPosition[axis] <= newVertPosition[axis] && newVertPosition[axis] <= 0 &&
                             (stretchCenter[axis] || scaleCenter[axis]))
                    {
                        // Vertex is inside the inner distance and should be stretched
                        newVertPosition[axis] *= innerScaledRatioMin[axis];
                    }
                    else if (borderPosPosition[axis] < newVertPosition[axis])
                    {
                        // Vertex is inside the outer stub and should not be stretched
                        newVertPosition[axis] =
                            borderPosPosition[axis] * innerScaleRatioMax[axis] +
                            (newVertPosition[axis] - borderPosPosition[axis]);
                        if (innerScaledMax[axis] < 0.0f)
                        // The mesh that would result from the linear transform above is still not small enough to
                        // fit into the expected scaled down bounding box. This means the stubs need to be scaled down
                        // to make them fit.
                        {
                            newVertPosition[axis] *= downscaleMax[axis];
                        }
                    }
                    else if (newVertPosition[axis] < borderNegPosition[axis])
                    {
                        // Vertex is inside the outer stub and should not be stretched
                        newVertPosition[axis] =
                            borderNegPosition[axis] * innerScaledRatioMin[axis] -
                            (borderNegPosition[axis] - newVertPosition[axis]);
                        if (innerScaledMin[axis] > 0.0f)
                        // The mesh that would result from the linear transform above is still not small enough to
                        // fit into the expected scaled down bounding box. This means the stubs need to be scaled down
                        // to make them fit.
                        {
                            newVertPosition[axis] *= -downscaleMin[axis];
                        }
                    }

                    // Undo pivot offset
                    resizedVertices[i] = Vector3.Scale(lossyScaleInv, newVertPosition);
                }
            }

            var clonedMesh = Instantiate(originalMesh);
            clonedMesh.vertices = resizedVertices;
            return clonedMesh;
        }

        private bool ShouldResize()
        {
            return _currentSize != transform.lossyScale
                   || Math.Abs(_cachedBorderXNegative - BorderXNegative) > Mathf.Epsilon
                   || Math.Abs(_cachedBorderYNegative - BorderYNegative) > Mathf.Epsilon
                   || Math.Abs(_cachedBorderZNegative - BorderZNegative) > Mathf.Epsilon
                   || Math.Abs(_cachedBorderXPositive - BorderXPositive) > Mathf.Epsilon
                   || Math.Abs(_cachedBorderYPositive - BorderYPositive) > Mathf.Epsilon
                   || Math.Abs(_cachedBorderZPositive - BorderZPositive) > Mathf.Epsilon;
        }

        private void UpdateCachedValues()
        {
            _cachedBorderXNegative = BorderXNegative;
            _cachedBorderXPositive = BorderXPositive;
            _cachedBorderYNegative = BorderYNegative;
            _cachedBorderYPositive = BorderYPositive;
            _cachedBorderZNegative = BorderZNegative;
            _cachedBorderZPositive = BorderZPositive;
        }

        private void DrawBorderCubeGizmo(Method scalingMethod, float borderNegative, float borderPositive, int axis)
        {
            var size = transform.lossyScale;
            var sizeInv = new Vector3(1 / size.x, 1 / size.y, 1 / size.z);
            var originalScaledBounds = ScaleBounds(_boundingBox, sizeInv);
            var boundingBoxSize = _boundingBox.size;
            switch (scalingMethod)
            {
                case Method.SLICE:
                    // The positive and negative borders are symmetrical, so one can be used
                    DrawPositiveDrawBorderForAxis(borderNegative, axis, originalScaledBounds, boundingBoxSize);
                    DrawNegativeBorderForAxis(borderNegative, axis, originalScaledBounds, boundingBoxSize);
                    break;

                case Method.SLICE_WITH_ASYMMETRICAL_BORDER:
                    //Positive Border
                    DrawPositiveDrawBorderForAxis(borderPositive, axis, originalScaledBounds, boundingBoxSize);

                    // Negative Border
                    DrawNegativeBorderForAxis(borderNegative, axis, originalScaledBounds, boundingBoxSize);
                    break;

                case Method.SCALE:
                default:
                    // Unity default scaling method, silently fall through.
                    break;
            }
        }

        private void DrawNegativeBorderForAxis(float borderNegative, int axis, Bounds originalScaledBounds,
            Vector3 boundingBoxSize)
        {
            boundingBoxSize[axis] = 0;
            var center = _boundingBox.center;
            center[axis] = _boundingBox.min[axis] - (originalScaledBounds.min[axis] -
                                                     (-Mathf.Abs(originalScaledBounds.min[axis] -
                                                                 PivotOffset[axis]) * borderNegative +
                                                      PivotOffset[axis])
                );
            if (center[axis] - PivotOffset[axis] > 0.0f)
            {
                center[axis] = _boundingBox.min[axis];
            }

            if (PivotOffset[axis] < _boundingBox.min[axis])
            {
                center[axis] = Mathf.Max(PivotOffset[axis] * transform.lossyScale[axis], center[axis]);
            }

            Gizmos.DrawWireCube(center, boundingBoxSize);
        }

        private void DrawPositiveDrawBorderForAxis(float borderNegative, int axis, Bounds originalScaledBounds,
            Vector3 boundingBoxSize)
        {
            boundingBoxSize[axis] = 0;
            var center = _boundingBox.center;
            center[axis] = _boundingBox.max[axis] - (originalScaledBounds.max[axis] -
                                                     (Mathf.Abs(originalScaledBounds.max[axis] -
                                                                PivotOffset[axis]) *
                                                         borderNegative + PivotOffset[axis]));
            if (center[axis] + PivotOffset[axis] < 0.0f)
            {
                center[axis] = _boundingBox.max[axis];
            }

            if (PivotOffset[axis] > _boundingBox.max[axis])
            {
                center[axis] = Mathf.Min(PivotOffset[axis] * transform.lossyScale[axis], center[axis]);
            }

            Gizmos.DrawWireCube(center, boundingBoxSize);
        }

        private Bounds ScaleBounds(Bounds originalBounds, Vector3 scale)
        {
            var originalScaledBoundsMin = Vector3.Scale(originalBounds.min, scale);
            var originalScaledBoundsMax = Vector3.Scale(originalBounds.max, scale);
            return new Bounds(
                (originalScaledBoundsMin + originalScaledBoundsMax) * 0.5f,
                originalScaledBoundsMax - originalScaledBoundsMin
            );
        }
    }
}
