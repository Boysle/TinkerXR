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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    ///     This contains helpers to serialize/deserialze Scene data to/from JSON
    /// </summary>
    [Feature(Feature.Scene)]
    public static class SerializationHelpers
    {
        /// <summary>
        /// Defines the coordinate systems that can be used for serialization.
        /// </summary>
        [Serializable, JsonConverter(typeof(StringEnumConverter))]
        public enum CoordinateSystem
        {
            Unity,
            Unreal,
        }

        private class GuidConverter : JsonConverter<Guid>
        {
            public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString("N").ToUpper());
            }

            public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var uuidStr = (string)reader.Value;
                return uuidStr != null ? Guid.Parse(uuidStr) : Guid.Empty;
            }
        }

        private class OVRAnchorConverter : JsonConverter<OVRAnchor>
        {
            public override void WriteJson(JsonWriter writer, OVRAnchor value, JsonSerializer serializer)
            {
                writer.WriteValue(value.Uuid.ToString("N").ToUpper());
            }

            public override OVRAnchor ReadJson(JsonReader reader, Type objectType, OVRAnchor existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var uuidStr = (string)reader.Value;
                return new OVRAnchor(0, uuidStr != null ? Guid.Parse(uuidStr) : Guid.Empty);
            }
        }

        private class Vector2Converter : JsonConverter<Vector2>
        {
            public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                // Disable indentation to make it more compact
                var prevFormatting = writer.Formatting;
                writer.Formatting = Formatting.None;
                writer.WriteValue(value.x);
                writer.WriteValue(value.y);
                writer.WriteEndArray();
                writer.Formatting = prevFormatting;
            }

            public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                Vector2 result = new()
                {
                    x = (float)reader.ReadAsDouble(),
                    y = (float)reader.ReadAsDouble()
                };
                reader.Read();
                if (reader.TokenType != JsonToken.EndArray)
                {
                    throw new Exception("Expected end of array");
                }

                return result;
            }
        }

        private class Vector3Converter : JsonConverter<Vector3>
        {
            public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                // Disable indentation to make it more compact
                var prevFormatting = writer.Formatting;
                writer.Formatting = Formatting.None;
                writer.WriteValue(value.x);
                writer.WriteValue(value.y);
                writer.WriteValue(value.z);
                writer.WriteEndArray();
                writer.Formatting = prevFormatting;
            }

            public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                Vector3 result = new()
                {
                    x = (float)reader.ReadAsDouble(),
                    y = (float)reader.ReadAsDouble(),
                    z = (float)reader.ReadAsDouble()
                };
                reader.Read();
                if (reader.TokenType != JsonToken.EndArray)
                {
                    throw new Exception("Expected end of array");
                }

                return result;
            }
        }

        private class IntArrayConverter : JsonConverter<int[]>
        {
            public override void WriteJson(JsonWriter writer, int[] value, JsonSerializer serializer)
            {
                int[] array = value;
                writer.WriteStartArray();
                var prevFormatting = writer.Formatting;
                writer.Formatting = Formatting.None;
                foreach (var item in array)
                {
                    writer.WriteValue(item);
                }

                writer.WriteEndArray();
                writer.Formatting = prevFormatting;
            }

            public override int[] ReadJson(JsonReader reader, Type objectType, int[] existingValue, bool hasExistingValue, JsonSerializer serial)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    var list = new System.Collections.Generic.List<int>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndArray)
                        {
                            return list.ToArray();
                        }

                        list.Add((int)(long)reader.Value);
                    }
                }

                throw new JsonReaderException("Expected start of array.");
            }
        }

        private class Vector3ArrayConverter : JsonConverter<Vector3[]>
        {
            public override void WriteJson(JsonWriter writer, Vector3[] value, JsonSerializer serializer)
            {
                Vector3[] array = value;
                writer.WriteStartArray();
                var prevFormatting = writer.Formatting;
                writer.Formatting = Formatting.None;
                foreach (var item in array)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(item.x);
                    writer.WriteValue(item.y);
                    writer.WriteValue(item.z);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
                writer.Formatting = prevFormatting;
            }

            public override Vector3[] ReadJson(JsonReader reader, Type objectType, Vector3[] existingValue, bool hasExistingValue, JsonSerializer serial)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    var list = new System.Collections.Generic.List<Vector3>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndArray)
                        {
                            return list.ToArray();
                        }

                        Vector3 result = new()
                        {
                            x = (float)reader.ReadAsDouble(),
                            y = (float)reader.ReadAsDouble(),
                            z = (float)reader.ReadAsDouble()
                        };
                        reader.Read();
                        if (reader.TokenType != JsonToken.EndArray)
                        {
                            throw new Exception("Expected end of array");
                        }

                        list.Add(result);
                    }
                }

                throw new JsonReaderException("Expected start of array.");
            }
        }

        public const float UnrealWorldToMeters = 100f; // The conversion factor from Unreal Engine units to Unity.

        private static List<JsonConverter> _converters = new()
        {
            new GuidConverter(),
            new OVRAnchorConverter(),
            new Vector2Converter(),
            new Vector3Converter(),
            new IntArrayConverter(),
            new Vector3ArrayConverter()
        };

        /// <summary>
        ///     Serializes the scene data into a JSON string. The scene data includes rooms, anchors, and their associated properties.
        ///     The method allows for the specification of the coordinate system (Unity or Unreal) and whether to include the global mesh data.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system to use for the serialization (Unity or Unreal).</param>
        /// <param name="includeGlobalMesh">A boolean indicating whether to include the global mesh data in the serialization. Default is true.</param>
        /// <param name="rooms">A list of rooms to serialize, if this is null then all rooms will be serialized.</param>
        /// <returns>A JSON string representing the serialized scene data.</returns>
        public static string Serialize(CoordinateSystem coordinateSystem = CoordinateSystem.Unity, bool includeGlobalMesh = true, List<MRUKRoom> rooms = null)
        {
            Data.SceneData sceneData = new()
            {
                CoordinateSystem = coordinateSystem,
                Rooms = new()
            };

            foreach (var room in rooms ?? MRUK.Instance.Rooms)
            {
                Data.RoomData roomData = new()
                {
                    Anchor = room.Anchor,
                    RoomLayout = new()
                    {
                        WallsUuid = new()
                    },
                    Anchors = new(),
                };

                foreach (var anchor in room.Anchors)
                {
                    Data.AnchorData anchorData = new()
                    {
                        Anchor = anchor.Anchor
                    };


                    if (anchor == room.CeilingAnchor)
                    {
                        roomData.RoomLayout.CeilingUuid = anchorData.Anchor.Uuid;
                    }

                    if (anchor == room.FloorAnchor)
                    {
                        roomData.RoomLayout.FloorUuid = anchorData.Anchor.Uuid;
                    }


                    if (room.WallAnchors.Contains(anchor))
                    {
                        roomData.RoomLayout.WallsUuid.Add(anchorData.Anchor.Uuid);
                    }

                    anchorData.SemanticClassifications = Utilities.SceneLabelsEnumToList(anchor.Label);
                    anchorData.Transform = new();
                    var localPosition = anchor.transform.position;
                    var localRotation = anchor.transform.eulerAngles;
                    if (coordinateSystem == CoordinateSystem.Unreal)
                    {
                        localPosition = new Vector3(localPosition.z * UnrealWorldToMeters, localPosition.x * UnrealWorldToMeters, localPosition.y * UnrealWorldToMeters);
                        localRotation = new Vector3(localRotation.x, 180f + localRotation.y, localRotation.z);
                    }

                    anchorData.Transform.Translation = localPosition;
                    anchorData.Transform.Rotation = localRotation;
                    anchorData.Transform.Scale = anchor.transform.localScale;
                    if (anchor.PlaneRect.HasValue)
                    {
                        var min = anchor.PlaneRect.Value.min;
                        var max = anchor.PlaneRect.Value.max;
                        if (coordinateSystem == CoordinateSystem.Unreal)
                        {
                            anchorData.PlaneBounds = new Data.PlaneBoundsData()
                            {
                                Min = new Vector2(-max.x * UnrealWorldToMeters, min.y * UnrealWorldToMeters),
                                Max = new Vector2(-min.x * UnrealWorldToMeters, max.y * UnrealWorldToMeters),
                            };
                        }
                        else
                        {
                            anchorData.PlaneBounds = new Data.PlaneBoundsData()
                            {
                                Min = min,
                                Max = max,
                            };
                        }
                    }

                    if (anchor.PlaneBoundary2D != null)
                    {
                        anchorData.PlaneBoundary2D = new();
                        anchorData.PlaneBoundary2D.Capacity = anchor.PlaneBoundary2D.Count;
                        if (coordinateSystem == CoordinateSystem.Unreal)
                        {
                            foreach (var p in anchor.PlaneBoundary2D)
                            {
                                anchorData.PlaneBoundary2D.Add(new Vector2(-p.x * UnrealWorldToMeters, p.y * UnrealWorldToMeters));
                            }

                            anchorData.PlaneBoundary2D.Reverse();
                        }
                        else
                        {
                            anchorData.PlaneBoundary2D = anchor.PlaneBoundary2D;
                        }
                    }

                    if (anchor.VolumeBounds.HasValue)
                    {
                        var min = anchor.VolumeBounds.Value.min;
                        var max = anchor.VolumeBounds.Value.max;
                        if (coordinateSystem == CoordinateSystem.Unreal)
                        {
                            anchorData.VolumeBounds = new Data.VolumeBoundsData()
                            {
                                Min = new Vector3(-max.z * UnrealWorldToMeters, min.x * UnrealWorldToMeters, min.y * UnrealWorldToMeters),
                                Max = new Vector3(-min.z * UnrealWorldToMeters, max.x * UnrealWorldToMeters, max.y * UnrealWorldToMeters),
                            };
                        }
                        else
                        {
                            anchorData.VolumeBounds = new Data.VolumeBoundsData()
                            {
                                Min = min,
                                Max = max,
                            };
                        }
                    }

                    Mesh globalMesh = anchor.GlobalMesh;
                    if (includeGlobalMesh && globalMesh)
                    {
                        Vector3[] vertices = globalMesh.vertices;
                        int[] triangles = globalMesh.triangles;
                        if (coordinateSystem == CoordinateSystem.Unreal)
                        {
                            for (int i = 0; i < vertices.Length; i++)
                            {
                                var vert = vertices[i];
                                vertices[i] = new Vector3(-vert.z, -vert.x, vert.y);
                            }

                            Array.Reverse(triangles);
                        }
                        else
                        {
                            vertices = globalMesh.vertices;
                            triangles = globalMesh.triangles;
                        }

                        anchorData.GlobalMesh = new Data.GlobalMeshData()
                        {
                            Positions = vertices,
                            Indices = triangles
                        };
                    }

                    roomData.Anchors.Add(anchorData);
                }

                sceneData.Rooms.Add(roomData);
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Formatting = Formatting.Indented;
            settings.Converters = _converters;
            string json = JsonConvert.SerializeObject(sceneData, settings);

            return json;
        }

        /// <summary>
        ///     Deserializes a JSON string into a list of MRUKRoom objects.
        /// </summary>
        /// <param name="json">The JSON string representing the serialized scene data.</param>
        /// <returns>A list of MRUKRoom objects representing the deserialized scene data.</returns>
        public static Data.SceneData Deserialize(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Converters = _converters;
            var sceneData = JsonConvert.DeserializeObject<Data.SceneData>(json, settings);
            for (int i = 0; i < sceneData.Rooms.Count; ++i)
            {
                var room = sceneData.Rooms[i];
                for (int j = 0; j < room.Anchors.Count; ++j)
                {
                    var anchor = room.Anchors[j];
                    // If the JSON is encoded using the Unreal coordinate system then we should convert it to Unity
                    // to avoid needing to deal with it differently when it is being used.
                    if (sceneData.CoordinateSystem == CoordinateSystem.Unreal)
                    {
                        anchor.Transform.Translation = new Vector3(
                            anchor.Transform.Translation.y / UnrealWorldToMeters,
                            anchor.Transform.Translation.z / UnrealWorldToMeters,
                            anchor.Transform.Translation.x / UnrealWorldToMeters);
                        anchor.Transform.Rotation = new Vector3(anchor.Transform.Rotation.x,
                            180 + anchor.Transform.Rotation.y, anchor.Transform.Rotation.z);

                        if (anchor.VolumeBounds != null)
                        {
                            anchor.VolumeBounds = new Data.VolumeBoundsData()
                            {
                                Min = new Vector3(anchor.VolumeBounds.Value.Min.y / UnrealWorldToMeters, anchor.VolumeBounds.Value.Min.z / UnrealWorldToMeters,
                                    -anchor.VolumeBounds.Value.Max.x / UnrealWorldToMeters),
                                Max = new Vector3(anchor.VolumeBounds.Value.Max.y / UnrealWorldToMeters, anchor.VolumeBounds.Value.Max.z / UnrealWorldToMeters,
                                    -anchor.VolumeBounds.Value.Min.x / UnrealWorldToMeters),
                            };
                        }

                        if (anchor.PlaneBounds != null)
                        {
                            anchor.PlaneBounds = new Data.PlaneBoundsData()
                            {
                                Min = new Vector2(-anchor.PlaneBounds.Value.Max.x / UnrealWorldToMeters, anchor.PlaneBounds.Value.Min.y / UnrealWorldToMeters),
                                Max = new Vector2(-anchor.PlaneBounds.Value.Min.x / UnrealWorldToMeters, anchor.PlaneBounds.Value.Max.y / UnrealWorldToMeters),
                            };
                        }

                        if (anchor.PlaneBoundary2D != null)
                        {
                            for (int k = 0; k < anchor.PlaneBoundary2D.Count; ++k)
                            {
                                var p = anchor.PlaneBoundary2D[k];
                                anchor.PlaneBoundary2D[k] = new Vector2(-p.x / UnrealWorldToMeters, p.y / UnrealWorldToMeters);
                            }

                            anchor.PlaneBoundary2D.Reverse();
                        }

                        if (anchor.GlobalMesh != null)
                        {
                            for (int k = 0; k < anchor.GlobalMesh.Value.Positions.Length; k++)
                            {
                                var vert = anchor.GlobalMesh.Value.Positions[k];
                                anchor.GlobalMesh.Value.Positions[k] = new Vector3(-vert.y, vert.z, -vert.x);
                            }

                            Array.Reverse(anchor.GlobalMesh.Value.Indices);
                        }
                    }

                    room.Anchors[j] = anchor;
                }


                sceneData.Rooms[i] = room;
            }

            // We converted the data to Unity coordinate system above
            sceneData.CoordinateSystem = CoordinateSystem.Unity;
            return sceneData;
        }
    }
}
