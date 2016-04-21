using UnityEngine;
using System.Collections;
using TNet;
using System;

namespace AngryRain.Multiplayer
{
    public class AIUnit : TNBehaviour
    {
        public List<DamageReceiver> allDamageReceivers = new List<DamageReceiver>();

        public new Rigidbody rigidbody { private set; get; }
        public NavMeshAgent agent { private set; get; }
        public PlayerCharacterController characterController { private set; get; }
        public ObjectHolder objectHolder = new ObjectHolder();
        public int serverWeaponID = 1;

        public MultiplayerManager multiplayerManager;


        private float nextCall = 0.0f;

        public PlayerManager targetManager;

        public bool dead = false;

        public float health = 100.0f;

        public float fireRate = 1.0f;
        private float nextFire = 0.0f;

        public Transform weaponHolder;

        public ParticleSystem tempMuzzleFlash;

        public Transform topBody;

        public float aimDamping = 2.0f;

        public float shootDistance = 20.0f;

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            characterController = GetComponentInChildren<PlayerCharacterController>();
            allDamageReceivers.buffer = GetComponentsInChildren<DamageReceiver>();
        }

        private void Start()
        {
            characterController.isGrounded = true;
            Local_SetRagdoll(false, null);
            rigidbody.isKinematic = true;
            agent.SetDestination(Vector3.zero);
            if (!tno.isMine)
                agent.enabled = false;
        }

        void FixedUpdate()
        {
            if (!tno.isMine) return;

            if (Time.time >= nextCall)
            {
                int nIndex = getClosestPlayer();
                if (nIndex != -1)
                {
                    targetManager = (PlayerManager)multiplayerManager.allInstantiatedPlayerManagers[nIndex];
                    //agent.SetDestination (targetManager.playerController.transform.position);
                    agent.destination = targetManager.playerController.transform.position;
                    tno.Send("Client_SetTM", Target.Others, nIndex);

                }
                else {
                    targetManager = null;
                }

                nextCall = Time.time + 1.0f;
            }
            characterController.animationSettings.velocity = agent.velocity;
            if (dead != true) return;
            Die();
            dead = false;
        }

        void LateUpdate()
        {
            if (dead || !targetManager || agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance > shootDistance) return;

            if (Time.time > nextFire && tno.isMine)
            {
                nextFire = Time.time + fireRate;
                MultiplayerProjectile proj = PoolManager.CreateProjectile(objectHolder.projectileBullet,
                    weaponHolder.position, weaponHolder.rotation);

                //Fire Projectile
                proj.StartProjectile(new Multiplayer.DamageGiver(null, serverWeaponID));
                tempMuzzleFlash.Play();
            }

            Vector3 lookPos = (targetManager.playerController.transform.position - transform.position);
            Quaternion lookRot = Quaternion.LookRotation(lookPos);
            lookRot *= Quaternion.Euler(0, -1.0f, 0);
            lookRot.x = 0;
            lookRot.z = 0;
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * aimDamping);
            Vector3 lookPos2 = (targetManager.playerController.transform.position - weaponHolder.position);
            Quaternion lookRot2 = Quaternion.LookRotation(lookPos2);
            characterController.animationSettings.lookRotation = new Vector2(lookRot2.eulerAngles.x - 6, 0);
        }

        [RFC]
        public void Client_SetTM(int nTM)
        {
            if (nTM == -1)
            {
                targetManager = null;
            }
            else {
                targetManager = (PlayerManager)multiplayerManager.allInstantiatedPlayerManagers[nTM];
            }
        }

        [RFC]
        public void Local_SetRagdoll(bool enable, Vector3[] velocity) { tno.Send("Client_SetRagdoll", Target.All, enable, velocity); }

        [RFC]
        public void Client_SetRagdoll(bool enable, Vector3[] velocity)
        {
            if (enable == true)
            {
                agent.Stop();
                transform.GetChild(0).GetComponent<Animator>().enabled = false;
                GetComponent<CapsuleCollider>().enabled = false;
                GetComponent<NavMeshAgent>().enabled = false;
                rigidbody.useGravity = true;
                this.enabled = false;
            }
            Debug.Log(allDamageReceivers.Count);
            int c = allDamageReceivers.Count;
            for (int i = 0; i < c; i++)
            {
                if (allDamageReceivers[i].GetComponent<Rigidbody>() != rigidbody)
                {
                    DamageReceiver dReceiver = allDamageReceivers[i];

                    dReceiver.GetComponent<Rigidbody>().isKinematic = !enable;
                    dReceiver.GetComponent<Rigidbody>().detectCollisions = enable;
                    if (enable && velocity != null && velocity.Length == allDamageReceivers.Count)
                        dReceiver.GetComponent<Rigidbody>().velocity = velocity[i];
                }
            }

            rigidbody.isKinematic = enable;
        }

        public void Die()
        {
            Vector3[] allVelocity = new Vector3[allDamageReceivers.Count];
            for (int i = 0; i < allVelocity.Length; i++)
                allVelocity[i] = allDamageReceivers[i].GetComponent<Rigidbody>().velocity;
            Local_SetRagdoll(true, allVelocity);
        }


        //Get the Index of the Closest Player from the MultiplayerManager
        private int getClosestPlayer()
        {
            float distance = Mathf.Infinity;
            int index = -1;
            for (int i = 0; i < multiplayerManager.allInstantiatedPlayerManagers.Length; i++)
            {
                if (multiplayerManager.allInstantiatedPlayerManagers[i].playerController != null && multiplayerManager.allInstantiatedPlayerManagers[i].playerController.gameObject.activeSelf == true)
                {
                    float dist = Vector3.Distance(multiplayerManager.allInstantiatedPlayerManagers[i].playerController.transform.position, this.transform.position);
                    if (dist < distance)
                    {
                        index = i;
                        distance = dist;
                    }
                }
            }
            return index;
        }

        public void Local_ReceiveDamage(DamageGiver dGiver, DamageReceiver dReceiver)
        {
            if (dGiver != null)
                tno.SendQuickly(15, Target.Host, dGiver.mWeapon, dReceiver.thisIndex);
        }

        [RFC(15)]
        public void Server_ReceiveDamage(int weaponID, int dReceiverIndex)
        {
            try
            {
                if (health > 0.0f)
                {
                    float damage;
                    if (weaponID == -2)//Suicide
                        damage = 100;
                    else
                    {
                        ServerWeaponInfo wInfo = CustomizationManager.GetServerWeapon(weaponID);
                        damage = wInfo != null ? wInfo.weaponDamage.GetDamage(0) : 0;
                    }

                    health -= damage;
                    if (health <= 0.0f)
                    {
                        Die();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.StackTrace);
            }
        }

        [System.Serializable]
        public class ObjectHolder
        {
            public Transform cameraOfffsetTransform;
            public Transform weaponRotOffsetTransform;
            public Transform weaponOffsetTransform;
            public Transform weaponPivotTransform;

            public AudioSource[] weaponAudioSource; //0=fire 1=reload

            public MultiplayerProjectile projectileBullet;
            public MultiplayerProjectile projectileRocket;
        }
    }


}