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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.UIElements;

// This script goes under Game Manager, it is used for creating/moving the white edges and other movement handles
// for the object manipulation tool/box/cube that wraps the selected objects around according to the coordinates
public class CubeHighlighter : MonoBehaviour
{
    public GameObject manipulationBox; // Object manipluation tool/box
    public GameObject cornerButton;
    public GameObject edgeButton;
    public GameObject faceButton;
    public GameObject movementButton;
    public GameObject[] rotationWheels;
    public GameObject workspacePlane;
    public Transform anchorTransform;
    public Material dashedLineMat;
    private Mesh manipulationBoxMesh;
    private Vector3[] vertices, worldVertices;
    private Quaternion[] rotationWheelDefaultAngles;
    private Transform manipulationBoxTransform;
    public GameObject objectManipulationParent, cornerButtonParent, edgeButtonParent;
    private GameObject[] cornerButtonArray;
    private GameObject[] edgeButtonArray;
    private GameObject[] faceButtonArray;
    private LineRenderer[] edgeLines, projectionLines, dashedProjectionLines; // Dashed ones are the vertical ones (on the case of horizontal plane)
    public Color manipulationEdgeLineColor;
    public float manipulationEdgeLineThickness;
    public float dashedLineGapSize;
    public float edgeResizerOffset;
    public float movementButtonOffset;
    public float additionalMovementButtonPrinterOffset = 0.010f;
    public static float additionalPrinterOffset; // For movement button
    public static float staticMovementButtonOffset;
    public float rotationWheelOffset;
    private int[][] faceIndices;
    public TextMeshPro debugText;
    public static Vector3 expectedHandlePos = Vector3.zero;



    public class PosRotValues
    {
        public float posx; public float posy; public float posz;
        public float rotx; public float roty; public float rotz;

        public PosRotValues(float xPosition, float yPosition, float zPosition, float xRotation, float yRotation, float zRotation)
        {
            posx = xPosition; posy = yPosition; posz = zPosition;
            rotx = xRotation; roty = yRotation; rotz = zRotation;
        }
    }
    private PosRotValues[] posRotValues;


    void Awake(){
        additionalPrinterOffset = additionalMovementButtonPrinterOffset;
        staticMovementButtonOffset = movementButtonOffset;

        // Get the Mesh component of the object manipulation box
        manipulationBoxMesh = manipulationBox.GetComponent<MeshFilter>().mesh;

        // Get the vertices (corners) of the object manipulation box
        vertices = manipulationBoxMesh.vertices;
        manipulationBoxTransform = manipulationBox.transform;

        // Get the world positions of the vertices of the object manipulation box
        worldVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++){
            worldVertices[i] = manipulationBoxTransform.TransformPoint(vertices[i]);
        }

        // Create a HashSet to store unique world space vertices
        HashSet<Vector3> uniqueVertices = new HashSet<Vector3>(worldVertices);

        // Get corner button objects (resize balls) at each unique world space vertex position
        cornerButtonArray = new GameObject[8];
        int j = 0;
        foreach (Vector3 uniqueVertex in uniqueVertices){
            cornerButtonArray[j] = cornerButtonParent.transform.GetChild(j).gameObject;
            cornerButtonArray[j].gameObject.SetActive(true);
            j++;       
        }

        // Create LineRenderers for the edges of the object manupilation box
        edgeLines = new LineRenderer[12];
        for (int i = 0; i < edgeLines.Length; i++)
        {
            GameObject lineObj = new GameObject("EdgeLine" + i);
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = manipulationEdgeLineColor; // Selecting this in editor
            lineRenderer.endColor = manipulationEdgeLineColor;
            lineRenderer.startWidth = manipulationEdgeLineThickness; // Selecting this in editor
            lineRenderer.endWidth = manipulationEdgeLineThickness;
            lineRenderer.gameObject.layer = LayerMask.NameToLayer("UI");
            lineRenderer.transform.SetParent(objectManipulationParent.transform);
            edgeLines[i] = lineRenderer;
        }

        // Create projection lines as a shadow reflecting on the workspace plane
        projectionLines = new LineRenderer[4];
        for (int i = 0; i < projectionLines.Length; i++)
        {
            GameObject lineObj = new GameObject("ProjectionLine" + i);
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // This can be changed to a dashed line
            lineRenderer.startColor = manipulationEdgeLineColor; // Selecting this in editor
            lineRenderer.endColor = manipulationEdgeLineColor;
            lineRenderer.startWidth = manipulationEdgeLineThickness/2f; // Selecting this in editor
            lineRenderer.endWidth = manipulationEdgeLineThickness/2f;
            lineRenderer.gameObject.layer = LayerMask.NameToLayer("UI");
            lineRenderer.transform.SetParent(objectManipulationParent.transform); // This makes sure it hides when omp is hidden
            projectionLines[i] = lineRenderer;
        }

