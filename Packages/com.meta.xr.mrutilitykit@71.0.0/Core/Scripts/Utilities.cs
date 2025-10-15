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
using System.Collections.Generic;
using Meta.XR.MRUtilityKit.Extensions;
using UnityEngine.Rendering;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Provides utility functions for various operations within the MR Utility Kit.
    /// </summary>
    public static class Utilities
    {
        private static Dictionary<GameObject, Bounds?> prefabBoundsCache = new();

        public static readonly float Sqrt2 = Mathf.Sqrt(2f);  // Square root of 2, commonly used in mathematical calculations.
        public static readonly float InvSqrt2 = 1f / Mathf.Sqrt(2f); // Inverse of the square root of 2.

        /// <summary>
        /// Retrieves the bounds of a prefab, calculating them if not already cached.
        /// </summary>
        /// <param name="prefab">The prefab GameObject to calculate bounds for.</param>
        /// <returns>The bounds of the prefab, or null if no Renderer is found.</returns>
        public static Bounds? GetPrefabBounds(GameObject prefab)
        {
            if (prefabBoundsCache.TryGetValue(prefab, out Bounds? cachedBounds))
            {
                return cachedBounds;
            }

            Bounds? bounds = CalculateBoundsRecursively(prefab.transform);
            prefabBoundsCache.Add(prefab, bounds);
            return bounds;
        }

        static Bounds? CalculateBoundsRecursively(Transform transform)
        {
            Bounds? bounds = null;
            Renderer renderer = transform.GetComponent<Renderer>();

            if (renderer != null && renderer.bounds.size != Vector3.zero)
            {
                // If the current GameObject has a renderer component, include its bounds
                bounds = renderer.bounds;
            }

            // Recursively process children
            foreach (Transform child in transform.transform)
            {
                Bounds? childBounds = CalculateBoundsRecursively(child);
                if (childBounds != null)
                {
                    if (bounds != null)
                    {
                        var boundsValue = bounds.Value;
                        boundsValue.Encapsulate(childBounds.Value);
                        bounds = boundsValue;
                    }
                    else
                    {
                        bounds = childBounds;
                    }
                }
            }

            return bounds;
        }

        /// <summary>
        /// Gets the name of an anchor based on its semantic classification.
        /// </summary>
        /// <param name="anchorData">The Data.AnchorData object representing the anchor.</param>
        /// <returns>The name of the anchor, or "UNDEFINED_ANCHOR" if no semantic classification is available.</returns>
        public static string GetAnchorName(Data.AnchorData anchorData)
        {
            return anchorData.SemanticClassifications.Count != 0
                ? anchorData.SemanticClassifications[0]
                : "UNDEFINED_ANCHOR";
        }

        internal static Rect? GetPlaneRectFromAnchorData(Data.AnchorData data)
        {
            if (data.PlaneBounds == null)
            {
                return null;
            }

            return new Rect(data.PlaneBounds.Value.Min, data.PlaneBounds.Value.Max - data.PlaneBounds.Value.Min);
        }

        internal static Bounds? GetVolumeBoundsFromAnchorData(Data.AnchorData data)
        {
            if (data.VolumeBounds == null)
            {
                return null;
            }

            Vector3 volumeBoundsMin = data.VolumeBounds.Value.Min;
            Vector3 volumeBoundsMax = data.VolumeBounds.Value.Max;
            Vector3 volumeBoundsCenterOffset = (volumeBoundsMin + volumeBoundsMax) * 0.5f;
            return new Bounds(volumeBoundsCenterOffset, volumeBoundsMax - volumeBoundsMin);
        }

        internal static Mesh GetGlobalMeshFromAnchorData(Data.AnchorData data)
        {
            if (data.GlobalMesh == null)
            {
                return null;
            }

            var newMesh = new Mesh();
            newMesh.indexFormat = data.GlobalMesh.Value.Indices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            newMesh.vertices = data.GlobalMesh.Value.Positions;
            newMesh.triangles = data.GlobalMesh.Value.Indices;

            return newMesh;
        }

        internal static void DestroyGameObjectAndChildren(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            foreach (Transform child in gameObject.transform)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(gameObject.gameObject);
        }

        /// <summary>
        /// Compares two lists for equality, checking if they contain the same elements in the same order.
        /// This method replaces the LINQ dependency.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists.</typeparam>
        /// <param name="list1">The first list to compare.</param>
        /// <param name="list2">The second list to compare.</param>
        /// <returns>True if the lists are equal, false otherwise.</returns>
        public static bool SequenceEqual<T>(this List<T> list1, List<T> list2)
        {
            if (list1 == null && list2 == null)
            {
                return true;
            }

            if (list1 == null && list2 != null)
            {
                return false;
            }

            if (list1 != null && list2 == null)
            {
                return false;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (!Equals(list1[i], list2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a given position is inside a polygon defined by a list of vertices.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <param name="polygon">The list of vertices defining the polygon.</param>
        /// <returns>True if the position is inside the polygon, false otherwise.</returns>
        public static bool IsPositionInPolygon(Vector2 position, List<Vector2> polygon)
        {
            int lineCrosses = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[(i + 1) % polygon.Count];

                if (position.y > Mathf.Min(p1.y, p2.y) && position.y <= Mathf.Max(p1.y, p2.y))
                {
                    if (position.x <= Mathf.Max(p1.x, p2.x))
                    {
                        if (p1.y != p2.y)
                        {
                            var frac = (position.y - p1.y) / (p2.y - p1.y);
                            var xIntersection = p1.x + frac * (p2.x - p1.x);
                            if (p1.x == p2.x || position.x <= xIntersection)
                            {
                                lineCrosses++;
                            }
                        }
                    }
                }
            }

            return (lineCrosses % 2) == 1;
        }

        internal static List<string> SceneLabelsEnumToList(MRUKAnchor.SceneLabels labelFlags)
        {
            var result = new List<string>(1);
            foreach (MRUKAnchor.SceneLabels label in Enum.GetValues(typeof(MRUKAnchor.SceneLabels)))
            {
                if ((labelFlags & label) != 0)
                {
                    result.Add(label.ToString());
                }
            }

            return result;
        }


        internal static MRUKAnchor.SceneLabels StringLabelsToEnum(IList<string> labels)
        {
            MRUKAnchor.SceneLabels result = 0;
            foreach (string label in labels)
            {
                result |= StringLabelToEnum(label);
            }

            return result;
        }

        internal static MRUKAnchor.SceneLabels StringLabelToEnum(string stringLabel)
        {
            var classification = OVRSemanticLabels.FromApiLabel(stringLabel);
            if (stringLabel != "OTHER" && classification == OVRSemanticLabels.Classification.Other)
            {
                Debug.LogError($"Unknown scene label: {stringLabel}");
            }

            return ClassificationToSceneLabel(classification);
        }

        internal static MRUKAnchor.SceneLabels ClassificationToSceneLabel(
            OVRSemanticLabels.Classification classification)
        {
            // MRUKAnchor.SceneLabels enum is defined by bit-shifting the OVRSemanticLabels.Classification int values
            // So we can also do this conversion at runtime
            int bitShift = (int)classification;
            return (MRUKAnchor.SceneLabels)(1 << bitShift);
        }

        internal static void DrawWireSphere(Vector3 center, float radius, Color color, float duration, int quality = 3)
        {
            quality = Mathf.Clamp(quality, 1, 10);

            int segments = quality << 2;
            int subdivisions = quality << 3;
            int halfSegments = segments >> 1;
            float strideAngle = 360F / subdivisions;
            float segmentStride = 180F / segments;

            Vector3 first;
            Vector3 next;
            for (int i = 0; i < segments; i++)
            {
                first = (Vector3.forward * radius);
                first = Quaternion.AngleAxis(segmentStride * (i - halfSegments), Vector3.right) * first;

                for (int j = 0; j < subdivisions; j++)
                {
                    next = Quaternion.AngleAxis(strideAngle, Vector3.up) * first;
                    UnityEngine.Debug.DrawLine(first + center, next + center, color, duration);
                    first = next;
                }
            }

            Vector3 axis;
            for (int i = 0; i < segments; i++)
            {
                first = (Vector3.forward * radius);
                first = Quaternion.AngleAxis(segmentStride * (i - halfSegments), Vector3.up) * first;
                axis = Quaternion.AngleAxis(90F, Vector3.up) * first;

                for (int j = 0; j < subdivisions; j++)
                {
                    next = Quaternion.AngleAxis(strideAngle, axis) * first;
                    UnityEngine.Debug.DrawLine(first + center, next + center, color, duration);
                    first = next;
                }
            }
        }

    }

    internal struct Float3X3
    {
        private Vector3 Row0;
        private Vector3 Row1;
        private Vector3 Row2;

        internal Float3X3(Vector3 row0, Vector3 row1, Vector3 row2)
        {
            Row0 = row0;
            Row1 = row1;
            Row2 = row2;
        }

        internal Float3X3(float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
        {
            Row0 = new Vector3(m00, m01, m02);
            Row1 = new Vector3(m10, m11, m12);
            Row2 = new Vector3(m20, m21, m22);
        }

        internal static Float3X3 Multiply(Float3X3 a, Float3X3 b)
        {
            Float3X3 result = new Float3X3();
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    result[i, j] = a[i, 0] * b[0, j] + a[i, 1] * b[1, j] + a[i, 2] * b[2, j];
                }
            }

            return result;
        }

        internal static Vector3 Multiply(Float3X3 a, Vector3 b)
        {
            return new Vector3(Vector3.Dot(a.Row0, b),
                Vector3.Dot(a.Row1, b),
                Vector3.Dot(a.Row2, b));
        }

        private float this[int row, int column]
        {
            get
            {
                switch (row)
                {
                    case 0: return Row0[column];
                    case 1: return Row1[column];
                    case 2: return Row2[column];
                    default: throw new IndexOutOfRangeException("Row index out of range: " + row);
                }
            }
            set
            {
                switch (row)
                {
                    case 0:
                        Row0[column] = value;
                        break;
                    case 1:
                        Row1[column] = value;
                        break;
                    case 2:
                        Row2[column] = value;
                        break;
                    default: throw new IndexOutOfRangeException("Row index out of range: " + row);
                }
            }
        }
    }

    internal static class WorleyNoise
    {
        private const float K = 0.142857142857f; // 1/7
        private const float Ko = 0.428571428571f; // 3/7
        private const float jitter = 1.0f; // Less gives more regular pattern

        private static Vector2 mod289(Vector2 v)
        {
            return new Vector2(v.x - Mathf.Floor((v.x * (1.0f / 289.0f)) * 289.0f),
                v.y - Mathf.Floor((v.y * (1.0f / 289.0f)) * 289.0f));
        }

        private static Vector3 mod289(Vector3 v)
        {
            return new Vector3(v.x - Mathf.Floor((v.x * (1.0f / 289.0f)) * 289.0f),
                v.y - Mathf.Floor((v.y * (1.0f / 289.0f)) * 289.0f),
                v.z - Mathf.Floor((v.z * (1.0f / 289.0f)) * 289.0f));
        }

        private static Vector3 permute(Vector3 x)
        {
            return mod289(Vector3.Scale(new Vector3(x.x * 34.0f + 1, x.y * 34.0f + 1, x.z * 34.0f + 1), x));
        }

        private static float mod7(float v)
        {
            return v - Mathf.Floor(v / 7.0f) * 7.0f;
        }

        private static Vector3 mod7(Vector3 v)
        {
            return new Vector3(v.x - Mathf.Floor(v.x / 7.0f) * 7.0f,
                v.y - Mathf.Floor(v.y / 7.0f) * 7.0f,
                v.z - Mathf.Floor(v.z / 7.0f) * 7.0f);
        }

        internal static Vector2 cellular(Vector2 P)
        {
            const float K = 0.142857142857f; // 1/7
            const float Ko = 0.428571428571f; // 3/7
            const float jitter = 1.0f; // Less gives more regular pattern

            var Pi = mod289(P.Floor());
            var Pf = P - P.Floor();
            var oi = new Vector3(-1.0f, 0.0f, 1.0f);
            var of = new Vector3(-0.5f, 0.5f, 1.5f);
            var px = permute(oi.Add(Pi.x));
            var p = permute(oi.Add(px.x).Add(Pi.y)); // p11, p12, p13
            var ox = mod289(p * K).Subtract(Ko);
            var _mod7 = mod7(p * K);
            var oy = (_mod7.Floor() * K).Subtract(Ko);
            var dx = ox * (Pf.x + 0.5f + jitter);
            var dy = of.Subtract(Pf.y) + jitter * oy;
            var d1 = Vector3.Scale(dx, dx) + Vector3.Scale(dy, dy); // d11, d12 and d13, squared
            p = permute(oi.Add(px.y + Pi.y)); // p21, p22, p23
            ox = mod289(p * K).Subtract(Ko);
            _mod7 = mod7(p * K);
            oy = (_mod7.Floor() * K).Subtract(Ko);
            dx = ox * (Pf.x - 0.5f + jitter);
            dy = Vector3.Scale(oy, of.Subtract(Pf.y)).Add(jitter);
            var d2 = Vector3.Scale(dx, dx) + Vector3.Scale(dy, dy); // d21, d22 and d23, squared
            p = permute(oi.Add(px.z + Pi.y)); // p31, p32, p33
            ox = mod289(p * K).Subtract(Ko);
            oy = mod7(p.Floor() * K * K).Subtract(Ko);
            dx = ox * (Pf.x - 1.5f + jitter);
            dy = Vector3.Scale(oy, of.Subtract(Pf.y).Add(jitter));
            var d3 = Vector3.Scale(dx, dx) + Vector3.Scale(dy, dy); // d31, d32 and d33, squared
            // Sort out the two smallest distances (F1, F2)
            var d1a = Vector3.Min(d1, d2);
            d2 = Vector3.Max(d1, d2); // Swap to keep candidates for F2
            d2 = Vector3.Min(d2, d3); // neither F1 nor F2 are now in d3
            d1 = Vector3.Min(d1a, d2); // F1 is now in d1
            d2 = Vector3.Max(d1a, d2); // Swap to keep candidates for F2
            d1.x = (d1.x < d1.y) ? d1.x : d1.y; // Swap if smaller
            d1.y = (d1.x < d1.y) ? d1.y : d1.x; // Swap if smaller
            d1.x = (d1.x < d1.z) ? d1.x : d1.z; // F1 is in d1.x
            d1.z = (d1.x < d1.z) ? d1.z : d1.x; // F1 is in d1.x
            d1.y = Mathf.Min(d1.y, d2.y); // F2 is now not in d2.yz
            d1.z = Mathf.Min(d1.z, d2.z); // F2 is now not in d2.yz
            d1.y = Mathf.Min(d1.y, d1.z); // nor in  d1.z
            d1.y = Mathf.Min(d1.y, d2.x); // F2 is in d1.y, we're done.
            return new Vector2(Mathf.Sqrt(d1.x), Mathf.Sqrt(d1.y)); // sqrt of F1 and F2
        }
    }

    internal static class SimplexNoise
    {
        internal static Vector3 srdnoise(Vector2 pos, float rot)
        {
            // Scale the input position
            var p = pos * 100f;

            // Calculate the integer and fractional parts of the position
            var ip = new Vector2(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            var fp = p - ip;

            // Calculate the dot product of the fractional part with the two basis vectors
            var d00 = Vector2.Dot(fp, new Vector2(0.5f, 0.5f));
            var d01 = Vector2.Dot(fp, new Vector2(0.7071067811865475f, -0.7071067811865475f));
            var d10 = Vector2.Dot(fp, new Vector2(-0.7071067811865475f, 0.7071067811865475f));
            var d11 = Vector2.Dot(fp, new Vector2(0.5f, -0.5f));

            // Calculate the noise value at the integer coordinates
            var n00 = Mathf.PerlinNoise(ip.x, ip.y);
            var n01 = Mathf.PerlinNoise(ip.x + 1, ip.y);
            var n10 = Mathf.PerlinNoise(ip.x, ip.y + 1);
            var n11 = Mathf.PerlinNoise(ip.x + 1, ip.y + 1);

            // Interpolate the noise values to get the final noise value
            var x0 = Mathf.Lerp(n00, n01, d00);
            var x1 = Mathf.Lerp(n10, n11, d00);
            var x = Mathf.Lerp(x0, x1, d10);

            // Rotate the noise value by the given angle
            var c = Mathf.Cos(rot);
            var s = Mathf.Sin(rot);
            var r = new Vector2(c, s);
            var i = new Vector2(1, 0);
            var o = new Vector2(0, 1);
            var u = r.x * i + r.y * o;
            var v = r.y * i - r.x * o;
            var noise = new Vector3(x, u.x, v.x);

            return noise;
        }
    }
}
