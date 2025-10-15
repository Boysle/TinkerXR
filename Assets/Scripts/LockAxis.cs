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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using TMPro;
using UnityEngine;

// Provides a axis-locking tool that allows users to selectively
// lock or unlock movement/scaling along the X, Y, or Z axes.
// Most of the base code is taken from the "UniformScaling" script
public class LockAxis : MonoBehaviour
{
    public static bool lockAxisOn = false;

    // The locked axes, if [0, 0, 0], no axis is locked
    // If [1, 1, 1], all axis are locked (should not be possible)
    // The represenation is as such [x, y, z]
    public static Vector3 lockedAxes = Vector3.zero;

    public GameObject movementModelParent, scalingAnchorModelParent;
    public LineRenderer lineRenderer;
    public Material redMat, greenMat, blueMat;
    public TextMeshPro overlayInfoText;
    private string[] colorfulStrings;

    // Parts about selecting the lock axis
    public FingerPinchValue fingerPinchValue; // Pinch value of the left hand index finger
    public Transform fingerTipPosition, axisSelectionCube, corners, indicatorSphere;
    public GameObject axisLockTool;

    private Transform activeCorner;

    void Start()
    {
        overlayInfoText.text = "active axes: <color=#FF0000> X</color> <color=#0000FF> Y</color> <color=#00FF00> Z</color>";
        lineRenderer.enabled = false;
        colorfulStrings = new string[3] { "<color=#FF0000> X</color>", "<color=#0000FF> Y</color>", "<color=#00FF00> Z</color>" }; // (Red, Blue, Green)

        axisLockTool.SetActive(false);

        // Deactivate each children of the axis lock tool corners
        foreach (Transform child in corners.transform)
        {
            child.gameObject.SetActive(false);
        }

        activeCorner = corners.GetChild(0);
    }
    
    void Update()
    {
        // When the left hand index finger is pinched, the axis lock tool should appear
        if (fingerPinchValue.Value() > 0.8f)
        {
            if (!axisLockTool.activeSelf) // If we did not turn the axis lock tool on, turn it on
            {
                axisLockTool.transform.position = fingerTipPosition.position + new Vector3(0.06f, 0.06f, 0.06f); // We can adjust this value as we wish
                axisLockTool.SetActive(true);
            }
            UpdateCubeAndLockAxis(FindClosestCorner());
        }

        // When the left hand index finger is not pinched anymore, the axis lock tool should disappear and the axis lock logic should apply
        if (axisLockTool.activeSelf && fingerPinchValue.Value() < 0.4f)
        {
            axisLockTool.SetActive(false);
        }
    }

    private Transform FindClosestCorner()
    {
        Transform closestChild = null;
        float closestDistance = Mathf.Infinity;

        // Iterate through each child of the parent object
        foreach (Transform child in corners.transform)
        {
            float distance = Vector3.Distance(child.position, fingerTipPosition.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChild = child;
            }
        }
        return closestChild;
    }

    private void UpdateCubeAndLockAxis(Transform closestCorner)
    {
        // If we have chosen a new corner
        if(closestCorner != activeCorner) 
        {
            activeCorner.gameObject.SetActive(false);
            closestCorner.gameObject.SetActive(true);
            activeCorner = closestCorner;
            indicatorSphere.position = closestCorner.position;
            Renderer sphereRenderer = indicatorSphere.GetComponent<Renderer>();
            switch (closestCorner.name)
            {
                case "Origin":
                    sphereRenderer.material.color = Color.white;
                    SetLockStates(new int[] { 0, 0, 0 }); // Lock nothing
                    break;
                case "X Corner":
                    sphereRenderer.material.color = Color.red;
                    SetLockStates(new int[] { 0, 1, 1 }); // Lock Y and Z
                    break;
                case "Y Corner":
                    sphereRenderer.material.color = Color.blue;
                    SetLockStates(new int[] { 1, 0, 1 }); // Lock X and Z
                    break;
                case "Z Corner":
                    sphereRenderer.material.color = Color.green;
                    SetLockStates(new int[] { 1, 1, 0 }); // Lock X and Y
                    break;
                case "XY Corner":
                    sphereRenderer.material.color = Color.magenta;
                    SetLockStates(new int[] { 0, 0, 1 }); // Lock Z
                    break;
                case "XZ Corner":
                    sphereRenderer.material.color = Color.yellow;
                    SetLockStates(new int[] { 0, 1, 0 }); // Lock Y
                    break;
                case "YZ Corner":
                    sphereRenderer.material.color = Color.cyan;
                    SetLockStates(new int[] { 1, 0, 0 }); // Lock X
                    break;
                default:
                    break;
            }
        }
    }

    private void SetLockStates(int[] lockStates)
    {
        if (lockStates.Length != 3)
        {
            Debug.LogError("Lock states array must have exactly 3 elements.");
            return;
        }

        string originalText = "active axes:";
        bool lockingAnAxis = false;

        // Iterate through each axis
        for (int i = 0; i < 3; i++)
        {
            // Set the lock state for the current axis
            lockedAxes[i] = lockStates[i];

            if (lockedAxes[i] == 1)
            {
                // Lock the axis (deactivate visuals)
                movementModelParent.transform.GetChild(i).gameObject.SetActive(false);
                scalingAnchorModelParent.transform.GetChild(i).gameObject.SetActive(false);
                lockingAnAxis = true;
            }
            else
            {
                // Unlock the axis (add to active list)
                originalText += colorfulStrings[i];

                // Activate visuals
                movementModelParent.transform.GetChild(i).gameObject.SetActive(true);
                scalingAnchorModelParent.transform.GetChild(i).gameObject.SetActive(true);
            }
        }

        // Update the overlay info text
        if (!lockingAnAxis)
        {
            lockAxisOn = false;
        }
        else
        {
            lockAxisOn = true;
        }

        overlayInfoText.text = originalText;
    }



    private void ToggleLock(int axis)
    {
        if (lockedAxes[axis] == 0)
        {
            // First check if the axis we are about to lock will cause all axes to be locked
            int counter = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i != axis && lockedAxes[i] == 1)
                {
                    counter++;
                }
            }
            if (counter >= 2)
            {
                Debug.LogError("Axis Lock Error: You cannot lock all axes at the same time!");
                return;
            }
            lockedAxes[axis] = 1;
        }
        else
        {
            lockedAxes[axis] = 0;
        }

        // Modify the debug text, setting the static "lockAxisOn" bool, and reconfiguring the movement handle and scaling anchor visuals
        string originalText = "locked axes:";
        bool noLocked = true;
        for (int i = 0; i < 3; i++)
        {
            if (lockedAxes[i] == 1)
            {
                originalText += colorfulStrings[i];
                noLocked = false;
                movementModelParent.transform.GetChild(i).gameObject.SetActive(false);
                scalingAnchorModelParent.transform.GetChild(i).gameObject.SetActive(false);
            }
            else
            {
                movementModelParent.transform.GetChild(i).gameObject.SetActive(true);
                scalingAnchorModelParent.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
        if (noLocked)
        {
            originalText += " none";
            lockAxisOn = false;
        }
        else
        {
            lockAxisOn = true;
        }
        overlayInfoText.text = originalText;
    }
}