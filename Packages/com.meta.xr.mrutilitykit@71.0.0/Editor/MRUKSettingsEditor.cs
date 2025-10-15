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

[CustomPropertyDrawer(typeof(ShowWhenAttribute))]
public class ShowWhenDrawer : PropertyDrawer
{
    private bool _showField = true;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var showWhenAttribute = (ShowWhenAttribute)attribute;
        var conditionField = property.serializedObject.FindProperty(showWhenAttribute.ConditionFieldName);

        if (conditionField == null)
        {
            ShowError(position, label, "Error getting the condition Field. Check the name.");
            return;
        }

        switch (conditionField.propertyType)
        {
            case SerializedPropertyType.Boolean:
                try
                {
                    var val = showWhenAttribute.CompareValue == null || (bool)showWhenAttribute.CompareValue;
                    _showField = conditionField.boolValue == val;
                }
                catch
                {
                    ShowError(position, label, "Invalid comparison Value Type");
                    return;
                }
                break;
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Float:
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.String:
            case SerializedPropertyType.Color:
            case SerializedPropertyType.ObjectReference:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Vector4:
            case SerializedPropertyType.Rect:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character:
            case SerializedPropertyType.AnimationCurve:
            case SerializedPropertyType.Bounds:
            case SerializedPropertyType.Gradient:
            case SerializedPropertyType.Quaternion:
            case SerializedPropertyType.ExposedReference:
            case SerializedPropertyType.FixedBufferSize:
            case SerializedPropertyType.Vector2Int:
            case SerializedPropertyType.Vector3Int:
            case SerializedPropertyType.RectInt:
            case SerializedPropertyType.BoundsInt:
            case SerializedPropertyType.ManagedReference:
            case SerializedPropertyType.Hash128:
            default:
                ShowError(position, label, "This type has not supported.");
                return;
        }

        if (_showField)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (_showField)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private void ShowError(Rect position, GUIContent label, string errorText)
    {
        EditorGUI.LabelField(position, label, new GUIContent(errorText));
        _showField = true;
    }
}
