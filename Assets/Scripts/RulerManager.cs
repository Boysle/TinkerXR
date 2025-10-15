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
using System.Globalization;
using TMPro;
using UnityEngine;

// An implementation for a virtual ruler tool
// This script can be found under the object manipulation parent
public class RulerManager : MonoBehaviour
{
    public Transform ruler, leftHand, edge1, edge2, textParent;
    public Vector3 spawnOffset1, spawnOffset2;
    public float realWorldScaleFactor = 100f;
    public TextMeshProUGUI distanceText;
    private LineRenderer lineRenderer;
    private bool rulerActive;

    void Start()
    {
        lineRenderer = ruler.GetComponent<LineRenderer>();
        ruler.gameObject.SetActive(false);
        rulerActive = false;
    }

    void Update()
    {
        // Update the positions of the line renderer
        lineRenderer.SetPosition(0, edge1.position);
        lineRenderer.SetPosition(1, edge2.position);

        // Calculate the distance between the two points
        float distance = Vector3.Distance(edge1.position, edge2.position) * realWorldScaleFactor;

        // Update the distance text
        distanceText.text = distance.ToString("F2", CultureInfo.InvariantCulture) + " cm";

        // Optionally, position the text between the two points in screen space
        Vector3 midPoint = (edge1.position + edge2.position) / 2;
        textParent.position = midPoint + new Vector3(0f, 0.03f, 0f);
    }

    public void HideRuler() // Hide or show the ruler object when the pokable button is pressed
    {
        rulerActive = !rulerActive;
        ruler.gameObject.SetActive(rulerActive);

        if (rulerActive) 
        {
            edge1.position = leftHand.position + spawnOffset1;
            edge2.position = leftHand.position + spawnOffset2;
        }
    }
}
