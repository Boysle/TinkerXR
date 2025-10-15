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

using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.Editor
{
    [InitializeOnLoad]
    internal static class OVRProjectSetupMRUK
    {
        static OVRProjectSetupMRUK()
        {
            // Add components that depend on DepthAPI to PST

            // Scene requirement support
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Features,
                isDone: buildTargetGroup =>
                {
                    var ovrCameraRig = FindComponentInScene<OVRCameraRig>();
                    var sceneSupport = OVRProjectConfig.CachedProjectConfig.sceneSupport;
                    return
                        ovrCameraRig == null ||
                        sceneSupport == OVRProjectConfig.FeatureSupport.Supported ||
                        sceneSupport == OVRProjectConfig.FeatureSupport.Required;
                },
                message: "MR Utility Kit recommends Scene Support to be set to \"Required\"",
                fix: buildTargetGroup =>
                {
                    var projectConfig = OVRProjectConfig.CachedProjectConfig;
                    projectConfig.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
                    projectConfig.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
                    OVRProjectConfig.CommitProjectConfig(projectConfig);
                },
                fixMessage: "Set Scene Support to \"Required\" in the project config"
            );
        }
        private static T FindComponentInScene<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var rootGameObject in rootGameObjects)
            {
                if (rootGameObject.TryGetComponent(out T foundComponent))
                {
                    return foundComponent;
                }
            }
            return null;
        }
    }
}
