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
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

// This script handles the snap to grid movements
// Normally, the resizing with the corner scalers is handled solely inside 
public class SnapToGrid : MonoBehaviour
{
    public RectangularPrismCreator rectangularPrismCreatorReference;
    public CubeHighlighter cubeHighlighterReference;

    public GameObject gizmoPrefab, anchorObject, manipulationTool, movementHandle; // The prefab to instantiate
    public GameObject plane;  // The plane on which to place the objects
    public static float gridSpacing = 0.02f; // The spacing between objects
    public float ceilingHeight = 0.1f; // The height of the ceiling above the plane
    private float xFault, yFault, zFault;
    private bool currentSnapping = false, holdingObject = false, holdingMovementObject = false, initializerBool = false;

    private static GameObject staticManipulationTool;
    public static float staticGridSpacing, staticxFault = 0, staticyFault = 0, staticzFault = 0;
    public static bool snappingOn;
    private GameObject obj;
    private int iterCounter;
    private Vector3 initialPos;

    public void Awake()
    {
        staticManipulationTool = manipulationTool;
        staticGridSpacing = gridSpacing;
        iterCounter = 0;
    }

    public void UpdateGridCoordinates()
    {
        /*
        // Get the plane's dimensions
        MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
        Vector3 planeSize = planeRenderer.bounds.size;

        // Calculate the start position in the middle of the plane
        Vector3 startPosition = planeRenderer.bounds.center;

        // Calculate the number of objects to place along each axis
        int xCount = Mathf.FloorToInt(planeSize.x / gridSpacing);
        int yCount = Mathf.FloorToInt(ceilingHeight / gridSpacing);
        int zCount = Mathf.FloorToInt(planeSize.z / gridSpacing);

        // Loop through each position in the grid and instantiate the prefab
        for (int x = -xCount / 2; x <= xCount / 2; x++)
        {
            for (int y = 0; y <= yCount; y++)
            {
                for (int z = -zCount / 2; z <= zCount / 2; z++)
                {
                    Vector3 position = startPosition + new Vector3(x * gridSpacing, y * gridSpacing, z * gridSpacing);
                    Instantiate(gizmoPrefab, position, Quaternion.identity);
                }
            }
        }
        */

        // Calculating displacement of grid locations in default world positions (0, 0, 0)
        Vector3 pos = plane.transform.position;
        xFault = pos.x - (gridSpacing * MathF.Floor(pos.x / gridSpacing));
        yFault = pos.y - (gridSpacing * MathF.Floor(pos.y / gridSpacing));
        zFault = pos.z - (gridSpacing * MathF.Floor(pos.z / gridSpacing));
        staticxFault = xFault;
        staticyFault = yFault;
        staticzFault = zFault;
    }

    public void Update()
    {
        // Run if snap to grid or locking axis is on while we are grabbing the corner scaler for resizing
        if ((snappingOn || LockAxis.lockAxisOn) && ObjectGrabDetector._isGrabbingCornerScaler)
        {
            currentSnapping = true;
            if (!holdingObject) 
            { 
                holdingObject = true; 
            }
            if (!initializerBool)
            {
                initializerBool = true;
                initialPos = Selection.selectionManipulationUIObject.transform.position;
            }
            obj = Selection.selectionManipulationUIObject;
            // This function also looks at locked axis requirements
            Vector3 bestGridPos = FindCornerGridPos(obj.transform.position, initialPos);
            // If the object changed a grid location, move the renderers
            if (bestGridPos != obj.transform.position)
            {
                // Getting the mesh renderer child object of the corner scaler and the anchor object
                obj.transform.GetChild(0).position = bestGridPos;
                anchorObject.transform.position = bestGridPos;
                UpdateManipulationToolWithInvoke();
            }
        }

        // If we stopped grabbing the corner button, run once
        if (holdingObject && !ObjectGrabDetector._isGrabbingCornerScaler)
        {
            initializerBool = false;
            obj.transform.position = anchorObject.transform.position;
            iterCounter++;
            // In case the position of the corner button is overwritten, loop through this with update
            if (iterCounter > 5)
            {
                if (obj.transform.position == anchorObject.transform.position)
                {
                    holdingObject = false;
                    UpdateManipulationToolWithInvoke();
                }
                iterCounter = 0;
            }
        }

        // Update the manipulation cube once as we turn snapping off while holding the scaler
        if (!snappingOn && ObjectGrabDetector._isGrabbingCornerScaler && currentSnapping)
        {
            currentSnapping = !currentSnapping;
            UpdateManipulationToolWithInvoke();
        }

        if (snappingOn && ObjectGrabDetector._isGrabbingMovementHandle)
        {
            if (!holdingMovementObject) { holdingMovementObject = true; }
        }

        // If we stopped grabbing the movement handle, run once
        if (holdingMovementObject && !ObjectGrabDetector._isGrabbingMovementHandle)
        {
            movementHandle.transform.position = CubeHighlighter.expectedHandlePos;
            iterCounter++;
            // In case the position of the corner button is overwritten, loop through this with update
            if (iterCounter > 5)
            {
                if (movementHandle.transform.position == CubeHighlighter.expectedHandlePos)
                {
                    holdingMovementObject = false;
                    UpdateManipulationToolWithInvoke();
                }
                iterCounter = 0;
            }
        }
    }

