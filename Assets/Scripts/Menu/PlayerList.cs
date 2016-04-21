using UnityEngine;
using System.Collections;
using TNet;
using UnityEngine.UI;

namespace AngryRain.Menu
{
    public static class Settings
    {
        public static bool updatePlayerList;
        public static bool updateLobbyInformation;
    }

    public class PlayerList : MonoBehaviour
    {
        public GameObject playerPanel;
        public List<GameObject> allPlayers = new List<GameObject>();

        void Update()
        {
            if (Settings.updatePlayerList)
            {
                Settings.updatePlayerList = false;
                UpdatePlayerList();
            }
        }

        public void UpdatePlayerList()
        {
            //Remove all players first
            for (int i = 0; i < allPlayers.Count; i++)
                Destroy(allPlayers[i]);
            allPlayers.Clear();

            //Populate the list with new copies
            int count = Multiplayer.MultiplayerManager.GetPlayers().Length;
            for (int i = 0; i < count; i++)
            {
                if (Multiplayer.MultiplayerManager.GetPlayers()[i].isConnected)
                {
                    GameObject panel = Instantiate(playerPanel) as GameObject;
                    allPlayers.Add(panel);
                    panel.SetActive(true);
                    panel.transform.SetParent(playerPanel.transform.parent, false);
                    panel.transform.Find("Text").GetComponent<Text>().text = Multiplayer.MultiplayerManager.GetPlayer(i).playerName;
                }
            }
        }
    }
}