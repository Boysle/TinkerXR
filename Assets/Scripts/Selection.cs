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
using UnityEngine.EventSystems;
using System.Linq;
using System;
using Oculus.Interaction;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using TMPro;
using UnityEngine.UI;
using LibCSG;
using Meta.XR.MRUtilityKit;
using UnityEngine.SceneManagement;

// This script stands inside game manager's child
public class ObjectMaterialPair{
    public Transform transform;
    public Material originalMaterial;
    public bool positivity; // positive (true) if solid object, negative (false) if hole object
    public bool referenceness; // true if reference object, false if normal object


    public ObjectMaterialPair(Transform objTransform, Material material, bool positive, bool reference)
    {
        transform = objTransform;
        originalMaterial = material;
        positivity = positive;
        referenceness = reference;
    }
}

[System.Serializable]
public class _SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public _SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
public class _SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public _SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}

[System.Serializable]
public class SerializableMeshInfo
{
    private float[] vertices;
    private int[] triangles;
    private float[] uv;
    private float[] uv2;
    private float[] normals;
    private float[] colors;

    public SerializableMeshInfo(Mesh m) // Constructor: takes a mesh and fills out SerializableMeshInfo data structure which basically mirrors Mesh object's parts.
    {
        vertices = new float[m.vertexCount * 3]; // initialize flattened vertices array.
        for (int i = 0; i < m.vertexCount; i++)
        {
            vertices[i * 3] = m.vertices[i].x;
            vertices[i * 3 + 1] = m.vertices[i].y;
            vertices[i * 3 + 2] = m.vertices[i].z;
        }
        triangles = new int[m.triangles.Length]; // initialize triangles array (1-dimensional so no need for flattening)
        for (int i = 0; i < m.triangles.Length; i++)
        {
            triangles[i] = m.triangles[i];
        }
        uv = new float[m.uv.Length * 2]; // initialize flattened uvs array
        for (int i = 0; i < m.uv.Length; i++)
        {
            uv[i * 2] = m.uv[i].x;
            uv[i * 2 + 1] = m.uv[i].y;
        }
        uv2 = new float[m.uv2.Length * 2]; // uv2
        for (int i = 0; i < m.uv2.Length; i++)
        {
            uv2[i * 2] = m.uv2[i].x;
            uv2[i * 2 + 1] = m.uv2[i].y;
        }
        normals = new float[m.normals.Length * 3]; // initialize flattened normals array
        for (int i = 0; i < m.normals.Length; i++)
        {
            normals[i * 3] = m.normals[i].x;
            normals[i * 3 + 1] = m.normals[i].y;
            normals[i * 3 + 2] = m.normals[i].z;
        }

        colors = new float[m.colors.Length * 4];
        for (int i = 0; i < m.colors.Length; i++)
        {
            colors[i * 4] = m.colors[i].r;
            colors[i * 4 + 1] = m.colors[i].g;
            colors[i * 4 + 2] = m.colors[i].b;
            colors[i * 4 + 3] = m.colors[i].a;
        }
    }

    // GetMesh gets a Mesh object from the current data in this SerializableMeshInfo object.
    // Sequential values are deserialized to Mesh original data types like Vector3 for vertices.
    public Mesh GetMesh()
    {
        Mesh m = new Mesh();
        List<Vector3> verticesList = new List<Vector3>();
        for (int i = 0; i < vertices.Length / 3; i++)
        {
            verticesList.Add(new Vector3(
                    vertices[i * 3], vertices[i * 3 + 1], vertices[i * 3 + 2]
                ));
        }
        m.SetVertices(verticesList);
        m.triangles = triangles;
        List<Vector2> uvList = new List<Vector2>();
        for (int i = 0; i < uv.Length / 2; i++)
        {
            uvList.Add(new Vector2(
                    uv[i * 2], uv[i * 2 + 1]
                ));
        }
        m.SetUVs(0, uvList);
        List<Vector2> uv2List = new List<Vector2>();
        for (int i = 0; i < uv2.Length / 2; i++)
        {
            uv2List.Add(new Vector2(
                    uv2[i * 2], uv2[i * 2 + 1]
                ));
        }
        m.SetUVs(1, uv2List);
        List<Vector3> normalsList = new List<Vector3>();
        for (int i = 0; i < normals.Length / 3; i++)
        {
            normalsList.Add(new Vector3(
                    normals[i * 3], normals[i * 3 + 1], normals[i * 3 + 2]
                ));
        }
        m.SetNormals(normalsList);

        List<Color> colorsList = new List<Color>();
        for (int i = 0; i < colors.Length / 4; i++)
        {
            colorsList.Add(new Color(
                    colors[i * 4],
                    colors[i * 4 + 1],
                    colors[i * 4 + 2],
                    colors[i * 4 + 3]
                ));
        }
        m.SetColors(colorsList);

        return m;
    }
}

[System.Serializable]
public class _GameObjectData
{
    public string typeName;
    public _SerializableVector3 position;
    public _SerializableQuaternion rotation;
    public _SerializableVector3 scale;
    public SerializableMeshInfo customMesh;

    public _GameObjectData(GameObject obj)
    {
        typeName = obj.name;
        if (typeName == "custom")
        {
            customMesh = new SerializableMeshInfo(obj.GetComponent<MeshFilter>().mesh);
        }
        position = new _SerializableVector3(obj.transform.position);
        rotation = new _SerializableQuaternion(obj.transform.rotation);
        Vector3 calculatedScale = obj.transform.localScale;
        Transform _parent = obj.transform.parent;
        while(_parent != null)
        {
            calculatedScale.x *= _parent.localScale.x;
            calculatedScale.y *= _parent.localScale.y;
            calculatedScale.z *= _parent.localScale.z;
            _parent = _parent.parent;
        }
        scale = new _SerializableVector3(calculatedScale);
    }
}

// The main script that handles selection, highlighting, and manipulation of objects in the scene,
// as well holding the information about created objects and their states (negative, reference, printer)

