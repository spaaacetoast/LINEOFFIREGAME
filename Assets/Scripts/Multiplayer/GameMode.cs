using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System;

namespace AngryRain.Multiplayer
{
    public class GameMode : MonoBehaviour
    {
        public static GameMode instance;

        /// <summary>
        /// This list contains all standard/default customSettings for the game
        /// </summary>
        public List<CustomSettings> customSettings = new List<CustomSettings>();

        /// <summary>
        /// This list contains all the ingame customSettings but also all the customSettings that where made by players. Empty this list when not in use.
        /// </summary>
        [HideInInspector]
        public List<CustomSettings> allCustomSettings = new List<CustomSettings>();

        public Texture2D[] gameModeLogos;

        #region Static

        public static readonly string[] objectiveNames = new string[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu" };
        public static readonly string[] objectiveNamesShort = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        public static void SaveAllGamemodeData()
        {
            for(int i = 0; i < instance.allCustomSettings.Count; i++)
            {
                while(instance.allCustomSettings[i].isDefault)
                {
                    instance.allCustomSettings.RemoveAt(i);
                }
            }

            string targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/lineoffire/levels/";
            string targetFile = "gamemodes.xml";
            XMLSerializer.Save<List<CustomSettings>>(targetFolder + targetFile, instance.allCustomSettings);

            LoadAllGamemodeData();
        }

        public static void LoadAllGamemodeData()
        {
            try
            {
                string targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/lineoffire/levels/";
                string targetFile = "gamemodes.xml";
                instance.allCustomSettings = XMLSerializer.Load<List<CustomSettings>>(targetFolder + targetFile);
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }
            finally
            {
                instance.allCustomSettings.AddRange(instance.customSettings);
            }
        }

        public static void LoadAllGamemodeData(bool loadOnlyWhenEmpty)
        {
            if (loadOnlyWhenEmpty && (instance.allCustomSettings == null || instance.allCustomSettings.Count == 0) || !loadOnlyWhenEmpty)
                LoadAllGamemodeData();
        }

        public static void ResetAllGamemodeData()
        {
            instance.allCustomSettings = null;
        }

        public static Texture2D GetGameModeLogo(int i)
        {
            return instance.gameModeLogos[i];
        }

        #endregion

        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        [System.Serializable]
        public class CustomSettings
        {
            public string gamemodeName;
            public int gamemodeLogo;

            public GameModeType gamemodeType = GameModeType.Deathmatch;

            public bool isDefault;
            /*public bool hasCustomPointHandler;
            public bool hasCustomLogic;*/

            public PlayerSettings playerSettings = new PlayerSettings();
            public MatchSettings matchSettings = new MatchSettings();
            public ScoreSettings scoreSettings = new ScoreSettings();
            public WeaponSettings weaponSettings = new WeaponSettings();
            public VehicleSettings vehicleSettings = new VehicleSettings();
        }

        [System.Serializable]
        public class PlayerSettings
        {
            //player
            public int playerHealth = 100;
            public int playerGravityMultiplier = 1;
            public int playerSpeedMultiplier = 1;
            public int playerJumpHeightMultiplier = 1;
            public bool enableHUD = true;

            //damage
            public bool enableFallDamage = true;
            public float gunDamageMultiplier = 1;
            public float meleeDamageMultiplier = 1;

            //Spawning
            public int spawningWaitTime = 4;
            public int spawningTeamKillPenalty = 4;
            public int spawningTeamKillPenaltyMax = 20;
        }

        [System.Serializable]
        public class MatchSettings
        {
            public int matchWaitTimeInSec = 5;
            public int matchMaxLengthInMin=10;
            [HideInInspector]
            public double matchStartTime; //*DEV Only*, Not visible for players

            public bool isTeamMode=true;
            public bool allowTeamKills=false;
            public int numberTeams = 2;

            public int matchStartClipNum; //*DEV Only*, Not visible for players
            public int gameModeClipNum; //*DEV Only*, Not visible for players
        }

        [System.Serializable]
        public class ScoreSettings
        {
            public ScoreHandlerType scoreHandler = ScoreHandlerType.TeamKills;

            public int maxScore = 2500;
            public int maxKills = 5;

            public float objectiveCaptureSpeed = 1;
            public int objectiveScoreAmount = 1;
            public int objectivePointDelay = 5;

            public PlayerScoreSettings playerScoreSettings = new PlayerScoreSettings();
        }

        [System.Serializable]
        public class PlayerScoreSettings
        {
            public int killScore = 50;
            public int assistScore = 10;
            public int reviveScore = 10;
            public int healScore = 5;
            public int headshotScore = 10;
            public int vehicleKillScore = 30;
            public int vehicleDestroyScore = 10;
            public int defendObjectiveScore = 25;
            public int captureObjectiveScore = 50;
        }

        [System.Serializable]
        public class WeaponSettings
        {
            public bool useCustomLoadouts = true;
        }

        [System.Serializable]
        public class VehicleSettings
        {
            public bool AllowEveryVehicle = true;
            public bool AllowLightLandVehicles = true;
            public bool AllowHeavyLandVehicles = true;
            public bool AllowHelicopter = true;
            public bool AllowJets = true;
        }

        public enum ScoreHandlerType
        {
            None, PlayerKills, TeamKills, PlayerScore, TeamScore, PlayerTeamScore
        }
    }

    public enum GameModeType
    {
        Deathmatch,
        TeamDeathmatch
    }
}