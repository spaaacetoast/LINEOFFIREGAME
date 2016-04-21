using UnityEngine;
using System.Collections;
using AngryRain.Multiplayer;
using AngryRain;

public class ParticleEffect : MonoBehaviour 
{
    public string particleName;
    public int particleID;
    public ParticleSystem[] particleSystems;
    public ParticleSystem[] randomParticleSystems;

    [Range(0,100)]
    public int randomChance = 10;
    public float usageTime = 1;
    [HideInInspector]
    public float lastTimeUse = 0;

    public LightSettings lightSettings = new LightSettings();
    public ExplosionSettings explosionSettings = new ExplosionSettings();

    public new Transform transform { get; set; }

    public bool initialized { get; set; }

    public void Initialize()
    {
        initialized = true;
        transform = GetComponent<Transform>();
    }

    public void PlayParticleEffect()
    {
        StartCoroutine("Disabler");
    }

    IEnumerator Disabler()
    {
        float startLightIntensity = 0;
        if (lightSettings.useLight)
            startLightIntensity = lightSettings.light.intensity;

        yield return new WaitForEndOfFrame();

        foreach (ParticleSystem part in particleSystems)
            part.Play(false);
        bool activeRandom = UnityEngine.Random.Range(0, 100) < randomChance;
        if (activeRandom)
            foreach (ParticleSystem part in randomParticleSystems)
                part.Play(false);

        if (lightSettings.useLight && (activeRandom && lightSettings.isRandomLight || !lightSettings.isRandomLight))
        {
            lightSettings.light.enabled = true;
            yield return new WaitForSeconds(lightSettings.lightDisableTime);
            if (lightSettings.smoothDisable)
            {
                while (lightSettings.light.intensity != 0)
                {
                    lightSettings.light.intensity = Mathf.MoveTowards(lightSettings.light.intensity, 0, lightSettings.disableSpeed);
                    yield return new WaitForFixedUpdate();
                }
                lightSettings.light.intensity = startLightIntensity;
            }
        }

        if (lightSettings.light)
            lightSettings.light.enabled = false;

        /*if (explosionSettings.isExplosion)
            PlayerCamera.allPlayerCameras[0].StartCameraShake(transform.position, explosionSettings.explosionStrength, explosionSettings.explosionMinRange, explosionSettings.explosionMaxRange);*/
    }

    [System.Serializable]
    public class LightSettings
    {
        public bool useLight;
        public bool smoothDisable;
        /// <summary>
        /// Should this light activate when the random particles also activate
        /// </summary>
        public bool isRandomLight;

        public float disableSpeed=1;

        public Light light;
        public float lightDisableTime = 0.05f;
    }

    [System.Serializable]
    public class ExplosionSettings
    {
        public bool isExplosion;
        public float explosionMinRange;
        public float explosionMaxRange;
        public float explosionStrength;
    }
}
