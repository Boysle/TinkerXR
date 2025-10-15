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
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class GeneralTests : MRUKTestBase
    {
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator ClassificationToSceneLabelsConversion()
        {
            var allLabels = Enum.GetValues(typeof(MRUKAnchor.SceneLabels)) as MRUKAnchor.SceneLabels[];
            Assert.IsNotNull(allLabels);
            var convertedLabels = Enum.GetValues(typeof(OVRSemanticLabels.Classification))
                .Cast<OVRSemanticLabels.Classification>()
                .Select(classification =>
                {
                    int bitShift = (int)classification;
                    Assert.IsTrue(bitShift >= 0);
                    Assert.IsTrue(bitShift < 32);
                    return Utilities.ClassificationToSceneLabel(classification);
                })
                .ToArray();
            Assert.IsTrue(allLabels.SequenceEqual(convertedLabels));
            yield return true;
        }
    }
}
