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
using UnityEngine;

public class JSONTestHelper : MonoBehaviour
{
    [SerializeField] internal TextAsset DefaultRoomNoAnchors;
    [SerializeField] internal TextAsset SceneWithRoom1;
    [SerializeField] internal TextAsset SceneWithRoom2;
    [SerializeField] internal TextAsset SceneWithScene1NewRoomGUID;
    [SerializeField] internal TextAsset SceneWithRoom1SceneAnchorLabelChanged;
    [SerializeField] internal TextAsset SceneWithRoom1SceneAnchorPlaneRectChanged;
    [SerializeField] internal TextAsset SceneWithRoom1SceneAnchorVolumeBoundsChanged;
    [SerializeField] internal TextAsset SceneWithRoom1SceneAnchorPlaneBoundaryChanged;
    [SerializeField] internal TextAsset SceneWithRoom1Room3;
    [SerializeField] internal TextAsset SceneWithRoom3Room1;
    [SerializeField] internal TextAsset SceneWithRoom1MoreAnchors;
    [SerializeField] internal TextAsset SceneWithRoom1LessAnchors;
    [SerializeField] internal TextAsset UnityExpectedSerializedScene;
    [SerializeField] internal TextAsset UnrealExpectedSerializedScene;
    [SerializeField] internal TextAsset HierarchyObjects;
}