        // Create dashed projection lines going down to the workspace desk
        dashedProjectionLines = new LineRenderer[4];
        for (int i = 0; i < dashedProjectionLines.Length; i++)
        {
            GameObject dashedLineObj = new GameObject("DashedLine" + i);
            LineRenderer lineRenderer = dashedLineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.material = dashedLineMat;
            lineRenderer.startColor = manipulationEdgeLineColor; // Selecting this in editor
            lineRenderer.endColor = manipulationEdgeLineColor;
            lineRenderer.startWidth = manipulationEdgeLineThickness/2f; // Selecting this in editor
            lineRenderer.endWidth = manipulationEdgeLineThickness/2f;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.material.mainTextureScale = new Vector2(dashedLineGapSize, 1); // Scale dashes proportionally
            lineRenderer.gameObject.layer = LayerMask.NameToLayer("UI");
            lineRenderer.transform.SetParent(objectManipulationParent.transform); // This makes sure it hides when omp is hidden
            dashedProjectionLines[i] = lineRenderer;
        }

        // Instantiate edge button objects (numerical resize cubes) at each unique world space vertex position
        edgeButtonArray = new GameObject[12];
        for (int k = 0; k < edgeButtonArray.Length; k++)
        {
            edgeButtonArray[k] = edgeButtonParent.transform.GetChild(k).gameObject;
            edgeButtonArray[k].gameObject.SetActive(true);
        }

        // Define the indices of the faces of the object manupilation box
        faceIndices = new int[][]{
        new int[]{0, 1, 3, 2}, // Front face
        new int[]{4, 5, 6, 7}, // Back face
        new int[]{0, 1, 7, 6}, // Bottom face
        new int[]{1, 3, 7, 5}, // Left face
        new int[]{3, 2, 4, 5}, // Top face
        new int[]{2, 0, 4, 6}  // Right face
        };

        // Define offset positions and rotations of the resize cones
        float of = edgeResizerOffset;
        posRotValues = new PosRotValues[6];
        posRotValues[0] = new PosRotValues(0f, 0f, -of, -90f, 0f, 0f); // Front face -- Swapped
        posRotValues[1] = new PosRotValues(0f, 0f, of, 90f, 0f, 0f); // Back face
        posRotValues[2] = new PosRotValues(0f, -of, 0f, 180f, 0f, 0f); // Bottom face
        posRotValues[3] = new PosRotValues(of, 0f, 0f, 0f, 0f, -90f); // Left face -- Swapped
        posRotValues[4] = new PosRotValues(0f, of, 0f, 0f, 0f, 0f); // Top face
        posRotValues[5] = new PosRotValues(-of, 0f, 0f, 0f, 0f, 90f); // Right face

        // Instantiate face button objects (locked axis movement/resizing cones) at each unique world space vertex position
        faceButtonArray = new GameObject[6];
        for (int i = 0; i < faceButtonArray.Length; i++)
        {
            GameObject newObj = Instantiate(faceButton, default, Quaternion.identity);
            faceButtonArray[i] = newObj;
            // Set the rotation of the face button
            Vector3 rot = FacePointRotationCalculator(i);
            faceButtonArray[i].transform.localEulerAngles = rot;
            newObj.transform.SetParent(objectManipulationParent.transform);
        }

        // Set the default angles for the rotation wheels
        rotationWheelDefaultAngles = new Quaternion[3];
        rotationWheelDefaultAngles[0] = rotationWheels[3].transform.rotation;
        rotationWheelDefaultAngles[1] = rotationWheels[4].transform.rotation;
        rotationWheelDefaultAngles[2] = rotationWheels[5].transform.rotation;

