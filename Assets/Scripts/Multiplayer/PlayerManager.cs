using UnityEngine;
using System.Collections;
using TNet;
using System;

namespace AngryRain.Multiplayer
{
    public class PlayerManager : TNBehaviour
    {
        public GameObject thisGameObject;
        public ClientPlayer clientPlayer;
        public LocalPlayer localPlayer { get; set; }
        public int pManagerID;
        
        public System.Collections.Generic.List<DamageReceiver> damageReceivers;

        public CharacterRendering characterRendering = new CharacterRendering();

        public void Initialize()
        {
            MultiplayerManager.GetPlayers()[pManagerID].playerManager = this;
            clientPlayer = MultiplayerManager.GetPlayers()[pManagerID];

            //if (mPlayer.isConnected)
                //multiplayerObject.syncTarget = mPlayer.isHost ? MultiplayerObject.TargetSync.HostToClient : MultiplayerObject.TargetSync.ClientToHost;

            if (clientPlayer.isMe)
            {
                localPlayer = LocalPlayerManager.localPlayers[clientPlayer.lPlayerIndex];
                //tno.ownerID = TNManager.player.id;
            }

            playerCharacter.Awake();
            playerPhysics.Initialize();
            InitializeRenderers();
            //GetComponent<VoiceChat>().Initialize(mPlayer);
        }

        float lastSend;
        bool canUseAction;

        public ExecutionStatus executeStatus { private set; get; }

        //Reference Variables
        public PlayerController playerController;
        public PlayerCharacterController playerCharacter;
        public PlayerPhysics playerPhysics;
        public SyncNetworkObject multiplayerObject;
        //public MultiplayerObject multiplayerObject;

        public SoundSettings soundSettings = new SoundSettings();

        #region Monobehaviours

        void Start()
        {
            //InitializeRenderers();
        }

        void Update()
        {
            if (clientPlayer == null) return;

            if (clientPlayer.isMe)
            {
                if (clientPlayer.isAlive)
                {
                    playerPhysics.transform.position = playerController.transform.position;
                    playerPhysics.transform.rotation = playerController.transform.rotation;
                    playerPhysics.rigidbody.velocity = playerController.playerMovement.rigidbody.velocity;
                    playerPhysics.rigidbody.angularVelocity = playerController.playerMovement.rigidbody.angularVelocity;

                    playerCharacter.animationSettings.velocity = playerController.playerMovement.rigidbody.velocity;

                    canUseAction = true;

                    if (Input.GetKeyDown(KeyCode.P))//Suicide
                        tno.Send(15, Target.Host, clientPlayer.mPlayerID, -2, -1);

                    playerCharacter.animationSettings.lookRotation = new Vector2(playerController.playerCamera.eulerAngles.x, 0);
                    UpdateExecutions();
                    CheckForThirdPersonDebugInput();
                    if (Input.GetKeyDown(KeyCode.F) && canUseAction && !LocalPlayerManager.localPlayers[0].enableLevelEditor)
                    {
                        CheckForThirdPersonDebugInput();
                        if (clientPlayer.vehicle == null && mVehicleTemp != null)
                        {
                            canUseAction = false;
                            Action_EnterVehicle();
                        }
                        else if (clientPlayer.vehicle != null)
                        {
                            canUseAction = false;
                            Action_LeaveVehicle();
                        }
                        else if (currentObjective == null && tempObjective != null)
                        {
                            canUseAction = false;
                            Action_ActivateObjective();
                        }
                    }
                }
            }

            //fire weapon if player isFiring
            if (isFiring)
                playerCharacter.FireAutomatic();
        }

        void FixedUpdate()
        {
            if (!MultiplayerManager.instance)
                return;

            if (clientPlayer != null && clientPlayer.isAlive)
            {
                if (clientPlayer.isMe)
                {
                    if (Time.time > lastSend)
                    {
                        lastSend = Time.time + 0.05f;
                        tno.SendQuickly(10, Target.Others, new Vector2(playerController.playerCamera.eulerAngles.x, 0));
                    }

                    HandleVehicle();
                    HandleObjectives();
                }
                else if(multiplayerObject.cs_Buffer.Count > 0)
                { 
                    #region Set Character Variables

                    PlayerCharacterController cac = playerCharacter;
                    cac.animationSettings.velocity = multiplayerObject.cs_Buffer[0].velocity;

                    #endregion
                }
            }
        }

        #endregion

        #region Receiving and Handeling Damage Server/Client

        public void Local_ReceiveDamage(DamageGiver dGiver, DamageReceiver dReceiver)
        {
            if (dGiver != null) {
				if(dGiver.mPlayer != null){
					tno.SendQuickly (15, Target.Host, dGiver.mPlayer.mPlayerID, dGiver.mWeapon, dReceiver.thisIndex);
				}
				else{
					tno.SendQuickly (14, Target.Host, dGiver.mWeapon, dReceiver.thisIndex);
				}
			}
		}

