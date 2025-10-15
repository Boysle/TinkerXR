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
using Meta.XR.Util;

namespace Meta.XR.MRUtilityKit
{
    // Wrapper struct that implements IDisposable to make sure the FreeMesh function is always called even in case of exceptions
    internal struct Mesh2fDisposer : IDisposable
    {
        public MRUKNativeFuncs.MrukMesh2f Mesh;
        internal Mesh2fDisposer(MRUKNativeFuncs.MrukMesh2f mesh)
        {
            Mesh = mesh;
        }
        public void Dispose()
        {
            MRUKNativeFuncs.FreeMesh(ref Mesh);
        }
    }

    /// <summary>
    /// Provides methods for triangulating polygons and clipping polygons with holes.
    /// </summary>
    [Feature(Feature.Scene)]
    public static class Triangulator
    {
        /// <summary>
        /// Triangulates a set of points using the ear clipping algorithm.
        /// </summary>
        /// <param name="vertices">The list of vertices that define the outline of a polygon to triangulate.</param>
        /// <param name="holes">The list of internal polygon holes that should not be triangulated.</param>
        /// <param name="outVertices">The list of vertices created from the triangulation.</param>
        /// <param name="outIndices">The list of indices created from the triangulation.</param>
        public static unsafe void TriangulatePoints(List<Vector2> vertices, List<List<Vector2>> holes, out Vector2[] outVertices, out int[] outIndices)
        {
            int numPolygons = holes != null ? holes.Count + 1 : 1;
            var polygons = new MRUKNativeFuncs.MrukPolygon2f[numPolygons];
            polygons[0].numPoints = (uint)vertices.Count;
            polygons[0].points = vertices.ToArray();
            if (holes != null)
            {
                for (int i = 0; i < holes.Count; ++i)
                {
                    polygons[i + 1].numPoints = (uint)holes[i].Count;
                    polygons[i + 1].points = holes[i].ToArray();
                }
            }

            using var mesh = new Mesh2fDisposer(MRUKNativeFuncs.TriangulatePolygon(polygons, (uint)numPolygons));
            outVertices = new Vector2[(int)mesh.Mesh.numVertices];
            for (uint i = 0; i < mesh.Mesh.numVertices; ++i)
            {
                outVertices[i] = mesh.Mesh.vertices[i];
            }
            outIndices = new int[(int)mesh.Mesh.numIndices];
            for (uint i = 0; i < mesh.Mesh.numIndices; ++i)
            {
                outIndices[i] = (int)mesh.Mesh.indices[i];
            }
        }
    }
}
