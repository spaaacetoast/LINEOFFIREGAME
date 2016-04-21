using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeEffect : MonoBehaviour 
{
    public bool beginOnAwake;

    //Animation type for fading
    public enum AnimationType { ImageBased = 0, Fade = 1, Rotation = 2, Position = 3 }
    public AnimationType animationType;

    //These are the sprites used in ImageBased animation, last sprite will be used as final and will be kept active
    [System.Serializable]
    public struct SpriteInfo { public Texture2D texture; public Rect uv; }
    public SpriteInfo[] spriteInfo;

    public Color fromColor;
    public Color targetColor;

    public Vector3 fromRotation;
    public Vector3 fromPosition;

    public float animationSpeed = 1;
    public float animationDelay;

    public bool resetObject;

    private RawImage image;
    private Text text;

    private Graphic graphic;

    void Awake()
    {
        image = GetComponent<RawImage>();
        text = GetComponent<Text>();

        graphic = GetComponent<Graphic>();
    }

    void OnEnable()
    {
        StopCoroutine("UpdateAnim");
        if (beginOnAwake)
            StartCoroutine("UpdateAnim");
    }

    IEnumerator UpdateAnim()
    {
        Color endColor = fromColor;
        Vector3 endRotation = transform.eulerAngles;
        Vector3 endPosition = transform.localPosition;

        if(resetObject)
        {
            if (animationType == AnimationType.Fade)
                graphic.color = fromColor;
            if (animationType == AnimationType.Rotation)
                transform.eulerAngles = fromRotation;
            if (animationType == AnimationType.Position)
                transform.localPosition = fromPosition;
        }

        yield return new WaitForSeconds(animationDelay);

        if (animationType == AnimationType.ImageBased)
        {
            int currentSprite = 0;
            int length = spriteInfo.Length;
            while (currentSprite < length)
            {
                yield return new WaitForSeconds(1 / animationSpeed);
                image.texture = spriteInfo[currentSprite].texture;
                image.uvRect = spriteInfo[currentSprite].uv;

                currentSprite++;
            }
        }
        if (animationType == AnimationType.Fade)
        {
            graphic.color = fromColor;

            float startTime = Time.time;
            float endTime = (1 / animationSpeed) + Time.time;
            while(Time.time <= endTime)
            {
                yield return new WaitForFixedUpdate();

                endColor = Color.Lerp(endColor, targetColor, animationSpeed);

                graphic.color = endColor;
            }
        }
        if (animationType == AnimationType.Rotation)
        {
            transform.eulerAngles = fromRotation;

            float startTime = Time.time;
            float endTime = (1 / animationSpeed) + Time.time;
            while (Time.time <= endTime || transform.rotation != Quaternion.Euler(endRotation))
            {
                yield return new WaitForFixedUpdate();
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(endRotation), animationSpeed);
            }
        }
        if (animationType == AnimationType.Position)
        {
            transform.localPosition = fromPosition;

            float startTime = Time.time;
            float endTime = (1 / animationSpeed) + Time.time;
            while (Time.time <= endTime)
            {
                yield return new WaitForFixedUpdate();
                transform.localPosition = Vector3.Lerp(transform.localPosition, endPosition, animationSpeed);
            }
        }
    }
}