        [RFC(15)]
        public void Server_ReceiveDamage(int playerID, int weaponID, int dReceiverIndex)
        {
            try
            {
                if (clientPlayer != null && clientPlayer.isAlive)
                {
                    float damage;
                    if (weaponID == -2)//Suicide
                        damage = 100;
                    else
                    {
                        ServerWeaponInfo wInfo = CustomizationManager.GetServerWeapon(weaponID);
                        damage = wInfo != null ? wInfo.weaponDamage.GetDamage(0) : 0;
                    }

                    clientPlayer.health -= damage;

                    Server_CheckHealth(playerID, weaponID, dReceiverIndex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.StackTrace);
            }
        }

        public void Server_ReceiveDamageVelocity(int playerID, int weaponID, Vector3 velocity)
        {
            try
            {
                if (clientPlayer.isAlive)
                {
                    float damage;
                    if (weaponID == -2)//Suicide
                        damage = 100;
                    else
                    {
                        ServerWeaponInfo wInfo = CustomizationManager.GetServerWeapon(weaponID);
                        damage = wInfo != null ? wInfo.weaponDamage.GetDamage(0) : 0;
                    }

                    clientPlayer.health -= damage;

                    Server_CheckHealth(playerID, weaponID, -1);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.StackTrace);
            }
        }

        public void Server_ReceiveDamageVelocity(int playerID, float damper, int weaponID, Vector3 velocity)
        {
            try
            {
                if (clientPlayer != null && clientPlayer.isAlive)
                {
                    float damage;
                    if (weaponID == -2)//Suicide
                        damage = 100;
                    else
                    {
                        ServerWeaponInfo wInfo = CustomizationManager.GetServerWeapon(weaponID);
                        damage = wInfo != null ? wInfo.weaponDamage.maximumDamage : 0;
                    }

                    clientPlayer.health -= damage * damper;

                    Server_CheckHealth(playerID, weaponID, -1);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.StackTrace);
            }
        }

        void Server_CheckHealth(int playerID, int weaponID, int dReceiverIndex)
        {
            tno.SendQuickly(16, Target.All, playerID);
            if (clientPlayer.health <= 0 && clientPlayer.isAlive && clientPlayer.isConnected)
            {
                ClientPlayer killer = MultiplayerManager.GetPlayer(playerID);

                MultiplayerManager.instance.ServerUpdateVariable(MultiplayerManager.SyncTarget.health, 0f, clientPlayer, true);
                MultiplayerManager.instance.ServerUpdateVariable(MultiplayerManager.SyncTarget.alive, false, clientPlayer, true);

                if (clientPlayer.mPlayerID != playerID && weaponID != -2)
                {
                    MultiplayerManager.instance.ServerUpdateVariable(MultiplayerManager.SyncTarget.kills, killer.kills + 1, killer, true);
                    MultiplayerManager.instance.ServerUpdateVariable(MultiplayerManager.SyncTarget.score, killer.score + 10, killer, true);
                }

                MultiplayerManager.instance.ServerUpdateVariable(MultiplayerManager.SyncTarget.deaths, clientPlayer.deaths + 1, clientPlayer, true);

                StartCoroutine(Server_HandleSpawnScreenDelay());

                tno.Send("Client_SetPlayerAliveState", Target.All, playerID, false, multiplayerObject.cs_Buffer[0].pos, multiplayerObject.cs_Buffer[0].rot, dReceiverIndex, (byte)PlayerClass.Assault);
                
                MultiplayerManager.Server_CheckMatchScore();

                //Write on the message board how he died
                if (clientPlayer.mPlayerID == playerID)//Suicide
                {
                    if (weaponID == 500)
                        MultiplayerManager.instance.AddKillText(killer.playerName + " fragged himself, epic fail", 3, playerID);
                    else
                        MultiplayerManager.instance.AddKillText(killer.playerName + " killed himself", 3, playerID);
                }
                else if (weaponID == 500)//Grenade
                    MultiplayerManager.instance.AddKillText(killer.playerName + " fragged " + clientPlayer.playerName, 2, playerID);
                else
                    MultiplayerManager.instance.AddKillText(killer.playerName + " killed " + clientPlayer.playerName, 0, playerID);
            }
            else
            {
                MultiplayerManager.instance.ServerUpdateVariable(MultiplayerManager.SyncTarget.health, clientPlayer.health, clientPlayer, false);
            }
        }

        [RFC(16)]
        void Client_ReceiveDamage(int shooterID)
        {
            if (clientPlayer.isMe)//If this player gets hit is me
            {
                playerController.playerCamera.ActivateGetHitEffect();
                localPlayer.playerGUI.EnableGitHitEffect();
                localPlayer.playerGUI.playerInfo.UpdatePlayerHealth(clientPlayer.health);
            }

            ClientPlayer cp = MultiplayerManager.GetPlayer(shooterID);
            if (cp.isMe)
            {
                cp.playerManager.localPlayer.playerGUI.EnableHitmarker();
            }
        }

        [RFC(12)]
        void Client_SpawnPlayer(Vector3 position, Vector3 rotation, byte playerClass)
        {
            Client_SetPlayerAliveState(-1, true, position, rotation, -1, playerClass);
        }

        [RFC]
        void Client_SetPlayerAliveState(int killerID, bool isAlive, Vector3 position, Vector3 rotation, int dReceiverIndex, byte playerClassByte)
        {
            try
            {
                executeStatus = ExecutionStatus.None;
                isFiring = false;

                PlayerControllerGUI.allInstances[0].UpdateNametag(clientPlayer);

                if (isAlive)
                {
                    clientPlayer.playerClass = (PlayerClass)playerClassByte;
                    multiplayerObject.enabled = true;
                    multiplayerObject.ForceSync();

                    if (clientPlayer.isMe)
                    {
                        playerController.transform.position = position;
                        playerController.transform.eulerAngles = rotation;

                        playerController.gameObject.SetActive(true);

                        playerCharacter.gameObject.SetActive(true);
                        playerCharacter.DisableAllWeapons();

                        playerController.ResetAllWeaponMagazines();
                        SetCharacterRenderingMode(CharacterRenderingMode.None);
                        playerController.playerMovement.GetComponent<Rigidbody>().isKinematic = false;
                        playerController.playerMovement.GetComponent<Rigidbody>().velocity = Vector3.zero;
                        playerController.SetStance(PlayerStance.Standing);

                        playerController.playerCamera.ResetCameraEffects();

                        playerController.playerVariables.cameraFollowHeadRotation = false;
                        playerController.playerVariables.isAiming = false;
                        playerController.playerVariables.isBoltingWeapon = false;
                        playerController.playerVariables.isReloading = false;
                        playerController.playerVariables.isSwitchingWeapons = false;

                        playerController.playerVariables.canShoot = true;
                        playerController.playerVariables.canAim = true;

                        localPlayer.playerGUI.crosshair.gameObject.SetActive(true);
                        localPlayer.playerGUI.playerInfo.gameObject.SetActive(true);
                        localPlayer.playerGUI.playerInfo.UpdatePlayerHealth(clientPlayer.health);

                        playerController.SetAimDelay(1);
                        playerController.SetFireDelay(1);

                        if (clientPlayer.playerClass == PlayerClass.Editor)
                        {
                            playerController.playerCamera.EnableCamera(playerController.transform, CameraResetType.PositionAndRotation, false, false, CameraType.LevelEditor);
                        }
                        else
                        {
                            playerController.playerCamera.EnableCamera(CameraResetType.PositionAndRotation, true);
                            playerController.SwitchWeapon(CustomizationManager.WeaponSpot.Primary, true);
                        }

                        StopCoroutine("HandleDeathAnimation");
                    }
                    else
                    {
                        SetCharacterRenderingMode(CharacterRenderingMode.ThirdPerson);
                    }

                    Local_SetStance(PlayerStance.Standing);

                    playerPhysics.transform.position = position;
                    playerPhysics.transform.eulerAngles = rotation;

                    playerCharacter.animator.enabled = true;

                    if (MultiplayerManager.matchSettings.modeSettings.matchSettings.isTeamMode && clientPlayer.team != null)
                        playerCharacter.SetTeamColor(clientPlayer.team.index);

                    playerCharacter.gameObject.SetActive(true);

                    if (TNManager.isHosting)
                    {
                        multiplayerObject.SetRole(clientPlayer.isHost ? FlowType.ServerToClient : FlowType.ClientToServer);
                        SyncTransform(true);
                        Local_SetRagdoll(false, null);
                    }

                    //This is required as you dont want the character to bounce against it own colliders
                    playerPhysics.rigidbody.isKinematic = false;//mPlayer.isMe;
                    playerPhysics.rigidbody.detectCollisions = !clientPlayer.isMe;
                    playerPhysics.collider.enabled = !clientPlayer.isMe;
                }
                else
                {
                    multiplayerObject.ForceSync();
                    playerCharacter.animator.enabled = false;
                    playerPhysics.rigidbody.isKinematic = true;
                    playerPhysics.collider.enabled = false;

                    if (clientPlayer.isMe)
                    {
                        playerCharacter.gameObject.SetActive(true);

                        localPlayer.playerGUI.playerInfo.gameObject.SetActive(false);
                        localPlayer.playerGUI.crosshair.gameObject.SetActive(false);

                        StartCoroutine("HandleDeathAnimation");

                        playerController.gameObject.SetActive(false);
                    }

                    if (killerID != clientPlayer.mPlayerID)
                        MultiplayerManager.GetPlayer(killerID).playerManager.PlayVoice(VoiceType.EnemyDown);
                    PlayVoice(VoiceType.DeathShout);

                    SetCharacterRenderingMode(CharacterRenderingMode.ThirdPerson);

                    //Give the last hit rigidbody a extra boost for effect
                    if (dReceiverIndex != -1)
                    {
                        Vector3 force = (position - MultiplayerManager.GetPlayer(killerID).playerManager.multiplayerObject.GetState(0).pos).normalized * 20;
                        damageReceivers[dReceiverIndex].GetComponent<Collider>().attachedRigidbody.AddForce ( force, ForceMode.VelocityChange );
                        Debug.DrawLine(position, force, Color.red, 5);
                    }

                    if(TNManager.isHosting)
                    {
                        Vector3[] allVelocity = new Vector3[damageReceivers.Count];
                        for (int i = 0; i < allVelocity.Length; i++)
                            allVelocity[i] = damageReceivers[i].GetComponent<Rigidbody>().velocity;
                        Local_SetRagdoll(true, allVelocity);

                        if (clientPlayer.vehicle != null)
                        {
                            clientPlayer.vehicle.Server_RequestVehicleUpdate(false, clientPlayer.mPlayerID, -1);
                                //clientPlayer.vehicle.Server_RequestVehicleReset(clientPlayer.mPlayerID);
                        }
                        else if(clientPlayer.vehicleSeat != null)
                        {
                            tno.Send("ForceResetVehicle", Target.All);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(gameObject.name + ", " + ex);
            }
        }

        IEnumerator HandleDeathAnimation()
        {
            SetCharacterRenderingMode(CharacterRenderingMode.ThirdPerson);
            SetCharacterRenderingForSplitscreen(false);

            Transform camera = playerController.playerCamera.transform;
            playerController.playerCamera.EnableCamera(null, CameraResetType.None, false, false, CameraType.None);
            playerController.playerCamera.cameraSettings.enableControls = false;
            playerController.playerCamera.camera.fieldOfView = 65;

            Vector3 targetPosition = camera.TransformPoint(Vector3.back*4f);

            Vector3 targetRotation = camera.eulerAngles;
            playerController.playerCamera.SetRotation(targetRotation);
            Vector3 newInput = targetRotation;

            while (!clientPlayer.isAlive)
            {
                yield return new WaitForEndOfFrame();

                newInput.x -= playerController.input.GetAxis("Look Vertical");
                newInput.y += playerController.input.GetAxis("Look Horizontal");
                camera.rotation = Quaternion.Slerp(camera.rotation, Quaternion.Euler(newInput), Time.deltaTime * 20);

                targetPosition = Vector3.Lerp(targetPosition, playerCharacter.animator.GetBoneTransform(HumanBodyBones.Chest).position + (Quaternion.Euler(newInput)*new Vector3(0,0,-5)) + Vector3.up, Time.deltaTime * 50);

                playerController.playerCamera.camera.fieldOfView = Mathf.Lerp(playerController.playerCamera.camera.fieldOfView, 60, Time.deltaTime * 10);
                camera.position = Vector3.Lerp(camera.position, targetPosition, Time.deltaTime * 10);
            }

            /*Transform pCameraTransform = playerController.thisPlayerCamera.thisTransform;
            Vector3 targetPosition = playerController.transform.TransformPoint(Vector3.back * 3.5f);
            Transform targetTransform = playerCharacter.characterHead;

            targetPosition.y = playerController.transform.position.y + 2.5f;

            Vector3 dampSmooth = Vector3.zero;

            while (!mPlayer.pVars.pIsAlive)
            {
                yield return new WaitForFixedUpdate();
                pCameraTransform.position = Vector3.SmoothDamp(pCameraTransform.position, targetPosition, ref dampSmooth, 0.5f, 3f, Time.fixedDeltaTime);
                pCameraTransform.rotation = Quaternion.Slerp(pCameraTransform.rotation, Quaternion.LookRotation(targetTransform.position - pCameraTransform.position + Vector3.up), Time.fixedDeltaTime * 0.5f);
            }*/
        }

        IEnumerator Server_HandleSpawnScreenDelay()
        {
            yield return new WaitForSeconds(4);
            if (clientPlayer.isMe)
                MultiplayerManager.instance.Client_SetSpawnScreen(true, clientPlayer.mPlayerID);
            else
                MultiplayerManager.instance.tno.Send("Client_SetSpawnScreen", clientPlayer.tPlayer, true, clientPlayer.mPlayerID);
        }

        [RFC]
        public void Local_SetRagdoll(bool enable, Vector3[] velocity) { tno.Send("Client_SetRagdoll", Target.All, enable, velocity); }

        [RFC]
        public void Client_SetRagdoll(bool enable, Vector3[] velocity)
        {
            int c = damageReceivers.Count;
            for (int i = 0; i < c; i++)
            {
                if (damageReceivers[i].GetComponent<Rigidbody>() != playerPhysics)
                {
                    DamageReceiver dReceiver = damageReceivers[i];

                    dReceiver.GetComponent<Rigidbody>().isKinematic = !enable;
                    dReceiver.GetComponent<Rigidbody>().detectCollisions = enable;
                    if (enable && velocity != null && velocity.Length == damageReceivers.Count)
                        dReceiver.GetComponent<Rigidbody>().velocity = velocity[i];
                }
            }

            playerPhysics.rigidbody.isKinematic = enable;
        }

        #endregion

        #region Vehicles

        /*public float timePressedActionButton;
        public bool hasPressedActionButton;*/

        [HideInInspector]
        public MultiplayerVehicle mVehicleTemp;
        [HideInInspector]
        public RaycastHit[] allHits;
        float lastCheck;

        void HandleVehicle()
        {
            allHits = playerController.playerCamera.allRaycastHits;
            //Enterin vehicle withouth action menu
            if (allHits != null && clientPlayer.vehicle == null)
            {
                mVehicleTemp = null;
                MultiplayerVehicle mVehicle = null;
                int c = allHits.Length;
                for (int i = 0; i < c; i++)
                {
                    Rigidbody vRig = allHits[i].rigidbody;
                    if (vRig)
                    {
                        mVehicle = vRig.GetComponent<MultiplayerVehicle>();
                        if (mVehicle != null && allHits[i].distance <= 2f)
                        {
                            mVehicleTemp = mVehicle;
                        }
                    }
                }
            }
        }

        void Action_EnterVehicle()
        {
            mVehicleTemp.Local_RequestVehicleUpdate(true, clientPlayer, -1);
        }

        void Action_LeaveVehicle()
        {
            try
            {
                if (clientPlayer.vehicle)
                    clientPlayer.vehicle.Local_RequestVehicleUpdate(false, clientPlayer, -1);
            }
            catch(System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public void CheckVehicleData()
        {
            if(clientPlayer.vehicle != null && clientPlayer.vehicle.GetPlayerInSeat(clientPlayer.mPlayerID) == null)
            {
                tno.Send("ForceResetVehicle", Target.All);
            }
        }

        [RFC]
        void ForceResetVehicle()
        {
            clientPlayer.vehicle = null;
            clientPlayer.vehicleSeat = null;
        }

        #endregion

        #region Objectives

        public MultiplayerObjective currentObjective { get; set; }
        public MultiplayerObjective tempObjective { get; set; }
        float searchTimeCooldown;

        void HandleObjectives()
        {
            if (allHits != null && currentObjective == null && Time.time > searchTimeCooldown)
            {
                tempObjective = null;
                MultiplayerObjective mObj = null;
                int c = allHits.Length;
                for (int i = 0; i < c; i++)
                {
                    Rigidbody vRig = allHits[i].rigidbody;
                    if (vRig)
                    {
                        mObj = vRig.GetComponent<MultiplayerObjective>();
                        if (mObj != null && allHits[i].distance <= 2f && mObj.progress < 1)
                        {
                            tempObjective = mObj;
                            break;
                        }
                    }
                }
            }
        }

        void Action_ActivateObjective()
        {
            tempObjective.Local_RequestActivation(clientPlayer);
            tempObjective = null;

            searchTimeCooldown = Time.time + 2;
        }

        #endregion

        #region Actions

        #region Firing

        public bool isFiring;

        public void Local_UpdateFiring(bool fire, FiringMode firingMode)
        {
            tno.Send("Client_SetFiring", Target.Others, fire, firingMode != FiringMode.Automatic);
            Client_SetFiring(fire, firingMode != FiringMode.Automatic);
        }

        [RFC]
        void Client_SetFiring(bool fire, bool semi)
        {
            if (semi && fire)
            {
                playerCharacter.FireSemiAutomatic();
                playerCharacter.animator.CrossFade("AR Fire Semi", 0.05f, 1, 0);
            }
            else
            {
                isFiring = fire;
                playerCharacter.animator.SetBool("isFiring", fire);
            }
        }

        public void Local_FireServerSideWeapon(FiringType firingType, int weaponID, Vector3 position, Quaternion rotation)
        {
            tno.Send(20, Target.Host, (byte)firingType, position, rotation, weaponID);
        }

        [RFC(20)]
        public void Server_FireServerSideWeapon(byte ft, Vector3 position, Quaternion rotation, int weaponID)
        {
            FiringType firingType = (FiringType)ft;
            TNManager.CreateEx(12, false, CustomizationManager.instance.grenade.gameObject, position, rotation, weaponID, clientPlayer.mPlayerID);
        }

        #endregion

        #region Weapon Switching

        public void Local_SwitchWeapon(string weapon)
        {
            tno.Send("Client_SwitchWeapon", Target.All, weapon);
        }

        [RFC]
        void Client_SwitchWeapon(string weapon)
        {
            playerCharacter.SwitchWeapon(weapon);
            if (clientPlayer.isMe)
                playerCharacter.DisableAllWeapons();
        }

        #endregion

        #region Lookat

        [RFC(10)]
        void Client_UpdateSyncVariables(Vector2 lookAt)
        {
            playerCharacter.animationSettings.lookRotation = lookAt;
        }

        #endregion

        #region Stances

        public void Local_SetStance(PlayerStance playerStance)
        {
            tno.Send("Client_SetStance", Target.All, (byte)playerStance);
        }

        [RFC]
        void Client_SetStance(byte newStance)
        {
            PlayerStance nextStance = (PlayerStance)newStance;
            playerCharacter.SetStance(nextStance);

            CapsuleCollider targetCollider = playerPhysics.GetComponent<CapsuleCollider>();
            targetCollider.center = GetStanceCenter(nextStance);
            if (nextStance == PlayerStance.Standing)
            {
                targetCollider.height = 1.8f;
                targetCollider.direction = 1;
            }
            else if (nextStance == PlayerStance.Crouching)
            {
                targetCollider.height = 1.5f;
                targetCollider.direction = 1;
            }
            else
            {
                targetCollider.height = 0.5f;
                targetCollider.direction = 1;
            }
        }

        Vector3 GetStanceCenter(PlayerStance stance)
        {
            if (stance == PlayerStance.Standing)
            {
                return new Vector3(0, 0.9f, 0);
            }
            else if (stance == PlayerStance.Crouching)
            {
                return new Vector3(0, 0.75f, 0);
            }
            else
                return Vector3.zero;
        }

        #endregion

        #region isGrounded

        public void SetGrounded(bool isGrounded)
        {
            tno.Send("Client_SetGrounded", Target.All, isGrounded);
        }

        [RFC]
        public void Client_SetGrounded(bool isGrounded)
        {
            playerCharacter.isGrounded = isGrounded;
        }

        #endregion

        #region Debug Third Person

        void CheckForThirdPersonDebugInput()
        {
            if(clientPlayer.vehicle == null && canUseAction)
            {
                canUseAction = false;
                if (Input.GetKeyDown(KeyCode.KeypadEnter))
                    SwitchThirdPersonMode();
            }
        }

        int mode = 0;//0 = normal, 1 = directbehind, 2 is properthirdperson, 3 is free mode

        void SwitchThirdPersonMode()
        {
            mode = (mode + 1) % 4;

            if (mode == 0)
            {
                localPlayer.playerCamera.EnableCamera(CameraResetType.PositionAndRotation, true);
                SetCharacterRenderingMode(CharacterRenderingMode.None);
            }
            else if (mode == 1)
            {
                localPlayer.playerCamera.EnableCamera(CameraResetType.PositionAndRotation, true);
                localPlayer.playerCamera.transform.localPosition = new Vector3(0,-0.3f,-3);
                SetCharacterRenderingMode(CharacterRenderingMode.ThirdPerson);
            }
            else if (mode == 2)
            {
                localPlayer.playerCamera.EnableCamera(CameraResetType.PositionAndRotation, true);
                localPlayer.playerCamera.transform.localPosition = new Vector3(0.5f, 0.1f, -2.5f);
                SetCharacterRenderingMode(CharacterRenderingMode.ThirdPerson);
            }
            else if (mode == 3)
            {
                localPlayer.playerCamera.EnableCamera(CameraResetType.PositionAndRotation, true, false, CameraType.None);
                SetCharacterRenderingMode(CharacterRenderingMode.ThirdPerson);
            }
        }

        #endregion

        #endregion

        #region Sync Input

        public InputSync lastInputSync = new InputSync() { horizontal = 0, vertical = 0, actionButton = false };

        void UpdateInput()
        {
            InputSync sync = new InputSync();
            Vector3 dir = playerController.playerMovement.inputDir;

            sync.horizontal = dir.x;
            sync.vertical = dir.z;

            sync.actionButton = Input.GetKey(KeyCode.F);
            sync.jumpButton = playerController.input.GetButton("Jump");
            if (TNManager.isHosting)
                lastInputSync = sync;
            else
                tno.SendQuickly(200, Target.Host, sync.Serialize<InputSync>());
        }

        [RFC(200)]
        protected void Server_ReceiveInput(byte[] input)
        {
            lastInputSync = input.Deserialize<InputSync>();
        }

        public void SyncTransform(bool local)
        {
            tno.Send(201, Target.All, local ? playerPhysics.transform.localPosition : playerPhysics.transform.position,
                local ? playerPhysics.transform.localEulerAngles : playerPhysics.transform.eulerAngles, local);
        }

        [RFC(201)]
        protected void Client_SetTransform(Vector3 pos, Vector3 rot, bool local)
        {
            if (clientPlayer.isMe)
            {
                if (local)
                {
                    playerController.transform.localPosition = pos;
                    playerController.transform.localEulerAngles = rot;
                }
                else
                {
                    playerController.transform.position = pos;
                    playerController.transform.eulerAngles = rot;
                }

                playerController.playerMovement.ResetMovement();
            }

            if (local)
            {
                playerPhysics.transform.localPosition = pos;
                playerPhysics.transform.localEulerAngles = rot;
            }
            else
            {
                playerPhysics.transform.position = pos;
                playerPhysics.transform.eulerAngles = rot;
            }

            if (!playerPhysics.rigidbody.isKinematic)
                playerPhysics.rigidbody.velocity = Vector3.zero;

            multiplayerObject.ForceSync();
        }

        [System.Serializable]
        public struct InputSync
        {
            public float horizontal;
            public float vertical;

            public bool actionButton;

            public bool jumpButton;
        }

        #endregion

        #region Execution

        void UpdateExecutions()
        {
            if (playerController.playerCamera.allRaycastHits == null || !canUseAction)
                return;

            allHits = playerController.playerCamera.allRaycastHits;
            int c = allHits.Length;
            for (int i = 0; i < c; i++)
            {
                PlayerPhysics pp = allHits[i].collider.GetComponent<PlayerPhysics>();
                if(pp != null && pp.playerManager != this && Input.GetKeyDown(KeyCode.F))
                {
                    canUseAction = false;
                    Local_RequestExecution(pp.playerManager);
                }
            }
        }

        public void Local_RequestExecution(PlayerManager victim)
        {
            tno.Send("Server_HandleExecution", Target.Host, victim.clientPlayer.mPlayerID);
        }

        [RFC]
        protected void Server_HandleExecution(int victimID)
        {
            ClientPlayer victim = MultiplayerManager.GetPlayer(victimID);

            if (clientPlayer.isAlive && victim.isAlive && executeStatus == ExecutionStatus.None && victim.playerManager.executeStatus == ExecutionStatus.None)
            {
                if(playerCharacter.playerStance == PlayerStance.Standing && victim.playerManager.playerCharacter.playerStance == PlayerStance.Standing)
                {
                    tno.Send("Client_PlayExecution", Target.All, victimID);
                }
            }
        }

        [RFC]
        protected void Client_PlayExecution(int victimID)
        {
            StopCoroutine("ExecutionTimer");
            StartCoroutine("ExecutionTimer", MultiplayerManager.GetPlayer(victimID).playerManager);
        }

        protected IEnumerator ExecutionTimer(PlayerManager victim)
        {
            playerCharacter.PlayExecutionAnimation(false, true);
            victim.playerCharacter.PlayExecutionAnimation(true, true);

            playerPhysics.transform.position = victim.playerPhysics.transform.TransformPoint(Vector3.back * 0.1194f);
            playerPhysics.transform.rotation = victim.playerPhysics.transform.rotation;

            executeStatus = ExecutionStatus.Killer;
            victim.executeStatus = ExecutionStatus.Victim;

            if(TNManager.isHosting)
            {
                SyncTransform(false);
                victim.SyncTransform(false);
            }

            if (clientPlayer.isMe)
            {
                /*Vector3 lookRot = playerController.transform.position - victim.playerPhysics.transform.position;
                lookRot.y = 0;
                playerController.transform.rotation = Quaternion.LookRotation(lookRot);*/
                playerController.transform.position = playerPhysics.transform.position;
                playerController.playerCamera.SetRotation(victim.playerPhysics.transform.eulerAngles);
                playerController.playerCamera.cameraSettings.enableControls = false;
                playerController.playerVariables.cameraFollowHeadRotation = true;
                SetCharacterRenderingMode(CharacterRenderingMode.FirstPersonWithArms);
            }

            if (victim.clientPlayer.isMe)
            {
                //objectHolder.syncRigidbody.Sync();
                victim.playerController.playerVariables.cameraFollowHeadRotation = true;
                victim.SetCharacterRenderingMode(CharacterRenderingMode.FirstPersonWithArms);
            }
            
            yield return new WaitForSeconds(3f);
            victim.executeStatus = ExecutionStatus.None;
            if (TNManager.isHosting)
                victim.Server_ReceiveDamageVelocity(clientPlayer.mPlayerID, -3, Vector3.zero);
            yield return new WaitForSeconds(0.5f);

            if (clientPlayer.isMe)
            {
                playerController.playerVariables.cameraFollowHeadRotation = false;
                SetCharacterRenderingMode(CharacterRenderingMode.FirstPerson);
            }

            executeStatus = ExecutionStatus.None;
            playerCharacter.PlayerAction(0);
            victim.playerCharacter.PlayerAction(0);
        }

        public enum ExecutionStatus
        {
            None,
            Killer,
            Victim
        }

        #endregion

        #region Character Rendering

        public CharacterRenderingMode currentCharacterRenderingMode { get; private set; }

        /// <summary>
        /// First dimension is based on the PlayerClass enum, 0-3 are the player classes, 4 are the base renderers, 5 is the forge editor renderer, Second dimension are the renderers
        /// </summary>
        public Renderer[] characterRenderers;
        GameObject[] characterRenderersGameObjects;
        int characterLayerDisabled = 1, characterLayerEnabled = 9;

        public void InitializeRenderers()
        {
            characterRenderers = new Renderer[19];
            characterRenderersGameObjects = new GameObject[19];

            //Assault
            characterRenderers[0] = transform.Find("PlayerPhysics/Character/Assault_Equipment001").GetComponent<Renderer>();
            characterRenderers[1] = transform.Find("PlayerPhysics/Character/Assault_Head001").GetComponent<Renderer>();
            characterRenderers[2] = transform.Find("PlayerPhysics/Character/Assault_Vest001").GetComponent<Renderer>();

            //Engineer
            characterRenderers[3] = transform.Find("PlayerPhysics/Character/Specialist_Equipment001").GetComponent<Renderer>();
            characterRenderers[4] = transform.Find("PlayerPhysics/Character/Specialist_Head001").GetComponent<Renderer>();
            characterRenderers[5] = transform.Find("PlayerPhysics/Character/Specialist_Vest001").GetComponent<Renderer>();

            //support
            characterRenderers[6] = transform.Find("PlayerPhysics/Character/Support_Equipment001").GetComponent<Renderer>();
            characterRenderers[7] = transform.Find("PlayerPhysics/Character/Support_Head001").GetComponent<Renderer>();
            characterRenderers[8] = transform.Find("PlayerPhysics/Character/Support_Vest001").GetComponent<Renderer>();

            //Marksman
            characterRenderers[9] = transform.Find("PlayerPhysics/Character/Marksman_Equipment001").GetComponent<Renderer>();
            characterRenderers[10] = transform.Find("PlayerPhysics/Character/Marksman_Head001").GetComponent<Renderer>();
            characterRenderers[11] = transform.Find("PlayerPhysics/Character/Marksman_Vest001").GetComponent<Renderer>();

            //Base Renderers
            characterRenderers[12] = transform.Find("PlayerPhysics/Character/Base_Arms_Left001").GetComponent<Renderer>();
            characterRenderers[13] = transform.Find("PlayerPhysics/Character/Base_Arms_Right001").GetComponent<Renderer>();
            characterRenderers[14] = transform.Find("PlayerPhysics/Character/Hands_TPV_Left001").GetComponent<Renderer>();
            characterRenderers[15] = transform.Find("PlayerPhysics/Character/Hands_TPV_Right001").GetComponent<Renderer>();
            characterRenderers[16] = transform.Find("PlayerPhysics/Character/Base_Jacket001").GetComponent<Renderer>();
            characterRenderers[17] = transform.Find("PlayerPhysics/Character/Base_Pants001").GetComponent<Renderer>();

            characterRenderers[18] = transform.Find("PlayerPhysics/Character/Forge Editor").GetComponent<Renderer>();

            int c = characterRenderers.Length;
            for(int i = 0; i < c; i++)
                characterRenderersGameObjects[i] = characterRenderers[i].gameObject;
        }

        public void SetCharacterRenderingMode(CharacterRenderingMode crm)
        {
            if (crm == CharacterRenderingMode.None)
            {
                int arrayLength = characterRenderers.Length;
                for (int i = 0; i < arrayLength; i++)
                    characterRenderers[i].enabled = false;

                playerCharacter.weaponSettings.weaponHolder.gameObject.SetActive(false);
            }
            else
            {
                currentCharacterRenderingMode = crm;

                int arrayLength = characterRenderers.Length;

                for (int i = 0; i < arrayLength; i++)
                    characterRenderers[i].enabled = false;

                int playerClassIndex = (int)clientPlayer.playerClass, playerClassStartIndex = playerClassIndex * 3, playerClassEndIndex = playerClassStartIndex + 3;
                for (int i = playerClassStartIndex; i < playerClassEndIndex; i++)
                    characterRenderers[i].enabled = true;

                for (int i = 12; i <= 15; i++)
                    characterRenderers[i].enabled = crm != CharacterRenderingMode.FirstPerson;
                for (int i = 16; i <= 17; i++)
                    characterRenderers[i].enabled = true;

                characterRenderers[18].enabled = clientPlayer.playerClass == PlayerClass.Editor;

                playerCharacter.weaponSettings.weaponHolder.gameObject.SetActive(crm == CharacterRenderingMode.ThirdPerson);
            }
        }

        bool currentSplitscreenRenderingStatus;
        /// <summary>
        /// When firstperson is true it means you are trying to render this character for first person.
        /// </summary>
        /// <param name="firstperson"></param>
        public void SetCharacterRenderingForSplitscreen(bool firstperson)
        {
            if (currentSplitscreenRenderingStatus == firstperson)
                return;
            currentSplitscreenRenderingStatus = firstperson;

            int arrayLength = characterRenderers.Length;
            for (int i = 0; i < arrayLength-1; i++)//-1 to exclude forge editor mesh
            {
                if (characterRenderers[i].enabled.Equals(true))
                {
                    if (currentCharacterRenderingMode == CharacterRenderingMode.FirstPerson)
                    {
                        if (i < 16)
                            characterRenderersGameObjects[i].layer = firstperson ? characterLayerDisabled : characterLayerEnabled;
                        else
                            characterRenderersGameObjects[i].layer = characterLayerEnabled;
                    }
                    else if (currentCharacterRenderingMode == CharacterRenderingMode.FirstPersonWithArms)
                    {
                        if (i < 12)
                            characterRenderersGameObjects[i].layer = firstperson ? characterLayerDisabled : characterLayerEnabled;
                        else
                            characterRenderersGameObjects[i].layer = characterLayerEnabled;
                    }
                    else
                    {
                        characterRenderersGameObjects[i].layer = firstperson ? characterLayerDisabled : characterLayerEnabled;
                    }
                }
            }

            characterRenderersGameObjects[18].layer = firstperson ? characterLayerDisabled : characterLayerEnabled;
        }

        public enum CharacterRenderingMode
        {
            FirstPerson,
            FirstPersonWithArms,
            ThirdPerson,
            None
        }

        #endregion

        #region Simple Data

        public Vector3 GetPosition()
        {
            return playerCharacter.transform.position;
        }

        public Vector3 GetPosition(bool applyOffset, bool returnHeadPosition)
        {
            if (applyOffset)
            {
                if (returnHeadPosition)
                    return playerCharacter.animationSettings.cameraFollow.position;
                else
                    return playerCharacter.transform.position + GetStanceCenter(playerCharacter.playerStance);
            }
            else return GetPosition();
        }

        #endregion

        #region Sound Controller

        public static float lastTimeSoundPlayVoice;

        void PlayVoice(VoiceType type)
        {
            if (type == VoiceType.EnemyDown)
            {
                if (lastTimeSoundPlayVoice < Time.time)
                {
                    lastTimeSoundPlayVoice = Time.time + 2.5f;
                    SoundManager.PlayAudioAtPoint(soundSettings.enemyDown, playerCharacter.transform.position, playerCharacter.transform, 0, 0, 1, 1, 5, 50);
                }
            }
            else if(type == VoiceType.DeathShout)
            {
                SoundManager.PlayAudioAtPoint(soundSettings.deathShouts[UnityEngine.Random.Range(0, soundSettings.deathShouts.Length-1)], playerCharacter.transform.position, playerCharacter.transform, 0, 0, 1, 1, 5, 25);
            }
        }

        enum VoiceType
        {
            EnemyDown, DeathShout
        }

        [System.Serializable]
        public class SoundSettings
        {
            public AudioClip enemyDown;
            public AudioClip[] deathShouts;
        }

        #endregion

        public void ResetParents()
        {
            playerPhysics.transform.parent = transform;
            playerCharacter.transform.parent = playerPhysics.transform;
            if (clientPlayer.isMe)
                playerController.transform.parent = transform;
        }

        public enum SetPlayerParent
        {
            None,
            PlayerManager,
            CurrentVehicleSeat
        }
    }
}