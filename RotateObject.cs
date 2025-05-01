// RotateObject.cs
// This script rotates a GameObject around its local axes based on the specified rotation speed.
// The rotation speed can be set in the Unity Inspector, and the rotation is applied in the Update method to ensure smooth rotation over time.
// The script uses Unity's MonoBehaviour class and the Transform component to handle the rotation of the GameObject.
using UnityEngine;
using System.Collections;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}
