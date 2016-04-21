using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.SceneManagement;

namespace AngryRain.Multiplayer.LevelEditor
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager instance;

        public List<LevelObject> allLevelObjects = new List<LevelObject>();
        public static List<LevelObjectManager> allLevelObjectManagers = new List<LevelObjectManager>();
        public static List<LevelObjectManager> allSceneLevelObjectManagers = new List<LevelObjectManager>();

        public static bool levelEditorEnabled = false;
        public static bool cinameticEditorEnabled = false;

        public Transform gizmosParent;
        [HideInInspector] public GizmoTransform currentGizmo;

        public static LevelSettings currentLevel = new LevelSettings();

        #region Variables Cinametic Editor

        public bool isPlaying;
        private float playStartTime;
        public float playSpeed = 1;
        public float currentTime;

        public List<TimedObject> allTimedObjects = new List<TimedObject>();

        #endregion

        #region MonoBehaviours

        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (levelEditorEnabled)
                currentMenu = MenuStart;
            if (cinameticEditorEnabled)
                currentMenu = Menu_MainCinametic;
        }

        void Start()
        {
            foreach(LevelObject lo in allLevelObjects)
                if (!TNManager.mInstance.objects.Contains(lo.instantiateObject))
                    TNManager.mInstance.objects.Add(lo.instantiateObject);
        }

        void Update()
        {
            if (levelEditorEnabled)
            {
                LocalPlayer lPlayer = LocalPlayerManager.localPlayers[0];

                if (InputManager.GetButtonDown(InputName.LevelEditorSwitch, LocalPlayerManager.localPlayers[0]) && lPlayer.clientPlayer.vehicle == false)
                {
                    lPlayer.enableLevelEditor = !lPlayer.enableLevelEditor;

                    if (lPlayer.enableLevelEditor)
                    {
                        lPlayer.playerCamera.EnableCamera(CameraResetType.None, false, false, CameraType.LevelEditor);

                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                    else
                    {
                        //lPlayer.currentPlayerCamera.EnableCamera(CameraResetType.Position, true);
                        DisableLevelEditorActive(lPlayer, false, CameraResetType.Position, false);
                    }

                    lPlayer.clientPlayer.playerManager.playerController.playerVariables.canShoot = !lPlayer.enableLevelEditor;
                    lPlayer.clientPlayer.playerManager.playerController.StartCoroutine("SwitchWeapon", lPlayer.enableLevelEditor ? CustomizationManager.WeaponSpot.None : CustomizationManager.WeaponSpot.Primary);
                }

                if (lPlayer.enableLevelEditor)
                {
                    if (lPlayer.playerCamera.cameraSettings.cameraType == CameraType.LevelEditor)
                    {
                        bool enableInput = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetKey(KeyCode.Space) ? 1 : Input.GetKey(KeyCode.LeftControl) ? -1 : 0, Input.GetAxisRaw("Vertical")) != Vector3.zero
                            || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                        
                        if (enableInput && !lPlayer.playerCamera.cameraSettings.enableControls)
                        {
                            lPlayer.playerCamera.cameraSettings.enableControls = true;
                            Cursor.visible = false;
                            Cursor.lockState = CursorLockMode.Locked;
                        }
                        else if (!enableInput && lPlayer.playerCamera.cameraSettings.enableControls)
                        {
                            lPlayer.playerCamera.cameraSettings.enableControls = false;
                            Cursor.visible = true;
                            Cursor.lockState = CursorLockMode.None;
                        }
                    }

                    if (lPlayer.clientPlayer.currentHoldingObject != null)
                    {
                        UpdateGizmo(lPlayer);

                        if (Input.GetKeyDown(KeyCode.Delete))
                        {
                            DisableLevelEditorActive(lPlayer, true, CameraResetType.None, true);
                        }
                        if (Input.GetKeyDown(KeyCode.Return))
                        {
                            DisableLevelEditorActive(lPlayer, false, CameraResetType.None, true);
                        }
                    }
                    else
                    {
                        if (gizmosParent.gameObject.activeSelf)
                            gizmosParent.gameObject.SetActive(false);

                        if (Input.GetMouseButtonDown(0))
                        {
                            RaycastHit[] hits = Physics.RaycastAll(lPlayer.playerCamera.camera.ScreenPointToRay(Input.mousePosition));

                            foreach(RaycastHit h in hits)
                            {
                                LevelObjectManager lom = FindLevelObjectManager(h.transform);
                                if (lom != null && lom.isSelected == false)
                                {
                                    //lom.ServerSetOwner(lPlayer.mPlayer.playerID, false);
                                    lom.ServerSetSelect(true, lPlayer.clientPlayer.mPlayerID, true);
                                    lom.ServerSetObjectState((byte)ObjectState.Phased, true);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (cinameticEditorEnabled && currentTimedObject != null)
                UpdateGizmo(AngryRain.LocalPlayerManager.localPlayers[0], currentTimedObject.levelObjectManager);

            if (isPlaying)//Update time and update all objects per frame
            {
                currentTime = (Time.time - playStartTime) / playSpeed;
                UpdateTimedObjects();
            }
        }

        void UpdateGizmo(LocalPlayer lPlayer)
        {
            Vector3 viewPortPos = lPlayer.playerCamera.camera.WorldToViewportPoint(lPlayer.clientPlayer.currentHoldingObject.rigidbody.position);
            viewPortPos.z = 1;
            viewPortPos = lPlayer.playerCamera.camera.ViewportToWorldPoint(viewPortPos);
            if (lPlayer.playerCamera.camera.WorldToViewportPoint(viewPortPos).z > 0)
            {
                gizmosParent.position = viewPortPos;
                if (!gizmosParent.gameObject.activeSelf)
                    gizmosParent.gameObject.SetActive(true);
            }
            else
            {
                if (gizmosParent.gameObject.activeSelf)
                    gizmosParent.gameObject.SetActive(false);
            }

            if (currentGizmo != null)
            {
                if (!lPlayer.clientPlayer.currentHoldingObject.editTransform)
                {
                    lPlayer.clientPlayer.currentHoldingObject.editTransform = true;
                    lPlayer.clientPlayer.currentHoldingObject.ServerSetObjectState((byte)ObjectState.Phased, true);
                }
                else
                {
                    Vector3 nextPosition = currentGizmo.GetNextPosition(lPlayer.clientPlayer.currentHoldingObject.rigidbody.position, lPlayer);
                    if (nextPosition == Vector3.zero) Debug.LogError("Returned Vector3 Zero");
                    lPlayer.clientPlayer.currentHoldingObject.rigidbody.position = nextPosition != Vector3.zero ? nextPosition : lPlayer.clientPlayer.currentHoldingObject.startPosition;
                }
            }
            else if (lPlayer.clientPlayer.currentHoldingObject.editTransform)
            {
                lPlayer.clientPlayer.currentHoldingObject.editTransform = false;
                lPlayer.clientPlayer.currentHoldingObject.ServerSetObjectState((byte)ObjectState.Phased, false);
                lPlayer.clientPlayer.currentHoldingObject.Local_SyncSaveParameters();
            }
        }

        void UpdateGizmo(LocalPlayer lPlayer, LevelObjectManager lom)
        {
            if (lPlayer == null || lom == null)
            {
                currentGizmo = null;
                if (gizmosParent.gameObject.activeSelf)
                    gizmosParent.gameObject.SetActive(false);
                return;
            }

            Vector3 viewPortPos = lPlayer.playerCamera.camera.WorldToViewportPoint(lom.rigidbody.position);
            viewPortPos.z = 1;
            viewPortPos = lPlayer.playerCamera.camera.ViewportToWorldPoint(viewPortPos);
            if (lPlayer.playerCamera.camera.WorldToViewportPoint(viewPortPos).z > 0)
            {
                gizmosParent.position = viewPortPos;
                if (!gizmosParent.gameObject.activeSelf)
                    gizmosParent.gameObject.SetActive(true);
            }
            else
            {
                if (gizmosParent.gameObject.activeSelf)
                    gizmosParent.gameObject.SetActive(false);
            }

            if (currentGizmo != null)
            {
                if (!lom.editTransform)
                {
                    lom.editTransform = true;
                    lom.ServerSetObjectState((byte)ObjectState.Phased, true);
                }
                else
                {
                    Vector3 nextPosition = currentGizmo.GetNextPosition(lom.rigidbody.position, lPlayer);
                    if (nextPosition == Vector3.zero) Debug.LogError("Returned Vector3 Zero");
                    lom.rigidbody.position = nextPosition != Vector3.zero ? nextPosition : lom.startPosition;
                }
            }
            else if (lom.editTransform)
            {
                lom.editTransform = false;
                lom.ServerSetObjectState((byte)ObjectState.Phased, false);
                lom.Local_SyncSaveParameters();
            }
        }

        #endregion

        #region Loading and Saving

        public static bool LoadLevel(string levelName)
        {
            ResetCurrentLevel();

            string targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/lineoffire/levels/";
            string targetFile = levelName + ".xml";

            if (!File.Exists(targetFolder + "/" + targetFile))
                return false;

            try
            {
                ResetCurrentLevel();

                LevelSettings levelSettings = XMLSerializer.Load<LevelSettings>(targetFolder + "/" + targetFile);

                foreach (LevelObjectSaveData los in levelSettings.allObjects)
                {
                    MultiplayerManager.CreateObjectSettings settings = new MultiplayerManager.CreateObjectSettings();

                    settings.objectIndex = los.objectIndex;
                    //settings.usedForAnimatedSpawn = 
                    settings.objectState = cinameticEditorEnabled ? ObjectState.Fixed : los.objectState;
                    settings.objectName = instance.allLevelObjects[los.objectIndex].instantiateObject.name;
                    settings.mPlayerOwner = LocalPlayerManager.localPlayers[0].clientPlayer.mPlayerID;

                    MultiplayerManager.CreateNetworkGameObject(true, instance.allLevelObjects[los.objectIndex].instantiateObject, los.startPosition.Get(), los.startRotation.Get(), settings);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw e;
            }

            return true;
        }

        public static bool LoadLevel(LevelSettings levelSettings)
        {
            try
            {
                ResetCurrentLevel();

                foreach (LevelObjectSaveData los in levelSettings.allObjects)
                {
                    MultiplayerManager.CreateObjectSettings settings = new MultiplayerManager.CreateObjectSettings();

                    settings.objectIndex = los.objectIndex;
                    //settings.usedForAnimatedSpawn = 
                    settings.objectState = los.objectState;
                    settings.objectName = instance.allLevelObjects[los.objectIndex].name;
                    settings.mPlayerOwner = LocalPlayerManager.localPlayers[0].clientPlayer.mPlayerID;

                    MultiplayerManager.CreateNetworkGameObject(true, instance.allLevelObjects[los.objectIndex].instantiateObject, los.startPosition.Get(), los.startRotation.Get(), settings);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw e;
            }

            return true;
        }

        public static void ResetCurrentLevel()
        {
            foreach (LevelObjectManager lom in allLevelObjectManagers)
            {
                if (!lom.isLevelObject)
                    TNManager.Destroy(lom.gameObject);
            }
            allLevelObjectManagers.Clear();
        }

        public static void SaveCurrentLevel()
        {
            if (currentLevel.customLevelName.Length < 3)
                currentLevel.customLevelName = "NewLevel (" + String.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ")";

            string targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/lineoffire/levels/";
            string targetFile = currentLevel.customLevelName + ".xml";

            currentLevel.allObjects = new List<LevelManager.LevelObjectSaveData>();

            foreach (LevelObjectManager lom in allLevelObjectManagers)
            {
                if (lom != null && !lom.isStaticObject && !lom.isSelected)
                {
                    currentLevel.allObjects.Add(new LevelObjectSaveData().SaveVariables(lom));
                }
            }

            if (!File.Exists(targetFolder + "/" + targetFile))
            {
                Directory.CreateDirectory(targetFolder);
            }

            if (currentLevel.mapName == "")
                currentLevel.mapName = MultiplayerManager.GetMap("", SceneManager.GetActiveScene().name).mapName;

            if (currentLevel.availableGamemodes.Count == 0)
                currentLevel.availableGamemodes = new List<GameModeType>() { GameModeType.Deathmatch, GameModeType.TeamDeathmatch };

            XMLSerializer.Save<LevelSettings>(targetFolder + "/" + targetFile, currentLevel);

            currentLevel.allObjects = null;
        }

        #endregion

        #region GUI, Menus

        Action currentMenu;
        LevelObjectType selectedObjectType;
        LocalPlayer currentlPlayer;

        /*void OnGUI()
        {
            //GUI.skin = Menu.MenuManager.instance.mainMenu.guiSkinMainMenu;
            if (!cinameticEditorEnabled)
                LevelEditorGUI();
            else
                currentMenu();
        }*/

        #region Level Editor

        void LevelEditorGUI()
        {
            GUIStyle s = new GUIStyle() { alignment = TextAnchor.LowerRight, normal = new GUIStyleState() { textColor = new Color(0.9f, 0.9f, 0.9f) } };

            currentlPlayer = LocalPlayerManager.localPlayers[0];
            if (levelEditorEnabled && MultiplayerManager.matchSettings.matchStatus == MatchStatus.HasStarted && currentlPlayer.clientPlayer != null && currentlPlayer.clientPlayer.isAlive)
            {
                if (currentlPlayer.enableLevelEditor)
                {
                    if (!currentlPlayer.clientPlayer.currentHoldingObject)
                    {
                        GUILayout.BeginArea(new Rect(Screen.width - 360, Screen.height - 460, 350, 450));
                        GUILayout.FlexibleSpace();

                        GUILayout.BeginVertical("Box");
                        GUILayout.Space(5);
                        GUILayout.Label("\tLevel Editor - " + currentlPlayer.playerName);
                        GUILayout.Space(4);
                        currentMenu();
                        GUILayout.EndVertical();

                        GUILayout.EndArea();

                        GUI.Label(new Rect(Screen.width - 560, Screen.height - 40, 190, 24), "Hide Level Editor [Up Arrow]", s);

                        if (currentlPlayer.playerMode == PlayerMode.normal)
                        {
                            GUI.Label(new Rect(Screen.width - 560, Screen.height - 55, 190, 24), "Enable Fly Mode [Down Arrow]", s);
                        }
                        else if (currentlPlayer.playerMode == PlayerMode.vehicle)
                        {
                            GUI.Label(new Rect(Screen.width - 560, Screen.height - 55, 190, 24), "Enable Fly Mode [Down Arrow]", s);
                            //Get Out Vehicle
                        }
                        else if (currentlPlayer.playerMode == PlayerMode.flymode)
                        {
                            GUI.Label(new Rect(Screen.width - 560, Screen.height - 55, 190, 24), "Disable Fly Mode [Down Arrow]", s);
                        }

                        if (currentlPlayer.playerCamera.cameraSettings.enableControls)
                        {
                            GUI.Box(new Rect(10, Screen.height - 50, 200, 40), "", "BoxActive");
                            GUI.Label(new Rect(25, Screen.height - 50, 200, 40), Input.GetMouseButton(1) && Input.GetMouseButton(2) ? "Translate, Rotation Active" : Input.GetMouseButton(1) ? "Rotation Active" : Input.GetMouseButton(2) ? "Translate Active" : "Active");
                        }
                        else
                        {
                            GUI.Box(new Rect(10, Screen.height - 50, 200, 40), "", "Box");
                            GUI.Label(new Rect(25, Screen.height - 50, 200, 40), "Not Active");
                        }
                    }
                    else
                    {
                        GUILayout.BeginArea(new Rect(Screen.width - 410, Screen.height - 210, 400, 200), "");
                        GUILayout.FlexibleSpace();
                        //GUILayout.BeginVertical("","Box");

                        GUILayout.Space(5);
                        GUILayout.Box("Currently Selected - " + currentlPlayer.clientPlayer.currentHoldingObject.thisLevelObject.name, "BoxActive");
                        GUILayout.Space(5);
                        GUILayout.Box("\t{Delete}\t\t\tDelete Object", "SmallBox", GUILayout.Height(26), GUILayout.Width(400));
                        GUILayout.Box("\t{Insert}\t\t\tObject Options", "SmallBox", GUILayout.Height(26), GUILayout.Width(400));

                        if (currentlPlayer.clientPlayer.currentHoldingObject.canRuntimeObjectStateBeChanged)
                        {
                            GUILayout.Space(5);
                            GUILayout.Box("\t{ < | > }\t\tObject Physics - " + currentlPlayer.clientPlayer.currentHoldingObject.currentObjectState, "SmallBox", GUILayout.Height(26), GUILayout.Width(400));
                        }

                        GUILayout.Space(5);
                        GUILayout.Box("\t{Enter}\tDeselect Object", "SmallBox", GUILayout.Height(30), GUILayout.Width(400));

                        //GUILayout.EndVertical();
                        GUILayout.EndArea();
                    }
                }
                else
                {
                    GUI.Label(new Rect(Screen.width - 350, Screen.height - 40, 250, 24), "Show Level Editor [Up Arrow]", s);
                }
            }
        }

        void MenuStart()
        {
            GUILayout.Button("Weapons");
            if (GUILayout.Button("Vehicles"))
            {
                selectedObjectType = LevelObjectType.Vehicle;
                currentMenu = MenuShowItems;
            }
            if (GUILayout.Button("Spawning"))
            {
                selectedObjectType = LevelObjectType.Spawn;
                currentMenu = MenuShowItems;
            }
            if(GUILayout.Button("Objectives"))
            {
                selectedObjectType = LevelObjectType.Objective;
                currentMenu = MenuShowItems;
            }
            GUILayout.Button("Scenery");
            GUILayout.Button("Structure");
        }

        void MenuShowItems()
        {
            int c = allLevelObjects.Count;
            for (int i = 0; i < c; i++)
            {
                if (allLevelObjects[i].levelObjectType == selectedObjectType)
                {
                    if (GUILayout.Button(allLevelObjects[i].name))
                    {
                        MultiplayerManager.CreateObjectSettings settings = new MultiplayerManager.CreateObjectSettings();

                        settings.objectName = allLevelObjects[i].instantiateObject.name;
                        settings.enableEditMode = true;
                        settings.mPlayerOwner = currentlPlayer.clientPlayer.mPlayerID;
                        settings.objectState = ObjectState.Fixed;
                        settings.objectIndex = i;

                        Vector3 startPosition = currentlPlayer.playerCamera.position + (currentlPlayer.playerCamera.rotation * (Vector3.forward * 15));
                        MultiplayerManager.CreateNetworkGameObject(true, allLevelObjects[i].instantiateObject, startPosition, Quaternion.identity, settings);
                        /*MultiplayerManager.CreateNetworkGameObject(allObjects[i].instantiateObject, 
                            currentlPlayer.mPlayer.playerID,
                            currentlPlayer.currentPlayerCamera.thisPosition + (currentlPlayer.currentPlayerCamera.thisRotation * (Vector3.forward * 15)), 
                            Quaternion.identity, 
                            allObjects[i].instantiateObject.name, true, true, 
                            i, 
                            ObjectState.Fixed);*/
                    }
                }
            }
            
            if (GUILayout.Button("Back"))
            {
                currentMenu = MenuStart;
            }
        }

        #endregion

        #region Cinametic Editor
        
        Vector2 scroll;
        bool[] showMenu = new bool[4];
        void Menu_MainCinametic()
        {
            if (!LocalPlayerManager.localPlayers[0].clientPlayer.isAlive)
                return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            LocalPlayer lPlayer = LocalPlayerManager.localPlayers[0];
            LevelObjectManager currentLOM = lPlayer.clientPlayer.currentHoldingObject;

            GUILayout.BeginArea(new Rect(Screen.width - 460, Screen.height - 610, 450, 600));
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical("Box");
            GUILayout.Space(5);
            GUILayout.Label("\tCinametic Editor");
            GUILayout.Space(4);

            if (currentLOM == null)
                GUILayout.Box("Select an object to animate it");
            else
            {
                GUILayout.BeginVertical("Box");
                GUILayout.Label(currentLOM.thisLevelObject.name);
                GUILayout.Label("ID: " + currentLOM.objectID);
                GUILayout.EndVertical();

                if (GUILayout.Button("Keyframes"))
                    currentMenu = Menu_KeyframeEditor;
                GUILayout.Button("Object Settings");

                GUILayout.Space(15);
                if (GUILayout.Button("Focus"))
                {
                    lPlayer.playerCamera.FocusCameraOnPoint(currentLOM.transform.position, 10);
                }
                GUILayout.Space(15);
            }

            if (GUILayout.Button("Settings"))
                currentMenu = Menu_Settings;

            GUILayout.EndVertical();

            GUILayout.EndArea();

            //Show all available object, Cameras, Players, Bots, Vehicles etc..
            GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));

            GUILayout.BeginVertical("Box");
            GUILayout.Space(5);
            GUILayout.Label("\tAvailable Objects", GUILayout.Height(26));
            GUILayout.Space(4);

            scroll = GUILayout.BeginScrollView(scroll);
            if (GUILayout.Button((showMenu[0] ? "- " : "+ ") + "Cameras", GUILayout.Height(26)))
                showMenu[0] = !showMenu[0];
            if (showMenu[0])
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                GUILayout.Label("Not Supported!");

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button((showMenu[1] ? "- " : "+ ") + "Players", GUILayout.Height(26)))
                showMenu[1] = !showMenu[1];
            if (showMenu[1])
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                GUILayout.Label("Not Supported!");

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button((showMenu[2] ? "- " : "+ ") + "Level Objects", GUILayout.Height(26)))
                showMenu[2] = !showMenu[2];
            if (showMenu[2])
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                for (int i = 0; i < LevelManager.allLevelObjectManagers.Count; i++)
                {
                    LevelObjectManager lom = LevelManager.allLevelObjectManagers[i];
                    if (GUILayout.Button(lom.thisLevelObject.name + ", ID:" + lom.objectID, lPlayer.clientPlayer.currentHoldingObject == lom ? "ButtonSelected" : "Button", GUILayout.Height(26)))
                        lPlayer.clientPlayer.currentHoldingObject = lPlayer.clientPlayer.currentHoldingObject == lom ? null : lom;
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button((showMenu[3] ? "- " : "+ ") + "Environment", GUILayout.Height(26)))
                showMenu[3] = !showMenu[3];
            if (showMenu[3])
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                GUILayout.Button("Lighting");
                GUILayout.Button("Weather");

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Settings", GUILayout.Height(26)))
                currentMenu = Menu_Settings;

            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        void Menu_Settings()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 460, Screen.height - 610, 450, 600));
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical("Box");
            GUILayout.Space(5);
            GUILayout.Label("\tCinametic Editor - Settings");
            GUILayout.Space(5);



            if (GUILayout.Button("Back"))
                currentMenu = Menu_MainCinametic;

            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        Vector2 scrollKeyframe;
        TimedObject currentTimedObject;
        TimedObject.KeyFrame currentSelectedKeyframe;
        float currentSelectedKeyframeTime = 0;
        float iterateRate = 1;

        void Menu_KeyframeEditor()
        {
            if (!LocalPlayerManager.localPlayers[0].clientPlayer.isAlive)
                return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            LocalPlayer lPlayer = LocalPlayerManager.localPlayers[0];
            LevelObjectManager currentLOM = lPlayer.clientPlayer.currentHoldingObject;
            currentTimedObject = TimedObject.GetTimedObject(currentLOM);

            GUI.Box(new Rect(10, Screen.height - 210, Screen.width - 20, 25), "\tCinametic Editor - Keyframe Editor");

            if (isPlaying)
                if (GUI.Button(new Rect(Screen.width - 410, Screen.height - 210, 100, 25), "Stop"))
                {
                    currentTime = 0;
                    isPlaying = false;
                }
            if (GUI.Button(new Rect(Screen.width - 310, Screen.height - 210, 100, 25), isPlaying ? "Pause" : "Play"))
            {
                if (isPlaying)
                {
                    isPlaying = false;
                }
                else
                {
                    if (currentTime == 0)
                        playStartTime = Time.time;
                    else
                        playStartTime = currentTime + Time.time;

                    isPlaying = true;
                }
            }
            if (GUI.Button(new Rect(Screen.width - 210, Screen.height - 210, 100, 25), "Zoom In"))
                iterateRate = iterateRate == 1 ? 0.5f : iterateRate == 0.5f ? 0.25f : iterateRate == 0.25f ? 0.125f: 0.125f;
            if(GUI.Button(new Rect(Screen.width - 110, Screen.height - 210, 100, 25), "Zoom Out"))
                iterateRate = iterateRate == 0.125f ? 0.25f : iterateRate == 0.25f ? 0.5f : iterateRate == 0.5f ? 1f : 1f;

            //Calculate view rect
            Rect view = new Rect();
            view.height = 49;
            view.width = Mathf.Max(Screen.width-20, 30*20);

            GUI.Box(new Rect(10, Screen.height - 180, Screen.width - 20, 65), "");
            scrollKeyframe = GUI.BeginScrollView(new Rect(10, Screen.height - 180, Screen.width - 20, 65), scrollKeyframe, view, true, false);

            for (float i = 0; i < 30; i += iterateRate)
            {
                TimedObject.KeyFrame keyframe = currentTimedObject != null ? currentTimedObject.GetKeyframeOnTime(i) : null;
                bool shouldShowSelect = isPlaying ? currentTime > i && currentTime < i + iterateRate : i == currentSelectedKeyframeTime;
                if (GUI.Button(new Rect(i * (20 / iterateRate), 0, 20, 50), "", i == currentSelectedKeyframeTime ? "ButtonSelected" : keyframe != null ? "ButtonSelected2" : "Button"))
                {
                    currentSelectedKeyframeTime = i;
                    currentSelectedKeyframe = keyframe;

                    currentTime = i;

                    UpdateTimedObjects();
                }
                if (i % 5 == 0)
                    GUI.Label(new Rect(i * (20/iterateRate), 5, 20, 15), i.ToString(), "Label2");
            }

            GUI.EndScrollView(false);

            GUI.Box(new Rect(10, Screen.height - 110, Screen.width - 20, 100), "");

            if (currentSelectedKeyframe == null || currentTimedObject == null)
            {
                if (GUI.Button(new Rect(210, Screen.height - 40, 200, 30), "Create new keyframe"))
                {
                    if (currentTimedObject == null)
                        currentTimedObject = TimedObject.CreateAndRegister(currentLOM);

                    currentSelectedKeyframe = currentTimedObject.GetKeyframeOnTime(currentSelectedKeyframeTime);
                    if (currentSelectedKeyframe == null)
                        currentSelectedKeyframe = currentTimedObject.SetKeyframeOnTime(currentSelectedKeyframeTime);

                    lPlayer.playerCamera.FocusCameraOnPoint(currentLOM.transform.position, 15);
                }
            }
            else
            {
                GUI.Label(new Rect(20, Screen.height - 100, 75, 20), "Position");
                GUI.Label(new Rect(105, Screen.height - 100, 25, 20), "X");
                GUI.TextField(new Rect(125, Screen.height - 100, 100, 20), currentSelectedKeyframe.position.x.ToString());
                GUI.Label(new Rect(230, Screen.height - 100, 25, 20), "Y");
                GUI.TextField(new Rect(250, Screen.height - 100, 100, 20), currentSelectedKeyframe.position.y.ToString());
                GUI.Label(new Rect(355, Screen.height - 100, 25, 20), "Z");
                GUI.TextField(new Rect(375, Screen.height - 100, 100, 20), currentSelectedKeyframe.position.z.ToString());

                GUI.Label(new Rect(20, Screen.height - 70, 50, 20), "Rotation");
                GUI.Label(new Rect(105, Screen.height - 70, 25, 20), "X");
                GUI.TextField(new Rect(125, Screen.height - 70, 100, 20), currentSelectedKeyframe.rotation.x.ToString());
                GUI.Label(new Rect(230, Screen.height - 70, 25, 20), "Y");
                GUI.TextField(new Rect(250, Screen.height - 70, 100, 20), currentSelectedKeyframe.rotation.y.ToString());
                GUI.Label(new Rect(355, Screen.height - 70, 25, 20), "Z");
                GUI.TextField(new Rect(375, Screen.height - 70, 100, 20), currentSelectedKeyframe.rotation.z.ToString());

                if (GUI.Button(new Rect(210, Screen.height - 40, 200, 30), "Delete keyframe"))
                {
                    currentTimedObject.allKeyFrames.Remove(currentSelectedKeyframe);
                    currentSelectedKeyframe = null;
                }
                if (GUI.Button(new Rect(410, Screen.height - 40, 200, 30), "Set keyframe"))
                {
                    currentSelectedKeyframe.position = currentLOM.transform.position;
                    currentSelectedKeyframe.rotation = currentLOM.transform.eulerAngles;
                }
            }
            if (GUI.Button(new Rect(10, Screen.height - 40, 200, 30), "Back"))
            {
                currentGizmo = null;
                UpdateGizmo(lPlayer, currentLOM);

                currentSelectedKeyframe = null;
                currentMenu = Menu_MainCinametic;
            }

            /*GUILayout.BeginArea(new Rect(0, Screen.height - 210, Screen.width, 200), "", "Box");

            GUILayout.Space(5);
            GUILayout.Label("\tCinametic Editor - Keyframe Editor");
            GUILayout.Space(5);

            scrollKeyframe = GUILayout.BeginScrollView(scrollKeyframe, true, false, GUILayout.Height(50));

            GUILayout.BeginHorizontal(GUILayout.Height(35));

            for (int i = 0; i <= 30; i++)
            {
                GUILayout.Button(i % 5 == 0 ? i.ToString() : "", GUILayout.Width(20));
            }

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            if (GUILayout.Button("Back", GUILayout.Height(30), GUILayout.Width(100)))
                currentMenu = Menu_MainCinametic;

            GUILayout.EndArea();*/
        }

        #endregion

        #endregion

        #region Static

        public static void DisableLevelEditorActive(LocalPlayer lPlayer, bool deleteCurrentObject, CameraResetType disableEditorComplete, bool showCursor)
        {
            if (lPlayer.clientPlayer.currentHoldingObject)
            {
                if (deleteCurrentObject)
                {
                    TNManager.Destroy(lPlayer.clientPlayer.currentHoldingObject.gameObject);
                }
                else
                {
                    lPlayer.clientPlayer.currentHoldingObject.Local_SyncSaveParameters();
                    lPlayer.clientPlayer.currentHoldingObject.ServerSetSelect(false, -1, false);
                }

                instance.gizmosParent.gameObject.SetActive(false);
            }

            if (disableEditorComplete != CameraResetType.None)
            {
                lPlayer.enableLevelEditor = false;
                lPlayer.playerCamera.EnableCamera(disableEditorComplete, !showCursor);
                Cursor.visible = showCursor;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        #endregion

        #region Console Commands

        public string SetLevelEditorEnabled(string[] args)
        {
            if (args.Length == 1)
            {
                bool enabled = false;
                try
                {
                    enabled = bool.Parse(args[0]);
                }
                catch
                {
                    return "The first parameter giving is incorrect, Must be TRUE or FALSE";
                }
                levelEditorEnabled = enabled;
                if (enabled)
                {
                    cinameticEditorEnabled = false;
                    currentMenu = MenuStart;
                }

                return "leveleditor.enabled is " + (enabled ? "enabled." : "disabled.");
            }
            else
            {
                return "The amount of parameters givin are incorrect, leveleditor.enable {TRUE|FALSE}";
            }
        }

        public string SetCinameticEditorEnabled(string[] args)
        {
            if (args.Length == 1)
            {
                bool enabled = false;
                try
                {
                    enabled = bool.Parse(args[0]);
                }
                catch
                {
                    return "The first parameter giving is incorrect, Must be TRUE or FALSE";
                }
                cinameticEditorEnabled = enabled;
                if (enabled)
                {
                    levelEditorEnabled = false;
                    currentMenu = Menu_MainCinametic;
                }

                return "cinametic.enabled is " + (enabled ? "enabled." : "disabled.");
            }
            else
            {
                return "The amount of parameters givin are incorrect, cinametic.enable {TRUE|FALSE}";
            }
        }

        #endregion

        #region Level Editor

        public LevelObjectManager FindLevelObjectManager(Transform startObject)
        {
            Transform currentParent = startObject;
            LevelObjectManager lom = null;

            while (lom == null)
            {
                if (lom = currentParent.GetComponent<LevelObjectManager>())
                {
                    return lom;
                }
                else if (currentParent.parent != null)
                {
                    currentParent = currentParent.parent;
                }
                else if (currentParent.parent == null)
                {
                    break;
                }
            }

            return null;
        }

        [System.Serializable]
        public class LevelObject
        {
            public string name;
            public LevelObjectType levelObjectType;
            public GameObject instantiateObject;
        }

        [System.Serializable]
        public class LevelObjectSaveData
        {
            public bool isLevelObject = true; //Is this object a level object or something different e.g. animated spawn droppod

            public int objectIndex; //The object index number from the global list

            public ObjectState objectState;

            public SerVector3 startPosition = new SerVector3();
            public SerQuaternion startRotation = new SerQuaternion();

            public int objectID; //Used to identify each object for CinameticEditor

            public LevelObjectSaveData SaveVariables(LevelObjectManager lom)
            {
                isLevelObject = lom.isLevelObject;

                objectIndex = lom.objectIndex;

                objectState = lom.currentObjectState;

                startPosition.Set(lom.startPosition);
                startRotation.Set(lom.startRotation);

                objectID = lom.objectID;

                return this;
            }

            public void LoadVariables(LevelObjectManager lom)
            {
                lom.isLevelObject = isLevelObject;

                lom.objectIndex = objectIndex;

                lom.currentObjectState = objectState;

                lom.startPosition = startPosition.Get();
                lom.startRotation = startRotation.Get();

                lom.objectID = objectID;
            }
        }

        #endregion

        #region Cinametic Editor

        void UpdateTimedObjects()
        {
            //Update Time - Handled correctly in the update loop now
            /*if (isPlaying)
                currentTime += Time.deltaTime * playSpeed;*/

            //Calculate data relative to time
            for (int i = 0; i < allTimedObjects.Count; i++)
            {
                TimedObject to = allTimedObjects[i];
                //bool hasNextKeyFrame = to.allKeyFrames.Count >= to.currentKeyFrame;
                bool hasNextKeyFrame = false;

                for (int c = 0; c < to.allKeyFrames.Count; c++)//Get current frame
                    if (to.allKeyFrames[c].frameTime <= currentTime)
                        to.currentKeyFrame = c;

                for (int c = 0; c < to.allKeyFrames.Count; c++)//Get next frame if its there
                {
                    if (to.allKeyFrames[c].frameTime > to.allKeyFrames[to.currentKeyFrame].frameTime)
                    {
                        to.nextKeyFrame = c;
                        hasNextKeyFrame = true;
                        break;
                    }
                }

                if (hasNextKeyFrame)//Calculate data with deltatime
                {
                    for (int c = 0; c < to.allKeyFrames.Count; c++)
                        if (to.allKeyFrames[c].frameTime < currentTime)
                            to.currentKeyFrame = c;

                    TimedObject.KeyFrame current = to.allKeyFrames[to.currentKeyFrame];
                    TimedObject.KeyFrame next = to.allKeyFrames[to.nextKeyFrame];

                    float timeDifference = next.frameTime - current.frameTime;
                    float relativeTime = currentTime - current.frameTime;
                    float deltaTime = relativeTime / timeDifference;

                    Vector3 nextPosition = Math.Vector3Lerp(current.position, next.position, deltaTime);
                    Vector3 nextRotation = Math.Vector3Lerp(current.rotation, next.rotation, deltaTime, true);

                    to.targetTransform.localPosition = nextPosition;
                    to.targetTransform.localEulerAngles = nextRotation;
                }
                else//Return current keyframe data
                {
                    TimedObject.KeyFrame current = to.allKeyFrames[to.currentKeyFrame];

                    Vector3 nextPosition = current.position;
                    Vector3 nextRotation = current.rotation;

                    to.targetTransform.localPosition = nextPosition;
                    to.targetTransform.localEulerAngles = nextRotation;
                }
            }
        }

        [System.Serializable]
        public class TimedObject
        {
            public int levelOjectID;
            public LevelObjectManager levelObjectManager;
            public Transform targetTransform;

            public int currentKeyFrame;
            public int nextKeyFrame;
            public List<KeyFrame> allKeyFrames = new List<KeyFrame>();


            [System.Serializable]
            public class KeyFrame
            {
                public float frameTime;

                public bool hasPosition;
                public Vector3 position;

                public bool hasRotation;
                public Vector3 rotation;
            }

            public KeyFrame GetKeyframeOnTime(float time)
            {
                for (int i = 0; i < allKeyFrames.Count; i++)
                    if (allKeyFrames[i].frameTime == time)
                        return allKeyFrames[i];
                return null;
            }

            public KeyFrame SetKeyframeOnTime(float time)
            {
                KeyFrame kf = GetKeyframeOnTime(time);
                if (kf == null)
                {
                    kf = new KeyFrame();

                    kf.frameTime = time;

                    kf.hasPosition = true;
                    kf.hasRotation = true;

                    kf.position = targetTransform.position;
                    kf.rotation = targetTransform.eulerAngles;

                    allKeyFrames.Add(kf);
                }
                return kf;
            }

            public static TimedObject GetTimedObject(LevelObjectManager lom)
            {
                for (int i = 0; i < instance.allTimedObjects.Count; i++)
                    if (instance.allTimedObjects[i].levelObjectManager == lom)
                        return instance.allTimedObjects[i];
                return null;
            }

            public static TimedObject CreateAndRegister(LevelObjectManager lom)
            {
                TimedObject to = GetTimedObject(lom);
                if (to != null)
                    return to;
                else
                {
                    to = new TimedObject();

                    to.levelOjectID = lom.objectID;
                    to.levelObjectManager = lom;
                    to.targetTransform = lom.transform;

                    instance.allTimedObjects.Add(to);

                    return to;
                }
            }
        }

        #endregion
    }

    [System.Serializable]
    public class LevelSettings
    {
        public string customLevelName = "";
        public string mapName = "";

        public List<GameModeType> availableGamemodes = new List<GameModeType>();

        public List<LevelManager.LevelObjectSaveData> allObjects;

        public int lastObjectID;

        //LevelOptions, Weather, etc...
    }

    public enum LevelObjectType
    {
        Weapon,
        Vehicle,
        Spawn,
        Objective,
        Scenery,
        Structure
    }
}