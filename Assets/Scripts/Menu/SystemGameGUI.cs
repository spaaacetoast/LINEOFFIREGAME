using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SystemGameGUI : MonoBehaviour 
{
    public static SystemGameGUI instance;
    public GameObject matchIntro { get; set; }

    void Awake()
    {
        instance = this;
        matchIntro = transform.Find("Canvas World/match intro").gameObject;
    }

    public void PlayMatchIntro()
    {
        StartCoroutine("IEPlayMatchIntro");
    }

    IEnumerator IEPlayMatchIntro()
    {
        matchIntro.SetActive(true);
        yield return new WaitForSeconds(15);
        matchIntro.SetActive(false);
    }

    #region Static

    public static void StaticSetMatchIntro(string gamemode, string custommode, string map, string custommap)
    {
        instance.matchIntro.transform.Find("Text").GetComponent<Text>().text = (gamemode + " | " + map).ToUpper();
        instance.matchIntro.transform.Find("Text 1").GetComponent<Text>().text = (custommode + " | " + custommap).ToUpper();
    }

    public static void StaticPlayMatchIntro()
    {
        instance.PlayMatchIntro();
    }

    public static void StaticSetEndScreen(string winner)
    {
        if (winner == "")
        {
            instance.transform.Find("Canvas World/end screen").gameObject.SetActive(false);
        }
        else
        {
            instance.transform.Find("Canvas World/end screen").gameObject.SetActive(true);
            instance.transform.Find("Canvas World/end screen/Image/Text").GetComponent<Text>().text = "MATCH OVER - " + winner.ToUpper() + " WON";
        }
    }

    public static void StaticSetLoadScreenStatus(bool status)
    {
        instance.transform.Find("Canvas Pixel Perfect/load screen").gameObject.SetActive(status);
    }

    #endregion
}
