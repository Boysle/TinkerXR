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

using System.Collections;
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Space Map generates a 2D top-down texture of the room’s layout.
    /// Parameters are exposed for complete RGBA control of the texture creation.
    /// </summary>
    [Feature(Feature.Scene)]
    public class SpaceMap : MonoBehaviour
    {
        [Tooltip("When the scene data is loaded, this controls what room(s) the prefabs will spawn in.")]
        public MRUK.RoomFilter CreateOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        // Instead of creating a texture all in code, we prefer an external texture
        // (this is so you can more easily link to the texture from the inspector, for example to use it in a material)
        // The trade-off for this simplicity is that the texture must have certain import settings, shown here
        // NOTE: You shouldn't be using a large texture size here for performance reasons.
        // Performance will be improved in future versions.
        [Tooltip(
            "Texture requirements: Read/Write enabled, RGBA 32 bit format. Texture suggestions: Wrap Mode = Clamped, size small (<128x128)")]
        public Texture2D TextureMap;

        Bounds MapBounds = new Bounds();

        /// <summary>
        /// The center of the texture. Note that Offset.y aligns with Unity's Z.
        /// </summary>
        public Vector2 Offset => new Vector2(MapBounds.center.x, MapBounds.center.z);

        /// <summary>
        /// The dimensions of the texture, including the MapBorder. Note that Scale.y aligns with Unity's Z.
        /// </summary>
        public Vector2 Scale => new Vector2(
            Mathf.Max(MapBounds.size.x, MapBounds.size.z) + MapBorder * 2,
            Mathf.Max(MapBounds.size.x, MapBounds.size.z) + MapBorder * 2);

        Color[,] Pixels;
        int PixelDimensions = 128;

        [Tooltip("The gradient of the generated map.")]
        public Gradient MapGradient = new();

        [Tooltip(
            "How far inside the room the left end of the Texture Gradient should appear. 0 is at the surface, negative is inside the room.")]
        public float InnerBorder = -0.5f;

        [Tooltip(
            "How far outside the room the right end of the Texture Gradient should appear. 0 is at the surface, positive is outside the room.")]
        public float OuterBorder = 0.0f;

        [Tooltip(
            "How much the texture map should extend from the room bounds, in meters. Should ideally be greater than or equal to outerPosition.")]
        public float MapBorder = 0.0f;

        const string MATERIAL_PROPERTY_NAME = "_SpaceMap";
        const string PARAMETER_PROPERTY_NAME = "_SpaceMapParams";


        private void Start()
        {
            if (MRUK.Instance && CreateOnStart != MRUK.RoomFilter.None)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(() =>
                {
                    switch (CreateOnStart)
                    {
                        case MRUK.RoomFilter.AllRooms:
                            CalculateMap();
                            break;
                        case MRUK.RoomFilter.CurrentRoomOnly:
                            CalculateMap(MRUK.Instance.GetCurrentRoom());
                            break;
                    }
                });
            }
        }

        /// <summary>
        /// Calculates the space map for the specified room or all rooms if no room is specified.
        /// Initializes map values, starts the pixel calculation process, and updates global shader properties.
        /// </summary>
        /// <param name="room">The room to calculate the map for. If null, calculates for all rooms.</param>
        public void CalculateMap(MRUKRoom room = null)
        {
            if (TextureMap == null)
            {
                Debug.LogWarning("No texture specified for Space Map");
                return;
            }

            // one-time startup behavior
            InitializeMapValues(room);

            // We may want to do this in chunks, to avoid a loading hitch as this gets more complex
            // TODO: send event when texture is done being calculated
            StartCoroutine(CalculatePixels(room));

            Shader.SetGlobalTexture(MATERIAL_PROPERTY_NAME, TextureMap);
            Shader.SetGlobalVector(PARAMETER_PROPERTY_NAME, new Vector4(Scale.x, Scale.y, Offset.x, Offset.y));
        }

        void InitializeMapValues(MRUKRoom room)
        {
            PixelDimensions = TextureMap.width;
            Pixels = new Color[PixelDimensions, PixelDimensions];

            if (room != null)
            {
                MapBounds = room.GetRoomBounds();
            }
            else
            {
                MapBounds = new();
                foreach (var currentRoom in MRUK.Instance.Rooms)
                {
                    MapBounds.Encapsulate(currentRoom.GetRoomBounds());
                }
            }

            transform.position = new Vector3(MapBounds.center.x, MapBounds.min.y, MapBounds.center.z);
            transform.localScale = new Vector3(Scale.x, MapBounds.size.y, Scale.y);
        }

        /// <summary>
        ///  A surface-relative value of how far a position is from all SceneAPI surfaces
        ///  negative values are behind the surface i.e. outside of the room or inside a volume
        /// </summary>
        /// <param name="room">The room to check the surface distance in.</param>
        /// <param name="worldPosition">The world position to check.</param>
        /// <returns>The distance to the nearest surface.</returns>
        public float GetSurfaceDistance(MRUKRoom room, Vector3 worldPosition)
        {
            float closestDist = Mathf.Infinity;
            float sign = 1f;
            if (room != null)
            {
                closestDist = room.TryGetClosestSurfacePosition(worldPosition, out Vector3 closestPos, out MRUKAnchor closestAnchor,
                    new LabelFilter(~(MRUKAnchor.SceneLabels.FLOOR | MRUKAnchor.SceneLabels.CEILING)));
                sign = room.IsPositionInRoom(worldPosition, false) ? 1 : -1;
            }
            else
            {
                foreach (var currentRoom in MRUK.Instance.Rooms)
                {
                    var dist = currentRoom.TryGetClosestSurfacePosition(worldPosition, out Vector3 closestPos, out MRUKAnchor closestAnchor,
                        new LabelFilter(~(MRUKAnchor.SceneLabels.FLOOR | MRUKAnchor.SceneLabels.CEILING)));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        sign = currentRoom.IsPositionInRoom(worldPosition) ? 1 : -1;
                    }
                }
            }

            float surfaceDistance = closestDist * sign;
            return surfaceDistance;
        }

        IEnumerator CalculatePixels(MRUKRoom room)
        {
            float halfPixel = 0.5f / PixelDimensions;
            float largestDimension = Mathf.Max(MapBounds.size.x, MapBounds.size.z) + MapBorder * 2;
            for (int x = 0; x < PixelDimensions; x++)
            {
                for (int y = 0; y < PixelDimensions; y++)
                {
                    // convert texel coordinate to world position
                    float xWorld = x / (float)PixelDimensions - 0.5f + halfPixel;
                    float yWorld = y / (float)PixelDimensions - 0.5f + halfPixel;
                    Vector3 worldPos = new Vector3(xWorld * largestDimension + MapBounds.center.x, 0, yWorld * largestDimension + MapBounds.center.z);
                    float distToSurface = -GetSurfaceDistance(room, worldPos);
                    float normalizedDist = Mathf.Clamp01((distToSurface - InnerBorder) / (OuterBorder - InnerBorder));
                    Color averageColor = MapGradient.Evaluate(normalizedDist);
                    Pixels[x, y] = averageColor;
                    TextureMap.SetPixel(x, y, averageColor);
                }
            }

            TextureMap.Apply();

            yield return null;
        }

        // <summary>
        /// Resets the pixel colors of the space map to black, effectively clearing any previously calculated data.
        /// </summary>
        public void ResetFreespace()
        {
            for (int x = 0; x < PixelDimensions; x++)
            {
                for (int y = 0; y < PixelDimensions; y++)
                {
                    Pixels[x, y] = Color.black;
                }
            }
        }

        /// <summary>
        /// Color clamps to edge color if worldPosition is off-grid.
        /// getBilinear blends the color between pixels.
        /// </summary>
        /// <param name="worldPosition">The world position to get the color for.</param>
        /// <param name="getBilinear">If true, blends the color between pixels. If false, clamps to the nearest pixel color.</param>
        /// <returns>The color at the specified position.</returns>
        public Color GetColorAtPosition(Vector3 worldPosition, bool getBilinear = true)
        {
            // GetPixelBilinear requires UV coordinates (0...1)
            // GetPixel requires pixel counts (0...pixelDimensions)
            if (getBilinear)
            {
                Vector2 UVcoords = GetPixelFromWorldPosition(worldPosition, true);
                return TextureMap.GetPixelBilinear(UVcoords.x, UVcoords.y);
            }
            else
            {
                Vector2 pixelCoords = GetPixelFromWorldPosition(worldPosition);
                int x = Mathf.FloorToInt(pixelCoords.x);
                int y = Mathf.FloorToInt(pixelCoords.y);
                return TextureMap.GetPixel(x, y);
            }
        }

        Vector2 GetPixelFromWorldPosition(Vector3 worldPosition, bool normalizedUV = false)
        {
            Vector3 localSpace = worldPosition - MapBounds.center;
            Vector2 scaledSpace = new Vector2(localSpace.x / MapBounds.size.x + 0.5f, localSpace.z / MapBounds.size.z + 0.5f);
            if (!normalizedUV)
            {
                scaledSpace *= PixelDimensions;
            }

            return scaledSpace;
        }
    }
}
