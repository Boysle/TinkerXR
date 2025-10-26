# TinkerXR

TinkerXR is an in-situ CAD and 3D printing AR interface that enables intuitive design and fabrication directly within users’ physical environments. It features spatial awareness, depth occlusion, and seamless integration with 3D printing workflows.

<sub>By [Oğuz Arslan](https://boysle.github.io)\*, [Artun Akdoğan](https://www.linkedin.com/in/artun-akdogan)\*, [Mustafa Doğa Doğan](https://www.dogadogan.com/)†</sub>

<sup>*Bogazici University and †Adobe Research, Basel, Switzerland</sup>

If you use **TinkerXR** as part of your research, you should cite it as follows:

> <sup>Oguz Arslan, Artun Akdogan, and Mustafa Doga Dogan. 2024. TinkerXR: In-Situ, Reality-Aware CAD and 3D Printing Interface for Novices. In *Proceedings of the 10th ACM Symposium on Computational Fabrication* (SCF ’25). Association for Computing Machinery. arXiv:2410.06113. [https://arxiv.org/abs/2410.06113](https://arxiv.org/abs/2410.06113)</sup>


## Project Page  
For detailed information, demos, and publications, please visit the [TinkerXR Project Page](https://tinkerxr.github.io/).

## Requirements  

- **Unity Editor Version:** Developed and Tested on Unity 2022.3.22f1 (LTS)  
- **SDKs:**  
  - [Meta XR All-in-One SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657?srsltid=AfmBOoo1ugJVmazrUHjySaQsPGgE4YFyGf7LGkutBpcKmv-jX1KyImIo) Version 71.0.0
  - [oculus-samples/Unity-DepthAPI](https://github.com/oculus-samples/Unity-DepthAPI) Version 67.0.0
- **Headset:** Meta Quest 3

## Getting Started  

**1.** Clone the repository:
  ```bash
  git clone https://github.com/Boysle/TinkerXR-SCF-2025.git
  ```

**2.** Open the project in Unity (recommended version above).

**3.** Make sure the required SDKs are installed via Unity Package Manager.

**4.** Connect your Quest 3 headset and press Play on scene editor to start exploring.

**5.** The system can be built on both PC or the headset itself.

## Script Structure in the Scene

#### GameManager Object
- **Cube Highlighter:** Manages the creation, positioning, and updating of the 3D manipulation box (Object Manipulation Tool) with interactive handles, edges, and projection lines.
- **Vertex Scaler:** Handles moving, rotating, and scaling the vertices of selected objects in response to manipulation UI (handles/wheels), including snapping and updating the manipulation gizmo.
- **Keyboard Input Handler:** Manages the virtual numeric keypad UI for scaling.
- **Calibrate World Coordinates:** Positions, orients, and scales the virtual grid workspace to MRUK-detected anchors, manages grid appearance and settings, and updates snapping coordinates for precise placement.
- **Snap to Grid:** Manages grid snapping and axis-locking for manipulation handles by computing nearest grid-aligned positions for corner and movement handles, tracking grid offsets, and updating the manipulation gizmo.
- **Ruler Manager:** Manages the ruler tool.
- **Uniform Scaling:** Constrains a corner handle to move along the line from the manipulator center to the corner to enforce uniform scaling.
- **Lock Axis:** Provides a pinch-activated axis-locking tool that lets users lock/unlock X/Y/Z movement or scaling.

#### Selection Object
- **Selection:** The main script controlling the object selection logic. Most of the other scripts highly depend on this script. Manages creation, highlighting/selection and material state of scene objects, integrates with the manipulation gizmo, and provides CSG merging, STL export, printer interaction, and save/load functionality. A modified version of _XRSelection_ script is used for the logic of selecting objects, 

#### Rectangular Prism Generator Object
- **Rectangular Prism Generator:** Calculates the world-space bounds of currently selected objects and updates the manipulation prism's position, scale, and visibility so it tightly encloses them.

#### Rotation Detector with Canvas Object
- **Hand Rotation Detector:** Detects the hand's orientation (Up/Down/Right) using dot products and switches selection, snapping, or uniform-scaling modes and their UI indicators accordingly.

#### Printer Prefab Object
- **Printer Properties:** Handles exporting meshes to STL (local or to a server), sending slicer/print requests, parsing returned G-code into extrusion segments and rendering them, and managing print-state UI and progress.

#### Select Workspace Object
- **Select Workspace:** Raycast to let the user point-and-pinch to select room anchors or object vertices, places/rotates the gizmo plane.


