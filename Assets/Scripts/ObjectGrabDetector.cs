/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

// Core component for detecting and managing object manipulation using Oculus HandGrab interactions.
// It tracks the grabbing state of various UI manipulation tools and updates the scene accordingly.
namespace Oculus.Interaction
{
    /// <summary>
    /// HandGrabGlow controls the glow properties of the OculusHand material to get a glow effect
    /// when pinch / palm grabbing objects depending on the per finger pinch / palm strength.
    /// To achive the glow effect, it also generates a custom UV channel and using the joints
    /// in the hand visual  component adds per finger mask information.
    /// </summary>
    public class ObjectGrabDetector : MonoBehaviour
    {

        #region Inspector
        public RectangularPrismCreator rectangularPrismCreatorReference;
        public CubeHighlighter cubeHighlighterReference;
        [SerializeField, Interface(typeof(IHandGrabInteractor), typeof(IInteractor))]
        private UnityEngine.Object _handGrabInteractor;
        public Transform manipulationCube;

        #endregion

        enum GrabState
        {
            None,
            Pinch,
            Palm
        }
        public static bool _isGrabbingUIElement, _isGrabbingMovementHandle = false, _isGrabbingRotationWheel = false, _isGrabbingEdgeScaler = false, _isGrabbingRulerEdge = false, _isGrabbingCornerScaler = false;
        private GrabState _grabState;
        private MeshRenderer _renderer;

        private IHandGrabInteractor HandGrabInteractor;
        private IHandGrabInteractable HandGrabInteractable;

        protected bool _started = false, errorGiven = false;

        void Awake()
        {
            _isGrabbingUIElement = false;
            _grabState = GrabState.None;
            HandGrabInteractor = _handGrabInteractor as IHandGrabInteractor;
            HandGrabInteractable = transform.GetComponent<IHandGrabInteractable>();
            _renderer = manipulationCube.GetComponent<MeshRenderer>();
        }

