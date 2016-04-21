using UnityEngine;
using System.Collections;

public class SceneSettings : MonoBehaviour 
{
    public static SceneSettings instance;

    void Awake()
    {
        instance = this;

        SpawnCamera.worldTransform = transform.Find("Spawn Camera");
        SpawnCamera.worldCamera = SpawnCamera.worldTransform.GetComponent<Camera>();
    }
}
