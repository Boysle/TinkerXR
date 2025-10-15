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
using Newtonsoft.Json;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Provides a container for various data structures used in MR Utility Kit.
    /// </summary>
    [Feature(Feature.Scene)]
    public static class Data
    {
        /// <summary>
        /// Represents transformation data including translation, rotation, and scale.
        /// </summary>
        [Serializable]
        public struct TransformData
        {
            /// <summary>
            /// The translation vector representing position in three-dimensional space.
            /// </summary>
            [JsonProperty("Translation")] public Vector3 Translation;
            /// <summary>
            /// The rotation vector representing orientation in three-dimensional space.
            /// </summary>
            [JsonProperty("Rotation")] public Vector3 Rotation;
            /// <summary>
            /// The scale vector representing the size in three-dimensional space.
            /// </summary>
            [JsonProperty("Scale")] public Vector3 Scale;
        }

        /// <summary>
        /// Defines the 2D bounds of a plane using minimum and maximum vectors.
        /// </summary>
        [Serializable]
        public struct PlaneBoundsData
        {
            /// <summary>
            /// The minimum vector of the plane bounds.
            /// </summary>
            [JsonProperty("Min")] public Vector2 Min;
            /// <summary>
            /// The maximum vector of the plane bounds.
            /// </summary>
            [JsonProperty("Max")] public Vector2 Max;
        }

        /// <summary>
        /// Defines the 3D volume bounds using minimum and maximum vectors.
        /// </summary>
        [Serializable]
        public struct VolumeBoundsData
        {
            /// <summary>
            /// The minimum vector of the volume bounds.
            /// </summary>
            [JsonProperty("Min")] public Vector3 Min;
            /// <summary>
            /// The maximum vector of the volume bounds.
            /// </summary>
            [JsonProperty("Max")] public Vector3 Max;
        }

        /// <summary>
        /// Contains data related to an anchor including its UUID and associated properties.
        /// </summary>
        [Serializable]
        public struct AnchorData
        {
            /// <summary>
            /// The UUID of the anchor, used for serialization.
            /// </summary>
            ///<remarks> When serializing the Anchor, we only serialize its UUID. The handle is not preserved as this
            /// is only valid on the device where it is loaded and not across sessions.
            /// </remarks>
            [JsonProperty("UUID")] public OVRAnchor Anchor;
            /// <summary>
            /// A list of semantic classifications associated with the anchor.
            /// </summary>
            [JsonProperty("SemanticClassifications")]
            public List<String> SemanticClassifications;
            /// <summary>
            /// The transformation data associated with the anchor.
            /// </summary>
            [JsonProperty("Transform")] public TransformData Transform;
            /// <summary>
            /// Optional <see cref="PlaneBoundsData"/> associated with the anchor.
            /// </summary>
            [JsonProperty("PlaneBounds")] public PlaneBoundsData? PlaneBounds;
            /// <summary>
            /// Optional <see cref="VolumeBoundsData"/>  associated with the anchor.
            /// </summary>
            [JsonProperty("VolumeBounds")] public VolumeBoundsData? VolumeBounds;
            /// <summary>
            /// A list of 2D vectors representing the boundary of the plane.
            /// </summary>
            [JsonProperty("PlaneBoundary2D")] public List<Vector2> PlaneBoundary2D;
            /// <summary>
            /// Optional global <see cref="GlobalMeshData"/> associated with the anchor.
            /// </summary>
            [JsonProperty("GlobalMesh")] public GlobalMeshData? GlobalMesh;
        }

        /// <summary>
        /// Represents the mesh data of a global mesh anchor.
        /// </summary>
        [Serializable]
        public struct GlobalMeshData
        {
            /// <summary>
            /// Array of indices that define the vertices of the mesh.
            /// </summary>
            [JsonProperty("Positions")] public Vector3[] Positions;
            /// <summary>
            /// Array of indices that define the triangles of the mesh.
            /// </summary>
            [JsonProperty("Indices")] public int[] Indices;
        }

        /// <summary>
        /// Contains UUIDs for different components of a room layout such as floor, ceiling, and walls.
        /// </summary>
        [Serializable]
        public struct RoomLayoutData
        {
            /// <summary>
            /// The UUID of the floor component.
            /// </summary>
            [JsonProperty("FloorUuid")] public Guid FloorUuid;
            /// <summary>
            /// The UUID of the ceiling component.
            /// </summary>
            [JsonProperty("CeilingUuid")] public Guid CeilingUuid;
            /// <summary>
            /// A list of UUIDs for the walls.
            /// </summary>
            [JsonProperty("WallsUuid")] public List<Guid> WallsUuid;

        }

        /// <summary>
        /// Contains data related to a room including its layout and associated anchors.
        /// </summary>
        [Serializable]
        public struct RoomData
        {
            /// <summary>
            /// The UUID of the room anchor.
            /// </summary>
            /// <remarks> When serializing the Anchor, we only serialize its UUID. The handle is not preserved as this
            /// is only valid on the device where it is loaded and not across sessions.</remarks>
            [JsonProperty("UUID")] public OVRAnchor Anchor;
            /// <summary>
            /// The layout of the room, detailing the floor, ceiling, and walls.
            /// </summary>
            [JsonProperty("RoomLayout")] public RoomLayoutData RoomLayout;
            /// <summary>
            /// A list of rooms within the scene.
            /// </summary>
            [JsonProperty("Anchors")] public List<AnchorData> Anchors;
        }

        /// <summary>
        /// Represents the entire scene data including coordinate system and rooms.
        /// </summary>
        [Serializable]
        public struct SceneData
        {
            /// <summary>
            /// The coordinate system used in the scene.
            /// </summary>
            [JsonProperty("CoordinateSystem")] public SerializationHelpers.CoordinateSystem CoordinateSystem;
            /// <summary>
            /// A list of rooms within the scene.
            /// </summary>
            [JsonProperty("Rooms")] public List<RoomData> Rooms;
        }
    }


}