        // Update the cube once at awake
        UpdateManipulationCube();
    }

    public void Update(){
        // Updating the manipulation box when we have selected any of the manipulation handles (we call it UI)
        if (Selection.selectedManipulationUI)
        {
            UpdateManipulationCube();
        }
        // debugText.text = rotationWheels[0].transform.rotation.eulerAngles.ToString();
    }

    // Update function here, updating the size of the manipulation tool to wrap around the objects inside
    public void UpdateManipulationCube(){
        // If the object manipulation prism has been moved or rescaled by a selection change
        // Get the vertices (corners) of the manipulation box in world space
        manipulationBoxTransform = manipulationBox.transform; // Transform of the manipulation box object
        manipulationBoxMesh = manipulationBox.GetComponent<MeshFilter>().mesh;
        vertices = manipulationBoxMesh.vertices;
        worldVertices = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            worldVertices[i] = manipulationBoxTransform.TransformPoint(vertices[i]);
        }

        // Create a HashSet to store unique world space vertices
        HashSet<Vector3> uniqueVertices = new HashSet<Vector3>(worldVertices);

        // Instantiate objects at each unique world space vertex position
        int j = 0;
        foreach (Vector3 uniqueVertex in uniqueVertices){
            // This if statement makes sure the selected corner button can move freely and the other buttons can follow along after it
            if (!(Selection.selectedManipulationUI && cornerButtonArray[j].gameObject == Selection.selectionManipulationUIObject))
            {
                cornerButtonArray[j].transform.position = uniqueVertex;
            }
            j++;
        }

        UpdateEdgeLines();

        int k = 0;
        foreach (LineRenderer line in edgeLines)
        {
            edgeButtonArray[k].transform.position = (line.GetPosition(0) + line.GetPosition(1)) / 2f;
            k++;
        }

        UpdateAllProjectionLines();

        UpdateFacePoints();
    }

    private Vector3[] cornerPositions;
    // Define the indices of the edges
    private static readonly int[][] edgeIndices = new int[][]
    {
        new int[]{0, 1}, new int[]{1, 3}, new int[]{3, 2}, new int[]{2, 0},
        new int[]{0, 6}, new int[]{1, 7}, new int[]{3, 5}, new int[]{2, 4},
        new int[]{4, 6}, new int[]{4, 5}, new int[]{5, 7}, new int[]{6, 7}
    };

    private void UpdateEdgeLines()
    {
        // Get the positions of the corner buttons
        cornerPositions = new Vector3[cornerButtonArray.Length];
        for (int i = 0; i < cornerButtonArray.Length; i++)
        {
            // If the position we are interested in is the renderer of the corner button while snapping to grid or uniform scaling
            if ((SnapToGrid.snappingOn || UniformScaling.uniformScalingOn || LockAxis.lockAxisOn) && cornerButtonArray[i] == Selection.selectionManipulationUIObject)
            {
                cornerPositions[i] = anchorTransform.position;
            }
            else
            {
                cornerPositions[i] = cornerButtonArray[i].transform.position;
            }
        }

        // Update line positions for each edge
        for (int i = 0; i < edgeLines.Length; i++)
        {
            edgeLines[i].SetPosition(0, cornerPositions[edgeIndices[i][0]]);
            edgeLines[i].SetPosition(1, cornerPositions[edgeIndices[i][1]]);
        }
    }

    // Dictionary to store the correspondence between the original indices and their positions in the projections array
    Dictionary<int, int> indexMapping = new Dictionary<int, int>();

    private void UpdateAllProjectionLines()
    {
        // Get the plane's normal and a point on the plane
        UnityEngine.Plane plane = new UnityEngine.Plane(workspacePlane.transform.up, workspacePlane.transform.position);

        // Create an array to store the corner positions and their distances to the plane
        var cornerDistances = cornerPositions.Select(position => new
        {
            Position = position,
            Distance = plane.GetDistanceToPoint(position)
        }).ToArray();

        // Sort the corners by distance
        var sortedCorners = cornerDistances.OrderBy(cd => Mathf.Abs(cd.Distance)).ToArray();

        // Select the closest 4 points
        var closestPoints = sortedCorners.Take(4).ToArray();

        // Get indices of the closest points in the original cornerPositions array
        var closestPointIndices = closestPoints.Select(cp => System.Array.IndexOf(cornerPositions, cp.Position)).ToArray();

        // Calculate the projections of these points on the plane
        Vector3[] projections = new Vector3[4];
        for (int i = 0; i < closestPoints.Length; i++)
        {
            projections[i] = ProjectPointOnPlane(plane, closestPoints[i].Position);
            // Debug.Log($"Point {i + 1}: {closestPoints[i].Position}, Projection: {projections[i]}");

            // Add the mapping to the dictionary
            indexMapping[closestPointIndices[i]] = i;
        }

        // Find the correct order of points to form a square for the projections
        OrderPointsAndDraw(projections, closestPointIndices);
    }


    Vector3 ProjectPointOnPlane(UnityEngine.Plane plane, Vector3 point)
    {
        float distance = plane.GetDistanceToPoint(point);
        return point - distance * plane.normal;
    }

    // Function for finding the correct order of points to form a square for the projections
    void OrderPointsAndDraw(Vector3[] points, int[] pointIndices)
    {
        int lineIndex = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                foreach (var edge in edgeIndices)
                {
                    // Check if the pair matches (a, b) or (b, a) since the edges are undirected
                    if (edge[0] == pointIndices[i] && edge[1] == pointIndices[j])
                    {
                        // Each LineRenderer will have 2 points set in start
                        projectionLines[lineIndex].SetPosition(0, points[indexMapping[pointIndices[i]]]);
                        projectionLines[lineIndex].SetPosition(1, points[indexMapping[pointIndices[j]]]); // Connect to the next point, wrapping around to the first point
                        lineIndex ++;
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            Vector3 startPoint = points[indexMapping[pointIndices[i]]];
            Vector3 endPoint = cornerPositions[pointIndices[i]];
            dashedProjectionLines[i].SetPosition(0, startPoint);
            dashedProjectionLines[i].SetPosition(1, endPoint);
        }
    }

    private void UpdateFacePoints()
    {
        // Calculate midpoints for each face based on corner positions and put them in place
        for (int i = 0; i < faceButtonArray.Length; i++)
        {
            Vector3 faceCenter = Vector3.zero;
            
            // Calculate the average position of the four corners of the face
            for (int j = 0; j < faceIndices[i].Length; j++)
            {
                faceCenter += cornerPositions[faceIndices[i][j]];
            }

            faceCenter /= 4f; // Divide by 4 to get the average

            // Set the position of the face button at the face center and offset
            Vector3 pos = FacePointPositionCalculator(faceCenter, i);
            faceButtonArray[i].transform.position = pos;
            float additionalOffset = 0f;
            if (Selection.selectedObjects.Count > 0 && Selection.selectedObjects[0].transform.name == "printer")
            {
                additionalOffset = additionalPrinterOffset;
            }
            if (i == 4) 
            {
                expectedHandlePos = new Vector3(pos.x, pos.y + movementButtonOffset + additionalOffset, pos.z);
                if (!ObjectGrabDetector._isGrabbingMovementHandle) // If we are grabbing the movement handle, skip it
                {
                    // Debug.LogWarning("Moving the movement object into its expected location." + Selection.selectionManipulationUIObject);
                    movementButton.transform.position = new Vector3(pos.x, pos.y + movementButtonOffset + additionalOffset, pos.z);
                }
            }

            // When the rotation wheels are not grabbed, set their roation and position to default.
            if (!ObjectGrabDetector._isGrabbingRotationWheel)
            {
                Bounds bounds = manipulationBox.transform.GetComponent<MeshRenderer>().bounds;
                Vector3 boxPos = manipulationBox.transform.position;
                // The posX, posY, and posZ floats are used for selecting whichever way is farther away to the manipulation box center
                if (i == 3) // Red wheel
                {
                    float posX = 0;
                    Vector3 rightSidePosition = boxPos + manipulationBox.transform.right * (bounds.size.x / 2);
                    posX = rightSidePosition.x + 2 * rotationWheelOffset;

                    rotationWheels[0].transform.position = new Vector3(posX, pos.y, pos.z);
                    rotationWheels[3].transform.position = new Vector3(posX, pos.y, pos.z);
                }
                if (i == 0) // Green wheel
                {
                    float posZ = 0;
                    Vector3 forwardSidePosition = boxPos - manipulationBox.transform.forward * (bounds.size.z / 2);
                    posZ = forwardSidePosition.z - 2 * rotationWheelOffset;

                    rotationWheels[1].transform.position = new Vector3(pos.x, pos.y, posZ);
                    rotationWheels[4].transform.position = new Vector3(pos.x, pos.y, posZ);
                }
                if (i == 4) // Blue wheel
                {
                    float firstValue = Mathf.Abs(boxPos.y - pos.y + rotationWheelOffset);
                    float secondValue = Mathf.Abs(boxPos.y - pos.y - rotationWheelOffset);
                    float posY;
                    if (firstValue > secondValue) { posY = pos.y - rotationWheelOffset; } else { posY = pos.y + rotationWheelOffset; }
                    rotationWheels[2].transform.position = new Vector3(pos.x, posY, pos.z);
                    rotationWheels[5].transform.position = new Vector3(pos.x, posY, pos.z);
                }
            }
        }
        // Resetting the rotations of the rotation wheels
        if (!ObjectGrabDetector._isGrabbingRotationWheel)
        {
            rotationWheels[0].transform.rotation = rotationWheelDefaultAngles[0];
            rotationWheels[1].transform.rotation = rotationWheelDefaultAngles[1];
            rotationWheels[2].transform.rotation = rotationWheelDefaultAngles[2];
            rotationWheels[3].transform.rotation = rotationWheelDefaultAngles[0];
            rotationWheels[4].transform.rotation = rotationWheelDefaultAngles[1];
            rotationWheels[5].transform.rotation = rotationWheelDefaultAngles[2];
        }
    }

    private Vector3 FacePointPositionCalculator(Vector3 location, int index)
    {
        Vector3 newLocation = new Vector3(location.x + posRotValues[index].posx, location.y + posRotValues[index].posy, location.z + posRotValues[index].posz);
        return newLocation;
    }

    private Vector3 FacePointRotationCalculator(int index)
    {
        Vector3 newRotation = new Vector3(posRotValues[index].rotx, posRotValues[index].roty, posRotValues[index].rotz);
        return newRotation;
    }

    public void Debugger()
    {
        // Debug.LogWarning("POKING");
    }
}