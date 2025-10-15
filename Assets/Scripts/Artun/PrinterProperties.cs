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
using TMPro;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using Unity.VisualScripting;
using System.Globalization;
using System;

public enum PRINTER_STATUS
{
    CONTINUE,
    PAUSE,
    STOP
};

[Serializable]
public class SliceOptionsClass
{
    public string name;
    public string label;
    //public string default;
    public string[] values;
}

[Serializable]
public class SlicersClass
{
    public string name;
    public string label;
    public SliceOptionsClass[] options;
}

// Feature-rich Unity component for handling 3D printing workflows, including mesh processing,
// slicing, sending STL data to a printer server, parsing G-code, and rendering extrusion paths.

public class PrinterProperties : MonoBehaviour
{
    public bool enlarged = false;
    public Vector3 originalScale = new Vector3(1, 1, 1);
    public Mesh meshToSave = null;

    public TMP_InputField printerIP_field;
    public TMP_InputField serverIP_field;
    public Slider progressBar;
    public TextMeshProUGUI progressPercentText;
    public TextMeshProUGUI infoText;
    public Canvas canvas;
    public bool printing = false;
    public bool printerObjectSentRequest = false;
    public Image startPrintImg, pausePrintImg, resumePrintImg, slicerSettingsImg, abortPrintImg;
    public TextMeshProUGUI startButtonText, slicerButtonText;
    bool startedPrinting = false, paused = false;

    void Start()
    {
        /*
        foreach (Canvas c in GetComponentsInChildren<Canvas>())
        {
            if (c.name == "Canvas")
            {
                canvas = c;
            }
        }
        foreach (TMP_InputField field in canvas.GetComponentsInChildren<TMP_InputField>())
        {
            if (field.gameObject.name == "PrinterIP")
            {
                printerIP_field = field;
            }
            else if (field.gameObject.name == "SeverIP")
            {
                serverIP_field = field;
            }
        }
        */

        printing = false;
        printerObjectSentRequest = false;
    }

    void Update()
    {
        Vector3 targetPostition = new Vector3(Camera.main.transform.position.x, canvas.transform.position.y, Camera.main.transform.position.z);
        canvas.transform.LookAt(targetPostition, Vector3.up);
        canvas.transform.Rotate(0, 180f, 0);
    }

    public void getSlicers()
    {
        string serverIP = serverIP_field.text.ToString();
        StartCoroutine(__getSlicers(serverIP));
    }