public class Selection : MonoBehaviour {
    public Material highlightMaterial;
    public Material selectionMaterial;
    public Material negativeBaseMaterial;
    public Material negativeOuterMaterial;
    public Material negativeSelectionMaterial;
    public Material referenceBaseMaterial;
    public Material referenceOuterMaterial;
    public Material referenceSelectionMaterial;
    public Material negativeReferenceOuterMaterial;
    public Material printerSelectionMaterial;
    public CubeHighlighter cubeHighlighterScriptReference;
    public RectangularPrismCreator rectangularPrismCreatorReference;
    public FingerPinchValue middleFingerPinchValue;
    public FingerPinchValue ringFingerPinchValue;
    // This can be used for changing more than one material in one object
    private Material[] selectionMatArray;
    private Material[] negativeMatArray;
    private Material[] referenceMatArray;
    private Material[] negativeReferenceMatArray;
    private Material[] negativeSelectionMatArray;
    private Material[] referenceSelectionMatArray;
    private Material[] negativeReferenceSelectionMatArray;
    private Material[] printerSelectionMatArray;
    public static List<ObjectMaterialPair> selectedObjects = new List<ObjectMaterialPair>();
    public static List<ObjectMaterialPair> oldSelectedObjects = new List<ObjectMaterialPair>();
    private List<GameObject> createdObjects = new List<GameObject>();
    public static bool selectedManipulationUI, selectedCtrl, selectionEnabled;
    public static GameObject selectionManipulationUIObject;
    public static GameObject objectOfInterest;
    public static int highlightCalls, selectionCalls;
    private Material originalMaterialHighlight;
    private Transform highlight;
    public Transform selection;
    public GameObject manipulationTool, worldObjects;
    private RaycastHit raycastHit;
    public static Selection instance = null;
    public GameObject specialObjectPrefab, cubePrefab, spherePrefab, cylinderPrefab, capsulePrefab, triangularPrismPrefab, pyramidPrefab, conePrefab, leftHand;
    public float creationDistance = 5f;
    public AudioSource audioSource;
    public AudioClip selectClip, deselectClip;

    // Artun's printer related variables
    public static bool isDragging = false, dropObjects = false;
    public GameObject printerObject;
    private HashSet<GameObject> printers;
    private HashSet<GameObject> negatives;
    private HashSet<GameObject> references;
    private CSGBrushOperation CSGOp;
    public float enlargeScale = 1.5f;
    public static TMP_InputField selectedField = null;
    public static bool selectedFieldTrue = false;
    public static Vector3[] originalObjectPositions = new Vector3[0];
    public float visibleInfoTextDuration = 3f; // Duration the text stays fully visible
    public float fadeInfoTextDuration = 2f; // Duration for the text to fade out
    public Coroutine fadeTextCoroutine; // Reference to the currently running coroutine
    public Transform objectManipulationParent;
    private bool changePrinterSelected = false;
    public TextMeshProUGUI screenWideInfoText;

    // Negative objects variable
    private bool carvingOn = false;
    public RawImage positiveToggleImage;
    public RawImage negativeToggleImage;
    public TextMeshProUGUI carvingToggleText;

    // Reference objects variable
    private bool referenceOn = false;
    public RawImage referenceOffToggleImage;
    public RawImage referenceOnToggleImage;
    public TextMeshProUGUI referenceToggleText;

    // Material shader change variable
    public Material[] materials; // Assign 7 materials in the Inspector
    private Shader litShader;
    private Shader unlitShader;
    bool colorLit = false;
    private HashSet<string> matNameHashSet = new HashSet<string>();

    // Added to make the created objects the child off the room
    private bool foundRoom = false;
    private MRUKRoom room;
    public static GameObject createdObjectsRoomParent;
    public Transform cameraRig;
    public TextMeshPro debugText;

    private GameObject createdPrinter;

    void Awake() {
        if (instance != null && instance != this)
            Destroy(gameObject);    // Ensures that there aren't multiple Singletons

        instance = this;
        selectionMatArray = new Material[2] { selectionMaterial, selectionMaterial };
        negativeMatArray = new Material[3] { negativeBaseMaterial, negativeBaseMaterial, negativeOuterMaterial };
        referenceMatArray = new Material[3] { referenceBaseMaterial, referenceBaseMaterial, referenceOuterMaterial };
        negativeReferenceMatArray = new Material[3] { referenceBaseMaterial, referenceBaseMaterial, negativeReferenceOuterMaterial };
        negativeSelectionMatArray = new Material[3] { negativeSelectionMaterial, negativeSelectionMaterial , negativeOuterMaterial };
        referenceSelectionMatArray = new Material[3] { referenceSelectionMaterial, referenceSelectionMaterial, referenceOuterMaterial };
        negativeReferenceSelectionMatArray = new Material[3] { referenceSelectionMaterial, referenceSelectionMaterial, negativeReferenceOuterMaterial };
        printerSelectionMatArray = new Material[2] { printerSelectionMaterial, printerSelectionMaterial };
        selectedManipulationUI = false;
        selectedCtrl = false;
        selectionEnabled = false;
        highlightCalls = 0; selectionCalls = 0;

        printers = new HashSet<GameObject>();
        negatives = new HashSet<GameObject>();
        references = new HashSet<GameObject>();
        CSGOp = new CSGBrushOperation();
        selectedField = null;

        positiveToggleImage.gameObject.SetActive(true);
        negativeToggleImage.gameObject.SetActive(true);
        negativeToggleImage.enabled = false;

        referenceOffToggleImage.gameObject.SetActive(true);
        referenceOnToggleImage.gameObject.SetActive(true);
        referenceOnToggleImage.enabled = false;

        // Load the shaders by their names
        litShader = Shader.Find("Meta/Depth/URP/Occlusion Lit");
        unlitShader = Shader.Find("Meta/Depth/URP/Occlusion Unlit");

        if (materials[0].shader == unlitShader)
        {
            colorLit = false;
        }

        foundRoom = false;
    }

