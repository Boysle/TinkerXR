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
using System.Reflection;
using System.Runtime.Serialization;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

// The script that is used for manipulating the selected objects
// This is also the section that turns snapping on and off based on the input from "HandRotationDetector" script
// This script goes under the Game Manager
public class VertexScaler : MonoBehaviour
{
    public GameObject anchorObjectModel;
    public Transform[] rotationWheelModels;

    private MeshFilter meshFilter;
    private Vector3[] vertices;
    private Vector3[][] worldVertices; // First locations of every vertex of object
    private Vector3[] worldLocations; // First locations of every object
    private Vector3[] snappedMovementValue;
    public static float x0 = 1f, y0 = 1f, z0 = 1f, initialRotation = 0f, currentAxisScale = 1f;
    public float rotationAngleSnap = 15f;
    private float v0x = 1f, v0y = 1f, v0z = 1f, v1x, v1y, v1z;
    public static Vector3 movedPoint, centerPoint, axisScale;
    private bool firstTime;
    public static Vector3 initialHandlePos = Vector3.zero;
    public static int activeRotationAxis = 0, activeScalingAxis = 0;
    public static GameObject[][] edgeScalerWithAxis;

    public RectangularPrismCreator rectangularPrismCreatorReference;
    public CubeHighlighter cubeHighlighterReference;

    void Start()
    {
        firstTime = true;
    }

