// This script creates a custom editor window in Unity that allows you to select all GameObjects in the scene that belong to a specified layer.
// It provides a simple interface where you can enter the layer name and click a button to select all objects in that layer.
// The script uses Unity's Editor namespace to create the custom window and handle the selection of GameObjects.
// It also includes error handling to ensure that the layer name entered is valid.
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SelectObjectsByLayer : EditorWindow
{
    private string layerName = "Default";

    [MenuItem("Tools/Select Objects by Layer")]
    public static void ShowWindow()
    {
        GetWindow<SelectObjectsByLayer>("Select Objects by Layer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select GameObjects by Layer", EditorStyles.boldLabel);
        layerName = EditorGUILayout.TextField("Layer Name", layerName);

        if (GUILayout.Button("Select Objects"))
        {
            int targetLayer = LayerMask.NameToLayer(layerName);
            if (targetLayer == -1)
            {
                EditorUtility.DisplayDialog("Error", "Invalid Layer Name!", "OK");
                return;
            }

            SelectObjectsWithLayer(targetLayer);
        }
    }

    private void SelectObjectsWithLayer(int layer)
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        List<GameObject> foundObjects = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layer)
                foundObjects.Add(obj);
        }

        Selection.objects = foundObjects.ToArray();
        EditorGUIUtility.PingObject(foundObjects.Count > 0 ? foundObjects[0] : null);
    }
}
