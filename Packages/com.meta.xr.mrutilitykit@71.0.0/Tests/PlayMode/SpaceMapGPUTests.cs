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
using UnityEngine;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class SpaceMapGPUTests : MRUKTestBase
    {
        private SpaceMapGPU SpaceMapGPU;
        private SpaceMapGPUTestHelper SpaceMapGPUTestHelper;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // batchmode (or better -nographics) will not have a GPU
            // and compute shaders only run on GPU's
            if (Application.isBatchMode)
            {
                yield return true;
                yield break;
            }
            yield return LoadScene(@"Packages\com.meta.xr.mrutilitykit\Tests\SpaceMapGPUTests.unity", false);
            SpaceMapGPU = FindObjectOfType<SpaceMapGPU>();
            SpaceMapGPUTestHelper = FindObjectOfType<SpaceMapGPUTestHelper>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadScene();
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CheckRoom1()
        {
            // batchmode (or better -nographics) will not have a GPU
            // and compute shaders only run on GPU's
            if (Application.isBatchMode)
            {
                yield return true;
                yield break;
            }
            yield return CheckRoom(0);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CheckRoom2()
        {
            // batchmode (or better -nographics) will not have a GPU
            // and compute shaders only run on GPU's
            if (Application.isBatchMode)
            {
                yield return true;
                yield break;
            }
            yield return CheckRoom(1);
        }


        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CheckRoom1WithUpdate()
        {
            // batchmode (or better -nographics) will not have a GPU
            // and compute shaders only run on GPU's
            if (Application.isBatchMode)
            {
                yield return true;
                yield break;
            }
            yield return CheckRoom(0, true);
        }

        private IEnumerator CheckRoom(int index, bool updateAnchors = false)
        {
            MRUK.Instance.SceneSettings.RoomIndex = index;
            MRUK.Instance.LoadSceneFromJsonString(MRUK.Instance.SceneSettings.SceneJsons[MRUK.Instance.SceneSettings.RoomIndex].text);
            SpaceMapGPU = SetupSpaceMapGPU();

            SpaceMapGPU.StartSpaceMap(MRUK.RoomFilter.AllRooms);

            while (SpaceMapGPU.Dirty)
            {
                yield return null;
            }

            if (CompareTextures(SpaceMapGPU.OutputTexture, index == 0 ? SpaceMapGPUTestHelper.Room : SpaceMapGPUTestHelper.RoomLessAnchors))
            {
                yield return true;
            }

            if (updateAnchors)
            {
                index = 1;
                MRUK.Instance.SceneSettings.RoomIndex = index;
                MRUK.Instance.LoadSceneFromJsonString(MRUK.Instance.SceneSettings.SceneJsons[MRUK.Instance.SceneSettings.RoomIndex].text);
                SpaceMapGPU = SetupSpaceMapGPU();

                SpaceMapGPU.StartSpaceMap(MRUK.RoomFilter.AllRooms);

                while (SpaceMapGPU.Dirty)
                {
                    yield return null;
                }
                if (CompareTextures(SpaceMapGPU.OutputTexture, SpaceMapGPUTestHelper.RoomLessAnchors))
                {
                    yield return true;
                }
            }
            yield return false;
        }


        private SpaceMapGPU SetupSpaceMapGPU()
        {
            var spaceMap = FindObjectOfType<SpaceMapGPU>();
            if (spaceMap == null)
            {
                Assert.Fail();
            }
            return spaceMap;
        }

        private bool CompareTextures(Texture2D left, Texture2D right, bool checkAlpha = false)
        {
            var colorsLeft = left.GetPixels();
            var colorsRight = right.GetPixels();

            if (colorsLeft.Length != colorsRight.Length)
            {
                return false;
            }

            for (var i = 0; i < colorsLeft.Length; i++)
            {
                for (var j = 0; j < colorsRight.Length; j++)
                {
                    var b = Mathf.Approximately(colorsLeft[i].r, colorsRight[j].r)
                            && Mathf.Approximately(colorsLeft[i].g, colorsRight[j].g)
                            && Mathf.Approximately(colorsLeft[i].b, colorsRight[j].b);
                    if (checkAlpha)
                    {
                        b = b && Mathf.Approximately(colorsLeft[i].a, colorsRight[j].a);
                    }

                    if (!b)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}

