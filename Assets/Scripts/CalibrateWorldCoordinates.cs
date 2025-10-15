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

using Meta.XR.MRUtilityKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityEngine.UIElements;

// Manages the placement and calibration of a virtual grid workspace aligned with real-world surfaces
// (horizontal planes, vertical walls, or created objects) using Meta XR’s MR Utility Kit. It detects
// selected anchors (tables, floors, walls, etc.), positions a grid plane accordingly, adjusts its
// orientation and scale, and provides UI tools for grid size, color, and material toggling. It also
// updates snapping coordinates for precise object placement.

public class CalibrateWorldCoordinates : MonoBehaviour
{
    public Transform player, realCamera;
    public static MRUKAnchor horizontalWorkspaceAnchor = null, verticalWorkspaceAnchor = null;
    public static Vector3 wallFaceNormal = Vector3.zero;
    public static Quaternion gizmoPlaneRotation;
    public static GameObject gizmoPlaneSelectedObject;
    public static int gizmoPlaneSelectedObjectVertexIndex;

    public GameObject plane; // The grid workspace we work on top of

    public GameObject gizmoBallPrefab;

    private bool occludedGrid = false; // Dictates whether the plane will be occluded by world objects
    public Material occludedGridMat, shaderGridMat;

    public SnapToGrid snapToGridReference;

    public static bool selectedWorkspace = false, workingOnCreatedObject = false;

    // This one is about changing the grid size visuals
    private static readonly string CellSizeProperty = "_Cell_Size"; // Property name from Shader
    private int cellSizeIndex = 2;
    private int gridColorIndex = 0;
    public Color[] gridColors;
    public RawImage gridColorImage;
    public TextMeshProUGUI cellSizeText;

    public GameObject saveLoadMenu, gridSettingsMenu;

    // Initializes default grid settings, resets workspace state, and applies the initial grid color and cell size.
    private void Start()
    {
        selectedWorkspace = false;
        workingOnCreatedObject = false;
        horizontalWorkspaceAnchor = null;
        verticalWorkspaceAnchor = null;

        // The default size of the grid cells
        SetCellSize(new Vector2(0.02f, 0.02f));

        plane.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Grid_Color", gridColors[gridColorIndex]); // Use the correct property name
        gridColorImage.color = gridColors[gridColorIndex];
    }

    // Waits until a workspace (horizontal, vertical, or created object) is selected, then triggers drawing and positioning of the grid plane once.
    void Update()
    {
        // Loop until the current room is loaded and the transform of the first available table is referenced
        if (!selectedWorkspace) // This makes sure this only runs once
        {
            MRUKRoom room = MRUK.Instance.GetCurrentRoom();
            if (room != null)
            {
                /* That part was used for taking only the first table
                foreach (MRUKAnchor anchor in MRUK.Instance.GetCurrentRoom().Anchors)
                {
                    if (anchor.AnchorLabels[0] == "TABLE")
                    {
                        horizontalWorkspaceAnchor = anchor.transform;
                        Debug.Log("Found the table object. The table's name is: " + horizontalWorkspaceAnchor.gameObject.name);
                        horizontalWorkspaceAnchor.gameObject.GetComponentInChildren<MeshRenderer>().sharedMaterial.color = Color.white;
                        DrawPlane();
                        break;
                    }
                }
                */
                // These statements activates only when a workplace is selected via finger pinch in "SelectWorkspace" script
                if (horizontalWorkspaceAnchor != null) //Do it for the horizontal workspace
                {
                    DrawHorizontalPlane();
                    selectedWorkspace = true;
                    plane.SetActive(true);
                }
                else if (verticalWorkspaceAnchor != null) //Do it for the vertical workspace
                {
                    DrawVerticalPlane();
                    selectedWorkspace = true;
                    plane.SetActive(true);
                }
                else if (workingOnCreatedObject) //Do it for created objects
                {
                    DrawPlaneOnObject();
                    selectedWorkspace = true;
                    plane.SetActive(true);
                }
            }
        }

    }

    // Aligns and repositions the grid plane onto a user-created object’s surface.
    void DrawPlaneOnObject()
    {
        RotateWorldForObjectPlane();
        RepositionPlane(null, true); //Editor's note: this works very well
    }

