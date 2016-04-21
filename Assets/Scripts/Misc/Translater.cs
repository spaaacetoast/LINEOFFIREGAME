using UnityEngine;
using System.Collections;

public class Translater : MonoBehaviour 
{
    public Transform target;

    public Vector3 rotationSpeed;
    public float distance;

    void Update()
    {
        Quaternion rot = transform.rotation * Quaternion.Euler(rotationSpeed);
        Vector3 pos = target.position + ((rot * Vector3.forward) * distance);
        transform.position = pos;
        transform.rotation = rot;
    }
}
