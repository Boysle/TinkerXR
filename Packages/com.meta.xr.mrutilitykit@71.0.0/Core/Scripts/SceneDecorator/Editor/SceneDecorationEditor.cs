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

#if UNITY_EDITOR
using System;
using System.Collections.ObjectModel;
using Meta.XR.Util;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    [CustomEditor(typeof(SceneDecoration))]
    [Feature(Feature.Scene)]
    public class SceneDecorationEditor : Editor
    {
        private static readonly Type[] maskTypes = new Type[]
        {
            typeof(AnchorComponentDistanceMask),
            typeof(AnchorDistanceMask),
            typeof(CellularNoiseMask),
            typeof(ColliderMask),
            typeof(CompositeMaskAdd),
            typeof(CompositeMaskAvg),
            typeof(CompositeMaskMax),
            typeof(CompositeMaskMin),
            typeof(CompositeMaskMul),
            typeof(ConstantMask),
            typeof(CookieMask),
            typeof(HeightMask),
            typeof(InsideCurrentRoomMask),
            typeof(NotInsideMask),
            typeof(RandomMask),
            typeof(RayDistanceMask),
            typeof(SimplexNoiseMask),
            typeof(SlopeMask),
            typeof(SpaceMapMask),
            typeof(SpaceMapGPUMask),
            typeof(StochasticMask)
        };

        private static readonly Type[] modifierTypes = new Type[]
        {
            typeof(DontDestroyOnLoadModifier),
            typeof(KeepUprightWithAnchorModifier),
            typeof(KeepUprightWithSurfaceModifier),
            typeof(RotationModifier),
            typeof(RotationModifierSpaceMap),
            typeof(ScaleModifier),
            typeof(ScaleUniformModifier)
        };

        private static readonly string[] excludedProperties = new string[] { "masks", "modifiers" };

        private bool masksVisible;
        private bool modifiersVisible;

        GenericMenu maskAddMenu;
        GenericMenu modifierAddMenu;

        ReorderableList maskList;
        ReorderableList modifierList;

        private void OnEnable()
        {
            maskAddMenu = new GenericMenu();
            foreach (Type maskType in maskTypes)
            {
                maskAddMenu.AddItem(new GUIContent(maskType.Name), false, CreateMask, maskType);
            }

            modifierAddMenu = new GenericMenu();
            foreach (Type modifierType in modifierTypes)
            {
                modifierAddMenu.AddItem(new GUIContent(modifierType.Name), false, CreateModifier, modifierType);
            }

            SerializedProperty arrayProp = serializedObject.FindProperty("masks");
            maskList = new ReorderableList(arrayProp.serializedObject, arrayProp, true, false, true, true)
            {
                multiSelect = true,
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.x += 10;
                    rect.width -= 10;
                    EditorGUI.PropertyField(rect, maskList.serializedProperty.GetArrayElementAtIndex(index));
                },
                elementHeightCallback = (int index) =>
                {
                    return EditorGUI.GetPropertyHeight(maskList.serializedProperty.GetArrayElementAtIndex(index));
                },
                onAddCallback = (ReorderableList list) =>
                {
                    maskAddMenu.ShowAsContext();
                },
                onRemoveCallback = (ReorderableList list) =>
                {
                    ReadOnlyCollection<int> deleteIndices = list.selectedIndices.Count > 0 ? list.selectedIndices : new ReadOnlyCollection<int>(new int[] { list.index });

                    foreach (int index in deleteIndices)
                    {
                        DeleteSubAsset(maskList.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue);
                    }

                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };

            arrayProp = serializedObject.FindProperty("modifiers");
            modifierList = new ReorderableList(arrayProp.serializedObject, arrayProp, true, false, true, true)
            {
                multiSelect = true,
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.x += 10;
                    rect.width -= 10;
                    EditorGUI.PropertyField(rect, modifierList.serializedProperty.GetArrayElementAtIndex(index));
                },
                elementHeightCallback = (int index) =>
                {
                    return EditorGUI.GetPropertyHeight(modifierList.serializedProperty.GetArrayElementAtIndex(index));
                },
                onAddCallback = (ReorderableList list) =>
                {
                    modifierAddMenu.ShowAsContext();
                },
                onRemoveCallback = (ReorderableList list) =>
                {
                    ReadOnlyCollection<int> deleteIndices = list.selectedIndices.Count > 0 ? list.selectedIndices : new ReadOnlyCollection<int>(new int[] { list.index });

                    foreach (int index in deleteIndices)
                    {
                        DeleteSubAsset(modifierList.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue);
                    }

                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, excludedProperties);

            masksVisible = EditorGUILayout.Foldout(masksVisible, "Masks", true);
            if (masksVisible)
            {
                maskList.DoLayoutList();
            }

            modifiersVisible = EditorGUILayout.Foldout(modifiersVisible, "Modifiers", true);
            if (modifiersVisible)
            {
                modifierList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateMask(object userData)
        {
            Undo.RecordObjects(serializedObject.targetObjects, "Create Mask");

            Type maskType = (Type)userData;
            Mask mask = (Mask)ScriptableObject.CreateInstance(maskType);
            mask.name = maskType.Name;
            EditorUtility.SetDirty(mask);

            foreach (Object obj in serializedObject.targetObjects)
            {
                SceneDecoration sceneDecoration = (SceneDecoration)obj;
                Array.Resize(ref sceneDecoration.masks, sceneDecoration.masks.Length + 1);
                sceneDecoration.masks[sceneDecoration.masks.Length - 1] = mask;

                AssetDatabase.AddObjectToAsset(mask, obj);
                EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }

        private void CreateModifier(object userData)
        {
            Undo.RecordObjects(serializedObject.targetObjects, "Create Modifier");

            Type modifierType = (Type)userData;
            Modifier modifier = (Modifier)ScriptableObject.CreateInstance(modifierType);
            modifier.name = modifierType.Name;
            EditorUtility.SetDirty(modifier);

            foreach (Object obj in serializedObject.targetObjects)
            {
                SceneDecoration sceneDecoration = (SceneDecoration)obj;
                Array.Resize(ref sceneDecoration.modifiers, sceneDecoration.modifiers.Length + 1);
                sceneDecoration.modifiers[sceneDecoration.modifiers.Length - 1] = modifier;

                AssetDatabase.AddObjectToAsset(modifier, obj);
                EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }

        private void DeleteSubAsset(Object subAsset)
        {
            Undo.DestroyObjectImmediate(subAsset);

            foreach (Object obj in serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
