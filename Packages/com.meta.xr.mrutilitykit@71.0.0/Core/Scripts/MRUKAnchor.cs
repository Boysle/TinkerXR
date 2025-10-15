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
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Represents an anchor within MR Utility Kit, providing spatial and semantic information.
    /// </summary>
    [Feature(Feature.Scene)]
    public class MRUKAnchor : MonoBehaviour
    {
        /// <summary>
        /// Flags enumeration for scene labels, indicating the type of object represented by an anchor and its classification.
        /// </summary>
        [Flags]
        public enum SceneLabels
        {
            FLOOR = 1 << OVRSemanticLabels.Classification.Floor,
            CEILING = 1 << OVRSemanticLabels.Classification.Ceiling,
            WALL_FACE = 1 << OVRSemanticLabels.Classification.WallFace,
            TABLE = 1 << OVRSemanticLabels.Classification.Table,
            COUCH = 1 << OVRSemanticLabels.Classification.Couch,
            DOOR_FRAME = 1 << OVRSemanticLabels.Classification.DoorFrame,
            WINDOW_FRAME = 1 << OVRSemanticLabels.Classification.WindowFrame,
            OTHER = 1 << OVRSemanticLabels.Classification.Other,
            STORAGE = 1 << OVRSemanticLabels.Classification.Storage,
            BED = 1 << OVRSemanticLabels.Classification.Bed,
            SCREEN = 1 << OVRSemanticLabels.Classification.Screen,
            LAMP = 1 << OVRSemanticLabels.Classification.Lamp,
            PLANT = 1 << OVRSemanticLabels.Classification.Plant,
            WALL_ART = 1 << OVRSemanticLabels.Classification.WallArt,
            GLOBAL_MESH = 1 << OVRSemanticLabels.Classification.SceneMesh,
            INVISIBLE_WALL_FACE = 1 << OVRSemanticLabels.Classification.InvisibleWallFace,
        };

        /// <summary>
        /// Flags enumeration for component type, scene anchors can either have plane or volume components associated with them or both.
        /// </summary>
        [Flags]
        public enum ComponentType
        {
            None = 0,
            Plane = 1 << 0,
            Volume = 1 << 1,
            All = Plane | Volume,
        }

        /// <see cref="OVRSemanticLabels.DeprecationMessage" />
        [Obsolete("Use '" + nameof(Label) + "' instead.")]
        public List<string> AnchorLabels => Utilities.SceneLabelsEnumToList(Label);

        /// <summary>
        /// The scene label categorizing the anchor.
        /// </summary>
        public SceneLabels Label
        {
            get;
            internal set;
        }

        /// <summary>
        /// Optional rectangular bounds on a plane associated with the anchor.
        /// </summary>
        public Rect? PlaneRect;

        /// <summary>
        /// Optional volumetric bounds associated with the anchor.
        /// </summary>
        public Bounds? VolumeBounds;

        /// <summary>
        /// A list of local-space points defining the boundary of the plane associated with the anchor.
        /// </summary>
        public List<Vector2> PlaneBoundary2D;

        /// <summary>
        /// Reference to the scene anchor associated with this anchor. You should not need to use this directly, only
        /// use this if you know what you are doing.
        /// </summary>
        public OVRAnchor Anchor = OVRAnchor.Null;

        /// <summary>
        /// Reference to the parent room object. This is set during construction.
        /// </summary>
        public MRUKRoom Room;

        /// <summary>
        /// References to child anchors, populated via <see cref="MRUKRoom.CalculateHierarchyReferences"/>.
        /// </summary>
        [NonSerialized] public MRUKAnchor ParentAnchor;

        /// <summary>
        /// A list of child anchors associated with this anchor.
        /// </summary>
        public List<MRUKAnchor> ChildAnchors = new List<MRUKAnchor>();

        [Obsolete("Use PlaneRect.HasValue instead.")]
        public bool HasPlane => PlaneRect != null; //!< Use PlaneRect.HasValue instead.

        [Obsolete("Use VolumeBounds.HasValue instead.")]
        public bool HasVolume => VolumeBounds != null; //!< Use VolumeBounds.HasValue instead.

        [Obsolete("Use HasValidHandle instead.")]
        public bool IsLocal => HasValidHandle; //!< Use HasValidHandle instead.

        /// <summary>
        /// An anchor will have a valid handle if it was loaded from device and there is a system level anchor backing it.
        /// Anchors loaded from JSON or Prefabs do not have a valid handle. You should not need to use this directly, only
        /// use this if you know what you are doing. The behaviour may change in the future.
        /// </summary>
        public bool HasValidHandle => Anchor.Handle != 0;

        /// <summary>
        /// The triangle mesh which covers the entire space, associated to the global mesh anchor.
        /// </summary>
        public Mesh GlobalMesh
        {
            get
            {
                if (!_globalMesh)
                {
                    _globalMesh = LoadGlobalMeshTriangles();
                }

                return _globalMesh;
            }
            set => _globalMesh = value;
        }

        Mesh _globalMesh;

        /// <summary>
        /// Performs a raycast against the anchor's plane and volume to determine if and where a ray intersects.
        /// This method avoids using colliders and Physics.Raycast for several reasons:
        /// <list type="bullet">
        /// <item>
        /// <description>It requires tags/layers to filter out Scene API object hits from general raycasts, which can be intrusive to a developer's pipeline by altering their project settings.</description>
        /// </item>
        /// <item>
        /// <description>It necessitates specific Plane and Volume prefabs that MRUK instantiates with colliders as children.</description>
        /// </item>
        /// <item>
        /// <description>Using Physics.Raycast is considered overkill since the locations of all Scene API primitives are already known; thus, there's no need to raycast everywhere to find them.</description>
        /// </item>
        /// </list>
        /// Instead, this method uses Plane.Raycast and other custom methods to determine if the ray has hit the surface of the object.
        /// </summary>
        /// <param name="ray">The ray to test for intersections.</param>
        /// <param name="maxDist">The maximum distance the ray should check for intersections.</param>
        /// <param name="hitInfo">Output parameter that will contain the hit information if an intersection occurs.</param>
        /// <param name="componentTypes">The type of components to ray cast against.</param>
        /// <returns>True if the ray hits either the plane or the volume, false otherwise.</returns>
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hitInfo, ComponentType componentTypes = ComponentType.All)
        {
            var localOrigin = transform.InverseTransformPoint(ray.origin);
            var localDirection = transform.InverseTransformDirection(ray.direction);
            Ray localRay = new Ray(localOrigin, localDirection);
            RaycastHit hitInfoPlane = default;
            RaycastHit hitInfoVolume = default;
            bool hitPlane = (componentTypes & ComponentType.Plane) != 0 && RaycastPlane(localRay, maxDist, out hitInfoPlane);
            bool hitVolume = (componentTypes & ComponentType.Volume) != 0 && RaycastVolume(localRay, maxDist, out hitInfoVolume);
            if (hitPlane && hitVolume)
            {
                // If the ray hit both the plane and the volume then pick whichever is closest
                if (hitInfoPlane.distance < hitInfoVolume.distance)
                {
                    hitInfo = hitInfoPlane;
                    return true;
                }
                else
                {
                    hitInfo = hitInfoVolume;
                    return true;
                }
            }

            if (hitPlane)
            {
                hitInfo = hitInfoPlane;
                return true;
            }

            if (hitVolume)
            {
                hitInfo = hitInfoVolume;
                return true;
            }

            hitInfo = new();
            return false;
        }

        /// <summary>
        /// Determines if a given position is within the boundary of the plane associated with the anchor.
        /// </summary>
        /// <param name="position">The local space point to check.</param>
        /// <returns>True if the position is within the boundary, false otherwise.</returns>
        public bool IsPositionInBoundary(Vector2 position)
        {
            if (PlaneBoundary2D == null || PlaneBoundary2D.Count == 0)
            {
                return false;
            }

            return Utilities.IsPositionInPolygon(position, PlaneBoundary2D);
        }

        /// <summary>
        /// Adds a reference to a child anchor.
        /// </summary>
        /// <param name="childObj">The child anchor to add.</param>
        public void AddChildReference(MRUKAnchor childObj)
        {
            if (childObj != null)
            {
                ChildAnchors.Add(childObj);
            }
        }

        /// <summary>
        /// Clears all references to child anchors.
        /// </summary>
        public void ClearChildReferences()
        {
            ChildAnchors.Clear();
        }

        /// <summary>
        /// Calculates the distance from a specified position to the closest surface of the anchor.
        /// </summary>
        /// <param name="position">The position from which to measure.</param>
        /// <param name="componentTypes">The component types to include.</param>
        /// <returns>The distance to the closest surface.</returns>
        public float GetDistanceToSurface(Vector3 position, ComponentType componentTypes = ComponentType.All) =>
            GetClosestSurfacePosition(position, out _, out _, componentTypes);

        /// <summary>
        /// Finds the closest surface position from a given point and returns the distance to it.
        /// </summary>
        /// <param name="testPosition">The position to test.</param>
        /// <param name="closestPosition">The closest position on the surface.</param>
        /// <param name="componentTypes">The component types to include.</param>
        /// <returns>The distance to the closest surface position.</returns>
        public float GetClosestSurfacePosition(Vector3 testPosition, out Vector3 closestPosition, ComponentType componentTypes = ComponentType.All) =>
            GetClosestSurfacePosition(testPosition, out closestPosition, out _, componentTypes);

        /// <summary>
        /// Finds the closest surface position from a given point, providing the position, normal at that position, and the distance.
        /// If the point is inside a volume, it will return a negative distance representing the distance beneath the surface.
        /// </summary>
        /// <param name="testPosition">The position to test.</param>
        /// <param name="closestPosition">The closest position on the surface.</param>
        /// <param name="normal">The normal at the closest position.</param>
        /// <param name="componentTypes">The component types to include.</param>
        /// <returns>The distance to the closest surface position.</returns>
        public float GetClosestSurfacePosition(Vector3 testPosition, out Vector3 closestPosition, out Vector3 normal, ComponentType componentTypes = ComponentType.All)
        {
            float candidateDistance = Mathf.Infinity;
            closestPosition = Vector3.zero;
            normal = Vector3.zero;

            if ((componentTypes & ComponentType.Volume) != 0 && VolumeBounds.HasValue)
            {
                var volumeBounds = VolumeBounds.Value;
                Vector3 localPosition = transform.InverseTransformPoint(testPosition);
                if (volumeBounds.Contains(localPosition))
                {
                    // in this case, we need custom math not provided by Bounds.ClosestPoint, which returns the original query position if inside
                    localPosition -= volumeBounds.center;
                    float minDist = float.MaxValue;
                    int minDistAxis = -1;
                    for (int i = 0; i < 3; i++)
                    {
                        float dist = volumeBounds.extents[i] - Mathf.Abs(localPosition[i]);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            minDistAxis = i;
                        }
                    }

                    float sign = Mathf.Sign(localPosition[minDistAxis]);
                    localPosition[minDistAxis] += minDist * sign;
                    candidateDistance = -minDist;
                    var localNormal = new Vector3
                    {
                        [minDistAxis] = sign
                    };
                    normal = transform.TransformDirection(localNormal);
                    closestPosition = transform.TransformPoint(localPosition + volumeBounds.center);
                }
                else
                {
                    closestPosition = volumeBounds.ClosestPoint(localPosition);
                    var delta = localPosition - closestPosition;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
                    {
                        normal = new Vector3(Mathf.Sign(delta.x), 0f, 0f);
                    }
                    else if (Mathf.Abs(delta.y) > Mathf.Abs(delta.z))
                    {
                        normal = new Vector3(0f, Mathf.Sign(delta.y), 0f);
                    }
                    else
                    {
                        normal = new Vector3(0f, 0f, Mathf.Sign(delta.z));
                    }
                    closestPosition = transform.TransformPoint(closestPosition);
                    normal = transform.TransformDirection(normal);
                    candidateDistance = Vector3.Distance(closestPosition, testPosition);
                }
            }
            else if ((componentTypes & ComponentType.Plane) != 0 && PlaneRect.HasValue)
            {
                var planeRect = PlaneRect.Value;
                Vector3 localPosition = transform.InverseTransformPoint(testPosition);
                localPosition.z = 0;

                if (localPosition.x > planeRect.max.x)
                {
                    localPosition.x = planeRect.max.x;
                }
                else if (localPosition.x < planeRect.min.x)
                {
                    localPosition.x = planeRect.min.x;
                }

                if (localPosition.y > planeRect.max.y)
                {
                    localPosition.y = planeRect.max.y;
                }
                else if (localPosition.y < planeRect.min.y)
                {
                    localPosition.y = planeRect.min.y;
                }

                closestPosition = transform.TransformPoint(localPosition);
                candidateDistance = Vector3.Distance(closestPosition, testPosition);
                normal = transform.forward;
            }

            return candidateDistance;
        }

        /// <summary>
        /// Gets the center position of the anchor. If volume bounds are defined, it returns the center of the volume; otherwise, it returns the anchor's position.
        /// </summary>
        /// <returns>The center position of the anchor.</returns>
        public Vector3 GetAnchorCenter()
        {
            if (VolumeBounds.HasValue)
            {
                return transform.TransformPoint(VolumeBounds.Value.center);
            }

            return transform.position;
        }

        /// <summary>
        /// A convenience function to get a transform-friendly Vector3 size of an anchor.
        /// If you'd like the size of the quad or volume instead, use <seealso cref="MRUKAnchor.PlaneRect" /> or <seealso cref="MRUKAnchor.VolumeBounds" />
        /// </summary>
        /// <returns>The size of the anchor</returns>
        [Obsolete("Use PlaneRect and VolumeBounds properties instead")]
        public Vector3 GetAnchorSize()
        {
            Vector3 returnSize = Vector3.one;

            if (HasPlane)
            {
                returnSize = new Vector3(PlaneRect.Value.size.x, PlaneRect.Value.size.y, 1);
            }

            // prioritize the volume's size, since that is likely the desired value
            if (HasVolume)
            {
                returnSize = VolumeBounds.Value.size;
            }

            return returnSize;
        }

        bool RaycastPlane(Ray localRay, float maxDist, out RaycastHit hitInfo)
        {
            hitInfo = new RaycastHit();

            if (!PlaneRect.HasValue)
            {
                return false;
            }

            // Early rejection if surface isn't facing raycast
            if (localRay.direction.z >= 0)
            {
                return false;
            }

            Plane plane = new Plane(Vector3.forward, 0);
            if (plane.Raycast(localRay, out float entryDistance) && entryDistance < maxDist)
            {
                // A Unity Plane is infinite, so we still need to calculate if the impact point is within the dimensions
                Vector3 localImpactPos = localRay.GetPoint(entryDistance);
                if (IsPositionInBoundary(new Vector2(localImpactPos.x, localImpactPos.y)))
                {
                    // WARNING: the outgoing RaycastHit object does NOT have collider info, so don't query for it
                    // (since this raycast method doesn't involve colliders
                    hitInfo.point = transform.TransformPoint(localImpactPos);
                    hitInfo.normal = transform.forward;
                    hitInfo.distance = entryDistance;
                    return true;
                }
            }

            return false;
        }

        bool RaycastVolume(Ray localRay, float maxDist, out RaycastHit hitInfo)
        {
            // Use the slab method to determine if the ray intersects with the bounding box
            // https://education.siggraph.org/static/HyperGraph/raytrace/rtinter3.htm
            hitInfo = new RaycastHit();
            if (!VolumeBounds.HasValue)
            {
                return false;
            }

            int hitAxis = 0;
            float distFar = Mathf.Infinity;
            float distNear = -Mathf.Infinity;
            Bounds volume = VolumeBounds.Value;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(localRay.direction[i]) > Mathf.Epsilon)
                {
                    float dist1 = (volume.min[i] - localRay.origin[i]) / localRay.direction[i];
                    float dist2 = (volume.max[i] - localRay.origin[i]) / localRay.direction[i];
                    if (dist1 > dist2)
                    {
                        // Swap as dist1 is the intersection with the plane
                        (dist1, dist2) = (dist2, dist1);
                    }

                    if (dist1 > distNear)
                    {
                        distNear = dist1;
                        hitAxis = i;
                    }

                    if (dist2 < distFar)
                    {
                        distFar = dist2;
                    }
                }
                else
                {
                    // The ray is parallel to the plane so no intersection
                    if (localRay.origin[i] < volume.min[i] || localRay.origin[i] > volume.max[i])
                    {
                        // No intersections
                        distNear = Mathf.Infinity;
                        break;
                    }
                }
            }

            if (distNear >= 0 && distNear <= distFar && distNear < maxDist)
            {
                Vector3 impactPos = localRay.GetPoint(distNear);
                Vector3 impactNormal = Vector3.zero;
                impactNormal[hitAxis] = localRay.direction[hitAxis] > 0 ? -1 : 1;
                // WARNING: the outgoing RaycastHit object does NOT have collider info, so don't query for it
                // (since this raycast method doesn't involve colliders
                // Transform the result back into world space
                hitInfo.point = transform.TransformPoint(impactPos);
                hitInfo.normal = transform.TransformDirection(impactNormal);
                hitInfo.distance = distNear;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the centers of the bounding box faces if a volume is defined, or the center of the plane if only a plane is defined.
        /// </summary>
        /// <returns>An array of Vector3 representing the centers of the bounding box faces or the plane center.</returns>
        public Vector3[] GetBoundsFaceCenters()
        {
            if (VolumeBounds.HasValue)
            {
                Vector3[] centers = new Vector3[6];
                Vector3 scale = VolumeBounds.Value.size;
                // anchor transform.position is at top of volume
                Vector3 cubeCenter = transform.position - transform.forward * scale.z * 0.5f;

                centers[0] = transform.position;
                centers[1] = cubeCenter - transform.forward * scale.z * 0.5f;
                centers[2] = cubeCenter + transform.right * scale.x * 0.5f;
                centers[3] = cubeCenter - transform.right * scale.x * 0.5f;
                centers[4] = cubeCenter + transform.up * scale.y * 0.5f;
                centers[5] = cubeCenter - transform.up * scale.y * 0.5f;
                return centers;
            }

            if (PlaneRect.HasValue)
            {
                Vector3[] centers = new Vector3[1];
                centers[0] = transform.position;
                return centers;
            }

            return null;
        }

        /// <summary>
        /// Tests if a given world position is inside the volume of the anchor, considering an optional distance buffer and whether to test vertical bounds.
        /// </summary>
        /// <param name="worldPosition">The world position to test.</param>
        /// <param name="testVerticalBounds">Whether to include vertical bounds in the test.</param>
        /// <param name="distanceBuffer">An optional buffer distance to expand the volume for the test.</param>
        /// <returns>True if the position is inside the volume, false otherwise.</returns>
        public bool IsPositionInVolume(Vector3 worldPosition, bool testVerticalBounds, float distanceBuffer = 0.0f)
        {
            if (!VolumeBounds.HasValue)
            {
                return false;
            }

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            var bounds = VolumeBounds.Value;
            bounds.Expand(distanceBuffer);
            if (testVerticalBounds)
            {
                return bounds.Contains(localPosition);
            }
            else
            {
                return (localPosition.x >= bounds.min.x) && (localPosition.x <= bounds.max.x)
                                                         && (localPosition.z >= bounds.min.z) && (localPosition.z <= bounds.max.z);
            }
        }

        /// <summary>
        /// Loads a mesh representing the global mesh associated with the anchor, if available.
        /// </summary>
        /// <returns>A Mesh object containing the triangles of the global mesh, or null if not available.</returns>
        public Mesh LoadGlobalMeshTriangles()
        {
            if (!HasAnyLabel(SceneLabels.GLOBAL_MESH))
            {
                return null; // for now only global mesh is supported
            }

            Anchor.TryGetComponent(out OVRTriangleMesh mesh);

            if (!mesh.TryGetCounts(out var vcount, out var tcount))
            {
                return new Mesh();
            }

            var trimesh = new Mesh
            {
                indexFormat = vcount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
            };

            using var vs = new NativeArray<Vector3>(vcount, Allocator.Temp);
            using var ts = new NativeArray<int>(tcount * 3, Allocator.Temp);

            if (!mesh.TryGetMesh(vs, ts))
            {
                return trimesh;
            }

            trimesh.SetVertices(vs);
            trimesh.SetIndices(ts, MeshTopology.Triangles, 0, true, 0);

            return trimesh;
        }

        /// <summary>
        /// Checks if the current object has a specific label. This method is obsolete.
        /// String-based labels are deprecated as of version 65. Please use the equivalent enum-based method.
        /// </summary>
        /// <param name="label">The label to check, provided as a string.</param>
        /// <returns>True if the object has the specified label, false otherwise.</returns>
        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public bool HasLabel(string label) => HasAnyLabel(Utilities.StringLabelToEnum(label));

        /// <summary>
        /// Checks if the current object has any of the specified labels. This method is obsolete.
        /// String-based labels are deprecated as of version 65. Please use the equivalent enum-based method.
        /// </summary>
        /// <param name="labels">A list of labels to check, provided as strings.</param>
        /// <returns>True if the object has any of the specified labels, false otherwise.</returns>
        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public bool HasAnyLabel(List<string> labels) => HasAnyLabel(Utilities.StringLabelsToEnum(labels));

        /// <summary>
        /// Checks if the anchor has any of the specified scene labels.
        /// </summary>
        /// <param name="labelFlags">The scene labels to check against the anchor's labels.</param>
        /// <returns>True if the anchor has any of the specified labels, false otherwise.</returns>
        public bool HasAnyLabel(SceneLabels labelFlags) => (Label & labelFlags) != 0;

        internal void UpdateAnchor(Data.AnchorData newData)
        {
            PlaneBoundary2D = newData.PlaneBoundary2D;
            PlaneRect = Utilities.GetPlaneRectFromAnchorData(newData);
            VolumeBounds = Utilities.GetVolumeBoundsFromAnchorData(newData);
            GlobalMesh = Utilities.GetGlobalMeshFromAnchorData(newData);
            Label = Utilities.StringLabelsToEnum(newData.SemanticClassifications);
        }

        /// <summary>
        /// Retrieves the labels associated with this object as an enum. This method is obsolete.
        /// Use the 'Label' property directly instead.
        /// </summary>
        /// <returns>The labels as an enum of type SceneLabels.</returns>
        /// <see cref="OVRSemanticLabels.DeprecationMessage" />
        [Obsolete("Use '" + nameof(Label) + "' instead.")]
        public SceneLabels GetLabelsAsEnum() => Label;

        /// <summary>
        /// Compares the current MRUKAnchor object with another Data.AnchorData object for equality.
        /// </summary>
        /// <param name="anchorData">The Data.AnchorData object to compare with.</param>
        /// <returns>True if both objects are equal, false otherwise.</returns>
        public bool Equals(Data.AnchorData anchorData)
        {
            return Anchor == anchorData.Anchor &&
                   PlaneRect == Utilities.GetPlaneRectFromAnchorData(anchorData) &&
                   VolumeBounds == Utilities.GetVolumeBoundsFromAnchorData(anchorData) &&
                   Label == Utilities.StringLabelsToEnum(anchorData.SemanticClassifications) &&
                   PlaneBoundary2D.SequenceEqual(anchorData.PlaneBoundary2D);
        }
    }
}
