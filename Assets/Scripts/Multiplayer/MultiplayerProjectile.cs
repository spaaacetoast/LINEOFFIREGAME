using UnityEngine;
using System.Collections;
using AngryRain.Multiplayer;

namespace AngryRain
{
    public class MultiplayerProjectile : MonoBehaviour
    {
        //Public Variables
        public int projectileID;
        public ParticleEffect[] impactEffect;
        public bool isAvailable = true;
        public Vector3 projectileSpeed;
        public float extraTimeTillAvailable = 0.5f;

        public GameObject[] projectileObjects; 
        public GameObject[] projectileObjectsTimed;

        //Public Varaibles, Not visible
        public new Transform transform { private set; get; }
        public Multiplayer.DamageGiver damageGiver { private set; get; }

        //Private Variables
        public LayerMask layerMask;
        private Vector3 position;

        void Awake()
        {
            transform = GetComponent<Transform>();
        }

        public void InitializeProjectile(Vector3 position, Quaternion rotation)
        {
            this.position = position;

            transform.position = position;
            transform.rotation = rotation;
        }

        public void StartProjectile(Multiplayer.DamageGiver dGiver, bool enableChildObjects = true, bool enableTimedChildObjects = true)
        {
            damageGiver = dGiver;
            StartCoroutine(HandleProjectile());
            StartCoroutine(EnableTimedObjects(enableChildObjects, enableTimedChildObjects));
        }

        IEnumerator EnableTimedObjects(bool enableChildObjects, bool enableTimedChildObjects)
        {
            //Enable all child objects, like bullet mesh

            yield return new WaitForSeconds(0.2f);

            if (enableTimedChildObjects)
                for (int i = 0; i < projectileObjectsTimed.Length; i++)
                    projectileObjectsTimed[i].SetActive(true);

            if (enableChildObjects)
                for (int i = 0; i < projectileObjects.Length; i++)
                    projectileObjects[i].SetActive(true);
        }

        IEnumerator HandleProjectile()
        {
            isAvailable = false;
            
            float deltaTime = Time.fixedDeltaTime;//first loop round should be fixed so distance is more then a few

            //Loop logic for movement, velocity and hit registration
            Vector3 lastPosition = position;
            while(true)
            {
                Vector3 forward = transform.rotation.eulerAngles.normalized;
                position += transform.rotation * (projectileSpeed * deltaTime);
                transform.position = position;

                RaycastHit hit;
                if (Physics.Linecast(lastPosition, position, out hit, layerMask))
                {
                    //Give the damage info to the DamageReceiver
                    DamageReceiver dr = hit.collider.GetComponent<DamageReceiver>();
                    if (dr != null)
                        dr.GetDamage(damageGiver);

                    //Create hit particle effect
                    CreateParticle(hit.point, Multiplayer.DamageReceiver.SurfaceType.Dirt, hit);
                    break;
                }

                yield return new WaitForFixedUpdate();
                lastPosition = position;
                deltaTime = Time.deltaTime;
            }

            yield return new WaitForEndOfFrame();//Wait one frame so game doesn't crash

            //Disable all child objects, like bullet mesh
            for (int i = 0; i < projectileObjects.Length; i++)
                projectileObjects[i].SetActive(false);

            yield return new WaitForSeconds(extraTimeTillAvailable);

            //Timed disable all child objects, for trailing effects
            for (int i = 0; i < projectileObjectsTimed.Length; i++)
                projectileObjectsTimed[i].SetActive(false);

            isAvailable = true;
        }