    void Update()
    {
        // Only works when the selection is enabled, usually after the workspace is selected
        // We need to make sure that all of the objects are deselected and have proper materials before disabling selection
        if (selectionEnabled)
        {
            // debugText.text = cameraRig.transform.position.ToString();
            // Making sure this only runs once
            if (!foundRoom)
            {
                room = MRUK.Instance.GetCurrentRoom();
                if (room != null)
                {
                    foundRoom = true;
                    // Similar to every created object, camera is put into the MRUK room with anchor so when the room is rotated for changing planes, the object's orientation do not change relative to the camera
                    cameraRig.transform.SetParent(room.transform);
                    MRUKAnchor cameraAnchor = cameraRig.GetComponent<MRUKAnchor>();
                    cameraAnchor.Room = room;
                    cameraAnchor.ParentAnchor = room.transform.Find("FLOOR").GetComponent<MRUKAnchor>();
                }
            }


            // Highlight Section
            // Reseting the old highlighted material when a different object is highlighted 
            if (highlight != null)
            {
                highlight.GetComponent<MeshRenderer>().sharedMaterial = originalMaterialHighlight;
                highlight = null;
            }
            // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // bool mouseRaycastHit = !EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out raycastHit);
            // bool mouseRaycastHit = false;
            // Make sure you have EventSystem in the hierarchy before using EventSystem

            // If highlightCalls is bigger than 0, meaning if we are calling to highlight an object
            if (highlightCalls > 0) // or mourseRaycastHit
            {
                // Change material when hovered over
                // if (mouseRaycastHit) { highlight = raycastHit.transform; }
                if (objectOfInterest != null) { highlight = objectOfInterest.transform; }
                else { highlight = null; }

                if (highlight != null && highlight.CompareTag("Selectable"))
                {
                    MeshRenderer mr = highlight.GetComponent<MeshRenderer>();
                    if (mr.sharedMaterial != highlightMaterial)
                    {
                        Material[] checkMats = highlight.GetComponent<MeshRenderer>().materials;
                        if (checkMats[0].name == "Material_Selected (Instance)" && checkMats[1].name != "Material_Selected (Instance)" && checkMats[1] != selectionMaterial)
                        {
                            highlight.GetComponent<MeshRenderer>().material = checkMats[1]; // Making sure the original object material is saved correctly on both materials
                        }

                        originalMaterialHighlight = highlight.GetComponent<MeshRenderer>().material;
                        mr.sharedMaterial = highlightMaterial;
                    }
                }
            }
            

            // Selection Section
            bool mouseClickHit = Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject();
            // If we selected anything
            if (mouseClickHit || (selectionCalls > 0 && XRSelection.tapSelected))
            {
                XRSelection.tapSelected = false;
                if (mouseClickHit) { selection = raycastHit.transform; }
                else if (objectOfInterest != null) { selection = objectOfInterest.transform; }
                // If we clicked a UI element, do nothing
                if (selection != null && selectedManipulationUI)
                {
                    // Debug.Log("Selected this object: ", selectionManipulationUIObject);
                    return;
                }
                // Highlighted click
                else
                {
                    // If ctrl is not pressed while clicking
                    if (!selectedCtrl)
                    {
                        DeselectAndRemoveOthers(selection);
                    }
                    AddOrRemoveSelectedObject(selection, selectedCtrl);
                    // Debug.Log("Selected number of objects: " + selectedObjects.Count);
                }
                CallUpdateFunction(); // Function to update manipulator location and its size
                /* This section was abandoned due to rotation issues
                 * Back then we just added selected objects as a child of the selected object at a selection click and let unity hadle resizing automatically
                // Setting off objects with old parents
                foreach (ObjectMaterialPair pair in oldSelectedObjects)
                {
                    pair.transform.SetParent(worldObjects.transform);
                }
                CallUpdateFunction(); // Function to update manipulator location and its size
                foreach (ObjectMaterialPair pair in selectedObjects)
                {
                    pair.transform.SetParent(manipulationTool.transform);
                }
                // Putting in objects with new parents
                oldSelectedObjects = new List<ObjectMaterialPair>(selectedObjects);
                */
            }

            // Empty space pinch with middle finger
            if (!selectedManipulationUI && highlightCalls == 0 && middleFingerPinchValue.Value() > 0.8f)
            {
                DeselectAllObjects();
            }

            // Empty space pinch with ring finger
            if (!selectedManipulationUI && ringFingerPinchValue.Value() > 0.5f)
            {
                SelectAllObjects();
            }

            // Should check if selected something and draging
            // Printer objects should not be selected in multi select mode
            // And should not trigger other printers.
            // Debug.Log("Printer okay.");

            // Part where the selecteded objects are dropped into the printer
            if (dropObjects)
            {
                foreach (GameObject p in printers)
                {
                    PrinterProperties pp = p.GetComponent<PrinterProperties>();
                    if (pp.enlarged)
                    {
                        ConvertToSTL(pp);
                    }
                }
            }

            // Revert the printer's effects after dropping
            if (isDragging || dropObjects)
            {
                foreach (GameObject p in printers)
                {
                    PrinterProperties pp = p.GetComponent<PrinterProperties>();
                    if (pp.enlarged)
                    {
                        pp.enlarged = false;
                        p.transform.localScale = pp.originalScale;
                    }
                }
                if (dropObjects)
                {
                    dropObjects = false;
                }
            }

            // Part where the printer is done with being highlighted
            if (isDragging && highlight != null)
            {
                GameObject obj = highlight.gameObject;
                if (obj.name == "printer")
                {
                    PrinterProperties pp = obj.GetComponent<PrinterProperties>();
                    if (!pp.enlarged)
                    {
                        pp.enlarged = true;
                        pp.originalScale = obj.transform.localScale;
                        obj.transform.localScale *= enlargeScale;
                    }
                }
            }

            // Check if the printer input field has been selected
            if (selectedFieldTrue == true)
            {
                screenWideInfoText.text = "Selected Box " + selectedField.name;
                selectedField.transform.GetChild(0).Find("Placeholder").GetComponent<TMP_Text>().text = "";
                selectedFieldTrue = false;
                StartCoroutine(SelectInputField());
            }
        }
    }

    IEnumerator SelectInputField()
    {
        yield return new WaitForEndOfFrame();
        selectedField.ActivateInputField();
    }

    // Method to deselect all objects
    public void DeselectAllObjects() {
        foreach (ObjectMaterialPair pair in selectedObjects) {
            // Restore the original material of each selected object
            MeshRenderer renderer = pair.transform.GetComponent<MeshRenderer>();
            if (renderer != null) {
                // If this deselected object is a negative non-reference object
                if (negatives.Contains(pair.transform.gameObject) && !references.Contains(pair.transform.gameObject))
                {
                    renderer.materials = negativeMatArray;
                }
                // If this deselected object is a positive reference object
                else if (!negatives.Contains(pair.transform.gameObject) && references.Contains(pair.transform.gameObject))
                {
                    renderer.materials = referenceMatArray;
                }
                // If this deselected object is a negative reference object
                else if (negatives.Contains(pair.transform.gameObject) && references.Contains(pair.transform.gameObject))
                {
                    renderer.materials = negativeReferenceMatArray;
                }
                // Default object
                else
                {
                    Material[] defMatArray = new Material[2] { pair.originalMaterial, pair.originalMaterial };
                    renderer.materials = defMatArray;
                }
            }
        }
        audioSource.transform.position = manipulationTool.transform.position;
        PlayAudio(deselectClip);

        // Clear the list to deselect all objects
        selectedObjects.Clear();
        selectedManipulationUI = false;
        ObjectGrabDetector._isGrabbingMovementHandle = false;
        ObjectGrabDetector._isGrabbingUIElement = false;
        CallUpdateFunction();
    }

    // Method to deselect all objects
    public void SelectAllObjects()
    {
        // If the printer is selected, deselect it first
        if(selectedObjects.Count > 0 && selectedObjects[0].transform.gameObject.name == "printer")
        {
            DeselectAllObjects();
        }

        HashSet<Transform> selectedSet = new HashSet<Transform>();
        foreach (ObjectMaterialPair pair in selectedObjects)
        {
            selectedSet.Add(pair.transform);
        }
        foreach (GameObject obj in createdObjects)
        {
            // Among all created objects, if it is not in the selecteds set, select it (and don't select the printer)
            if (!selectedSet.Contains(obj.transform) && obj.name != "printer")
            {
                AddSelectedObject(obj.transform, true);
            }
        }
        selectedManipulationUI = false;
        ObjectGrabDetector._isGrabbingMovementHandle = false;
        ObjectGrabDetector._isGrabbingUIElement = false;
        CallUpdateFunction();
    }

