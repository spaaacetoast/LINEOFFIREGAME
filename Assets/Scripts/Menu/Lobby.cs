using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TNet;
using AngryRain.Multiplayer;

namespace AngryRain.Menu
{
    public class Lobby : TNBehaviour
    {
        public Text timeText;

        public Text mapText;
        public Text modeText;

        public float waitTime = 60;
        private float startTime = 0;

        bool startedMatch;

        void Start()
        {
            startTime = Time.time;
            if (!TNManager.isHosting)
                tno.Send("ServerRequestTime", Target.Host, TNManager.player.id);
            else
                Settings.updateLobbyInformation = true;
        }

        void Update()
        {
            int leftOverTime = Mathf.Max((int)(waitTime - (Time.time - startTime)), 0);
            timeText.text = "MATCH STARTS IN " + leftOverTime;

            if (TNManager.isHosting)
            {
                if (leftOverTime == 0 && !startedMatch || Input.GetKeyDown(KeyCode.Return))
                {
                    startedMatch = true;
                    Multiplayer.MultiplayerManager.instance.Server_StartMatch();
                }
            }

            if(Settings.updateLobbyInformation)
            {
                Settings.updateLobbyInformation = false;
                mapText.text = MultiplayerManager.matchSettings.mapSelection.selectedMap.mapName;
                modeText.text = MultiplayerManager.matchSettings.modeSettings.gamemodeName;
            }
        }

        [RFC]
        public void ServerRequestTime(int player)        
        {
            tno.Send("ClientUpdateTime", player, waitTime - (Time.time - startTime));
        }

        [RFC]
        public void ClientUpdateTime(float time)
        {
            startTime = Time.time;
            waitTime = time;
        }
    }
}