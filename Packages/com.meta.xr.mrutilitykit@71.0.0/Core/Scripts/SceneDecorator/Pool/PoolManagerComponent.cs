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
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public class PoolManagerComponent : MonoBehaviour
    {
        [Serializable]
        public abstract class CallbackProvider : MonoBehaviour
        {
            public abstract Pool<GameObject>.Callbacks GetPoolCallbacks();
        }

        [Serializable]
        internal class PoolableData : MonoBehaviour
        {
            internal Pool<GameObject> Pool;
            internal Vector3 Scale;
            internal MRUKAnchor Anchor;
        }

        [Serializable]
        internal struct PoolDesc
        {
            public enum PoolType
            {
                CIRCULAR
                , FIXED
            }

            public PoolType poolType;
            public GameObject primitive;
            public int size;
            public CallbackProvider callbackProviderOverride;
        }

        private static class DefaultCallbacks
        {
            public static GameObject Create(GameObject primitive)
            {
                var e = primitive.activeSelf;
                primitive.SetActive(false);
                var go = Instantiate(primitive, Vector3.zero, Quaternion.identity);
                primitive.SetActive(e);
                return go;
            }

            public static void OnGet(GameObject go)
            {
                go.SetActive(true);
            }

            public static void OnRelease(GameObject go)
            {
                go.SetActive(false);
            }
        }

        public static readonly Pool<GameObject>.Callbacks DEFAULT_CALLBACKS = new()
        {
            Create = DefaultCallbacks.Create,
            OnGet = DefaultCallbacks.OnGet,
            OnRelease = DefaultCallbacks.OnRelease,
        };

        [SerializeField] internal PoolDesc[] defaultPools;

        [NonSerialized] public PoolManager<GameObject, Pool<GameObject>> poolManager = new();

        protected internal virtual void InitDefaultPools(Pool<GameObject>.Callbacks? defaultCallbacks = null)
        {
            if (defaultCallbacks == null)
            {
                defaultCallbacks = DEFAULT_CALLBACKS;
            }

            foreach (PoolDesc pd in defaultPools)
            {
                Pool<GameObject>.Callbacks callbacks = defaultCallbacks.Value;
                CallbackProvider cp = pd.callbackProviderOverride == null
                    ? pd.primitive.GetComponent<CallbackProvider>()
                    : pd.callbackProviderOverride;
                if (cp != null)
                {
                    callbacks = cp.GetPoolCallbacks();
                }

                Pool<GameObject> pool;
                switch (pd.poolType)
                {
                    case PoolDesc.PoolType.FIXED:
                        pool = new FixedPool<GameObject>(pd.primitive, pd.size, callbacks);
                        break;
                    case PoolDesc.PoolType.CIRCULAR: //circular is default for now
                    default:
                        pool = new CircularPool<GameObject>(pd.primitive, pd.size, callbacks);
                        break;
                }

                poolManager.AddPool(pd.primitive, pool);
            }
        }

        /*
         * Create is a drop-in replacement for Instantiate that uses a pool if available.
         * Note that it is not named Instantiate so that it is easy to find & replace Instantiate
         *  calls with Create calls when switching to using Pools.
         */
        public GameObject Create(GameObject primitive, Vector3 position, Quaternion rotation, MRUKAnchor anchor, Transform parent = null)
        {
            Pool<GameObject> pool = poolManager.GetPool(primitive);
            if (pool == null)
            {
                return Instantiate(primitive, position, rotation, parent);
            }

            //Temporarily disable the OnGet callback,
            //as we only want to call it after we've adjusted the GameObject's transform
            Action<GameObject> onGet = pool.callbacks.OnGet;
            pool.callbacks.OnGet = null;

            GameObject go = pool.Get();
            if (go == null)
            {
                //If we are using a FixedPool, we have run out of pooled GameObjects
                pool.callbacks.OnGet = onGet;
                return null;
            }

            if (!go.TryGetComponent<PoolableData>(out var poolableData))
            {
                poolableData = go.AddComponent<PoolableData>();
            }

            poolableData.Scale = go.transform.localScale;
            poolableData.Anchor = anchor;
            poolableData.Pool = pool;

            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, rotation);

            onGet(go);

            //ensure the OnGet callback gets restored
            pool.callbacks.OnGet = onGet;

            return go;
        }

        public GameObject Create(GameObject primitive, MRUKAnchor anchor, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            Pool<GameObject> pool = poolManager.GetPool(primitive);
            if (pool == null)
            {
                return Instantiate(primitive, parent, instantiateInWorldSpace);
            }

            //Temporarily disable the OnGet callback,
            //as we only want to call it after we've adjusted the GameObject's transform
            Action<GameObject> onGet = pool.callbacks.OnGet;
            pool.callbacks.OnGet = null;

            GameObject go = pool.Get();
            if (go == null)
            {
                //if we are using a FixedPool, we have run out of pooled GameObjects
                pool.callbacks.OnGet = onGet;
                return null;
            }

            if (!go.TryGetComponent<PoolableData>(out var poolableData))
            {
                poolableData = go.AddComponent<PoolableData>();
            }

            poolableData.Scale = go.transform.localScale;
            poolableData.Anchor = anchor;
            poolableData.Pool = pool;

            go.transform.SetParent(parent);
            if (parent)
            {
                if (instantiateInWorldSpace)
                {
                    go.transform.SetPositionAndRotation(parent.position, parent.rotation);
                }
                else
                {
                    go.transform.localRotation = parent.localRotation;
                    go.transform.localPosition = parent.localPosition;
                }
            }

            onGet(go);

            //ensure the OnGet callback gets restored
            pool.callbacks.OnGet = onGet;
            return go;
        }

        public T Create<T>(T primitive, Vector3 position, Quaternion rotation, MRUKAnchor anchor, Transform parent = null) where T : Component
        {
            GameObject go = Create(primitive.gameObject, position, rotation, anchor, parent);
            return go == null ? null : go.GetComponent<T>();
        }

        public T Create<T>(T primitive, MRUKAnchor anchor, Transform parent = null, bool instantiateInWorldSpace = false) where T : Component
        {
            GameObject go = Create(primitive.gameObject, anchor, parent, instantiateInWorldSpace);
            return go == null ? null : go.GetComponent<T>();
        }


        /*
         * Release is a drop-in replacement for Destroy that releases into a pool if available.
         * Note that it is not named Destroy so that it is easy to find & replace Destroy
         *  calls with Release calls when switching to using Pools.
         */
        public void Release(GameObject go)
        {
            if (go.TryGetComponent<PoolableData>(out var poolableData) &&
                poolableData.Pool != null)
            {
                go.transform.localScale = poolableData.Scale;
                poolableData.Anchor = null;
                poolableData.Pool.Release(go);
            }
            else
            {
                Destroy(go);
            }
        }
    }
}
