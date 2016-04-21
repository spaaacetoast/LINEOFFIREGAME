using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AngryRain.Multiplayer
{
    [System.Serializable]
    public class Team
    {
        public string name;
        public int index;

        public List<ClientPlayer> allPlayers = new List<ClientPlayer>();
        public List<Squad> allSquads = new List<Squad>();

        public int teamScore { get { int score = 0; for (int i = 0; i < allPlayers.Count; i++) { score += allPlayers[i].score; } return score; } }
        public int teamKills { get { int score = 0; for (int i = 0; i < allPlayers.Count; i++) { score += allPlayers[i].kills; } return score; } }
    }

    [System.Serializable]
    public class Squad
    {
        public string name;
        public int mTeam;
        public System.Collections.Generic.List<ClientPlayer> allPlayers = new System.Collections.Generic.List<ClientPlayer>();
    }
}