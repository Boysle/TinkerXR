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

[CustomEditor(typeof(GridSliceResizer))]
public class GridSliceResizerEditor : Editor
{
    private SerializedProperty _originalMesh;

    private SerializedProperty _borderXNegative;
    private SerializedProperty _borderXPositive;
    private SerializedProperty _borderYNegative;
    private SerializedProperty _borderYPositive;
    private SerializedProperty _borderZNegative;
    private SerializedProperty _borderZPositive;

    private SerializedProperty _pivotOffset;
    private SerializedProperty _stretchCenter;

    private SerializedProperty _scalingX;
    private SerializedProperty _scalingY;
    private SerializedProperty _scalingZ;

    private GUIStyle _underText;
    private SerializedProperty _updateInPlayMode;

    private void OnEnable()
    {
        _originalMesh = serializedObject.FindProperty(nameof(GridSliceResizer.OriginalMesh));
        _pivotOffset = serializedObject.FindProperty(nameof(GridSliceResizer.PivotOffset));
        _stretchCenter = serializedObject.FindProperty(nameof(GridSliceResizer.StretchCenter));
        _scalingX = serializedObject.FindProperty(nameof(GridSliceResizer.ScalingX));
        _scalingY = serializedObject.FindProperty(nameof(GridSliceResizer.ScalingY));
        _scalingZ = serializedObject.FindProperty(nameof(GridSliceResizer.ScalingZ));
        _borderXNegative = serializedObject.FindProperty(nameof(GridSliceResizer.BorderXNegative));
        _borderYNegative = serializedObject.FindProperty(nameof(GridSliceResizer.BorderYNegative));
        _borderZNegative = serializedObject.FindProperty(nameof(GridSliceResizer.BorderZNegative));
        _borderXPositive = serializedObject.FindProperty(nameof(GridSliceResizer.BorderXPositive));
        _borderYPositive = serializedObject.FindProperty(nameof(GridSliceResizer.BorderYPositive));
        _borderZPositive = serializedObject.FindProperty(nameof(GridSliceResizer.BorderZPositive));
        _updateInPlayMode = serializedObject.FindProperty(nameof(GridSliceResizer.UpdateInPlayMode));
        _underText = new GUIStyle
        {
            fontStyle = FontStyle.Italic,
            normal =
            {
                textColor = Color.gray
            }
        };
    }


    public override void OnInspectorGUI()
    {
        var resizable = target as GridSliceResizer;
        if (!resizable)
        {
            return;
        }

        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(),
                false);
        }

        EditorGUILayout.PropertyField(_updateInPlayMode);
        EditorGUILayout.PropertyField(_originalMesh);
        EditorGUILayout.PropertyField(_pivotOffset);
        EditorGUILayout.PropertyField(_stretchCenter, new GUIContent("Stretch Center",
            "Determines whether the center part of the object should be scaled per each axis."));
        EditorGUILayout.PropertyField(_scalingX,
            new GUIContent("Scaling X", "The scaling method applied on the X axis"));
        EditorGUI.indentLevel++;
        switch (resizable.ScalingX)
        {
            case GridSliceResizer.Method.SLICE:
                EditorGUILayout.PropertyField(_borderXNegative,
                    new GUIContent("Border X",
                        "Define a symmetrical border area that will not be stretched on this axis"));
                break;
            case GridSliceResizer.Method.SLICE_WITH_ASYMMETRICAL_BORDER:
                EditorGUILayout.PropertyField(_borderXNegative,
                    new GUIContent("Border X Negative",
                        "Lower bound of an asymmetrical border area that will not be stretched on this axis"));
                EditorGUILayout.PropertyField(_borderXPositive,
                    new GUIContent("Border X Positive",
                        "Upper bound of an asymmetrical border area that will not be stretched on this axis"));
                break;
            case GridSliceResizer.Method.SCALE:
            default:
                CreateUnderText("Unity's default scaling method", _underText);
                break;
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.PropertyField(_scalingY,
            new GUIContent("Scaling Y", "The scaling method applied on the Y axis"));
        EditorGUI.indentLevel++;
        switch (resizable.ScalingY)
        {
            case GridSliceResizer.Method.SLICE:
                EditorGUILayout.PropertyField(_borderYNegative,
                    new GUIContent("Border Y",
                        "Define a symmetrical border area that will not be stretched on this axis"));
                break;
            case GridSliceResizer.Method.SLICE_WITH_ASYMMETRICAL_BORDER:
                EditorGUILayout.PropertyField(_borderYNegative,
                    new GUIContent("Border Y Negative",
                        "Lower bound of an asymmetrical border area that will not be stretched on this axis"));
                EditorGUILayout.PropertyField(_borderYPositive,
                    new GUIContent("Padding Y Positive",
                        "Upper bound of an asymmetrical border area that will not be stretched on this axis"));
                break;
            case GridSliceResizer.Method.SCALE:
            default:
                CreateUnderText("Unity's default scaling method", _underText);
                break;
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.PropertyField(_scalingZ,
            new GUIContent("Scaling Z", "The scaling method applied on the Z axis"));
        EditorGUI.indentLevel++;
        switch (resizable.ScalingZ)
        {
            case GridSliceResizer.Method.SLICE:
                EditorGUILayout.PropertyField(_borderZNegative,
                    new GUIContent("Border Z",
                        "Define a symmetrical border area that will not be stretched on this axis"));
                break;
            case GridSliceResizer.Method.SLICE_WITH_ASYMMETRICAL_BORDER:
                EditorGUILayout.PropertyField(_borderZNegative,
                    new GUIContent("Border Z Negative",
                        "Lower bound of an asymmetrical border area that will not be stretched on this axis"));
                EditorGUILayout.PropertyField(_borderZPositive,
                    new GUIContent("Border Z Positive",
                        "Upper bound of an asymmetrical border area that will not be stretched on this axis"));
                break;
            case GridSliceResizer.Method.SCALE:
            default:
                CreateUnderText("Unity's default scaling method", _underText);
                break;
        }

        EditorGUI.indentLevel--;
        serializedObject.ApplyModifiedProperties();
    }

    private static void CreateUnderText(string label, GUIStyle style)
    {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField(new GUIContent(label), style);
        EditorGUI.indentLevel--;
    }
}
