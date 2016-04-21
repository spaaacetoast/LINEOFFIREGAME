using UnityEngine;
using System.Collections;
using AngryRain;
using UnityEngine.UI;
using AngryRain.Multiplayer;

public class PlayerTag : MonoBehaviour 
{
    public Camera targetCamera;

    public ClientPlayer targetPlayer;
    public ClientPlayer cameraOwner;

    public float maxRotation = 15;

    private Text textObject;
    private Image arrowImage;

    public void Init()
    {
        textObject = transform.Find("Text").GetComponent<Text>();
        arrowImage = transform.Find("Image").GetComponent<Image>();
    }

    void Update()
    {
        if (targetPlayer.isConnected && targetPlayer.isAlive)
        {
            Transform targetTransform = targetPlayer.playerManager.playerCharacter.transform;
            Vector3 screenPos = targetCamera.WorldToScreenPoint(targetTransform.position + (Vector3.up * 1.5f));
            transform.eulerAngles = new Vector3(0, (screenPos.x - (Screen.width / 2)) * 45 / Screen.width, 0);
            screenPos.z = 0;
            screenPos.x -= 2560 / 2;
            screenPos.y -= 1440 / 2;
            transform.localPosition = screenPos;
        }
    }

    void OnEnable()
    {
        UpdateNameTag();
    }

    public void UpdateNameTag()
    {
        if (targetPlayer != null && targetPlayer.isConnected && targetPlayer.isAlive)
        {
            gameObject.SetActive(true);
            textObject.text = targetPlayer.playerName;

            bool sameTeam = MultiplayerManager.AreWeOnTheSameTeam(cameraOwner, targetPlayer);
            textObject.color = sameTeam ? new Color(0, 0, 1, 0.5f) : new Color(1, 0, 0, 0.5f);
            arrowImage.color = sameTeam ? new Color(0, 0, 1, 0.5f) : new Color(1, 0, 0, 0.5f);

            return;
        }

        gameObject.SetActive(false);
    }
}
