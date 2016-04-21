using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TNet;
using System;
using AngryRain.Multiplayer;
using AngryRain.Multiplayer.LevelEditor;

namespace AngryRain
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(LevelObjectManager))]
    [RequireComponent(typeof(SyncNetworkObject))]
    public class MultiplayerVehicle : TNBehaviour
    {
        private LevelObjectManager lom;

        public string vehicleName;
        public VehicleDestructionSettings destructionSettings;
        public VehicleSeat[] vehicleSeats;

        public float velocityMagnitude { get; set; }
        public Vector3 relativeVelocity { get; set; }
        public Vector3 relativeAngularVelocity { get; set; }

        public float vehicleHealth = 1000;
        public bool vehicleIsAlive = true;

        public void Start()
        {
            for (int i = 0; i < vehicleSeats.Length; i++)
                vehicleSeats[i].seatIndex = i;
            tno.rebuildMethodList = true;

            lom = GetComponent<LevelObjectManager>();
        }

        public void Update ()
        {
            int c = vehicleSeats.Length;
            for (int i = 0; i < c; i++)
            {
                VehicleSeat seat = vehicleSeats[i];
                if (seat.clientPlayer != null)
                {
                    if (seat.clientPlayer.isMe)
                    {
                        if (Input.GetKeyDown(KeyCode.L))
                            tno.Send(150, Target.Host, seat.clientPlayer.mPlayerID, -2);
                    }
                }
            }
        }

        public void Local_RequestVehicleUpdate(bool enterVehicle, ClientPlayer mPlayer, int nextSeat)
        {
            tno.Send(100, Target.Host, enterVehicle, mPlayer.mPlayerID, nextSeat);
        }

        [RFC(100)]
        public void Server_RequestVehicleUpdate(bool enterVehicle, int mPlayerID, int nextSeat)
        {
            try
            {
                if (vehicleIsAlive)
                {
                    if (nextSeat < 0)//Leave or entering this vehicle
                    {
                        ClientPlayer mPlayer = Multiplayer.MultiplayerManager.GetPlayer(mPlayerID);
                        VehicleSeat vSeat = enterVehicle ? GetNextAvailableSeat() : GetPlayerInSeat(mPlayerID);

                        if (vSeat != null)
                        {
                            tno.Send("Client_UpdateSeat", Target.All, enterVehicle, vSeat.seatIndex, mPlayerID);

                            if (vSeat.shouldTakeOwnership)
                                tno.ownerID = enterVehicle ? mPlayer.tPlayer.id : TNManager.player.id;
                        }
                        else
                        {
                            Debug.LogError("Request for update returned empty!");
                            mPlayer.playerManager.CheckVehicleData();
                        }
                    }
                    else//Switching seats
                    {
                        ClientPlayer mPlayer = Multiplayer.MultiplayerManager.GetPlayer(mPlayerID);
                        VehicleSeat oSeat = GetPlayerInSeat(mPlayerID);
                        VehicleSeat nSeat = vehicleSeats[nextSeat];
                        if (mPlayer != null && oSeat != null && nSeat != null && nSeat.clientPlayer == null)
                        {
                            tno.Send("Client_UpdateSeatSwitch", Target.All, oSeat.seatIndex, nextSeat, mPlayerID);
                        }
                    }
                }
                else if (nextSeat >= 0)
                {
                    //KILL ALL PLAYERS, BOEM BIETCHAS
                    for (int i = 0; i < vehicleSeats.Length; i++)
                    {
                        if (vehicleSeats[i].clientPlayer != null && vehicleSeats[i].clientPlayer.isConnected)
                            vehicleSeats[i].clientPlayer.playerManager.Server_ReceiveDamage(-1, -2, -1);
                    }
                }
                else//if player died
                {
                    ClientPlayer mPlayer = Multiplayer.MultiplayerManager.GetPlayer(mPlayerID);
                    VehicleSeat vSeat = enterVehicle ? GetNextAvailableSeat() : GetPlayerInSeat(mPlayerID);

                    if (vSeat != null)
                    {
                        tno.Send("Client_UpdateSeat", Target.All, false, vSeat.seatIndex, mPlayerID);

                        vSeat.clientPlayer = null;
                    }

                    tno.Send("Client_UpdateSeat", Target.Others, false, vSeat == null ? -1 : vSeat.seatIndex, mPlayerID);

                    mPlayer.vehicle = null;
                    mPlayer.vehicleSeat = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        [RFC]
        public void Client_UpdateSeat(bool enterVehicle, int seat, int clientPlayerID)
        {
            try
            {
                ClientPlayer clientPlayer = Multiplayer.MultiplayerManager.GetPlayer(clientPlayerID);
                VehicleSeat vSeat = vehicleSeats[seat];
                if (enterVehicle)//Entering vehicle
                {
                    if (clientPlayer.vehicleSeat != null && clientPlayer.vehicleSeat.clientPlayer == clientPlayer)
                        clientPlayer.vehicleSeat.clientPlayer = null;

                    vSeat.clientPlayer = clientPlayer;
                    clientPlayer.vehicle = this;
                    clientPlayer.vehicleSeat = vSeat;

                    clientPlayer.playerManager.playerPhysics.transform.SetParent(vSeat.seatCharacter, false);
                    if (clientPlayer.isMe)
                    {
                        clientPlayer.playerManager.playerController.transform.SetParent(vSeat.seatPlayer, false);
                        clientPlayer.playerManager.playerController.transform.position = vSeat.seatPlayer.position;
                        clientPlayer.playerManager.playerController.transform.rotation = vSeat.seatPlayer.rotation;
                        clientPlayer.playerManager.playerController.playerCamera.SetRotation(Vector3.zero);
                        clientPlayer.playerManager.playerController.playerMovement.enabled = false;
                        clientPlayer.playerManager.playerController.playerMovement.rigidbody.detectCollisions = false;
                        clientPlayer.playerManager.playerController.playerMovement.rigidbody.isKinematic = true;
                        clientPlayer.playerManager.playerController.playerMovement.rigidbody.interpolation = RigidbodyInterpolation.None;
                    }

                    clientPlayer.playerManager.playerCharacter.animator.Play(vSeat.characterStance);

                    SendMessage("VehiclePlayerEntering", vSeat, SendMessageOptions.DontRequireReceiver);
                }
                else//Leaving vehicle
                {
                    if (clientPlayer.isAlive)
                    {
                        clientPlayer.playerManager.playerCharacter.SetStance(0);
                        clientPlayer.playerManager.ResetParents();
                        if (TNManager.isHosting)
                            clientPlayer.playerManager.playerPhysics.transform.position = vSeat.seatExitPoint.position;
                    } 

                    if(TNManager.isHosting)
                        clientPlayer.playerManager.SyncTransform(false);

                    if (clientPlayer.isMe)
                    {
                        clientPlayer.playerManager.playerController.playerMovement.enabled = true;
                        clientPlayer.playerManager.playerController.playerMovement.rigidbody.detectCollisions = true;
                        clientPlayer.playerManager.playerController.playerMovement.rigidbody.isKinematic = false;
                        clientPlayer.playerManager.playerController.playerMovement.rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    }

                    vSeat.clientPlayer = null;
                    clientPlayer.vehicle = null;
                    clientPlayer.vehicleSeat = null;

                    SendMessage("VehiclePlayerLeaving", vSeat, SendMessageOptions.DontRequireReceiver);
                    //VehiclePlayerLeaving(vSeat);
                }

                if(seat < 0 && !enterVehicle)
                {
                    clientPlayer.vehicle = null;
                    clientPlayer.vehicleSeat = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }

        [RFC(103)]
        public void Server_RequestVehicleReset(int mPlayerID)
        {
            VehicleSeat seat = GetPlayerInSeat(mPlayerID);
            if(seat != null)
            {
                tno.Send("Client_UpdateSeat", Target.All, false, seat.seatIndex, mPlayerID);
            }
        }

        #region Save player on destroy

        void OnDestroy()
        {
            for (int i = 0; i < vehicleSeats.Length; i++)
            {
                if (vehicleSeats[i].clientPlayer != null)
                {
                    vehicleSeats[i].clientPlayer.vehicle = null;
                    vehicleSeats[i].clientPlayer.vehicleSeat = null;

                }
            }
        }

        #endregion

        #region Vehicle Health and Damage

        public void Local_ReceiveDamage(DamageGiver dGiver, DamageReceiver dReceiver)
        {
            tno.Send("Server_ReceiveDamage", Target.Host, dGiver.mPlayer.mPlayerID, dGiver.mWeapon);
        }

        [RFC]
        protected void Server_ReceiveDamage ( int playerID, int weaponID )
        {
            try
            {
                if (vehicleIsAlive)
                {
                    float damage;

                    if (weaponID == -2)//Suicide
                        damage = 10000;
                    else
                    {
                        ServerWeaponInfo wInfo = CustomizationManager.GetServerWeapon(weaponID);
                        damage = wInfo != null ? wInfo.weaponDamage.GetDamage(0) : 0;
                    }

                    vehicleHealth -= damage;

                    ServerCheckHealth(playerID, weaponID);
                }
            }
            catch ( Exception ex )
            {
                Debug.LogError ( ex );
            }
        }

        public void Server_ReceiveCollisionDamage(int playerID, int damage, int weaponID)
        {
            vehicleHealth -= damage;
            ServerCheckHealth(playerID, weaponID);
        }

        void ServerCheckHealth(int playerID, int weaponID)
        {
            if (vehicleHealth <= 0)
            {
                vehicleHealth = 0;
                vehicleIsAlive = false;

                //KILL ALL PLAYERS, BOEM BIETCHAS
                for (int i = 0; i < vehicleSeats.Length; i++)
                {
                    if (vehicleSeats[i].clientPlayer != null && vehicleSeats[i].clientPlayer.isConnected)
                        vehicleSeats[i].clientPlayer.playerManager.Server_ReceiveDamageVelocity(playerID, 100, weaponID, lom.rigidbody.velocity);

                    vehicleSeats[i].clientPlayer = null;
                }

                //DESTROY THIS VEHICLE, SPARTAAAAAAAAA MOTHERFUCKAAAAAAA
                tno.Send(151, Target.All, true, vehicleHealth);

                StartCoroutine(Server_StartRespawnTimer());
            }
            else
            {
                tno.Send("Client_ReceiveDamage", Target.Others, false, vehicleHealth);
                Client_ReceiveDamage(false, vehicleHealth);
            }
        }

        [RFC]
        protected void Client_ReceiveDamage(bool shouldDestroy, float currentHealth)
        {
            vehicleHealth = currentHealth;

            if (!shouldDestroy) return;

            PoolManager.CreateParticle(destructionSettings.explosion, transform.position, transform.rotation);
            foreach (GameObject t in destructionSettings.destructionEffects)
            {
                t.SetActive(true);
                ParticleSystem.EmissionModule em = t.GetComponent<ParticleSystem>().emission;
                em.enabled = true;
            }

            foreach (VehicleSeat t in vehicleSeats)
                t.clientPlayer = null;
        }

        [RFC]
        protected void Client_RespawnVehicle()
        {
            vehicleIsAlive = true;
            vehicleHealth = 1000;

            foreach (GameObject t in destructionSettings.destructionEffects)
            {
                ParticleSystem.EmissionModule em = t.GetComponent<ParticleSystem>().emission;
                em.enabled = false;
            }

            transform.position = lom.startPosition;
            transform.rotation = lom.startRotation;

            lom.rigidbody.velocity = Vector3.zero;
            lom.rigidbody.angularVelocity = Vector3.zero;
        }

        IEnumerator Server_StartRespawnTimer()
        {
            yield return new WaitForSeconds(lom.respawnTime);
            tno.Send("Client_RespawnVehicle", Target.All);
        }

        #endregion

        #region VehicleCollision

        protected void OnCollisionEnter(Collision col)
        {
            if (TNManager.isHosting)
            {
                DamageReceiver dr = col.collider.GetComponent<DamageReceiver>();
                if (dr != null)
                {
                    print((int)col.relativeVelocity.magnitude * 10);
                    if (dr.targetObject == TargetObject.Vehicle && dr.mVehicle != null)
                    {
                        dr.mVehicle.Server_ReceiveCollisionDamage(vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isConnected ? vehicleSeats[0].clientPlayer.mPlayerID : -1, (int)col.relativeVelocity.magnitude * 10, 1);
                    }
                    else if (dr.targetObject == TargetObject.PlayerManager && dr.pManager != null)
                    {
                        dr.pManager.Server_ReceiveDamageVelocity(vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isConnected ? vehicleSeats[0].clientPlayer.mPlayerID : -1, (int)col.relativeVelocity.magnitude * 10, -1, col.relativeVelocity + lom.rigidbody.velocity);
                    }
                }

                PlayerController pc = col.collider.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.playerManager.Server_ReceiveDamageVelocity(vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isConnected ? vehicleSeats[0].clientPlayer.mPlayerID : -1, (int)col.relativeVelocity.magnitude * 10, -1, col.relativeVelocity + lom.rigidbody.velocity);
                }
            }
        }

        #endregion

        VehicleSeat GetNextAvailableSeat()
        {
            for (int i = 0; i < vehicleSeats.Length; i++)
            {
                if (vehicleSeats[i].clientPlayer == null || vehicleSeats[i].clientPlayer != null && !vehicleSeats[i].clientPlayer.isConnected)
                    return vehicleSeats[i];
            }
            return null;
        }

        public VehicleSeat GetPlayerInSeat(int mPlayerID)
        {
            for (int i = 0; i < vehicleSeats.Length; i++)
            {
                if (vehicleSeats[i].clientPlayer != null && vehicleSeats[i].clientPlayer.mPlayerID == mPlayerID)
                    return vehicleSeats[i];
            }
            return null;
        }

        [System.Serializable]
        public class VehicleSeat
        {
            public string seatName, characterStance;
            public bool shouldTakeOwnership;
            public Transform seatPlayer, seatCharacter, seatExitPoint;
            public Vector3 cameraPosition;
            public Vector2 minimumRotation, maximumRotation;
            public ClientPlayer clientPlayer;

            //Public, Not Visible
            public int seatIndex { get; set; }
        }

        [System.Serializable]
        public class VehicleDestructionSettings
        {
            public ParticleEffect explosion;
            public GameObject[] destructionEffects;
        }
    }
}