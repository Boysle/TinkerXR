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
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class CRUDTests : MRUKTestBase
    {
        private MRUKRoom _currentRoom;
        private JSONTestHelper _jsonTestHelper;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadScene(@"Packages\\com.meta.xr.mrutilitykit\\Tests\\CRUDTests.unity");
            _currentRoom = MRUK.Instance.GetCurrentRoom();
            _jsonTestHelper = FindObjectOfType<JSONTestHelper>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (MRUK.Instance != null)
            {
                foreach (var room in MRUK.Instance.Rooms)
                {
                    room.AnchorCreatedEvent.RemoveAllListeners();
                    room.AnchorUpdatedEvent.RemoveAllListeners();
                    room.AnchorRemovedEvent.RemoveAllListeners();
                }
                MRUK.Instance.RoomCreatedEvent.RemoveAllListeners();
                MRUK.Instance.RoomUpdatedEvent.RemoveAllListeners();
                MRUK.Instance.RoomRemovedEvent.RemoveAllListeners();
            }

            yield return UnloadScene();
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator VerifyStartFromJson()
        {
            Assert.AreEqual(12, _currentRoom.Anchors.Count);
            Debug.Log(_currentRoom);
            yield return null;
        }
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TwoAnchorsLess()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            int counterAnchorsOldRoom = _currentRoom.Anchors.Count;

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1LessAnchors.text);

            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");

            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(2, counterAnchorDeleted);
            Assert.AreEqual(0, counterAnchorUpdated);

            Assert.AreEqual(counterAnchorsOldRoom, _currentRoom.Anchors.Count + counterAnchorDeleted);


            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TwoNewAnchors()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            int counterAnchorsUpdate = _currentRoom.Anchors.Count;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);
            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1MoreAnchors.text);

            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");

            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(2, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(0, counterAnchorUpdated);
            Assert.AreEqual(counterAnchorsUpdate, _currentRoom.Anchors.Count - counterAnchorCreated);


            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomOrderSwitched()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1Room3.text);

            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom3Room1.text);
            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");

            Assert.AreEqual(0, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(0, counterAnchorUpdated);

            yield return null;
        }


        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorPlaneBoundaryChanged()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1SceneAnchorPlaneBoundaryChanged.text);
            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");

            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(1, counterAnchorUpdated);

            yield return null;
        }


        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorVolumeBoundsChanged()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1SceneAnchorVolumeBoundsChanged.text);

            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");


            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(1, counterAnchorUpdated);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorPlaneRectChanged()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1SceneAnchorPlaneRectChanged.text);

            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");


            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(1, counterAnchorUpdated);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorLabelChanged()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom1SceneAnchorLabelChanged.text);

            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");


            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(1, counterAnchorUpdated);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomUUIDChanged()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithScene1NewRoomGUID.text);
            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");


            Assert.AreEqual(1, counterRoomUpdated);
            Assert.AreEqual(0, counterRoomCreated);
            Assert.AreEqual(0, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(0, counterAnchorDeleted);
            Assert.AreEqual(0, counterAnchorUpdated);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Room2Loaded()
        {
            int counterRoomUpdated = 0;
            int counterRoomDeleted = 0;
            int counterRoomCreated = 0;

            int counterAnchorUpdated = 0;
            int counterAnchorDeleted = 0;
            int counterAnchorCreated = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => counterRoomUpdated++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => counterRoomCreated++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => counterRoomDeleted++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => counterAnchorCreated++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => counterAnchorDeleted++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => counterAnchorUpdated++);

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.SceneWithRoom2.text);

            Debug.Log($"counterRoomUpdated {counterRoomUpdated} counterRoomDeleted {counterRoomDeleted} counterRoomCreated {counterRoomCreated} " +
                      $"counterAnchorUpdated {counterAnchorUpdated} counterAnchorDeleted {counterAnchorDeleted} counterAnchorCreated {counterAnchorCreated}");

            Assert.AreEqual(0, counterRoomUpdated);
            Assert.AreEqual(1, counterRoomCreated);
            Assert.AreEqual(1, counterRoomDeleted);
            Assert.AreEqual(0, counterAnchorCreated);
            Assert.AreEqual(12, counterAnchorDeleted);
            Assert.AreEqual(0, counterAnchorUpdated);

            yield return null;
        }
    }
}

