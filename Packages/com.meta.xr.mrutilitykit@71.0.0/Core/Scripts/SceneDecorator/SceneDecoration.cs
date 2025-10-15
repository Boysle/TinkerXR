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
using Meta.XR.Util;
using UnityEditor;
using UnityEngine;


namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    [CreateAssetMenu(fileName = "SceneDecoration", menuName = "Meta/MRUK/Scene Decoration")]
    public class SceneDecoration : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Each prefab has an own pool of this size")]
        public int Poolsize;

        [SerializeField]
        [Tooltip("Those prefabs will be used (randomly chosen) when a candidate has been found where to spawn.")]
        public GameObject[] decorationPrefabs;

        [SerializeField]
        [Tooltip("The effect will run on all anchors with the given labels.")]
        public MRUKAnchor.SceneLabels executeSceneLabels;

        [SerializeField]
        [Tooltip("Which kind of targets should be used")]
        public Target targets;

        [SerializeField]
        [Tooltip("If using physics layers as targets")]
        public LayerMask targetPhysicsLayers;

        [SerializeField]
        [Tooltip("Changes the placement direction")]
        public Placement placement;

        [SerializeField]
        [Tooltip("Which direction to shoot")]
        public Vector3 placementDirection = Vector3.down;

        [SerializeField]
        [Tooltip("Shoot backwards")]
        public bool selectBehind = true;

        [SerializeField]
        [Tooltip("Offset from where to start the ray")]
        public Vector3 rayOffset = new(0f, 0f, 0.1f);

        [SerializeField]
        [Tooltip("Where to attach the created decorator")]
        public SpawnHierarchy spawnHierarchy;

        [SerializeField]
        [Tooltip("How to distribute the decorations")]
        public DistributionType distributionType;

        [SerializeField]
        [Tooltip("Distribute as a grid")]
        public GridDistribution gridDistribution;

        [SerializeField]
        [Tooltip("Generates uniform sampling points with simplex noise")]
        public SimplexDistribution simplexDistribution;

        [SerializeField]
        [Tooltip("Generates staggered concentric distribution")]
        public StaggeredConcentricDistribution staggeredConcentricDistribution;

        [SerializeField]
        [Tooltip("Random distribution")]
        public RandomDistribution randomDistribution;

        [SerializeField]
        public Mask[] masks;

        [SerializeField]
        public Constraint[] constraints;

        [SerializeField]
        public Modifier[] modifiers;

        [SerializeField]
        public bool discardParentScaling = true;

        [SerializeField]
        public float lifetime = 0f;

        [SerializeField]
        [Tooltip("Red: Physics Raycast, Magenta: Collider Raycast (only using this collider), Cyan: Startpos, Blue: Endpos")]
        public bool DrawDebugRaysAndImpactPoints = false;

#if UNITY_EDITOR
        private void OnValidate()
        {
            int i = 0;
            int len = masks.Length;
            for (; i < len; ++i)
            {
                if (masks[i] == null)
                {
                    --len;
                    masks[i] = masks[len];
                    --i;
                }
            }

            if (len < masks.Length)
            {
                Array.Resize(ref masks, len);
                EditorUtility.SetDirty(this);
            }

            i = 0;
            len = modifiers.Length;
            for (; i < len; ++i)
            {
                if (modifiers[i] == null)
                {
                    --len;
                    modifiers[i] = modifiers[len];
                    --i;
                }
            }

            if (len < modifiers.Length)
            {
                Array.Resize(ref modifiers, len);
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
