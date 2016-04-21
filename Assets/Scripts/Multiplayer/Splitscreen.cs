using UnityEngine;
using System.Collections;

namespace AngryRain.Multiplayer
{
    /// <summary>
    /// Static class with methods for supporting and managing the splitscreen stuff
    /// </summary>
    public class Splitscreen
    {
        /// <summary>
        /// Returns a Rect with the screen constraints for the specific player depending on how many LocalPlayers there are
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public static Rect GetRect(int playerIndex)
        {
            int count = LocalPlayerManager.localPlayers.size;

            switch (count)
            {
                case 1:
                    return new Rect(0,0,Screen.width, Screen.height);
                case 2:
                    return new Rect(0, Screen.height * 0.5f * playerIndex, Screen.width, Screen.height * 0.5f);
                case 3:
                    return playerIndex == 0 ? new Rect(0,0,Screen.width, Screen.height * 0.5f) : new Rect(Screen.width * 0.5f * (playerIndex-1), Screen.height * 0.5f, Screen.width * 0.5f, Screen.height * 0.5f);
                case 4:
                    return new Rect(Screen.width * 0.5f * (playerIndex % 2), Screen.height * 0.5f * (int)(playerIndex / 3), Screen.width * 0.5f, Screen.height * 0.5f);
            }

            return new Rect();
        }

        /// <summary>
        /// This must be called every time the status of a renderer on the player changes
        /// </summary>
        public static void UpdateCamerasForSplitscreenRendering()
        {
            for (int i = 0; i < LocalPlayerManager.localPlayers.size; i++)
            {
                PlayerCamera cam = LocalPlayerManager.localPlayers[i].playerCamera;
                cam.Initialize();

                //Check if camera has been initialized already
                ExcludeObjectRendering eor = cam.GetComponent<ExcludeObjectRendering>();

                if (eor == null)
                    eor = cam.gameObject.AddComponent<ExcludeObjectRendering>();

                TNet.List<Renderer> excluded = new TNet.List<Renderer>();
                TNet.List<Renderer> included = new TNet.List<Renderer>();

                for (int p = 0; p < LocalPlayerManager.localPlayers.size; p++)
                {
                    PlayerManager pMan = LocalPlayerManager.localPlayers[p].clientPlayer.playerManager;

                    GameObject character = pMan.playerCharacter.gameObject;
                    GameObject playerCon = pMan.playerController.gameObject;

                    Renderer[] allCharacterRenderers = character.GetComponentsInChildren<Renderer>();
                    Renderer[] allPlayerConRenderers = playerCon.GetComponentsInChildren<Renderer>();

                    if (LocalPlayerManager.localPlayers[p].playerCamera != cam)
                    {
                        for (int r = 0; r < allPlayerConRenderers.Length; r++)
                            excluded.Add(allPlayerConRenderers[r]);
                    }
                    else
                    {
                        for (int r = 0; r < allPlayerConRenderers.Length; r++)
                            included.Add(allPlayerConRenderers[r]);

                    }
                }

                eor.excludedObjects = excluded;
                eor.includedObjects = included;
            }
        }
    }
}