    // Reorients the MRUK room transform relative to the selected object’s plane rotation for correct alignment.
    private void RotateWorldForObjectPlane()
    {
        plane.transform.rotation = Quaternion.identity;
        // Debug.Log("Rotation of the MRUK room previously: " + MRUK.Instance.GetCurrentRoom().transform.rotation.eulerAngles);
        // Debug.Log("Rotation of the plane: " + plane.transform.rotation.eulerAngles);
        // Debug.Log("Rotation of the original plane: " + gizmoPlaneRotation.eulerAngles);
        // Debug.Log("Rotation of the original object after: " + gizmoPlaneSelectedObject.transform.localRotation.eulerAngles);
        
        MRUK.Instance.GetCurrentRoom().transform.rotation = Quaternion.Inverse(gizmoPlaneRotation);

        // Debug.Log("Rotation of the MRUK room: " + MRUK.Instance.GetCurrentRoom().transform.rotation.eulerAngles);
    }

    // Identifies the corners of a horizontal anchor (e.g., table, floor, ceiling), calculates proper orientation/scale, and repositions the grid plane accordingly.
    void DrawHorizontalPlane()
    {
        BoxCollider boxCollider = horizontalWorkspaceAnchor.GetComponentInChildren<BoxCollider>();

        if (boxCollider != null)
        {
            // Get the center and size of the BoxCollider
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;

            // Calculate the 8 corners of the BoxCollider
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            corners[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;
            corners[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;
            corners[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;
            corners[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;
            corners[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;
            corners[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;
            corners[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;

            for (int i = 0; i < 8; i++)
            {
                //Instantiate(gizmoBallPrefab, corners[i], Quaternion.identity);
            }

            // Transform corners from local to world space
            for (int i = 0; i < corners.Length; i++)
            {
                if (horizontalWorkspaceAnchor.transform.gameObject.name == "FLOOR" || horizontalWorkspaceAnchor.transform.gameObject.name == "CEILING")
                {
                    corners[i] = horizontalWorkspaceAnchor.transform.GetChild(0).GetChild(0).TransformPoint(corners[i]);
                }
                else
                {
                    corners[i] = horizontalWorkspaceAnchor.transform.TransformPoint(corners[i]);
                }
            }

            // Used below
            Vector3[] sortedCorners, topCorners;
            if (horizontalWorkspaceAnchor.transform.gameObject.name == "FLOOR") // if thats floor, use the lower 4 corners instead
            {
                // Sort corners by their Y coordinate in ascending order (lowest first)
                sortedCorners = corners.OrderBy(c => c.y).ToArray();

                // Order the lowest 4 corners to form a plane
                topCorners = sortedCorners.Take(4).ToArray();
            }
            else
            {
                // Sort corners by their Y coordinate in world space
                sortedCorners = corners.OrderByDescending(c => c.y).ToArray();

                // Order the top 4 corners to form a plane
                topCorners = sortedCorners.Take(4).ToArray();
            }


            // Indicate the corners of the first locations of the objects
            foreach (Vector3 corner in topCorners)
            {
                // Instantiate(gizmoBallPrefab, corner, Quaternion.identity);
            }

            // Rotate the scene and resize the plane
            if(horizontalWorkspaceAnchor.transform.gameObject.name == "CEILING")
            {
                RotateWorldAndResizeHorizontalPlane(topCorners, 180);
            }
            else
            {
                RotateWorldAndResizeHorizontalPlane(topCorners, 0);
            }


            // Get the center and size of the BoxCollider
            center = boxCollider.center;
            size = boxCollider.size;

            // Calculate the 8 corners of the BoxCollider
            corners = new Vector3[8];
            corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            corners[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;
            corners[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;
            corners[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;
            corners[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;
            corners[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;
            corners[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;
            corners[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;

            // Transform corners from local to world space
            for (int i = 0; i < corners.Length; i++)
            {
                if (horizontalWorkspaceAnchor.transform.gameObject.name == "FLOOR" || horizontalWorkspaceAnchor.transform.gameObject.name == "CEILING")
                {
                    corners[i] = horizontalWorkspaceAnchor.transform.GetChild(0).GetChild(0).TransformPoint(corners[i]);
                }
                else
                {
                    corners[i] = horizontalWorkspaceAnchor.transform.TransformPoint(corners[i]);
                }
            }

            if (horizontalWorkspaceAnchor.transform.gameObject.name == "FLOOR" || horizontalWorkspaceAnchor.transform.gameObject.name == "CEILING") // if thats floor, use the lower 4 corners instead
            {
                // Sort corners by their Y coordinate in ascending order (lowest first)
                sortedCorners = corners.OrderBy(c => c.y).ToArray();

                // Order the lowest 4 corners to form a plane
                topCorners = sortedCorners.Take(4).ToArray();
            }
            else
            {
                // Sort corners by their Y coordinate in world space
                sortedCorners = corners.OrderByDescending(c => c.y).ToArray();

                // Order the top 4 corners to form a plane
                topCorners = sortedCorners.Take(4).ToArray();
            }

            // Reposition the plane
            RepositionPlane(topCorners, false);
        }
    }

    // Detects wall-like anchors, determines which side of the wall is usable, and aligns/resizes the grid plane to that vertical surface.
    void DrawVerticalPlane()
    {
        Transform verticalT;
        string anchorName = verticalWorkspaceAnchor.transform.gameObject.name;
        if (anchorName == "WALL_FACE" || anchorName == "INVISIBLE_WALL_FACE" || anchorName == "DOOR_FRAME" || anchorName == "WINDOW_FRAME" || anchorName == "SCREEN" || anchorName == "WALL_ART")
        {
            verticalT = verticalWorkspaceAnchor.transform.GetChild(0).GetChild(0);
        }
        else
        {
            verticalT = verticalWorkspaceAnchor.transform;
        }



        BoxCollider boxCollider = verticalT.GetComponentInChildren<BoxCollider>();

        if (boxCollider != null)
        {
            // Get the center and size of the BoxCollider
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;

            // Calculate the 8 corners of the BoxCollider
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            corners[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;
            corners[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;
            corners[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;
            corners[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;
            corners[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;
            corners[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;
            corners[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;

            // Transform corners from local to world space
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = verticalT.TransformPoint(corners[i]);
            }

            // Sort corners by their distance to the plane defined by the normal
            Vector3[] side1 = new Vector3[4]; //Side in normal direction
            Vector3[] side2 = new Vector3[4]; //Side in other direction
            int side1Index = 0;
            int side2Index = 0;

            foreach (Vector3 corner in corners)
            {
                if (Vector3.Dot(wallFaceNormal, corner - verticalT.position) > 0)
                {
                    side1[side1Index++] = corner;
                }
                else
                {
                    side2[side2Index++] = corner;
                }
            }

            // Calculate the center of each side
            Vector3 side1Center = Vector3.zero;
            foreach (Vector3 corner in side1)
            {
                side1Center += corner;
            }
            side1Center /= 4;

            Vector3 side2Center = Vector3.zero;
            foreach (Vector3 corner in side2)
            {
                side2Center += corner;
            }
            side2Center /= 4;

            // Determine which side is closer to the room center
            Vector3[] selectedSide = (side1Center.sqrMagnitude < side2Center.sqrMagnitude) ? side1 : side2;


            // Instantiate gizmos to visualize the corners of the selected side
            foreach (Vector3 corner in selectedSide)
            {
                //Instantiate(gizmoBallPrefab, corner, Quaternion.identity);
            }

            // Perform operations on the selected side corners
            // For example, repositioning the plane, etc.
            RotateWorldAndResizeVerticalPlane(selectedSide);

            // Get the center and size of the BoxCollider
            center = boxCollider.center;
            size = boxCollider.size;

            // Calculate the 8 corners of the BoxCollider
            corners = new Vector3[8];
            corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            corners[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;
            corners[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;
            corners[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;
            corners[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;
            corners[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;
            corners[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;
            corners[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;

            // Transform corners from local to world space
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = verticalT.TransformPoint(corners[i]);
            }


            wallFaceNormal.Normalize();
            Vector3 selectedSideCenter = boxCollider.center - (wallFaceNormal * boxCollider.size.y * 0.5f);

            // Create an array of Vector3 with a size of 4
            Vector3[] selectedCenterArray = new Vector3[4];

            // Assign the displaced value to each element in the array
            for (int i = 0; i < selectedCenterArray.Length; i++)
            {
                selectedCenterArray[i] = selectedSideCenter;
                selectedCenterArray[i] = verticalT.TransformPoint(selectedCenterArray[i]);
            }

            RepositionPlane(selectedCenterArray, false);
        }
    }

    // Adjusts the rotation and scale of the grid plane based on the wall’s normal and selected vertices.
    private void RotateWorldAndResizeVerticalPlane(Vector3[] vertices)
    {
        // Sort the selected side by the y-coordinate to find the highest 2 corners
        Vector3[] highestCorners = vertices.OrderByDescending(c => c.y).Take(2).ToArray();

        // Sort the selected side by the y-coordinate to find the lowest 2 corners
        Vector3[] lowestCorners = vertices.OrderBy(c => c.y).Take(2).ToArray();

        float angle = 0f;
        Vector3 horizontalVector = highestCorners[0] - highestCorners[1];
        float horizontalScale = Vector3.Distance(highestCorners[0], highestCorners[1]) / 10f;
        float verticalScale = (highestCorners[0].y - lowestCorners[0].y) / 10f;

        // Ensure the normal is a unit vector
        wallFaceNormal.Normalize();
        // Calculate the rotation needed to align the plane's up vector with the normal
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, wallFaceNormal);
        // Apply the rotation to the plane
        plane.transform.rotation = rotation;
        plane.transform.localScale = new Vector3(horizontalScale, 1, verticalScale);

        /*
        if (Mathf.Abs(horizontalVector.x) > Mathf.Abs(horizontalVector.z))
        {
            angle = Mathf.Atan(Mathf.Abs(horizontalVector.z / horizontalVector.x));
        }
        else
        {
            angle = Mathf.Atan(Mathf.Abs(horizontalVector.x / horizontalVector.z));
        }
        */

        if (horizontalVector.x >= 0)
        {
            angle = Mathf.Atan(horizontalVector.z / horizontalVector.x);
        }
        else 
        {
            angle = Mathf.Atan(horizontalVector.z / horizontalVector.x) + MathF.PI;
        }
        angle = Mathf.Rad2Deg * angle;


        wallFaceNormal.y = 0;
        wallFaceNormal.Normalize();
        float normalAngle = Vector3.SignedAngle(Vector3.forward, wallFaceNormal, Vector3.up);
        plane.transform.rotation = Quaternion.Euler(90, normalAngle + angle, 0);

        MRUK.Instance.GetCurrentRoom().transform.rotation = Quaternion.Euler(0, angle, 0);

        wallFaceNormal = plane.transform.up;
        wallFaceNormal.y = 0;
        // Debug.Log("Wall face normal: " + wallFaceNormal.ToString());
    }

    // Computes scaling and angle for horizontal workspaces (tables, floor, ceiling) and rotates the MRUK room for alignment.
    private void RotateWorldAndResizeHorizontalPlane(Vector3[] vertices, float planeAngleForCeiling)
    {
        float angle = 0f;
        float distance1 = Vector3.Distance(vertices[0], vertices[1])/10f;
        float distance2 = Vector3.Distance(vertices[0], vertices[2])/10f;
        float distance3 = Vector3.Distance(vertices[0], vertices[3])/10f;
        Vector3 vector1 = vertices[0] - vertices[1];
        Vector3 vector2 = vertices[0] - vertices[2];
        Vector3 vector3 = vertices[0] - vertices[3];

        // This section may include mathematical errors
        float maxNum = Mathf.Max(distance1, Mathf.Max(distance2, distance3));
        if (maxNum == distance1)
        {
            if (Mathf.Abs(vector2.x) > Mathf.Abs(vector2.z))
            {
                plane.transform.localScale = new Vector3(distance2, 1f, distance3);
                angle = Mathf.Atan(Mathf.Abs(vector2.z / vector2.x));
            }
            else
            {
                plane.transform.localScale = new Vector3(distance3, 1f, distance2);
                angle = Mathf.Atan(Mathf.Abs(vector2.x / vector2.z));
            }
            // Debug.Log("The first angle: " + angle);
            // Debug.Log("Vectors: " + vector2);
        }
        else if (maxNum == distance2)
        {
            if (Mathf.Abs(vector1.x) > Mathf.Abs(vector1.z))
            {
                plane.transform.localScale = new Vector3(distance1, 1f, distance3);
                angle = Mathf.Atan(Mathf.Abs(vector1.z / vector1.x));
            }
            else
            {
                plane.transform.localScale = new Vector3(distance3, 1f, distance1);
                angle = Mathf.Atan(Mathf.Abs(vector1.x / vector1.z));
            }
            // Debug.Log("The second angle: " + angle);
            // Debug.Log("Vectors: " + vector1);
        }
        else if (maxNum == distance3)
        {
            if (Mathf.Abs(vector1.x) > Mathf.Abs(vector1.z))
            {
                plane.transform.localScale = new Vector3(distance1, 1f, distance2);
                angle = Mathf.Atan(vector1.z / vector1.x);
            }
            else
            {
                plane.transform.localScale = new Vector3(distance2, 1f, distance1);
                angle = Mathf.Atan(- vector1.x / vector1.z);
            }
            // Debug.Log("The third angle: " + angle);
            // Debug.Log("Vectors: " + vector1);
        }

        angle = Mathf.Rad2Deg * angle;
        plane.transform.rotation = Quaternion.Euler(planeAngleForCeiling, 0, 0);
        MRUK.Instance.GetCurrentRoom().transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    // Moves the grid plane either to the average of the given vertices (anchors) or to a specific vertex on a created object.
    private void RepositionPlane(Vector3[] vertices, bool forCreatedObjects)
    {
        if (!forCreatedObjects)
        {
            // Calculate the average position of the four points
            Vector3 centerPosition = (vertices[0] + vertices[1] + vertices[2] + vertices[3]) / 4f;

            // Set the object's position to the calculated center position
            plane.transform.position = centerPosition;
        }
        else
        {
            MeshCollider mc = gizmoPlaneSelectedObject.GetComponent<MeshCollider>();
            plane.transform.position = mc.transform.TransformPoint(mc.sharedMesh.vertices[gizmoPlaneSelectedObjectVertexIndex]);
            // Debug.Log("Shared mesh vertex index = " + gizmoPlaneSelectedObjectVertexIndex);
            plane.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
        }

        /* We decided not to use reflection and instead use direct invocation for efficiency
        // Call the private function using Reflection, this function is in RectangularPrismCreator Script
        MethodInfo method = snapToGridReference.GetType().GetMethod("UpdateGridCoordinates", BindingFlags.Public | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(snapToGridReference, null);
        }
        */
        snapToGridReference.UpdateGridCoordinates();
    }

    // Toggles between occluded and normal grid materials, adjusting the grid’s position slightly to avoid z-fighting.
    public void ChangeGridMaterial()
    {
        if (!occludedGrid)
        {
            plane.GetComponent<MeshRenderer>().sharedMaterial = occludedGridMat;
            // Move the grid so that there is no z-fighting issue when it is onto the grid
            Vector3 movement = plane.transform.up * 0.02f;
            plane.transform.position += movement;
        }
        else
        {
            plane.GetComponent<MeshRenderer>().sharedMaterial = shaderGridMat;
            Vector3 movement = plane.transform.up * 0.02f;
            plane.transform.position -= movement;
        }
        plane.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Grid_Color", gridColors[gridColorIndex]);
        occludedGrid = !occludedGrid;
    }

    // Updates shader/material parameters to change the grid’s cell size for both occluded and normal modes.
    public void SetCellSize(Vector2 newCellSize)
    {
        shaderGridMat.SetVector(CellSizeProperty, new Vector4(newCellSize.x, newCellSize.y, 0, 0));
        occludedGridMat.SetVector(CellSizeProperty, new Vector4(newCellSize.x, newCellSize.y, 0, 0));
    }

    // Cycles through predefined grid cell sizes and updates the UI text.
    public void CellSizeButtonAction()
    {
        float[] sizes = { 0.05f, 0.02f, 0.01f, 0.005f };
        string[] names = { "5", "2", "1", "0.5" };

        SnapToGrid.gridSpacing = sizes[cellSizeIndex];
        SnapToGrid.staticGridSpacing = sizes[cellSizeIndex];
        SetCellSize(new Vector2(sizes[cellSizeIndex], sizes[cellSizeIndex]));
        cellSizeText.text = names[cellSizeIndex] + "cm";

        cellSizeIndex += 1;
        if (cellSizeIndex >= 4) { cellSizeIndex = 0; }
    }

    // Cycles through available grid colors, updating the plane material and preview UI.
    public void GridColorButtonAction()
    {
        gridColorIndex += 1;
        if (gridColorIndex >= 3) { gridColorIndex = 0; }
        plane.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Grid_Color", gridColors[gridColorIndex]); // Use the correct property name
        gridColorImage.color = gridColors[gridColorIndex];
    }

    // Switches between save/load menu and grid settings menu in the UI.
    public void GridSettingsButtonAction()
    {
        saveLoadMenu.SetActive(false);
        gridSettingsMenu.SetActive(true);
    }
    public void GoBackButtonAction()
    {
        gridSettingsMenu.SetActive(false);
        saveLoadMenu.SetActive(true);
    }

    // Quit app
    public void QuitApplication()
    {
        Application.Quit();
    }
}