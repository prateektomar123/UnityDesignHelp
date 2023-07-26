using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

public class RandomTransformEditor : EditorWindow
{
    [MenuItem("Tools/Random Transform Creator")]
    private static void ShowWindow()
    {
        // Create the window and set its properties
        RandomTransformEditor window = GetWindow<RandomTransformEditor>();
        window.titleContent = new GUIContent("Random Transform Creator");
        window.minSize = new Vector2(250, 250);
        window.Show();
    }

    private GameObject targetObject;
    private int numberOfTransforms = 100;
    private float minDistanceBetweenTransforms = 2f;
    private float placementRadius = 20f;
    private bool randomRotation = true;

    private void OnGUI()
    {
        // Target GameObject field
        targetObject = EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true) as GameObject;

        // Number of Transforms field
        numberOfTransforms = EditorGUILayout.IntField("Number of Transforms", numberOfTransforms);

        // Minimum Distance Between Transforms field
        minDistanceBetweenTransforms = EditorGUILayout.FloatField("Min Distance Between Transforms", minDistanceBetweenTransforms);

        // Placement Radius field
        placementRadius = EditorGUILayout.FloatField("Placement Radius", placementRadius);

        // Random Rotation toggle
        randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);

        // Create Transforms button
        if (GUILayout.Button("Create Transforms"))
        {
            if (targetObject != null)
            {
                CreateRandomTransforms();
            }
            else
            {
                Debug.LogError("Target GameObject is not assigned!");
            }
        }
    }

    private void CreateRandomTransforms()
    {
        // List to store the positions of already created transforms
        List<Vector3> createdPositions = new List<Vector3>();

        // Loop to create specified number of random transforms
        for (int i = 0; i < numberOfTransforms; i++)
        {
            Vector3 randomPosition = GetRandomPosition(createdPositions);
            if (randomPosition != Vector3.zero)
            {
                // Create new empty GameObject
                GameObject newTransform = new GameObject("RandomTransform" + i);
                newTransform.transform.position = randomPosition;

                // Random rotation if enabled
                if (randomRotation)
                {
                    newTransform.transform.rotation = Random.rotation;
                }

                // Parent the new transform to the target object
                newTransform.transform.parent = targetObject.transform;

                // Add the position to the list of created positions
                createdPositions.Add(randomPosition);
            }
            else
            {
                // Unable to find a valid position after multiple attempts, stop spawning
                Debug.LogWarning("Unable to find a valid position for all transforms. Stopping.");
                break;
            }
        }

        // Recalculate the NavMesh
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
    }

    private Vector3 GetRandomPosition(List<Vector3> createdPositions)
    {
        int attempts = 10; // Number of attempts to find a valid position
        Vector2 randomCircle = Vector2.zero;

        for (int i = 0; i < attempts; i++)
        {
            randomCircle = Random.insideUnitCircle * placementRadius;
            Vector3 randomPosition = new Vector3(randomCircle.x, 0f, randomCircle.y) + targetObject.transform.position;

            bool isValidPosition = true;

            // Check the distance between the new position and already created positions
            foreach (Vector3 createdPosition in createdPositions)
            {
                if (Vector3.Distance(randomPosition, createdPosition) < minDistanceBetweenTransforms)
                {
                    isValidPosition = false;
                    break;
                }
            }

            if (isValidPosition)
            {
                return randomPosition;
            }
        }

        return Vector3.zero; // Return zero vector if a valid position is not found within the attempts limit
    }
}
