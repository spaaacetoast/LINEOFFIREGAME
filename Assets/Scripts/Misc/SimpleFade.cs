using UnityEngine;
using System.Collections;

public class SimpleFade : MonoBehaviour 
{
    public bool fadeOnAwake;
    public bool setShaderToTransparent;
    public bool deleteWhenDone = true;

    public Color fadeFrom;
    public Color fadeTo;
    public float speed;

    void Start()
    {
        if (fadeOnAwake)
            StartFade();
        if (setShaderToTransparent)
            GetComponent<Renderer>().material.shader = Shader.Find("Transparent/Diffuse");
    }

    public void StartFade()
    {
        GetComponent<Renderer>().material.color = fadeFrom;
        StartCoroutine(FadeTo());
    }

    IEnumerator FadeTo()
    {
        while (GetComponent<Renderer>().material.color != fadeTo)
        {
            yield return new WaitForFixedUpdate();
            GetComponent<Renderer>().material.color = Color.Lerp(GetComponent<Renderer>().material.color, fadeTo, speed);
        }

        if (deleteWhenDone)
            Destroy(gameObject);
    }
}
