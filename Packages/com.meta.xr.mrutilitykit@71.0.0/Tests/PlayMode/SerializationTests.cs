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
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TestTools.Utils;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class SerializationTests : MRUKTestBase
    {
        private JSONTestHelper _jsonTestHelper;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadScene(@"Packages\\com.meta.xr.mrutilitykit\\Tests\\RayCastTests.unity");
            _jsonTestHelper = FindObjectOfType<JSONTestHelper>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadScene();
        }

        /// <summary>
        /// Test that serialization to the Unity coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator SerializationToUnity()
        {
            var json = MRUK.Instance.SaveSceneToJsonString(SerializationHelpers.CoordinateSystem.Unity);

            var splitJson = json.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            var splitExpected = _jsonTestHelper.UnityExpectedSerializedScene.text.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            splitJson = json
                .Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Contains("RoomLabel"))
                .ToArray();
            splitExpected = _jsonTestHelper.UnityExpectedSerializedScene.text
                .Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Contains("RoomLabel"))
                .ToArray();



            for (int i = 0; i < splitExpected.Length && i < splitJson.Length; i++)
            {
                if (Regex.IsMatch(splitExpected[i], "[A-F0-9]{32}") &&
                    Regex.IsMatch(splitJson[i], "[A-F0-9]{32}"))
                {
                    // Ignore GUIDs because they change every time
                    continue;
                }

                Assert.AreEqual(splitExpected[i], splitJson[i], "Line {0}", i + 1);
            }

            Assert.AreEqual(splitExpected.Length, splitJson.Length, "Number of lines");
            yield return null;
        }



        /// <summary>
        /// Test that serialization to the Unreal coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator SerializationToUnreal()
        {
            var json = MRUK.Instance.SaveSceneToJsonString(SerializationHelpers.CoordinateSystem.Unreal);

            var splitJson = json.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            var splitExpected = _jsonTestHelper.UnrealExpectedSerializedScene.text.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            splitJson = json
                .Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Contains("RoomLabel"))
                .ToArray();
            splitExpected = _jsonTestHelper.UnrealExpectedSerializedScene.text
                .Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Contains("RoomLabel"))
                .ToArray();

            for (int i = 0; i < splitExpected.Length && i < splitJson.Length; i++)
            {
                if (Regex.IsMatch(splitExpected[i], "[A-F0-9]{32}") &&
                    Regex.IsMatch(splitJson[i], "[A-F0-9]{32}"))
                {
                    // Ignore GUIDs because they change every time
                    continue;
                }

                Assert.AreEqual(splitExpected[i], splitJson[i], "Line {0}", i + 1);
            }

            Assert.AreEqual(splitExpected.Length, splitJson.Length, "Number of lines");
            yield return null;
        }

        /// <summary>
        /// Test that deserialization from the Unity coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator DeserializationFromUnity()
        {
            ValidateLoadedScene(_jsonTestHelper.UnityExpectedSerializedScene.text);
            yield return null;
        }

        /// <summary>
        /// Test that deserialization from the Unreal coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator DeserializationFromUnreal()
        {
            ValidateLoadedScene(_jsonTestHelper.UnrealExpectedSerializedScene.text);
            yield return null;
        }

        /// <summary>
        /// Make sure the order of wall anchors matches that of the room layout
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator WallOrderMatchesRoomLayout()
        {
            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.UnityExpectedSerializedScene.text);
            var sceneData = SerializationHelpers.Deserialize(_jsonTestHelper.UnityExpectedSerializedScene.text);

            Assert.AreEqual(8, sceneData.Rooms[0].RoomLayout.WallsUuid.Count);
            Assert.AreEqual(8, MRUK.Instance.Rooms[0].WallAnchors.Count);
            int i = 0;
            foreach (var anchorUuid in sceneData.Rooms[0].RoomLayout.WallsUuid)
            {
                Assert.AreEqual(MRUK.Instance.Rooms[0].WallAnchors[i++].Anchor.Uuid, anchorUuid);
            }

            yield return null;
        }

        void ValidateLoadedScene(string sceneJson)
        {
            MRUK.Instance.LoadSceneFromJsonString(sceneJson);
            Assert.NotNull(MRUK.Instance.GetCurrentRoom());
            var loadedRoom = MRUK.Instance.GetCurrentRoom();
            MRUK.Instance.LoadSceneFromPrefab(MRUK.Instance.SceneSettings.RoomPrefabs[0], false);
            var expectedRoom = MRUK.Instance.Rooms[1];
            Assert.IsNotNull(expectedRoom);
            var loadedAnchors = loadedRoom.Anchors;
            var expectedAnchors = expectedRoom.Anchors;
            Assert.AreEqual(expectedAnchors.Count, loadedAnchors.Count);
            for (int i = 0; i < loadedAnchors.Count; i++)
            {
                var loadedAnchor = loadedAnchors[i];
                var expectedAnchor = expectedAnchors[i];
                // Skip UUID check as they could change every time
                if (loadedAnchor.PlaneRect.HasValue)
                {
                    Assert.That(loadedAnchor.PlaneRect.Value.position, Is.EqualTo(expectedAnchor.PlaneRect.Value.position).Using(Vector2EqualityComparer.Instance));
                    Assert.That(loadedAnchor.PlaneRect.Value.size, Is.EqualTo(expectedAnchor.PlaneRect.Value.size).Using(Vector2EqualityComparer.Instance));
                }

                if (loadedAnchor.VolumeBounds.HasValue)
                {
                    Assert.That(loadedAnchor.VolumeBounds.Value.extents, Is.EqualTo(expectedAnchor.VolumeBounds.Value.extents).Using(Vector3EqualityComparer.Instance));
                    Assert.That(loadedAnchor.VolumeBounds.Value.center, Is.EqualTo(expectedAnchor.VolumeBounds.Value.center).Using(Vector3EqualityComparer.Instance));
                }

                Assert.That(loadedAnchor.transform.position, Is.EqualTo(expectedAnchor.transform.position).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.transform.rotation.eulerAngles, Is.EqualTo(expectedAnchor.transform.rotation.eulerAngles).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.transform.localScale, Is.EqualTo(expectedAnchor.transform.localScale).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.GetAnchorCenter(), Is.EqualTo(expectedAnchor.GetAnchorCenter()).Using(Vector3EqualityComparer.Instance));
                if (loadedAnchor.PlaneBoundary2D != null)
                {
                    var loadedPlaneBoundary2D = loadedAnchor.PlaneBoundary2D;
                    var expectedPlaneBoundary2D = expectedAnchor.PlaneBoundary2D;
                    Assert.IsTrue(loadedPlaneBoundary2D.SequenceEqual(expectedPlaneBoundary2D, Vector2EqualityComparer.Instance));
                }

                Assert.AreEqual(expectedAnchor.Label, loadedAnchor.Label);
                var loadedBoundsFaceCenters = loadedAnchor.GetBoundsFaceCenters();
                var expectedBoundsFaceCenters = expectedAnchor.GetBoundsFaceCenters();
                Assert.IsTrue(loadedBoundsFaceCenters.SequenceEqual(expectedBoundsFaceCenters, Vector3EqualityComparer.Instance));
            }
        }
    }
}

