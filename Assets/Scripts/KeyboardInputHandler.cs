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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;

// Manages a virtual numerical keyboard in Unity for modifying object scales via a TMP_InputField.
// This script is placed in Game Manager
public class KeyboardInputHandler : MonoBehaviour
{
    // Reference to the TextMeshPro InputField
    public TMP_InputField inputField;
    [SerializeField]
    public GameObject KeyboardMenuObject;
    public GameObject ObjectManipulationParent;
    public GameObject LeftHandMenus;
    public float realSizeScale = 20.2f;

    // Variable to hold the copied text
    private string copiedText;
    public static bool keyboardOpen ,firstKeyboardAction; // Value changed in ObjectGrabDetector

    void Start()
    {
        keyboardOpen = false;
        firstKeyboardAction = false;
        KeyboardMenuObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // This is the first thing to do once a keyboard is being opened
        // The keyboard might be still open when we need to do an update to this
        if (firstKeyboardAction)
        {
            firstKeyboardAction = false;
            if (!keyboardOpen)
            {
                KeyboardMenuObject.SetActive(true);
                HideOtherManipulationTools(keyboardOpen);
                HideOtherMenus(keyboardOpen);
                keyboardOpen = true;
            }

            if (inputField != null)
            {
                // Clear the current text first
                inputField.text = string.Empty;
                // Put in the initial scale of the object
                if (VertexScaler.activeScalingAxis == 0)
                {
                    inputField.text += (VertexScaler.axisScale.x * realSizeScale).ToString("F4", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'); ; // 4 decimals
                    VertexScaler.currentAxisScale = VertexScaler.axisScale.x;
                }
                else if (VertexScaler.activeScalingAxis == 1)
                {
                    inputField.text += (VertexScaler.axisScale.y * realSizeScale).ToString("F4", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'); ;
                    VertexScaler.currentAxisScale = VertexScaler.axisScale.y;
                }
                else if (VertexScaler.activeScalingAxis == 2)
                {
                    inputField.text += (VertexScaler.axisScale.z * realSizeScale).ToString("F4", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'); ;
                    VertexScaler.currentAxisScale = VertexScaler.axisScale.z;
                }
            } 
        }
    }

    // Function to add a letter to the InputField
    public void AddCharacter(string character)
    {
        // Check if inputField is not null
        if (inputField != null)
        {
            // Get the current text
            string currentText = inputField.text;

            // Constraint: Only allow one dot
            if (character == "." && currentText.Contains("."))
            {
                Debug.LogError("Cannot add another dot.");
                return;
            }

            // Constraint: Remove leading zero if not followed by a dot
            if (currentText == "0" && character != ".")
            {
                currentText = "";
            }

            // Constraint: Add 0 in front if . is the first character
            if (character == "." && currentText == "")
            {
                currentText = "0";
            }

            // Add the letter to the current text
            currentText += character;

            // Update the input field's text
            inputField.text = currentText;

            UpdateWorldObjects();
        }
        else
        {
            Debug.LogError("InputField is not assigned.");
        }
    }

    public void Add1()
    {
        AddCharacter("1");
    }
    public void Add2()
    {
        AddCharacter("2");
    }
    public void Add3()
    {
        AddCharacter("3");
    }
    public void Add4()
    {
        AddCharacter("4");
    }
    public void Add5()
    {
        AddCharacter("5");
    }
    public void Add6()
    {
        AddCharacter("6");
    }
    public void Add7()
    {
        AddCharacter("7");
    }
    public void Add8()
    {
        AddCharacter("8");
    }
    public void Add9()
    {
        AddCharacter("9");
    }
    public void Add0()
    {
        AddCharacter("0");
    }
    public void AddDot()
    {
        AddCharacter(".");
    }

    public void DeleteLastCharacter()
    {
        // Check if inputField is not null and has text
        if (inputField != null && inputField.text.Length > 0)
        {
            // Remove the last character
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }

        UpdateWorldObjects();
    }

    // Function to copy the text from the InputField
    public void CopyText()
    {
        if (inputField != null)
        {
            // Copy the text
            copiedText = inputField.text;
        }
    }

    // Function to paste the copied text into the InputField
    public void PasteText()
    {
        if (inputField != null)
        {
            // Clear the current text first
            inputField.text = string.Empty;
            // Paste the copied text
            inputField.text += copiedText;

            UpdateWorldObjects();
        }
    }

    public void DoneButtonAction()
    {
        UpdateWorldObjects();
        HideOtherManipulationTools(keyboardOpen);
        HideOtherMenus(keyboardOpen);
        keyboardOpen = false;
        KeyboardMenuObject.gameObject.SetActive(false); // Turn off the keyboard
    }

    public void UpdateWorldObjects()
    {
        if (inputField != null)
        {
            // Try to parse the text to a float
            if (float.TryParse(inputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                if (value != 0f) // Making sure we have no zero length
                {
                    VertexScaler.currentAxisScale = (value / realSizeScale);
                }
            }
            else
            {
                Debug.LogError("Invalid float value.");
            }
        }
    }

    // Hiding/showing other tools and menus to avoid interference while the keyboard is active
    public void HideOtherManipulationTools(bool hide)
    {
        foreach (Transform child in ObjectManipulationParent.transform)
        {
            // Check if the child's name does not match the specified name
            // We need to find a better way to do this other than just searching by names
            if (child.name != "Edge Button Parent" && !child.name.Contains("EdgeLine") && !child.name.Contains("ProjectionLine") && !child.name.Contains("Resize Cone") && !child.name.Contains("Anchor Object") && !child.name.Contains("Ruler Tool") && !child.name.Contains("Uniform Scaler Line"))
            {
                // Deactivate the child GameObject
                child.gameObject.SetActive(hide);
            }

            // Hide the manipulation object in the other axes
            int ax = VertexScaler.activeScalingAxis;
            if (child.name == "Edge Button Parent")
            {
                foreach (Transform schild in child.transform)
                {
                    if (schild.GetComponent<ActiveScalingAxis>().GetAxisValue() != ax)
                    {
                        schild.gameObject.SetActive(hide);
                    }
                }
            }
        }
    }
    public void HideOtherMenus(bool hide)
    {
        foreach (Transform child in LeftHandMenus.transform)
        {
            // Check if the child's name does not match the specified name
            if (child.name != "Numerical Keyboard Menu")
            {
                // Deactivate the child GameObject
                child.gameObject.SetActive(hide);
            }
        }
    }
}
