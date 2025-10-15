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
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Serializable]
    public enum ConstraintModeCheck
    {
        Value,
        Bool
    }

    [Serializable]
    public struct Constraint
    {
        [SerializeField]
        public string name;

        [SerializeField]
        public bool enabled;

        [SerializeField]
        public Mask mask;

        [SerializeField] public ConstraintModeCheck modeCheck;

        [SerializeField] public float min;
        [SerializeField] public float max;
    }

    public struct Candidate
    {
        public GameObject decorationPrefab;
        public Vector2 localPos;
        public Vector2 localPosNormalized;
        public RaycastHit hit;
        public Vector3 anchorCompDists;
        public float anchorDist;
        public float slope;
    }

    [Flags]
    public enum Axes
    {
        X = 0x1,
        Y = 0x2,
        Z = 0x4
    }

    public enum DistributionType
    {
        GRID = 0x0,
        SIMPLEX = 0x1,
        STAGGERED_CONCENTRIC = 0x2,
        RANDOM = 0x3
    }

    public enum Placement
    {
        LOCAL_PLANAR,
        WORLD_PLANAR,
        SPHERICAL
    }

    public enum SpawnHierarchy
    {
        ROOT,
        SCENE_DECORATOR_CHILD,
        ANCHOR_CHILD,
        TARGET_CHILD,
        TARGET_COLLIDER_CHILD
    }

    [Flags]
    public enum Target
    {
        GLOBAL_MESH = 0x1, //The final position will be determined by a point on the global mesh
        RESERVED_MESH = 0x2, //Not supported yet, same as GLOBAL_MESH
        PHYSICS_LAYERS = 0x4, //All physics layers defined will help determine the position
        CUSTOM_COLLIDERS = 0x8, //Use a custom collider to determine the position
        CUSTOM_TAGS = 0x16 //use only gameobjects with custom tag, needs a collider
    }
}
