using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using AngryRain;
using AngryRain.Multiplayer;

public class CustomizationPanel : MonoBehaviour
{
    public PlayerControllerGUI playerGUI;

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        StartCoroutine(HandleEnableAnimation());
    }

    void OnDisable()
    {
        DisableUnmovablePlayer();
        DisableCharacter();
    }

    IEnumerator HandleEnableAnimation()
    {
        EnableUnmovablePlayer();
        EnableCharacter();

        playerGUI.localPlayer.playerCamera.transform.rotation = Quaternion.Euler(0, 90, 0);
        playerGUI.localPlayer.playerCamera.camera.fieldOfView = 7f;

        float startTime = Time.time;
        while (Time.time - startTime < 10)
        {
            float t = Time.time - startTime;
            playerGUI.localPlayer.playerCamera.transform.rotation = Quaternion.Lerp(playerGUI.localPlayer.playerCamera.transform.rotation, Quaternion.Euler(0, 180, 0), Time.deltaTime * 5);
            yield return new WaitForEndOfFrame();
        }
    }

    void EnableUnmovablePlayer()
    {
        playerGUI.localPlayer.playerController.gameObject.SetActive(true);
        playerGUI.localPlayer.playerController.SwitchWeapon(CustomizationManager.WeaponSpot.None, true);
        playerGUI.localPlayer.playerController.playerMovement.rigidbody.isKinematic = true;
        playerGUI.localPlayer.playerController.playerMovement.rigidbody.position = new Vector3(0, -500, 0);

        playerGUI.localPlayer.playerController.playerVariables.canAim = false;
        playerGUI.localPlayer.playerController.playerVariables.canShoot = false;
        playerGUI.localPlayer.playerController.objectHolder.cameraOfffsetTransform.localPosition = Vector3.zero;

        playerGUI.localPlayer.playerCamera.EnableCamera(CameraResetType.PositionAndRotation, false);
        playerGUI.localPlayer.playerCamera.cameraSettings.cameraType = AngryRain.CameraType.None;
        playerGUI.localPlayer.playerCamera.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
        playerGUI.localPlayer.playerCamera.camera.clearFlags = CameraClearFlags.SolidColor;
        playerGUI.localPlayer.playerCamera.camera.farClipPlane = 50;
    }

    void DisableUnmovablePlayer()
    {
        playerGUI.localPlayer.playerCamera.EnableCamera(CameraResetType.None, false);
        playerGUI.localPlayer.playerCamera.cameraSettings.cameraType = AngryRain.CameraType.None;
        playerGUI.localPlayer.playerCamera.camera.clearFlags = CameraClearFlags.Skybox;
        playerGUI.localPlayer.playerCamera.camera.farClipPlane = 1000;
    }

    void EnableCharacter()
    {
        PlayerManager manager = playerGUI.localPlayer.clientPlayer.playerManager;

        manager.playerCharacter.gameObject.SetActive(true);
        manager.playerCharacter.transform.SetParent(null);

        manager.playerCharacter.transform.position = new Vector3(0, -500.9f, -18);
        manager.playerCharacter.transform.rotation = Quaternion.Euler(0,-25,0);

        manager.playerCharacter.animator.Play("WeaponLowered", 0);
        manager.playerCharacter.animator.SetLayerWeight(2, 0);

        manager.SetCharacterRenderingForSplitscreen(false);
        manager.SetCharacterRenderingMode(PlayerManager.CharacterRenderingMode.ThirdPerson);

        manager.Client_SetRagdoll(false, null);
    }

    void DisableCharacter()
    {
        PlayerManager manager = playerGUI.localPlayer.clientPlayer.playerManager;

        manager.playerCharacter.gameObject.SetActive(false);
        manager.ResetParents();
    }

    bool isInitialized;
    void Initialize()
    {
        if (isInitialized)
            return;



        isInitialized = true;
    }

    #region Navigation

    public void NavigationCharacterMenu()
    {
        EnableCharacter();
    }

    public void NavigationWeaponMenu()
    {
        DisableCharacter();
    }

    string currentSelectedWeapon = "";

    public void NavigationWeaponMenu_WeaponTypeSelection(bool next)
    {

    }

    public void NavigationWeaponMenu_WeaponSelection(bool next)
    {

    }

    public void NavigationWeaponMenu_WeaponCustomize()
    {

    }

    #endregion
}
