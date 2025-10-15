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

using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Oculus.Interaction
{
    /// <summary>
    /// Visually displays the current state of an interactable.
    /// </summary>
    public class XRSelection : MonoBehaviour
    {
        [Tooltip("The interactable to monitor for state changes.")]
        /// <summary>
        /// The interactable to monitor for state changes.
        /// </summary>
        [SerializeField, Interface(typeof(IInteractableView))]
        private UnityEngine.Object _interactableView;
        private IInteractableView InteractableView;
        private int highlihgtCalling, selectionCalling;
        public Transform myObject;
        public static bool tapSelected = false;

        protected bool _started = false;

        protected virtual void Awake()
        {
            InteractableView = _interactableView as IInteractableView;
            highlihgtCalling = 0;
            selectionCalling = 0;
        }


        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(InteractableView, nameof(InteractableView));

            UpdateVisual();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                InteractableView.WhenStateChanged += UpdateVisualState;
                UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractableView.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisual()
        {
            switch (InteractableView.State)
            {
                case InteractableState.Normal:
                    if (highlihgtCalling == 1)
                    {
                        Selection.highlightCalls -= 1;
                        Selection.dropObjects = true;
                        highlihgtCalling = 0;
                        Debug.Log("DeHighlighting Me. Highligting Calls: " + Selection.highlightCalls);
                    }
                    if (selectionCalling == 1)
                    {
                        Selection.selectionCalls -= 1;
                        selectionCalling = 0;
                        Debug.Log("DeSelecting Me. Selection Calls: " + Selection.selectionCalls);
                    }
                    if (Selection.objectOfInterest != null)
                    {
                        Selection.objectOfInterest = null;
                    }
                    break;
                case InteractableState.Hover: // When ray is hitting interactable object
                    if (!gameObject.CompareTag("InputField"))
                    {
                        if (highlihgtCalling == 0)
                        {
                            Selection.highlightCalls += 1;
                            Selection.objectOfInterest = gameObject;
                            highlihgtCalling = 1;
                            Debug.Log("Hovering Over Me. Highlight Calls: " + Selection.highlightCalls);
                        }
                        if (selectionCalling == 1)
                        {
                            Selection.selectionCalls -= 1;
                            selectionCalling = 0;
                            Debug.Log("DeSelecting Me. Selection Calls: " + Selection.selectionCalls);
                        }
                    }
                    break;
                case InteractableState.Select: // When the pinch gesture is done while the ray is hitting interactable object
                    if (gameObject.CompareTag("InputField"))
                    {
                        Debug.Log("Selected the input field.");
                        Selection.selectedField = gameObject.transform.GetComponentInChildren<TMP_InputField>();
                        Selection.selectedFieldTrue = true;
                    }
                    else if (selectionCalling == 0)
                    {
                        Selection.selectionCalls += 1;
                        Selection.objectOfInterest = gameObject;
                        selectionCalling = 1;
                        Debug.Log("Selecting Me. Selection Calls: " + Selection.selectionCalls);
                        tapSelected = true;
                    }
                    break;
                case InteractableState.Disabled:
                    break;
            }
        }


        private void UpdateVisualState(InteractableStateChangeArgs args) => UpdateVisual();

        #region Inject

        public void InjectAllInteractableDebugVisual(IInteractableView interactableView, Renderer renderer)
        {
            InjectInteractableView(interactableView);
        }

        public void InjectInteractableView(IInteractableView interactableView)
        {
            _interactableView = interactableView as UnityEngine.Object;
            InteractableView = interactableView;
        }

        #endregion
    }
}

