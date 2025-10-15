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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class EffectMeshTests : MRUKTestBase
    {
        private MRUKRoom _currentRoom;
        private JSONTestHelper _jsonTestHelper;

        private static int Room1VertCountWall = 4;
        private static int Room1VertCountFloor = 8;
        private static int Room1VertCountCeiling = 8;
        private static int Room1VertCountTable = 24;
        private static int Room1VertCountOther = 24;


        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadScene(@"Packages\com.meta.xr.mrutilitykit\Tests\EffectMeshTests.unity");

            _jsonTestHelper = FindObjectOfType<JSONTestHelper>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroyAll();
            yield return UnloadScene();
        }

        private int GetRoom1Vertices()
        {
            return 7 * Room1VertCountWall
                + Room1VertCountFloor
                + Room1VertCountCeiling
                + Room1VertCountTable
                + 2 * Room1VertCountOther;
        }
        private int GetRoom1VerticesMoreAnchors()
        {
            return 7 * Room1VertCountWall
                   + Room1VertCountFloor
                   + Room1VertCountCeiling
                   + Room1VertCountTable
                   + 4 * Room1VertCountOther;
        }

        private int GetRoom1Room3Vertices()
        {
            return 7 * Room1VertCountWall //room1
                   + Room1VertCountFloor
                   + Room1VertCountCeiling
                   + Room1VertCountTable
                   + 2 * Room1VertCountOther
                   + 7 * Room1VertCountWall //room3
                   + Room1VertCountFloor
                   + Room1VertCountCeiling
                   + Room1VertCountTable
                   + 2 * Room1VertCountOther;
        }

        private int GetDefaultRoomVertices()
        {
            return 7 * Room1VertCountWall
                + Room1VertCountFloor
                + Room1VertCountCeiling;
        }

        /// <summary>
        /// This function tests the count of vertices for each anchor in a scene.
        /// It iterates over each room and anchor, and asserts that the count of vertices matches the expected count based on the anchor's label.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountVertsFromObjectsInSequenceWithChildren()
        {
            SetupEffectMesh();
            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom3Room1.text);
            yield return null;

            foreach (var room in MRUK.Instance.Rooms)
            {
                foreach (var anchor in room.Anchors)
                {
                    switch (anchor.Label)
                    {
                        case MRUKAnchor.SceneLabels.FLOOR:
                            Assert.AreEqual(Room1VertCountFloor, CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.CEILING:
                            Assert.AreEqual(Room1VertCountCeiling, CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.DOOR_FRAME:
                        case MRUKAnchor.SceneLabels.WINDOW_FRAME:
                        case MRUKAnchor.SceneLabels.SCREEN:
                        case MRUKAnchor.SceneLabels.WALL_ART:
                        case MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE:
                        case MRUKAnchor.SceneLabels.WALL_FACE:
                            Assert.AreEqual(Room1VertCountWall, CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.STORAGE:
                        case MRUKAnchor.SceneLabels.BED:
                        case MRUKAnchor.SceneLabels.TABLE:
                        case MRUKAnchor.SceneLabels.COUCH:
                        case MRUKAnchor.SceneLabels.PLANT:
                        case MRUKAnchor.SceneLabels.LAMP:
                        case MRUKAnchor.SceneLabels.OTHER:
                            Assert.AreEqual(Room1VertCountOther, CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.GLOBAL_MESH:
                            break;
                    }
                }
            }

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in a default room with no anchors.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesDefaultRoomNoAnchors()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.DefaultRoomNoAnchors.text);
            yield return null;

            var vertCount = CountVertex();

            var expectedVerts = GetDefaultRoomVertices();
            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in a default room with anchors.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1WithAnchors()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1.text);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();

            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in a room1 after room3 got removed.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom3Removed()
        {
            var effectMesh = SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom3Room1.text);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Room3Vertices();

            Assert.AreEqual(expectedVerts, vertCount);

            //track room updates, we want just vertices of Room one for this test
            effectMesh.TrackUpdates = true;

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1.text);
            yield return null;

            vertCount = CountVertex();
            expectedVerts = GetRoom1Vertices();
            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 after anchors got added.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1AddedRoomByUser()
        {
            var effectMesh = SetupEffectMesh();

            //we just track one room, and we simulate an added room by the user
            effectMesh.SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1.text);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();
            Assert.AreEqual(expectedVerts, vertCount);

            effectMesh.gameObject.SetActive(false);
            effectMesh.TrackUpdates = false;
            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom3Room1.text);
            yield return null;

            effectMesh.gameObject.SetActive(true);

            _currentRoom = MRUK.Instance.GetCurrentRoom();
            List<MRUKRoom> manualCreateEffectMesh = new();

            foreach (var room in MRUK.Instance.Rooms)
            {
                bool foundRoom = false;
                foreach (var anchor in room.Anchors)
                {
                    if (!anchor.GetComponentInChildren<Renderer>())
                    {
                        manualCreateEffectMesh.Add(room);
                        foundRoom = true;
                        break;
                    }
                }
                if (foundRoom)
                {
                    break;
                }
            }


            Assert.AreEqual(1, manualCreateEffectMesh.Count);

            foreach (var room in manualCreateEffectMesh)
            {
                effectMesh.CreateMesh(room);
                yield return null;
            }

            vertCount = CountVertex();
            expectedVerts = GetRoom1Room3Vertices();
            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 after anchors got added manually.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1AddedAnchorsByUser()
        {
            var effectMesh = SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1.text);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();

            Assert.AreEqual(expectedVerts, vertCount);

            effectMesh.gameObject.SetActive(false);
            effectMesh.TrackUpdates = false;
            effectMesh.SpawnOnStart = MRUK.RoomFilter.None;
            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1MoreAnchors.text);
            yield return null;

            effectMesh.gameObject.SetActive(true);

            _currentRoom = MRUK.Instance.GetCurrentRoom();
            List<MRUKAnchor> manualCreateEffectMesh = new();

            foreach (var anchor in _currentRoom.Anchors)
            {
                if (!anchor.GetComponentInChildren<Renderer>())
                {
                    manualCreateEffectMesh.Add(anchor);
                }
            }

            Assert.AreEqual(2, manualCreateEffectMesh.Count);

            foreach (var anchor in manualCreateEffectMesh)
            {
                effectMesh.CreateEffectMesh(anchor);
                yield return null;
            }

            vertCount = CountVertex();
            expectedVerts = GetRoom1VerticesMoreAnchors();
            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 after anchors got added by an update.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1AddedAnchorsBySystem()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1.text);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();

            Assert.AreEqual(expectedVerts, vertCount);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1MoreAnchors.text);
            yield return null;

            vertCount = CountVertex();
            expectedVerts = GetRoom1VerticesMoreAnchors();

            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 and room3 at startup.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1Room3Startup()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom3Room1.text);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Room3Vertices();

            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        private int CountVertex()
        {
            var allObjects = (GameObject[])FindObjectsOfType(typeof(GameObject));
            int vertCount = 0;
            foreach (GameObject obj in allObjects)
            {
                Renderer rend = obj.GetComponentInChildren<Renderer>();
                if (!rend)
                {
                    continue;
                }

                MeshFilter mf = obj.GetComponent<MeshFilter>();
                if (!mf)
                {
                    continue;
                }

                vertCount += mf.mesh.vertexCount;
            }
            return vertCount;
        }

        private int CountVertex(MRUKAnchor anchor)
        {
            int vertCount = 0;
            foreach (var rend in anchor.gameObject.GetComponentsInChildren<Renderer>())
            {
                MeshFilter mf = rend.gameObject.GetComponent<MeshFilter>();
                if (!mf)
                {
                    continue;
                }

                vertCount += mf.mesh.vertexCount;

            }

            return vertCount;
        }

        private EffectMesh SetupEffectMesh()
        {
            var effectMesh = FindObjectOfType<EffectMesh>();
            if (effectMesh == null)
            {
                Assert.Fail();
            }

            effectMesh.SpawnOnStart = MRUK.RoomFilter.AllRooms;
            effectMesh.TrackUpdates = true;
            return effectMesh;
        }

        private void DestroyAll()
        {
            DestroyAll<MeshRenderer>();
            DestroyAll<MeshFilter>();
            DestroyAll<EffectMesh>();
            DestroyAll<MRUKAnchor>();
            DestroyAll<MRUKRoom>();
        }
        private void DestroyAll<T>() where T : Component
        {
            var allObjects = (T[])GameObject.FindObjectsOfType(typeof(T));
            foreach (var obj in allObjects)
            {
                DestroyImmediate(obj.gameObject);
            }
        }
    }
}