    public void DuplicateSelectedObjects()
    {
        foreach (ObjectMaterialPair pair in selectedObjects)
        {
            GameObject newObject = Instantiate(pair.transform.gameObject);
            newObject.name = pair.transform.gameObject.name;
            Material[] originMats = new Material[2] { pair.originalMaterial, pair.originalMaterial };
            newObject.GetComponent<MeshRenderer>().materials = originMats;
            Transform newTransform = newObject.transform;
            newTransform.position = pair.transform.position + new Vector3(0.11f, 0, 0);
            ObjectMaterialPair newPair = new ObjectMaterialPair(newTransform, pair.originalMaterial, pair.positivity, pair.referenceness);
            createdObjects.Add(newObject);
        }
    }

    // Method to add or remove a selected object based on Ctrl key
    void AddOrRemoveSelectedObject(Transform selectedTransform, bool ctrlPressed) {
        bool isInList = IsTransformInList(selectedTransform);

        if (ctrlPressed && isInList) {
            // Ctrl pressed and object is already in the list, remove it
            RemoveSelectedObject(selectedTransform);
        }
        else if (!isInList) {
            // Ctrl pressed and object not in the list or ctrl not pressed and object not in the list, add it
            // Make sure the printer is not selectable by mutliple selection
            if (selectedTransform.name != "printer" && (selectedObjects.Count == 0 || selectedObjects[0].transform.gameObject.name != "printer"))
            {
                AddSelectedObject(selectedTransform, false);
            }
        }
        else {
            // Ctrl not pressed and the object is in list
            audioSource.transform.position = selectedTransform.position;
            PlayAudio(selectClip);
        }
    }

    // Method to add a selected object to the list
    void AddSelectedObject(Transform selectedTransform, bool useIteratively) {
        Material origin;
        if (!useIteratively) { origin = originalMaterialHighlight; }
        else { origin = selectedTransform.GetComponent<MeshRenderer>().materials[1]; }

        bool isPositive = !selectedTransform.name.Contains("negative");
        bool isReference = selectedTransform.name.Contains("reference");
        // Apply the selected material to the selected object
        MeshRenderer renderer = selectedTransform.GetComponent<MeshRenderer>();
        if (renderer != null) {
            // In case we are selecting negative objects, have a special selection material for it
            if (!isPositive && !isReference)
            {
                isPositive = false;
                renderer.materials = negativeSelectionMatArray;
                if (!useIteratively)
                {
                    originalMaterialHighlight = negativeSelectionMaterial;
                    // Debug.Log("Original material: " + originalMaterialHighlight);
                }
            }
            // In case we are selecting a positive reference object, have a special selection material for it 
            else if (isPositive && isReference)
            {
                renderer.materials = referenceSelectionMatArray;
                if (!useIteratively)
                {
                    originalMaterialHighlight = referenceSelectionMaterial;
                    // Debug.Log("Original material: " + originalMaterialHighlight);
                }
            }
            // In case we are selecting a negative reference object, have a special selection material for it 
            else if (!isPositive && isReference)
            {
                renderer.materials = negativeReferenceSelectionMatArray;
                if (!useIteratively)
                {
                    originalMaterialHighlight = referenceSelectionMaterial;
                    // Debug.Log("Original material: " + originalMaterialHighlight);
                }
            }
            // In case we are selecting printer, have a special selection material for it 
            else if (selectedTransform.name == "printer")
            {
                renderer.materials = printerSelectionMatArray;
                if (!useIteratively)
                {
                    originalMaterialHighlight = printerSelectionMaterial;
                    // Debug.Log("Original material: " + originalMaterialHighlight);
                }
            }
            // Normal object
            else
            {
                renderer.materials = selectionMatArray;
                if (!useIteratively)
                {
                    originalMaterialHighlight = selectionMaterial;
                    // Debug.Log("Original material: " + originalMaterialHighlight);
                }
            }
        }

        ObjectMaterialPair pair = new ObjectMaterialPair(selectedTransform, origin, isPositive, isReference);
        selectedObjects.Add(pair);
        audioSource.transform.position = pair.transform.position;
        PlayAudio(selectClip);

        // If we have selected a printer, we make sure that the printer object can only be moved or rotated on Y axis
        bool printerNot;
        if (selectedTransform.name == "printer")
        {
            printerNot = false;
        }
        else 
        { 
            printerNot = true;
        }
        if (changePrinterSelected == printerNot)
        {
            objectManipulationParent.Find("Corner Button Parent").gameObject.SetActive(printerNot);
            objectManipulationParent.Find("Edge Button Parent").gameObject.SetActive(printerNot);
            objectManipulationParent.Find("Rotation Wheel Parent").gameObject.SetActive(printerNot);
            changePrinterSelected = !changePrinterSelected;
        }
    }

    // Method to remove a selected object from the list
    void RemoveSelectedObject(Transform selectedTransform) {
        ObjectMaterialPair pairToRemove = selectedObjects.Find(pair => pair.transform == selectedTransform);
        if (pairToRemove != null) {
            selectedObjects.Remove(pairToRemove);
            audioSource.transform.position = pairToRemove.transform.position;
            PlayAudio(deselectClip);

            // Revert the material of the deselected object
            MeshRenderer renderer = selectedTransform.GetComponent<MeshRenderer>();
            if (renderer != null) {
                Material[] defMatArray = new Material[2] { pairToRemove.originalMaterial, pairToRemove.originalMaterial };
                renderer.materials = defMatArray;
            }
            originalMaterialHighlight = pairToRemove.originalMaterial;
        }
    }

    // Method to check if a transform is in the list
    bool IsTransformInList(Transform transformToCheck) {
        foreach (ObjectMaterialPair pair in selectedObjects) {
            if (pair.transform == transformToCheck) {
                return true; // Transform is in the list
            }
        }
        return false; // Transform is not in the list
    }

    // Method that will deselect other transforms and only have the clicked transform selected
    void DeselectAndRemoveOthers(Transform selectedTransform) {
        // Check if the selected transform is in the list
        ObjectMaterialPair selectedPair = selectedObjects.Find(pair => pair.transform == selectedTransform);
        if (selectedPair == null) {
            AddSelectedObject(selectedTransform, false);
        }

        selectedPair = selectedObjects.Find(pair => pair.transform == selectedTransform);
        // Restore the original material of each selected object except the selected one
        foreach (ObjectMaterialPair pair in selectedObjects) {
            if (pair != selectedPair) {
                MeshRenderer renderer = pair.transform.GetComponent<MeshRenderer>();
                if (renderer != null) {
                    Material[] defMatArray = new Material[2] { pair.originalMaterial, pair.originalMaterial };
                    renderer.materials = defMatArray;
                }
            }
        }

        // Clear the list and add back the selected object
        selectedObjects.Clear();
        selectedObjects.Add(selectedPair);
    }

