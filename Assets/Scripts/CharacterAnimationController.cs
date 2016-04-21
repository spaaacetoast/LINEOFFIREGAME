using UnityEngine;
using System.Collections;

namespace AngryRain
{
    public class CharacterAnimationController : MonoBehaviour
    {
        #region Public Variables

        public Animator thisAnimator;
        public Transform thisTransform;
        public Renderer[] thisRenderers;

        public Transform cameraHelper;
        public Transform characterHead;

        [HideInInspector]
        public Vector3 velocity;
        [HideInInspector]
        public Vector3 relativeVelocity;
        [HideInInspector]
        public Vector3 relativeAngularVelocity;
        private float velocityMagnitude;

        public bool selfCalcVelocity;

        private int hashSpeed;
        private int hashDirection;

        public Transform leftHandIK;
        public Transform rightHandIK;

        public Transform weaponHolder;
        public Transform rightShoulder;

        public LayerMask feetLayers;

        public Quaternion targetViewRotation = Quaternion.identity;
        public bool applyRotation;

        private Vector3 lastAngVel;

        public PlayerStance playerStance;
        public bool enableLookAround = true;

        public Renderer[] renderersAffectedByTeam;
        public Texture2D[] teamTextures;

        #endregion

        #region Monobehaviours

        void Awake()
        {
            thisAnimator = GetComponent<Animator>();
            thisTransform = transform;
            thisRenderers = GetComponentsInChildren<Renderer>();
        }

        void Start()
        {
            thisTransform = transform;

            hashSpeed = Animator.StringToHash("Speed");
            hashDirection = Animator.StringToHash("Direction");

            foreach (PlayerWeaponInfo p in weaponSettings.allWeapons)
                p.InitializeWeapon();
        }

        void Update()
        {
            if (applyRotation)
            {
                thisTransform.localRotation = Quaternion.Lerp(thisTransform.localRotation, Quaternion.Euler(0, targetViewRotation.eulerAngles.y, 0), Time.deltaTime * 20);
            }
            else
            {
                thisTransform.localRotation = Quaternion.Lerp(thisTransform.localRotation, Quaternion.Euler(0, 0, Mathf.Clamp(relativeAngularVelocity.y * velocityMagnitude / 5, -1, 1) * -15), Time.deltaTime * 5);
            }

            Vector3 targetEuler = targetViewRotation.eulerAngles;
            Vector3 ourEuler = thisTransform.eulerAngles;
            Vector3 ourAngleDiff = ourEuler - targetEuler;

            ourAngleDiff = Math.Vector3CorrectRotation(ourAngleDiff);

            if (enableLookAround)
            {
                thisAnimator.SetFloat("LookVertical", ourAngleDiff.x, 0.02f, Time.deltaTime);
                thisAnimator.SetFloat("LookHorizontal", ourAngleDiff.y, 0.02f, Time.deltaTime);
            }

            if (!float.IsNaN(relativeVelocity.z)) thisAnimator.SetFloat(hashSpeed, relativeVelocity.z, 0.05f, Time.deltaTime);
            if (!float.IsNaN(relativeVelocity.z)) thisAnimator.SetFloat(hashDirection, relativeVelocity.x, 0.05f, Time.deltaTime);
            thisAnimator.SetFloat("DirectionChange", relativeAngularVelocity.y, 0.05f, Time.deltaTime);
        }

        void FixedUpdate()
        {
            velocityMagnitude = velocity.magnitude;
        }

        void OnAnimatorIK(int layerIndex)
        {
            if (leftHandIK)
            {
                thisAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                thisAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                thisAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIK.position);
                thisAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIK.rotation);
            }

