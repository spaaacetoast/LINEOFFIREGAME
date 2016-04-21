using UnityEngine;
using System.Collections;
using AngryRain;

public class SpawnScreenMenu : MonoBehaviour 
{
    public PlayerControllerGUI playerGUI;
    public GameObject CustomizationMenu;

    CanvasGroup canvasGroup;
    bool isSwitching;

    void Awake()
    {
        canvasGroup = transform.parent.GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        StartCoroutine(HandleEnableAnim());
    }

    public void SwitchToCustomize()
    {
        if (isSwitching)
            return;

        isSwitching = true;
        StartCoroutine(HandleSwitchToCustomize());
    }

    public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    IEnumerator HandleEnableAnim()
    {
        float startTime = Time.time;
        while (Time.time - startTime <= 1f)
        {
            float t = Time.time - startTime;
            canvasGroup.alpha = curve.Evaluate(t*4);
            canvasGroup.GetComponent<RectTransform>().localScale = Vector3.Lerp(Vector3.one * 0.975f, Vector3.one, curve.Evaluate(t * 2));
            //canvasGroup.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(Vector3.right * 200, Vector3.zero, curve.Evaluate(t * 2));
            yield return new WaitForEndOfFrame();
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.GetComponent<RectTransform>().localScale = Vector3.one;
        canvasGroup.alpha = 1;
    }

    IEnumerator HandleSwitchToCustomize()
    {
        canvasGroup.blocksRaycasts = false;

        playerGUI.FullscreenFade(true);

        float startTime = Time.time;
        while (Time.time - startTime <= 0.5f)
        {
            float t = Time.time - startTime;
            canvasGroup.alpha = curve.Evaluate(1 - (t * 4));
            canvasGroup.GetComponent<RectTransform>().localScale = Vector3.Lerp(Vector3.one * 0.975f, Vector3.one, curve.Evaluate(1 - (t * 4)));
            //canvasGroup.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(Vector3.right * -200, Vector3.zero, curve.Evaluate(1 - (t * 4)));
            yield return new WaitForEndOfFrame();
        }

        playerGUI.FullscreenFade(false);
        //playerGUI.NavigateTo(CustomizationMenu);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.GetComponent<RectTransform>().localScale = Vector3.one;
        canvasGroup.alpha = 1;
    }
}
