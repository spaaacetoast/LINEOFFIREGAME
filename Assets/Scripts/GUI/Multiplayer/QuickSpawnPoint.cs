using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using AngryRain;

public class QuickSpawnPoint : MonoBehaviour 
{
    public PlayerControllerGUI playerControllerGUI;

    public int requestSpawnAfterTime = 6;

    Text deployTimer;
    float enabledTime;
    bool hasRequestedSpawn;

    void Awake()
    {
        deployTimer = transform.Find("deploy timer").GetComponent<Text>();
    }

    void OnEnable()
    {
        enabledTime = Time.time;
        hasRequestedSpawn = false;

        if (playerControllerGUI.localPlayer.controllerType == Rewired.ControllerType.Joystick)
            transform.Find("cancel text").GetComponent<Text>().text = "PRESS [JUMP] TO CANCEL";
        else
            transform.Find("cancel text").GetComponent<Text>().text = "PRESS [SPACE] TO CANCEL";

        StartCoroutine(PlayEnableAnim());
    }

    void Update()
    {
        float progress = Time.time - enabledTime;

        if (!hasRequestedSpawn)
        {
            if (progress > requestSpawnAfterTime)
            {
                hasRequestedSpawn = true;

                deployTimer.text = "DEPLOYING...";
                playerControllerGUI.Event_SpawnLocalPlayer();
                //AngryRain.Multiplayer.MultiplayerManager.instance.Local_RequestPlayerSpawn(playerControllerGUI.localPlayer);
            }
            else
            {
                deployTimer.text = "DEPLOYING IN " + (int)(requestSpawnAfterTime + 1 - progress) + "...";
            }
        }

        if (playerControllerGUI.localPlayer.playerInput.GetButton("Jump"))
        {
            PlayerControllerGUI.allInstances[0].NavigateTo("spawnscreen");
            StopCoroutine("PlayEnableAnim");
        }
    }

    public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    IEnumerator PlayEnableAnim()
    {
        transform.localPosition = new Vector3(300, 0);

        RectTransform image = transform.Find("Image").GetComponent<RectTransform>();
        image.sizeDelta = new Vector2(500, 100);

        RectTransform bar = transform.Find("bar").GetComponent<RectTransform>();

        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 0;

        float start = Time.time;
        while (Time.time - start <= requestSpawnAfterTime)
        {
            cg.alpha = Mathf.Lerp(cg.alpha, 1, Time.deltaTime * 2);
            image.sizeDelta = Vector3.Lerp(image.sizeDelta, new Vector3(5, 100), Time.deltaTime * 2);
            transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(200, 0), Time.deltaTime * 2);
            bar.localPosition = new Vector3(-50 - (Mathf.Floor((Time.time - start)*3%3)*10), 0, 0);
            yield return new WaitForEndOfFrame();
        }
    }
}
