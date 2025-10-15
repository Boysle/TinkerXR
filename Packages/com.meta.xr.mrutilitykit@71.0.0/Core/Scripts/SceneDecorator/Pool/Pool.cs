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
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public abstract class Pool<T> where T : class
    {
        protected struct Entry
        {
            public bool active;
            public T t;
        }

        protected Entry[] pool;
        protected Dictionary<T, int> indices;
        protected int index;

        public struct Callbacks
        {
            public Func<T, T> Create;
            public Action<T> OnGet, OnRelease;
        }

        protected abstract int CountAll
        {
            get;
        }

        protected abstract int CountActive
        {
            get;
        }

        public virtual int CountInactive => CountAll - CountActive;

        public Callbacks callbacks;

        public abstract T Get();
        public abstract void Release(T t);

        protected void Swap(int i0, int i1)
        {
            indices[pool[i0].t] = i1;
            indices[pool[i1].t] = i0;

            (pool[i0], pool[i1]) = (pool[i1], pool[i0]);
        }
    }
}
