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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using UnityEngine.UIElements;

// Handles workspace selection in VR using raycasting, gizmos, and room anchors.
public class SelectWorkspace : MonoBehaviour
{
    // We apply a ray from the right hand
    public Transform rayStartPoint;
    public float rayLength = 5f;
    public Color rayColor = Color.red;  // Color of the ray
    private LineRenderer lineRenderer;

    public MRUKAnchor.SceneLabels labelFilter;
    public TMPro.TextMeshPro environmentDebugText;

    public FingerPinchValue indexFingerPinchValue;
    public GameObject gizmoPhysicsBall, gizmoBall, gizmoPlane, workspacePlane;
    private GameObject[] gizmoBalls;

    private bool rayAvailable = true;

    public Selection selectionReference;

    // Start is called before the first frame update
    void Start()
    {
        // Get the LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        // Set the number of points in the LineRenderer
        lineRenderer.positionCount = 2;

        // Shows the label of the room object
        environmentDebugText.gameObject.SetActive(true);

        rayAvailable = true;

        //InvokeRepeating("InstantiatePrefab", 0f, 1f);

        /*
        gizmoBalls = new GameObject[5];
        for (int i = 0; i < 5; i++) {
            GameObject ball = Instantiate(gizmoBall, rayStartPoint.position, Quaternion.identity);
            gizmoBalls[i] = ball;
        }
        */
    }

