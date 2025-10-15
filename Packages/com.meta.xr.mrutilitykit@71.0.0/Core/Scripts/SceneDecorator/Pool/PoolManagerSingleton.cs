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

using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    [SingletonMonoBehaviour.InstantiationSettings(dontDestroyOnLoad = false)]
    public class PoolManagerSingleton : SingletonMonoBehaviour<PoolManagerSingleton>
    {
        public PoolManager<GameObject, Pool<GameObject>> poolManager => poolManagerComponent.poolManager;

        private PoolManagerComponent poolManagerComponent;

        /*
         * Create is a drop-in replacement for Instantiate that uses a pool if available.
         * Note that it is not named Instantiate so that it is easy to find & replace Instantiate
         *  calls with Create calls when switching to using Pools.
         */
        public GameObject Create(GameObject primitive, Vector3 position, Quaternion rotation, MRUKAnchor anchor, Transform parent = null)
        {
            if (poolManagerComponent == null)
            {
                poolManagerComponent = GetComponent<PoolManagerComponent>();
            }

            return poolManagerComponent.Create(primitive, position, rotation, anchor, parent);
        }

        public GameObject Create(GameObject primitive, MRUKAnchor anchor, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            return poolManagerComponent.Create(primitive, anchor, parent, instantiateInWorldSpace);
        }

        public T Create<T>(T primitive, Vector3 position, Quaternion rotation, MRUKAnchor anchor, Transform parent = null) where T : Component
        {
            return poolManagerComponent.Create<T>(primitive, position, rotation, anchor, parent);
        }

        public T Create<T>(T primitive, MRUKAnchor anchor, Transform parent = null, bool instantiateInWorldSpace = false) where T : Component
        {
            return poolManagerComponent.Create<T>(primitive, anchor, parent, instantiateInWorldSpace);
        }

        /*
         * Release is a drop-in replacement for Destroy that releases into a pool if available.
         * Note that it is not named Destroy so that it is easy to find & replace Destroy
         *  calls with Release calls when switching to using Pools.
         */
        public void Release(GameObject go)
        {
            if (Instance == null)
            {
                Destroy(go);
                return;
            }

            if (go == null)
            {
                return;
            }

            Instance.poolManagerComponent.Release(go);
        }

        void Start()
        {
            poolManagerComponent = GetComponent<PoolManagerComponent>();
        }
    }
}
