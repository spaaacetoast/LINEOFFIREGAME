using UnityEngine;
using System.Collections;
using TNet;

public class SpawnCamera : MonoBehaviour 
{
    public static List<SpawnCamera> allSpawnCameras = new List<SpawnCamera>();

    //For accesing the world spawn camera
    public static Transform worldTransform;
    public static Camera worldCamera;

    //Local variables
    public new Transform transform { private set; get; }
    public Vector3 position { private set; get; }
    public Vector3 rotation { private set; get; }

    //Team Settings
    public bool isTeamCamera = false;
    public int targetTeam = 0;

    //Initial
    public bool isInitialCamera;

    private void Awake()
    {
        allSpawnCameras.Add(this);
        transform = GetComponent<Transform>();
    }

    private void Start()
    {
        if (!TNManager.isHosting) return;

        position = transform.position;
        rotation = transform.eulerAngles;
    }
}
