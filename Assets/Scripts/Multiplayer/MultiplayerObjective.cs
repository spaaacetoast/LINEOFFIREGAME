using UnityEngine;
using System.Collections;
using TNet;
using AngryRain;
using AngryRain.Multiplayer;

public class MultiplayerObjective : TNBehaviour 
{
    public string objectiveName;
    public ObjectiveType objectiveType;

    public ClientPlayer targetMPlayer;

    public bool isDestroyed;
    public float progress;
    public float captureRate = 0.01f;
    public float timeInSecForDetonation = 30;

    float captureTime;

    public GameObject activatedEffect;
    public ParticleEffect destroyedEffect;

    /*void OnTriggerEnter(Collider col)
    {
        if (TNManager.isHosting)
        {
            if (objectiveType == ObjectiveType.PlantAndDestroy)
            {
                if (targetMPlayer == null)
                {
                    Rigidbody target = col.attachedRigidbody;
                    if (target != null)
                    {
                        PlayerManager pManager = target.transform.root.GetComponent<PlayerManager>();
                        if (pManager != null)
                        {
                            if (targetMPlayer == null && pManager.lastInputSync.actionButton)
                            {
                                targetMPlayer = pManager.mPlayer;

                            }
                        }
                    }
                }
            }
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (TNManager.isHosting)
        {
            if (objectiveType == ObjectiveType.PlantAndDestroy)
            {
                if (targetMPlayer != null)
                {
                    Rigidbody target = col.attachedRigidbody;
                    if (target != null)
                    {
                        PlayerManager pManager = target.transform.root.GetComponent<PlayerManager>();
                        if(pManager != null && targetMPlayer == pManager.mPlayer)
                        {
                            targetMPlayer = null;
                        }
                    }
                }
            }
        }
    }*/

    float timePressed;

    void Start()
    {
        tno.rebuildMethodList = true;
    }

    public void Local_RequestActivation(ClientPlayer mPlayer)
    {
        tno.Send("Server_RequestActivation", Target.Host, mPlayer.mPlayerID);
    }

    [RFC]
    public void Server_RequestActivation(int mPlayerID)
    {
        ClientPlayer mPlayer = MultiplayerManager.GetPlayer(mPlayerID);

        if (targetMPlayer != null && !targetMPlayer.playerManager.lastInputSync.actionButton)
            tno.Send("SetObjectivePlayer", Target.All, -1);

        if(targetMPlayer == null && mPlayer.isConnected)
        {
            tno.Send("SetObjectivePlayer", Target.All, mPlayer.mPlayerID);
            tno.Send("SetObjectiveState", Target.All, 0);
        }
    }

    void Update()
    {
        if (TNManager.isHosting)
        {
            if (targetMPlayer != null)
            {
                if (targetMPlayer.playerManager.lastInputSync.actionButton)
                {
                    progress += captureRate;
                    if (progress >= 1)
                    {
                        progress = 1;
                        tno.Send("SetObjectivePlayer", Target.All, -1);
                        captureTime = Time.time;
                    }
                    tno.Send("SetObjectiveState", Target.All, progress);
                }
                else
                {
                    tno.Send("SetObjectivePlayer", Target.All, -1);
                    tno.Send("SetObjectiveState", Target.All, 0);
                }
            }

            if (!isDestroyed && progress == 1 && Time.time > captureTime + timeInSecForDetonation)
            {
                tno.Send("DestroyObjective", Target.All);
            }
        }
    }

    [RFC]
    public void SetObjectivePlayer(int mPlayerID)
    {
        if (mPlayerID >= 0)
        {
            ClientPlayer mPlayer = MultiplayerManager.GetPlayer(mPlayerID);
            mPlayer.playerManager.currentObjective = this;
            targetMPlayer = mPlayer;
        }
        else
        {
            targetMPlayer.playerManager.currentObjective = null;
            targetMPlayer = null;
        }
    }

    [RFC]
    public void SetObjectiveState(float progress)
    {
        this.progress = progress;

        activatedEffect.SetActive(progress == 1 && !isDestroyed);
    }

    [RFC]
    public void DestroyObjective()
    {
        isDestroyed = true;
        PoolManager.CreateParticle(destroyedEffect, transform.position, transform.rotation);
        activatedEffect.SetActive(false);

        //PlayerCamera.allPlayerCameras[0].StartCameraShake(transform.position, 75, 50, 25);

        //MultiplayerManager.ExplosionDamage(transform.position, 100, 10, 2f);
    }
}

public enum ObjectiveType
{
    /*Capture,
    Takeable,
    Destroyable,*/
    PlantAndDestroy
}