using UnityEngine;
using System.Collections;

public class CameraEnableDepth : MonoBehaviour 
{
	void Start () 
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
	}
}
