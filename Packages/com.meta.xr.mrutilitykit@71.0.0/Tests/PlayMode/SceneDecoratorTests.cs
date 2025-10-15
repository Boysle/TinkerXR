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
using System.Linq;
using Meta.XR.MRUtilityKit.SceneDecorator;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class SceneDecoratorTests : MRUKTestBase
    {
        private MRUKRoom _currentRoom;

        private static readonly int Expected_CubeGridFloorNotInside_GlobalMesh = 7;
        private static readonly int Expected_EggSimplexWallPhysicsCollider = 18;
        private static readonly int Expected_FloorCeilingWallGlobalMeshStaggered = 988;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            //todo - change path when removing oculusinternal
            yield return LoadScene(@"Packages\com.meta.xr.mrutilitykit\Tests\SceneDecoratorTests.unity");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadScene();
        }



        private IEnumerator RunTest(int decorationIndex, int expected)
        {
            var decorator = SetupSceneDecorator();
            var decoration = GetSceneDecoration(decorationIndex);
            decorator.sceneDecorations.Clear();
            decorator.sceneDecorations.Add(decoration);
            MRUK.Instance.LoadSceneFromJsonString(GetJsonString());
            yield return null;
            decorator.DecorateScene();
            yield return null;
            var createdDecorations = CountDecorations(decorationIndex);
            Assert.AreEqual(expected, createdDecorations);
            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CubeGridFloorNotInside_GlobalMesh()
        {
            yield return RunTest(0, Expected_CubeGridFloorNotInside_GlobalMesh);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator EggSimplexWallPhysicsCollider()
        {
            yield return RunTest(1, Expected_EggSimplexWallPhysicsCollider);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator FloorCeilingWallGlobalMeshStaggered()
        {
            yield return RunTest(2, Expected_FloorCeilingWallGlobalMeshStaggered);
        }

        private int CountDecorations(int decorationsIndex)
        {
            var counter = 0;
            var prefab = GetSceneDecoration(decorationsIndex).decorationPrefabs[0];
            foreach (var go in FindObjectsOfType<PoolManagerComponent.PoolableData>())
            {
                if (go.name.Length < prefab.name.Length)
                {
                    continue;
                }

                if (go.name.Substring(0, prefab.name.Length) == prefab.name && go.gameObject.activeSelf)
                {
                    counter++;
                }

            }
            return counter;
        }

        private string GetJsonString()
        {
            var decorationrefs = FindObjectOfType<SceneDecoratorTestReferences>();
            if (decorationrefs == null)
            {
                Assert.Fail();
            }
            return decorationrefs.Scene1.text;
        }

        private SceneDecoration GetSceneDecoration(int index)
        {
            var decorationrefs = FindObjectOfType<SceneDecoratorTestReferences>();
            if (decorationrefs == null)
            {
                Assert.Fail();
            }
            return decorationrefs.Decorations[index];
        }
        private SceneDecorator.SceneDecorator SetupSceneDecorator()
        {
            var sceneDecorator = FindObjectOfType<SceneDecorator.SceneDecorator>();
            sceneDecorator.recursionLimit = 1;
            sceneDecorator.DecorateOnStart = MRUK.RoomFilter.None;
            sceneDecorator.TrackUpdates = true;
            if (sceneDecorator == null)
            {
                Assert.Fail();
            }
            return sceneDecorator;
        }
    }
}