    // Searches and returns to the closest available grid position to the corner
    private Vector3 FindCornerGridPos(Vector3 pos, Vector3 initialPos)
    {
        /*
        float x = pos.x - xFault, y = pos.y - yFault, z = pos.z - zFault;

        // Find the closest multiple of the increment
        float closestxValue = Mathf.Round(x / gridSpacing) * gridSpacing;
        float closestyValue = Mathf.Round(y / gridSpacing) * gridSpacing;
        float closestzValue = Mathf.Round(z / gridSpacing) * gridSpacing;

        // Check if the closest value is greater than the given value
        if (closestxValue > x) { closestxValue -= gridSpacing; }
        if (closestyValue > y) { closestyValue -= gridSpacing; }
        if (closestzValue > z) { closestzValue -= gridSpacing; }

        return new Vector3(closestxValue + xFault, closestyValue + yFault, closestzValue + zFault);
        */

        Vector3 finalPosition = pos;

        // Check if locking is enabled
        if (LockAxis.lockAxisOn)
        {
            // Get the locked axes
            Vector3 lockedAxes = LockAxis.lockedAxes;

            // If all axes are locked, return the original position (no snapping or adjustment)
            if (lockedAxes == Vector3.one)
                return initialPos;

            // Step 1: Determine the closest point on the line/plane based on the locked axes
            Vector3 closestPoint = initialPos;

            if (lockedAxes == new Vector3(1, 0, 0)) // Locked X-axis (line along YZ-plane)
            {
                closestPoint.x = initialPos.x; // X is fixed
                closestPoint.y = obj.transform.position.y;
                closestPoint.z = obj.transform.position.z;
            }
            else if (lockedAxes == new Vector3(0, 1, 0)) // Locked Y-axis (line along XZ-plane)
            {
                closestPoint.x = obj.transform.position.x;
                closestPoint.y = initialPos.y; // Y is fixed
                closestPoint.z = obj.transform.position.z;
            }
            else if (lockedAxes == new Vector3(0, 0, 1)) // Locked Z-axis (line along XY-plane)
            {
                closestPoint.x = obj.transform.position.x;
                closestPoint.y = obj.transform.position.y;
                closestPoint.z = initialPos.z; // Z is fixed
            }
            else if (lockedAxes == new Vector3(1, 1, 0)) // Locked X and Y axes (plane along XY)
            {
                closestPoint.x = initialPos.x; // X is fixed
                closestPoint.y = initialPos.y; // Y is fixed
                closestPoint.z = obj.transform.position.z;
            }
            else if (lockedAxes == new Vector3(1, 0, 1)) // Locked X and Z axes (plane along XZ)
            {
                closestPoint.x = initialPos.x; // X is fixed
                closestPoint.y = obj.transform.position.y;
                closestPoint.z = initialPos.z; // Z is fixed
            }
            else if (lockedAxes == new Vector3(0, 1, 1)) // Locked Y and Z axes (plane along YZ)
            {
                closestPoint.x = obj.transform.position.x;
                closestPoint.y = initialPos.y; // Y is fixed
                closestPoint.z = initialPos.z; // Z is fixed
            }

            // Step 2: Apply grid snapping if enabled
            if (snappingOn)
            {
                if (lockedAxes.x == 0)
                {
                    float x = closestPoint.x - xFault;
                    float closestxValue = Mathf.Round(x / gridSpacing) * gridSpacing;
                    if (closestxValue > x) closestxValue -= gridSpacing;
                    closestPoint.x = closestxValue + xFault;
                }

                if (lockedAxes.y == 0)
                {
                    float y = closestPoint.y - yFault;
                    float closestyValue = Mathf.Round(y / gridSpacing) * gridSpacing;
                    if (closestyValue > y) closestyValue -= gridSpacing;
                    closestPoint.y = closestyValue + yFault;
                }

                if (lockedAxes.z == 0)
                {
                    float z = closestPoint.z - zFault;
                    float closestzValue = Mathf.Round(z / gridSpacing) * gridSpacing;
                    if (closestzValue > z) closestzValue -= gridSpacing;
                    closestPoint.z = closestzValue + zFault;
                }
            }

            // Final position is the closest point with or without snapping
            finalPosition = closestPoint;
        }
        else if (snappingOn)
        {
            // Default grid snapping when no locking is applied
            float x = pos.x - xFault;
            float y = pos.y - yFault;
            float z = pos.z - zFault;

            float closestxValue = Mathf.Round(x / gridSpacing) * gridSpacing;
            float closestyValue = Mathf.Round(y / gridSpacing) * gridSpacing;
            float closestzValue = Mathf.Round(z / gridSpacing) * gridSpacing;

            if (closestxValue > x) closestxValue -= gridSpacing;
            if (closestyValue > y) closestyValue -= gridSpacing;
            if (closestzValue > z) closestzValue -= gridSpacing;

            finalPosition = new Vector3(closestxValue + xFault, closestyValue + yFault, closestzValue + zFault);
        }

        return finalPosition;
    }

