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

// Makes a GameObject face the camera when applyBillboardEffect is true

public class ScaleBillboard : MonoBehaviour
{
    public GameObject billboardPrefab; // Reference to the prefab of the billboard asset
    public float minDistance = 5.0f; // Minimum distance to start scaling
    public float maxDistance = 15.0f; // Maximum distance for full scale
    public float defaultScale = 0.2f; // Default scale when viewed from a distance
    public bool applyBillboardEffect = true;

    void Update()
    {
        if (applyBillboardEffect)
        {
            // Make the billboard always face the camera
            transform.LookAt(Camera.main.transform);
        }

        // Calculate the distance between the billboard and the camera
        float distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);

        // Calculate the scale factor based on distance
        float scaleFactor = defaultScale;

        if (distanceToCamera < maxDistance)
        {
            float t = Mathf.InverseLerp(minDistance, maxDistance, distanceToCamera);
            scaleFactor = Mathf.Lerp(defaultScale, 1.0f, t);
        }

        float zScaleFactor = 1.0f;
        if(!applyBillboardEffect) { zScaleFactor = scaleFactor; }

        // Apply the scale to the billboard
        transform.localScale = new Vector3(scaleFactor, scaleFactor, zScaleFactor);
    }
}