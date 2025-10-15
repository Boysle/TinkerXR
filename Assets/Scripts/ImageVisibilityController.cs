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

// This script is created so that the canvases created on the hands hide the images on them when they are backwards

public class ImageVisibilityController : MonoBehaviour
{
    private CanvasRenderer[] canvasRenderers;

    void Start()
    {
        canvasRenderers = GetComponentsInChildren<CanvasRenderer>();
    }

    void Update()
    {
        Vector3 toCamera = Camera.main.transform.position - transform.position;
        Vector3 forward = transform.forward;

        // Check if the angle between the canvas's forward direction and the camera's direction is greater than 90 degrees
        float alpha = Vector3.Dot(forward, toCamera) > 0 ? 0 : 1;

        // Set the alpha value for all CanvasRenderers
        foreach (CanvasRenderer canvasRenderer in canvasRenderers)
        {
            canvasRenderer.SetAlpha(alpha);
        }
    }
}
