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
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

// Enables uniform scaling of 3D objects by constraining corner manipulator movement along a line

public class UniformScaling : MonoBehaviour
{
    public MonoBehaviour rectangularPrismCreatorReference;
    public MonoBehaviour cubeHighlighterScriptReference;

    public static bool uniformScalingOn = false;
    private bool currentUniform = false, holdingObject = false;

    public GameObject anchorObject, manipulationTool, cornerButtonParent;
    public LineRenderer lineRenderer;
    public float outsideLineLength;

    private GameObject obj;
    private Vector3 initialCenter, initialEnd;
    private Renderer manipulatorRenderer;
    private int iterCounter, firstFirstFirst;

    public static Vector3 expectedCornerPosition;

    void Start()
    {
        manipulatorRenderer = manipulationTool.GetComponent<Renderer>();
        lineRenderer.enabled = false;
        iterCounter = 0;
        firstFirstFirst = 0;
        currentUniform = false;
        holdingObject = false;
    }

    void Update()
    {
        if (uniformScalingOn && ObjectGrabDetector._isGrabbingCornerScaler && !LockAxis.lockAxisOn)
        {
            obj = Selection.selectionManipulationUIObject;
            // Run once when uniform scaling is set to on
            if ((!currentUniform || !holdingObject || (firstFirstFirst < 2)))
            {
                if (firstFirstFirst == 1)
                {
                    lineRenderer.enabled = true;
                }
                
                initialCenter = manipulationTool.transform.position;

                // Debug.LogError("Corner Object Position = " + obj.transform.position);

                initialEnd = obj.transform.position;
                firstFirstFirst++;
            }
            currentUniform = true;
            holdingObject = true;

            if (firstFirstFirst > 0)
            {
                // Draw the line from the middle of the object
                float manLength = (Mathf.Sqrt(Mathf.Pow(manipulatorRenderer.bounds.size.x, 2) + Mathf.Pow(manipulatorRenderer.bounds.size.y, 2) + Mathf.Pow(manipulatorRenderer.bounds.size.z, 2)) / 2);
                float lineDistance = outsideLineLength + manLength;
                Vector3 endPoint = manipulationTool.transform.position + (initialEnd - initialCenter).normalized * lineDistance;
                // Debug.Log("EndPoint" + endPoint + "StartPoint" + manipulationTool.transform.position);
                DrawLine(manipulationTool.transform.position, endPoint);
                // Debug.LogError("Hippo manippo center is = " + manipulationTool.transform.position);

                Vector3 bestUniformPos = FindCornerUniformPos(obj.transform.position);
                // Getting the mesh renderer child object of the corner scaler and the anchor object
                obj.transform.GetChild(0).position = bestUniformPos;
                anchorObject.transform.position = bestUniformPos;
                UpdateManipulationToolWithInvoke();
            }
        }

        // If we stopped grabbing the corner button, run once
        if (holdingObject && !ObjectGrabDetector._isGrabbingCornerScaler)
        {
            lineRenderer.enabled = false;
            obj.transform.position = anchorObject.transform.position;
            iterCounter++;
            // In case the position of the corner button is overwritten, loop through this with update
            if (iterCounter > 5)
            {
                if (obj.transform.position == anchorObject.transform.position)
                {
                    firstFirstFirst = 0;
                    holdingObject = false;
                    UpdateManipulationToolWithInvoke();
                }
                iterCounter = 0;
            }
        }

        // Update the manipulation cube once as we turn uniform scaling off while holding the scaler
        if (!uniformScalingOn && currentUniform)
        {
            lineRenderer.enabled = false;
            currentUniform = false;
            firstFirstFirst = 0;
            if (ObjectGrabDetector._isGrabbingCornerScaler)
            {
                UpdateManipulationToolWithInvoke();
            }
        }
    }

    // Finds the closest point of the scaler collider to the line
    private Vector3 FindCornerUniformPos(Vector3 pos)
    {
        // Line direction vector (from pointA to pointB)
        Vector3 lineDirection = initialEnd - initialCenter;

        // Vector from pointA to the object's position
        Vector3 toObject = obj.transform.position - initialCenter;

        // Projection of toObject onto the line direction
        float t = Vector3.Dot(toObject, lineDirection) / lineDirection.sqrMagnitude;

        // Calculate the closest point on the line
        Vector3 closestPoint = initialCenter + t * lineDirection;

        return closestPoint;
    }

    void DrawLine(Vector3 start, Vector3 end)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void UpdateManipulationToolWithInvoke()
    {
        // Call the private function using Reflection, this function is in RectangularPrismCreator Script
        MethodInfo method = rectangularPrismCreatorReference.GetType().GetMethod("UpdateManipulatorLocation", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(rectangularPrismCreatorReference, null);
        }
        // Call the private function using Reflection, this function is in CubeHighlighter Script
        method = cubeHighlighterScriptReference.GetType().GetMethod("UpdateManipulationCube", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(cubeHighlighterScriptReference, null);
        }
    }
}