    void CallUpdateFunction()
    {
        /* We decided not to use reflection and instead use direct invocation for efficiency
        // Call the private function using Reflection, this function is in RectangularPrismCreator Script
        MethodInfo method = rectangularPrismCreatorReference.GetType().GetMethod("UpdateManipulatorLocation", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(rectangularPrismCreatorReference, null);
        }
        // Call the private function using Reflection, this function is in CubeHighlighter Script
        method = cubeHighlighterScriptReference.GetType().GetMethod("UpdateManipulationCube", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(cubeHighlighterScriptReference, null);
        }
        */
        // Call the public function in RectangularPrismCreator Script
        rectangularPrismCreatorReference.UpdateManipulatorLocation();
        cubeHighlighterScriptReference.UpdateManipulationCube();
    }

    public void DeleteObject()
    {
        foreach (ObjectMaterialPair pair in selectedObjects)
        {
            int ind = createdObjects.FindIndex(obj => obj.transform == pair.transform);
            if (ind != -1)
            {
                if (createdObjects[ind].name == "printer")
                {
                    printers.Remove(createdObjects[ind]);
                }
                // If the negatives hashset contains this gameobject, remove it from the set, since it will be destroyed
                if (negatives.Contains(createdObjects[ind].transform.gameObject))
                {
                    negatives.Remove(createdObjects[ind]);
                }
                // If the references hashset contains this gameobject, remove it from the set, since it will be destroyed
                if (references.Contains(createdObjects[ind].transform.gameObject))
                {
                    references.Remove(createdObjects[ind]);
                }

                Destroy(createdObjects[ind]);
                createdObjects.RemoveAt(ind);
            }
        }
        selectedObjects.Clear();
        oldSelectedObjects.Clear();
        selectedManipulationUI = false;
        ObjectGrabDetector._isGrabbingMovementHandle = false;
        ObjectGrabDetector._isGrabbingUIElement = false;
        RectangularPrismCreator.objectManipulationParent.SetActive(false);
    }

    public void DestroyAllObjects()
    {
        foreach (GameObject obj in createdObjects)
        {
            Destroy(obj);
        }
        printers.Clear();
        selectedObjects.Clear();
        oldSelectedObjects.Clear();
        createdObjects.Clear();
        selectedManipulationUI = false;
        ObjectGrabDetector._isGrabbingMovementHandle = false;
        ObjectGrabDetector._isGrabbingUIElement = false;
        RectangularPrismCreator.objectManipulationParent.SetActive(false);
    }

    public void PlayAudio(AudioClip clip)
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void AddSpecialObject()
    {
        GameObject special = Instantiate(specialObjectPrefab);
        special.name = "special";
        special.gameObject.SetActive(true);
        AddItem(special);
    }

    public void AddCube()
    {
        // Create cube
        GameObject cube = Instantiate(cubePrefab);
        cube.name = "cube";
        cube.gameObject.SetActive(true);
        AddItem(cube);
    }

    public void AddCylinder()
    {
        // Create cylinder
        GameObject cylinder = Instantiate(cylinderPrefab);
        cylinder.name = "cylinder";
        cylinder.gameObject.SetActive(true);
        AddItem(cylinder);
    }

    public void AddSphere()
    {
        // Create sphere
        GameObject sphere = Instantiate(spherePrefab);
        sphere.name = "sphere";
        sphere.gameObject.SetActive(true);
        AddItem(sphere);
    }

    public void AddCapsule()
    {
        // Create capsule
        GameObject capsule = Instantiate(capsulePrefab);
        capsule.name = "capsule";
        capsule.gameObject.SetActive(true);
        AddItem(capsule);
    }

    public void AddTriangularPrism()
    {
        // Create triangular prism
        GameObject triangularprism = Instantiate(triangularPrismPrefab);
        triangularprism.name = "triangularprism";
        triangularprism.gameObject.SetActive(true);
        AddItem(triangularprism);
    }

    public void AddPyramid()
    {
        // Create pyramid
        GameObject pyramid = Instantiate(pyramidPrefab);
        pyramid.name = "pyramid";
        pyramid.gameObject.SetActive(true);
        AddItem(pyramid);
    }

    public void AddCone()
    {
        // Create cone
        GameObject cone = Instantiate(conePrefab);
        cone.name = "cone";
        cone.gameObject.SetActive(true);
        AddItem(cone);
    }


    public void AddItem(GameObject item)
    {
        // Debug.Log("Creating item: " + item.name + " Total vertices: " + item.GetComponent<MeshFilter>().sharedMesh.vertexCount);

        // Every created object is put into the MRUK room with anchor so when the room is rotated for changing planes, the object's orientation do not change relative to the camera
        item.transform.SetParent(room.transform);
        MRUKAnchor itemAnchor = item.GetComponent<MRUKAnchor>();
        itemAnchor.Room = room;
        itemAnchor.ParentAnchor = room.transform.Find("FLOOR").GetComponent<MRUKAnchor>();

        // Creating the object above the palm of the left hand
        Vector3 pos = leftHand.transform.TransformPoint(new Vector3(1f, 1f, 0f) * creationDistance);
        item.transform.position = pos;
        float yscale = 0.1f;
        if (item.name == "cone") { yscale = 0.08f; }

        if (item.name != "printer")
        {
            item.transform.localScale = new Vector3(0.1f, yscale, 0.1f);
        }
        item.tag = "Selectable";
        createdObjects.Add(item);
        if (item.name != "printer")
        {
            if (carvingOn && !referenceOn) // Negative and normal
            {
                item.name += " (negative)";
                Material[] negativeMats = new Material[3];
                negativeMats[0] = negativeBaseMaterial; negativeMats[1] = negativeBaseMaterial; negativeMats[2] = negativeOuterMaterial;
                item.GetComponent<MeshRenderer>().materials = negativeMats;
                negatives.Add(item);
            }
            else if (!carvingOn && referenceOn) // Positive and reference
            {
                item.name += " (reference)";
                Material[] referenceMats = new Material[3];
                referenceMats[0] = referenceBaseMaterial; referenceMats[1] = referenceBaseMaterial; referenceMats[2] = referenceOuterMaterial;
                item.GetComponent<MeshRenderer>().materials = referenceMats;
                references.Add(item);
            }
            else if (carvingOn && referenceOn) // Negative and reference
            {
                item.name += " (negative reference)";
                Material[] referenceNegativeMats = new Material[3];
                referenceNegativeMats[0] = referenceBaseMaterial; referenceNegativeMats[1] = referenceBaseMaterial; referenceNegativeMats[2] = negativeReferenceOuterMaterial;
                item.GetComponent<MeshRenderer>().materials = referenceNegativeMats;
                references.Add(item);
                negatives.Add(item);
            }
        }
    }

    public void AddPrinter()
    {
        // Create printer area object
        GameObject printer = Instantiate(printerObject);
        printer.transform.SetParent(room.transform);
        printer.gameObject.SetActive(true);
        printer.name = "printer";
        AddItem(printer);
        printers.Add(printer);
        createdPrinter = printer;
    }

    // Function assigned to the solid/hole button, dictates the created objects being negative or positive
    public void CarvingButtonAction()
    {
        // Turn carving on and off
        carvingOn = !carvingOn;
        positiveToggleImage.enabled = !carvingOn;
        negativeToggleImage.enabled = carvingOn;
        carvingToggleText.text = carvingOn ? "hole" : "solid";
    }

