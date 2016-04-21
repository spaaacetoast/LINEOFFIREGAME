using UnityEngine;
using System.Collections;

public class RenderResolution : MonoBehaviour 
{
    public float renderResolutionMultiplier = 1;
    public bool enableRenderResolution;
    public RenderTextureFormat renderTextureFormat;
    private bool pEnable;

    private Camera thisCamera;
    private RenderTexture frameBuffer;
    private Material thisMaterial;

    void Start()
    {
        thisCamera = GetComponent<Camera>();
        //thisMaterial = new Material(
        //    "Shader \"Hidden/Invert\" {" +
        //    "SubShader {" +
        //    "    Pass {" +
        //    "        ZTest Always Cull Off ZWrite Off" +
        //    "        SetTexture [_MainTex] { combine one-texture }" +
        //    "    }" +
        //    "}" +
        //    "}"
        //);
    }

    void LateUpdate()
    {
        if (enableRenderResolution && !pEnable)//Enabling
        {
            frameBuffer = RenderTexture.GetTemporary(Mathf.NextPowerOfTwo((int)(Screen.width * renderResolutionMultiplier)), Mathf.NextPowerOfTwo((int)(Screen.height * renderResolutionMultiplier)), 0, renderTextureFormat, RenderTextureReadWrite.Linear);
            frameBuffer.filterMode = FilterMode.Point;
            thisCamera.targetTexture = frameBuffer;
            pEnable = true;
        }
        else if (!enableRenderResolution && pEnable)//Disabling
        {
            RenderTexture.ReleaseTemporary(frameBuffer);
            frameBuffer = null;
            thisCamera.targetTexture = null;
            pEnable = false;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (pEnable)
        {
            //src.filterMode = FilterMode.Point; //Set filtering of the source image to point for hq2x to work
            Graphics.Blit(src, dest, thisMaterial); //Upscale the image
        }
        else
            Graphics.Blit(src, dest);
    }
}