    public IEnumerator __getSlicers(string serverUrl)
    {
        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/slice", "GET"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            string response = request.downloadHandler.text;
            Debug.Log($"Wait Response: {response}");
            SlicersClass[] json = JsonUtility.FromJson<SlicersClass[]>(response);
        }
    }


    public void ProcessMesh(Mesh prefab)
    {
        meshToSave = prefab;

        string serverIP = serverIP_field.text.ToString();
        string stlData = ExportMeshAsSTL(meshToSave).ToString().Replace(",", ".");

        if (string.IsNullOrEmpty(serverIP))
        {
            using (StreamWriter sw = new StreamWriter("model.stl"))
            {
                sw.Write(stlData);
            }
            Debug.Log("STL file saved at: " + "model.stl");
        }
        else
        {
            printerObjectSentRequest = true;

            string printer = "ultimaker_s3";
            string options = "{\"support_enable\": true, \"support_structure\": \"tree\", \"layer_height\": 0.2, \"infill_sparse_density\": 50, \"infill_pattern\": \"grid\"}";

            StartCoroutine(SendSTLData(stlData, serverIP, printer, options));
            Debug.Log("IP test " + serverIP);
        }
    }

    public void printModel()
    {
        //StartCoroutine(StartPrintProc(serverIP));
    }

    void meshProcessOfflineOK(string[] filePaths)
    {
        if (meshToSave != null)
        {
            string stlData = ExportMeshAsSTL(meshToSave).ToString().Replace(",", ".");
            using (StreamWriter sw = new StreamWriter(filePaths[0]))
            {
                sw.Write(stlData);
            }
            Debug.Log("STL file saved at: " + filePaths[0]);
        }
        else
        {
            Debug.Log("meshToSave is NULL!");
        }
    }

    public static StringBuilder ExportMeshAsSTL(Mesh mesh)
    {
        StringBuilder stlData = new StringBuilder();
        stlData.Append("solid UnityMesh\n");

        float scaleFactor = 1000f; // Adjust this as needed
        Quaternion rotation = Quaternion.Euler(270, 90, 90); // Rotate by 90 degrees around the y-axis

        float minY = Vector3.positiveInfinity.y;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 transformedVertex = Selection.instance.transform.TransformPoint(rotation * mesh.vertices[i] * scaleFactor);
            transformedVertex.x = -transformedVertex.x;  // Negate the X component to fix mirroring
            minY = Mathf.Min(minY, transformedVertex.y);
        }

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            Vector3 v1 = Selection.instance.transform.TransformPoint(rotation * (mesh.vertices[mesh.triangles[i + 0]] * scaleFactor));
            Vector3 v2 = Selection.instance.transform.TransformPoint(rotation * (mesh.vertices[mesh.triangles[i + 1]] * scaleFactor));
            Vector3 v3 = Selection.instance.transform.TransformPoint(rotation * (mesh.vertices[mesh.triangles[i + 2]] * scaleFactor));

            // Negate the X components to correct the mirroring
            v1.x = -v1.x;
            v2.x = -v2.x;
            v3.x = -v3.x;

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

    public GameObject mainMenu, slicerSettingsMenu;

    public void ReturnToMainMenu(bool back)
    {
        // If this is the go back button, turn on the main panel and turn off the slicer settings panel
        if (back)
        {
            mainMenu.SetActive(true);
            slicerSettingsMenu.SetActive(false);
        }
        else
        {
            mainMenu.SetActive(false);
            slicerSettingsMenu.SetActive(true);
        }
    }

    public void ChangeStatus(string status)
    {
        string serverIP = serverIP_field.text.ToString();
        string printerIP = printerIP_field.text.ToString();

        StartCoroutine(StatePrinterStatus(status, serverIP, printerIP));

        if (status == "pause")
        {
            if (startedPrinting == false) // If we first began printing
            {
                startPrintImg.gameObject.SetActive(false);
                pausePrintImg.gameObject.SetActive(true); startButtonText.text = "pause print";
                resumePrintImg.gameObject.SetActive(false);
                abortPrintImg.gameObject.SetActive(true); slicerButtonText.text = "abort print";
                slicerSettingsImg.gameObject.SetActive(false);
            }
            else // If everything is running, switch pause and resume
            {
                if (paused == false)
                {
                    pausePrintImg.gameObject.SetActive(false);
                    resumePrintImg.gameObject.SetActive(true); startButtonText.text = "resume print";
                }
                else
                {
                    pausePrintImg.gameObject.SetActive(true); startButtonText.text = "pause print";
                    resumePrintImg.gameObject.SetActive(false);
                }
                paused = !paused;
            }
            startedPrinting = true;
        }
        else if (status == "stop")
        {
            if (startedPrinting == false) // If we are just pressing the slicer settings button
            {
                // Doing nothing for now
            }
            else // If we are pressing the abort button
            {
                startPrintImg.gameObject.SetActive(true); startButtonText.text = "start print";
                pausePrintImg.gameObject.SetActive(false);
                resumePrintImg.gameObject.SetActive(false);
                abortPrintImg.gameObject.SetActive(false);
                slicerSettingsImg.gameObject.SetActive(true); slicerButtonText.text = "slicer settings";
                paused = false;
                startedPrinting = false;
            }
        }
    }

    public IEnumerator StatePrinterStatus(string status, string serverUrl, string printerIP)
    {
        string p_json = $"{{\"modelName\": \"{status}\", \"printerIP\": \"{printerIP}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(p_json);

        Debug.Log($"Change state: {status}");
        using (UnityWebRequest p_request = new UnityWebRequest($"{serverUrl}/print", "PUT"))
        {
            p_request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            p_request.downloadHandler = new DownloadHandlerBuffer();
            p_request.SetRequestHeader("Content-Type", "application/json");

            yield return p_request.SendWebRequest();

            if (p_request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {p_request.error}");
                printerObjectSentRequest = false;
                yield break;
            }
            string p_response = p_request.downloadHandler.text;
            Debug.Log($"Printer State Response: {p_response}");
        }
    }

    public IEnumerator SendSTLData(string stlData, string serverUrl, string printer, string options)
    {
        // Create a JSON object with the stlFileData
        //string json = $"{{\"modelName\": \"model\", \"stlFileData\": \"{System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stlData))}\"}}";

        // Convert the JSON string to a byte array
        //byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        string p_json = $"{{\"modelName\": \"model\", \"stlFileData\": \"{System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stlData))}\", \"printer\": \"{printer}\", \"options\": {options}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(p_json);

        //Debug.Log($"{serverUrl}/slice");
        //Debug.LogError(json);

        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/slice", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
                printerObjectSentRequest = false;
                yield break;
            }

            string response = request.downloadHandler.text;
            SlicerClass sJson = JsonUtility.FromJson<SlicerClass>(response);
            Debug.Log($"gcode: {sJson.gcode}");

            ParseGCode(sJson.gcode);
            CreateLineRenderers();

        }
    }


    public IEnumerator StartPrintProc(string serverUrl)
    {
        string printerIP = printerIP_field.text.ToString();
        if (!string.IsNullOrEmpty(printerIP))
        {
            Debug.Log("Waiting for printer's response. You might need to authenticate the server from printer: " + printerIP);
            infoText.text = "waiting for authentication";
            infoText.color = Color.yellow;
            StopCoroutine(Selection.instance.fadeTextCoroutine);
            infoText.alpha = 1.0f;

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
                    printerObjectSentRequest = false;

                    infoText.text = "print error";
                    infoText.color = Color.red;
                    StopCoroutine(Selection.instance.fadeTextCoroutine);
                    infoText.alpha = 1.0f;
                    yield break;
                }

                string p_response = p_request.downloadHandler.text;
                Debug.Log($"Printer Response: {p_response}");

                infoText.text = "waiting for printer preparation";
                infoText.color = Color.green;
                StopCoroutine(Selection.instance.fadeTextCoroutine);
                infoText.alpha = 1.0f;

                StartCoroutine(getPrinterStatus(serverUrl, printerIP));
            }
        }
    }

    public IEnumerator getPrinterStatus(string serverUrl, string printerUrl)
    {
        while (true)
        {
            yield return new WaitForSeconds(3);

            using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/print?printerIP={printerUrl}", "GET"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();
                string response = request.downloadHandler.text;
                Debug.Log($"Wait Response: {response}");
                WaitingJsonClass json = JsonUtility.FromJson<WaitingJsonClass>(response);
                Debug.Log(json.progress);

                if (json.state != "pre_print" && json.state != "printing" && json.state != "wait_cleanup")
                {
                    progressBar.gameObject.SetActive(false);
                    printerObjectSentRequest = false;
                    printing = false;
                    infoText.text = "done";
                    infoText.color = Color.green;
                    StopCoroutine(Selection.instance.fadeTextCoroutine);
                    infoText.alpha = 1.0f;
                    yield break;
                }

                if (json.time_total != 0)
                {
                    if (!printing)
                    {
                        printing = true;
                        infoText.text = "printing in progress";
                        infoText.color = Color.cyan;
                        StopCoroutine(Selection.instance.fadeTextCoroutine);
                        infoText.alpha = 1.0f;
                    }
                    if (json.progress == 1)
                    {
                        printing = true;
                        infoText.text = "print complete";
                        infoText.color = Color.green;
                        StopCoroutine(Selection.instance.fadeTextCoroutine);
                        infoText.alpha = 1.0f;
                    }

                    progressBar.value = json.progress;
                    progressBar.gameObject.SetActive(true);
                    progressPercentText.text = (json.progress * 100).ToString("F0") + "%";
                }
            }
        }
    }

    public float extrusionWidth = 0.1f; // Width of extrusion lines
    public Material extrusionMaterial, supportMaterial; // Material for extrusion lines

    private List<(List<Vector3>, string)> lineSegments = new List<(List<Vector3>, string)>(); // Include mode with each segment



    // Parse G-code into segments, grouping extrusion points
    public void ParseGCode(string gcode)
    {
        //string[] lines = File.ReadAllLines(filePath);

        Vector3 currentPosition = Vector3.zero; // Start at origin
        bool isExtruding = false;              // Flag to track extrusion state
        List<Vector3> currentSegment = new List<Vector3>();

        string currentMode = "";

        foreach (string line in gcode.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            // Ignore comments, setting the mode for the following lines so that we can skip printing certain layers
            if (line.StartsWith(";"))
            {
                if (line.StartsWith(";TYPE:"))
                {
                    currentMode = line.Substring(6); // Get everything after ";TYPE:"
                }
                else if (line.StartsWith(";BRIDGE"))
                {
                    currentMode = "BRIDGE";
                }
                continue;
            }

            // Handle G1 (linear), G2 (clockwise arc), and G3 (counterclockwise arc)
            if (line.StartsWith("G0") || line.StartsWith("G1") || line.StartsWith("G2") || line.StartsWith("G3"))
            {
                bool extrusionDetected = false;
                Vector3 nextPosition = currentPosition;
                Vector3 centerOffset = Vector3.zero; // For G2/G3 arcs

                // Parse tokens in the line
                string[] tokens = line.Split(' ');
                foreach (string token in tokens)
                {
                    if (token.StartsWith("X"))
                    {
                        nextPosition.x = float.Parse(token.Substring(1), CultureInfo.InvariantCulture);
                    }
                    else if (token.StartsWith("Y"))
                    {
                        nextPosition.z = float.Parse(token.Substring(1), CultureInfo.InvariantCulture);
                    }
                    else if (token.StartsWith("Z"))
                    {
                        nextPosition.y = float.Parse(token.Substring(1), CultureInfo.InvariantCulture);
                    }
                    else if (token.StartsWith("I"))
                    {
                        centerOffset.x = float.Parse(token.Substring(1), CultureInfo.InvariantCulture);
                    }
                    else if (token.StartsWith("J"))
                    {
                        centerOffset.y = float.Parse(token.Substring(1), CultureInfo.InvariantCulture);
                    }
                    else if (token.StartsWith("E"))
                    {
                        extrusionDetected = true; // Extrusion detected
                    }
                }

                if (line.StartsWith("G2") || line.StartsWith("G3"))
                {
                    // Generate arc points for G2/G3
                    bool isClockwise = line.StartsWith("G2");
                    List<Vector3> arcPoints = GenerateArcPoints(currentPosition, nextPosition, centerOffset, isClockwise);

                    // Add the arc points to the current segment
                    currentSegment.AddRange(arcPoints);
                }
                if (extrusionDetected && (currentMode == "SKIRT" || currentMode == "WALL-OUTER" || currentMode == "WALL-INNER" || currentMode == "SUPPORT"))
                {
                    if (!isExtruding && currentSegment.Count > 0)
                    {
                        // Save the current travel segment and start a new one
                        // lineSegments.Add(currentSegment);
                        currentSegment = new List<Vector3>();
                    }

                    // Add extrusion points
                    currentSegment.Add(currentPosition);
                    currentSegment.Add(nextPosition);
                }
                else if (isExtruding && currentSegment.Count > 0)
                {
                    // Close the current segment
                    lineSegments.Add((currentSegment, currentMode));
                    currentSegment = new List<Vector3>();
                }

                isExtruding = extrusionDetected;
                currentPosition = nextPosition; // Update current position
            }
        }

        // Add the final segment if not empty
        if (currentSegment.Count > 0)
        {
            lineSegments.Add((currentSegment, currentMode));
        }

        Debug.Log("Parsed " + lineSegments.Count + " line segments.");
    }

    // Generate points for an arc (G2/G3)
    List<Vector3> GenerateArcPoints(Vector3 start, Vector3 end, Vector3 offset, bool isClockwise)
    {
        List<Vector3> arcPoints = new List<Vector3>();

        // Center of the arc
        Vector3 center = new Vector3(start.x + offset.x, start.y + offset.y, start.z);

        // Calculate angles
        float startAngle = Mathf.Atan2(start.y - center.y, start.x - center.x);
        float endAngle = Mathf.Atan2(end.y - center.y, end.x - center.x);

        // Ensure angles are properly ordered for clockwise/counterclockwise
        if (isClockwise && endAngle > startAngle) endAngle -= 2 * Mathf.PI;
        if (!isClockwise && endAngle < startAngle) endAngle += 2 * Mathf.PI;

        float radius = Vector3.Distance(start, center);
        int segmentCount = Mathf.CeilToInt(Mathf.Abs(endAngle - startAngle) / (Mathf.PI / 180)) * 10; // 10 points per degree

        // Generate points along the arc
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = Mathf.Lerp(startAngle, endAngle, (float)i / segmentCount);
            float x = center.x + radius * Mathf.Cos(t);
            float y = center.y + radius * Mathf.Sin(t);
            arcPoints.Add(new Vector3(x, y, start.z));
        }

        return arcPoints;
    }

    public void CreateLineRenderers()
    {
        GameObject lineRenderers = new GameObject("Line Renderers");
        lineRenderers.transform.SetParent(transform, false);
        Vector3 ls = lineRenderers.transform.localScale;
        ls = new Vector3(ls.x / 200, ls.y / 200, ls.z / 200);
        lineRenderers.transform.localScale = ls;
        Vector3 printerBounds = GetComponent<MeshRenderer>().bounds.extents;
        lineRenderers.transform.position += new Vector3(printerBounds.x, -printerBounds.y, printerBounds.z);

        int rendererCount = 0;

        foreach (var (segment, mode) in lineSegments)
        {
            Material selectedMaterial = extrusionMaterial; // Default to extrusion material

            // Select the appropriate material based on the mode
            if (mode == "SKIRT" || mode == "SUPPORT")
            {
                selectedMaterial = supportMaterial; // Use the support material for skirt and support
            }

            CreateLineRenderer(lineRenderers, segment, rendererCount++, selectedMaterial);
        }

        Debug.Log("Created " + rendererCount + " LineRenderers.");
    }


    // Create a single LineRenderer for a given segment
    void CreateLineRenderer(GameObject parent, List<Vector3> points, int index, Material material)
    {
        GameObject lineObject = new GameObject("LineRenderer_" + index);
        lineObject.transform.SetParent(parent.transform, false);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        lineRenderer.startWidth = extrusionWidth / 200;
        lineRenderer.endWidth = extrusionWidth / 200;

        if (material != null)
        {
            lineRenderer.material = material;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }
}