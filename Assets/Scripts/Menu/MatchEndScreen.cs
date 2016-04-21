using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AngryRain;
using AngryRain.Multiplayer;
using UnityEngine.SceneManagement;

public class MatchEndScreen : MonoBehaviour 
{
    Image[] blocks = new Image[4];

    void OnEnable()
    {
        blocks[0] = transform.Find("Block1").GetComponent<Image>();
        blocks[1] = transform.Find("Block2").GetComponent<Image>();
        blocks[2] = transform.Find("Block3").GetComponent<Image>();
        blocks[3] = transform.Find("Block4").GetComponent<Image>();
        StartCoroutine(TimedReturnToMainMenu());
    }

    IEnumerator TimedReturnToMainMenu()
    {
        float time = Time.unscaledTime;

        for (int i = 0; i < 4; i++)
            blocks[i].color = new Color(1, 1, 1, 0);

        while (Time.unscaledTime - time < 3)
        {
            Time.timeScale = Mathf.Lerp(1, 0, (Time.unscaledTime - time) / 3);
            yield return new WaitForEndOfFrame();
        }
        Time.timeScale = 0;

        time = Time.unscaledTime;
        LocalPlayerManager.localPlayers[0].playerCamera.EnableCamera(CameraResetType.None, false);
        PlayerCamera camera = LocalPlayerManager.localPlayers[0].playerCamera;
        Transform transform = camera.transform;
        LocalPlayerManager.localPlayers[0].clientPlayer.playerManager.SetCharacterRenderingMode(PlayerManager.CharacterRenderingMode.ThirdPerson);

        camera.enabled = false;
        camera.camera.fieldOfView = 40;

        while (Time.unscaledTime - time < 3)
        {
            transform.position = LocalPlayerManager.localPlayers[0].clientPlayer.playerManager.playerCharacter.transform.position + (new Vector3(0,0,-0.1f)*(Time.unscaledTime - time) + new Vector3(0,1.5f,-4));
            transform.rotation = Quaternion.identity;
            yield return new WaitForEndOfFrame();
        }

        SystemGameGUI.StaticSetLoadScreenStatus(true);
        Time.timeScale = 1;

        yield return new WaitForSeconds(3);

        AngryRain.ClientPlayer[] allPlayers = AngryRain.Multiplayer.MultiplayerManager.GetPlayers();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i].isConnected)
            {
                allPlayers[i].playerManager.playerCharacter.gameObject.SetActive(false);
                if (allPlayers[i].playerManager.playerController)
                    allPlayers[i].playerManager.playerController.gameObject.SetActive(false);
            }
        }

        AsyncOperation async = SceneManager.LoadSceneAsync("mainmenu");
        async.allowSceneActivation = false;
        while (async.progress < 0.9f)
        {
            print(async.progress);
            yield return null;
        }
        async.allowSceneActivation = true;

        yield return new WaitForSeconds(1);

        gameObject.SetActive(false);
        SystemGameGUI.StaticSetLoadScreenStatus(false);

        NavigationController.NavigateTo("lobby");

        AngryRain.Menu.Settings.updateLobbyInformation = true;
        AngryRain.Menu.Settings.updatePlayerList = true;
    }
}
