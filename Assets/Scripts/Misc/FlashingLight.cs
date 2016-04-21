using UnityEngine;
using System.Collections;

public class FlashingLight : MonoBehaviour 
{
    public float flashSpeed;

    void Start()
    {
        StopCoroutine("DoFlashingLight");
        StartCoroutine("DoFlashingLight");
    }

    void OnEnable()
    {
        StopCoroutine("DoFlashingLight");
        StartCoroutine("DoFlashingLight");
    }

    bool goPositive = true;
    public float minIntensity = 0.05f;
    public float maxIntensity = 8;

    IEnumerator DoFlashingLight()
    {
        Light light = GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 0;

            while (true)
            {
                light.intensity += flashSpeed * (goPositive ? 1 : -1);
                light.intensity = Mathf.Clamp(light.intensity, minIntensity, maxIntensity);
                if (light.intensity >= maxIntensity)
                    goPositive = false;
                if (light.intensity <= minIntensity)
                    goPositive = true;
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
