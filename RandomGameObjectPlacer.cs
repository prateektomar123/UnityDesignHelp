// This script allows you to randomly place GameObjects in the scene based on a list of target transforms.
// It provides a custom editor in Unity that allows you to specify the GameObjects to be placed and the number of instances for each prefab.
// The script includes a button in the inspector to trigger the placement of GameObjects, and it handles the instantiation of the prefabs at the specified transforms.
// The script also includes error handling to ensure that the necessary data is provided before placing the GameObjects.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomGameObjectPlacer))]
public class RandomGameObjectPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RandomGameObjectPlacer placer = (RandomGameObjectPlacer)target;

        if (GUILayout.Button("Place GameObjects"))
        {
            placer.PlaceGameObjects();
        }
    }
}

[System.Serializable]
public class GameObjectData
{
    public GameObject prefabToPlace;
    public int numberOfObjects = 1;
}

public class RandomGameObjectPlacer : MonoBehaviour
{
    public List<Transform> targetTransforms = new List<Transform>();
    public List<GameObjectData> gameObjectDataList = new List<GameObjectData>();

    public void PlaceGameObjects()
    {
        if (gameObjectDataList.Count == 0)
        {
            Debug.LogError("Please add at least one GameObject data.");
            return;
        }

        if (targetTransforms.Count == 0)
        {
            Debug.LogError("Please add at least one target transform.");
            return;
        }

        List<Transform> availableTransforms = new List<Transform>(targetTransforms);

        foreach (var gameObjectData in gameObjectDataList)
        {
            if (gameObjectData.prefabToPlace == null)
            {
                Debug.LogError("Please assign a GameObject prefab to place.");
                continue;
            }

            if (availableTransforms.Count == 0)
            {
                Debug.LogError("Not enough target transforms to place all GameObjects.");
                return;
            }

            for (int i = 0; i < gameObjectData.numberOfObjects; i++)
            {
                int randomIndex = Random.Range(0, availableTransforms.Count);
                Transform randomTransform = availableTransforms[randomIndex];
                availableTransforms.RemoveAt(randomIndex);

                Instantiate(gameObjectData.prefabToPlace, randomTransform.position, randomTransform.rotation);
            }
        }
    }
}
