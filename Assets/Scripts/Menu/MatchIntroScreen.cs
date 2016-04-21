using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MatchIntroScreen : MonoBehaviour 
{
    public Image back1 { private set; get; }
    public Image back2 { private set; get; }
    public Text text1 { private set; get; }
    public Text text2 { private set; get; }

    void Awake()
    {
        back1 = transform.Find("back").GetComponent<Image>();
        back2 = transform.Find("back 1").GetComponent<Image>();
        text1 = transform.Find("Text").GetComponent<Text>();
        text2 = transform.Find("Text 1").GetComponent<Text>();
    }

    void OnEnable()
    {
        StartCoroutine("HandleAnimation");
    }

    IEnumerator HandleAnimation()
    {
        back1.color = new Color(0, 0, 0, 1);
        back2.color = new Color(0, 0, 0, 0.4f);
        text1.color = new Color(1, 1, 1, 0);
        text2.color = new Color(1, 1, 1, 0);

        yield return new WaitForEndOfFrame();



        yield return new WaitForSeconds(1);

        float startTime = Time.time;

        while (Time.time - startTime < 2)//Enable Text, Takes 2 seconds
        {
            yield return new WaitForEndOfFrame();
            text1.color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, Time.time - 0.5f - startTime);
            text2.color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, Time.time - 1 - startTime);
        }

        text1.color = new Color(1, 1, 1, 1);
        text2.color = new Color(1, 1, 1, 1);

        while(Time.time - startTime < 5)//Disable black background, Takes 2 seconds
        {
            yield return new WaitForEndOfFrame();
            back1.color = Color.Lerp(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), ((Time.time - 2 - startTime) / 2));
        }

        back1.color = new Color(0, 0, 0, 0);

        while (Time.time - startTime < 6)//Disable Text, Takes 1 second
        {
            yield return new WaitForEndOfFrame();
            text1.color = Color.Lerp(new Color(1, 1, 1, 1), new Color(0, 0, 0, 0), (Time.time - 5 - startTime) * 2);
            text2.color = Color.Lerp(new Color(1, 1, 1, 1), new Color(0, 0, 0, 0), (Time.time - 5 - (startTime + 0.25f)) * 2);
            back2.color = Color.Lerp(new Color(0, 0, 0, 0.4f), new Color(0, 0, 0, 0), (Time.time - 5 - (startTime + 0.5f)) * 2);
        }

        text1.color = new Color(1, 1, 1, 0);
        text2.color = new Color(1, 1, 1, 0);
        back2.color = new Color(0, 0, 0, 0);
    }
}
