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
