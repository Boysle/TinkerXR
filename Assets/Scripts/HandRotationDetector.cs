/*
MIT License

Copyright (c) 2025 Oðuz Arslan, Artun Akdoðan, Mustafa Doða Doðan

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum HandRotationState
{
    Up, // Palm facing upwards
    Down, // Palm facing downwards
    Right // Thumb pointing upwards
}

// Monitors the orientation of a hand (or other target object) and updates UI and interaction modes based on its rotation.
// It detects whether the hand is Up, Down, or Right using dot products between the hand’s axes and world directions.
public class HandRotationDetector : MonoBehaviour
{
    public GameObject targetObject; // The GameObject we want to check, in this case, this is our hand
    public float dotProductThreshold = 0.5f; // Threshold for dot product comparisons
    public TextMeshProUGUI enabledSelectionInfoText, disabledSelectionInfoText, enabledSnapInfoText, disabledSnapInfoText, enabledUniformScalingInfoText;
    public Image enabledSelectionImage, disabledSelectionImage, enabledSnapImage, disabledSnapImage, enabledUniformScalingImage;
    public Image upBg, downBg, rightBg;
    public TextMeshProUGUI selectionInfoText, movementInfoText;
    private bool currentGrabBool = false, firstTimeBool = true;
    public static bool receivedGrabBool = false;
    public HandRotationState handState = HandRotationState.Down;

    void Update()
    {
        // Check if the targetObject is looking left or right
        if (targetObject != null)
        {
            // Get the up vector of the plane
            Vector3 up = targetObject.transform.up;
            // Calculate the dot product between the plane's up vector and the world up vector
            float dotProductUp = Vector3.Dot(up, Vector3.up);

            // Here, forward is along fingers, right is along thumb up, up is along inside of palm
            Vector3 right = targetObject.transform.up; 
            float dotProductRight = Mathf.Max(Mathf.Abs(Vector3.Dot(right, Vector3.right)), Mathf.Abs(Vector3.Dot(right, Vector3.forward)));

            float maxDotProduct = Mathf.Max(Mathf.Abs(dotProductUp), Mathf.Abs(dotProductRight));

            // If the dot product of the Up/Down direction is higher, choose either one of them
            if (maxDotProduct == Mathf.Abs(dotProductUp))
            {
                // In the case of left hand looking up run once
                if (dotProductUp > dotProductThreshold && (handState != HandRotationState.Up || currentGrabBool != receivedGrabBool || firstTimeBool))
                {
                    firstTimeBool = false;
                    handState = HandRotationState.Up;
                    if (currentGrabBool != receivedGrabBool) { currentGrabBool = !currentGrabBool; }
                    // Debug.Log("The targetObject is looking up.");
                    selectionInfoText.text = "multiple selection";
                    movementInfoText.text = "snap to grid";
                    upBg.gameObject.SetActive(true);
                    downBg.gameObject.SetActive(false);
                    rightBg.gameObject.SetActive(false);
                    // In case of no object manipulation
                    if (!ObjectGrabDetector._isGrabbingUIElement)
                    {
                        Selection.selectedCtrl = true;
                        UniformScaling.uniformScalingOn = false;
                        // We could make a function to turn one on and turn the others off here
                        enabledSelectionInfoText.alpha = 1.0f;
                        disabledSelectionInfoText.alpha = 0.0f;
                        enabledSnapInfoText.alpha = 0.0f;
                        disabledSnapInfoText.alpha = 0.0f;
                        enabledUniformScalingInfoText.alpha = 0.0f;
                        enabledSelectionImage.gameObject.SetActive(true);
                        disabledSelectionImage.gameObject.SetActive(false);
                        enabledSnapImage.gameObject.SetActive(false);
                        disabledSnapImage.gameObject.SetActive(false);
                        enabledUniformScalingImage.gameObject.SetActive(false);
                    }
                    // In case of object manipulation
                    else
                    {
                        SnapToGrid.snappingOn = true;
                        UniformScaling.uniformScalingOn = false;
                        enabledSelectionInfoText.alpha = 0.0f;
                        disabledSelectionInfoText.alpha = 0.0f;
                        enabledSnapInfoText.alpha = 1.0f;
                        disabledSnapInfoText.alpha = 0.0f;
                        enabledUniformScalingInfoText.alpha = 0.0f;
                        enabledSelectionImage.gameObject.SetActive(false);
                        disabledSelectionImage.gameObject.SetActive(false);
                        enabledSnapImage.gameObject.SetActive(true);
                        disabledSnapImage.gameObject.SetActive(false);
                        enabledUniformScalingImage.gameObject.SetActive(false);
                    }
                }
                // In the case of left hand looking down run once
                else if (dotProductUp < -dotProductThreshold && (handState != HandRotationState.Down || currentGrabBool != receivedGrabBool || firstTimeBool))
                {
                    firstTimeBool = false;
                    handState = HandRotationState.Down;
                    if (currentGrabBool != receivedGrabBool) { currentGrabBool = !currentGrabBool; }
                    // Debug.Log("The targetObject is looking down.");
                    selectionInfoText.text = "single selection";
                    movementInfoText.text = "free movement";
                    upBg.gameObject.SetActive(false);
                    downBg.gameObject.SetActive(true);
                    rightBg.gameObject.SetActive(false);
                    // In case of no object manipulation
                    if (!ObjectGrabDetector._isGrabbingUIElement)
                    {
                        Selection.selectedCtrl = false;
                        UniformScaling.uniformScalingOn = false;
                        enabledSelectionInfoText.alpha = 0.0f;
                        disabledSelectionInfoText.alpha = 1.0f;
                        enabledSnapInfoText.alpha = 0.0f;
                        disabledSnapInfoText.alpha = 0.0f;
                        enabledUniformScalingInfoText.alpha = 0.0f;
                        enabledSelectionImage.gameObject.SetActive(false);
                        disabledSelectionImage.gameObject.SetActive(true);
                        enabledSnapImage.gameObject.SetActive(false);
                        disabledSnapImage.gameObject.SetActive(false);
                        enabledUniformScalingImage.gameObject.SetActive(false);
                    }
                    // In case of object manipulation
                    else
                    {
                        // RecalibrateCornerButton(Selection.selectionManipulationUIObject.transform);
                        SnapToGrid.snappingOn = false;
                        UniformScaling.uniformScalingOn = false;
                        enabledSelectionInfoText.alpha = 0.0f;
                        disabledSelectionInfoText.alpha = 0.0f;
                        enabledSnapInfoText.alpha = 0.0f;
                        disabledSnapInfoText.alpha = 1.0f;
                        enabledUniformScalingInfoText.alpha = 0.0f;
                        enabledSelectionImage.gameObject.SetActive(false);
                        disabledSelectionImage.gameObject.SetActive(false);
                        enabledSnapImage.gameObject.SetActive(false);
                        disabledSnapImage.gameObject.SetActive(true);
                        enabledUniformScalingImage.gameObject.SetActive(false);
                    }
                }
            }
            // If the dot product of the Right/Left direction is higher, choose that
            else 
            {
                // In the case of left hand looking right/left run once
                if (Mathf.Abs(dotProductRight) > dotProductThreshold && (handState != HandRotationState.Right || currentGrabBool != receivedGrabBool || firstTimeBool) && !LockAxis.lockAxisOn)
                {
                    firstTimeBool = false;
                    handState = HandRotationState.Right;
                    if (currentGrabBool != receivedGrabBool) { currentGrabBool = !currentGrabBool; }
                    // Debug.Log("The targetObject is looking right.");
                    movementInfoText.text = "uniform scaling";
                    upBg.gameObject.SetActive(false);
                    downBg.gameObject.SetActive(false);
                    rightBg.gameObject.SetActive(true);
                    // In case of no object manipulation
                    if (!ObjectGrabDetector._isGrabbingUIElement || true)
                    {
                        // Put in the thing that will turn uniform scaling on
                        SnapToGrid.snappingOn = false;
                        UniformScaling.uniformScalingOn = true;

                        enabledSelectionInfoText.alpha = 0.0f;
                        disabledSelectionInfoText.alpha = 0.0f;
                        enabledSnapInfoText.alpha = 0.0f;
                        disabledSnapInfoText.alpha = 0.0f;
                        enabledUniformScalingInfoText.alpha = 1.0f;
                        enabledSelectionImage.gameObject.SetActive(false);
                        disabledSelectionImage.gameObject.SetActive(false);
                        enabledSnapImage.gameObject.SetActive(false);
                        disabledSnapImage.gameObject.SetActive(false);
                        enabledUniformScalingImage.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    private void RecalibrateCornerButton(Transform trans)
    {
        if (trans.tag == "Corner Scaler")
        {
            Transform rendererTransform = trans.GetChild(0);
            Vector3 diff = rendererTransform.position - trans.position;
            trans.position += diff;
            rendererTransform.localPosition = new Vector3(0, 0, 0);
        }
    }
}

