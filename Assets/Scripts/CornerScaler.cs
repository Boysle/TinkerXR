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

// Enables interactive resizing of a 3D cube via corner handles (buttons).
// Each corner handle determines its scaling direction based on position, and
// when selected, moving it updates both the cube’s position and scale dynamically.
// The script also switches the handle’s visuals between a sphere and
// an anchor model depending on interaction state. 
// This script goes under each corner scaler button (resize balls)
public class CornerScaler : MonoBehaviour{
    public float scaleFactor = 1f; // Scale factor for resizing
    private bool started = false;
    public GameObject cube, cornerButtonParent, anchorObjectModel;
    private Vector3 previousPosition;
    private int[] directionArr;
    
    private void Start()
    {   
        //This part is used for telling which corner button is at which location
        directionArr = new int[3] {1, 1, 1};
        int childCount = cornerButtonParent.transform.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++){
            children[i] = cornerButtonParent.transform.GetChild(i);
        }
        //Sorting the childs and getting the lower 4 
        System.Array.Sort(children, (a, b) => a.position.x.CompareTo(b.position.x));
        for (int i = 0; i < 4; i++){
            if (this.gameObject == children[i].gameObject) {
                directionArr[0] = -1;
            }
        }
        System.Array.Sort(children, (a, b) => a.position.y.CompareTo(b.position.y));
        for (int i = 0; i < 4; i++){
            if (this.gameObject == children[i].gameObject){
                directionArr[1] = -1;
            }
        }
        System.Array.Sort(children, (a, b) => a.position.z.CompareTo(b.position.z));
        for (int i = 0; i < 4; i++){
            if (this.gameObject == children[i].gameObject){
                directionArr[2] = -1;
            }
        }

        anchorObjectModel.gameObject.SetActive(false);
    }

    void Update(){
        // If we have selected this corner button, change its visuals and adjust the object manipulation cube based on its movement
        if (Selection.selectedManipulationUI && this.gameObject == Selection.selectionManipulationUIObject){
            if (!started){
                started = true;
                // We used to change its color, now we change the visual model
                // transform.GetComponent<MeshRenderer>().material.color = Color.blue;
                transform.GetChild(0).gameObject.SetActive(false); // Sphere model
                anchorObjectModel.SetActive(true); // Anchor model
                previousPosition = Selection.selectionManipulationUIObject.transform.position;
            }
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
        else if (started){
            started = false;
            // We used to change its color, now we change the visuals
            // transform.GetComponent<MeshRenderer>().material.color = Color.white;
            transform.GetChild(0).gameObject.SetActive(true); // Sphere model
            anchorObjectModel.SetActive(false); // Anchor model
        }
    }
}
