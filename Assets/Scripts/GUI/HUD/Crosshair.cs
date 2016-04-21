using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using AngryRain;


public class Crosshair : MonoBehaviour 
{
    public PlayerControllerGUI playerControllerGUI;

    public RectTransform rectTransform { private set; get; }
    public CanvasGroup canvasGroup { private set; get; }

    public float multiplier;

    public float recoilMultiplier = 1;

    private float visibility = 0;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        PlayerController pc = playerControllerGUI.localPlayer.playerController;
        bool shouldEnable = !(pc.playerVariables.isReloading || pc.playerVariables.isAiming || pc.playerMovement.isRunning);
        visibility = Mathf.MoveTowards(visibility, shouldEnable ? 1 : 0, Time.deltaTime * 5);
        UpdateCrosshair();
    }

    void UpdateCrosshair()
    {
        float range = visibility * multiplier;

        if (playerControllerGUI.localPlayer.playerController.weaponSettings.currentWeapon != null)
            range *= playerControllerGUI.localPlayer.playerController.weaponSettings.currentWeapon.fireSettings.sprayRate*2;

        rectTransform.sizeDelta = new Vector2(range, range);

        transform.localEulerAngles = new Vector3(0, 0, visibility * 90);
        canvasGroup.alpha = Mathf.Lerp(0, 0.5f, visibility);
    }
}
