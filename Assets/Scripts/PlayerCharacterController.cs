using UnityEngine;
using System.Collections;
using AngryRain;
using AngryRain.Multiplayer;

public class PlayerCharacterController : MonoBehaviour 
{
    //Public Variables
    public AnimationSettings animationSettings = new AnimationSettings();

    [System.Serializable]
    public class AnimationSettings
    {
        public Transform leftHand;
        public Transform rightHand;

        public Vector3 velocity = Vector3.zero;

        public Vector2 lookRotation;

        public Transform cameraFollow;

        public bool debugWeaponPositionSetup;
    }

    public WeaponSettings weaponSettings = new WeaponSettings();

    [System.Serializable]
    public class WeaponSettings
    {
        public CharacterWeapon[] allWeapons;
        public CharacterWeapon currentWeapon;

        public Transform weaponHolder;

        public MultiplayerProjectile projectileBullet;

        [System.Serializable]
        public class CharacterWeapon
        {
            public string weaponName;
            public AnimationSettings animationSettings = new AnimationSettings();
            public FireSettings fireSettings = new FireSettings();

            public Vector3 holderPositionOffset;
            public Vector3 holderRotationOffset;
            public AvatarIKGoal targetHoldingWeapon = AvatarIKGoal.RightHand;

            public Vector3 lookUpOffsetIK;
            public Vector3 lookDownOffsetIK;
            public Vector3 lookLeftOffsetIK;
            public Vector3 lookRightOffsetIK;

            [System.Serializable]
            public class AnimationSettings
            {
                public AnimationClip idleClip;

                public Transform leftHandIK;
                public Transform rightHandIK;
            }

            [System.Serializable]
            public class FireSettings
            {
                public float fireRate = 0.1f;
                public float sprayRate = 1;

                public Transform projectileSpawnpoint;
                public ParticleEffect muzzleflash;
                public SoundItem firingAudioClip;
            }
        }
    }

    public SoundSettings soundSettings = new SoundSettings();

    [System.Serializable]
    public class SoundSettings
    {
        public SoundItem[] footstepSounds;
    }

    public PlayerStance playerStance;

    private bool _isGrounded;
    public bool isGrounded { set { _isGrounded = value; animator.SetBool("isGrounded", value); } get { return _isGrounded; } }

    //Private Variables
    public new Transform transform { private set; get; }
    public new GameObject gameObject { private set; get; }
    public Animator animator { private set; get; }
    public PlayerManager playerManager { private set; get; }
    public ParticleSystem stepSmoke { private set; get; }

    public bool useIK = true;

    public void Awake()
    {
        transform = GetComponent<Transform>();
        gameObject = transform.gameObject;
        animator = GetComponent<Animator>();
        playerManager = transform.root.GetComponent<PlayerManager>();

        stepSmoke = transform.Find("Step Smoke").GetComponent<ParticleSystem>();
    }

    void Start()
    {
        weaponSettings.currentWeapon = weaponSettings.allWeapons[0];
    }

    void Update()
    {
        UpdateCharacterAnimator();
        PlayFootStepSound();
    }

    void OnAnimatorIK()
    {
        UpdateWeaponTransform();
        ApplyIK();
    }

    #region Animation

    int forwardHash = Animator.StringToHash("forwardspeed");
    int sideHash = Animator.StringToHash("sidespeed");

    int horizontalLookHash = Animator.StringToHash("LookHorizontal");
    int verticalLookHash = Animator.StringToHash("LookVertical");

    Quaternion lastStandAngle = Quaternion.identity;
    bool walkingBackwards = false;

