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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using SimpleFileBrowser;
using TMPro;
using LibCSG;
using UnityEngine.ProBuilder.Shapes;

public class InvalidTypeError: Exception{};

[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
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
public class SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(Quaternion quaternion)
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
public class GameObjectData
{
    public string typeName;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public SerializableVector3 scale;
    public SerializableMeshInfo customMesh;

    public GameObjectData(GameObject obj)
    {
        typeName = obj.name;
        if (typeName == "custom"){
            customMesh = new SerializableMeshInfo(obj.GetComponent<MeshFilter>().mesh);
        }
        position = new SerializableVector3(obj.transform.position);
        rotation = new SerializableQuaternion(obj.transform.rotation);
        scale = new SerializableVector3(obj.transform.localScale);
    }

    public GameObject CreateGameObject()
    {
        GameObject obj;
        if (typeName=="sphere"){
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        } else if(typeName=="cube"){
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }else if (typeName == "custom"){
            obj = new GameObject();
            obj.AddComponent<MeshFilter>().mesh = customMesh.GetMesh();
        } else {
            throw new InvalidTypeError();
        }
        obj.name = typeName;
        obj.transform.position = position.ToVector3();
        obj.transform.rotation = rotation.ToQuaternion();
        obj.transform.localScale = scale.ToVector3();
        return obj;
    }
}


public class ModelCreator : MonoBehaviour
{
    public Button deleteButton;
    public Button loadButton;
    public Button saveButton;
    public Button convertButton;
    public Button addSphereButton;
    public Button addCubeButton;
    public Button addPrinterButton;
    public GameObject printerObject;
    public Button negativeButton;
    public TMP_Text negativeText;
    public GameObject resultObject;
    private bool isNegative = false;

    private List<GameObject> createdObjects;
    private HashSet<GameObject> printers;
    private HashSet<GameObject> negatives;
    private CSGBrushOperation CSGOp;
    private float distance = 10;

    //private string saveFilePath = "SaveData.dat";
    //private string stlPath = "CombinedModel.stl";

    
    public Material highlightMaterial;
    public Material selectionMaterial;

    public static ModelCreator instance = null;
    void Awake(){
        if (instance != null && instance != this)
            Destroy(gameObject);    // Ensures that there aren't multiple Singletons

        instance = this;
    }

    void Start()
    {
        // Button click event
        deleteButton.onClick.AddListener(DeleteObject);
        loadButton.onClick.AddListener(LoadGameObject);
        saveButton.onClick.AddListener(SaveGameObject);
        convertButton.onClick.AddListener(ConvertToSTLBasic);
        addSphereButton.onClick.AddListener(AddSphere);
        addCubeButton.onClick.AddListener(AddCube);
        addPrinterButton.onClick.AddListener(AddPrinter);
        negativeButton.onClick.AddListener(ToggleNegative);

        createdObjects = new List<GameObject>();
        printers = new HashSet<GameObject>();
        negatives = new HashSet<GameObject>();
        CSGOp = new CSGBrushOperation();
    }

    void ToggleNegative(){
        isNegative = !isNegative;
        negativeText.SetText(isNegative ? "-" : "+");
    }

    void Update(){
        // Should check if selected something and draging
        // Printer objects should not be selected in multi select mode
        // And should not trigger other printers.
        //Debug.Log("ok");
        float enlargeScale = 1.5f;
        bool isDragging = true;
        bool mouseClickUp = Input.GetMouseButtonUp(0);


        if(isDragging || mouseClickUp){
            foreach(GameObject p in printers){
                PrinterProperties pp = p.GetComponent<PrinterProperties>();
                if (pp.enlarged){
                    pp.enlarged = false;
                    p.transform.localScale = pp.originalScale;
                }
            }
        }

        if(isDragging){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; i++){
                GameObject obj = hits[i].collider.gameObject;
                if (obj.name == "printer"){
                    PrinterProperties pp = obj.GetComponent<PrinterProperties>();
                    if(!pp.enlarged){
                        pp.enlarged = true;
                        pp.originalScale = obj.transform.localScale;
                        obj.transform.localScale *= enlargeScale;
                    }
                }
            }
        }

        if(mouseClickUp){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; i++){
                GameObject obj = hits[i].collider.gameObject;
                if (obj.name == "printer"){
                    ConvertToSTL(obj.GetComponent<PrinterProperties>());
                }
            }
        }
    }

    public void DeleteObject(){
        int ind = createdObjects.FindIndex(obj=>obj.transform==Selection.instance.selection);
        if(ind!=-1){
            Selection.instance.selection = null;
            if(createdObjects[ind].name=="printer"){
                printers.Remove(createdObjects[ind]);
            }
            Destroy(createdObjects[ind]);
            createdObjects.RemoveAt(ind);
        }
    }

    public void SaveGameObject()
    {
        FileBrowser.SetFilters( false, new FileBrowser.Filter( "Save File", ".dat" ) );
        FileBrowser.ShowSaveDialog( SaveGameObjectOK, null, FileBrowser.PickMode.Files, false, null, null, "Save As", "Save" );
    }
    public void SaveGameObjectOK(string[] filePaths)
    {
        FileStream fileStream = File.Open(filePaths[0], FileMode.Create);
        BinaryFormatter formatter = new BinaryFormatter();
        for(int i=0; i<createdObjects.Count; i++){
            GameObjectData gameObjectData = new GameObjectData(createdObjects[i]);
            formatter.Serialize(fileStream, gameObjectData);
        }
        fileStream.Close();
        Debug.Log("Saved GameObject data to: " + filePaths[0]);
    }

    public void LoadGameObject()
    {
        FileBrowser.SetFilters( false, new FileBrowser.Filter( "Save File", ".dat" ) );
        FileBrowser.ShowLoadDialog( LoadGameObjectOK, null, FileBrowser.PickMode.Files, false, null, null, "Load File", "Load" );
    }
    public void LoadGameObjectOK(string[] filePaths)
    {
        if (File.Exists(filePaths[0]))
        {
            FileStream fileStream = File.Open(filePaths[0], FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();
            while(fileStream.Position < fileStream.Length){
                GameObjectData gameObjectData = (GameObjectData)formatter.Deserialize(fileStream);
                GameObject loadedObject = gameObjectData.CreateGameObject();
                loadedObject.tag = "Selectable";
                if(loadedObject.name=="printer"){
                    loadedObject.AddComponent<PrinterProperties>();
                    printers.Add(loadedObject);
                }
                createdObjects.Add(loadedObject);
            }
            fileStream.Close();
            Debug.Log("Loaded GameObject data from: " + filePaths[0]);
        }
        else
        {
            Debug.Log("No save file found at: " + filePaths[0]);
        }
    }

    void AddSphere(){
        // Create sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "sphere";
        AddItem(sphere);
    }

    void AddCube(){
        // Create cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "cube";
        AddItem(cube);
    }

    void AddPrinter(){
        // Create cube
        GameObject printer = Instantiate(printerObject);
        printer.name = "printer";
        AddItem(printer);
        printer.AddComponent<PrinterProperties>();
        printers.Add(printer);
    }

    void AddItem(GameObject item){
        Vector3 pos = Camera.main.transform.TransformPoint(Vector3.forward * distance);
        item.transform.position = pos;
        item.AddComponent<DragAndDrop>();
        item.tag = "Selectable";
        createdObjects.Add(item);
        Debug.Log(isNegative);
        if(isNegative && item.name != "printer"){
            Debug.Log("onNegative");
            item.GetComponent<Renderer>().material.color = new Color(0, 204, 102);
            negatives.Add(item);
        }
    }

    void ConvertToSTLBasic(){
        this.uniteObjects(createdObjects, resultObject.GetComponent<MeshFilter>().mesh);
        Bounds bound = resultObject.GetComponent<MeshFilter>().mesh.bounds;
        resultObject.GetComponent<BoxCollider>().size = bound.size;
        resultObject.GetComponent<BoxCollider>().center = bound.center;
    }

    void uniteObjects(List<GameObject> objectList, Mesh result){
        CSGBrush resultBrush = new CSGBrush("resultBrushObj");

        if (objectList.Count > 0)
        {
            int count = 0;
            // Combine cube and sphere into a single mesh
            for (int i = 0; i < objectList.Count; i++)
            {
                if (!negatives.Contains(objectList[i]))
                {
                    CSGBrush objectBrush = new CSGBrush(objectList[i]);
                    objectBrush.build_from_mesh(objectList[i].GetComponent<MeshFilter>().mesh);
                    CSGOp.merge_brushes(Operation.OPERATION_UNION, resultBrush, objectBrush, ref resultBrush);
                    count++;
                }
            }
            for (int i = 0; i < objectList.Count; i++)
            {
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
        this.uniteObjects(createdObjects, target);

        if (printerObject != null){
            printerObject.ProcessMesh(target);
        } else{
            Debug.Log("Printer is NULL!");
        }
    }
}
