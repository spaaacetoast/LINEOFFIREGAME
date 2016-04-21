using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AngryRain;
using AngryRain.Multiplayer;
using System.Linq;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour 
{
    public static Scoreboard instance;

    public GameObject teamBoard;
    public GameObject playerRecord;

    GameObject scoreboard;
    PlayerControllerGUI playerGUI;

    bool isEnabled;
    float lastActiveTime;

    List<GameObject> allTeamBoards = new List<GameObject>();
    List<GameObject> allPlayerRecords = new List<GameObject>();

    void Awake()
    {
        scoreboard = transform.Find("Menus/scoreboard").gameObject;
        playerGUI = GetComponent<PlayerControllerGUI>();
        instance = this;
        SetActive(false);
    }

    void Update()
    {
        bool buttonScore = playerGUI.localPlayer.playerInput.GetButton("Scoreboard");
        if (isEnabled != buttonScore) { SetActive(buttonScore); UpdateScore(); }
        if (isEnabled && lastActiveTime <= Time.time + 0.25f)
            UpdateScore();
    }

    public void SetActive(bool active)
    {
        isEnabled = active;
        scoreboard.SetActive(active);
    }

    public void SwitchActive()
    {
        SetActive(!isEnabled);
    }

    public void InitScore()
    {
        for (int i = 0; i < allTeamBoards.Count; i++)
            Destroy(allTeamBoards[i]);
        for (int i = 0; i < allPlayerRecords.Count; i++)
            Destroy(allPlayerRecords[i]);

        allTeamBoards.Clear();
        allPlayerRecords.Clear();

        int numberTeam = MultiplayerManager.matchSettings.modeSettings.matchSettings.numberTeams;
        for (int i = 0; i < numberTeam; i++)
        {
            GameObject board = Instantiate(teamBoard);
            board.transform.SetParent(teamBoard.transform.parent, false);
            board.SetActive(true);
            allTeamBoards.Add(board);
        }

        for (int i = 0; i < MultiplayerManager.GetPlayers().Length; i++)
        {
            GameObject player = Instantiate(playerRecord);
            player.transform.SetParent(playerRecord.transform.parent, false);
            allPlayerRecords.Add(player);
            player.SetActive(true);
        }
    }

    public void UpdateScore()
    {
        List<ClientPlayer> SortedList = MultiplayerManager.GetPlayers().OrderBy(o => o.score).ToList();
        for (int i = 0; i < allPlayerRecords.Count; i++)
        {
            GameObject record = allPlayerRecords[i];
            ClientPlayer player = MultiplayerManager.GetPlayers()[i];
            if (player.isConnected)
            {
                record.SetActive(true);

                if (player.team == null)
                    record.transform.SetParent(allTeamBoards[0].transform, false);
                else
                    record.transform.SetParent(allTeamBoards[player.team.index].transform, false);
            }
            else
                record.SetActive(false);

            for (int x = 0; x < SortedList.Count; x++)
            {
                if (SortedList[x] == player)
                    record.transform.SetSiblingIndex(2 + x);
            }

            record.transform.Find("Player").GetComponent<Text>().text = player.playerName;
            record.transform.Find("Score").GetComponent<Text>().text = player.score.ToString();
            record.transform.Find("Deaths").GetComponent<Text>().text = player.deaths.ToString();
            record.transform.Find("Kills").GetComponent<Text>().text = player.kills.ToString();
            record.transform.Find("Ping").GetComponent<Text>().text = player.ping.ToString();

            record.GetComponent<Image>().color = player.isMe ? new Color(0.1f, 0.1f, 0.1f, 1) : new Color(0.3f, 0.3f, 0, 1);    
        }
    }
}
