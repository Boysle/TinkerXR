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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Meta.XR.MRUtilityKit.Tests
{
    public class MRUKTestBase : MonoBehaviour
    {
        protected const int DefaultTimeoutMs = 10000;

        protected IEnumerator LoadScene(string sceneToLoad, bool awaitMRUKInit = true)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(sceneToLoad,
                new LoadSceneParameters(LoadSceneMode.Single));
            if (awaitMRUKInit)
            {
                yield return new WaitUntil(() => MRUK.Instance.IsInitialized);
            }
        }

        protected IEnumerator UnloadScene()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 1; i--)
            {
                var asyncOperation =
                    SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i).name); // Clear/reset scene
                yield return new WaitUntil(() => asyncOperation.isDone);
            }
        }
    }
}

