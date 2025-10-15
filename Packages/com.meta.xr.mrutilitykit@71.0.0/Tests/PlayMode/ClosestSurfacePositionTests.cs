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
using UnityEngine.TestTools.Utils;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class ClosestSurfacePositionTests : MRUKTestBase
    {
        private MRUKRoom _currentRoom;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadScene(@"Packages\\com.meta.xr.mrutilitykit\\Tests\\RayCastTests.unity");
            _currentRoom = MRUK.Instance.GetCurrentRoom();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadScene();
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Y Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Plane_Closest()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.1f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH, MRUKAnchor.ComponentType.Plane));
            Assert.That(distance, Is.EqualTo(0.15f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.74f, 0.25f, -1.02f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(Vector3.up).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Y Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsInsideYNeg()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.1f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal);
            Assert.That(distance, Is.EqualTo(-0.1f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.74f, 0f, -1.02f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(Vector3.down).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Y Positive plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsInsideYPos()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.4f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal);
            Assert.That(distance, Is.EqualTo(-0.1f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.74f, 0.5f, -1.02f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(Vector3.up).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the X Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsInsideXNeg()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(-0.4f, 0.25f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal);
            Assert.That(distance, Is.EqualTo(-0.0609238148f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(-0.460909486f, 0.25f, -1.0186826f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(-0.999766171f, 0, 0.0216231868f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the X Positive plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsInsideXPos()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(1.8f, 0.25f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal);
            Assert.That(distance, Is.EqualTo(-0.140905142f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(1.94087219f, 0.25f, -1.02304673f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(0.999766171f, 0f, -0.0216231868f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Z Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsInsideZNeg()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.2f, -1.9f), out var surfacePosition, out var closestAnchor, out var normal);
            Assert.That(distance, Is.EqualTo(-0.0241975784).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.7394768f, 0.199999988f, -1.92419195f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(-0.0216231868f, 0, -0.999766231f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Z Positive plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsInsideZPos()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.2f, -0.2f), out var surfacePosition, out var closestAnchor, out var normal);
            Assert.That(distance, Is.EqualTo(-0.0841835737f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.741820335f, 0.199999988f, -0.115835905f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(0.0216231868f, 0f, 0.999766231f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Y Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsOutsideYNeg()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, -0.1f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(distance, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.74f, 0f, -1.02f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(Vector3.down).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Y Positive plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsOutsideYPos()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.6f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(distance, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.74f, 0.5f, -1.02f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(Vector3.up).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the X Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsOutsideXNeg()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(-0.6f, 0.25f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(distance, Is.EqualTo(0.139029458f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(-0.461003065f, 0.25f, -1.0230062f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(-0.999766171f, 0, 0.0216231868f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the X Positive plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsOutsideXPos()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(2.0f, 0.25f, -1.02f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(distance, Is.EqualTo(0.0590481535f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(1.94096565f, 0.25f, -1.01872313f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(0.999766171f, 0f, -0.0216231868f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Z Negative plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsOutsideZNeg()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.2f, -2.1f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(distance, Is.EqualTo(0.175755471f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.743800402f, 0.199999988f, -1.92428553f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(-0.0216231868f, 0, -0.999766231f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }

        /// <summary>
        /// Test that the position is inside a volume closest to the Z Positive plane
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClosestSurfacePosition_Volume_IsOutsideZPos()
        {
            var distance = _currentRoom.TryGetClosestSurfacePosition(new Vector3(0.74f, 0.2f, 0f), out var surfacePosition, out var closestAnchor, out var normal, new LabelFilter(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(distance, Is.EqualTo(0.115769513f).Within(1e-6f));
            Assert.That(closestAnchor.Label, Is.EqualTo(MRUKAnchor.SceneLabels.COUCH));
            Assert.That(surfacePosition, Is.EqualTo(new Vector3(0.737496674f, 0.200000048f, -0.115742445f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(normal, Is.EqualTo(new Vector3(0.0216231868f, 0f, 0.999766231f)).Using(Vector3EqualityComparer.Instance));

            yield return null;
        }
    }
}