        void Update()
        {
            // If we have selected the object, run once
            if (_grabState == GrabState.None && HandGrabInteractor.HandGrabApi.IsHandPinchGrabbing(GrabbingRule.DefaultPinchRule) && !_isGrabbingUIElement)
            {
                HandGrabInteractable = HandGrabInteractor.TargetInteractable;
                if (HandGrabInteractable != null)
                {
                    _grabState = GrabState.Pinch;
                    _isGrabbingUIElement = true;
                    Selection.selectionManipulationUIObject = HandGrabInteractable.RelativeTo.gameObject;
                    if (Selection.selectionManipulationUIObject.tag == "Corner Scaler")
                    {
                        _isGrabbingCornerScaler = true;
                    }
                    else { _isGrabbingCornerScaler = false; }
                    if (Selection.selectionManipulationUIObject.tag == "Movement Handle") {
                        // Saving the initial position of all objects in case they will be thrown into the printer, we want to put them back into their original position
                        Selection.originalObjectPositions = new Vector3[Selection.selectedObjects.Count];
                        for (int objectIndex = 0; objectIndex < Selection.originalObjectPositions.Length; objectIndex++)
                        {
                            Selection.originalObjectPositions[objectIndex] = Selection.selectedObjects[objectIndex].transform.position;
                        }
                        // Saving the initial position of the movement handle to deduce how much we moved the handle in the end
                        Vector3 v = Selection.selectionManipulationUIObject.transform.position;
                        VertexScaler.initialHandlePos = new Vector3(v.x, v.y, v.z);
                        _isGrabbingMovementHandle = true;
                        if (Selection.selectedObjects[0].transform.gameObject.name != "printer")
                        {
                            Selection.isDragging = true;
                        }
                    }
                    else { _isGrabbingMovementHandle = false; }
                    if (Selection.selectionManipulationUIObject.tag == "Rotation Wheel")
                    {
                        VertexScaler.centerPoint = manipulationCube.position;
                        if (Selection.selectionManipulationUIObject.name == "Rotation Wheel Red")
                        {
                            VertexScaler.activeRotationAxis = 0; // Rotation value around X axis
                            VertexScaler.initialRotation = Selection.selectionManipulationUIObject.transform.rotation.eulerAngles.x;
                        }
                        else if (Selection.selectionManipulationUIObject.name == "Rotation Wheel Green")
                        {
                            VertexScaler.activeRotationAxis = 1; // Rotation value around Z axis
                            VertexScaler.initialRotation = Selection.selectionManipulationUIObject.transform.rotation.eulerAngles.z;
                        }
                        else if (Selection.selectionManipulationUIObject.name == "Rotation Wheel Blue")
                        {
                            VertexScaler.activeRotationAxis = 2; // Rotation value around Y axis
                            VertexScaler.initialRotation = Selection.selectionManipulationUIObject.transform.rotation.eulerAngles.y;
                        }
                        _isGrabbingRotationWheel = true;
                    }
                    else { _isGrabbingRotationWheel = false; }
                    if (Selection.selectionManipulationUIObject.tag == "Edge Scaler")
                    {
                        ActiveScalingAxis activeScalingAxis = Selection.selectionManipulationUIObject.GetComponent<ActiveScalingAxis>();
                        if (activeScalingAxis != null)
                        {
                            VertexScaler.centerPoint = manipulationCube.position;
                            VertexScaler.activeScalingAxis = activeScalingAxis.GetAxisValue();
                            VertexScaler.axisScale = new Vector3(manipulationCube.transform.localScale.x, manipulationCube.transform.localScale.y, manipulationCube.transform.localScale.z);
                        }
                        _isGrabbingEdgeScaler = true;
                        KeyboardInputHandler.firstKeyboardAction = true;
                    }
                    else { _isGrabbingEdgeScaler = false; }
                    if (Selection.selectionManipulationUIObject.tag == "Ruler Edge")
                    {
                        _isGrabbingRulerEdge = true;
                    }
                    else { _isGrabbingRulerEdge = false; }
                    // Debug.Log("Selection Manipulation UI Object is: " + Selection.selectionManipulationUIObject);
                    Selection.selectedManipulationUI = true;
                    errorGiven = false;
                    HandRotationDetector.receivedGrabBool = true;
                    // Getting the initial edge sizes of the object manipulation tool at the beginning of selecting a manipulation ui
                    Vector3 size = _renderer.bounds.size;
                    VertexScaler.x0 = size.x; VertexScaler.y0 = size.y; VertexScaler.z0 = size.z;
                    // Getting the moving ui's initial locaiton
                    VertexScaler.movedPoint = Selection.selectionManipulationUIObject.transform.position;
                }
                else if (!errorGiven)
                {
                    // Debug.LogError("Error: HandGrabInteractable is null!");
                    errorGiven = true;
                }
            }
            // If we have deselected the object, run once
            // We add another object manipulation tool size check after every leave action
            else if (_grabState != GrabState.None && !HandGrabInteractor.HandGrabApi.IsHandPinchGrabbing(GrabbingRule.DefaultPinchRule) && _isGrabbingUIElement)
            {
                _isGrabbingCornerScaler = false;
                _isGrabbingMovementHandle = false;
                _isGrabbingRotationWheel = false;
                _isGrabbingEdgeScaler = false;
                _isGrabbingRulerEdge = false;
                _grabState = GrabState.None;
                _isGrabbingUIElement = false;
                Selection.isDragging = false;
                HandRotationDetector.receivedGrabBool = false;

                RecalibrateButtons(Selection.selectionManipulationUIObject.transform);
                // Debug.Log("Stopped grabbing the " + Selection.selectionManipulationUIObject.transform.tag);

                Selection.selectionManipulationUIObject = null;
                Selection.selectedManipulationUI = false;
                UpdateManipulationToolWithInvoke();
            }
        }

        public void UpdateManipulationToolWithInvoke()
        {
            /* We decided not to use reflection and instead use direct invocation for efficiency
            // Call the private function using Reflection, this function is in RectangularPrismCreator Script
            MethodInfo method = rectangularPrismCreatorReference.GetType().GetMethod("UpdateManipulatorLocation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(rectangularPrismCreatorReference, null);
            }
            // Call the private function using Reflection, this function is in CubeHighlighter Script
            method = cubeHighlighterReference.GetType().GetMethod("UpdateManipulationCube", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(cubeHighlighterScriptReference, null);
            }
            */
            rectangularPrismCreatorReference.UpdateManipulatorLocation();
            cubeHighlighterReference.UpdateManipulationCube();
        }

        private void RecalibrateButtons(Transform trans)
        {
            if (trans.tag == "Corner Scaler")
            {
                Transform rendererTransform = trans.GetChild(0);
                Vector3 diff = rendererTransform.position - trans.position;
                trans.position += diff;
                rendererTransform.localPosition = new Vector3(0, 0, 0);
            }
            else if (trans.tag == "Movement Handle")
            {
                float additionalOffset = 0f;
                if (Selection.selectedObjects.Count > 0 && Selection.selectedObjects[0].transform.name == "printer")
                {
                    additionalOffset = CubeHighlighter.additionalPrinterOffset;
                }
                trans.position = manipulationCube.position + new Vector3(0, CubeHighlighter.staticMovementButtonOffset + additionalOffset, 0);
            }
        }
    }
}
