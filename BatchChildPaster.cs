using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BatchChildPaster : EditorWindow
{
    private GameObject objectToCopy;
    private List<GameObject> parentObjects = new List<GameObject>();
    private Vector2 scrollPosition; // For scroll view

    [MenuItem("Tools/Batch Child Paster")]
    public static void ShowWindow()
    {
        GetWindow<BatchChildPaster>("Batch Child Paster");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Child Paster", EditorStyles.boldLabel);

        // Field to assign the object to copy
        objectToCopy = (GameObject)EditorGUILayout.ObjectField("Object to Copy", objectToCopy, typeof(GameObject), true);

        // Button to add selected parents
        if (GUILayout.Button("Add Selected Parents"))
        {
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (!parentObjects.Contains(selected))
                {
                    parentObjects.Add(selected);
                }
            }
        }

        // Scroll view for parent objects
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 100)); // Adjust height dynamically
        GUILayout.Label("Parent Objects:");
        for (int i = 0; i < parentObjects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            parentObjects[i] = (GameObject)EditorGUILayout.ObjectField(parentObjects[i], typeof(GameObject), true);
            if (GUILayout.Button("Remove"))
            {
                parentObjects.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Button to execute the paste operation (always visible)
        if (GUILayout.Button("Paste as Children"))
        {
            if (objectToCopy == null || parentObjects.Count == 0)
            {
                Debug.LogWarning("Please assign an object to copy and at least one parent.");
                return;
            }

            Undo.IncrementCurrentGroup();
            foreach (GameObject parent in parentObjects)
            {
                if (parent != null)
                {
                    GameObject newChild = Instantiate(objectToCopy, parent.transform);
                    newChild.name = objectToCopy.name; // Preserve the original name
                    Undo.RegisterCreatedObjectUndo(newChild, "Paste as Child");
                }
            }
            Undo.SetCurrentGroupName("Batch Paste as Children");
        }
    }
}