    // Function assigned to the reference on/off button, dictates the created objects being reference or normal
    public void ReferenceButtonAction()
    {
        // Turn carving on and off
        referenceOn = !referenceOn;
        referenceOnToggleImage.enabled = referenceOn;
        referenceOffToggleImage.enabled = !referenceOn;
        referenceToggleText.text = referenceOn ? "reference" : "block";
    }

    // This is the function called when we press the "combine" button
    public void UniteSelected()
    {
        // Warning: In the case of all negative objects selected or negative objects removing all positive objects, gives out meshless output, while having the gameobject still in the scene 

        // If we have not selected anything, do not continue
        if (selectedObjects.Count <= 0) 
        {
            return;
        }

        bool allReference = true;
        // The goal of this first section is to get the combined bounds of the object in the x, y, and z axis
        int index = 0;
        float minOtherBoundsx = Mathf.Infinity, minOtherBoundsy = Mathf.Infinity, minOtherBoundsz = Mathf.Infinity;
        float maxOtherBoundsx = -Mathf.Infinity, maxOtherBoundsy = -Mathf.Infinity, maxOtherBoundsz = -Mathf.Infinity;

        for (int ind = 0; ind < selectedObjects.Count; ind++)
        {
            // If this selected object is not a negative (hole) object, take its bounds and if any of them extends the already set bounds (in world space), adjust it accordingly
            // Note: do not use MeshRenderer.bounds for these, it is absolutely busted for our situation
            if (!negatives.Contains(selectedObjects[ind].transform.gameObject))
            {
                Bounds bounds = selectedObjects[ind].transform.GetComponent<MeshCollider>().sharedMesh.bounds;
                bounds = selectedObjects[ind].transform.TransformBounds(bounds);
                maxOtherBoundsx = Mathf.Max(maxOtherBoundsx, bounds.max.x/2f);
                maxOtherBoundsy = Mathf.Max(maxOtherBoundsy, bounds.max.y/2f);
                maxOtherBoundsz = Mathf.Max(maxOtherBoundsz, bounds.max.z/2f);
                minOtherBoundsx = Mathf.Min(minOtherBoundsx, bounds.min.x/2f);
                minOtherBoundsy = Mathf.Min(minOtherBoundsy, bounds.min.y/2f);
                minOtherBoundsz = Mathf.Min(minOtherBoundsz, bounds.min.z/2f);
                index = ind; // In the end, the integer "index" becomes the last positive (solid) object's index in the selected objects array
            }
            // In case of any of the selected objects is a printer, exit the function without doing anything
            if (selectedObjects[ind].transform.gameObject.name == "printer")
            {
                return;
            }
            // If we come by a single non-reference object in the selecteds, the whole combination will be non-reference
            if (allReference && !references.Contains(selectedObjects[ind].transform.gameObject))
            {
                allReference = false;
            }
        }

        // The goal of the next section is to subtract negative mesh from positive mesh
        List<GameObject> selectedsList = new List<GameObject>();
        foreach (ObjectMaterialPair pair in selectedObjects)
        {
            selectedsList.Add(pair.transform.gameObject);
        }

        // Getting the bounds and the of the last selected positive object (here, selectedObjects[index] will become the combination of the objects)
        Bounds firstBounds = selectedObjects[index].transform.GetComponent<MeshCollider>().sharedMesh.bounds;
        firstBounds = selectedObjects[index].transform.TransformBounds(firstBounds);
        // Debug.Log("FirstBounds = " + firstBounds.min + " ____ " + firstBounds.max);
        Vector3 firstMin = (firstBounds.min) / 2f;
        Vector3 firstMax = (firstBounds.max) / 2f;
        Vector3 scale = selectedObjects[index].transform.localScale; 

        // Unite the objects
        this.uniteObjects(selectedsList, selectedObjects[index].transform.gameObject.GetComponent<MeshFilter>().mesh);

        // Added after we put the room rotation on all direncitonss
        selectedObjects[index].transform.localRotation = Quaternion.Inverse(room.transform.rotation);

        // Set the collider of the combined object according its mesh
        selectedObjects[index].transform.gameObject.GetComponent<MeshCollider>().sharedMesh = selectedObjects[index].transform.gameObject.GetComponent<MeshFilter>().mesh;

        // To get the bounds off the object, we fix the scale by dividing the older object's scale (new object's scale will be 1)
        Bounds secondBounds = selectedObjects[index].transform.GetComponent<MeshCollider>().sharedMesh.bounds;
        // Debug.Log("SecondBounds = " + secondBounds.min + " ____ " + secondBounds.max);
        Vector3 secondBoundsMin = new Vector3(
            secondBounds.min.x / scale.x, 
            secondBounds.min.y / scale.y, 
            secondBounds.min.z / scale.z);
        Vector3 secondBoundsMax = new Vector3(
            secondBounds.max.x / scale.x,
            secondBounds.max.y / scale.y,
            secondBounds.max.z / scale.z);
        secondBoundsMin = selectedObjects[index].transform.TransformPoint(secondBoundsMin);
        secondBoundsMax = selectedObjects[index].transform.TransformPoint(secondBoundsMax);
        Vector3 secondMin = (secondBoundsMin) / 2f;
        Vector3 secondMax = (secondBoundsMax) / 2f;

        // The object's name is now custom
        selectedObjects[index].transform.name = "custom";
        if (allReference) { selectedObjects[index].transform.name += " (reference)"; }
        else if (references.Contains(selectedObjects[index].transform.gameObject)) // Turn it back to normal object from reference object
        {
            references.Remove(selectedObjects[index].transform.gameObject);
            selectedObjects[index].referenceness = false;
        }

        // Debug.Log("FirstMin = " + firstMin + " SecondMin = " + secondMin + " DiffMin = " + (secondMin - firstMin));
        // Debug.Log("FirstMax = " + firstMax + " SecondMax = " + secondMax + " DiffMax = " + (secondMax - firstMax));
        // Debug.Log("MinOtherBoundsX = " + minOtherBoundsx + " firstMinX = " + firstMin.x + " MaxOtherBoundsX = " + maxOtherBoundsx + " firstMaxX = " + firstMax.x);


        // This is the section where we relocate the position of the combined object so it perfectly sits on the position that is combined previously
        // The logic goes like this: ultimately we are taking the last positive object's position and shift it so that the new center of this combined object alignes with the object
        Vector3 diffVec = Vector3.zero;
        // If the initial object is bounded by the other objects to combine in x direction
        if (minOtherBoundsx < firstMin.x && maxOtherBoundsx > firstMax.x)
        {
            diffVec.x = -(maxOtherBoundsx + minOtherBoundsx - firstMax.x - firstMin.x) / 2f;
            // Debug.Log("Bounded by the other, not moving x... " + diffVec.x);
        }
        else if (minOtherBoundsx >= firstMin.x && maxOtherBoundsx >= firstMax.x)
        {
            diffVec.x = secondMin.x - firstMin.x;
            // Debug.Log("Going left on x by " + diffVec.x);
            // Debug.Log("SecondMin " + secondMin.x + " FirstMin " + firstMin.x);
            // Debug.Log("SecondMax " + secondMax.x + " FirstMax " + firstMax.x);
        }
        else if (minOtherBoundsx <= firstMin.x && maxOtherBoundsx <= firstMax.x)
        {
            diffVec.x = secondMax.x - firstMax.x;
            // Debug.Log("Going right on x by " + diffVec.x);
        }
        if (minOtherBoundsy < firstMin.y && maxOtherBoundsy > firstMax.y)
        {
            diffVec.y = -(maxOtherBoundsy + minOtherBoundsy - firstMax.y - firstMin.y) / 2f;
            // Debug.Log("Bounded by the other, not moving y... " + diffVec.y);
        }
        else if (minOtherBoundsy >= firstMin.y && maxOtherBoundsy >= firstMax.y)
        {
            diffVec.y = secondMin.y - firstMin.y;
            // Debug.Log("Going down on y by " + diffVec.y);
        }
        else if (minOtherBoundsy <= firstMin.y && maxOtherBoundsy <= firstMax.y)
        {
            diffVec.y = secondMax.y - firstMax.y;
            // Debug.Log("Going up on y by " + diffVec.y);
        }
        if (minOtherBoundsz < firstMin.z && maxOtherBoundsz > firstMax.z)
        {
            diffVec.z = -(maxOtherBoundsz + minOtherBoundsz - firstMax.z - firstMin.z)/2f;
            // Debug.Log("Bounded by the other, not moving z... " + diffVec.z);
        }
        else if (minOtherBoundsz >= firstMin.z && maxOtherBoundsz >= firstMax.z)
        {
            diffVec.z = secondMin.z - firstMin.z;
            // Debug.LogError("Going backward on z by " + diffVec.z);
        }
        else if (minOtherBoundsz <= firstMin.z && maxOtherBoundsz <= firstMax.z)
        {
            diffVec.z = secondMax.z - firstMax.z;
            // Debug.LogError("Going forward on z by " + diffVec.z);
        }
        // selectedObjects[index].transform.GetComponent<MRUKAnchor>().ParentAnchor = room.transform.Find("printer").GetComponent<MRUKAnchor>();
        selectedObjects[index].transform.position -= (diffVec * 2f);
        selectedObjects[index].transform.localScale = new Vector3(1f, 1f, 1f);

        // selectedObjects[index].transform.Rotate(0, 180, 0); 

        RemoveSelectedObject(selectedObjects[index].transform); // Remvoes the first object from selecteds list
        DeleteObject(); // Deletes all selected objects, only the remaining combined object is not deleted
    }

