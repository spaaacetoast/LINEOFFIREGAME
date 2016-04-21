using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using AngryRain.Multiplayer.LevelEditor;
using TNet;
using UnityEngine.SceneManagement;

namespace AngryRain.Multiplayer
{
    public class MultiplayerManager : TNBehaviour
    {
        public static MultiplayerManager instance = null;
        public static MultiplayerMatchSettings matchSettings = new MultiplayerMatchSettings();

        public TNManager TNManager;

        public ObjectHolder objectHolder = new ObjectHolder();

        public System.Collections.Generic.List<Map> allMaps = new System.Collections.Generic.List<Map>();

        public ClientPlayer[] allPlayers = new ClientPlayer[0];
        public List<Team> allTeams = new List<Team>();

        public PlayerController playerControllerPrefab;
        public PlayerManager playerManagerPrefab;
        public PlayerManager[] allInstantiatedPlayerManagers = new PlayerManager[0];

        public static int showDebug;

        public LayerMask playerVisibilityLayers;//Used in the player spotting action

        public float serverTime;

        #region MonoBehaviours

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(transform.parent.gameObject);

            LocalPlayer localPlayer = new LocalPlayer() { playerName = PlayerPrefs.GetString("playername", "quest" + UnityEngine.Random.Range(1, 1000)) };
            localPlayer.playerInput = Rewired.ReInput.players.GetPlayer(localPlayer.playerIndex);
            localPlayer.playerInput.isPlaying = true;
            LocalPlayerManager.localPlayers.Add(localPlayer);

            OptionManager.LoadOptions();
            LocalPlayerManager.LoadPreferences();

            //Reset player to Keyboard and Mouse input
            localPlayer.playerInput.controllers.ClearAllControllers();
            if (OptionManager.currentOptions.playerInputSettings[0].inputType == InputType.Controller)
                localPlayer.playerInput.controllers.AddController(Rewired.ReInput.controllers.GetControllers(Rewired.ControllerType.Joystick)[0], true);
            else
            {
                localPlayer.playerInput.controllers.AddController(Rewired.ReInput.controllers.Keyboard, true);
                localPlayer.playerInput.controllers.AddController(Rewired.ReInput.controllers.Mouse, true);
            }

            TNManager.rebuildMethodList = true;
        }

