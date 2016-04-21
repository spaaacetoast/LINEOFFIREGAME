using UnityEngine;
using System.Collections;

public class PerlinRotationShake : MonoBehaviour 
{
    public Vector3 offsetX, offsetY, shakeSize, speedX, speedY;
    public bool additiveRotation;

    void Update()
    {
        Vector3 rot = new Vector3(
            Mathf.PerlinNoise(offsetX.x + (Time.time * speedX.x), offsetY.x + (Time.time * speedX.x)) * shakeSize.x,
            Mathf.PerlinNoise(offsetX.y + (Time.time * speedX.y), offsetY.y + (Time.time * speedY.y)) * shakeSize.y,
            Mathf.PerlinNoise(offsetX.z + (Time.time * speedX.z), offsetY.z + (Time.time * speedY.z)) * shakeSize.z);

        transform.localEulerAngles = additiveRotation ? transform.localEulerAngles + rot : rot;
    }
}

[System.Serializable]
public class ShakeSettings
{
    public AnimationCurve playCurve;
    public Vector3 offsetX, offsetY, shakeSize, speedX, speedY;

    public ShakeSettings(Vector3 offsetX, Vector3 offsetY, Vector3 shakeSize, Vector3 speedX, Vector3 speedY, AnimationCurve playCurve)
    {
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.shakeSize = shakeSize;
        this.speedX = speedX;
        this.speedY = speedY;
        this.playCurve = playCurve;
    }

    public Vector3 GetRotation()
    {
        Vector3 rot = new Vector3(
            Mathf.PerlinNoise(offsetX.x + (Time.time * speedX.x), offsetY.x + (Time.time * speedX.x)) * shakeSize.x,
            Mathf.PerlinNoise(offsetX.y + (Time.time * speedX.y), offsetY.y + (Time.time * speedY.y)) * shakeSize.y,
            Mathf.PerlinNoise(offsetX.z + (Time.time * speedX.z), offsetY.z + (Time.time * speedY.z)) * shakeSize.z);

        return rot;
    }
}