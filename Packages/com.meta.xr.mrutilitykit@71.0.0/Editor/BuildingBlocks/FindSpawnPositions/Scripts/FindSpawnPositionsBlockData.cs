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

using System.Collections.Generic;
using System.Linq;
using Meta.XR.BuildingBlocks.Editor;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    public class FindSpawnPositionsBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            var createdObjects = base.InstallRoutine(selectedGameObject);
            var block = createdObjects.FirstOrDefault();

            if (block != null && selectedGameObject != null)
            {
                // Case when dropping hover a selected GameObject
                // We will spawn said object instead, and limit the amount to 1 by default
                block.transform.SetParent(selectedGameObject.transform, false);
                var findSpawnPositions = block.GetComponent<FindSpawnPositions>();
                if (findSpawnPositions != null)
                {
                    findSpawnPositions.SpawnObject = selectedGameObject;
                    findSpawnPositions.SpawnAmount = 1;
                }
            }

            return createdObjects;
        }
    }
}
