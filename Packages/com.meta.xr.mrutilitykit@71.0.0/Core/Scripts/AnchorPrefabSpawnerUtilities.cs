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
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    ///     A utility class for spawning prefabs at anchor points in the scene.
    /// </summary>
    public static class AnchorPrefabSpawnerUtilities
    {
        // Utilities to use when dealing with anchors that have valid volume bounds (e.g.: couches, tables, etc.)

        #region VolumeUtilities

        /// <summary>
        ///     Calculates a transformation matrix that matches the volume of the anchor.
        /// </summary>
        /// <param name="anchorInfo">The anchor of which volume has to be match.</param>
        /// <param name="matchAspectRatio">Whether to match the aspect ratio of the anchor.</param>
        /// <param name="calculateFacingDirection">Whether to calculate the facing direction of the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="scalingMode">The scaling mode to use. See <see cref="AnchorPrefabSpawner.ScalingMode" />.</param>
        /// <param name="alignMode">The alignment mode to use. See <see cref="AnchorPrefabSpawner.AlignMode" />.</param>
        /// <returns>A transformation matrix that matches the volume of the anchor.</returns>
        /// <remarks>
        ///     This method calculates the local scale and pose of a prefab based on the anchor's volume,
        ///     and then combines these to return a transformation matrix.
        /// </remarks>
        /// <seealso cref="AnchorPrefabSpawnerUtilities.GetTransformationMatrixMatchingAnchorPlaneRect" />
        public static Matrix4x4 GetTransformationMatrixMatchingAnchorVolume(MRUKAnchor anchorInfo,
            bool matchAspectRatio, bool calculateFacingDirection, Bounds? prefabBounds,
            AnchorPrefabSpawner.ScalingMode scalingMode = AnchorPrefabSpawner.ScalingMode.Stretch,
            AnchorPrefabSpawner.AlignMode alignMode = AnchorPrefabSpawner.AlignMode.Automatic)
        {
            var localScale = GetPrefabScaleBasedOnAnchorVolume(anchorInfo, matchAspectRatio, calculateFacingDirection,
                prefabBounds, out var cardinalAxisIndex, scalingMode);
            var prefabPose = GetPoseBasedOnAnchorVolume(anchorInfo, prefabBounds, cardinalAxisIndex, localScale,
                alignMode);
            return Matrix4x4.TRS(prefabPose.position, prefabPose.rotation, localScale);
        }

        /// <summary>
        ///     Scales a prefab based on the specified scaling mode.
        /// </summary>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <param name="scalingMode">The scaling mode to use. See <see cref="AnchorPrefabSpawner.ScalingMode" />.</param>
        /// <returns>The scaled local scale of the prefab.</returns>
        /// <remarks>
        ///     This method is used to scale a prefab when instantiating it as an anchor with a volume.
        ///     If the scaling mode is UniformScaling, it scales the prefab uniformly based on the smallest axis.
        ///     If the scaling mode is UniformXZScale, it scales the prefab uniformly on the X and Z axes.
        ///     If the scaling mode is NoScaling, it returns a unit vector.
        ///     If the scaling mode is Custom, it uses the provided custom scaling logic.
        ///     If the scaling mode is Stretch, it returns the anchor's original local scale.
        /// </remarks>
        public static Vector3 ScalePrefab(Vector3 localScale,
            AnchorPrefabSpawner.ScalingMode scalingMode = AnchorPrefabSpawner.ScalingMode.Stretch)
        {
            switch (scalingMode)
            {
                case AnchorPrefabSpawner.ScalingMode.UniformScaling:
                    var smallestAxisScale = Mathf.Min(localScale.x > localScale.y ? localScale.y : localScale.x,
                        localScale.z);
                    localScale.x = localScale.y = localScale.z = smallestAxisScale;
                    return localScale;
                case AnchorPrefabSpawner.ScalingMode.UniformXZScale:
                    localScale.x = localScale.z = Mathf.Min(localScale.x, localScale.z);
                    return localScale;
                case AnchorPrefabSpawner.ScalingMode.NoScaling:
                    return Vector3.one;
                case AnchorPrefabSpawner.ScalingMode.Stretch:
                    return localScale;
                case AnchorPrefabSpawner.ScalingMode.Custom:
                    throw new ArgumentException(
                        "A custom scaling method was selected but no implementation was provided. " +
                        "To customize the scaling logic either extend the AnchorPrefabSpawner class or use the default" +
                        "scaling mode and modify the prefab's local scale afterwards.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(scalingMode), scalingMode,
                        "The ScalingMode used is not defined");
            }
        }

        /// <summary>
        ///     Aligns the pivot points of a prefab and of the anchors' volumebased on the specified alignment mode.
        /// </summary>
        /// <param name="anchorVolumeBounds">The volume bounds of the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <param name="alignMode">The alignment mode to use. See <see cref="AnchorPrefabSpawner.AlignMode" />.</param>
        /// <returns>The pivot points of the prefab and of the anchor's volume.</returns>
        /// <remarks>
        ///     This method aligns the pivots of a prefab and of an anchor based on the specified alignment mode.
        ///     The pivot calculations will impact where the prefab will be instantiated in the scene.
        ///     If the alignment mode is Automatic or Bottom, it aligns the pivot at the bottom center of the prefab.
        ///     If the alignment mode is Center, it aligns the pivot at the center of the prefab.
        ///     If the alignment mode is Custom, it uses the provided custom alignment logic.
        ///     It then scales the pivot point of the prefab and returns the pivot points of the prefab and the anchor.
        /// </remarks>
        public static Vector3 AlignPrefabPivot(
            Bounds anchorVolumeBounds, Bounds? prefabBounds, Vector3 localScale,
            AnchorPrefabSpawner.AlignMode alignMode = AnchorPrefabSpawner.AlignMode.Automatic)
        {
            var pivots = (prefabPivot: new Vector3(), anchorVolumePivot: new Vector3());
            switch (alignMode)
            {
                case AnchorPrefabSpawner.AlignMode.Automatic:
                case AnchorPrefabSpawner.AlignMode.Bottom:
                    if (prefabBounds.HasValue)
                    {
                        var center = prefabBounds.Value.center;
                        var min = prefabBounds.Value.min;
                        pivots.prefabPivot = new Vector3(-center.x, center.z, min.y);
                    }

                    pivots.anchorVolumePivot = anchorVolumeBounds.center;
                    pivots.anchorVolumePivot.z = anchorVolumeBounds.min.z;
                    break;
                case AnchorPrefabSpawner.AlignMode.Center:
                    if (prefabBounds.HasValue)
                    {
                        var center = prefabBounds.Value.center;
                        pivots.prefabPivot = new Vector3(-center.x, center.z, center.y);
                    }

                    pivots.anchorVolumePivot = anchorVolumeBounds.center;
                    break;
                case AnchorPrefabSpawner.AlignMode.NoAlignment:
                    break;
                case AnchorPrefabSpawner.AlignMode.Custom:
                    throw new ArgumentException(
                        "A custom volume alignment method was selected but no implementation was provided. " +
                        "To customize the alignment logic either extend the AnchorPrefabSpawner class or use the default " +
                        "alignment mode and modify the prefab's local position afterwards.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignMode), alignMode,
                        "The AlignMode used is not defined");
            }

            pivots.prefabPivot.x *= localScale.x;
            pivots.prefabPivot.y *= localScale.z;
            pivots.prefabPivot.z *= localScale.y;
            var localPosition = pivots.anchorVolumePivot - pivots.prefabPivot;
            return localPosition;
        }

        /// <summary>
        ///     Selects a prefab from a list that has the closest size to the volume of an anchor.
        /// </summary>
        /// <param name="anchor">The anchor to compare sizes with.</param>
        /// <param name="prefabList">The list of prefabs to select from.</param>
        /// <param name="sizeMatchingPrefab">The selected prefab with the closest size to the anchor's volume.</param>
        /// <returns>True if a matching prefab is found, false otherwise.</returns>
        /// <remarks>
        ///     This method selects a prefab from a list that has the closest size to the volume of an anchor.
        ///     It first checks if the anchor has a volume. If not, it throws an exception.
        ///     It then calculates the volume of the anchor and each prefab in the list.
        ///     It selects the prefab with the smallest difference in size to the anchor's volume.
        /// </remarks>
        public static bool GetPrefabWithClosestSizeToAnchor(MRUKAnchor anchor, List<GameObject> prefabList,
            out GameObject sizeMatchingPrefab)
        {
            sizeMatchingPrefab = null;
            if (!anchor.VolumeBounds.HasValue)
            {
                throw new InvalidOperationException(
                    "Cannot match a prefab with the closest size to this anchor as the latter has no volume");
            }

            var anchorVolume = anchor.VolumeBounds.Value.size.x * anchor.VolumeBounds.Value.size.y *
                               anchor.VolumeBounds.Value.size.z;
            var anchorAverageSide = MathF.Pow(anchorVolume, 1f / 3.0f); // cubic root
            var closestSizeDifference = Mathf.Infinity;
            foreach (var prefab in prefabList)
            {
                var bounds = Utilities.GetPrefabBounds(prefab);
                if (!bounds.HasValue)
                {
                    continue;
                }

                var prefabVolume = bounds.Value.size.x * bounds.Value.size.y * bounds.Value.size.z;
                var prefabAverageSide = Mathf.Pow(prefabVolume, 1.0f / 3.0f); // cubic root
                var sizeDifference = Mathf.Abs(anchorAverageSide - prefabAverageSide);
                if (sizeDifference >= closestSizeDifference)
                {
                    continue;
                }

                closestSizeDifference = sizeDifference;
                sizeMatchingPrefab = prefab;
                return true;
            }

            return false;
        }

        #endregion VolumeUtilities

        // Utilities to use when dealing with anchors that have valid plane rects (e.g.: walls, ceiling, floor, etc.)

        #region PlaneRectUtilities

        /// <summary>
        ///     Calculates a transformation matrix that matches the plane rectangle of the anchor.
        /// </summary>
        /// <param name="anchorInfo">Information about the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="scaling">The scaling mode to use. See <see cref="AnchorPrefabSpawner.ScalingMode" />.</param>
        /// <param name="alignment">The alignment mode to use. See <see cref="AnchorPrefabSpawner.AlignMode" />.</param>
        /// <returns>A transformation matrix that matches the plane rectangle of the anchor.</returns>
        /// <remarks>
        ///     This method calculates and returns a transformation matrix for a prefab that matches the plane rect of
        ///     an anchor, by determining the local scale and pose of the prefab based on the rect.
        /// </remarks>
        public static Matrix4x4 GetTransformationMatrixMatchingAnchorPlaneRect(MRUKAnchor anchorInfo,
            Bounds? prefabBounds,
            AnchorPrefabSpawner.ScalingMode scaling = AnchorPrefabSpawner.ScalingMode.Stretch,
            AnchorPrefabSpawner.AlignMode alignment = AnchorPrefabSpawner.AlignMode.Automatic)

        {
            var localScale = GetPrefabScaleBasedOnAnchorPlaneRect(anchorInfo, prefabBounds, scaling);
            var prefabPose =
                GetPoseBasedOnAnchorPlaneRect(anchorInfo, alignment, prefabBounds, localScale);
            return Matrix4x4.TRS(prefabPose.position, prefabPose.rotation, localScale);
        }

        /// <summary>
        ///     Selects a prefab based on the specified selection mode.
        /// </summary>
        /// <param name="anchor">The anchor to use for selection.</param>
        /// <param name="prefabSelectionMode">The selection mode to use. See <see cref="AnchorPrefabSpawner.SelectionMode" />.</param>
        /// <param name="prefabs">The list of prefabs to select from.</param>
        /// <param name="random">The random generator used to generate the index of the prefab to be selected</param>
        /// <returns>True if a prefab was selected, false otherwise.</returns>
        /// <remarks>
        ///     This method selects a prefab based on the specified selection mode.
        ///     If the selection mode is Random, it selects a random prefab from the list.
        ///     If the selection mode is ClosestSize, it selects the prefab with the closest size to the anchor.
        ///     If the selection mode is Custom, it uses the provided custom selection logic.
        /// </remarks>
        public static GameObject SelectPrefab(MRUKAnchor anchor, AnchorPrefabSpawner.SelectionMode prefabSelectionMode,
            List<GameObject> prefabs, System.Random random)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                return null;
            }

            GameObject selectedPrefab = null;
            switch (prefabSelectionMode)
            {
                case AnchorPrefabSpawner.SelectionMode.Random:
                    if (random == null)
                    {
                        throw new InvalidOperationException("When setting the SelectionMode to random, " +
                                                            "make sure to call AnchorPrefabSpawnerUtilities.InitializeRandom(seed)");
                    }

                    var index = random.Next(0, prefabs.Count);
                    selectedPrefab = prefabs[index];
                    break;
                case AnchorPrefabSpawner.SelectionMode.ClosestSize:
                    GetPrefabWithClosestSizeToAnchor(anchor, prefabs, out selectedPrefab);
                    break;
                case AnchorPrefabSpawner.SelectionMode.Custom:
                    throw new ArgumentException(
                        "A custom prefab selection method was selected but no implementation was provided. " +
                        "To customize the selection logic extend the AnchorPrefabSpawner class.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(prefabSelectionMode), prefabSelectionMode,
                        "The SelectionMode used is not defined");
            }

            return selectedPrefab;
        }

        /// <summary>
        ///     Aligns the pivots of a prefab and of the anchor's plane rect, based on the specified alignment mode.
        /// </summary>
        /// <param name="planeRect">The plane rectangle of the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <param name="alignMode">The alignment mode to use. See <see cref="AnchorPrefabSpawner.AlignMode" />.</param>
        /// <returns>The pivot points of the prefab and the anchor plane rectangle.</returns>
        /// <remarks>
        ///     This method aligns the pivots of a prefab and of an anchor based on the specified alignment mode.
        ///     The pivot calculations will impact where the prefab will be instantiated in the scene.
        ///     If the alignment mode is Automatic or Center, it aligns the pivot at the center of the prefab.
        ///     If the alignment mode is Bottom, it aligns the pivot at the bottom of the prefab.
        ///     If the alignment mode is Custom, it uses the provided custom alignment logic.
        ///     It then scales the pivot point of the prefab and returns the pivot points of the prefab and the anchor plane rectangle.
        /// </remarks>
        public static Vector3 AlignPrefabPivot(
            Rect planeRect, Bounds? prefabBounds, Vector2 localScale,
            AnchorPrefabSpawner.AlignMode alignMode = AnchorPrefabSpawner.AlignMode.Automatic)
        {
            var pivots = (prefabPivot: new Vector3(), anchorPlaneRectPivot: new Vector3());

            switch (alignMode)
            {
                case AnchorPrefabSpawner.AlignMode.Automatic:
                case AnchorPrefabSpawner.AlignMode.Center:
                    pivots.prefabPivot = prefabBounds?.center ?? Vector3.zero;
                    pivots.anchorPlaneRectPivot = planeRect.center;
                    break;
                case AnchorPrefabSpawner.AlignMode.Bottom:
                    if (prefabBounds.HasValue)
                    {
                        var center = prefabBounds.Value.center;
                        var min = prefabBounds.Value.min;
                        pivots.prefabPivot = new Vector3(center.x, min.y);
                    }

                    pivots.anchorPlaneRectPivot = planeRect.center;
                    pivots.anchorPlaneRectPivot.y = planeRect.min.y;
                    break;
                case AnchorPrefabSpawner.AlignMode.NoAlignment:
                    break;
                case AnchorPrefabSpawner.AlignMode.Custom:
                    throw new ArgumentException(
                        "A custom volume alignment method was selected but no implementation was provided." +
                        "To customize the alignment logic either extend the AnchorPrefabSpawner class or use the default" +
                        "alignment mode and modify the prefab's local position afterwards.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignMode), alignMode,
                        "The AlignMode used is not defined");
            }

            pivots.prefabPivot.Scale(localScale);
            var localPosition = new Vector3(pivots.anchorPlaneRectPivot.x - pivots.prefabPivot.x,
                pivots.anchorPlaneRectPivot.y - pivots.prefabPivot.y, 0);
            return localPosition;
        }

        /// <summary>
        ///     Scales a prefab based on the specified scaling mode.
        /// </summary>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <param name="scalingMode">The scaling mode to use. See <see cref="AnchorPrefabSpawner.ScalingMode" />.</param>
        /// <returns>The scaled local scale of the prefab.</returns>
        /// <remarks>
        ///     This method is used to scale a prefab when instantiating it as an anchor with a PlaneRect.
        ///     If the scaling mode is Stretch, it returns the original local scale.
        ///     If the scaling mode is UniformScaling or UniformXZScale, it scales the prefab uniformly.
        ///     If the scaling mode is NoScaling, it returns a unit vector.
        ///     If the scaling mode is Custom, it uses the provided custom scaling logic.
        /// </remarks>
        public static Vector3 ScalePrefab(Vector2 localScale,
            AnchorPrefabSpawner.ScalingMode scalingMode = AnchorPrefabSpawner.ScalingMode.Stretch)
        {
            switch (scalingMode)
            {
                case AnchorPrefabSpawner.ScalingMode.Stretch:
                    break;
                case AnchorPrefabSpawner.ScalingMode.UniformScaling:
                case AnchorPrefabSpawner.ScalingMode.UniformXZScale:
                    localScale.x = localScale.y = Mathf.Min(localScale.x, localScale.y);
                    break;
                case AnchorPrefabSpawner.ScalingMode.NoScaling:
                    localScale = Vector2.one;
                    break;
                case AnchorPrefabSpawner.ScalingMode.Custom:
                    throw new ArgumentException(
                        "A custom scaling method was selected but no implementation was provided. " +
                        "To customize the scaling logic either extend the AnchorPrefabSpawner class or use the default" +
                        "scaling mode and modify the prefab's local scale afterwards.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(scalingMode), scalingMode, null);
            }

            return new Vector3(localScale.x, localScale.y, 0.5f * (localScale.x + localScale.y));
        }

        #endregion PlaneRectUtilities

        #region Helpers

        /// <summary>
        ///     Calculates the local scale of a prefab based on the volume of an anchor.
        /// </summary>
        /// <param name="anchorInfo">Information about the anchor.</param>
        /// <param name="matchAspectRatio">Whether to match the aspect ratio of the anchor.</param>
        /// <param name="calculateFacingDirection">Whether to calculate the facing direction of the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="cardinalAxisIndex">The index of the cardinal axis.</param>
        /// <param name="scaling">The scaling mode to use. See <see cref="AnchorPrefabSpawner.ScalingMode" />.</param>
        /// <returns>The local scale of the prefab.</returns>
        /// <remarks>
        ///     This method calculates the local scale of a prefab based on the anchor's volume,
        ///     adjusting for aspect ratio if necessary, and applies the scaling mode before returning the local scale.
        /// </remarks>
        internal static Vector3 GetPrefabScaleBasedOnAnchorVolume(MRUKAnchor anchorInfo, bool matchAspectRatio,
            bool calculateFacingDirection, Bounds? prefabBounds, out int cardinalAxisIndex,
            AnchorPrefabSpawner.ScalingMode scaling = AnchorPrefabSpawner.ScalingMode.Stretch)
        {
            cardinalAxisIndex = 0;
            if (!anchorInfo.VolumeBounds.HasValue)
            {
                throw new InvalidOperationException(
                    "The prefab's pose can't be calculated when the anchor's volume bounds are null." +
                    "Consider using GetPrefabScaleBasedOnAnchorPlaneRect in case the anchor has a volume.");
            }

            var prefabSize = prefabBounds?.size ?? Vector3.one;
            cardinalAxisIndex = 0;
            if (calculateFacingDirection && !matchAspectRatio)
            {
                anchorInfo.Room.GetDirectionAwayFromClosestWall(anchorInfo, out cardinalAxisIndex);
            }

            var volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value, cardinalAxisIndex);

            var volumeSize = volumeBounds.size;
            var localScale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y,
                volumeSize.y / prefabSize.z); // flipped z and y to correct orientation
            if (matchAspectRatio)
            {
                MatchAspectRatio(anchorInfo, calculateFacingDirection, prefabSize, volumeSize, ref cardinalAxisIndex,
                    ref volumeBounds, ref localScale);
            }

            localScale = ScalePrefab(localScale, scaling);
            return localScale;
        }

        /// <summary>
        ///     Calculates the pose of a prefab based on the volume of an anchor.
        /// </summary>
        /// <param name="anchorInfo">Information about the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="cardinalAxisIndex">The index of the cardinal axis.</param>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <param name="alignment">The alignment mode to use. See <see cref="AnchorPrefabSpawner.AlignMode" />.</param>
        /// <returns>The pose of the prefab.</returns>
        /// <remarks>
        ///     This method calculates and returns the pose of a prefab based on the anchor's volume, after determining
        ///     the volume bounds of the anchor and the pivot points of the prefab and the anchor.
        /// </remarks>
        internal static Pose GetPoseBasedOnAnchorVolume(MRUKAnchor anchorInfo, Bounds? prefabBounds,
            int cardinalAxisIndex, Vector3 localScale,
            AnchorPrefabSpawner.AlignMode alignment = AnchorPrefabSpawner.AlignMode.Automatic)
        {
            if (!anchorInfo.VolumeBounds.HasValue)
            {
                throw new InvalidOperationException(
                    "The prefab's pose can't be calculated when the anchor's volume bounds are null." +
                    " Consider using GetPoseBasedOnAnchorPlaneRect in case the anchor has a plane rect.");
            }

            var volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value, cardinalAxisIndex);
            var localPosition = AlignPrefabPivot(volumeBounds, prefabBounds, localScale, alignment);
            // scene geometry is unusual, we need to swap Y/Z for a more standard prefab structure
            var localRotation = Quaternion.Euler((cardinalAxisIndex - 1) * 90, -90, -90);
            return new Pose(localPosition, localRotation);
        }

        /// <summary>
        ///     Matches the aspect ratio of a prefab to the volume of an anchor.
        /// </summary>
        /// <param name="anchorInfo">Information about the anchor.</param>
        /// <param name="calculateFacingDirection">Whether to calculate the facing direction of the anchor.</param>
        /// <param name="prefabSize">The size of the prefab.</param>
        /// <param name="volumeSize">The size of the volume.</param>
        /// <param name="cardinalAxisIndex">The index of the cardinal axis.</param>
        /// <param name="volumeBounds">The volume bounds of the anchor.</param>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <remarks>
        ///     This method adjusts the aspect ratio of a prefab to match the volume of an anchor.
        ///     It calculates the distortion of both, rotates the prefab if necessary, and updates the volume bounds and local
        ///     scale based on the cardinal axis index and the need to calculate the facing direction.
        /// </remarks>
        internal static void MatchAspectRatio(MRUKAnchor anchorInfo, bool calculateFacingDirection, Vector3 prefabSize,
            Vector3 volumeSize, ref int cardinalAxisIndex, ref Bounds volumeBounds, ref Vector3 localScale)
        {
            var prefabSizeRotated = new Vector3(prefabSize.z, prefabSize.y, prefabSize.x);
            var scaleRotated = new Vector3(volumeSize.x / prefabSizeRotated.x,
                volumeSize.z / prefabSizeRotated.y, volumeSize.y / prefabSizeRotated.z);

            var distortion = Mathf.Max(localScale.x, localScale.z) / Mathf.Min(localScale.x, localScale.z);
            var distortionRotated = Mathf.Max(scaleRotated.x, scaleRotated.z) /
                                    Mathf.Min(scaleRotated.x, scaleRotated.z);

            var rotateToMatchAspectRatio = distortion > distortionRotated;
            if (rotateToMatchAspectRatio)
            {
                cardinalAxisIndex = 1;
            }

            if (calculateFacingDirection)
            {
                anchorInfo.Room.GetDirectionAwayFromClosestWall(anchorInfo, out cardinalAxisIndex,
                    rotateToMatchAspectRatio ? new List<int> { 0, 2 } : new List<int> { 1, 3 });
            }

            if (cardinalAxisIndex == 0 || !anchorInfo.VolumeBounds.HasValue)
            {
                return; // no need to update the volume bounds
            }

            // Update the volume bounds
            volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value, cardinalAxisIndex);
            volumeSize = volumeBounds.size;
            localScale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y,
                volumeSize.y / prefabSize.z); // flipped z and y to correct orientation
        }


        /// <summary>
        ///     Rotates the volume bounds based on the specified rotation.
        /// </summary>
        /// <param name="bounds">The volume bounds to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The rotated volume bounds.</returns>
        /// <remarks>
        ///     This method rotates the volume bounds based on the specified rotation.
        ///     If the rotation is 1, it rotates the bounds 90 degrees counterclockwise.
        ///     If the rotation is 2, it rotates the bounds 180 degrees.
        ///     If the rotation is 3, it rotates the bounds 90 degrees clockwise.
        ///     With any other rotation it returns the original bounds.
        /// </remarks>
        internal static Bounds RotateVolumeBounds(Bounds bounds, int rotation)
        {
            var center = bounds.center;
            var size = bounds.size;
            return rotation switch
            {
                1 => new Bounds(new Vector3(-center.y, center.x, center.z), new Vector3(size.y, size.x, size.z)),
                2 => new Bounds(new Vector3(-center.x, -center.x, center.z), size),
                3 => new Bounds(new Vector3(center.y, -center.x, center.z), new Vector3(size.y, size.x, size.z)),
                _ => bounds
            };
        }

        /// <summary>
        ///     Calculates the local scale of a prefab based on the plane rectangle of an anchor.
        /// </summary>
        /// <param name="anchorInfo">Information about the anchor.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="scalingMode">The scaling mode to use. See <see cref="AnchorPrefabSpawner.ScalingMode" />.</param>
        /// <returns>The local scale of the prefab.</returns>
        /// <remarks>
        ///     This method calculates and returns the local scale of a prefab based on the anchor's plane rect,
        ///     applying the scaling mode.
        /// </remarks>
        internal static Vector3 GetPrefabScaleBasedOnAnchorPlaneRect(MRUKAnchor anchorInfo, Bounds? prefabBounds,
            AnchorPrefabSpawner.ScalingMode scalingMode = AnchorPrefabSpawner.ScalingMode.Stretch)
        {
            if (!anchorInfo.PlaneRect.HasValue)
            {
                throw new InvalidOperationException(
                    "The prefab's pose can't be calculated when the anchor's plane rect is null." +
                    " Consider using GetPrefabScaleBasedOnAnchorVolume in case the anchor has a volume.");
            }

            var prefabSize = prefabBounds?.size ?? Vector3.one;
            var planeSize = anchorInfo.PlaneRect.Value.size;
            var scale = new Vector2(planeSize.x / prefabSize.x, planeSize.y / prefabSize.y);

            scale = ScalePrefab(scale, scalingMode);

            return scale;
        }

        /// <summary>
        ///     Calculates the pose of a prefab based on the plane rect of an anchor.
        /// </summary>
        /// <param name="anchorInfo">Information about the anchor.</param>
        /// <param name="alignmentMode">The alignment mode to use. See <see cref="AnchorPrefabSpawner.AlignMode" />.</param>
        /// <param name="prefabBounds">The bounds of the prefab.</param>
        /// <param name="localScale">The local scale of the prefab.</param>
        /// <returns>The pose of the prefab.</returns>
        /// <remarks>
        ///     This method calculates and returns the pose of a prefab based on the anchor's plane rect,
        ///     determining the pivot points and local position and rotation .
        /// </remarks>
        internal static Pose GetPoseBasedOnAnchorPlaneRect(MRUKAnchor anchorInfo,
            AnchorPrefabSpawner.AlignMode alignmentMode, Bounds? prefabBounds, Vector2 localScale)
        {
            if (!anchorInfo.PlaneRect.HasValue)
            {
                throw new InvalidOperationException(
                    "The prefab's pose can't be calculated when the anchor's plane rect is null." +
                    " Consider using GetPoseBasedOnAnchorVolume in case the anchor has a volume.");
            }

            var localPosition =
                AlignPrefabPivot(anchorInfo.PlaneRect.Value, prefabBounds, localScale, alignmentMode);
            var localRotation = Quaternion.identity;
            return new Pose(localPosition, localRotation);
        }

        #endregion Helpers
    }
}
