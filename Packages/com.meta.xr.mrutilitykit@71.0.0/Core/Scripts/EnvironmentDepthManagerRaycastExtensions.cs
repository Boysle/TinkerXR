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
using System.Linq;
using System.Runtime.CompilerServices;
using Meta.XR.EnvironmentDepth;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Meta.XR
{
    internal static class EnvironmentDepthManagerRaycastExtensions
    {
        private const Eye DefaultEye = Eye.Both;
        private const float MinXYSize = 0.05f;
        private static EnvironmentDepthRaycaster _depthRaycast;

        public static bool Raycast(this EnvironmentDepthManager depthManager, Ray ray, out DepthRaycastHit hitInfo, float maxDistance = 100f, Eye eye = DefaultEye, bool reconstructNormal = true, bool allowOccludedRayOrigin = true)
        {
            EnsureDepthRaycastComponentIsPresent(depthManager);
            if (reconstructNormal)
            {
                var result = _depthRaycast.Raycast(ray, out var position, out var normal, out var normalConfidence, maxDistance, eye, allowOccludedRayOrigin);
                hitInfo = new DepthRaycastHit
                {
                    result = result,
                    point = position,
                    normal = normal,
                    normalConfidence = normalConfidence
                };
                return result == DepthRaycastResult.Success;
            }
            var posOnlyResult = _depthRaycast.Raycast(ray, maxDistance, eye, allowOccludedRayOrigin);
            hitInfo = new DepthRaycastHit
            {
                result = posOnlyResult.status,
                point = posOnlyResult.position
            };
            return posOnlyResult.status == DepthRaycastResult.Success;
        }

        /// <summary>
        /// Enables/disables depth texture prefetching for raycasts (enabled by default).<br/>
        /// Enable warm-up to always get raycast results immediately.<br/>
        /// Disable warm-up to save performance when not casting rays. When warm-up is disabled, the first 3 subsequent raycasts will return <see cref="DepthRaycastResult.NotReady"/>.
        /// This is not an issue when you cast rays continuously, but if you cast rays every 4th frame, you’ll never get a successful result.
        /// </summary>
        public static void SetRaycastWarmUpEnabled(this EnvironmentDepthManager depthManager, bool value)
        {
            EnsureDepthRaycastComponentIsPresent(depthManager);
            _depthRaycast._warmUpRaycast = value;
        }

        private static void EnsureDepthRaycastComponentIsPresent(EnvironmentDepthManager depthManager)
        {
            Assert.IsNotNull(depthManager, nameof(depthManager));
            if (_depthRaycast == null)
            {
                Assert.IsNull(depthManager.GetComponent<EnvironmentDepthRaycaster>());
                _depthRaycast = depthManager.gameObject.AddComponent<EnvironmentDepthRaycaster>();
                _depthRaycast.depthManager = depthManager;
            }
        }

        public static bool PlaceBox(this EnvironmentDepthManager depthManager, Ray ray, Vector3 boxSize, Vector3 upwards, out DepthRaycastHit hit, float maxDistance = 100f)
        {
            if (boxSize.x < MinXYSize || boxSize.y < MinXYSize)
            {
                Debug.LogWarning($"'x' and 'y' components of the '{nameof(boxSize)}' should be greater than {MinXYSize} to determine the surface normal.");
                hit = new DepthRaycastHit { result = DepthRaycastResult.NoHit };
                return false;
            }
            if (boxSize.z < 0f)
            {
                Debug.LogWarning($"'z' component of the '{nameof(boxSize)}' should be >= 0f.");
                hit = new DepthRaycastHit { result = DepthRaycastResult.NoHit };
                return false;
            }
            if (!depthManager.Raycast(ray, out hit, maxDistance) || hit.normalConfidence < 0.5f)
            {
                Log("center raycast failed");
                return false;
            }

            // Check if all corners of the base rectangle lay on the same plane.
            DrawLine(hit.point, hit.point + hit.normal, Color.white);
            Span<Vector3> cornerOffsets = stackalloc Vector3[]
            {
                new Vector3(-1, -1), new Vector3(-1, 1), new Vector3(1, 1), new Vector3(1, -1)
            };
            for (int i = 0; i < cornerOffsets.Length; i++)
            {
                cornerOffsets[i] = Vector3.Scale(boxSize * 0.5f, cornerOffsets[i]);
            }
            var rotation = Quaternion.LookRotation(hit.normal, upwards);
            Span<Vector3> corners = stackalloc Vector3[cornerOffsets.Length];
            float cornerPosToleranceSqr = Mathf.Pow(Mathf.Max(boxSize.x, boxSize.y) * 0.2f, 2);
            for (int i = 0; i < corners.Length; i++)
            {
                var cornerPos = hit.point + rotation * cornerOffsets[i];
                DrawLine(ray.origin, cornerPos, Color.white);
                if (!depthManager.Raycast(new Ray(ray.origin, cornerPos - ray.origin), out var cornerHit))
                {
                    Log($"corner {i} raycast failed");
                    return false;
                }
                if (Vector3.Project(cornerHit.point - hit.point, hit.normal).sqrMagnitude > cornerPosToleranceSqr)
                {
                    Log($"corner {i} is not on the same plane");
                    return false;
                }
                if (Vector3.Dot(cornerHit.normal, hit.normal) < 0.6f)
                {
                    Log($"corner {i} has different normal");
                    return false;
                }
                corners[i] = cornerHit.point;
                DrawLine(cornerPos, cornerHit.point, Color.blue);
            }

            // Update normal based on corners
            var normal1 = -Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]).normalized;
            var normal2 = -Vector3.Cross(corners[1] - corners[3], corners[2] - corners[3]).normalized;
            var normalFromCorners = Vector3.Normalize(normal1 + normal2);
            if (Vector3.Dot(normalFromCorners, hit.normal) < 0.9f)
            {
                Log("Vector3.Dot(normalFromCorners, normal) < threshold");
                return false;
            }
            hit.normal = normalFromCorners;

            // Check for collisions.
            rotation = Quaternion.LookRotation(hit.normal, upwards);
            const float collisionCheckOffset = 0.05f;
            if (boxSize.z >= collisionCheckOffset)
            {
                return !depthManager.CheckBox(hit.point + hit.normal * (boxSize.z * 0.5f + collisionCheckOffset), boxSize * 0.5f, rotation);
            }

            Span<int> indices = stackalloc int[]
            {
                0, 1, 1, 2, 2, 3, 3, 0, 0, 2, 1, 3
            };
            for (int i = 0; i < indices.Length; i += 2)
            {
                var curCorner = hit.point + rotation * cornerOffsets[indices[i]] + hit.normal * collisionCheckOffset;
                var nextCorner = hit.point + rotation * cornerOffsets[indices[i + 1]] + hit.normal * collisionCheckOffset;
                var direction = nextCorner - curCorner;
                var collisionCheckRay = new Ray(curCorner, direction);
                if (depthManager.Raycast(collisionCheckRay, out var collisionHit, direction.magnitude))
                {
                    DrawLine(curCorner, collisionHit.point, Color.red);
                    return false;
                }
                DrawLine(curCorner, nextCorner, Color.green);
            }

            return true;
        }

        public static bool CheckBox(this EnvironmentDepthManager depthManager, Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            Span<Vector3> axes = stackalloc[]
            {
                Vector3.right, Vector3.up, Vector3.forward
            };
            int numAxes = axes.Length;
            for (int i = 0; i < numAxes; i++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var p0 = center - orientation * halfExtents * sign;
                    var p1 = p0 + orientation * axes[i] * (halfExtents[i % 3] * 2f) * sign;

                    const int density = 2;
                    var delta = orientation * axes[(i + 1) % numAxes] * (halfExtents[(i + 1) % 3] * 2f) / density * sign;
                    for (int j = 0; j < density + 1; j++)
                    {
                        var direction = p1 - p0;
                        float distance = direction.magnitude;
                        // The user can supply a box with zero size along any axis.
                        // Do the collision check only if the box edge length is non-zero.
                        if (distance > 0.01f)
                        {
                            depthManager.Raycast(new Ray(p0, direction), out var hit, distance, reconstructNormal: false, allowOccludedRayOrigin: false);
                            if (hit.result != DepthRaycastResult.NoHit)
                            {
                                // If any edge of the box is occluded by or intersects with the depth texture, return false.
                                DrawLine(p0, hit.point, Color.red);
                                DrawLine(Vector3.zero, hit.point, Color.red);
                                return true;
                            }
                        }
                        DrawLine(p0, p1, Color.green);
                        p0 += delta;
                        p1 += delta;
                    }
                }
            }
            return false;
        }

        [System.Diagnostics.Conditional("DEBUG_DEPTH_RAYCAST")]
        private static void Log(string msg)
        {
            Debug.Log($"{Time.frameCount} {msg}");
        }

        [System.Diagnostics.Conditional("DEBUG_DEPTH_RAYCAST")]
        internal static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            Debug.DrawLine(start, end, color);
        }
    }

    [AddComponentMenu("")] // hide component from the 'Add Component' menu to hide it from the user
    internal class EnvironmentDepthRaycaster : MonoBehaviour
    {
        private static readonly int EnvironmentDepthTextureId = Shader.PropertyToID("_EnvironmentDepthTexture");
        private static readonly int EnvironmentDepthTextureSizeId = Shader.PropertyToID("_EnvironmentDepthTextureSize");
        private static readonly int CopiedDepthTextureId = Shader.PropertyToID("_CopiedDepthTexture");
        private static readonly int EnvironmentDepthZBufferParamsId = Shader.PropertyToID("_EnvironmentDepthZBufferParams");
        private const int TextureSize = 128;
        private const int NumEyes = 2;

        private ComputeShader _shader;
        internal EnvironmentDepthManager depthManager;
        private ComputeBuffer _computeBuffer;
        private NativeArray<float> _depthTexturePixels;
        private NativeArray<float> _gpuRequestBuffer;
        private bool _isDepthTextureAvailable;
        private AsyncGPUReadbackRequest? _currentGpuReadbackRequest;
        private uint? _prevTextureId;
        private bool _copyDepthTexture;

        private Matrix4x4[] _matrixVP = new Matrix4x4[NumEyes];
        private Matrix4x4[] _matrixV = new Matrix4x4[NumEyes];
        private Matrix4x4[] _matrixVP_inv = new Matrix4x4[NumEyes];
        private readonly Plane[][] _camFrustumPlanes = { new Plane[6], new Plane[6] };
        private Vector4 _EnvironmentDepthZBufferParams;
        private DepthFrameDesc[] _depthFrameDesc = new DepthFrameDesc[NumEyes];
        private Matrix4x4 _worldToTrackingSpaceMatrix = Matrix4x4.identity;
        internal bool _warmUpRaycast;
        private int _currentEyeIndex;
        private XRDisplaySubsystem _xrDisplay;


        private void Awake()
        {
            const string shaderName = "CopyDepthTexture";
            _shader = Resources.Load<ComputeShader>(shaderName);
            Assert.IsNotNull(_shader, "Compute shader '" + shaderName + "' not found in the Resources folder.");

            const int numPixels = TextureSize * TextureSize * NumEyes;
            _computeBuffer = new ComputeBuffer(numPixels, sizeof(float));
            _depthTexturePixels = new NativeArray<float>(numPixels, Allocator.Persistent);
            _gpuRequestBuffer = new NativeArray<float>(numPixels, Allocator.Persistent);

            var displays = new List<XRDisplaySubsystem>(1);
            SubsystemManager.GetInstances(displays);
            _xrDisplay = displays.Single();
            Assert.IsNotNull(_xrDisplay, nameof(_xrDisplay));
        }

        private void OnDisable() => InvalidateDepthTexture();

        private void InvalidateDepthTexture()
        {
            _copyDepthTexture = false;
            _isDepthTextureAvailable = false;
            _prevTextureId = null;
        }

        private void OnDestroy()
        {
            if (_currentGpuReadbackRequest.HasValue && !_currentGpuReadbackRequest.Value.done)
            {
                _currentGpuReadbackRequest.Value.WaitForCompletion();
            }
            _computeBuffer.Dispose();
            _depthTexturePixels.Dispose();
            _gpuRequestBuffer.Dispose();
        }

        private void CreateTextureCopyRequestIfNeeded()
        {
            if (_currentGpuReadbackRequest.HasValue)
            {
                return;
            }
            if (!(depthManager.enabled && depthManager.IsDepthAvailable))
            {
                InvalidateDepthTexture();
                return;
            }

            uint textureId = 0;
            if (!_xrDisplay.running || !EnvironmentDepthManager._provider.GetDepthTextureId(ref textureId) || _prevTextureId == textureId)
            {
                return;
            }
            _prevTextureId = textureId;

            if (!_warmUpRaycast && !_copyDepthTexture)
            {
                InvalidateDepthTexture();
                return;
            }
            _copyDepthTexture = false;

            _depthFrameDesc[0] = EnvironmentDepthManager._provider.GetFrameDesc(0);
            _depthFrameDesc[1] = EnvironmentDepthManager._provider.GetFrameDesc(1);
            _worldToTrackingSpaceMatrix = depthManager.GetTrackingSpaceWorldToLocalMatrix();

#if UNITY_2022_3_OR_NEWER
            var depthTexture = _xrDisplay.GetRenderTexture(textureId);
#else
            RenderTexture depthTexture = null;
#endif
            if (depthTexture == null)
            {
                return;
            }
            _shader.SetTexture(0, EnvironmentDepthTextureId, depthTexture);
            _shader.SetFloat(EnvironmentDepthTextureSizeId, depthTexture.width);
            Assert.AreEqual(depthTexture.width, depthTexture.height);

            _EnvironmentDepthZBufferParams = Shader.GetGlobalVector(EnvironmentDepthZBufferParamsId);
            _shader.SetVector(EnvironmentDepthZBufferParamsId, _EnvironmentDepthZBufferParams);
            _shader.SetBuffer(0, CopiedDepthTextureId, _computeBuffer);
            _shader.Dispatch(0, 1, 1, 1);

            _currentGpuReadbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref _gpuRequestBuffer, _computeBuffer);
        }

        private void UpdateTextureCopyRequest()
        {
            if (!_currentGpuReadbackRequest.HasValue || !_currentGpuReadbackRequest.Value.done)
            {
                return;
            }
            if (_currentGpuReadbackRequest.Value.hasError)
            {
                Debug.LogError("AsyncGPUReadback.RequestIntoNativeArray() hasError");
            }
            else
            {
                // AsyncGPUReadback.RequestIntoNativeArray() requires an exclusive access to the NativeArray,
                // so use two buffers and swap them after the request is completed
                (_depthTexturePixels, _gpuRequestBuffer) = (_gpuRequestBuffer, _depthTexturePixels);

                // Update depth camera matrices after the depth texture is received.
                // We don't use the most recent DepthFrameDesc and _worldToTrackingSpaceMatrix. Instead, we use the saved values from the frame in which CreateTextureCopyRequestIfNeeded() was called.
                // This guarantees that the _depthTexturePixels array is in sync with depth camera matrices.
                for (int i = 0; i < NumEyes; i++)
                {
                    EnvironmentDepthUtils.CalculateDepthCameraMatrices(_depthFrameDesc[i], out var proj, out var view);
                    view *= _worldToTrackingSpaceMatrix;
                    _matrixV[i] = view;
                    _matrixVP[i] = proj * view;

                    GeometryUtility.CalculateFrustumPlanes(_matrixVP[i], _camFrustumPlanes[i]);
                    _matrixVP_inv[i] = _matrixVP[i].inverse;
                }

                _isDepthTextureAvailable = true;

            }

            _currentGpuReadbackRequest = null;
        }

        /// This method is called before other MonoBehaviour.Update(), see 'Project Settings/Script Execution Order'.
        private void Update()
        {
            if (depthManager == null)
            {
                Destroy(this);
                return;
            }

            UpdateTextureCopyRequest();
            CreateTextureCopyRequestIfNeeded();
        }

        private Vector2Int WorldPosToNonNormalizedTextureCoords(Vector3 worldPos)
        {
            var clipPos = _matrixVP[_currentEyeIndex] * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1.0f);
            var uv = (new Vector2(clipPos.x, clipPos.y) / clipPos.w + Vector2.one) * 0.5f;
            var texCoord = new Vector2Int(Mathf.Clamp((int)(uv.x * TextureSize), 0, TextureSize - 1),
                                          Mathf.Clamp((int)(uv.y * TextureSize), 0, TextureSize - 1));
            Assert.IsTrue(IsInBounds(texCoord));
            return texCoord;
        }

        private float SampleDepthTexture(Vector2Int texCoord)
        {
            Assert.IsTrue(IsInBounds(texCoord));
            return _depthTexturePixels[texCoord.x + texCoord.y * TextureSize + TextureSize * TextureSize * _currentEyeIndex];
        }

        private Vector3 WorldPosAtDepthTexCoord(Vector2Int texCoord)
        {
            float linearDepth = SampleDepthTexture(texCoord);
            float clipSpaceDepth = linearDepth == 0.0f ? 0.0f : _EnvironmentDepthZBufferParams.x / linearDepth - _EnvironmentDepthZBufferParams.y;
            const float oneOverTextureSize = 1f / TextureSize;
            var clipPos = new Vector4(texCoord.x * oneOverTextureSize * 2.0f - 1.0f,
                                      texCoord.y * oneOverTextureSize * 2.0f - 1.0f, clipSpaceDepth, 1.0f);
            var homogeneousWorldPos = _matrixVP_inv[_currentEyeIndex] * clipPos;
            Vector3 worldPos = homogeneousWorldPos / homogeneousWorldPos.w;
            return worldPos;
        }

        private float WorldPosToLinearDepth(Vector3 worldPos)
        {
            var viewPos = _matrixV[_currentEyeIndex] * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1.0f);
            float linearDepth = -viewPos.z;
            return linearDepth;
        }

        /// https://atyuwen.github.io/posts/normal-reconstruction/
        /// 5 taps in each direction: | z | x | * | y | w |, '*' denotes the center sample.
        private Vector3 ReconstructNormal(Vector2Int texCoord)
        {
            float centerDepth = SampleDepthTexture(texCoord);
            var centerWorldPos = WorldPosAtDepthTexCoord(texCoord);
            var horDeriv = ClosestDerivativeToAdjacentExtrapolations(new Vector2Int(1, 0));
            var verDeriv = ClosestDerivativeToAdjacentExtrapolations(new Vector2Int(0, 1));
            // Flip the sign because Unity uses left-handed coordinate system
            return -Vector3.Normalize(Vector3.Cross(horDeriv, verDeriv));

            Vector3 ClosestDerivativeToAdjacentExtrapolations(Vector2Int axis)
            {
                var offsets = new Vector4(
                    SampleDepthTexture(texCoord - axis),
                    SampleDepthTexture(texCoord + axis),
                    SampleDepthTexture(texCoord - axis * 2),
                    SampleDepthTexture(texCoord + axis * 2)
                );
                var extrapolations = new Vector2(Mathf.Abs(offsets.x * offsets.z / (2 * offsets.z - offsets.x) - centerDepth),
                                                 Mathf.Abs(offsets.y * offsets.w / (2 * offsets.w - offsets.y) - centerDepth));
                return extrapolations.x > extrapolations.y ?
                    WorldPosAtDepthTexCoord(texCoord + axis) - centerWorldPos :
                    centerWorldPos - WorldPosAtDepthTexCoord(texCoord - axis);
            }
        }

        internal DepthRaycastResult Raycast(Ray ray, out Vector3 position, out Vector3 normal, out float normalConfidence, float maxDistance, Eye eye, bool allowOccludedRayOrigin)
        {
            normal = default;
            normalConfidence = 0f;
            var result = Raycast(ray, maxDistance, eye, allowOccludedRayOrigin);
            position = result.position;
            _currentEyeIndex = result.eyeIndex; // modify current eye index to calculate the normal based on selected eye index
            if (result.status != DepthRaycastResult.Success)
            {
                return result.status;
            }

            const int filterOffset = 2;
            const int filterSize = filterOffset + 2; // We calculate normal based on two-pixel offset in each direction
            var texCoord = WorldPosToNonNormalizedTextureCoords(position);
            if (texCoord.x < filterSize || texCoord.x >= TextureSize - filterSize || texCoord.y < filterSize || texCoord.y >= TextureSize - filterSize)
            {
                return DepthRaycastResult.RayOutsideOfDepthCameraFrustum;
            }

            Span<Vector2Int> normalFilterOffsets = stackalloc Vector2Int[]
            {
                new Vector2Int(-filterOffset, 0),
                new Vector2Int(filterOffset, 0),
                new Vector2Int(0, 0),
                new Vector2Int(0, -filterOffset),
                new Vector2Int(0, filterOffset)
            };
            int numOffsets = normalFilterOffsets.Length;
            Span<Vector3> normals = stackalloc Vector3[numOffsets];

            var avgNormal = Vector3.zero;
            for (int i = 0; i < numOffsets; i++)
            {
                var curNormal = ReconstructNormal(texCoord + normalFilterOffsets[i]);
                normals[i] = curNormal;
                avgNormal += curNormal;
            }
            avgNormal = Vector3.Normalize(avgNormal);
            normal = avgNormal;

            float filteredCount = 0;
            for (int i = 0; i < numOffsets; i++)
            {
                if (Vector3.Dot(normals[i], avgNormal) > 0.95f)
                {
                    filteredCount++;
                }
            }
            normalConfidence = filteredCount / numOffsets;

            return result.status;
        }

        internal (DepthRaycastResult status, Vector3 position, int eyeIndex) Raycast(Ray ray, float maxDistance, Eye eye, bool allowOccludedRayOrigin)
        {
            Assert.IsTrue(maxDistance >= 0f);
            _copyDepthTexture = true;
            CreateTextureCopyRequestIfNeeded();
            if (!_isDepthTextureAvailable)
            {
                return (DepthRaycastResult.NotReady, default, default);
            }

            if (eye != Eye.Both)
            {
                return GetRaycastResultForEye(eye == Eye.Left ? 0 : 1);
            }

            // Raycast using both left and right eye and select the best result.
            // If either left or right raycast returns DepthRaycastResult.Success, use the first succeeded result because we can't be more precise than that.
            // For example, if one eye sees the hit point, another eye will either see the same point or the point will be occluded.
            var left = GetRaycastResultForEye(0);
            if (left.status == DepthRaycastResult.Success)
            {
                return left;
            }
            var right = GetRaycastResultForEye(1);
            if (right.status == DepthRaycastResult.Success)
            {
                return right;
            }
            // If both raycasts have occluded hit points, take the one that traveled longer distance from ray.origin (bigger distance -> more environment data observed -> more precise result).
            if (left.status == DepthRaycastResult.HitPointOccluded && right.status == DepthRaycastResult.HitPointOccluded)
            {
                return Vector3.Distance(ray.origin, left.position) > Vector3.Distance(ray.origin, right.position) ? left : right;
            }
            // It doesn't matter which one we choose here because both raycasts failed.
            return left;

            (DepthRaycastResult status, Vector3 position, int eyeIndex) GetRaycastResultForEye(int index)
            {
                if (!ClampRayOriginToCamFrustumPlanes(ref ray, _camFrustumPlanes[index], ref maxDistance))
                {
                    return (DepthRaycastResult.RayOutsideOfDepthCameraFrustum, default, default);
                }

                // Clamp maxDistance by nearPlane when casting in the opposite direction (in the direction of user's head).
                // Without the clamping 'WorldPosToLinearDepth(rayEnd)' produces negative value, but negative depth doesn't work with 1/depth optimization.
                var nearPlane = _camFrustumPlanes[index][4];
                bool isOppositeDir = Vector3.Dot(ray.direction, nearPlane.normal) < 0f;
                if (isOppositeDir && nearPlane.Raycast(ray, out float distToNearPlane) && maxDistance > distToNearPlane)
                {
                    maxDistance = distToNearPlane;
                }

                var status = RaycastInternal(ray, out var position, maxDistance, index, allowOccludedRayOrigin);
                return (status, position, index);
            }
        }

        private static Vector3 ClosestPointOnFirstRay(Vector3 ray1Pos, Vector3 ray1Dir, Vector3 ray2Pos, Vector3 ray2Dir)
        {
            var lineVec3 = ray2Pos - ray1Pos;
            var crossVec1and2 = Vector3.Cross(ray1Dir, ray2Dir);
            var crossVec3and2 = Vector3.Cross(lineVec3, ray2Dir);
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / Vector3.Dot(crossVec1and2, crossVec1and2);
            var intersection = ray1Pos + ray1Dir * s;
            return intersection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInBounds(Vector2Int texCoord) => texCoord.x >= 0 && texCoord.x < TextureSize && texCoord.y >= 0 && texCoord.x < TextureSize;

        private DepthRaycastResult RaycastInternal(Ray ray, out Vector3 position, float maxDistance, int eyeIndex, bool allowOccludedRayOrigin)
        {
            position = default;
            if (!Mathf.Approximately(ray.direction.sqrMagnitude, 1f))
            {
                Debug.LogError("ray.direction should be normalized.");
                return DepthRaycastResult.NoHit;
            }
            if (maxDistance < 0.01f)
            {
                return DepthRaycastResult.NoHit;
            }

            _currentEyeIndex = eyeIndex;
            var rayOrigin = ray.origin;
            var rayDir = ray.direction.normalized;
            var rayEnd = rayOrigin + maxDistance * rayDir;

            var projectedRayOrigin = WorldPosToNonNormalizedTextureCoords(rayOrigin);
            if (!allowOccludedRayOrigin)
            {
                var rayOriginDepth = WorldPosToLinearDepth(rayOrigin);
                var envDepth = SampleDepthTexture(projectedRayOrigin);
                if (rayOriginDepth > envDepth)
                {
                    EnvironmentDepthManagerRaycastExtensions.DrawLine(Vector3.zero, rayOrigin, Color.blue);
                    return DepthRaycastResult.RayOccluded;
                }
            }
            var projectedRayEnd = WorldPosToNonNormalizedTextureCoords(rayEnd);
            int lengthX = projectedRayEnd.x - projectedRayOrigin.x;
            int lengthY = projectedRayEnd.y - projectedRayOrigin.y;
            int maxSideLength = Mathf.Max(Mathf.Abs(lengthX), Mathf.Abs(lengthY));
            float invDepthStart = 1.0f / WorldPosToLinearDepth(rayOrigin);
            float invDepthEnd = 1.0f / WorldPosToLinearDepth(rayEnd);

            float deltaX = (float)lengthX / maxSideLength;
            float deltaY = (float)lengthY / maxSideLength;
            float invRayDepthDelta = (invDepthEnd - invDepthStart) / maxSideLength;

            float currentX = projectedRayOrigin.x;
            float currentY = projectedRayOrigin.y;
            float invRayDepth = invDepthStart;
            bool foundEmptySpace = false; // We only start tracing after we find some empty space
            for (int i = 0; i <= maxSideLength; i++)
            {
                var texCoord = new Vector2Int((int)currentX, (int)currentY);
                if (!IsInBounds(texCoord))
                {
                    return DepthRaycastResult.RayOutsideOfDepthCameraFrustum;
                }

                float envDepth = SampleDepthTexture(texCoord);
                if (envDepth != 0.0f)
                {
                    float rayDepth = 1.0f / invRayDepth;
                    if (!foundEmptySpace)
                    {
                        foundEmptySpace = envDepth > rayDepth;
                    }
                    else
                    {
                        if (envDepth <= rayDepth)
                        {
                            // If the discontinuity in the depth texture is big, it can mean that:
                            // - the actual hit point is occluded by the real-world object
                            // - the actual hit point lies on the invisible side of a solid object
                            // There is no way to distinguish between these two cases, so return 'HitPointOccluded'
                            var prevTexCoord = new Vector2Int((int)(currentX - deltaX), (int)(currentY - deltaY));
                            float prevEnvDepth = SampleDepthTexture(prevTexCoord);

                            // Returning a closet point on raycast ray creates an effect of 'interpolation' between worldPos1 and worldPos2, producing smooth results
                            var worldPos1 = WorldPosAtDepthTexCoord(prevTexCoord);
                            var worldPos2 = WorldPosAtDepthTexCoord(texCoord);
                            position = ClosestPointOnFirstRay(rayOrigin, rayDir, worldPos1, worldPos2 - worldPos1);
                            return prevEnvDepth - envDepth > 0.3f ? DepthRaycastResult.HitPointOccluded : DepthRaycastResult.Success;
                        }
                    }
                }

                currentX += deltaX;
                currentY += deltaY;
                invRayDepth += invRayDepthDelta;
            }

            return foundEmptySpace ? DepthRaycastResult.NoHit : DepthRaycastResult.RayOccluded;
        }

        private static bool ClampRayOriginToCamFrustumPlanes(ref Ray ray, Plane[] planes, ref float maxDistance)
        {
            Assert.AreEqual(6, planes.Length);
            if (GeometryUtility.TestPlanesAABB(planes, new Bounds(ray.origin, Vector3.zero)))
            {
                return true;
            }
            // Skip the far cam plane at the last index 5
            for (int i = 0; i < 5; i++)
            {
                var plane = planes[i];
                if (plane.Raycast(ray, out float dist))
                {
                    const float tolerance = 0.01f;
                    if (GeometryUtility.TestPlanesAABB(planes, new Bounds(ray.GetPoint(dist + tolerance), Vector3.zero)))
                    {
                        maxDistance -= dist;
                        if (maxDistance <= 0f)
                        {
                            return false;
                        }

                        ray.origin = ray.GetPoint(dist);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    internal struct DepthRaycastHit
    {
        public DepthRaycastResult result;
        public Vector3 point;
        public Vector3 normal;
        public float normalConfidence;
    }

    internal enum DepthRaycastResult
    {
        Success,
        /// Hit point is not visible, the <see cref="DepthRaycastHit.point"/> contains the last visible point on ray.
        HitPointOccluded,
        /// Call <see cref="EnvironmentDepthManagerRaycastExtensions.SetRaycastWarmUpEnabled"/> with 'true' to keep the depth raycast always ready.
        NotReady,
        /// Depth raycast only works with the visible area in front of the user.
        RayOutsideOfDepthCameraFrustum,
        /// Raycast ray is completely occluded by the depth texture.
        RayOccluded,
        /// Either no depth info is available yet or the raycast's maxDistance is less than the environment depth.
        NoHit
    }

    internal enum Eye
    {
        Left,
        Right,
        Both
    }
}