        private IEnumerator Start()
        {
            TNManager.AddRCCs(this);
            if (SceneManager.GetActiveScene().name != "mainmenu")
            {
                if (offlineMultiplayer.quickJoinServer)
                {
                    ConnectToIP(offlineMultiplayer.targetServerIP);
                }
                else
                {
                    MultiplayerMatchSettings mmSettings = new MultiplayerMatchSettings() {hasServerStarted = true};
                    mmSettings.mapRotation.Add(new MultiplayerMatchSettings.MapSelection()
                    {
                        gameMode = new GameMode.CustomSettings()
                        {
                            gamemodeType = offlineMultiplayer.hostGameMode,
                            gamemodeName = offlineMultiplayer.hostGameModeName,
                            isDefault = true,
                            matchSettings = new GameMode.MatchSettings()
                            {
                                isTeamMode = offlineMultiplayer.hostGameMode != GameModeType.Deathmatch,
                                numberTeams = offlineMultiplayer.hostGameMode == GameModeType.Deathmatch ? 1 : 2
                            }
                        },
                        selectedMap = GetMap("", SceneManager.GetActiveScene().name)
                    });

                    StartServer(mmSettings);
                    yield return new WaitForSeconds(1);
                    if (TNManager.isHosting)
                        OnLevelWasLoaded();
                }
            }
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.O))
            {
                AddLocalPlayer();
            }

            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                if (Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (Cursor.visible)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnApplicationQuit()
        {
            instance = null;
            LocalPlayerManager.SavePreferences();

            TNManager.Disconnect();
            TNServerInstance.Stop();
        }

        public string SetDebug(params string[] args)
        {
            if (args.Length != 1)
                return "the amount of parameters giving is not correct, debug.level {INT}";

            int i = 0;
            try
            {
                i = int.Parse(args[0]);
            }
            catch (System.FormatException e)
            {
                return "the input giving was not a number, " + e;
            }

            showDebug = i;
            return "debug level set to " + i;
        }

        #endregion

        #region Connection

        public static bool StartServer(MultiplayerMatchSettings ms)
        {
            bool started = TNServerInstance.Start(2552, 2552);
            matchSettings = ms;
            TNManager.Connect("127.0.0.1", 2552);
            TNManager.noDelay = true;
            return started;
        }

        public static void StopServer()
        {
            TNServerInstance.Stop();
        }

        public static void DisconnectFromServer()
        {
            TNManager.Disconnect();
        }

        public static string ConnectToIP(params string[] args)
        {
            if (args.Length != 2)
                return "the amount of parameters giving is not correct, net.connect {IP, PORT}";

            try
            {
                //int port = int.Parse(args[1]);
                TNManager.Connect(args[0], int.Parse(args[1]));
                return "connecting to " + args[0] + ":" + args[1];
            }
            catch (System.FormatException e)
            {
                return "the port giving was not a number, " + e;
            }
        }

        public static void ConnectToIP(string ip)
        {
            TNManager.Connect(ip);
        }

        private void OnNetworkError(string error)
        {
            Debug.LogError(error);
        }

        private void OnNetworkConnect(bool success, string error)
        {
            try
            {
                for (int i = 0; i < instance.allPlayers.Length; i++)
                    instance.allPlayers[i] = null;

                if (instance.allInstantiatedPlayerManagers.Length > 0)
                    foreach (PlayerManager pm in instance.allInstantiatedPlayerManagers)
                        if (pm != null && pm.thisGameObject)
                            Destroy(pm.thisGameObject);

                if (!success)
                {
                    Debug.LogError(error);

                    if (SceneManager.GetActiveScene().name == "mainmenu")
                    {
                        NavigationController.SetMessage(false);
                        NavigationController.SetError(error);
                        NavigationController.NavigateTo("multiplayer menu");
                    }
                    else
                    {
                        
                    }
                }
                else
                {
                    if (SceneManager.GetActiveScene().name == "mainmenu")
                    {
                        NavigationController.SetMessage("Joining channel...");
                    }
                    TNManager.JoinChannel(1, null);
                }
            }
            catch (System.Exception es)
            {
                Debug.LogError(es.StackTrace);
            }
        }

        private void OnNetworkJoinChannel(bool success, string message)
        {
            try
            {
                if (success)
                {
                    if (TNManager.isHosting)
                    {
                        TNManager.SetPlayerLimit(matchSettings.serverSettings.maxPlayers);

                        allPlayers = new ClientPlayer[matchSettings.serverSettings.maxPlayers];
                        allInstantiatedPlayerManagers = new PlayerManager[matchSettings.serverSettings.maxPlayers];
                        int c = matchSettings.serverSettings.maxPlayers;
                        for (int i = 0; i < c; i++)
                        {
                            allPlayers[i] = new ClientPlayer();
                            allPlayers[i].isConnected = false;
                            allPlayers[i].listIndex = i;

                            TNManager.CreateEx(11, true, instance.playerManagerPrefab.thisGameObject, i, matchSettings.serverSettings.maxPlayers);
                        }

                        Server_HandlePlayerJoinRequest(TNManager.player.id, 0);
                    }

                    if (SceneManager.GetActiveScene().name == "mainmenu")
                    {
                        NavigationController.SetMessage(false);
                        NavigationController.NavigateTo("lobby");
                    }

                    StartCoroutine("SyncPing");
                }
                else
                {
                    //When cant connect, pop error
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.StackTrace);
            }
        }

        private void OnNetworkPlayerJoin(Player pl)
        {
            if (!TNManager.isHosting) return;

            tno.Send("Client_Sync", pl, 1, matchSettings.Serialize());
            if (matchSettings.hasServerStarted)
            {
                tno.Send("Client_UpdateServerInfo", pl, matchSettings.hasServerBeenLoaded);
                tno.Send("Client_StartMatch", pl);
            }
        }

        private void OnNetworkPlayerLeave(Player pl)
        {
            if (TNManager.isHosting)
            {
                try//Try removing the specific players
                {
                    System.Collections.Generic.List<ClientPlayer> mPlayers = GetPlayers(pl.id);
                    if (mPlayers.Count == 0)
                        throw new Exception("Nobody was found when removing.");
                    for (int i = 0; i < mPlayers.Count; i++)
                    {
                        Debug.Log("Player " + mPlayers[i].playerName + " has disconnected!");
                        mPlayers[i].playerManager.name = "_PlayerManager (" + mPlayers[i].listIndex + ")";
                        mPlayers[i].playerName = String.Empty;
                        mPlayers[i].isConnected = false;
                        mPlayers[i].tPlayer = null;
                        tno.Send("Client_UpdatePlayer", Target.Others, mPlayers[i].mPlayerID, true);
                    }
                }
                catch (Exception ex)//Check all players for if the tnetplayer is still connected to find out wich is who
                {
                    Debug.LogError("Removing player(s) did not work, checking all players for connection. \n" + ex);
                    for (int i = 0; i < allPlayers.Length; i++)
                    {
                        if (allPlayers[i].isConnected && (allPlayers[i].tPlayer == null || allPlayers[i].tPlayer.id == pl.id))
                        {
                            Debug.Log("Player " + allPlayers[i].playerName + " has disconnected!");
                            allPlayers[i].playerManager.name = "_PlayerManager ( " + allPlayers[i].listIndex + " )";
                            allPlayers[i].playerName = String.Empty;
                            allPlayers[i].isConnected = false;
                            allPlayers[i].tPlayer = null;
                            tno.Send("Client_UpdatePlayer", Target.Others, allPlayers[i].mPlayerID, true);

                            allPlayers[i].playerManager.Server_ReceiveDamageVelocity(allPlayers[i].mPlayerID, -2, Vector3.zero);
                        }
                    }
                }
            }
        }

        [RFC]
        private void Client_Sync(int t, byte[] array)
        {
            if (t == 0)
            {

            }
            if (t == 1)//Receive All settings and initialize players
            {
                matchSettings = Serializer.Deserialize<MultiplayerMatchSettings>(array);

                for (int i = 0; i < allPlayers.Length; i++)
                    allPlayers[i] = null;

                allPlayers = new ClientPlayer[matchSettings.serverSettings.maxPlayers];
                int c = matchSettings.serverSettings.maxPlayers;
                for (int i = 0; i < c; i++)
                {
                    allPlayers[i] = new ClientPlayer();
                    allPlayers[i].isConnected = false;
                    allPlayers[i].listIndex = i;
                }

                for (int i = 0; i < LocalPlayerManager.localPlayers.size; i++)
                {
                    tno.Send("Server_HandlePlayerJoinRequest", Target.Host, TNManager.player.id, i);
                }

                AngryRain.Menu.Settings.updateLobbyInformation = true;
            }
        }

        private void OnNetworkDisconnect()
        {            
            StartCoroutine(HandleDisconnect());
        }

        private IEnumerator HandleDisconnect()
        {
            if (SceneManager.GetActiveScene().name.Equals("mainmenu", System.StringComparison.OrdinalIgnoreCase))
                NavigationController.SetMessage("Disconnecting...");

            TNManager.Disconnect();
            TNServerInstance.Stop();

            yield return new WaitForSeconds(0.1f);//Remove All props
            
            yield return new WaitForSeconds(0.1f);//Remove playermanagers
            foreach (PlayerManager pManager in allInstantiatedPlayerManagers)
            {
                if (pManager.clientPlayer != null && !pManager.clientPlayer.isMe || pManager.clientPlayer == null)
                {
                    TNManager.Destroy(pManager.thisGameObject);
                }
            }
            yield return new WaitForSeconds(0.1f);//Remove player and reset all settings
            foreach (LocalPlayer lPlayer in LocalPlayerManager.localPlayers)
            {
                if (lPlayer.clientPlayer != null && lPlayer.clientPlayer.playerManager)
                {
                    if (lPlayer.clientPlayer.playerManager.playerController.gameObject)
                        Destroy(lPlayer.clientPlayer.playerManager.playerController.gameObject);
                    if (lPlayer.clientPlayer.playerManager.playerCharacter.gameObject)
                        Destroy(lPlayer.clientPlayer.playerManager.playerCharacter.gameObject);
                    if (lPlayer.clientPlayer.playerManager.thisGameObject)
                        TNManager.Destroy(lPlayer.clientPlayer.playerManager.thisGameObject);
                }
                lPlayer.clientPlayer = null;
                lPlayer.enableLevelEditor = false;
            }

            allTeams = new TNet.List<Team>();
            allPlayers = new ClientPlayer[0];
            instance.allInstantiatedPlayerManagers = new PlayerManager[0];
            matchSettings = null;
            matchSettings = new MultiplayerMatchSettings();
            yield return new WaitForSeconds(0.1f);//load mainmenu
            if (!SceneManager.GetActiveScene().name.Equals("mainmenu", System.StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.LoadSceneAsync("mainmenu");
            }
            else
            {
                NavigationController.SetMessage(false);
                NavigationController.NavigateTo("multiplayer menu");
            }
        }

        [RFC]
        private void Client_UpdatePlayer(int mPlayerID, bool remove)
        {
            try//Try removing the specific players
            {
                ClientPlayer mPlayer = GetPlayer(mPlayerID);
                if (remove)
                {
                    Debug.Log("Player " + mPlayer.playerName + " has disconnected!");
                    mPlayer.playerManager.name = "_PlayerManager (" + mPlayer.listIndex + ")";
                    mPlayer.playerName = String.Empty;
                    mPlayer.isConnected = false;
                    mPlayer.tPlayer = null;
                }
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        [RFC]
        private void Client_StartPingSync()
        {
            StartCoroutine("SyncPing");
        }

        private IEnumerator SyncPing()
        {
            while(LocalPlayerManager.localPlayers[0].clientPlayer == null)
                yield return new WaitForSeconds(0.25f);

            while (true)
            {
                tno.SendQuickly("Client_GetPing", Target.All, LocalPlayerManager.localPlayers[0].clientPlayer.listIndex, TNManager.ping);
                yield return new WaitForSeconds(0.25f);
            }
        }

        [RFC]
        private void Client_GetPing(int tPlayer, int ping)
        {
            allPings[tPlayer] = ping;
        }

        private static int[] allPings = new int[64];

        public static int GetPing(int tPlayer)
        {
            foreach (ClientPlayer t in instance.allPlayers)
                if (t.tPlayer.id == tPlayer)
                {
                    t.ping = allPings[t.listIndex];
                    return t.ping;
                }
            return 0;
        }

        #endregion

        #region Player Handeling

        private int lastID = 0;

        [RFC]
        public void Server_HandlePlayerJoinRequest(int playerID, int lPlayerIndex)
        {
            Player playerInfo = TNManager.GetPlayer(playerID);
            ClientPlayer mPlayer = null;
            int c = matchSettings.serverSettings.maxPlayers;
            if (lPlayerIndex == 0)
                for (int i = 0; i < c; i++)
                    if (allPlayers[i].isConnected)
                        tno.Send("Client_CreatePlayer", playerInfo, allPlayers[i].listIndex, allPlayers[i].mPlayerID, allPlayers[i].tPlayer.id, allPlayers[i].lPlayerIndex, allPlayers[i].isHost);

            for (int i = 0; i < c; i++)
            {
                ClientPlayer player = allPlayers[i];
                if (!player.isConnected)
                {
                    player.isConnected = true;
                    player.isHost = playerInfo == TNManager.player;
                    player.listIndex = i;
                    player.tPlayer = playerInfo;
                    player.lPlayerIndex = lPlayerIndex;

                    tno.Send("Client_CreatePlayer", Target.All, player.listIndex, lastID, playerID, lPlayerIndex, player.isHost);
                    lastID++;

                    mPlayer = player;

                    break;
                }
            }

            if (mPlayer == null)
            {
                //Kick Player cause no spots are free, Or 1/more MPlayers are glitched with (isConnected = true)
                tno.Send("Client_KickPlayer", Target.All, playerInfo, -1);
            }
            else
            {
                tno.Send("Client_StartPingSync", playerInfo);
            }
        }

        [RFC]
        public void Server_HandlePlayerLeaveRequest()
        {

        }

        [RFC]
        public void Client_CreatePlayer(int index, int playerID, int tpID, int lPlayerIndex, bool isHost)
        {
            try
            {
                Player playerInfo = TNManager.GetPlayer(tpID);
                ClientPlayer player = allPlayers[index];

                string playerName = playerInfo.name;
                if (lPlayerIndex != 0)
                    playerName += " (" + (lPlayerIndex + 1) + ")";

                player.isConnected = true;
                player.isHost = isHost;
                player.playerName = playerName;
                player.mPlayerID = playerID;
                player.tPlayer = playerInfo;
                player.lPlayerIndex = lPlayerIndex;
                player.isMe = player.tPlayer == TNManager.player;

                allInstantiatedPlayerManagers[player.listIndex].Initialize();

                if (TNManager.isHosting)
                    allInstantiatedPlayerManagers[player.listIndex].tno.ownerID = playerInfo.id;

                if (player.isMe)
                {
                    LocalPlayerManager.localPlayers[lPlayerIndex].clientPlayer = player;
                    GameObject pl = Instantiate(playerControllerPrefab.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
                    if (pl != null)
                    {
                        pl.SetActive(false);

                        PlayerController playerController = pl.GetComponent<PlayerController>();
                        player.playerManager.playerController = playerController;
                        playerController.playerManager = player.playerManager;
                        playerController.localPlayer = LocalPlayerManager.localPlayers[lPlayerIndex];

                        playerController.playerCamera.Initialize();
                        playerController.Initialize();
                        playerController.playerMovement.Initialize();

                        LocalPlayerManager.localPlayers[lPlayerIndex].playerCamera = playerController.playerCamera;
                        playerController.transform.parent = player.playerManager.transform;
                        playerController.gameObject.name = "PlayerController ( " + playerName + " )";

                        playerController.playerCamera.playerController = playerController;
                    }

                    PlayerControllerGUI.CreatePlayerGUI(lPlayerIndex);
                }

                player.playerManager.gameObject.name = "_PlayerManager ( " + playerName + " )";

                if (SceneManager.GetActiveScene().name == "mainmenu")
                {
                    AngryRain.Menu.Settings.updatePlayerList = true;
                }
                else
                {
                    PlayerControllerGUI.allInstances[0].AddText(playerName + " has joined the match");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        [RFC]
        public void Client_KickPlayer(int p, int index)
        {
            if (index != -1)
            {
                ClientPlayer player = allPlayers[index];
                player.isConnected = false;
            }

            if (p == TNManager.player.id)
            {
                TNManager.Disconnect();
            }
        }

        private Player GetTPlayerByTplayerID(int ID)
        {
            Player playerInfo = null;
            foreach (Player pl in TNManager.players)
                if (pl.id == ID)
                    playerInfo = pl;
            if (playerInfo == null && ID == TNManager.player.id) playerInfo = TNManager.player;
            return playerInfo;
        }

        #endregion

        #region Splitscreen

        private void AddLocalPlayer()
        {
            LocalPlayer newPlayer = new LocalPlayer();

            LocalPlayerManager.localPlayers.Add(newPlayer);

            newPlayer.playerName = LocalPlayerManager.localPlayers[0].playerName + " (" + (LocalPlayerManager.localPlayers.IndexOf(newPlayer) + 1) + ")";
            newPlayer.playerIndex = LocalPlayerManager.localPlayers.IndexOf(newPlayer);
            newPlayer.controllerType = Rewired.ControllerType.Joystick;

            newPlayer.playerInput = Rewired.ReInput.players.GetPlayer(newPlayer.playerIndex);
            newPlayer.playerInput.isPlaying = true;

            //get first available gamepad
            foreach(Rewired.Controller con in Rewired.ReInput.controllers.GetControllers(Rewired.ControllerType.Joystick))
            {
                bool isUsed = false;
                foreach(Rewired.Player player in Rewired.ReInput.players.AllPlayers)
                {
                    if(player.controllers.ContainsController(con))
                        isUsed = true;
                }

                if(!isUsed)
                {
                    newPlayer.playerInput.controllers.AddController(con, true);
                    break;
                }
            }
            
            tno.Send("Server_HandlePlayerJoinRequest", Target.Host, TNManager.playerID, newPlayer.playerIndex);
        }

        #endregion

        #region Match Sync

        public void Server_StartMatch()
        {
            tno.Send("Client_StartMatch", Target.All);
        }

        [RFC]
        private void Client_StartMatch()
        {
            StartCoroutine("Client_StartMatchIE");
        }

        public IEnumerator Client_StartMatchIE()
        {
            if (SceneManager.GetActiveScene().name != matchSettings.mapRotation[matchSettings.currentMapRotationIndex].selectedMap.sceneName)
            {
                SystemGameGUI.StaticSetLoadScreenStatus(true);
                TNManager.SetTimeout(300);
                matchSettings.hasServerStarted = true;
                float t = Time.time + 3;
                AsyncOperation async = SceneManager.LoadSceneAsync(matchSettings.mapRotation[matchSettings.currentMapRotationIndex].selectedMap.sceneName);
                async.allowSceneActivation = false;
                while (async.progress < 0.9f || t > Time.time)
                {
                    matchSettings.mapLoadingProgress = async.progress;
                    yield return null;
                }
                async.allowSceneActivation = true;
                yield return new WaitForSeconds(0.5f);
                SystemGameGUI.StaticSetLoadScreenStatus(false);
                TNManager.SetTimeout(10);
            }
            else
            {
                OnLevelWasLoaded();
            }
        }

        #endregion

        #region Match Handeling

        #region Spawning

        public void Local_RequestPlayerSpawn(int lPlayer)
        {
            Local_RequestPlayerSpawn(LocalPlayerManager.localPlayers[lPlayer]);
        }

        public void Local_RequestPlayerSpawn(LocalPlayer lPlayer)
        {
            PlayerClass pc = lPlayer.clientPlayer.playerClass;
            if (TNManager.isHosting) Server_HandelPlayerSpawn(lPlayer.clientPlayer.mPlayerID, (byte)pc); else tno.Send("Server_HandelPlayerSpawn", Target.Host, lPlayer.clientPlayer.mPlayerID, (byte)pc);
        }

        [RFC]
        private void Server_HandelPlayerSpawn(int playerID, byte playerClass)
        {
            if (LevelManager.cinameticEditorEnabled)
                playerClass = (byte)PlayerClass.Editor;

            ClientPlayer mPlayer = GetPlayer(playerID);

            LevelEditor.Spawnpoint SP = LevelEditor.Spawnpoint.allSpawnpoints[UnityEngine.Random.Range(0, LevelEditor.Spawnpoint.allSpawnpoints.Count)];

            ServerUpdateVariable(SyncTarget.alive, true, mPlayer, true);
            ServerUpdateVariable(SyncTarget.health, 100f, mPlayer, true);

            tno.Send("Client_SetSpawnScreen", mPlayer.tPlayer, false, mPlayer.mPlayerID);
            mPlayer.playerManager.tno.Send("Client_SpawnPlayer", Target.All, SP.thisPosition, SP.thisRotation, playerClass);
            mPlayer.playerManager.tno.Send("Client_SetTransform", Target.All, SP.thisPosition, SP.thisRotation, false);

            tno.Send("Client_HandelPlayerSpawn", Target.All, playerID, playerClass);
        }

        [RFC]
        public void Client_HandelPlayerSpawn(int playerID, byte playerClass)
        {
            ClientPlayer mPlayer = GetPlayer(playerID);
            if (mPlayer.isMe)
                Splitscreen.UpdateCamerasForSplitscreenRendering();
        }

        //bool isFirstSpawn = true;

        [RFC]
        public void Client_SetSpawnScreen(bool enableSpawnScreen, int playerID)
        {
            ClientPlayer cPlayer = GetPlayer(playerID);
            cPlayer.enableSpawnScreen = enableSpawnScreen;

            Cursor.visible = enableSpawnScreen;
            Cursor.lockState = enableSpawnScreen ? CursorLockMode.None : CursorLockMode.Locked;

            if (enableSpawnScreen)
            {
                PlayerControllerGUI.allInstances[cPlayer.lPlayerIndex].NavigateTo("quick spawnscreen");
                if (offlineMultiplayer.quickSpawn)
                {
                    //Local_RequestPlayerSpawn(0);
                }
            }
            else
            {
                PlayerControllerGUI.allInstances[cPlayer.lPlayerIndex].FullscreenFade(false);
                if (PlayerControllerGUI.allInstances[cPlayer.lPlayerIndex].currentMenu != null)
                    PlayerControllerGUI.allInstances[cPlayer.lPlayerIndex].DisableAllMenus();
            }
        }

        private IEnumerator HandleMatchIntro()
        {
            SystemGameGUI.StaticSetMatchIntro(matchSettings.modeSettings.gamemodeType.ToString(), matchSettings.modeSettings.gamemodeName,
                matchSettings.mapSelection.selectedMap.mapName, matchSettings.mapSelection.customLevel);
            SystemGameGUI.StaticPlayMatchIntro();
            yield return new WaitForSeconds(6);
        }

        #endregion

        #region Loading and Starting Match

        private void OnLevelWasLoaded()
        {
            if (instance != this || SceneManager.GetActiveScene().name == "mainmenu")
                return;

            if (TNManager.isHosting)
            {
                if (matchSettings.modeSettings.matchSettings.isTeamMode)
                    Server_StartTeamMatch();
                LevelEditor.LevelManager.LoadLevel(matchSettings.mapRotation[matchSettings.currentMapRotationIndex].customLevel);
                matchSettings.hasServerBeenLoaded = true;
                matchSettings.hasClientBeenLoaded = true;
                tno.Send("Client_UpdateServerInfo", Target.All, true);
                StartCoroutine(Server_ClientLoadedIE(TNManager.player));
            }
            else
            {
                tno.Send("Server_ClientLoaded", Target.Host, TNManager.player.id);
            }
            StartCoroutine(HandleMatchIntro());
        }

        [RFC]
        private void Server_ClientLoaded(int tnetPlayer)//When players join, Load level with player target and spawn all alive players. After that send loading done message
        {
            TNet.Player pl = GetTPlayerByTplayerID(tnetPlayer);
            StartCoroutine(Server_ClientLoadedIE(pl));
        }

        /// <summary>
        /// Called on the server. Server waits for the timer to finish and after that it will sync everybody and give everybody permission to spawn
        /// </summary>
        /// <param name="tnetPlayer"></param>
        /// <returns></returns>
        private IEnumerator Server_ClientLoadedIE(Player tnetPlayer)
        {
            if (tnetPlayer == TNManager.player)
            {
                //Add option to wait for all players to be loaded
                yield return new WaitForSeconds(matchSettings.serverWaitTime);
                matchSettings.matchStatus = MatchStatus.HasStarted;
            }

            while (!matchSettings.hasServerBeenLoaded)//Wait till we have loaded the match
                yield return null;

            //LevelEditor.LevelManager.LoadLevel(tnetPlayer);
            tno.Send("Client_UpdateClientInfo", tnetPlayer, true);
            yield return null;//Added for sync bug
            tno.Send("Client_SetMatchStatus", tnetPlayer, (byte)matchSettings.matchStatus);
            tno.Send("Client_Initialize", tnetPlayer);

            if (tnetPlayer != TNManager.player)
            {
                yield return null;
                SyncAllTeamsAndSquads(tnetPlayer);
            } 
            
            foreach (ClientPlayer mPlayer in allPlayers)
            {
                ServerUpdateVariable(SyncTarget.alive, true, mPlayer, true, tnetPlayer.id);
                ServerUpdateVariable(SyncTarget.health, 100f, mPlayer, true, tnetPlayer.id);
                if (mPlayer.isConnected && mPlayer.isAlive)
                {
                    mPlayer.playerManager.tno.Send("Client_SpawnPlayer", tnetPlayer, mPlayer.playerManager.multiplayerObject.GetState(0).pos, mPlayer.playerManager.multiplayerObject.GetState(0).rot, (byte)mPlayer.playerClass);

                    if (mPlayer.vehicle != null)//If player is in a vehicle, Send the client a vehicle join update
                        mPlayer.vehicle.Local_RequestVehicleUpdate(true, mPlayer, mPlayer.vehicleSeat.seatIndex);
                }
            }

            yield return new WaitForSeconds(8);

            //Enable spawning on this player
            foreach (ClientPlayer mPlayer in allPlayers)
            {
                if (mPlayer.isConnected && mPlayer.tPlayer == tnetPlayer)
                {
                    if (!LevelManager.cinameticEditorEnabled)
                        tno.Send("Client_SetSpawnScreen", mPlayer.tPlayer, true, mPlayer.mPlayerID);
                    else
                        Server_HandelPlayerSpawn(mPlayer.mPlayerID, (byte)PlayerClass.Editor);
                }
            }
        }

        [RFC]
        private void Client_UpdateServerInfo(bool hasLoaded)
        {
            matchSettings.hasServerBeenLoaded = hasLoaded;
        }

        [RFC]
        private void Client_UpdateClientInfo(bool hasLoaded)
        {
            matchSettings.hasClientBeenLoaded = hasLoaded;
        }

        [RFC]
        private void Client_SetMatchStatus(byte matchStatus)
        {
            matchSettings.matchStatus = (MatchStatus)matchStatus;
        }

        [RFC]
        private void Client_Initialize()
        {
            Scoreboard.instance.InitScore();
            PlayerControllerGUI.allInstances[0].InitializeNametags();
        }

        #endregion

        #region Sync Variables

        /// <summary>
        /// Server will update the selected typeVariable to all the clients, Only send BOOL, INT or FLOAT
        /// </summary>
        /// <param name="typeVariable"></param>
        /// <param name="var"></param>

        public void ServerUpdateVariable(SyncTarget typeVariable, object value, ClientPlayer updatePlayer, bool reliable)
        {
            if (TNManager.isHosting)
            {
                //int t = value.GetType().Equals(typeof(int)) ? 0 : value.GetType().Equals(typeof(float)) ? 1 : value.GetType().Equals(typeof(bool)) ? 2 : -1;

                ClientUpdateVariable((byte)typeVariable, value, updatePlayer.mPlayerID);

                if (reliable)
                    tno.Send(10, Target.Others, (byte)typeVariable, value, updatePlayer.mPlayerID);
                else
                    tno.SendQuickly(10, Target.Others, (byte)typeVariable, value, updatePlayer.mPlayerID);
            }
        }

        public void ServerUpdateVariable(SyncTarget typeVariable, object value, ClientPlayer updatePlayer, bool reliable, int tPlayerTarget)
        {
            if (TNManager.isHosting)
            {
                //int t = value.GetType().Equals(typeof(int)) ? 0 : value.GetType().Equals(typeof(float)) ? 1 : value.GetType().Equals(typeof(bool)) ? 2 : -1;

                /*if (t == -1)
                {
                    Debug.LogError("Value returned -1, Type is " + value.GetType() + ", Value is " + value);
                    return;
                }*/

                if (reliable)
                    tno.Send(10, TNManager.GetPlayer(tPlayerTarget), (byte)typeVariable, value, updatePlayer.mPlayerID);
                else
                    tno.SendQuickly(10, TNManager.GetPlayer(tPlayerTarget), (byte)typeVariable, value, updatePlayer.mPlayerID);
            }
        }

        [RFC(10)]
        public void ClientUpdateVariable(byte typeVariable, object value, int playerID)
        {
            try
            {
                ClientPlayer player = GetPlayer(playerID);
                SyncTarget typeVar = (SyncTarget)typeVariable;

                if (player != null)
                {
                    if (typeVar == SyncTarget.health)
                        player.health = (float)value;
                    else if (typeVar == SyncTarget.alive)
                        player.isAlive = (bool)value;
                    else if (typeVar == SyncTarget.kills)
                        player.kills = (int)value;
                    else if (typeVar == SyncTarget.deaths)
                        player.deaths = (int)value;
                    else if (typeVar == SyncTarget.score)
                        player.score = (int)value;
                    else
                        Debug.LogError(typeVariable + " " + typeVar + ", Something gone wrong!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public enum SyncTarget
        {
            health,
            alive,
            kills,
            deaths,
            score
        }

        #endregion

        #region Teams

        private void Server_StartTeamMatch()
        {
            SetupTeams();
            SyncAllTeamsAndSquads();
            DistributePlayers();
        }

        //TODO: Give friends with players more priority to join squads together
        private void DistributePlayers()
        {
            for (int i = 0; i < allPlayers.Length; i++)
                DistributePlayer(allPlayers[i]);
        }

        private void DistributePlayer(ClientPlayer player)
        {
            if (!player.isConnected || !matchSettings.modeSettings.matchSettings.isTeamMode)
                return;

            foreach (Team team in allTeams)
            {
                if (team.allPlayers.Contains(player))
                    team.allPlayers.Remove(player);

                foreach (Squad squad in team.allSquads)
                    if (squad.allPlayers.Contains(player))
                        squad.allPlayers.Remove(player);
            }

            int teamWithLowestPlayerCount = 0;
            int lowestPlayerCount = 64;
            int firstAvailableSquad = -1;

            for (int t = 0; t < allTeams.Count; t++)//Check for the team with the lowest player count
            {
                if (allTeams[t].allPlayers.Count < lowestPlayerCount)
                {
                    lowestPlayerCount = allTeams[t].allPlayers.Count;
                    teamWithLowestPlayerCount = t;
                }
            }

            allTeams[teamWithLowestPlayerCount].allPlayers.Add(player);//Add player to the team
            player.team = allTeams[teamWithLowestPlayerCount];

            for (int t = 0; t < allTeams[teamWithLowestPlayerCount].allSquads.Count; t++)//Check for the first squad with an open spot
            {
                if (allTeams[teamWithLowestPlayerCount].allSquads[t].allPlayers.Count < 4)
                {
                    firstAvailableSquad = t;
                    break;
                }
            }

            if (firstAvailableSquad != -1)
            {
                allTeams[teamWithLowestPlayerCount].allSquads[firstAvailableSquad].allPlayers.Add(player);
                player.squad = allTeams[teamWithLowestPlayerCount].allSquads[firstAvailableSquad];
            }

            //Sync
            tno.Send("AssignPlayerToTeam", Target.Others, player.mPlayerID, teamWithLowestPlayerCount, firstAvailableSquad);
        }

        private void SetupTeams()
        {
            int amount = matchSettings.modeSettings.matchSettings.numberTeams;
            print("setting up teams, " + amount);
            allTeams = new TNet.List<Team>();
            for (int i = 0; i < amount; i++)
            {
                Team team = new Team() { name = "Team" + (i + 1) };
                team.allSquads = new System.Collections.Generic.List<Squad>();
                for (int y = 0; y < 4; y++ )
                {
                    team.allSquads.Add(new Squad() { name = GameMode.objectiveNames[UnityEngine.Random.Range(0, GameMode.objectiveNames.Length)] });
                }
                allTeams.Add(team);
                team.index = allTeams.IndexOf(team);
            }
        }

        private void SyncAllTeamsAndSquads()
        {
            System.Collections.Generic.List<SyncTeamData> allTeamsData = new System.Collections.Generic.List<SyncTeamData>();

            foreach (Team team in allTeams)
            {
                System.Collections.Generic.List<int> teamPlayers = new System.Collections.Generic.List<int>();
                System.Collections.Generic.List<SyncSquadData> squads = new System.Collections.Generic.List<SyncSquadData>();

                foreach (ClientPlayer mPlayer in team.allPlayers)
                    teamPlayers.Add(mPlayer.mPlayerID);

                foreach (Squad squad in team.allSquads)
                {
                    System.Collections.Generic.List<int> squadPlayers = new System.Collections.Generic.List<int>();

                    foreach (ClientPlayer mPlayer in squad.allPlayers)
                        squadPlayers.Add(mPlayer.mPlayerID);

                    squads.Add(new SyncSquadData() { allPlayers = squadPlayers.ToArray(), squadName = squad.name });
                }

                allTeamsData.Add(new SyncTeamData() { allPlayers = teamPlayers.ToArray(), allSquads = squads.ToArray(), teamName = team.name });
            }

            tno.Send("Client_SyncTeams", Target.Others, Serializer.Serialize<System.Collections.Generic.List<SyncTeamData>>(allTeamsData));
        }

        private void SyncAllTeamsAndSquads(TNet.Player player)
        {
            System.Collections.Generic.List<SyncTeamData> allTeamsData = new System.Collections.Generic.List<SyncTeamData>();

            foreach (Team team in allTeams)
            {
                System.Collections.Generic.List<int> teamPlayers = new System.Collections.Generic.List<int>();
                System.Collections.Generic.List<SyncSquadData> squads = new System.Collections.Generic.List<SyncSquadData>();

                foreach (ClientPlayer mPlayer in team.allPlayers)
                    teamPlayers.Add(mPlayer.mPlayerID);

                foreach (Squad squad in team.allSquads)
                {
                    System.Collections.Generic.List<int> squadPlayers = new System.Collections.Generic.List<int>();

                    foreach (ClientPlayer mPlayer in squad.allPlayers)
                        squadPlayers.Add(mPlayer.mPlayerID);

                    squads.Add(new SyncSquadData() { allPlayers = squadPlayers.ToArray(), squadName = squad.name });
                }

                allTeamsData.Add(new SyncTeamData() { allPlayers = teamPlayers.ToArray(), allSquads = squads.ToArray(), teamName = team.name });
            }

            tno.Send("Client_SyncTeams", player, Serializer.Serialize<System.Collections.Generic.List<SyncTeamData>>(allTeamsData));
        }

        /// <summary>
        /// Assign a team to a specific player, use -1 for targetteam to remove a player from all teams e.g. when a player leaves
        /// </summary>
        /// <param name="mPlayer"></param>
        /// <param name="targetTeam"></param>
        [RFC]
        private void AssignPlayerToTeam(int mPlayerID, int targetTeam, int targetSquad)
        {
            ClientPlayer mPlayer = GetPlayer(mPlayerID);
            if (mPlayer == null)
                return;

            if (targetTeam == -1)//Remove player from all teams
            {
                mPlayer.team = null;
                allTeams[targetTeam].allPlayers.Remove(mPlayer);
                return;
            }

            //Search all teams and squads if the player is a member of and remove player from existing teams and squads
            foreach (Team team in allTeams)
            {
                if (team.allPlayers.Contains(mPlayer))
                    team.allPlayers.Remove(mPlayer);

                foreach (Squad squad in team.allSquads)
                    if (squad.allPlayers.Contains(mPlayer))
                        squad.allPlayers.Remove(mPlayer);
            }

            //Add Player to new team
            allTeams[targetTeam].allPlayers.Add(mPlayer);
            allTeams[targetTeam].allSquads[targetSquad].allPlayers.Add(mPlayer);
            mPlayer.team = allTeams[targetTeam];
            mPlayer.squad = allTeams[targetTeam].allSquads[targetSquad];
        }

        /// <summary>
        /// Sync every team, squad and player to be equal to the server.
        /// </summary>
        /// <param name="data"></param>
        [RFC]
        public void Client_SyncTeams(byte[] data)
        {
            System.Collections.Generic.List<SyncTeamData> allTeamsData = Serializer.Deserialize<System.Collections.Generic.List<SyncTeamData>>(data);

            allTeams = new TNet.List<Team>();//Reset everything before any old references are going to excist
            foreach (ClientPlayer mPlayer in allPlayers)
                mPlayer.team = null;

            int amount = allTeamsData.Count;
            for (int i = 0; i < amount; i++)
            {
                Team team = new Team() { name = allTeamsData[i].teamName };

                for (int p = 0; p < allTeamsData[i].allPlayers.Length; p++)
                {
                    team.allPlayers.Add(GetPlayer(allTeamsData[i].allPlayers[p]));//Adding players to the allPlayers list in team
                }

                for (int s = 0; s < allTeamsData[i].allSquads.Length; s++)
                {
                    Squad squad = new Squad() { name = allTeamsData[i].allSquads[s].squadName };

                    for (int p = 0; p < allTeamsData[i].allSquads[s].allPlayers.Length; p++)
                    {
                        squad.allPlayers.Add(GetPlayer(allTeamsData[i].allSquads[s].allPlayers[p]));//Adding players to their squads allPlayers list
                    }

                    team.allSquads.Add(squad);
                }

                allTeams.Add(team);
                team.index = allTeams.IndexOf(team);

                //Assigning the team to the players internal variables
                foreach (ClientPlayer mPlayer in team.allPlayers)
                {
                    mPlayer.team = team;
                }
            }
        }

        [RFC]
        public void Client_SyncTeam(byte[] data)
        {
            SyncTeamData teamData = Serializer.Deserialize<SyncTeamData>(data);


        }//STILL NEEEDS TO BEEE DONEEEEEEEE---------------------------------------------------------------------------------------------------------

        [System.Serializable]
        private class SyncTeamData
        {
            public int[] allPlayers;
            public string teamName;
            public SyncSquadData[] allSquads;
        }

        [System.Serializable]
        private class SyncSquadData
        {
            public int[] allPlayers;
            public string squadName;
        }

        #endregion

        #region Text Information

        public void AddText(string text)
        {
            tno.Send("Client_AddText", Target.All, text);
        }

        [RFC]
        private void Client_AddText(string text)
        {
            PlayerControllerGUI.allInstances[0].AddText(text);
        }

        public void AddKillText(string text, int killtype, int cPlayerID)
        {
            tno.Send("Client_AddKillText", Target.All, text, killtype, cPlayerID);
        }

        [RFC]
        private void Client_AddKillText(string text, int killType, int cPlayerID)
        {
            PlayerControllerGUI.allInstances[0].AddText(text);
            ClientPlayer cp = GetPlayer(cPlayerID);
            if (cp.isMe)
                cp.playerManager.localPlayer.playerGUI.AddKillText(killType);
        }

        #endregion

        [RFC]
        public void Client_EndMatch(int winnerID)
        {
            matchSettings.matchStatus = MatchStatus.HasEnded;
            SystemGameGUI.StaticSetEndScreen(matchSettings.modeSettings.matchSettings.isTeamMode ? allTeams[winnerID].name + " TEAM" : GetPlayer(winnerID).playerName);
        }

        public static void Server_CheckMatchScore()
        {
            if (matchSettings.matchStatus != MatchStatus.HasStarted)
                return;

            GameMode.ScoreSettings currentSettings = matchSettings.modeSettings.scoreSettings;
            if(currentSettings.scoreHandler == GameMode.ScoreHandlerType.PlayerKills)
            {
                for(int i = 0; i < instance.allPlayers.Length; i++)
                {
                    if (instance.allPlayers[i].kills >= currentSettings.maxKills)
                    {
                        instance.tno.Send("Client_EndMatch", Target.All, instance.allPlayers[i].mPlayerID);
                        return;
                    }
                }
            }
            else if (currentSettings.scoreHandler == GameMode.ScoreHandlerType.TeamKills)
            {
                for (int i = 0; i < instance.allTeams.Count; i++)
                {
                    if (instance.allTeams[i].teamKills >= currentSettings.maxKills)
                    {
                        instance.tno.Send("Client_EndMatch", Target.All, i);
                        return;
                    }
                }
            }
            else if (currentSettings.scoreHandler == GameMode.ScoreHandlerType.PlayerScore)
            {
                for (int i = 0; i < instance.allPlayers.Length; i++)
                {
                    if (instance.allPlayers[i].score >= currentSettings.maxScore)
                    {
                        instance.tno.Send("Client_EndMatch", Target.All, instance.allPlayers[i].mPlayerID);
                        return;
                    }
                }
            }
            else if (currentSettings.scoreHandler == GameMode.ScoreHandlerType.TeamScore)
            {
                for (int i = 0; i < instance.allTeams.Count; i++)
                {
                    if (instance.allTeams[i].teamScore >= currentSettings.maxScore)
                    {
                        instance.tno.Send("Client_EndMatch", Target.All, i);
                        return;
                    }
                }
            }
        }

        #endregion

        #region Object

        static public void CreateNetworkGameObject(bool persistent, GameObject prefab, Vector3 pos, Quaternion rot, CreateObjectSettings settings)
        {
            LevelManager.currentLevel.lastObjectID++;
            TNManager.CreateEx(10, persistent, prefab, pos, rot, LevelManager.currentLevel.lastObjectID, Serializer.Serialize<CreateObjectSettings>(settings));
        }

        static public void CreateNetworkProjectile(bool persistent, GameObject prefab, Vector3 pos, Quaternion rot, int weaponID, int mPlayerID)
        {
            TNManager.CreateEx(12, persistent, prefab, pos, rot, weaponID, mPlayerID);
        }

        private static int lastInstanceID = 0;

        [RCC(10)]
        private static GameObject OnCreateNetworkGameObject(GameObject prefab, Vector3 pos, Quaternion rot, int id, byte[] settings)
        {
            try
            {
                CreateObjectSettings serSettings = Serializer.Deserialize<CreateObjectSettings>(settings);
                GameObject go = Instantiate(prefab, pos, rot) as GameObject;
                go.SetActive(true);
                go.name = serSettings.objectName;
                instance.StartCoroutine(instance.HandleCreateNetworkGameObject(go, serSettings, pos, rot, id));
                return go;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message + ex.StackTrace);
            }
            return null;
        }

        private IEnumerator HandleCreateNetworkGameObject(GameObject go, CreateObjectSettings settings, Vector3 pos, Quaternion rot, int id)//Delay execution with 1 frame so Awake can activate to initialize tnet network tno
        {
            yield return null;

            //MPlayer mplayer = GetPlayer(settings.owner);
            //go.GetComponent<TNObject>().ownerID = mplayer.thisTNetPlayer.id;

            LevelEditor.LevelObjectManager lom = go.GetComponent<LevelEditor.LevelObjectManager>();
            lom.objectIndex = settings.objectIndex;
            lom.startPosition = pos;
            lom.startRotation = rot;
            lom.objectID = id;

            if (TNManager.isHosting)
            {
                lom.instanceID = lastInstanceID;
                lastInstanceID++;

                if (settings.enableEditMode)
                {
                    lom.ServerSetSelect(true, settings.mPlayerOwner, true);
                    lom.ServerSetObjectState((byte)LevelEditor.ObjectState.Phased, true);
                }
                else
                {
                    lom.ServerSetOwner(settings.mPlayerOwner, false);//Test, Was disabled figuring out why
                    lom.ServerSetObjectState((byte)settings.objectState, false);
                }
            }
        }

        [RCC(11)]
        private static GameObject OnCreatePlayerManager(GameObject prefab, int managerID, int maxPlayers)
        {
            try
            {
                if (instance.allInstantiatedPlayerManagers == null || instance.allInstantiatedPlayerManagers.Length != maxPlayers && managerID == 0)
                    instance.allInstantiatedPlayerManagers = new PlayerManager[maxPlayers];

                GameObject gO = GameObject.Instantiate(instance.playerManagerPrefab.thisGameObject, Vector3.zero, Quaternion.identity) as GameObject;
                instance.allInstantiatedPlayerManagers[managerID] = gO.GetComponent<PlayerManager>();
                instance.allInstantiatedPlayerManagers[managerID].pManagerID = managerID;
                DontDestroyOnLoad(gO);

                gO.name = "_PlayerManager ( " + managerID + " )";
                gO.SetActive(true);
                instance.allInstantiatedPlayerManagers[managerID].playerCharacter.gameObject.SetActive(false);

                //if (TNManager.isHosting) instance.allInstantiatedPlayerManagers[managerID].Initialize();

                return gO;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
            return null;
        }

        [RCC(12)]
        private static GameObject OnCreateNetworkProjectile(GameObject prefab, Vector3 pos, Quaternion rot, int weaponID, int mPlayerID)
        {
            try
            {
                GameObject go = null;
                MultiplayerProjectile mpoj = prefab.GetComponent<MultiplayerProjectile>();
                if (mpoj != null)
                {
                    mpoj = PoolManager.CreateProjectile(mpoj, pos, rot);
                    go = mpoj.gameObject;

                    mpoj.StartProjectile(new DamageGiver(GetPlayer(mPlayerID), weaponID));
                }
                Grenade grenade = prefab.GetComponent<Grenade>();
                if (grenade != null)
                {
                    grenade = (Instantiate(grenade.gameObject, pos, rot) as GameObject).GetComponent<Grenade>();
                    grenade.Initialize(mPlayerID);
                    grenade.Throw();

                    go = grenade.gameObject;
                }
                return go;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message + ex.StackTrace);
            }
            return null;
        }

        [System.Serializable]
        public class CreateObjectSettings
        {
            //General Options
            public string objectName;

            public int mPlayerOwner; //mPlayer that should be the owner of this object
            public int objectIndex; //The object index number from the global list

            //LevelEditor
            public bool enableEditMode; //Only for level editor
            public ObjectState objectState; //The object state wich the object should be created on

            //Vehicle Creation
            public bool usedForAnimatedSpawn;
            public int[] allPlayersForAnimatedSpawn; //MPlayer IDs Only
        }

        #endregion

        #region Static

        /// <summary>
        /// returns MPlayer by mPlayerID, returns null when MPlayer is not found.
        /// </summary>
        /// <param name="mPlayerID"></param>
        /// <returns></returns>
        public static ClientPlayer GetPlayer(int mPlayerID)
        {
            if (mPlayerID == -1)//return host
                return LocalPlayerManager.localPlayers[0].clientPlayer;

            for (int i = 0; i < matchSettings.serverSettings.maxPlayers; i++ )
                if(instance.allPlayers[i].mPlayerID == mPlayerID)
                    return instance.allPlayers[i];

            return null;
        }

        public static ClientPlayer[] GetPlayers()
        {
            return instance.allPlayers;
        }

        public static System.Collections.Generic.List<ClientPlayer> GetPlayers(int tPlayer)
        {
            System.Collections.Generic.List<ClientPlayer> allPlayers = new System.Collections.Generic.List<ClientPlayer>();
            for (int i = 0; i < allPlayers.Count; i++)
                if (allPlayers[i].tPlayer.id == tPlayer)
                    allPlayers.Add(allPlayers[i]);
            return allPlayers;
        }

        public static Map GetMap(string map, string scene)
        {
            for (int i = 0; i < instance.allMaps.Count; i++)
            {
                if (instance.allMaps[i].mapName.Equals(map, StringComparison.OrdinalIgnoreCase) || instance.allMaps[i].sceneName.Equals(scene, StringComparison.OrdinalIgnoreCase))
                {
                    return instance.allMaps[i];
                }
            }
            return null;
        }
        public static Map GetMap(int map)
        {
            if (instance.allMaps.Count >= map)
                return instance.allMaps[map];
            return null;
        }

        /*public static void ExplosionDamage(int shooterID, Vector3 position, int damage, float maximumDistance, float minimumDistance, int weaponID)
        {
            if (TNManager.isHosting)
            {
                try
                {
                    for (int i = 0; i < GetPlayers().Length; i++)
                    {
                        if (GetPlayers()[i].isConnected && GetPlayers()[i].isAlive)
                        {
                            float distance = Vector3.Distance(GetPlayers()[i].playerManager.GetPosition(true, false), position);
                            print(distance);
                            if (distance < maximumDistance)
                            {
                                float damper = Mathf.Clamp01((maximumDistance - (distance - minimumDistance)) / (maximumDistance + minimumDistance));
                                //print(Vector3.Project(new Vector3(0, 0, 25 * damper), position - GetPlayers()[i].pManager.GetPosition(false, false)));
                                GetPlayers()[i].playerManager.Server_ReceiveDamageVelocity(shooterID, (int)(damage * damper), weaponID, Vector3.Project(new Vector3(0, 0, 25 * damper), (GetPlayers()[i].playerManager.GetPosition(false, false) - position + Vector3.up) * damage));
                            }
                        }
                    }

                    Collider[] allColliders = Physics.OverlapSphere(position, maximumDistance);
                    Rigidbody[] allRigidbodyDistinct = allColliders.Select(x => x.attachedRigidbody).OfType<Rigidbody>().Distinct<Rigidbody>().ToArray();
                    int rigidbodyCount = allRigidbodyDistinct.Length;
                    for (int i = 0; i < rigidbodyCount; i++)
                        allRigidbodyDistinct[i].AddExplosionForce(damage * 10, position, maximumDistance, 2);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }*/

        public static void ExplosionDamage(ClientPlayer shooter, Vector3 position, float minimumDistance, float maximumDistance, int weaponID)
        {
            Collider[] allColliders = Physics.OverlapSphere(position, maximumDistance);
            List<DamageReceiver> allDamageReceivers = new List<DamageReceiver>();

            for (int i = 0; i < allColliders.Length; i++)
            {
                DamageReceiver dr = allColliders[i].GetComponent<DamageReceiver>();
                if (dr != null)
                    allDamageReceivers.Add(dr);
            }

            List<PlayerManager> allPlayerManagers = new List<PlayerManager>();
            for (int i = 0; i < allDamageReceivers.Count; i++)
            {
                if (allDamageReceivers[i].targetObject == TargetObject.PlayerManager && !allPlayerManagers.Contains(allDamageReceivers[i].pManager))
                    allPlayerManagers.Add(allDamageReceivers[i].pManager);
            }

            for (int i = 0; i < allPlayerManagers.Count; i++)
            {
                float distance = Vector3.Distance(shooter.playerManager.playerPhysics.transform.position, position);
                Vector3 dir = (shooter.playerManager.playerPhysics.transform.position + Vector3.up - position).normalized;
                Debug.Log(Math.GetValueOverDistance(1, distance, minimumDistance, maximumDistance) + ", Distance: " + distance + ", Min: " + minimumDistance + ", Max: " + maximumDistance);
                allPlayerManagers[i].Server_ReceiveDamageVelocity(shooter.mPlayerID, Math.GetValueOverDistance(1, distance, minimumDistance, maximumDistance), weaponID, (dir * 7.5f) + allPlayerManagers[i].multiplayerObject.cs_Buffer[0].velocity);
            }
        }

        public static bool AreWeOnTheSameTeam(ClientPlayer c1, ClientPlayer c2)
        {
            if (!matchSettings.modeSettings.matchSettings.isTeamMode)
                return false;

            if (c1.team == null || c2.team == null)
                return false;

            if (c1.team.index == c2.team.index)
                return true;

            return false;
        }

        #endregion

        #region GUI
        
        #region PlayerTags

        private float lastCheckTime;

        private void GUIShowPlayerTags()
        {
            if (matchSettings.matchStatus == MatchStatus.NotStarted)
            {
                GUI.skin = objectHolder.playertagsking;

                if (TNManager.isHosting && Time.time > lastCheckTime)
                {
                    lastCheckTime = Time.time + 0.1f;
                    for (int i = 0; i < allPlayers.Length; i++)
                    {
                        for (int a = 0; a < allPlayers.Length; a++)
                        {
                            if (i == a) continue;
                            ClientPlayer X = allPlayers[i];
                            ClientPlayer Y = allPlayers[a];

                            if (!X.isConnected || !Y.isConnected || !X.isAlive || !Y.isAlive) continue;
                            RaycastHit rayhit;
                            bool isNowVisible = !Physics.Linecast(X.playerManager.GetPosition(true, true), Y.playerManager.GetPosition(true, true), out rayhit, playerVisibilityLayers);
                            if (isNowVisible != Y.isVisible)
                                tno.Send("ReceiveTagData", Y.tPlayer, isNowVisible, Y.mPlayerID);
                        }
                    }
                }

                for (int i = 0; i < allPlayers.Length; i++)
                {
                    if (LocalPlayerManager.localPlayers[0].clientPlayer == null ||
                        i == LocalPlayerManager.localPlayers[0].clientPlayer.mPlayerID) continue;
                    ClientPlayer target = allPlayers[i];

                    if (!target.isConnected || !target.isAlive) continue;
                    Vector3 worldPosition = target.playerManager.GetPosition(true, true);
                    Vector3 screenPosition = LocalPlayerManager.localPlayers[0].playerCamera.camera.WorldToScreenPoint(worldPosition);
                    float screenSpaceDistanceToMiddle = Vector2.Distance(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(screenPosition.x, screenPosition.y));

                    if (!(screenPosition.z > 0)) continue;
                    if (screenSpaceDistanceToMiddle > Screen.height * 0.1f)
                    {
                        GUI.Box(new Rect(screenPosition.x - 20, Screen.height - screenPosition.y - 40, 40, 40), "");
                    }
                    else
                    {
                        GUI.Label(new Rect(screenPosition.x - 100, Screen.height - screenPosition.y - 50, 200, 50), target.playerName);
                    }
                }
            }
        }

        [RFC]
        private void ReceiveTagData(bool visible, int mPlayerID)
        {
            GetPlayer(mPlayerID).isVisible = visible;
        }

        #endregion

        #endregion

        #region Menu Events

        public void Action_StartServer()
        {
            NavigationController.NavigateTo("");
            NavigationController.SetMessage("Creating server...");

            MultiplayerMatchSettings mms = new MultiplayerMatchSettings();

            mms.serverSettings.maxPlayers = 16;
            mms.serverSettings.serverName = LocalPlayerManager.localPlayers[0].playerName + "'s game";

            MultiplayerMatchSettings.MapSelection ms = new MultiplayerMatchSettings.MapSelection();
            ms.selectedMap = allMaps[0];
            ms.gameMode = new GameMode.CustomSettings() { gamemodeName = offlineMultiplayer.hostGameModeName, isDefault = true, matchSettings = new GameMode.MatchSettings() { isTeamMode = true } };
            mms.mapRotation.Add(ms);

            StartServer(mms);
        }

        #endregion

        #region Offline/Fast Multiplayer

        public OfflineMultiplayer offlineMultiplayer = new OfflineMultiplayer();
        [System.Serializable]
        public class OfflineMultiplayer
        {
            public string hostServerName = "QuickMultiplayerServer";
            public int hostMaxPlayers = 8;
            public AngryRain.Multiplayer.GameModeType hostGameMode = AngryRain.Multiplayer.GameModeType.Deathmatch;
            public string hostGameModeName = "";

            public bool quickJoinServer;
            public string targetServerIP;

            public bool quickSpawn;
            public bool quickSpawnOnLocation;
            public Vector3 quickSpawnLocation;
        }

        #endregion

        [System.Serializable]
        public class MultiplayerMatchSettings
        {
            public ServerSettings serverSettings = new ServerSettings();

            public int currentMapRotationIndex;
            public System.Collections.Generic.List<MapSelection> mapRotation = new System.Collections.Generic.List<MapSelection> ();

            [System.NonSerialized]
            public bool hasServerStarted = false; //Has the server started the match, Is the server in the loading screen
            [System.NonSerialized]
            public bool hasClientBeenLoaded = false; //Has this client finished loading
            [System.NonSerialized]
            public bool hasServerBeenLoaded = false; //Has the server finished loading
            public MatchStatus matchStatus = MatchStatus.NotStarted; //Has the match begun, Is the countdown on 0

            public float serverWaitTime = 0; //Wait time so that everybody who load before this time has to wait

            public float mapLoadingProgress = 0;

            public GameMode.CustomSettings modeSettings
            {
                get 
                {
                    try
                    {
                        return mapRotation[currentMapRotationIndex].gameMode;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Trying to acces CurrentGamemode under number " + currentMapRotationIndex);
                        Debug.LogError(ex);
                        return null;
                    }
                }
            }
            public MapSelection mapSelection
            {
                get
                {
                    try
                    {
                        return mapRotation[currentMapRotationIndex];
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Trying to acces CurrentGamemode under number " + currentMapRotationIndex);
                        Debug.LogError(ex);
                        return null;
                    }
                }
            }

            [System.Serializable]
            public class ServerSettings
            {
                public string serverName = "HeyHeyWelcome";
                public string serverPassword = "";
                public int maxPlayers = 16;
            }

            [System.Serializable]
            public class MapSelection
            {
                public Map selectedMap;
                public string customLevel = "";
                public GameMode.CustomSettings gameMode;
            }
        }

        [System.Serializable]
        public class ObjectHolder
        {
            public GameObject droppodPrefab;
            public GUISkin skin;
            public GUISkin playertagsking;

            public Texture hitmarker;
        }
    }

    [System.Serializable]
    public class Map
    {
        public string mapName;
        public string sceneName;
    }
}

public enum MatchStatus
{
    NotStarted,
    HasStarted,
    HasEnded
}