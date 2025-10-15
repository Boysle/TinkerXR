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
using System.Linq;
using System.Threading;
using UnityEngine;

// Debug/utility script for manipulating object bounds, scaling, and parent-child hierarchy in Unity

public class RendererBoundsTest : MonoBehaviour
{
    public GameObject objectToEnclose;
    public GameObject objectManipulationPrism;
    public GameObject parentObject;
    private Mesh mesh;
    private Vector3[] vertices, modifiedVerts;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Fit object manipulation cube on the object
        if (Input.GetKeyDown("k"))
        {
            Bounds combinedBounds = objectToEnclose.transform.GetComponent<SkinnedMeshRenderer>().bounds;
            // Calculate dimensions
            float width = combinedBounds.size.x;
            float height = combinedBounds.size.y;
            float depth = combinedBounds.size.z;

            // Calculate center position
            Vector3 center = combinedBounds.center;

            objectManipulationPrism.transform.position = center;
            objectManipulationPrism.transform.localScale = new Vector3(width, height, depth);
        }
        
        // Detatch and reattatch the parent object from child objects after every action to make sure the parent object scale is always uniform
        if (Input.GetKeyDown("p"))
        {
            for (int i = parentObject.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = parentObject.transform.GetChild(i);
                child.SetParent(null); // Detach the child from the parent
            }
            parentObject.transform.localScale = new Vector3(1, 1, 1);

            // Imagine adding the childs again everytime here                                   
        }

        // Stretch object manipulation cube
        if (Input.GetKeyDown("l"))
        {
            Vector3 original = objectManipulationPrism.transform.localScale;
            objectManipulationPrism.transform.localScale = new Vector3(original.x, original.y * 2, original.z);
            // We need to do this after every child object of this parent object is removed
        }

        // Stretch the parent of selected object
        if (Input.GetKeyDown("r"))
        {
            Vector3 original = parentObject.transform.localScale;
            parentObject.transform.localScale = new Vector3(original.x, original.y * 2, original.z);
        }
    }
}