            if (rightHandIK)
            {
                thisAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                thisAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                thisAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIK.position);
                thisAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIK.rotation);
            }
            //thisAnimator.FootPlacement(true, 0.15f, feetLayers);
        }

        #endregion

        #region Look At

        Quaternion targetRot = Quaternion.identity;
        public bool resetWaistRotation = false;
        public bool handleIdle;
        public float waistResetDirection;

        void HandleLookAt()
        {
            handleIdle = velocityMagnitude < 0.1f;
            if (handleIdle)
            {
                if (!resetWaistRotation)
                {
                    Vector3 targetDir = targetViewRotation * Vector3.forward;

                    waistResetDirection = Vector3.Dot(targetDir, transform.forward);
                    float angle = Mathf.Acos(waistResetDirection) * Mathf.Rad2Deg;

                    //float angle = Quaternion.Angle(thisTransform.localRotation, Quaternion.Euler(0, playerControllerSettings.targetViewRotation.eulerAngles.y, 0));
                    if (angle > 65)
                    {
                        targetRot = Quaternion.Euler(0, targetViewRotation.eulerAngles.y, 0);
                        resetWaistRotation = true;
                    }
                    thisAnimator.SetFloat(hashDirection, 0, 0.1f, Time.fixedDeltaTime);
                }
                else
                {
                    thisTransform.localRotation = Quaternion.RotateTowards(thisTransform.localRotation, targetRot, 5f);
                    thisAnimator.SetFloat(hashDirection, waistResetDirection < 0 ? -1 : 1, 0.1f, Time.fixedDeltaTime);

                    Vector3 targetDir = targetRot * Vector3.forward;
                    float dot = Vector3.Dot(targetDir, transform.forward);
                    float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
                    if (angle < 1)
                        resetWaistRotation = false;
                    else if (angle > 65)
                    {
                        resetWaistRotation = false;
                        targetRot = Quaternion.Euler(0, targetViewRotation.eulerAngles.y, 0);
                        //print(dot <= 0 ? "left " + dot : "right " + dot);
                    }
                }
            }
        }

        #endregion

        #region VelocityCalculation

        Vector3 lastPosition = Vector3.zero;
        Vector3 lastRotation = Vector3.zero;

        void CalculateVelocity()
        {
            relativeVelocity = thisTransform.InverseTransformDirection(thisTransform.localPosition - lastPosition) / Time.fixedDeltaTime;
            relativeAngularVelocity = Math.Vector3CorrectRotation(thisTransform.localEulerAngles - lastRotation) / Time.fixedDeltaTime;

            lastPosition = thisTransform.localPosition;
            lastRotation = thisTransform.localEulerAngles;
        }

        #endregion

        #region Weapon Handeling

        public WeaponSettings weaponSettings = new WeaponSettings();

        #region Firing

        float lastShootTime;

        /// <summary>
        /// Fire the current gun while respecting the firerate variable
        /// </summary>
        public void FireAutomatic()
        {
            if (Time.time > lastShootTime)
            {
                lastShootTime = Time.time + weaponSettings.currentWeapon.fireSettings.fireRate;
                FireGun();
            }
        }

        /// <summary>
        /// Fires the current gun and ignoring the firerate variable
        /// WARNING: DO NOT USE THIS EVERY FRAME
        /// </summary>
        public void FireSemiAutomatic()
        {
            FireGun();
        }

        void FireGun()
        {
            PlayerWeaponInfo cw = weaponSettings.currentWeapon;

            if (cw == null)
                return;

            float[] randomRange = { 0, 0 };

            randomRange[0] = UnityEngine.Random.Range(-cw.fireSettings.sprayRate, cw.fireSettings.sprayRate);
            randomRange[1] = UnityEngine.Random.Range(-cw.fireSettings.sprayRate, cw.fireSettings.sprayRate);

            Vector3 currentSpawnPos = cw.projectileSpawnpoint.position;
            Quaternion currentSpawnRot = targetViewRotation * Quaternion.Euler(randomRange[0], randomRange[1], 0);
            MultiplayerProjectile proj = PoolManager.CreateProjectile(weaponSettings.projectileBullet, currentSpawnPos, currentSpawnRot);
            proj.StartProjectile(null);
            cw.muzzleFlash.PlayParticleEffect();
            //SoundManager.PlaySound(weaponSettings.weaponAudioSource, cw.soundSettings.audioShootClipName);
        }

        #endregion

        #region Switch Weapons

        public void DisableAllWeapons()
        {
            foreach (PlayerWeaponInfo wep in weaponSettings.allWeapons)
                if (wep.gameObject) wep.gameObject.SetActive(false);
        }

        public void SwitchWeapon(string weapon)
        {
            DisableAllWeapons();
            PlayerWeaponInfo nextWep = weaponSettings.GetWeapon(weapon);
            weaponSettings.currentWeapon = nextWep;

            if (nextWep != null)
                if (nextWep.gameObject != null)
                    nextWep.gameObject.SetActive(true);
        }

        #endregion

        [System.Serializable]
        public class WeaponSettings
        {
            public PlayerWeaponInfo currentWeapon;
            public PlayerWeaponInfo[] allWeapons;

            public MultiplayerProjectile projectileBullet;

            public AudioSource weaponAudioSource;

            public PlayerWeaponInfo GetWeapon(string name)
            {
                foreach (PlayerWeaponInfo wep in allWeapons)
                    if (wep.weaponName.ToLower() == name.ToLower())
                        return wep;
                return null;
            }
        }

        #endregion

        #region Actions

        public void SetStance(PlayerStance newStance)
        {
            playerStance = newStance;

            thisAnimator.SetInteger("Stance", playerStance == PlayerStance.Standing ? 0 : playerStance == PlayerStance.Crouching ? 1 : 2);
        }

        public void SetStance(int nextStance)
        {
            thisAnimator.SetInteger("Stance", nextStance);
        }

        public void PlayerAction(int actionID, bool enableLookAround)
        {
            thisAnimator.SetInteger("animationID", actionID);
            thisAnimator.SetTrigger("playAction");

            this.enableLookAround = enableLookAround;
            if (!enableLookAround)
            {
                thisAnimator.SetFloat("LookVertical", 0);
                thisAnimator.SetFloat("LookHorizontal", 0);
            }
        }

        public void PlayExecutionAnimation(bool victim, bool enable)
        {
            thisAnimator.CrossFade(enable ? (victim ? "ThroatSlitVictim" : "ThroatSlitAttacker") : "Standing", 0.5f);

            this.enableLookAround = !enable;
            if (enable)
            {
                thisAnimator.SetFloat("LookVertical", 0);
                thisAnimator.SetFloat("LookHorizontal", 0);
            }
        }

        #endregion

        #region Teams

        public void SetTeamColor(int team)
        {
            /*Color targetColor = team == 0 ? new Color(0.2f, 0.63f, 1) : new Color(1, 0.3f, 0.2f);

            for (int i = 0; i < renderersAffectedByTeam.Length; i++)
                renderersAffectedByTeam[i].material.SetColor("_Color", targetColor);*/

            for (int i = 0; i < renderersAffectedByTeam.Length; i++)
                renderersAffectedByTeam[i].material.SetTexture("_MainTex", teamTextures[team]);
        }

        #endregion

        #region Character Rendering

        public CustomizableRenderer[] allCustomizableRenderers;

        [System.Serializable]
        public class CustomizableRenderer
        {
            public string name;
        }

        #endregion

        Vector3 lookAtDamper;
    }
}