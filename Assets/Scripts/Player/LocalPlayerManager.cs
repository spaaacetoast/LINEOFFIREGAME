using UnityEngine;
using System.Collections;
using AngryRain.Multiplayer.LevelEditor;
using TNet;
using XInputDotNetPure;

namespace AngryRain
{
    public class LocalPlayerManager
    {
        public static List<LocalPlayer> localPlayers = new List<LocalPlayer>();

        public static void LoadPreferences()
        {
            localPlayers[0].playerName = PlayerPrefs.GetString("mpname");
            if (Application.isEditor)
                localPlayers[0].playerName = "Editor-" + localPlayers[0].playerName;
            TNManager.playerName = localPlayers[0].playerName;
        }
        
        public static void SavePreferences()
        {
            PlayerPrefs.SetString("mpname", localPlayers[0].playerName.Replace("Editor-", ""));
        }
    }

    public class LocalPlayer
    {
        public string playerName = ""; //What is my name
        public int playerIndex = 0; //Local player index, For splitscreen
        public ClientPlayer clientPlayer; //What is my clientPlayer, when its null not connected or BUG

        public PlayerController playerController { get { return clientPlayer.playerManager.playerController; } }
        public PlayerControllerGUI playerGUI { get { return PlayerControllerGUI.allInstances[playerIndex]; } }

        //Input
        public Rewired.Player playerInput;
        public Rewired.ControllerType controllerType = Rewired.ControllerType.Keyboard;

        public PlayerCamera playerCamera; //Wich camera is mine

        public PlayerMode playerMode = PlayerMode.normal; //PlayerMode, Are we flying or walking or in a vehicle etc..

        //Level Editor Variables
        public bool enableLevelEditor; //Is the level editor enabled
    }

    public class ClientPlayer
    {
        public string playerName = "";

        public bool isHost = false;
        public bool isConnected = false;
        public bool isMe = false;

        public int listIndex, lPlayerIndex, mPlayerID, ping;

        public TNet.Player tPlayer;//This TNET Player isntance

        public Multiplayer.PlayerManager playerManager;

        public bool enableSpawnScreen = false;

        public Multiplayer.Team team;
        public Multiplayer.Squad squad;

        public MultiplayerVehicle vehicle = null;
        public MultiplayerVehicle.VehicleSeat vehicleSeat = null;

        public LevelObjectManager currentHoldingObject = null;

        public bool isVisible;

        public int kills, deaths, score;
        public float health = 0;
        public bool isAlive = false;

        public PlayerClass playerClass = PlayerClass.Assault;
    }

    public enum PlayerMode
    {
        normal,
        vehicle,
        flymode
    }
}