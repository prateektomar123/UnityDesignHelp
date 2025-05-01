// This script is used to draw gizmos in the Unity editor for spawn points.
// It allows you to visualize the spawn points in the scene view by drawing a cube at the position of the GameObject this script is attached to.
// The size of the cube can be adjusted through the 'size' variable in the inspector. The gizmos are drawn in yellow color for better visibility.
// The script uses Unity's Gizmos class to draw the cube and includes methods for drawing the gizmos when the GameObject is selected or when the scene view is updated.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPtGizmos : MonoBehaviour
{
    public float size;
    // public Color gizmoColor;

    void OnDrawGizmos()
    {
        DrawGizmos();
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(transform.position, new Vector3(size, size, size));
    }
}
