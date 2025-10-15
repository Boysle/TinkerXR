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

// Positions a GameObject relative to another target object by applying a configurable offset.
// It only updates when the target object is currently selected in the manipulation UI.
public class FollowObjectWithOffset : MonoBehaviour
{
    public GameObject objectToFollow;
    public Vector3 offset;

    void Update()
    {
        if (objectToFollow != null && Selection.selectedManipulationUI && Selection.selectionManipulationUIObject == objectToFollow.gameObject)
        {
            Vector3 newOffset = new Vector3(offset.x, offset.y - (transform.lossyScale.y / 2f), offset.z);
            transform.position = objectToFollow.transform.position + newOffset;
        }
    }
}