    void UpdateCharacterAnimator()
    {

        Debug.DrawRay(transform.position + Vector3.up, animationSettings.velocity, Color.red);
        Debug.DrawRay(transform.position + Vector3.up, transform.parent.InverseTransformDirection(animationSettings.velocity), Color.blue);
        if (animationSettings.velocity.magnitude > 1)
        {
            if (isGrounded)
            {
                Vector3 newVel = animationSettings.velocity;
                newVel.y = 0;
                Quaternion targetRot = Quaternion.LookRotation(newVel);

                walkingBackwards = Vector3.Dot(newVel, transform.parent.forward) < -0.1f;
                if (walkingBackwards)
                {
                    targetRot = targetRot * Quaternion.AngleAxis(180, Vector3.up);
                }

                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 8);
                lastStandAngle = transform.rotation;

                Debug.DrawRay(transform.position + transform.forward + Vector3.up, transform.forward * -2);
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8);
            }
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, lastStandAngle, Time.deltaTime * 8);

            if (Quaternion.Angle(transform.rotation, transform.parent.rotation) > 85)
                lastStandAngle = transform.parent.rotation;
        }

        animationSettings.lookRotation.y = -transform.localEulerAngles.y;

        float targetSpeed = isGrounded ? animationSettings.velocity.magnitude : 0;
        animator.SetFloat(forwardHash, walkingBackwards ? -targetSpeed : targetSpeed, 0.1f, Time.deltaTime);

        animator.SetFloat(horizontalLookHash, Math.CorrectRotation(animationSettings.lookRotation.y));
        animator.SetFloat(verticalLookHash, Math.CorrectRotation(-animationSettings.lookRotation.x));    
    }

    #endregion

    #region IK

    void UpdateWeaponTransform()
    {
        WeaponSettings.CharacterWeapon cw = weaponSettings.currentWeapon;
        Transform targetHand = cw.targetHoldingWeapon == AvatarIKGoal.LeftHand ? animationSettings.leftHand : animationSettings.rightHand;

        Vector3 lookIKOffset = Vector3.zero;
        animationSettings.lookRotation.y = -Math.CorrectRotation(animationSettings.lookRotation.y);
        Vector2 progress = animationSettings.lookRotation / 150;
        lookIKOffset += cw.lookDownOffsetIK * Mathf.Clamp(progress.y * 2, -1, 0);
        lookIKOffset += cw.lookUpOffsetIK * Mathf.Clamp(progress.y * 2, 0, 1);
        lookIKOffset += cw.lookLeftOffsetIK * Mathf.Clamp(progress.x * 2, -1, 0);
        lookIKOffset += cw.lookRightOffsetIK * Mathf.Clamp(progress.x * 2, 0, 1);

        weaponSettings.weaponHolder.rotation = targetHand.rotation; //targetHand.TransformDirection(cw.holderRotationOffset) /*+ lookIKOffset + targetHand.eulerAngles*/;
        weaponSettings.weaponHolder.Rotate(cw.holderRotationOffset, Space.Self);
        weaponSettings.weaponHolder.position = targetHand.TransformPoint(cw.holderPositionOffset);
    }

    void ApplyIK()
    {
        if (useIK)
        {
            //animator.FootPlacement(true, 0, 0);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            //animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);

            animator.SetIKPosition(AvatarIKGoal.LeftHand, weaponSettings.currentWeapon.animationSettings.leftHandIK.position);
            //animator.SetIKPosition(AvatarIKGoal.RightHand, weaponSettings.currentWeapon.animationSettings.rightHandIK.position);

            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            //animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            animator.SetIKRotation(AvatarIKGoal.LeftHand, weaponSettings.currentWeapon.animationSettings.leftHandIK.rotation * Quaternion.Euler(0, -90, 0));
            //animator.SetIKRotation(AvatarIKGoal.RightHand, weaponSettings.currentWeapon.animationSettings.rightHandIK.rotation * Quaternion.Euler(0, -90, 0));
        }
    }

    #endregion

    #region Actions

    public void SetStance(PlayerStance newStance)
    {
        playerStance = newStance;

        animator.SetInteger("Stance", playerStance == PlayerStance.Standing ? 0 : playerStance == PlayerStance.Crouching ? 1 : 2);
    }

    public void SetStance(int nextStance)
    {
        animator.SetInteger("Stance", nextStance);
    }

    public void PlayerAction(int actionID)
    {
        animator.SetInteger("animationID", actionID);
        animator.SetTrigger("playAction");
    }

    public void PlayExecutionAnimation(bool victim, bool enable)
    {
        animator.CrossFade(enable ? (victim ? "ThroatSlitVictim" : "ThroatSlitAttacker") : "Standing", 0.5f);

        if (enable)
        {
            animator.SetFloat("LookVertical", 0);
            animator.SetFloat("LookHorizontal", 0);
        }
    }

    int lastStepSound;
    float footSwitchTime;
    float footStepTime;

    public void PlayFootStepSound()
    {
        if (!isGrounded)
            return;

        lastStepSound++;
        if (lastStepSound == soundSettings.footstepSounds.Length)
            lastStepSound = 0;

        if (Time.time > footStepTime && animationSettings.velocity.magnitude > 2)
        {
            footStepTime = Time.time + 0.35f;
            soundSettings.footstepSounds[lastStepSound].Play(transform.position, transform);
        }

        float stepHeight = animator.GetFloat("StepHeight");

        if (Time.time > footSwitchTime)
        {
            if (stepHeight > 0.7f)
            {
                footSwitchTime = Time.time + 0.2f;
                stepSmoke.transform.position = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                stepSmoke.Play();
                //SoundManager.PlayAudioAtPoint(soundSettings.footstepSounds[lastStepSound], transform.position, null);
            }
            else if (stepHeight < -0.7f)
            {
                footSwitchTime = Time.time + 0.2f;
                stepSmoke.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                stepSmoke.Play();
            }
        }
    }

    #endregion

    #region Weapon Handeling

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
        WeaponSettings.CharacterWeapon cw = weaponSettings.currentWeapon;

        if (cw == null || playerManager.clientPlayer.isMe)
            return;

        float[] randomRange = { 0, 0 };

        randomRange[0] = UnityEngine.Random.Range(-cw.fireSettings.sprayRate, cw.fireSettings.sprayRate);
        randomRange[1] = UnityEngine.Random.Range(-cw.fireSettings.sprayRate, cw.fireSettings.sprayRate);

        Vector3 currentSpawnPos = cw.fireSettings.projectileSpawnpoint.position;
        Quaternion currentSpawnRot = Quaternion.Euler(animationSettings.lookRotation.x, transform.parent.eulerAngles.y, 0) * Quaternion.Euler(randomRange[0], randomRange[1], 0);
        MultiplayerProjectile proj = PoolManager.CreateProjectile(weaponSettings.projectileBullet, currentSpawnPos, currentSpawnRot);
        proj.StartProjectile(null);

        cw.fireSettings.muzzleflash.PlayParticleEffect();
        cw.fireSettings.firingAudioClip.Play(transform.position, transform);
    }

    #endregion

    #region WeaponSwitching

    public void DisableAllWeapons()
    {

    }

    public void SwitchWeapon(string weapon)
    {

    }

    #endregion

    #endregion

    #region Customization

    public void SetTeamColor(int index)
    {

    }

    #endregion

    #region Effects

    void EnableFadeEffect(bool fadeIn)
    {
        StopCoroutine("HandleFadeEffect");
        StartCoroutine("HandleFadeEffect");
    }

    IEnumerator HandleFadeEffect(bool fadeIn)
    {
        yield return new WaitForEndOfFrame();
    }

    #endregion

    #region Renderers



    #endregion
}
