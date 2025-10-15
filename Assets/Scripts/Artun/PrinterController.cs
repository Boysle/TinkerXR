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
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.IO;
using System.Text;
using System.Collections;
using SimpleFileBrowser;
using TMPro;
using System;

[Serializable]
public class WaitingJsonClass
{
    public DateTime datetime_cleaned;
    public DateTime datetime_finished;
    public DateTime datetime_started;
    public string name;
    public string pause_source;
    public float progress;
    public string reprint_original_uuid;
    public string result;
    public string source;
    public string source_application;
    public string source_user;
    public string state;
    public int time_elapsed;
    public int time_total;
    public string uuid;
}

[Serializable]
public class SlicerClass
{
    public string gcodePath;
    public string gcode;
}

// Fully functional Unity component to handle STL processing, sending to a printer server, and monitoring printer state.

public class PrinterController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Transform printer;
    public float enlargeScale = 1.5f;
    public TMP_InputField IP_field;
    public TMP_InputField printerIP_field;
    public Mesh meshToSave = null;
    private Vector3 originalScale;
    private bool mouseOver = false;
    public static PrinterController instance = null;

    public Button continueButton;
    public Button abortButton;
    public Button pauseButton;

    void Awake(){
        if (instance != null && instance != this)
            Destroy(gameObject);    // Ensures that there aren't multiple Singletons

        instance = this;
    }

    void Start()
    {
        originalScale = printer.localScale;
        continueButton.onClick.AddListener(continueFnc);
        pauseButton.onClick.AddListener(pauseFnc);
        abortButton.onClick.AddListener(abortFnc);
    }

    void continueFnc(){
        string IP = IP_field.text.ToString();
        string printerIP = printerIP_field.text.ToString();
        StartCoroutine(StatePrinterStatus("continue", IP, printerIP));
    }

    void pauseFnc(){
        string IP = IP_field.text.ToString();
        string printerIP = printerIP_field.text.ToString();
        StartCoroutine(StatePrinterStatus("pause", IP, printerIP));
    }

    void abortFnc(){
        string IP = IP_field.text.ToString();
        string printerIP = printerIP_field.text.ToString();
        StartCoroutine(StatePrinterStatus("stop", IP, printerIP));
    }

    public IEnumerator StatePrinterStatus(string status, string serverUrl, string printerIP){
        string p_json = $"{{\"state\": \"{status}\", \"printerIP\": \"{printerIP}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(p_json);
        using (UnityWebRequest p_request = new UnityWebRequest($"{serverUrl}/print", "PUT"))
        {
            p_request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            p_request.downloadHandler = new DownloadHandlerBuffer();
            p_request.SetRequestHeader("Content-Type", "application/json");

            yield return p_request.SendWebRequest();

            if (p_request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {p_request.error}");
                yield break;
            }
            string p_response = p_request.downloadHandler.text;
            Debug.Log($"Printer State Response: {p_response}");
        }
    }

    public void ProcessMesh(Mesh prefab)
    {
        string IP = IP_field.text.ToString();
        meshToSave = prefab;
        if(string.IsNullOrEmpty(IP)){
            // Process the prefab (e.g., instantiate it or send it to a printer)
            FileBrowser.SetFilters( false, new FileBrowser.Filter( "STL", ".stl" ) );
            FileBrowser.ShowSaveDialog( meshProcessOfflineOK, null, FileBrowser.PickMode.Files, false, null, null, "Save As", "Save" );
        } else{
            StringBuilder stlData = ExportMeshAsSTL(meshToSave);
            StartCoroutine(SendSTLData(stlData.ToString(), IP));
            Debug.Log("IP test " + IP);
        }
    }

    public void SetMouseOver(bool value)
    {
        mouseOver = value;
    }

    public bool IsMouseOver()
    {
        return mouseOver;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetMouseOver(true);
        // Enlarge the printer icon when a draggable prefab enters its trigger area
        printer.localScale = originalScale * enlargeScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetMouseOver(false);
        // Reset the printer icon scale when the draggable prefab exits its trigger area
        printer.localScale = originalScale;
    }

    void meshProcessOfflineOK(string[] filePaths){
        if(meshToSave!=null){
            StringBuilder stlData = ExportMeshAsSTL(meshToSave);
            using (StreamWriter sw = new StreamWriter(filePaths[0]))
            {
                sw.Write(stlData.ToString());
            }
            Debug.Log("STL file saved at: " + filePaths[0]);
        }
        else{
            Debug.Log("meshToSave is NULL!");
        }
    }
    
    public StringBuilder ExportMeshAsSTL(Mesh mesh)
    {
        StringBuilder stlData = new StringBuilder();
        stlData.Append("solid UnityMesh\n");

        float scaleFactor = 20f; // Adjust this as needed
        Quaternion rotation = Quaternion.Euler(0, 90, 0); // Rotate by 90 degrees around the y-axis

        float minY = Vector3.positiveInfinity.y;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            float v = transform.TransformPoint(rotation * mesh.vertices[i] * scaleFactor).y;
            minY = Mathf.Min(minY, v);
        }

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            Vector3 v1 = transform.TransformPoint(rotation * (mesh.vertices[mesh.triangles[i + 0]] * scaleFactor));
            Vector3 v2 = transform.TransformPoint(rotation * (mesh.vertices[mesh.triangles[i + 1]] * scaleFactor));
            Vector3 v3 = transform.TransformPoint(rotation * (mesh.vertices[mesh.triangles[i + 2]] * scaleFactor));

            v1.y -= minY;
            v2.y -= minY;
            v3.y -= minY;

            Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            stlData.Append("facet normal " + normal.x + " " + normal.y + " " + normal.z + "\n");
            stlData.Append("    outer loop\n");
            stlData.Append("        vertex " + v1.x + " " + v1.y + " " + v1.z + "\n");
            stlData.Append("        vertex " + v2.x + " " + v2.y + " " + v2.z + "\n");
            stlData.Append("        vertex " + v3.x + " " + v3.y + " " + v3.z + "\n");
            stlData.Append("    endloop\n");
            stlData.Append("endfacet\n");
        }

        stlData.Append("endsolid UnityMesh\n");

        return stlData;
    }

    private IEnumerator SendSTLData(string stlData, string serverUrl)
    {
        // Create a JSON object with the stlFileData
        string json = $"{{\"modelName\": \"model\", \"stlFileData\": \"{System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stlData))}\"}}";

        // Convert the JSON string to a byte array
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        Debug.Log($"{serverUrl}/slice");

        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/slice", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
                yield break;
            }

            string response = request.downloadHandler.text;
            Debug.Log($"Response: {response}");

            
            string printerIP = printerIP_field.text.ToString();
            if(!string.IsNullOrEmpty(printerIP)){
                Debug.Log("Waiting for printer's response. You might need to authenticate the server from printer: " + printerIP);
                string p_json = $"{{\"modelName\": \"model\", \"printerIP\": \"{printerIP}\"}}";
                // Convert the JSON string to a byte array
                byte[] p_bodyRaw = Encoding.UTF8.GetBytes(p_json);
                using (UnityWebRequest p_request = new UnityWebRequest($"{serverUrl}/print", "POST"))
                {
                    p_request.uploadHandler = new UploadHandlerRaw(p_bodyRaw);
                    p_request.downloadHandler = new DownloadHandlerBuffer();
                    p_request.SetRequestHeader("Content-Type", "application/json");

                    yield return p_request.SendWebRequest();

                    if (p_request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Printer Error: {p_request.error}");
                        Debug.Log($"You may need to confirm removal");
                        yield break;
                    }

                    string p_response = p_request.downloadHandler.text;
                    Debug.Log($"Printer Response: {p_response}");
                    
                    StartCoroutine(getPrinterStatus(serverUrl, printerIP));
                }
            }
        }
    }

    public IEnumerator getPrinterStatus(string serverUrl, string printerUrl){
        while (true){
            yield return new WaitForSeconds(5);
            using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/print?printerIP={printerUrl}", "GET")){
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();
                string response = request.downloadHandler.text;
                Debug.Log($"Wait Response: {response}");
                WaitingJsonClass json = JsonUtility.FromJson<WaitingJsonClass>(response);
                Debug.Log(json.progress);
            }
        }
    }
}