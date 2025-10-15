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

// Enables interactive resizing of a cube via edge handles. When an edge handle is selected and dragged,
// the script adjusts both the cube’s position and scale based on the handle’s movement. It also
// provides visual feedback by changing the handle’s color during interaction.
public class EdgeScaler : MonoBehaviour
{
    public float scaleFactor = 1f; // Scale factor for resizing
    private bool started = false;
    private GameObject cube;
    private Vector3 previousPosition;
    private int[] directionArr;

    void Update()
    {
        if (Selection.selectedManipulationUI && this.gameObject == Selection.selectionManipulationUIObject)
        {
            if (!started)
            {
                started = true;
                transform.GetComponent<MeshRenderer>().material.color = Color.blue;
                previousPosition = Selection.selectionManipulationUIObject.transform.position;
            }
            // Debug.Log("Started with: " + Selection.selectionManipulationUIObject.name);
            Vector3 movementDelta = transform.position - previousPosition;

            previousPosition = transform.position;


            // Adjusting the position of the object manipulation cube
            float xPos = cube.transform.position.x + (scaleFactor * movementDelta.x / 10f);
            float yPos = cube.transform.position.y + (scaleFactor * movementDelta.y / 10f);
            float zPos = cube.transform.position.z + (scaleFactor * movementDelta.z / 10f);
            cube.transform.position = new Vector3(xPos, yPos, zPos);

            // Adjusting the scale of the object manipulation cube
            float xScale = cube.transform.localScale.x + (scaleFactor * movementDelta.x * directionArr[0]);
            float yScale = cube.transform.localScale.y + (scaleFactor * movementDelta.y * directionArr[1]);
            float zScale = cube.transform.localScale.z + (scaleFactor * movementDelta.z * directionArr[2]);
            cube.transform.localScale = new Vector3(xScale, yScale, zScale);
        }
        else if (started)
        {
            started = false;
            transform.GetComponent<MeshRenderer>().material.color = Color.white;
            // Debug.Log("Cancelled.");
        }
    }
}
