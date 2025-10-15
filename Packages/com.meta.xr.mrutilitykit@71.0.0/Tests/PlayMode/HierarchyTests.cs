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
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


namespace Meta.XR.MRUtilityKit.Tests
{
    public class HierarchyTests : MRUKTestBase
    {
        private JSONTestHelper _jsonTestHelper;

        private struct HierarchyHelper
        {
            public string GUID;
            public MRUKAnchor.SceneLabels SematicLabel;
            public List<HierarchyHelper> Children;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadScene(@"Packages\\com.meta.xr.mrutilitykit\\Tests\\CRUDTests.unity");
            _jsonTestHelper = FindObjectOfType<JSONTestHelper>();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadScene();
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator HierarchiesFromJson()
        {
            var d = new Dictionary<HierarchyHelper, bool>
            {
                {
                    new HierarchyHelper()
                    {
                        GUID = "058B6ACF4967FA486CC4C81067BE55E5",
                        SematicLabel = MRUKAnchor.SceneLabels.FLOOR,
                        Children = new List<HierarchyHelper>(){
                            new()
                            {
                                GUID = "E258583E8278B7BC698F7A54981424FE",
                                SematicLabel = MRUKAnchor.SceneLabels.TABLE,
                                Children = new()
                            },
                            new()
                            {
                                GUID = "7D8FCF7EAAE03A2C35489354436B2D87",
                                SematicLabel = MRUKAnchor.SceneLabels.COUCH,
                                Children = new()
                            },
                            new()
                            {
                                GUID = "169A841AA80A20EDB4C13505D82941A3",
                                SematicLabel = MRUKAnchor.SceneLabels.STORAGE,
                                Children = new()
                            },
                            new()
                            {
                                GUID = "68BBDFBCFDD8AFC72F8F0B7A763B4912",
                                SematicLabel = MRUKAnchor.SceneLabels.COUCH,
                                Children = new()
                            }
                        }
                    }, false
                },
                {
                    new HierarchyHelper()
                    {
                        GUID = "E258583E8278B7BC698F7A54981424FE",
                        SematicLabel = MRUKAnchor.SceneLabels.TABLE,
                        Children = new List<HierarchyHelper>(){
                            new()
                            {
                                GUID = "12F9238EB10E42130C065B8CEFBEA3EF",
                                SematicLabel = MRUKAnchor.SceneLabels.COUCH,
                                Children = new()
                            },
                            new()
                            {
                                GUID = "E9BF3D55119B527C9BF0AEE2357B2EFB",
                                SematicLabel = MRUKAnchor.SceneLabels.SCREEN,
                                Children = new()
                            },
                            new()
                            {
                                GUID = "48FBB6DE2E154FA57A685DDAEBA1BB35",
                                SematicLabel = MRUKAnchor.SceneLabels.TABLE,
                                Children = new()
                            }
                        }
                    }, false
                },
                {
                    new HierarchyHelper()
                    {
                        GUID = "7D8FCF7EAAE03A2C35489354436B2D87",
                        SematicLabel = MRUKAnchor.SceneLabels.COUCH,
                        Children = new List<HierarchyHelper>(){
                            new()
                            {
                                GUID = "89CF3EE269046676AD30BD0857F904F0",
                                SematicLabel = MRUKAnchor.SceneLabels.COUCH,
                                Children = new()
                            }
                        }
                    }, false
                },
                {
                    new HierarchyHelper()
                    {
                        GUID = "3C00CF8F4C61DB7B04314A65EF261AD9",
                        SematicLabel = MRUKAnchor.SceneLabels.BED,
                        Children = new List<HierarchyHelper>(){
                            new()
                            {
                                GUID = "3C00CF8F4C61DB7B04314A65EF261AD8",
                                SematicLabel = MRUKAnchor.SceneLabels.BED,
                                Children = new()
                            }
                        }
                    }, false
                },
                {
                    new HierarchyHelper()
                    {
                        GUID = "68BBDFBCFDD8AFC72F8F0B7A763B4912",
                        SematicLabel = MRUKAnchor.SceneLabels.COUCH,
                        Children = new List<HierarchyHelper>(){
                            new()
                            {
                                GUID = "3C00CF8F4C61DB7B04314A65EF261AD9",
                                SematicLabel = MRUKAnchor.SceneLabels.BED,
                                Children = new()
                            },
                            new()
                            {
                                GUID = "B3F81F3FBD0EEFB9E075B2E424DE440F",
                                SematicLabel = MRUKAnchor.SceneLabels.TABLE,
                                Children = new()
                            }
                        }
                    }, false
                }
            };

            MRUK.Instance.LoadSceneFromJsonString(_jsonTestHelper.HierarchyObjects.text);

            for (var i = 0; i < d.Keys.Count; i++)
            {
                var kv = d.ElementAt(i);
                var key = kv.Key;

                var uuidAnchorToFind = key.GUID;

                var result = false;

                var sAnchorFound = "";

                foreach (var anchor in MRUK.Instance.GetCurrentRoom().Anchors)
                {
                    var foundAnchor = false;
                    var uuidAnchorScene = anchor.Anchor.Uuid.ToString().Replace("-", "").ToUpper();
                    if (uuidAnchorToFind == uuidAnchorScene && anchor.Label == key.SematicLabel)
                    {
                        var counter = 0;
                        sAnchorFound = $"Anchor found {uuidAnchorScene} has {anchor.ChildAnchors.Count} Children";
                        foreach (var anchorchild in anchor.ChildAnchors)
                        {
                            var uuidAnchorChildScene = anchorchild.Anchor.Uuid.ToString().Replace("-", "").ToUpper();

                            foreach (var child in key.Children)
                            {
                                if (child.GUID == uuidAnchorChildScene && child.SematicLabel == anchorchild.Label)
                                {
                                    counter++;
                                }
                            }
                        }

                        if (counter == anchor.ChildAnchors.Count)
                        {
                            foundAnchor = true;
                        }
                    }

                    if (foundAnchor)
                    {
                        result = true;
                        break;
                    }
                }

                var s = $"Expecting {key.SematicLabel} with {key.GUID} to have {key.Children.Count} children. {sAnchorFound}";

                Assert.True(result, s);
            }
            yield return null;
        }

    }
}