    private void Update()
    {
        // If the object manipulation tool is being moved
        if (Selection.selectedManipulationUI)
        {
            if (firstTime)
            {
                if (ObjectGrabDetector._isGrabbingMovementHandle)
                {
                    // Initialize the list of first locations all selected objects
                    worldLocations = new Vector3[Selection.selectedObjects.Count];
                    snappedMovementValue = new Vector3[Selection.selectedObjects.Count];
                }
                else
                {
                    // Initialize the list of first locations of all vertices of all selected objects
                    worldVertices = new Vector3[Selection.selectedObjects.Count][];
                }
            }
            int i = 0;
            foreach (ObjectMaterialPair pair in Selection.selectedObjects)
            {
                meshFilter = pair.transform.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError("MeshFilter component not found.");
                    return;
                }
                Mesh mesh = meshFilter.mesh;
                vertices = mesh.vertices;
                if (firstTime)
                {
                    if (ObjectGrabDetector._isGrabbingMovementHandle)
                    {
                        worldLocations[i] = pair.transform.position;
                    }
                    else
                    {
                        worldVertices[i] = new Vector3[vertices.Length];
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            worldVertices[i][j] = LocalPointToWorld(pair.transform, vertices[j]);
                        }
                    }
                }
                if (ObjectGrabDetector._isGrabbingMovementHandle)
                {
                    MoveAllVertices(pair.transform, i);
                }
                else if (ObjectGrabDetector._isGrabbingRotationWheel)
                {
                    RotateAllVertices(pair.transform, i, activeRotationAxis, rotationAngleSnap);
                    // ObjectGrabDetector._isGrabbingRotationWheel = false; // To make sure it runs once for testing
                }
                else if (ObjectGrabDetector._isGrabbingEdgeScaler)
                {
                    ScaleAllVertices(pair.transform, i, activeScalingAxis);
                    // Update the object manipulation tool after scaling all vertices
                }
                else if (!ObjectGrabDetector._isGrabbingRulerEdge) // The last case is using the CORNERSCALER BUTTONS
                {
                    ShiftAllVertices(pair.transform, i);
                }
                i++;
            }
            firstTime = false;
            UpdateManipulationToolWithInvoke();
        }
        else
        {
           firstTime = true;
        }
    }

    public Vector3 LocalPointToWorld(Transform pairTransform, Vector3 p) => pairTransform.transform.TransformPoint(p);

    void RotateAllVertices(Transform t, int index, int axis, float rotationAngleDegrees)
    {
        Vector3 pointRot = Selection.selectionManipulationUIObject.transform.rotation.eulerAngles; 
        float xnew, ynew, znew;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Calculate the vector from centerPoint to vertex location
            Vector3 vector = worldVertices[index][i] - centerPoint;
            // Convert the rotation angle from degrees to radians
            float rotationAngleRadians = rotationAngleDegrees * Mathf.Deg2Rad;

            xnew = vector.x; ynew = vector.y; znew = vector.z;


            if (axis == 0) // Rotate around X-axis (Red wheel)
            {
                // Removed for the risk of gimbal lock
                // rotationWheelModels[0].rotation = Quaternion.Euler (pointRot.x - initialRotation, rotationWheelModels[0].eulerAngles.y, rotationWheelModels[0].eulerAngles.z);
                // Calculate the rotation offset in the x-axis, preserve the existing y and z rotation, and combine rotations
                Quaternion rotationOffset = Quaternion.Euler(pointRot.x - initialRotation, 0, 0);
                Quaternion preservedRotation = Quaternion.Euler(0, rotationWheelModels[0].eulerAngles.y, rotationWheelModels[0].eulerAngles.z);
                rotationWheelModels[0].rotation = preservedRotation * rotationOffset;

                float rotationAngleRad = pointRot.x - initialRotation;
                if (SnapToGrid.snappingOn) 
                {
                    rotationAngleRad = Mathf.Round(rotationAngleRad / rotationAngleSnap) * rotationAngleSnap;
                }
                rotationAngleRad *= Mathf.Deg2Rad;
                ynew = vector.y * Mathf.Cos(rotationAngleRad) - vector.z * Mathf.Sin(rotationAngleRad);
                znew = vector.y * Mathf.Sin(rotationAngleRad) + vector.z * Mathf.Cos(rotationAngleRad);
            }
            else if (axis == 1) // Rotate around Z-axis (Green wheel)
            {
                // Removed for the risk of gimbal lock
                // rotationWheelModels[1].rotation = Quaternion.Euler(rotationWheelModels[1].eulerAngles.x, rotationWheelModels[1].eulerAngles.y, pointRot.z - initialRotation);
                // Calculate the rotation offset in the z-axis, preserve the existing x and y rotation, and combine rotations
                Quaternion rotationOffset = Quaternion.Euler(0, 0, pointRot.z - initialRotation);
                Quaternion preservedRotation = Quaternion.Euler(rotationWheelModels[1].eulerAngles.x, rotationWheelModels[1].eulerAngles.y, 0);
                rotationWheelModels[1].rotation = preservedRotation * rotationOffset;

                float rotationAngleRad = pointRot.z - initialRotation;
                if (SnapToGrid.snappingOn) 
                {
                    rotationAngleRad = Mathf.Round(rotationAngleRad / rotationAngleSnap) * rotationAngleSnap;
                }
                rotationAngleRad *= Mathf.Deg2Rad;
                xnew = vector.x * Mathf.Cos(rotationAngleRad) - vector.y * Mathf.Sin(rotationAngleRad);
                ynew = vector.x * Mathf.Sin(rotationAngleRad) + vector.y * Mathf.Cos(rotationAngleRad);
            }
            else if (axis == 2) // Rotate around Y-axis (Blue wheel)
            {
                // Removed for the risk of gimbal lock
                // rotationWheelModels[2].rotation = Quaternion.Euler(rotationWheelModels[2].eulerAngles.x, pointRot.y - initialRotation, rotationWheelModels[2].eulerAngles.z);
                // Calculate the rotation offset in the y-axis, preserve the existing x and z rotation, and combine rotations
                Quaternion rotationOffset = Quaternion.Euler(0, pointRot.y - initialRotation, 0);
                Quaternion preservedRotation = Quaternion.Euler(rotationWheelModels[2].eulerAngles.x, 0, rotationWheelModels[2].eulerAngles.z);
                rotationWheelModels[2].rotation = preservedRotation * rotationOffset;

                float rotationAngleRad = pointRot.y - initialRotation;
                if (SnapToGrid.snappingOn) 
                {
                    rotationAngleRad = Mathf.Round(rotationAngleRad / rotationAngleSnap) * rotationAngleSnap;
                }
                rotationAngleRad *= Mathf.Deg2Rad;
                znew = vector.z * Mathf.Cos(rotationAngleRad) - vector.x * Mathf.Sin(rotationAngleRad);
                xnew = vector.z * Mathf.Sin(rotationAngleRad) + vector.x * Mathf.Cos(rotationAngleRad);
            }
            // The new vertex location after rotation
            Vector3 rotatedVector = new Vector3(xnew, ynew, znew);
            Vector3 newVertexLocation = centerPoint + rotatedVector;
            vertices[i] = t.transform.InverseTransformPoint(newVertexLocation);
        }
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
        UpdateCollider(t); // This may cause performance issues, maybe we need to use invoke
    }
    
    void ScaleAllVertices (Transform t, int index, int axis)
    {
        float xnew, ynew, znew;
        for (int i = 0; i < vertices.Length; i++)
        {
            xnew = worldVertices[index][i].x; ynew = worldVertices[index][i].y; znew = worldVertices[index][i].z;
            if (axis == 0)
            {
                xnew = ((xnew - centerPoint.x) * currentAxisScale / axisScale.x) + centerPoint.x;
            }
            else if (axis == 1)
            {
                ynew = ((ynew - centerPoint.y) * currentAxisScale / axisScale.y) + centerPoint.y;
            }
            else if (axis == 2)
            {
                znew = ((znew - centerPoint.z) * currentAxisScale / axisScale.z) + centerPoint.z;
            }
            Vector3 scaledVector = new Vector3(xnew, ynew, znew);
            vertices[i] = t.transform.InverseTransformPoint(scaledVector);
        }
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
        UpdateCollider(t); // This may cause performance issues, maybe we need to use invoke
    }

    // Function used with scaling corner buttons (for resizing the object) CORNER SCALING
    void ShiftAllVertices(Transform t, int index) 
    {
        // Going through all the vertices of one selected object
        Vector3 pointPos;
        if (!SnapToGrid.snappingOn && !UniformScaling.uniformScalingOn && !LockAxis.lockAxisOn) // If there is no snapping, uniform scaling, or locked axis; make the model follow its collider
        {
            pointPos = Selection.selectionManipulationUIObject.transform.position;
            anchorObjectModel.transform.position = pointPos; // Moving the anchor object while having found the transform position
        }
        else // !!! If there is snapping or uniform scaling or locked axis, make the model follow its renderer, which's position is adjusted in snap to grid script !!!
        {
            pointPos = anchorObjectModel.transform.position;
        }
        float xnew, ynew, znew;
        for (int i = 0; i < vertices.Length; i++)
        {
            v1x = movedPoint.x - worldVertices[index][i].x;
            v1y = movedPoint.y - worldVertices[index][i].y;
            v1z = movedPoint.z - worldVertices[index][i].z;
            v0x = pointPos.x - movedPoint.x;
            v0y = pointPos.y - movedPoint.y;
            v0z = pointPos.z - movedPoint.z;
            xnew = v0x * (x0 - Math.Abs(v1x)) / x0;
            // Debug.Log("movedPointx = " + movedPoint.x + " worldVertex = " + worldVertices[index] + " v1x = " + v1x + " x0 = " + x0 + " v0x = " + v0x);
            ynew = v0y * (y0 - Math.Abs(v1y)) / y0;
            znew = v0z * (z0 - Math.Abs(v1z)) / z0;
            Vector3 delta = new Vector3(xnew, ynew, znew);
            vertices[i] = worldVertices[index][i] + delta;
            vertices[i] = t.transform.InverseTransformPoint(vertices[i]);
        }
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
        UpdateCollider(t); // This may cause performance issues, maybe we need to use invoke
    }

    void UpdateCollider(Transform t)
    {
        MeshCollider collider = t.GetComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.sharedMesh = meshFilter.mesh;

        // These do not belong here but added for test
        meshFilter.mesh.RecalculateTangents();
        meshFilter.mesh.RecalculateUVDistributionMetrics();
    }

    // Used for moving the objects around with the movement handle, moves the objects themselves
    void MoveAllVertices(Transform t, int index) // Function used with movement handle
    {
        Vector3 pointPos = Selection.selectionManipulationUIObject.transform.position;
        Vector3 delta;
        delta = pointPos - initialHandlePos;
        if (SnapToGrid.snappingOn || LockAxis.lockAxisOn)
        {
            Vector3 snapValue = SnapToGrid.FindMovementGridPosDiff(pointPos, delta);
            if(snapValue != snappedMovementValue[index])
            {
                snappedMovementValue[index] = snapValue;
                t.position = worldLocations[index] + snapValue;
            }
        }
        else
        {
            t.position = worldLocations[index] + delta;
            snappedMovementValue[index] = new Vector3(0, 0, 0);
        }
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
        method = cubeHighlighterScriptReference.GetType().GetMethod("UpdateManipulationCube", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(cubeHighlighterReference, null);
        }
        */
        rectangularPrismCreatorReference.UpdateManipulatorLocation();
        cubeHighlighterReference.UpdateManipulationCube();
    }
}