    void uniteObjects(List<GameObject> objectList, Mesh result)
    {
        CSGBrush resultBrush = new CSGBrush("resultBrushObj");

        if (objectList.Count > 0)
        {
            int count = 0;
            // First, combine all of the positive objects into a single mesh
            for (int i = 0; i < objectList.Count; i++)
            {
                if (objectList[i].name == "printer")
                {
                    continue;
                }
                if (!negatives.Contains(objectList[i]))
                {
                    CSGBrush objectBrush = new CSGBrush(objectList[i]);
                    objectBrush.build_from_mesh(objectList[i].GetComponent<MeshFilter>().mesh);
                    CSGOp.merge_brushes(Operation.OPERATION_UNION, resultBrush, objectBrush, ref resultBrush);
                    count++;
                }
            }
            // Then, combine all of the negative objects with the combined positive object
            for (int i = 0; i < objectList.Count; i++)
            {
                if (objectList[i].name == "printer")
                {
                    continue;
                }
                if (negatives.Contains(objectList[i]))
                {
                    CSGBrush objectBrush = new CSGBrush(objectList[i]);
                    objectBrush.build_from_mesh(objectList[i].GetComponent<MeshFilter>().mesh);
                    CSGOp.merge_brushes(Operation.OPERATION_SUBTRACTION, resultBrush, objectBrush, ref resultBrush);
                    count++;
                }
            }
        }
        resultBrush.getMeshNormal(result);
    }

    void ConvertToSTL(PrinterProperties printerObject = null)
    {
        // Subtract negative mesh from positive mesh
        Mesh target = new Mesh();
        List <GameObject> selectedsList = new List<GameObject>();
        foreach (ObjectMaterialPair pair in selectedObjects)
        {
            selectedsList.Add(pair.transform.gameObject);
        }
        
        // Put all the selected objects into their original positions
        for (int i = 0; i < selectedObjects.Count; i++)
        {
            selectedObjects[i].transform.position = originalObjectPositions[i];
        }
        CallUpdateFunction();

        this.uniteObjects(selectedsList, target);

        if (printerObject != null)
        {
            if (printerObject.printerObjectSentRequest) 
            {
                return;
            }

            Vector3 sub = (printerObject.gameObject.GetComponent<MeshRenderer>().bounds.size / enlargeScale) - target.bounds.size;

            // If the selected objects are larer than the printable area, return
            TextMeshProUGUI infoText = printerObject.transform.GetChild(1).Find("Information Text").GetComponent<TextMeshProUGUI>();
            // To make sure the courotines do not overlap
            if (fadeTextCoroutine != null)
            {
                StopCoroutine(fadeTextCoroutine);
            }
            if (sub.x < 0 || sub.y < 0 || sub.z < 0)
            {
                Debug.LogWarning("Objects size is larger than printer size!");
                // Show the error info on top of the printer object
                infoText.text = "too large for printer";
                infoText.color = Color.red;
                fadeTextCoroutine = StartCoroutine(DisplayAndFadeText(infoText));
                return;
            }
            else
            {
                infoText.text = "model placed successfully";
                infoText.color = Color.green;
                fadeTextCoroutine = StartCoroutine(DisplayAndFadeText(infoText));
            }

            // Putting clone of the merged objects to preview in the printer area
            int notNeg = 0;
            for (notNeg = 0; notNeg < selectedsList.Count(); notNeg++)
            {
                Material mat = selectedsList[notNeg].GetComponent<MeshRenderer>().materials[1];
                if (!(mat.name == "ProHover Selected" || mat.name == "ProHover Selected (Instance)"))
                {
                    break;
                }
            }
            if (notNeg == selectedsList.Count())
            {
                Debug.LogError("No valid object found to put into print!");
                return;
            }

            GameObject newObject = Instantiate(selectedsList[notNeg].transform.gameObject);
            newObject.GetComponent<MeshFilter>().mesh = target;
            // Note: we used to mark the object as "unselectable" since we did not want it to be movable
            //newObject.GetComponent<MeshCollider>().enabled = false; 
            //newObject.tag = "Untagged";
            //Destroy(newObject.transform.GetChild(0).gameObject);
            newObject.transform.localScale = new Vector3(1f, 1f, 1f);
            newObject.transform.localScale *= enlargeScale;
            newObject.transform.position = printerObject.transform.position;
            newObject.transform.position -= new Vector3(0, newObject.GetComponent<MeshRenderer>().bounds.min.y - printerObject.GetComponent<MeshRenderer>().bounds.min.y, 0);

            foreach (Transform trans in printerObject.transform)
            {
                if(trans.name != "Collider" && trans.name != "Canvas" && trans.name != "Printer Name Canvas" && trans.name != "Plate Plane" && trans.name != "Buttons Menu")
                {
                    Destroy(trans.gameObject);                
                }
            }

            newObject.transform.SetParent(printerObject.transform);

            printerObject.ProcessMesh(target);

            // Get the MeshFilter (which holds the mesh used by the MeshRenderer)
            MeshFilter meshFilter = newObject.GetComponent<MeshFilter>();
            MeshCollider meshCollider = newObject.GetComponent<MeshCollider>();

            if (meshFilter != null && meshCollider != null)
            {
                // Assign the mesh from the MeshFilter to the MeshCollider
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }
        else
        {
            Debug.LogError("Printer is NULL!");
        }
    }

    private IEnumerator DisplayAndFadeText(TextMeshProUGUI uiText)
    {
        // Ensure the text is fully visible initially
        SetAlpha(1f, uiText);

        // Wait for the visible duration
        yield return new WaitForSeconds(visibleInfoTextDuration);

        // Start fading out the text
        float elapsedTime = 0f;
        while (elapsedTime < fadeInfoTextDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInfoTextDuration);
            SetAlpha(alpha, uiText);
            yield return null;
        }

        // Ensure the text is fully faded out
        SetAlpha(0f, uiText);
    }

