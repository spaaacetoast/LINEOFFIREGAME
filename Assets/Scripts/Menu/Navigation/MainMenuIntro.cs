using UnityEngine;
using System.Collections;

public class MainMenuIntro : MonoBehaviour 
{
    public float timeTillActiveControl = 4;
    public string nextMenu = "mainmenu";

    float startTime;

    void Start()
    {
        startTime = Time.time;
    }

	void Update() 
    {
        if (Time.time - startTime > timeTillActiveControl && Rewired.ReInput.players.GetPlayer(0).GetButtonDown("Submit"))
        {
            startTime = Time.time;
            StartCoroutine(TimedIntro());
        }
	}

    IEnumerator TimedIntro()
    {
        GetComponent<Animator>().Play("startmenu_fadeout");
        yield return new WaitForSeconds(1.25f);
        gameObject.SetActive(false);
        NavigationController.NavigateTo(nextMenu);
    }
}
