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
using System.Runtime.CompilerServices;
using Meta.XR.Util;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;

[assembly: InternalsVisibleTo("meta.xr.mrutilitykit.tests")]

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Represents a room within MR Utility Kit, containing various anchors and providing a set of utility functions.
    /// </summary>
    [Feature(Feature.Scene)]
    public class MRUKRoom : MonoBehaviour
    {
        /// <summary>
        /// The primary anchor associated with the room.
        /// </summary>
        public OVRAnchor Anchor = OVRAnchor.Null;



        /// <summary>
        ///     A room is considered local if it was loaded from device. If it was loaded from some other source,
        ///     e.g. JSON or Prefab then it is not local.
        /// </summary>
        public bool IsLocal => Anchor.Handle != 0;

        /// <summary>
        /// Contains all the scene anchors in the room.
        /// </summary>
        public List<MRUKAnchor> Anchors
        {
            get;
        } = new();

        /// <summary>
        /// Contains all the wall anchors in the room.
        /// </summary>
        public List<MRUKAnchor> WallAnchors
        {
            get;
        } = new();

        /// <summary>
        /// The floor anchor in the room.
        /// </summary>
        public MRUKAnchor FloorAnchor
        ;


        /// <summary>
        /// The ceiling anchor in the room.
        /// </summary>
        public MRUKAnchor CeilingAnchor
        ;

        /// <summary>
        /// The global mesh anchor in the room.
        /// </summary>
        public MRUKAnchor GlobalMeshAnchor
        {
            get;
            internal set;
        }

        /// <summary>
        /// A list of seat poses in the room:
        /// suggested placements for remote avatars, that exist only on COUCH objects
        /// couchPoses are in couchAnchor space
        /// </summary>
        public struct CouchSeat
        {
            public MRUKAnchor couchAnchor; // The anchor associated with the couch. This anchor provides the spatial context for the seat poses.
            public List<Pose> couchPoses; // A list of poses representing suggested placements for avatars or users on the couch. Each pose is relative to the couch anchor's space.
        };

        /// <summary>
        /// Contains a list of suggested seat placements within the room, specifically on couch objects.
        /// Each seat pose is defined relative to the couch anchor's space.
        /// </summary>
        public List<CouchSeat> SeatPoses
        {
            get;
        } = new();

        struct Surface
        {
            public MRUKAnchor Anchor;
            public float UsableArea;
            public bool IsPlane;
            public Rect Bounds;
            public Matrix4x4 Transform;
        }

        Bounds _roomBounds = new();

        // a CW list of bottom-left corner points of each wall, in Unity space
        private List<Vector3> _corners = new();

        // track the pose to compute cached data only if the room has been moved since the last request
        private Pose? _prevRoomPose = default;

        /// <summary>
        ///     Gets fired when a new anchor of this room has been created
        /// </summary>
        [field: SerializeField, FormerlySerializedAs(nameof(AnchorCreatedEvent))]
        public UnityEvent<MRUKAnchor> AnchorCreatedEvent
        {
            get;
            private set;
        } = new();

        /// <summary>
        ///     Gets fired after a component of the corresponding anchor has changed
        /// </summary>
        [field: SerializeField, FormerlySerializedAs(nameof(AnchorUpdatedEvent))]
        public UnityEvent<MRUKAnchor> AnchorUpdatedEvent
        {
            get;
            private set;
        } = new();

        /// <summary>
        ///     Gets fired when the anchor has been deleted.
        /// </summary>
        [field: SerializeField, FormerlySerializedAs(nameof(AnchorRemovedEvent))]
        public UnityEvent<MRUKAnchor> AnchorRemovedEvent
        {
            get;
            private set;
        } = new();

        /// <summary>
        ///     Registers a callback function to be called when an anchor is created.
        /// </summary>
        /// <param name="callback">
        ///     The function to be called when an anchor is created. It takes one parameter:
        ///     - `MRUKAnchor` The created anchor object.
        /// </param>
        [Obsolete("Use UnityEvent AnchorCreatedEvent directly instead")]
        public void RegisterAnchorCreatedCallback(UnityAction<MRUKAnchor> callback)
        {
            AnchorCreatedEvent.AddListener(callback);
        }

        /// <summary>
        ///     Registers a callback function to be called before an anchor is updated.
        /// </summary>
        /// <param name="callback">
        ///     The function to be called when an anchor is updated. It takes one parameter:
        ///     - `MRUKAnchor` The updated anchor object.
        /// </param>
        [Obsolete("Use UnityEvent AnchorUpdatedEvent directly instead")]
        public void RegisterAnchorUpdatedCallback(UnityAction<MRUKAnchor> callback)
        {
            AnchorUpdatedEvent.AddListener(callback);
        }

        /// <summary>
        ///     Registers a callback function to be called when an anchor is removed.
        /// </summary>
        /// <param name="callback">
        ///     The function to be called when an anchor is removed. It takes one parameter:
        ///     - `MRUKAnchor` The removed anchor object.
        /// </param>
        [Obsolete("Use UnityEvent AnchorRemovedEvent directly instead")]
        public void RegisterAnchorRemovedCallback(UnityAction<MRUKAnchor> callback)
        {
            AnchorRemovedEvent.AddListener(callback);
        }

        /// <summary>
        ///     UnRegisters a callback function to be called when an anchor is created.
        /// </summary>
        /// <param name="callback">
        ///     The function to be called when an anchor is created. It takes one parameter:
        ///     - `MRUKAnchor` The created anchor object.
        /// </param>
        [Obsolete("Use UnityEvent AnchorCreatedEvent directly instead")]
        public void UnRegisterAnchorCreatedCallback(UnityAction<MRUKAnchor> callback)
        {
            AnchorCreatedEvent.RemoveListener(callback);
        }

        /// <summary>
        ///     UnRegisters a callback function to be called before an anchor is updated.
        /// </summary>
        /// <param name="callback">
        ///     The function to be called when an anchor is updated. It takes one parameter:
        ///     - `MRUKAnchor` The updated anchor object.
        /// </param>
        [Obsolete("Use UnityEvent AnchorUpdatedEvent directly instead")]
        public void UnRegisterAnchorUpdatedCallback(UnityAction<MRUKAnchor> callback)
        {
            AnchorUpdatedEvent.RemoveListener(callback);
        }

        /// <summary>
        ///     UnRegisters a callback function to be called when an anchor is removed.
        /// </summary>
        /// <param name="callback">
        ///     The function to be called when an anchor is removed. It takes one parameter:
        ///     - `MRUKAnchor` The removed anchor object.
        /// </param>
        [Obsolete("Use UnityEvent AnchorRemovedEvent directly instead")]
        public void UnRegisterAnchorRemovedCallback(UnityAction<MRUKAnchor> callback)
        {
            AnchorRemovedEvent.RemoveListener(callback);
        }

        /// <summary>
        /// Updates the room's label and the GameObject's name based on the provided room data.
        /// If a room label is available, it sets the GameObject's name to include the label.
        /// Otherwise, it uses the room's anchor UUID for the GameObject's name.
        /// </summary>
        /// <param name="roomData">The data containing information about the room, including the label and anchor.</param>
        internal void UpdateRoomLabel(Data.RoomData roomData)
        {
            {
                gameObject.name = $"Room - {roomData.Anchor.Uuid}";
            }
        }


        internal void UpdateRoom(Data.RoomData roomData)
        {
            var useRoomLayout = true;

            if (useRoomLayout)
            {
                WallAnchors.Clear();
                foreach (var wallUuid in roomData.RoomLayout.WallsUuid)
                {
                    var anchor = FindAnchorByUuid(wallUuid);

                    Assert.IsNotNull(anchor, $"Wall anchor with UUID {wallUuid} not found!");
                    if (anchor)
                    {
                        WallAnchors.Add(anchor);
                    }
                }

                FloorAnchor = FindAnchorByUuid(roomData.RoomLayout.FloorUuid);
                CeilingAnchor = FindAnchorByUuid(roomData.RoomLayout.CeilingUuid);
                Assert.IsNotNull(CeilingAnchor, $"Ceiling anchor with UUID {roomData.RoomLayout.CeilingUuid} not found!");
                Assert.IsNotNull(FloorAnchor, $"Floor anchor with UUID {roomData.RoomLayout.FloorUuid} not found!");
            }
        }

        private MRUKAnchor FindAnchorByUuid(Guid uuid)
        {
            foreach (var anchor in Anchors)
            {
                if (anchor.Anchor.Uuid == uuid)
                {
                    return anchor;
                }
            }

            return null;
        }

        /// <summary>
        ///     Compute further information to the room.
        ///     Important: requires that the room's child <seealso cref="MRUKAnchor" />
        ///     has been properly instantiated and data populated.
        /// </summary>
        internal void ComputeRoomInfo()
        {
            // Find the global mesh anchor
            foreach (var anchor in Anchors)
            {
                if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.GLOBAL_MESH))
                {
                    GlobalMeshAnchor = anchor;
                    break;
                }
            }

            CalculateSeatPoses();
            CalculateHierarchyReferences();
        }

        /// <summary>
        /// Returns all the Scene objects (anchors) in the room.
        /// Useful if you want to do your own calculations within the Mixed Reality Utility Kit framework.
        /// </summary>
        /// <returns>A list of MRUKAnchor objects representing the anchors in the room.</returns>
        [Obsolete("Use Anchors property instead")]
        public List<MRUKAnchor> GetRoomAnchors()
        {
            return Anchors;
        }

        /// <summary>
        ///     Removes an anchor from the internal list and destroys the gameobject and it's children.
        /// </summary>
        /// <param name="anchor">The Anchor to remove and destroy</param>
        public void RemoveAndDestroyAnchor(MRUKAnchor anchor)
        {
            if (anchor == null)
            {
                return;
            }

            Anchors.Remove(anchor);
            Utilities.DestroyGameObjectAndChildren(anchor.gameObject);
        }

        /// <summary>
        /// Retrieves the floor anchor of this room.
        /// </summary>
        /// <returns>The floor anchor.</returns>
        [Obsolete("Use FloorAnchor property instead")]
        public MRUKAnchor GetFloorAnchor()
        {
            return FloorAnchor;
        }

        /// <summary>
        /// Retrieves the ceiling anchor of this room.
        /// </summary>
        /// <returns>The ceiling anchor.</returns>
        [Obsolete("Use CeilingAnchor property instead")]
        public MRUKAnchor GetCeilingAnchor()
        {
            return CeilingAnchor;
        }

        /// <summary>
        /// Retrieves the global mesh anchor of this room.
        /// </summary>
        /// <returns>The global mesh anchor.</returns>stead")]
        public MRUKAnchor GetGlobalMeshAnchor()
        {
            return GlobalMeshAnchor;
        }

        /// <summary>
        /// Retrieves the list of wall anchors of this room.
        /// </summary>
        /// <returns>A list of wall anchors.</returns>
        [Obsolete("Use WallAnchors property instead")]
        public List<MRUKAnchor> GetWallAnchors()
        {
            return WallAnchors;
        }

        /// <summary>
        ///     Calculates seat poses (free space on a COUCH object) for humans/avatars. <br />
        ///     Y-up is vertical, Z-forward will point away from the closest WALL_FACE
        /// </summary>
        void CalculateSeatPoses()
        {
            SeatPoses.Clear();
            float seatWidth = MRUK.Instance.SceneSettings.SeatWidth;

            foreach (var anchor in Anchors)
            {
                if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.COUCH))
                {
                    CouchSeat newSeat = new CouchSeat
                    {
                        couchAnchor = anchor,
                        couchPoses = new List<Pose>()
                    };

                    Vector2 surfaceDim = anchor.PlaneRect?.size ?? Vector2.one;
                    float surfaceRatio = surfaceDim.x / surfaceDim.y;
                    Vector3 seatFwd = GetFacingDirection(anchor);
                    Vector3 seatUp = Vector3.up;
                    Vector3.OrthoNormalize(ref seatFwd, ref seatUp);
                    Quaternion anchorSpace = Quaternion.Inverse(anchor.transform.rotation);
                    if (surfaceRatio < 2.0f && surfaceRatio > 0.5f)
                    {
                        // if the surface dimensions are mostly square (likely a chair), just have one centered seat
                        Pose seatPose = new Pose(Vector3.zero, anchorSpace * Quaternion.LookRotation(seatFwd, seatUp));
                        newSeat.couchPoses.Add(seatPose);

                        SeatPoses.Add(newSeat);
                    }
                    else
                    {
                        bool xLong = surfaceDim.x > surfaceDim.y;

                        float longestDim = xLong ? surfaceDim.x : surfaceDim.y;
                        float numSeats = Mathf.Floor(longestDim / seatWidth);
                        float seatBuffer = (longestDim - (numSeats * seatWidth)) / numSeats;
                        for (int k = 0; k < numSeats; k++)
                        {
                            Vector3 seatRight = xLong ? anchor.transform.right : anchor.transform.up;
                            Vector3 seatPos = Vector3.zero;
                            // start at the edge
                            seatPos -= seatRight * longestDim * 0.5f;
                            seatPos += seatRight * seatBuffer * 0.5f;
                            // the first seat's position
                            seatPos += seatRight * seatWidth * 0.5f;
                            // now we increment
                            seatPos += seatRight * seatWidth * k;
                            seatPos += seatRight * seatBuffer * k;

                            Pose seatPose = new Pose(anchorSpace * seatPos, anchorSpace * Quaternion.LookRotation(seatFwd, seatUp));
                            newSeat.couchPoses.Add(seatPose);
                            SeatPoses.Add(newSeat);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a clockwise (when viewed top-down) list of wall corner points at floor level.
        /// This method is useful for obtaining the geometric outline of a room based on its walls.
        /// </summary>
        /// <returns>A list of Vector3 points representing the corners of the room's walls at floor level, arranged in a clockwise order.</returns>
        public List<Vector3> GetRoomOutline()
        {
            CalculateRoomOutlineAndBounds();
            return _corners;
        }

        /// <summary>
        /// Identifies the key wall in the room. A key wall is the longest wall with no other room points behind it,
        /// allowing for a more controlled artistic environment, akin to a stage. This method is useful for scenarios
        /// where a clear, unobstructed view of one wall is necessary. Can be used to craft a "stage-like" environment.
        /// </summary>
        /// <param name="wallScale">Outputs the scale of the key wall, if found.</param>
        /// <param name="tolerance">A tolerance value to account for precision issues in determining if points are behind the wall.</param>
        /// <returns>The anchor representing the key wall, or null if no suitable wall is found.</returns>
        public MRUKAnchor GetKeyWall(out Vector2 wallScale, float tolerance = 0.1f)
        {
            wallScale = Vector3.one;

            // first, sort the walls in order of x length
            // TODO: this is probably expensive, and shouldn't be called every frame. Cache them?
            List<MRUKAnchor> sortedWalls = new List<MRUKAnchor>(WallAnchors);
            MRUKAnchor keyWall = null;
            sortedWalls = SortWallsByWidth(sortedWalls);

            List<Vector3> corners = GetRoomOutline();

            // second, find the first one with no other walls behind it
            // count down because the default sorting is from shortest to longest
            for (int i = sortedWalls.Count - 1; i >= 0; i--)
            {
                bool noPointsBehind = true;

                // loop through the other corners, making sure none is behind the wall in question
                for (int k = 0; k < corners.Count; k++)
                {
                    Vector3 vecToCorner = corners[k] - sortedWalls[i].transform.position;

                    // due to anchor precision, we use a tolerance value
                    // for example, an adjacent wall edge may be just behind the wall, leading to a false result
                    vecToCorner += sortedWalls[i].transform.forward * tolerance;

                    noPointsBehind &= Vector3.Dot(sortedWalls[i].transform.forward, vecToCorner) >= 0.0f;

                    // if any corner is behind this wall, it already fails and we don't need to test other corners
                    if (!noPointsBehind)
                    {
                        break;
                    }
                }

                // early exit upon finding the first one
                if (noPointsBehind)
                {
                    wallScale = sortedWalls[i].PlaneRect.Value.size;
                    keyWall = sortedWalls[i];
                    break;
                }
            }

            return keyWall;
        }

        /// <summary>
        /// Sorts a list of wall anchors based on the width of their associated planes.
        /// This method is useful for organizing walls in ascending order of their widths, which can be helpful
        /// for operations that require processing walls from narrowest to widest or for selecting specific walls based on their size.
        /// </summary>
        /// <param name="walls">The list of wall anchors to sort.</param>
        /// <returns>A new list of wall anchors sorted by the width of their planes.</returns>
        public static List<MRUKAnchor> SortWallsByWidth(List<MRUKAnchor> walls)
        {
            List<MRUKAnchor> sortedWalls = new List<MRUKAnchor>();
            for (int i = 0; i < walls.Count; i++)
            {
                for (int j = i + 1; j < walls.Count; j++)
                {
                    if (walls[i].PlaneRect.Value.size.x > walls[j].PlaneRect.Value.size.x)
                    {
                        (walls[i], walls[j]) = (walls[j], walls[i]);
                    }
                }
            }

            sortedWalls.AddRange(walls);
            return sortedWalls;
        }

        /// <summary>
        /// Casts a ray against ONLY Scene API objects, returning all results.
        /// This method is used as a replacement for Physics.RaycastAll, specifically tailored to interact with Scene API objects.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="labelFilter">The filter to apply to scene anchors to determine which ones should be considered.</param>
        /// <param name="raycastHits">A list to store the RaycastHit results.</param>
        /// <param name="anchorList">A list to store the MRUKAnchor objects hit by the ray.</param>
        /// <returns>True if one or more objects were hit, false otherwise.</returns>
        public bool RaycastAll(Ray ray, float maxDist, LabelFilter labelFilter, List<RaycastHit> raycastHits, List<MRUKAnchor> anchorList)
        {
            RaycastHit outHit;
            raycastHits.Clear();
            anchorList.Clear();
            foreach (var anchor in Anchors)
            {
                if (labelFilter.PassesFilter(anchor.Label) && anchor.Raycast(ray, maxDist, out outHit, labelFilter.ComponentTypes ?? MRUKAnchor.ComponentType.All))
                {
                    raycastHits.Add(outHit);
                    anchorList.Add(anchor);
                }
            }

            return raycastHits.Count > 0;
        }

        /// <summary>
        /// Casts a ray against ONLY Scene API objects, returning the closest result.
        /// This method is used as a replacement for Physics.Raycast, specifically tailored to interact with Scene API objects.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="labelFilter">The filter to apply to scene anchors to determine which ones should be considered.</param>
        /// <param name="hit">The closest RaycastHit result, if any.</param>
        /// <param name="outAnchor">The MRUKAnchor associated with the closest hit, if any.</param>
        /// <returns>True if an object was hit, false otherwise.</returns>
        public bool Raycast(Ray ray, float maxDist, LabelFilter labelFilter, out RaycastHit hit, out MRUKAnchor outAnchor)
        {
            hit = new RaycastHit();
            outAnchor = null;
            bool hitSomething = false;
            float closestDist = maxDist;

            foreach (var anchor in Anchors)
            {
                if (labelFilter.PassesFilter(anchor.Label) && anchor.Raycast(ray, closestDist, out RaycastHit rayHit, labelFilter.ComponentTypes ?? MRUKAnchor.ComponentType.All))
                {
                    closestDist = rayHit.distance;
                    hit = rayHit;
                    outAnchor = anchor;
                    hitSomething = true;
                }
            }

            return hitSomething;
        }

        /// <summary>
        /// Casts a ray against ONLY Scene API objects, returning the closest result.
        /// This method is used as a replacement for Physics.Raycast, without specifying a label filter.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="hit">The closest RaycastHit result, if any.</param>
        /// <param name="anchor">The MRUKAnchor associated with the closest hit, if any.</param>
        /// <returns>True if an object was hit, false otherwise.</returns>
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hit, out MRUKAnchor anchor)
        {
            return Raycast(ray, maxDist, new LabelFilter(), out hit, out anchor);
        }

        /// <summary>
        /// Casts a ray against ONLY Scene API objects, returning the closest result.
        /// This method is used as a replacement for Physics.Raycast, allowing for a specified label filter but not returning the anchor.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="labelFilter">The filter to apply to scene anchors to determine which ones should be considered.</param>
        /// <param name="hit">The closest RaycastHit result, if any.</param>
        /// <returns>True if an object was hit, false otherwise.</returns>
        public bool Raycast(Ray ray, float maxDist, LabelFilter labelFilter, out RaycastHit hit)
        {
            return Raycast(ray, maxDist, labelFilter, out hit, out var _);
        }

        /// <summary>
        /// Casts a ray against ONLY Scene API objects, returning the closest result.
        /// This method is used as a replacement for Physics.Raycast, without specifying a label filter or returning the anchor.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="hit">The closest RaycastHit result, if any.</param>
        /// <returns>True if an object was hit, false otherwise.</returns>
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hit)
        {
            return Raycast(ray, maxDist, new LabelFilter(), out hit, out var _);
        }


        /// <summary>
        /// Calculates the best pose based on a raycast, considering specific label filters and positioning methods.
        /// This method is useful for determining the optimal position and orientation for placing objects in a scene based on surface interactions.
        /// i.e. how best to "place an app/widget" on a surface.
        /// </summary>
        /// <remarks> <list type="bullet">
        /// <item>
        /// When hitting vertical surfaces; Y is world-up, Z is surface normal.
        /// </item>
        /// <item>
        /// When hitting horizontal surfaces; Y is world-up, Z is best-guess at player-facing.
        /// </item>
        /// <item>
        ///  "best guess" = when on ceiling/floor, Z faces player; when on surface (desk/couch), Z faces closest edge to player
        /// </item>
        /// </list></remarks>
        /// <param name="ray">The ray to cast into the scene.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="labelFilter">The filter to apply to scene anchors to determine which ones should be considered.</param>
        /// <param name="sceneAnchor">The scene anchor that the raycast hit, if any.</param>
        /// <param name="surfaceNormal">The normal of the surface at the hit point.</param>
        /// <param name="positioningMethod">The method to use for positioning the pose. Defaults to <see cref="MRUK.PositioningMethod.DEFAULT"/>.</param>
        /// <returns>The best pose calculated from the raycast, or a default pose if no suitable surface is found.</returns>
        public Pose GetBestPoseFromRaycast(Ray ray, float maxDist, LabelFilter labelFilter, out MRUKAnchor sceneAnchor, out Vector3 surfaceNormal,
            MRUK.PositioningMethod positioningMethod = MRUK.PositioningMethod.DEFAULT)
        {
            sceneAnchor = null;
            Pose bestPose = new Pose();
            surfaceNormal = Vector3.up;

            if (Raycast(ray, maxDist, labelFilter, out var closestHit, out sceneAnchor))
            {
                Vector3 defaultPose = closestHit.point;
                surfaceNormal = closestHit.normal;
                Vector3 poseUp = Vector3.up;
                // by default, use the surface normal for pose forward
                // caution: make sure all the cases of this being "up" are caught below
                Vector3 poseFwd = closestHit.normal;

                if (Vector3.Dot(closestHit.normal, Vector3.up) >= 0.9f && sceneAnchor.VolumeBounds.HasValue)
                {
                    // this is a volume object, and the ray has hit the top surface
                    // "snap" the pose Z to align with the closest edge
                    Vector3 toPlane = ray.origin - sceneAnchor.transform.position;
                    Vector3 planeYup = Vector3.Dot(sceneAnchor.transform.up, toPlane) > 0.0f ? sceneAnchor.transform.up : -sceneAnchor.transform.up;
                    Vector3 planeXup = Vector3.Dot(sceneAnchor.transform.right, toPlane) > 0.0f ? sceneAnchor.transform.right : -sceneAnchor.transform.right;
                    Vector3 planeFwd = sceneAnchor.transform.forward;

                    Vector2 anchorScale = sceneAnchor.VolumeBounds.Value.size;
                    Vector3 nearestCorner = sceneAnchor.transform.position + planeXup * anchorScale.x * 0.5f + planeYup * anchorScale.y * 0.5f;
                    Vector3.OrthoNormalize(ref planeFwd, ref toPlane);
                    nearestCorner -= sceneAnchor.transform.position;
                    bool xUp = Vector3.Angle(toPlane, planeYup) > Vector3.Angle(nearestCorner, planeYup);
                    poseFwd = xUp ? planeXup : planeYup;
                    float offset = xUp ? anchorScale.x : anchorScale.y;
                    switch (positioningMethod)
                    {
                        case MRUK.PositioningMethod.CENTER:
                            defaultPose = sceneAnchor.transform.position;
                            break;
                        case MRUK.PositioningMethod.EDGE:
                            defaultPose = sceneAnchor.transform.position + poseFwd * offset * 0.5f;
                            break;
                        case MRUK.PositioningMethod.DEFAULT:
                            break;
                    }
                }
                else if (Mathf.Abs(Vector3.Dot(closestHit.normal, Vector3.up)) >= 0.9f)
                {
                    // This may be the floor, ceiling or any other horizontal plane surface
                    poseFwd = new Vector3(ray.origin.x - closestHit.point.x, 0, ray.origin.z - closestHit.point.z).normalized;
                }

                bestPose.position = defaultPose;
                bestPose.rotation = Quaternion.LookRotation(poseFwd, poseUp);
            }
            else
            {
                Debug.Log("Best pose not found, no surface anchor detected.");
            }

            return bestPose;
        }

        /// <summary>
        /// Calculates the best pose based on a raycast, considering specific label filters and positioning methods.
        /// This version of the method does not return the surface normal explicitly.
        /// </summary>
        /// <param name="ray">The ray to cast into the scene.</param>
        /// <param name="maxDist">The maximum distance the ray should check for collisions.</param>
        /// <param name="labelFilter">The filter to apply to scene anchors to determine which ones should be considered.</param>
        /// <param name="sceneAnchor">The scene anchor that the raycast hit, if any.</param>
        /// <param name="positioningMethod">The method to use for positioning the pose. Defaults to <see cref="MRUK.PositioningMethod.DEFAULT"/>.</param>
        /// <returns>The best pose calculated from the raycast, or a default pose if no suitable surface is found.</returns>
        public Pose GetBestPoseFromRaycast(Ray ray, float maxDist, LabelFilter labelFilter, out MRUKAnchor sceneAnchor, MRUK.PositioningMethod positioningMethod = MRUK.PositioningMethod.DEFAULT)
        {
            Pose bestPose = GetBestPoseFromRaycast(ray, maxDist, labelFilter, out sceneAnchor, out Vector3 surfaceNormal, positioningMethod);
            return bestPose;
        }

        /// <summary>
        /// Tests if a given position is inside the floor outline of the room's walls. This method can optionally test the vertical bounds
        /// to ensure the position is not outside the room's floor or ceiling, preventing the room from being treated as an "infinite column."
        /// </summary>
        /// <param name="queryPosition">The world position to test.</param>
        /// <param name="testVerticalBounds">If true, the method also checks if the position is within the vertical bounds of the room (floor and ceiling).
        /// Defaults to true.</param>
        /// <returns>True if the position is within the room's boundaries, false otherwise.</returns>
        public bool IsPositionInRoom(Vector3 queryPosition, bool testVerticalBounds = true)
        {
            //this is a fallback because the anchor can be deleted but this gets only updated once per frame
            if (FloorAnchor == null)
            {
                return false;
            }
            var isInRoom = false;

            var localPos = FloorAnchor.transform.InverseTransformPoint(queryPosition);
            isInRoom |= FloorAnchor.IsPositionInBoundary(localPos);


            // by default, this just tests the bounds when viewed top-down
            // to truly be a 3D test, also check the floor/ceiling
            if (testVerticalBounds)
            {
                var roomBounds = GetRoomBounds();
                isInRoom &= TestVerticalBounds(queryPosition, roomBounds);
            }
            return isInRoom;
        }

        private bool TestVerticalBounds(Vector3 queryPosition, Bounds roomBounds)
        {
            return (queryPosition.y <= roomBounds.max.y && queryPosition.y >= roomBounds.min.y);
        }

        /// <summary>
        /// Get a world-oriented bounding box of the room
        /// </summary>
        /// <returns>The bounding box that encapsulates the entire room.</returns>
        public Bounds GetRoomBounds()
        {
            CalculateRoomOutlineAndBounds();

            return _roomBounds;
        }
        private void CalculateRoomOutlineAndBounds()
        {
            if (!FloorAnchor || !CeilingAnchor)
            {
                Debug.LogWarning("Floor or Ceiling anchor not found");
                return;
            }

            if (_prevRoomPose is Pose pose && (transform.position == pose.position && transform.rotation == pose.rotation))
            {
                // Room hasn't moved, no need to calculate outline or bounds
                return;
            }

            _prevRoomPose = new(transform.position, transform.rotation);

            var xMin = Mathf.Infinity;
            var xMax = Mathf.NegativeInfinity;
            var yMin = Mathf.Infinity;
            var yMax = Mathf.NegativeInfinity;
            var zMin = Mathf.Infinity;
            var zMax = Mathf.NegativeInfinity;

            _corners.Clear();
            foreach (var point in FloorAnchor.PlaneBoundary2D)
            {
                Vector3 pos = FloorAnchor.transform.TransformPoint(new Vector3(point.x, point.y, 0f));

                xMin = Mathf.Min(xMin, pos.x);
                xMax = Mathf.Max(xMax, pos.x);
                yMin = Mathf.Min(yMin, pos.y);
                yMax = Mathf.Max(yMax, pos.y);
                zMin = Mathf.Min(zMin, pos.z);
                zMax = Mathf.Max(zMax, pos.z);

                _corners.Add(pos);
            }
            foreach (var point in CeilingAnchor.PlaneBoundary2D)
            {
                Vector3 pos = CeilingAnchor.transform.TransformPoint(new Vector3(point.x, point.y, 0f));

                xMin = Mathf.Min(xMin, pos.x);
                xMax = Mathf.Max(xMax, pos.x);
                yMin = Mathf.Min(yMin, pos.y);
                yMax = Mathf.Max(yMax, pos.y);
                zMin = Mathf.Min(zMin, pos.z);
                zMax = Mathf.Max(zMax, pos.z);
            }

            _roomBounds.center = new Vector3((xMax + xMin) * 0.5f, (yMax + yMin) * 0.5f, (zMax + zMin) * 0.5f);
            _roomBounds.size = new Vector3(xMax - xMin, yMax - yMin, zMax - zMin);
        }

        /// <summary>
        ///  Test if a position is inside of a Scene volume, and optionally return the object. <br />
        ///  To also check if a position is inside the room walls, use IsPositionInRoom(). <br />
        ///  Use distanceBuffer as a cheap way to check volume intersection.
        /// </summary>
        /// <param name="worldPosition">The world position to test.</param>
        /// <param name="sceneObject">The scene object that the position is inside of, if any.</param>
        /// <param name="testVerticalBounds">Whether to test vertical bounds of the volume.</param>
        /// <param name="distanceBuffer">A buffer distance for volume intersection tests.</param>
        /// <returns>True if the position is inside a scene volume, false otherwise.</returns>
        public bool IsPositionInSceneVolume(Vector3 worldPosition, out MRUKAnchor sceneObject, bool testVerticalBounds, float distanceBuffer = 0.0f)
        {
            bool isInObject = false;
            sceneObject = null;
            foreach (var anchor in Anchors)
            {
                if (anchor.IsPositionInVolume(worldPosition, testVerticalBounds, distanceBuffer))
                {
                    isInObject = true;
                    sceneObject = anchor;
                    break;
                }
            }

            return isInObject;
        }


        /// <summary>
        ///  Get a "likely" direction this anchor is facing. For planes, this is always the normal (Z-forward).
        ///  For volumes, we use contextual clues; primarily, the closest wall is the "back" of the volume,
        ///  and the facing direction is its axis most aligned with this wall normal.
        /// </summary>
        /// <param name="anchor">The anchor to evaluate.</param>
        /// <returns>The facing direction of the anchor.</returns>
        public Vector3 GetFacingDirection(MRUKAnchor anchor)
        {
            // For planes, just use the anchor Z
            if (!anchor.VolumeBounds.HasValue)
            {
                return anchor.transform.forward;
            }

            return GetDirectionAwayFromClosestWall(anchor, out var _);
        }

        internal Vector3 GetDirectionAwayFromClosestWall(MRUKAnchor anchor, out int cardinalAxisIndex, List<int> excludedAxes = null)
        {
            float closestWallDistance = Mathf.Infinity;
            // Due to the odd rotation of anchors, we need to use transform.up here instead of transform.forward
            // as forward actually points upwards.
            Vector3 awayFromWall = anchor.transform.up;
            cardinalAxisIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (excludedAxes != null && excludedAxes.Contains(i))
                {
                    continue;
                }

                // shoot rays along cardinal directions
                Vector3 cardinalAxis = Quaternion.Euler(0, 90f * i, 0) * -anchor.transform.up;

                foreach (var wallAnchor in WallAnchors)
                {
                    if (wallAnchor.Raycast(new Ray(anchor.transform.position, cardinalAxis), closestWallDistance, out var outHit))
                    {
                        closestWallDistance = outHit.distance;
                        // whichever wall is closest, point Z-forward away from it
                        cardinalAxisIndex = i;
                        awayFromWall = -cardinalAxis;
                    }
                }
            }

            return awayFromWall;
        }

        /// <summary>
        ///  Test if a position is inside of a Scene volume (couch, desk, etc.). <br />
        ///  To also check if a position is inside the room walls, use IsPositionInRoom(). <br />
        ///  Use distanceBuffer as a cheap way to check volume intersection.
        /// </summary>
        /// <param name="worldPosition">The world position to test.</param>
        /// <param name="distanceBuffer">A buffer distance for volume intersection tests, defaults to 0.0f.</param>
        /// <returns>True if the position is inside a scene volume, false otherwise.</returns>
        public bool IsPositionInSceneVolume(Vector3 worldPosition, float distanceBuffer = 0.0f)
        {
            bool isInObject = IsPositionInSceneVolume(worldPosition, out _, true, distanceBuffer);
            return isInObject;
        }

        /// <summary>
        ///  Test if a position is inside of a Scene volume (couch, desk, etc.). <br />
        ///  To also check if a position is inside the room walls, use IsPositionInRoom(). <br />
        ///  Use distanceBuffer as a cheap way to check volume intersection.
        /// </summary>
        /// /// <param name="worldPosition">The world position to test.</param>
        /// <param name="testVerticalBounds">Whether to test vertical bounds of the volume.</param>
        /// <param name="distanceBuffer">A buffer distance for volume intersection tests, defaults to 0.0f.</param>
        /// <returns>True if the position is inside a scene volume, false otherwise.</returns>
        public bool IsPositionInSceneVolume(Vector3 worldPosition, bool testVerticalBounds, float distanceBuffer = 0.0f)
        {
            bool isInObject = IsPositionInSceneVolume(worldPosition, out _, testVerticalBounds, distanceBuffer);
            return isInObject;
        }

        /// <summary>
        /// Returns the best-suggested seat, for scenarios like remote caller placement.
        /// </summary>
        /// <param name="ray">The ray to use for finding the closest seat.</param>
        /// <param name="seatPose">The pose of the closest seat.</param>
        /// <param name="couch">The anchor associated with the closest seat.</param>
        /// <returns>True if a seat is found, false otherwise.</returns>
        public bool TryGetClosestSeatPose(Ray ray, out Pose seatPose, out MRUKAnchor couch)
        {
            Pose bestPose = new Pose();
            couch = null;

            float closestDot = -1.0f;
            for (int i = 0; i < SeatPoses.Count; i++)
            {
                Quaternion anchorSpace = SeatPoses[i].couchAnchor.transform.rotation;
                Vector3 anchorPosition = SeatPoses[i].couchAnchor.transform.position;
                for (int k = 0; k < SeatPoses[i].couchPoses.Count; k++)
                {
                    Vector3 seatWorldPosition = anchorPosition + anchorSpace * SeatPoses[i].couchPoses[k].position;
                    Vector3 vecToSeat = (seatWorldPosition - ray.origin).normalized;
                    float thisDot = Vector3.Dot(ray.direction, vecToSeat);
                    if (thisDot > closestDot)
                    {
                        closestDot = thisDot;
                        bestPose.position = seatWorldPosition;
                        bestPose.rotation = anchorSpace * SeatPoses[i].couchPoses[k].rotation;
                        couch = SeatPoses[i].couchAnchor;
                    }
                }
            }

            seatPose.position = bestPose.position;
            seatPose.rotation = bestPose.rotation;

            return (SeatPoses.Count > 0);
        }

        /// <summary>
        /// Returns all seats in the room (a human-spaced position on a couch).
        /// </summary>
        /// <returns>An array of poses representing all seats in the room.</returns>
        public Pose[] GetSeatPoses()
        {
            List<Pose> poses = new List<Pose>();
            for (int i = 0; i < SeatPoses.Count; i++)
            {
                Quaternion anchorSpace = SeatPoses[i].couchAnchor.transform.rotation;
                Vector3 anchorPosition = SeatPoses[i].couchAnchor.transform.position;
                for (int k = 0; k < SeatPoses[i].couchPoses.Count; k++)
                {
                    Pose worldPos = new Pose(anchorPosition + anchorSpace * SeatPoses[i].couchPoses[k].position, anchorSpace * SeatPoses[i].couchPoses[k].rotation);
                    poses.Add(worldPos);
                }
            }

            return poses.ToArray();
        }

        /// <summary>
        /// Attempts to return the parent of an anchor, if it exists.
        /// This hierarchical relationship is by reference, not literally in the scene.
        /// </summary>
        /// <param name="queryAnchor">The anchor for which to find the parent.</param>
        /// <param name="parentAnchor">The parent anchor, if one exists.</param>
        /// <returns>True if the parent anchor exists, otherwise false.</returns>
        [Obsolete("Use ParentAnchor property instead")]
        public bool TryGetAnchorParent(MRUKAnchor queryAnchor, out MRUKAnchor parentAnchor)
        {
            parentAnchor = queryAnchor.ParentAnchor;
            return (parentAnchor != null);
        }

        /// <summary>
        /// Attempts to return the logical children of an anchor, if there are any.
        /// This hierarchical relationship is by reference, not literally in the scene.
        /// </summary>
        /// <param name="queryAnchor">The anchor for which to find the children.</param>
        /// <param name="childAnchors">An array of child anchors, if any exist.</param>
        /// <returns>True if there are one or more child anchors, otherwise false.</returns>
        [Obsolete("Use ChildAnchors property instead")]
        public bool TryGetAnchorChildren(MRUKAnchor queryAnchor, out MRUKAnchor[] childAnchors)
        {
            childAnchors = queryAnchor.ChildAnchors?.ToArray();
            return (childAnchors != null && childAnchors.Length > 0);
        }

        /// <summary>
        ///     (internal only) <br />
        ///     One-time calcuation, finds parent-child relationships between anchors. <br />
        ///     Because this relationship isn't a literal scene-graph hierarchy, we can't just use transform.parent or transform.GetChild()
        /// </summary>
        void CalculateHierarchyReferences()
        {
            const float coPlanarTolerance = 0.1f;
            for (int i = 0; i < Anchors.Count; i++)
            {
                Anchors[i].ClearChildReferences();
                if (Anchors[i].HasAnyLabel(MRUKAnchor.SceneLabels.WALL_FACE) && Anchors[i].PlaneRect.HasValue)
                {
                    // find all _anchors that are a "child" of this wall using heuristics
                    for (int k = 0; k < Anchors.Count; k++)
                    {
                        if (Anchors[k] == Anchors[i])
                        {
                            continue;
                        }

                        if (Anchors[k].PlaneRect.HasValue && !Anchors[k].VolumeBounds.HasValue)
                        {
                            float angle = Vector3.Angle(Anchors[k].transform.right, Anchors[i].transform.right);
                            // first check if they're co-planar (X-axes closely align)
                            bool alignsWithWall = (angle <= 5.0f);
                            // then check if it's close enough to the wall in local-Z
                            Vector3 localPos = Anchors[i].transform.InverseTransformPoint(Anchors[k].transform.position);
                            bool positionedOnWall = Mathf.Abs(localPos.z) <= coPlanarTolerance;
                            // then check if the center is within the bounds
                            // (checking each edge should be unnecessary, since they must be created on the wall via Room Setup)
                            bool withinWall = localPos.x <= Anchors[i].PlaneRect.Value.max.x && localPos.x >= Anchors[i].PlaneRect.Value.min.x;

                            // through these checks, we should have very high confidence that these anchors are related, even if the individual tolerances are generous
                            if (alignsWithWall && positionedOnWall && withinWall)
                            {
                                // take careful note of the iterators (i,k)
                                Anchors[i].AddChildReference(Anchors[k]);
                                Anchors[k].ParentAnchor = Anchors[i];
                            }
                        }
                    }
                }
                else if (Anchors[i].HasAnyLabel(MRUKAnchor.SceneLabels.FLOOR))
                {
                    // check volumes that are on the floor (should be all volumes, unless volumes are stacked)
                    for (int k = 0; k < Anchors.Count; k++)
                    {
                        if (Anchors[k].VolumeBounds.HasValue)
                        {
                            Vector3 volumeCenterBottom = Anchors[k].transform.position + Vector3.up * Anchors[k].VolumeBounds.Value.min.z;

                            bool volumeOnFloor = (volumeCenterBottom.y - Anchors[i].transform.position.y) <= coPlanarTolerance;

                            if (volumeOnFloor)
                            {
                                // take careful note of the iterators (i,k)
                                Anchors[i].AddChildReference(Anchors[k]);
                                Anchors[k].ParentAnchor = Anchors[i];
                            }
                        }
                    }
                }
                else if (Anchors[i].VolumeBounds.HasValue)
                {
                    Bounds parentVolumeBounds = Anchors[i].VolumeBounds.Value;

                    // treat this anchor (i) as a parent, and search for a child (k)
                    for (int k = 0; k < Anchors.Count; k++)
                    {
                        if (Anchors[k] == Anchors[i])
                        {
                            continue;
                        }

                        if (Anchors[k].VolumeBounds.HasValue)
                        {
                            var childVolumeBounds = Anchors[k].VolumeBounds.Value;
                            var childAnchorBottom = Anchors[k].transform.position + Vector3.up * Anchors[k].VolumeBounds.Value.min.z;
                            var parentAnchorTop = Anchors[i].transform.position + Vector3.up * Anchors[i].VolumeBounds.Value.max.z;

                            // if the child's bottom is coplanar with the parent's top, this is likely a hierarchy
                            var isOnTop = Mathf.Abs(childAnchorBottom.y - parentAnchorTop.y) <= coPlanarTolerance;

                            if (isOnTop)
                            {
                                // still need to check to ensure at least one corner is within the bounds of the parent candidate bounds
                                bool anyCornerInside = false;
                                for (int c = 0; c < 4; ++c)
                                {
                                    // Get a different corner on each iteration of the loop (height is not important here)
                                    Vector3 cornerPos = new Vector3(i < 2 ? childVolumeBounds.min.x : childVolumeBounds.max.x, i % 2 == 0 ? childVolumeBounds.min.y : childVolumeBounds.max.y, 0.0f);
                                    // convert corner to world space
                                    cornerPos = Anchors[k].transform.TransformPoint(cornerPos);

                                    Vector3 parentRelativeCorner = Anchors[i].transform.InverseTransformPoint(cornerPos);

                                    const float fpTolerance = 0.001f; //1 mm
                                    var b1 = fpTolerance + (parentRelativeCorner.x - parentVolumeBounds.min.x) >= 0;
                                    var b2 = fpTolerance + (parentVolumeBounds.max.x - parentRelativeCorner.x) >= 0;
                                    var b3 = fpTolerance + (parentRelativeCorner.y - parentVolumeBounds.min.y) >= 0;
                                    var b4 = fpTolerance + (parentVolumeBounds.max.y - parentRelativeCorner.y) >= 0;

                                    if (b1 && b2 && b3 && b4)
                                    {
                                        anyCornerInside = true;
                                        break;
                                    }
                                }

                                if (anyCornerInside)
                                {

                                    //check if we already have identified a floor
                                    if (Anchors[k].ParentAnchor != null)
                                    {
                                        if (Anchors[k].ParentAnchor.HasAnyLabel(MRUKAnchor.SceneLabels.FLOOR))
                                        {
                                            continue; //we do not overwrite an identified floor parent. this can happen when bounding boxes on the floor are colliding
                                        }
                                    }
                                    // take careful note of the iterators (i,k)
                                    Anchors[i].AddChildReference(Anchors[k]);
                                    Anchors[k].ParentAnchor = Anchors[i];
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the room contains all specified labels. This method is obsolete.
        /// Use <see cref="HasAllLabels"/> instead.
        /// <see cref="OVRSemanticLabels.DeprecationMessage" />
        /// </summary>
        /// <param name="labels">Array of label names to check.</param>
        /// <returns>True if the room contains all specified labels, otherwise false.</returns>
        [Obsolete("Use '" + nameof(HasAllLabels) + "()' instead.")]
        public bool DoesRoomHave(string[] labels) => HasAllLabels(Utilities.StringLabelsToEnum(labels));

        /// <summary>
        /// Determines whether a room contains all the provided Scene API labels.
        /// </summary>
        /// <param name="labelFlags">The labels to check, represented as a combination of MRUKAnchor.SceneLabels.</param>
        /// <returns>True if the room contains all specified labels, otherwise false.</returns>
        public bool HasAllLabels(MRUKAnchor.SceneLabels labelFlags)
        {
            foreach (var anchor in Anchors)
            {
                labelFlags &= ~anchor.Label;
                if (labelFlags == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to find the closest surface position to a given world position within the room, considering only anchors that pass the specified label filter.
        /// </summary>
        /// <param name="worldPosition">The world position from which to find the closest surface.</param>
        /// <param name="surfacePosition">The closest surface position to the specified world position, if found.</param>
        /// <param name="closestAnchor">The anchor associated with the closest surface position, if found.</param>
        /// <param name="labelFilter">A filter to apply to the anchors when searching for the closest surface. Defaults to an empty filter, which includes all labels.</param>
        /// <returns>The distance to the closest surface position. Returns <see cref="Mathf.Infinity"/> if no valid surface is found.</returns>
        public float TryGetClosestSurfacePosition(Vector3 worldPosition, out Vector3 surfacePosition, out MRUKAnchor closestAnchor, LabelFilter labelFilter = new()) =>
            TryGetClosestSurfacePosition(worldPosition, out surfacePosition, out closestAnchor, out _, labelFilter);

        /// <summary>
        /// Attempts to find the closest surface position to a given world position within the room, considering only anchors that pass the specified label filter.
        /// </summary>
        /// <param name="worldPosition">The world position from which to find the closest surface.</param>
        /// <param name="surfacePosition">The closest surface position to the specified world position, if found.</param>
        /// <param name="closestAnchor">The anchor associated with the closest surface position, if found.</param>
        /// <param name="normal">The normal of the closest surface position, if found.</param>
        /// <param name="labelFilter">A filter to apply to the anchors when searching for the closest surface. Defaults to an empty filter, which includes all labels.</param>
        /// <returns>The distance to the closest surface position. Returns <see cref="Mathf.Infinity"/> if no valid surface is found.</returns>
        public float TryGetClosestSurfacePosition(Vector3 worldPosition, out Vector3 surfacePosition, out MRUKAnchor closestAnchor, out Vector3 normal, LabelFilter labelFilter = new())
        {
            float distance = Mathf.Infinity;
            surfacePosition = Vector3.zero;
            closestAnchor = null;
            normal = Vector3.zero;

            foreach (var anchor in Anchors)
            {
                if (!labelFilter.PassesFilter(anchor.Label))
                {
                    continue;
                }

                float dist = anchor.GetClosestSurfacePosition(worldPosition, out Vector3 thisSurfPos, out var thisNormal, labelFilter.ComponentTypes ?? MRUKAnchor.ComponentType.All);
                if (dist < distance)
                {
                    distance = dist;
                    surfacePosition = thisSurfPos;
                    normal = thisNormal;
                    closestAnchor = anchor.GetComponent<MRUKAnchor>();
                }
            }

            return distance;
        }

        /// <summary>
        /// Finds the largest surface within the room based on the specified anchor label.
        /// This method is obsolete and should not be used in new code.
        /// </summary>
        /// <param name="anchorLabel">The label of the anchor as a string.</param>
        /// <returns>The MRUKAnchor associated with the largest surface that matches the specified label.</returns>
        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public MRUKAnchor FindLargestSurface(string anchorLabel) => FindLargestSurface(Utilities.StringLabelToEnum(anchorLabel));

        /// <summary>
        /// Finds the anchor with the largest available surface area that matches the specified label flags.
        /// This method is more flexible than checking for specific conditions like table space and can be adapted for various usage scenarios.
        /// </summary>
        /// <param name="labelFlags">The labels to consider when searching for the largest surface. These are represented as a combination of MRUKAnchor.SceneLabels.</param>
        /// <returns>The MRUKAnchor with the largest surface area that matches the specified labels, or null if no suitable anchor is found.</returns>
        public MRUKAnchor FindLargestSurface(MRUKAnchor.SceneLabels labelFlags)
        {
            MRUKAnchor largestAnchor = null;
            float largestSurfaceArea = 0;
            foreach (var anchor in Anchors)
            {
                if (!anchor.HasAnyLabel(labelFlags))
                {
                    continue;
                }

                float thisSurfaceArea = 0f;

                if (anchor.PlaneRect.HasValue)
                {
                    Vector2 quadScale = anchor.PlaneRect.Value.size;
                    thisSurfaceArea = quadScale.x * quadScale.y;
                }
                else if (anchor.VolumeBounds.HasValue)
                {
                    Vector3 volumeSize = anchor.VolumeBounds.Value.size;
                    thisSurfaceArea = volumeSize.x * volumeSize.y;
                }

                if (thisSurfaceArea > largestSurfaceArea)
                {
                    largestSurfaceArea = thisSurfaceArea;
                    largestAnchor = anchor;
                }
            }

            return largestAnchor;
        }

        /// <summary>
        ///     Generate a random position in a room, while avoiding volume scene
        ///     objects and points that are too close to surfaces.
        ///     This function uses random sampling and a maximum number of iterations.
        /// </summary>
        /// <param name="minDistanceToSurface">
        ///     Reject points whose proximity to
        ///     a surface is less than the parameter.
        /// </param>
        /// <param name="avoidVolumes">
        ///     Do not allow points to be within volume
        ///     scene objects.
        /// </param>
        /// <returns>A position that adhers to the constraints, null otherwise.</returns>
        public Vector3? GenerateRandomPositionInRoom(float minDistanceToSurface, bool avoidVolumes)
        {
            if (!FloorAnchor)
            {
                return null;
            }

            Vector3 extents = GetRoomBounds().extents;
            float minExtent = Mathf.Min(extents.x, extents.y, extents.z);
            if (minDistanceToSurface > minExtent)
            {
                // We can exit early here as we know it's not possible to generate a position in the room that satisfies
                // the minDistanceToSurface requirement
                return null;
            }

            const int maxIterations = 1000;
            // Bail after MaxIteration tries to avoid infinite loop in case MinDistanceToSurface is too large
            // and we can't find a position which does not intersect with the walls and volumes
            for (int i = 0; i < maxIterations; ++i)
            {
                Vector3 spawnPosition = new Vector3(
                    UnityEngine.Random.Range(_roomBounds.min.x + minDistanceToSurface, _roomBounds.max.x - minDistanceToSurface),
                    UnityEngine.Random.Range(_roomBounds.min.y + minDistanceToSurface, _roomBounds.max.y - minDistanceToSurface),
                    UnityEngine.Random.Range(_roomBounds.min.z + minDistanceToSurface, _roomBounds.max.z - minDistanceToSurface)
                );
                if (!IsPositionInRoom(spawnPosition))
                {
                    // Reject points that are outside the room
                    continue;
                }

                LabelFilter filter = new LabelFilter(MRUKAnchor.SceneLabels.WALL_FACE);
                float closestDist = TryGetClosestSurfacePosition(spawnPosition, out Vector3 _, out MRUKAnchor _, filter);
                if (closestDist <= minDistanceToSurface)
                {
                    // Reject points that are too close to the walls
                    continue;
                }

                if (avoidVolumes && IsPositionInSceneVolume(spawnPosition, minDistanceToSurface))
                {
                    // Reject points inside volumes if avoid volumes has been enabled
                    continue;
                }

                return spawnPosition;
            }

            return null;
        }

        /// <summary>
        ///     Generate a position on any valid surface in the room, limited by the type
        ///     of surface and the classification of the object.
        /// </summary>
        /// <param name="surfaceTypes">
        ///     The type of surface by which to limit
        ///     the generation.
        /// </param>
        /// <param name="minDistanceToEdge">
        ///     Limit the generated point to
        ///     not being close to a surface's edges and corners.
        /// </param>
        /// <param name="labelFilter">The labels to include</param>
        /// <param name="position">
        ///     The generated position.
        ///     <see cref="Vector3.zero" /> is returned if no position was
        ///     generated.
        /// </param>
        /// <param name="normal">
        ///     The generated surface normal.
        ///     <see cref="Vector3.zero" /> is returned if nothing was
        ///     generated.
        /// </param>
        /// <returns>True if a position was found, false otherwise.</returns>
        public bool GenerateRandomPositionOnSurface(MRUK.SurfaceType surfaceTypes, float minDistanceToEdge, LabelFilter labelFilter, out Vector3 position, out Vector3 normal)
        {
            List<Surface> surfaces = new();
            float totalUsableSurfaceArea = 0f;
            float minWidth = 2f * minDistanceToEdge;

            // define these as the negative early exit conditions
            position = Vector3.zero;
            normal = Vector3.zero;

            foreach (var anchor in Anchors)
            {
                if (!labelFilter.PassesFilter(anchor.Label))
                {
                    continue;
                }

                if (anchor.PlaneRect.HasValue)
                {
                    bool skipPlane = false;
                    if (anchor.transform.forward.y >= Utilities.InvSqrt2)
                    {
                        if ((surfaceTypes & MRUK.SurfaceType.FACING_UP) == 0)
                        {
                            skipPlane = true;
                        }
                    }
                    else if (anchor.transform.forward.y <= -Utilities.InvSqrt2)
                    {
                        if ((surfaceTypes & MRUK.SurfaceType.FACING_DOWN) == 0)
                        {
                            skipPlane = true;
                        }
                    }
                    else if ((surfaceTypes & MRUK.SurfaceType.VERTICAL) == 0)
                    {
                        skipPlane = true;
                    }

                    if (!skipPlane)
                    {
                        var size = anchor.PlaneRect.Value.size;
                        if (size.x > minWidth && size.y > minWidth)
                        {
                            var usableArea = (size.x - minWidth) * (size.y - minWidth);
                            totalUsableSurfaceArea += usableArea;
                            surfaces.Add(new()
                            {
                                Anchor = anchor,
                                UsableArea = usableArea,
                                IsPlane = true,
                                Bounds = anchor.PlaneRect.Value,
                                Transform = anchor.transform.localToWorldMatrix
                            });
                        }
                    }
                }

                if (anchor.VolumeBounds.HasValue)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        Rect bounds;
                        Matrix4x4 faceTransform;
                        if (i == 0)
                        {
                            if ((surfaceTypes & MRUK.SurfaceType.FACING_UP) == 0)
                            {
                                continue;
                            }
                        }
                        else if (i == 1)
                        {
                            if ((surfaceTypes & MRUK.SurfaceType.FACING_DOWN) == 0)
                            {
                                continue;
                            }
                        }
                        else if ((surfaceTypes & MRUK.SurfaceType.VERTICAL) == 0)
                        {
                            continue;
                        }

                        switch (i)
                        {
                            case 0:
                                // +Z face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.x,
                                    xMax = anchor.VolumeBounds.Value.max.x,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, 0f, anchor.VolumeBounds.Value.max.z), Quaternion.identity, Vector3.one);
                                break;
                            case 1:
                                // -Z face
                                bounds = new()
                                {
                                    xMin = -anchor.VolumeBounds.Value.max.x,
                                    xMax = -anchor.VolumeBounds.Value.min.x,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, 0f, anchor.VolumeBounds.Value.min.z), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
                                break;
                            case 2:
                                // +X face
                                bounds = new()
                                {
                                    xMin = -anchor.VolumeBounds.Value.max.z,
                                    xMax = -anchor.VolumeBounds.Value.min.z,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(anchor.VolumeBounds.Value.max.x, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
                                break;
                            case 3:
                                // -X face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.z,
                                    xMax = anchor.VolumeBounds.Value.max.z,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(anchor.VolumeBounds.Value.min.x, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
                                break;
                            case 4:
                                // +Y face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.x,
                                    xMax = anchor.VolumeBounds.Value.max.x,
                                    yMin = -anchor.VolumeBounds.Value.max.z,
                                    yMax = -anchor.VolumeBounds.Value.min.z
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, anchor.VolumeBounds.Value.max.y, 0f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                                break;
                            case 5:
                                // -Y face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.x,
                                    xMax = anchor.VolumeBounds.Value.max.x,
                                    yMin = anchor.VolumeBounds.Value.min.z,
                                    yMax = anchor.VolumeBounds.Value.max.z
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, anchor.VolumeBounds.Value.min.y, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
                                break;
                            default:
                                throw new SwitchExpressionException();
                        }

                        var size = bounds.size;
                        if (size.x > minWidth && size.y > minWidth)
                        {
                            var usableArea = (size.x - minWidth) * (size.y - minWidth);
                            totalUsableSurfaceArea += usableArea;
                            surfaces.Add(new()
                            {
                                Anchor = anchor,
                                UsableArea = usableArea,
                                IsPlane = false,
                                Bounds = bounds,
                                Transform = anchor.transform.localToWorldMatrix * faceTransform
                            });
                        }
                    }
                }
            }

            if (surfaces.Count == 0)
            {
                return false;
            }

            const int maxIterations = 1000;
            for (int i = 0; i < maxIterations; ++i)
            {
                // Pick a random surface weighted by surface area (surfaces with a larger
                // area have more chance of being chosen)
                var rand = UnityEngine.Random.Range(0, totalUsableSurfaceArea);
                int index = 0;
                for (; index < surfaces.Count - 1; ++index)
                {
                    rand -= surfaces[index].UsableArea;
                    if (rand <= 0.0f)
                    {
                        break;
                    }
                }

                var surface = surfaces[index];
                var bounds = surface.Bounds;
                Vector2 pos = new Vector2(
                    UnityEngine.Random.Range(bounds.xMin + minDistanceToEdge, bounds.xMax - minDistanceToEdge),
                    UnityEngine.Random.Range(bounds.yMin + minDistanceToEdge, bounds.yMax - minDistanceToEdge)
                );

                if (surface.IsPlane && !surface.Anchor.IsPositionInBoundary(pos))
                {
                    continue;
                }

                position = surface.Transform.MultiplyPoint3x4(new Vector3(pos.x, pos.y, 0f));
                normal = surface.Transform.MultiplyVector(Vector3.forward);
                return true;
            }

            return false;
        }

        internal bool UpdateWorldLock(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;

            if (FloorAnchor == null)
            {
                return false;
            }

            var anchor = FloorAnchor.Anchor;
            var anchorCurrentTransform = FloorAnchor.transform;


            // If an anchor is not local then we should not try to locate it.
            // This will happen when loading a scene from Prefab or JSON string.
            if (anchor.Handle != 0 &&
                anchor.TryGetComponent<OVRLocatable>(out var locatable) &&
                locatable.TryGetSceneAnchorPose(out var pose) &&
                pose.Position.HasValue && pose.Rotation.HasValue)
            {
                var anchorNewTransform = Matrix4x4.TRS(pose.Position.Value, pose.Rotation.Value, Vector3.one);

                var adjustment = anchorCurrentTransform.localToWorldMatrix * anchorNewTransform.inverse;

                // Only use the Yaw component of the rotation, we don't want to introduce any errors with
                // pitch or roll.
                position = adjustment.GetPosition();
                rotation = adjustment.rotation;
                // Debug.LogError("Adjusting the rotation of the room (positon, rotation): " + position + rotation);
                return true;
            }

            return false;
        }

        void OnDestroy()
        {
            MRUK.Instance?.OnRoomDestroyed(this);
            AnchorCreatedEvent.RemoveAllListeners();
            AnchorRemovedEvent.RemoveAllListeners();
            AnchorUpdatedEvent.RemoveAllListeners();
        }

        /// <summary>
        ///     Creates an anchor in the specified room using the provided data and coordinate system.
        /// </summary>
        /// <param name="anchorData">The data for the anchor.</param>
        /// <returns>The created anchor.</returns>
        internal MRUKAnchor CreateAnchor(Data.AnchorData anchorData)
        {
            var anchorName = Utilities.GetAnchorName(anchorData);
            var anchorGO = new GameObject(anchorName);

            anchorGO.transform.SetParent(transform);
            anchorGO.transform.position = anchorData.Transform.Translation;
            anchorGO.transform.rotation = Quaternion.Euler(anchorData.Transform.Rotation);
            anchorGO.transform.localScale = anchorData.Transform.Scale;

            var createdAnchor = anchorGO.AddComponent<MRUKAnchor>();
            createdAnchor.Room = this;
            createdAnchor.Anchor = anchorData.Anchor;
            createdAnchor.UpdateAnchor(anchorData);

            Anchors.Add(createdAnchor);
            AnchorCreatedEvent.Invoke(createdAnchor);
            return createdAnchor;
        }

        /// <summary>
        ///     Compares the current MRUKRoom data to another room data. If all the anchors contained within it are
        ///     identical then this function returns true.
        /// </summary>
        /// <param name="roomData">The other room data.</param>
        /// <returns>True if the two rooms are identical, false otherwise.</returns>
        public bool IsIdenticalRoom(Data.RoomData roomData)
        {
            bool allAnchorsEqual = true;
            foreach (var anchor in Anchors)
            {
                bool anchorEqual = false;
                foreach (var anchorData in roomData.Anchors)
                {
                    if (anchor.Equals(anchorData))
                    {
                        anchorEqual = true;
                        break;
                    }
                }

                if (!anchorEqual)
                {
                    allAnchorsEqual = false;
                    break;
                }
            }

            return Anchor == roomData.Anchor && allAnchorsEqual && roomData.Anchors.Count == Anchors.Count;
        }

        /// <summary>
        ///     Checks to see if the room is the same. They are classed as the same room if any
        ///     of the anchors contained within it have the same UUID, even if some anchors may have
        ///     been added, removed or modified.
        /// </summary>
        /// <param name="roomData">The other room data.</param>
        /// <returns>True if the two rooms are the same, false otherwise.</returns>
        public bool IsSameRoom(Data.RoomData roomData)
        {
            foreach (var anchor in Anchors)
            {
                foreach (var anchorData in roomData.Anchors)
                {
                    if (anchor.Anchor == anchorData.Anchor)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
