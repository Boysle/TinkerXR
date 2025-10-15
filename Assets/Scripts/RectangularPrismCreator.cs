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

using UnityEngine;
using System.Collections.Generic;

// This script is called in "Selection" script which is in the Game Manager
// The script is located inside the GameObject called Rectangular Prism Generator inside Game Manager
// This script is responsible for resizing the object manipulation box/cube/(called prism here)
// such that it perfectly wraps the selected objects around
public class RectangularPrismCreator : MonoBehaviour{
    private List<ObjectMaterialPair> objectsToEnclose;
    public GameObject objectManipulationPrism;
    public GameObject objectManParent;
    public GameObject gizmoBall;
    public static GameObject objectManipulationParent;

    private void Awake()
    {
        objectManipulationParent = objectManParent;
    }

    // Function called in the "Selection" script
    public void UpdateManipulatorLocation(){
        // Ensure there are objects in the list, else return
        objectsToEnclose = Selection.selectedObjects;
        if (objectsToEnclose.Count == 0)
        {
            objectManipulationParent.SetActive(false);
            return;
        }

        // Activate the object manipulation box
        if (!objectManipulationParent.activeSelf)
        {
            objectManipulationParent.SetActive(true);
        }
        
        /* We used to do this with renderer.bounds but it had faulty outcomes when the objcets are rotated
        // Initialize combined bounds using the first object
        Bounds combinedBounds = objectsToEnclose[0].transform.GetComponent<Renderer>().bounds;

        // Iterate through remaining objects to update combined bounds
        for (int i = 1; i < objectsToEnclose.Count; i++)
        {
            Bounds objectBounds = objectsToEnclose[i].transform.GetComponent<Renderer>().bounds;
            combinedBounds.Encapsulate(objectBounds);
        }

        // Calculate dimensions
        float width = combinedBounds.size.x;
        float height = combinedBounds.size.y;
        float depth = combinedBounds.size.z;
        Debug.LogError("Width = " + width + " Height = " + height + " Depth = " + depth);

        // Calculate center position
        Vector3 center = combinedBounds.center;
        */

        // Initialize variables for calculating min and max bounds
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        // Iterate through all objects and their vertices to calculate combined min/max bounds
        foreach (var objectPair in objectsToEnclose)
        {
            // Get the object's mesh filter and mesh to extract vertices
            Mesh mesh = objectPair.transform.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;

            // Convert vertices from local to world space and update min/max bounds
            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = objectPair.transform.TransformPoint(vertex);

                min = Vector3.Min(min, worldVertex); // Update min bounds
                max = Vector3.Max(max, worldVertex); // Update max bounds
            }
        }

        // Calculate the combined width, height, and depth
        float width = max.x - min.x;
        float height = max.y - min.y;
        float depth = max.z - min.z;

        // Calculate the center position
        Vector3 center = (min + max) / 2;

        // Update the manipulation box position and scale
        objectManipulationPrism.transform.position = center;
        objectManipulationPrism.transform.localScale = new Vector3(width*5f, height*5f, depth*5f);

        objectManipulationPrism.transform.rotation = Quaternion.identity;

        /*
        // Debugging: Instantiate gizmo balls at each vertex of every selected object
        foreach (var objectPair in objectsToEnclose)
        {
            // Get the object's mesh filter and mesh to extract vertices
            Mesh mesh = objectPair.transform.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;

            // Convert vertices from local to world space and instantiate gizmo balls
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldVertex = objectPair.transform.TransformPoint(vertices[i]);
                Instantiate(gizmoBall, worldVertex, Quaternion.identity);
            }
        }
        */
    }
}
