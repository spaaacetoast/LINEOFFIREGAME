using UnityEngine;
using System.Collections;

namespace AngryRain.Multiplayer.AnimatedSpawns
{
    public class DropPod : MultiplayerVehicle 
    {
        public float cooldownHeight = 150;

        private Rigidbody thisRigidbody;
        private Transform thisTransform;

        private float groundHeight;

        public bool hasCooldowned;

        public GameObject fireGameObject;
        public GameObject cooldownExplosion;
        public GameObject touchdownExplosion;
        public ParticleSystem smokeParticle;

        public Transform playerSpawnpoint;

        void Awake()
        {
            thisRigidbody = GetComponent<Rigidbody>();
            thisTransform = transform;
        }

        private new IEnumerator Start()
        {
            base.Start();

            while (TNManager.isHosting && GetComponent<LevelEditor.LevelObjectManager>().thisOwner == null)
                yield return null;
            if (TNManager.isHosting)
            {
                Local_RequestVehicleUpdate(true, GetComponent<LevelEditor.LevelObjectManager>().thisOwner, -1);
                GetComponent<LevelEditor.LevelObjectManager>().ServerSetOwner(LocalPlayerManager.localPlayers[0].clientPlayer.mPlayerID, false);
                Activate(GetComponent<LevelEditor.LevelObjectManager>().thisOwner);
            }
        }

        void Activate(ClientPlayer mPlayer)
        {
            RaycastHit hit;
            if (Physics.Raycast(thisRigidbody.position, Vector3.down, out hit))
            {
                groundHeight = hit.point.y;
                thisRigidbody.isKinematic = false;
                thisRigidbody.velocity = new Vector3(0, -10, 0);
            }
            else
            {
                Server_RequestVehicleUpdate(false, mPlayer.mPlayerID, -1);
                TNManager.Destroy(gameObject);
            }
        }

        void FixedUpdate()
        {
            if (!hasCooldowned && thisRigidbody.position.y < groundHeight + cooldownHeight)
            {
                hasCooldowned = true;
                cooldownExplosion.SetActive(true);
                fireGameObject.SetActive(false);
            }
        }

        private new void OnCollisionEnter(Collision col)
        {
            base.OnCollisionEnter(col);
            if (TNManager.isHosting)
            {
                thisRigidbody.isKinematic = true;
                ParticleSystem.EmissionModule em = smokeParticle.emission;
                em.enabled = true;
                //thisRigidbody.rotation = Quaternion.LookRotation(col.contacts[0].normal);
                thisRigidbody.rotation = Quaternion.identity;
                touchdownExplosion.SetActive(true);

                if (vehicleSeats[0].clientPlayer != null)
                    Client_UpdateSeat(false, 0, vehicleSeats[0].clientPlayer.mPlayerID);

                TNManager.Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (vehicleSeats[0].clientPlayer != null)
            {

            }
        }
    }
}