    private void SetAlpha(float alpha, TextMeshProUGUI uiText)
    {
        if (uiText != null)
        {
            Color color = uiText.color;
            color.a = alpha;
            uiText.color = color;
        }
    }

    public void SaveGameObject()
    {
        String fileName = Path.Combine(Application.persistentDataPath, "TableCADSave.dat");

        using (FileStream fileStream = File.Open(fileName, FileMode.Create))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            for (int i = 0; i < createdObjects.Count; i++)
            {
                _GameObjectData gameObjectData = new _GameObjectData(createdObjects[i]);
                formatter.Serialize(fileStream, gameObjectData);
            }
        }

        Debug.Log("Saved GameObject data to: " + fileName);
    }


    public void LoadGameObject()
    {
        String fileName = Path.Combine(Application.persistentDataPath, "TableCADSave.dat");

        if (File.Exists(fileName))
        {
            using (FileStream fileStream = File.Open(fileName, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();

                while (fileStream.Position < fileStream.Length)
                {
                    _GameObjectData gameObjectData = (_GameObjectData)formatter.Deserialize(fileStream);
                    GameObject loadedObject;

                    // Instantiate objects based on typeName
                    switch (gameObjectData.typeName)
                    {
                        case "sphere":
                            loadedObject = Instantiate(spherePrefab);
                            break;
                        case "cube":
                            loadedObject = Instantiate(cubePrefab);
                            break;
                        case "cylinder":
                            loadedObject = Instantiate(cylinderPrefab);
                            break;
                        case "capsule":
                            loadedObject = Instantiate(capsulePrefab);
                            break;
                        case "triangularprism":
                            loadedObject = Instantiate(triangularPrismPrefab);
                            break;
                        case "pyramid":
                            loadedObject = Instantiate(pyramidPrefab);
                            break;
                        case "cone":
                            loadedObject = Instantiate(conePrefab);
                            break;
                        case "printer":
                            loadedObject = Instantiate(printerObject);
                            break;
                        case "custom":
                            loadedObject = Instantiate(cubePrefab);
                            loadedObject.GetComponent<MeshFilter>().mesh = gameObjectData.customMesh.GetMesh();
                            loadedObject.GetComponent<MeshCollider>().sharedMesh = loadedObject.GetComponent<MeshFilter>().mesh;
                            break;
                        default:
                            throw new InvalidTypeError();
                    }

                    // Set object properties
                    loadedObject.SetActive(true);
                    loadedObject.name = gameObjectData.typeName;
                    loadedObject.transform.position = gameObjectData.position.ToVector3();
                    loadedObject.transform.rotation = gameObjectData.rotation.ToQuaternion();
                    loadedObject.transform.localScale = gameObjectData.scale.ToVector3();

                    if (loadedObject.name == "printer")
                    {
                        printers.Add(loadedObject);
                    }

                    loadedObject.tag = "Selectable";
                    createdObjects.Add(loadedObject);
                }
            }

            Debug.Log("Loaded GameObject data from: " + fileName);
        }
        else
        {
            Debug.Log("No save file found at: " + fileName);
        }
    }

    public void ExportModel()
    {
        String fileName = "ExportedSTLModel.stl";
        String filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Subtract negative mesh from positive mesh
        Mesh target = new Mesh();
        this.uniteObjects(createdObjects, target);

        // Save as STL
        StringBuilder stlData = PrinterProperties.ExportMeshAsSTL(target);
        using (StreamWriter sw = new StreamWriter(filePath))
        {
            sw.Write(stlData.ToString().Replace(",", "."));
        }
        Debug.Log("STL file saved at: " + filePath);
        debugText.text = "STL file saved at: " + filePath;
    }


    public void RestartScene()
    {
        DeselectAllObjects();

        MRUK.Instance.GetCurrentRoom().transform.rotation = Quaternion.Euler(0, 0, 0);

        // Get the name of the current active scene
        string sceneName = SceneManager.GetActiveScene().name;
        // Load the scene with the same name
        SceneManager.LoadScene(sceneName);
    }

    public void ChangeColorShader()
    {
        DeselectAllObjects();

        // Iterate over the array of materials
        for (int i = 0; i < materials.Length; i++)
        {
            // Check if the material is using the lit shader and switch to unlit, or vice versa
            if (colorLit)
            {
                materials[i].shader = unlitShader;
            }
            else
            {
                materials[i].shader = litShader;
            }
        }

        // Change it for the created objects since they may carry instances of the materials rather their original
        for (int i = 0; i < createdObjects.Count(); i++) 
        {
            if (createdObjects[i].name != "printer")
            {
                for (int j = 0; j < 2; j++)
                {
                    Material materialInstance = createdObjects[i].GetComponent<MeshRenderer>().sharedMaterials[j];
                    // If its an instance instead of the original version
                    if (materialInstance.name.Contains("Instance") && !materialInstance.name.Contains("ProHover"))
                    {
                        // Check if the material is using the lit shader and switch to unlit, or vice versa
                        if (colorLit)
                        {
                            materialInstance.shader = unlitShader;
                        }
                        else
                        {
                            materialInstance.shader = litShader;
                        }
                        break;
                    }
                }                
            }
        }
        colorLit = !colorLit;
    }
}