    // Update is called once per frame
    void Update()
    {
        if (rayAvailable) // If this is the first time we are doing this
        {
            Ray ray = new Ray(rayStartPoint.position, -rayStartPoint.right);

            MRUKRoom room = MRUK.Instance.GetCurrentRoom();

            if (room != null) {

                bool hasHit = room.Raycast(ray, rayLength, LabelFilter.Included(labelFilter), out RaycastHit hit, out MRUKAnchor anchor);

                int layerMask = LayerMask.GetMask("Outlined");
                bool hasHitGameObject = Physics.Raycast(ray, out RaycastHit hitGameObject, rayLength, layerMask);
                /*
                if (anchor != null) 
                {
                    for (int i = 0; i < anchor.PlaneBoundary2D.Count; i++)
                    {
                        gizmoBalls[i].transform.position = anchor.transform.position + new Vector3(anchor.PlaneBoundary2D[i].y, 0, anchor.PlaneBoundary2D[i].x);
                    }
                }
                */
                if (hasHitGameObject) // If the ray hit a usual game object that has been created
                {
                    if (!gizmoPlane.activeSelf) // If the gizmo plane is inactive, activate it
                    {
                        gizmoPlane.SetActive(true);
                    }
                    Vector3 hitNormal = hitGameObject.normal;
                    Vector3 hitPoint = hitGameObject.point;
                    // Set the positions of the LineRenderer
                    lineRenderer.SetPosition(0, rayStartPoint.position);
                    lineRenderer.SetPosition(1, hitGameObject.point);

                    MeshCollider meshCollider = hitGameObject.collider as MeshCollider;
                    int selectedVertexIndex = 0;

                    if (meshCollider != null && meshCollider.sharedMesh != null)
                    {
                        Mesh mesh = meshCollider.sharedMesh;
                        int triangleIndex = hitGameObject.triangleIndex;


                        if (triangleIndex >= 0 && triangleIndex * 3 + 2 < mesh.triangles.Length)
                        {
                            Vector3[] vertices = mesh.vertices;
                            int[] triangles = mesh.triangles;

                            // Retrieve the 3 vertices of the triangle
                            int v1 = triangles[triangleIndex * 3 + 0];
                            int v2 = triangles[triangleIndex * 3 + 1];
                            int v3 = triangles[triangleIndex * 3 + 2];

                            // Transform vertices to world space
                            Vector3 worldV1 = meshCollider.transform.TransformPoint(vertices[v1]);
                            Vector3 worldV2 = meshCollider.transform.TransformPoint(vertices[v2]);
                            Vector3 worldV3 = meshCollider.transform.TransformPoint(vertices[v3]);

                            // Find the vertex closest to the hit point
                            Vector3 closestVertex = worldV1; // Default to v1
                            selectedVertexIndex = v1;
                            float minDistance = Vector3.Distance(hitPoint, worldV1);

                            float distanceToV2 = Vector3.Distance(hitPoint, worldV2);
                            if (distanceToV2 < minDistance)
                            {
                                closestVertex = worldV2;
                                minDistance = distanceToV2;
                                selectedVertexIndex = v2;
                            }

                            float distanceToV3 = Vector3.Distance(hitPoint, worldV3);
                            if (distanceToV3 < minDistance)
                            {
                                closestVertex = worldV3;
                                selectedVertexIndex = v3;
                            }

                            // Move gizmoBall to the closest vertex
                            gizmoPlane.transform.position = closestVertex;

                            // Calculate the right vector parallel to the XZ plane
                            Vector3 rightInXZ = Vector3.Cross(Vector3.up, hitNormal).normalized;
                            if (rightInXZ == Vector3.zero)
                            {
                                // Handle edge case where hitNormal is parallel to Vector3.up
                                rightInXZ = Vector3.Cross(Vector3.forward, hitNormal).normalized;
                            }

                            // Calculate the forward vector based on the desired right and hitNormal
                            Vector3 forward = Vector3.Cross(hitNormal, rightInXZ).normalized;

                            // Construct the rotation
                            gizmoPlane.transform.rotation = Quaternion.LookRotation(forward, hitNormal);

                            Debug.Log($"Placed gizmoPlane at closest vertex: {closestVertex}, facing normal: {hitNormal}");
                        }
                        else
                        {
                            Debug.LogError("Triangle index out of range.");
                        }
                    }


                    // Hiding the text when the hover over the object
                    // environmentDebugText.transform.rotation = Quaternion.LookRotation(-hitNormal);
                    environmentDebugText.gameObject.SetActive(false);

                    if (indexFingerPinchValue.Value() == 1)
                    {
                        // What happens after the pinch action goes here (activates update function inside CalibrateWorldCoordinates)
                        CalibrateWorldCoordinates.gizmoPlaneSelectedObject = hitGameObject.transform.gameObject;
                        CalibrateWorldCoordinates.gizmoPlaneSelectedObjectVertexIndex = selectedVertexIndex;
                        CalibrateWorldCoordinates.gizmoPlaneRotation = gizmoPlane.transform.localRotation;
                        CalibrateWorldCoordinates.workingOnCreatedObject = true;
                        gizmoPlane.SetActive(false);

                        // Turn off the visual debuggers
                        lineRenderer.gameObject.SetActive(false);
                        environmentDebugText.gameObject.SetActive(false);
                        // Turn off the renderers of the scene objects
                        foreach (MRUKAnchor myAnchor in MRUK.Instance.GetCurrentRoom().Anchors)
                        {
                            MeshRenderer mr = myAnchor.transform.GetComponentInChildren<MeshRenderer>();
                            if (mr != null)
                            { // && myAnchor != anchor  ---- this section is taken out, used to be kept on as the selected workspace
                                mr.enabled = false;
                            }
                        }
                        rayAvailable = false;

                        // Enable the ability to select objects after the workspace is selected
                        Selection.selectionEnabled = true;
                    }
                }

                else if (hasHit) // If the ray hit a room object (furniture) with the selected label filters
                {
                    if (gizmoPlane.activeSelf) // If the gizmo plane is active, deactivate it
                    {
                        gizmoPlane.SetActive(false);
                    }
                    Vector3 hitPoint = hit.point;
                    Vector3 hitNormal = hit.normal;
                    // Set the positions of the LineRenderer
                    lineRenderer.SetPosition(0, rayStartPoint.position);
                    lineRenderer.SetPosition(1, hit.point);

                    string label = anchor.Label.ToString();

                    environmentDebugText.gameObject.SetActive(true);
                    environmentDebugText.transform.position = hitPoint;
                    environmentDebugText.transform.rotation = Quaternion.LookRotation(-hitNormal);
                    environmentDebugText.text = "select : " + label.ToLower(); // Making sure its written in lowercase

                    if (indexFingerPinchValue.Value() == 1)
                    {
                        if (label == "WALL_FACE" || label == "INVISIBLE_WALL_FACE" || label == "DOOR_FRAME" || label == "WINDOW_FRAME" || label == "SCREEN" || label == "WALL_ART") // If we selected to work on the wall
                        {
                            CalibrateWorldCoordinates.verticalWorkspaceAnchor = anchor;
                            CalibrateWorldCoordinates.wallFaceNormal = hitNormal;
                        }
                        else // If we selected to work on the table or ground or others
                        {
                            CalibrateWorldCoordinates.horizontalWorkspaceAnchor = anchor;
                        }
                        // Turn off the visual debuggers
                        lineRenderer.gameObject.SetActive(false);
                        environmentDebugText.gameObject.SetActive(false);

                        // Turn off the renderers of the scene objects
                        foreach (MRUKAnchor myAnchor in MRUK.Instance.GetCurrentRoom().Anchors)
                        {
                            MeshRenderer mr = myAnchor.transform.GetComponentInChildren<MeshRenderer>();
                            if (mr != null )
                            { // && myAnchor != anchor  ---- this section is taken out, used to be kept on as the selected workspace
                                mr.enabled = false;
                            }
                        }
                        rayAvailable = false;

                        // Enable the ability to select objects after the workspace is selected
                        Selection.selectionEnabled = true;
                    }
                }
            }
        }
    }

    
    // This function is called with the add new plane button, activates the whole code in the update above
    public void AddNewPlane()
    {
        // Turn on the visual debuggers
        lineRenderer.gameObject.SetActive(true);
        environmentDebugText.gameObject.SetActive(true);
        // Turn on the renderers of the scene objects
        foreach (MRUKAnchor myAnchor in MRUK.Instance.GetCurrentRoom().Anchors)
        {
            MeshRenderer mr = myAnchor.transform.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            { // && myAnchor != anchor  ---- this section is taken out, used to be kept on as the selected workspace
                mr.enabled = true;
            }
        }

        // Deselect all objects before selecting the objects
        selectionReference.DeselectAllObjects();

        // Disable the ability to select objects
        Selection.selectionEnabled = false;
        
        // Reset the rotation of the room before choosing the new plane
        MRUK.Instance.GetCurrentRoom().transform.rotation = Quaternion.Euler(0, 0, 0);
        workspacePlane.SetActive(false);

        CalibrateWorldCoordinates.horizontalWorkspaceAnchor = null;
        CalibrateWorldCoordinates.verticalWorkspaceAnchor = null;
        CalibrateWorldCoordinates.workingOnCreatedObject = false;
        CalibrateWorldCoordinates.selectedWorkspace = false;
        rayAvailable = true;
    }

    void InstantiatePrefab()
    {
        // Instantiate the prefab at the specified position with no rotation
        Instantiate(gizmoPhysicsBall, rayStartPoint.position, Quaternion.identity);
    }
}
