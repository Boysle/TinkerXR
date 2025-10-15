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
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;
using UnityEngine;

public class AnchorPrefabSpawnerInstallationRoutine : InstallationRoutine
{
    internal enum PrefabSpawnerVariant
    {
        DefaultView,
        LegacyRoomModelView
    }

    [SerializeField]
    [Variant(Behavior = VariantAttribute.VariantBehavior.Parameter,
         Description = "Initial configuration types for Anchor Prefab Spawner block.")]
    internal PrefabSpawnerVariant AnchorPrefabSpawnerTheme = PrefabSpawnerVariant.DefaultView;

    public override List<GameObject> Install(BlockData block, GameObject selectedGameObject)
    {
        var defaultPrefab = Prefab.transform.GetChild(0).gameObject;
        var roomModelPrefab = Prefab.transform.GetChild(1).gameObject;
        var spawnedPrefab = AnchorPrefabSpawnerTheme == PrefabSpawnerVariant.DefaultView ? Instantiate(defaultPrefab) : Instantiate(roomModelPrefab);
        var prefabName = AnchorPrefabSpawnerTheme == PrefabSpawnerVariant.DefaultView ? defaultPrefab.name : roomModelPrefab.name;
        spawnedPrefab.name = $"{Utils.BlockPublicTag} {prefabName}";
        Undo.RegisterCreatedObjectUndo(spawnedPrefab, $"install {prefabName}");
        return new List<GameObject> { spawnedPrefab };
    }
}
