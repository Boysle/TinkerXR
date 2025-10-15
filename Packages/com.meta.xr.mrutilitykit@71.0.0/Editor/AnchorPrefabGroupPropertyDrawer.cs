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

using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnchorPrefabSpawner.AnchorPrefabGroup))]
public class DropTablePropertyDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.indentLevel = 0;
        var prefabsProperty = property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.Prefabs));
        var labelsProperty = property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.Labels));
        var matchAspectRatioProperty =
            property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.MatchAspectRatio));
        var calculateFacingDirectionProperty =
            property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.CalculateFacingDirection));
        var scalingProperty = property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.Scaling));
        var alignmentProperty = property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.Alignment));
        var ignorePrefabSizeProperty =
            property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.IgnorePrefabSize));
        var prefabSelection =
            property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.PrefabSelection));

        // Display the labels names as the title of the property in the inspector
        var sceneLabelsFlags = (MRUKAnchor.SceneLabels)labelsProperty.enumValueFlag;

        var propertyName = (int)sceneLabelsFlags switch
        {
            0 => "Nothing",
            -1 => "Everything",
            _ => sceneLabelsFlags.ToString()
        };
        if (propertyName.Split(',').Length > 4)
        {
            propertyName = "Mixed..."; // to avoid overflowing the inspector
        }

        label.text = propertyName;

        property.isExpanded =
            EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded, label, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var propertyHeight = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(new Rect(position.x, position.y + propertyHeight, position.width, propertyHeight),
                labelsProperty);
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 2, position.width,
                    propertyHeight), prefabsProperty);
            // Account in the property height the number of prefabs in the list
            var adjustedPropertyHeight = propertyHeight;
            if (prefabsProperty.isExpanded)
            {
                adjustedPropertyHeight += propertyHeight * 2;
                for (var i = 0; i < prefabsProperty.arraySize - 1; i++)
                {
                    adjustedPropertyHeight += EditorGUI.GetPropertyHeight(prefabsProperty.GetArrayElementAtIndex(i));
                }
            }

            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 3 + adjustedPropertyHeight, position.width,
                    propertyHeight), matchAspectRatioProperty);
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 4 + adjustedPropertyHeight, position.width,
                    propertyHeight), calculateFacingDirectionProperty);
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 5 + adjustedPropertyHeight, position.width,
                    propertyHeight), scalingProperty);
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 6 + adjustedPropertyHeight, position.width,
                    propertyHeight), alignmentProperty);
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 7 + adjustedPropertyHeight, position.width,
                    propertyHeight), ignorePrefabSizeProperty);
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + propertyHeight * 8 + adjustedPropertyHeight, position.width,
                    propertyHeight), prefabSelection);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var totalHeight = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
        {
            return totalHeight;
        }

        totalHeight += EditorGUIUtility.singleLineHeight * 11; // 8 properties + 1 for the label + 2 extra padding lines
        var prefabsProperty = property.FindPropertyRelative(nameof(AnchorPrefabSpawner.AnchorPrefabGroup.Prefabs));
        if (!prefabsProperty.isExpanded)
        {
            return totalHeight;
        }

        totalHeight += EditorGUIUtility.singleLineHeight;
        for (var i = 0; i < prefabsProperty.arraySize - 1; i++)
        {
            totalHeight += EditorGUI.GetPropertyHeight(prefabsProperty.GetArrayElementAtIndex(i));
        }

        return totalHeight;
    }
}
