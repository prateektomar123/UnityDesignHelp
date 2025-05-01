// This script is used to copy the transform data of a selected GameObject in Unity.
// It allows you to duplicate the selected GameObject and set the pose of the duplicate object and its children.
// The script uses Unity's Editor class to create a custom menu item in the GameObject menu. When the menu item is clicked, it duplicates the selected object and sets the transform data for the duplicate object and its children.
// The script includes error handling to ensure that an object is selected before attempting to duplicate it. It also uses Undo.RecordObject to allow for undoing the changes made to the transform data.
using UnityEditor;
using UnityEngine;

public class CopyTransform : Editor
{
    [MenuItem("GameObject/SetPose", false, -10)]
    public static void SetPoseMenuItem()
    {
        // Get the clicked object in the Unity Editor
        GameObject originalObject = Selection.activeGameObject;

        if (originalObject == null)
        {
            Debug.LogWarning("No object selected. Please select an object to set the pose.");
            return;
        }

        // Create a duplicate of the selected object
        GameObject duplicatedObject = Instantiate(originalObject);

        // Set the transform data for the duplicate object and its children
        SetTransformData(duplicatedObject.transform);
    }

    private static void SetTransformData(Transform obj)
    {
        // Iterate through each child transform and set the pose
        foreach (Transform childTransform in obj.GetComponentsInChildren<Transform>())
        {
            // Skip the root transform
            if (childTransform == obj)
                continue;

            Undo.RecordObject(childTransform, "Set Pose");

            // Set the local pose of the transform
            childTransform.localPosition = childTransform.localPosition;
            childTransform.localRotation = childTransform.localRotation;
            childTransform.localScale = childTransform.localScale;
        }

        // Notify the user
        Debug.Log("Pose set for the duplicate object and its children.");
    }
}