    // Searches and returns to the closest available grid position to the moved direction's furthest vertex to the object
    public static Vector3 FindMovementGridPosDiff(Vector3 pointPos, Vector3 delta)
    {
        float finalxDiff, finalyDiff, finalzDiff;
        float x, y, z;
        float xscale, yscale, zscale;

        // Position of the manipulator
        Vector3 manipulatorPos = pointPos - new Vector3(0, CubeHighlighter.staticMovementButtonOffset + (staticManipulationTool.transform.lossyScale.y / 2f), 0);

        // Handle axis locking
        Vector3 lockedAxes = LockAxis.lockedAxes;

        // Calculate the calibrated manipulator position
        if (delta.x > 0)
        {
            xscale = staticManipulationTool.transform.lossyScale.x / 2;
        }
        else
        {
            xscale = -staticManipulationTool.transform.lossyScale.x / 2;
        }
        x = manipulatorPos.x + xscale - staticxFault;

        yscale = -staticManipulationTool.transform.lossyScale.y / 2;
        y = manipulatorPos.y + yscale - staticyFault;

        if (delta.z > 0)
        {
            zscale = staticManipulationTool.transform.lossyScale.z / 2;
        }
        else
        {
            zscale = -staticManipulationTool.transform.lossyScale.z / 2;
        }
        z = manipulatorPos.z + zscale - staticzFault;

        // Initialize final differences with delta as default
        finalxDiff = delta.x;
        finalyDiff = delta.y;
        finalzDiff = delta.z;

        if (LockAxis.lockAxisOn)
        {
            // Adjust based on locked axes
            if (lockedAxes.x == 1)
            {
                finalxDiff = 0; // X-axis is locked, no movement
            }

            if (lockedAxes.y == 1)
            {
                finalyDiff = 0; // Y-axis is locked, no movement
            }

            if (lockedAxes.z == 1)
            {
                finalzDiff = 0; // Z-axis is locked, no movement
            }
        }

        if (snappingOn)
        {
            // Apply grid snapping if enabled
            if (lockedAxes.x == 0)
            {
                float closestxValue = Mathf.Round(x / staticGridSpacing) * staticGridSpacing;
                if (closestxValue > x) closestxValue -= staticGridSpacing;
                finalxDiff = closestxValue + staticxFault - xscale - manipulatorPos.x + delta.x + 0.0015f;
            }

            if (lockedAxes.y == 0)
            {
                float closestyValue = Mathf.Round(y / staticGridSpacing) * staticGridSpacing;
                if (closestyValue > y) closestyValue -= staticGridSpacing;
                finalyDiff = closestyValue + staticyFault - yscale - manipulatorPos.y + delta.y - 0.005f;
            }

            if (lockedAxes.z == 0)
            {
                float closestzValue = Mathf.Round(z / staticGridSpacing) * staticGridSpacing;
                if (closestzValue > z) closestzValue -= staticGridSpacing;
                finalzDiff = closestzValue + staticzFault - zscale - manipulatorPos.z + delta.z - 0.0015f;
            }
        }

        return new Vector3(finalxDiff, finalyDiff, finalzDiff);
    }

    public void UpdateManipulationToolWithInvoke()
    {
        /* We decided not to use reflection and instead use direct invocation for efficiency
        // Call the private function using Reflection, this function is in RectangularPrismCreator Script
        MethodInfo method = rectangularPrismCreatorReference.GetType().GetMethod("UpdateManipulatorLocation", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(rectangularPrismCreatorReference, null);
        }
        // Call the private function using Reflection, this function is in CubeHighlighter Script
        method = cubeHighlighterReference.GetType().GetMethod("UpdateManipulationCube", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(cubeHighlighterScriptReference, null);
        }
        */
        rectangularPrismCreatorReference.UpdateManipulatorLocation();
        cubeHighlighterReference.UpdateManipulationCube();
    }
}
