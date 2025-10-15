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
using UnityEngine.Assertions;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [Feature(Feature.Scene)]
    public static class SingletonMonoBehaviour
    {
        /// <summary>
        /// Defines settings for the instantiation of singleton MonoBehaviours.
        /// </summary>
        public class InstantiationSettings : Attribute
        {
            public bool dontDestroyOnLoad;
        }
    }

    /// <summary>
    ///     A base class for creating singleton MonoBehaviours that can be instantiated in code or as part of the scene.<br />
    ///     <br />
    ///     If unspecified, SingletonMonoBehaviours <b>are</b> destroyed when a scene change occurs.<br />
    ///     Use the <see cref="SingletonMonoBehaviour.InstantiationSettings"/> attribute to change the DontDestroyOnLoad behaviour.
    /// </summary>
    /// <remarks>Consider setting [DefaultExecutionOrder(-1)] on the derived class.</remarks>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var existingInstances = FindObjectsOfType<T>(true);

                    // We don't handle multiple singletons in the scene, make the user clean it up
                    Assert.IsFalse(existingInstances.Length > 1,
                        $"There are {existingInstances.Length} instances of {typeof(T)} in the scene. Only one instance may exist.");

                    if (existingInstances.Length > 0)
                    {
                        _instance = existingInstances[0];
                    }
                }

                return _instance;
            }
        }

        private static void InitializeSingleton()
        {
            var attribute = Attribute.GetCustomAttribute(typeof(T), typeof(SingletonMonoBehaviour.InstantiationSettings));
            if (attribute is SingletonMonoBehaviour.InstantiationSettings instantiationSettings)
            {
                if (instantiationSettings.dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(_instance.transform);
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                InitializeSingleton();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"An instance of {typeof(T)} already exists, destroying this instance.");
                Destroy(this);
            }
        }

        protected virtual void OnDestroy()
        {
            _instance = null;
        }
    }
}
