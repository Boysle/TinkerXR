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

/// @cond

namespace Meta.XR.MRUtilityKit
{
    public static class TelemetryConstants
    {
        public static class MarkerId
        {
            public const int LoadSceneFromDevice = 651892966;
            public const int LoadSceneFromPrefab = 651889651;
            public const int LoadSceneFromJson = 651895197;
            public const int LoadEffectMesh = 651897605;
            public const int LoadFindSpawnPositions = 651888440;
            public const int LoadRoomGuardian = 651901100;
            public const int LoadSceneDebugger = 651897568;
            public const int LoadAnchorPrefabSpawner = 651902681;
            public const int LoadGridSliceResizer = 651896136;
            public const int LoadSceneNavigation = 651889094;
            public const int LoadSceneDecoration = 651888752;
            public const int LoadDestructibleGlobalMeshSpawner = 651898938;
            public const int LoadEnvironmentRaycastManager = 651891190;
        }

        public static class AnnotationType
        {
            public const string SceneName = "SceneName";
            public const string NumRooms = "NumRooms";
        }
    }
}
/// @endcond
