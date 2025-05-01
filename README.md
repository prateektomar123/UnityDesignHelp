# Unity Design Tools

This repository contains a collection of Unity scripts designed to enhance the development workflow by providing tools for object placement, transformation, visualization, and more. These scripts are particularly useful for level design, object management, and debugging in Unity projects.

## Table of Contents

- [Scripts Overview](#scripts-overview)
- [Installation](#installation)
- [Usage](#usage)
- [Scripts Details](#scripts-details)
  - [CopyTransform](#copytransform)
  - [SpawnPtGizmos](#spawnptgizmos)
  - [RotateObject](#rotateobject)
  - [RandomTransformEditor](#randomtransformeditor)
  - [RandomGameObjectPlacer](#randomgameobjectplacer)
  - [PickupsSpawner](#pickupsspawner)
  - [FindObjectsByLayer](#findobjectsbylayer)
- [License](#license)

---

## Scripts Overview

This project includes the following scripts:

1. **CopyTransform**: Duplicate GameObjects and set their transform data.
2. **SpawnPtGizmos**: Visualize spawn points in the Unity Editor using gizmos.
3. **RotateObject**: Rotate GameObjects based on specified rotation speeds.
4. **RandomTransformEditor**: Create random transforms around a target GameObject.
5. **RandomGameObjectPlacer**: Place GameObjects randomly at specified transforms.
6. **PickupsSpawner**: Spawn and respawn pickups at random spawn points.
7. **FindObjectsByLayer**: Select all GameObjects in the scene belonging to a specified layer.

---

## Installation

1. Clone this repository or download the ZIP file.
2. Copy the scripts into your Unity project's `Assets` folder.
3. Attach the scripts to GameObjects or use them via the Unity Editor as described below.

---

## Usage

Each script has a specific purpose and can be used independently. Refer to the [Scripts Details](#scripts-details) section for detailed instructions on how to use each script.

---

## Scripts Details

### CopyTransform

- **File**: [CopyTransform.cs](CopyTransform.cs)
- **Description**: Duplicates a selected GameObject and sets the transform data for the duplicate and its children.
- **Usage**: 
  - Select a GameObject in the Unity Editor.
  - Use the menu item `GameObject > SetPose` to duplicate and set the pose.

---

### SpawnPtGizmos

- **File**: [SpawnPtGizmos.cs](SpawnPtGizmos.cs)
- **Description**: Draws yellow gizmos in the Unity Editor to visualize spawn points.
- **Usage**: Attach this script to a GameObject to visualize its position with a cube in the Scene view.

---

### RotateObject

- **File**: [RotateObject.cs](RotateObject.cs)
- **Description**: Rotates a GameObject around its local axes based on a specified rotation speed.
- **Usage**: Attach this script to a GameObject and set the `rotationSpeed` in the Inspector.

---

### RandomTransformEditor

- **File**: [RandomTransformEditor.cs](RandomTransformEditor.cs)
- **Description**: Creates random transforms around a target GameObject.
- **Usage**:
  - Open the custom editor via `Tools > Random Transform Creator`.
  - Configure the settings and click `Create Transforms`.

---

### RandomGameObjectPlacer

- **File**: [RandomGameObjectPlacer.cs](RandomGameObjectPlacer.cs)
- **Description**: Places GameObjects randomly at specified transforms.
- **Usage**:
  - Attach this script to a GameObject.
  - Configure the `targetTransforms` and `gameObjectDataList` in the Inspector.
  - Use the `Place GameObjects` button in the Inspector.

---

### PickupsSpawner

- **File**: [PickupsSpawner.cs](PickupsSpawner.cs)
- **Description**: Spawns and respawns pickups at random spawn points.
- **Usage**:
  - Attach this script to a GameObject.
  - Configure the `spawnPoints`, `prefab`, `delay`, and `amount` in the Inspector.

---

### FindObjectsByLayer

- **File**: [FindObjectsByLayer.cs](FindObjectsByLayer.cs)
- **Description**: Selects all GameObjects in the scene that belong to a specified layer.
- **Usage**:
  - Open the custom editor via `Tools > Select Objects by Layer`.
  - Enter the layer name and click `Select Objects`.

---

## License

This project is licensed under the MIT License. You are free to use, modify, and distribute the scripts as needed.

---