        /*IEnumerator HandleProjectile()
        {
            isFinished = false;
            bool showRenderer = UnityEngine.Random.Range(0, 100) < showRendererChance;
            bool areRenderersEnabledFirstFrame = false;

            float startTime = Time.time;
            float sec = startTime + 0.1f;

            particleSettings.PrepareParticles(true);
            particleSettings.SetParticles(true);

            GetComponent<PigeonCoopToolkit.Effects.Trails.Trail>().Emit = true;

            while (!isFinished)
            {
                thisForward = thisRotation * Vector3.forward;
                thisEndPosition = (thisForward * projectileSpeed.z) + thisPosition;

                if (showRenderer && areRenderersEnabledFirstFrame)
                {
                    if (lineRenderer)
                        lineRenderer.enabled = true;
                    if (meshRenderer)
                        meshRenderer.enabled = true;

                    showRenderer = false;
                }

                RaycastHit rayHit;
                if (Physics.Raycast(thisPosition, thisForward, out rayHit, projectileSpeed.z, layerMask))
                {
                    isFinished = true;
                    thisEndPosition = rayHit.point;
                    lastColliderHit = rayHit.collider;
                    if (damageGiver != null && lastColliderHit)
                    {
                        if (typeOfProjectile == FiringType.Bullet)
                        {
                            AngryRain.Multiplayer.DamageReceiver dReceiver = lastColliderHit.GetComponent<AngryRain.Multiplayer.DamageReceiver>();
                            if (dReceiver != null)
                            {
                                damageGiver.damageReceiver = dReceiver;
                                dReceiver.GetDamage(damageGiver);
                            }
                        }
                        else
                        {
                            //Multiplayer.MultiplayerManager.ExplosionDamage(thisEndPosition, (int)CustomizationManager.instance.GetWeaponDamage(damageGiver.mWeapon), maximumRangeExplosionDamage, minimumRangeExplosionDamage);
                        }

                        particleSettings.SetParticles(false);
                    }

                    AngryRain.Multiplayer.DamageReceiver dmgReceiver = lastColliderHit.GetComponent<AngryRain.Multiplayer.DamageReceiver>();
                    if (dmgReceiver != null)
                    {
                        CreateParticle(thisEndPosition, dmgReceiver.surfaceType, rayHit);
                    }
                    else
                    {
                        CreateParticle(thisEndPosition, Multiplayer.DamageReceiver.SurfaceType.Dirt, rayHit);
                    }
                    GetComponent<PigeonCoopToolkit.Effects.Trails.Trail>().Emit = false;
                    thisPosition = thisEndPosition;
                }

                transform.localPosition = thisPosition;
                thisPosition = thisEndPosition;

                yield return new WaitForFixedUpdate();

                if (!areRenderersEnabledFirstFrame)
                    particleSettings.PrepareParticles(false);

                areRenderersEnabledFirstFrame = true;
            }
        }

        bool DoBulletCheck()
        {
            return false;
        }

        //Old and original method
        /*IEnumerator HandleProjectile()
        {
            isFinished = false;

            if (meshRenderer)
                meshRenderer.enabled = false;
            lineRenderer.enabled = false;

            float sec = Time.time + 0.1f;
            while (!isFinished)
            {
                thisForward = thisRotation * Vector3.forward;
                thisEndPosition = (thisForward * projectileSpeed.z) + thisPosition;

                lineRenderer.SetPosition(0, thisPosition);
                lineRenderer.SetPosition(1, thisEndPosition);

                RaycastHit rayHit;
                if (Physics.Raycast(thisPosition, thisForward, out rayHit, projectileSpeed.z, layerMask))
                {
                    isFinished = true;
                    thisEndPosition = rayHit.point;
                    lastColliderHit = rayHit.collider;
                    if (lastColliderHit)
                    {
                        AngryRain.Multiplayer.DamageReceiver dReceiver = lastColliderHit.GetComponent<AngryRain.Multiplayer.DamageReceiver>();
                        if (dReceiver != null)
                        {
                            damageGiver.damageReceiver = dReceiver;
                            dReceiver.GetDamage(damageGiver);
                        }
                        //lastColliderHit.SendMessage("ReceiveDamage", this, SendMessageOptions.DontRequireReceiver);
                    }

                    CreateParticle(thisEndPosition, rayHit.normal);

                    yield return null;
                    lineRenderer.enabled = false;
                    if (meshRenderer)
                        meshRenderer.enabled = false;

                    thisPosition = thisEndPosition;
                }
                else if (Time.time > sec && meshRenderer)
                {
                    transform.localPosition = thisPosition;
                    yield return new WaitForFixedUpdate();
                    meshRenderer.enabled = true;
                }

                thisPosition = thisEndPosition;
                yield return new WaitForFixedUpdate();

                if (!isFinished)
                    lineRenderer.enabled = true;
            }
        }*/

        /*WaitForSeconds PrintTime(string time)
        {
            print(time);
            return new WaitForSeconds(1);
        }*/

        void CreateParticle(Vector3 pos, AngryRain.Multiplayer.DamageReceiver.SurfaceType surface, RaycastHit hitObject)
        {
            switch (surface)
            {
                case Multiplayer.DamageReceiver.SurfaceType.Dirt:
                        PoolManager.CreateParticle(impactEffect[0], pos, Quaternion.LookRotation(hitObject.normal));
                        break;
                case Multiplayer.DamageReceiver.SurfaceType.Stone:
                        PoolManager.CreateParticle(impactEffect[1], pos, Quaternion.LookRotation(hitObject.normal));
                    //bullethole inst+parent
                        break;
                case Multiplayer.DamageReceiver.SurfaceType.Metal:
                        PoolManager.CreateParticle(impactEffect[2], pos, Quaternion.LookRotation(hitObject.normal));
                        //bullethole inst+parent
                        break;
                case Multiplayer.DamageReceiver.SurfaceType.Wood:
                        PoolManager.CreateParticle(impactEffect[3], pos, Quaternion.LookRotation(hitObject.normal));
                        //bullethole inst+parent
                        break;
                case Multiplayer.DamageReceiver.SurfaceType.Water:
                        PoolManager.CreateParticle(impactEffect[4], pos, Quaternion.LookRotation(hitObject.normal));
                        break;
            }
        }
    }
}