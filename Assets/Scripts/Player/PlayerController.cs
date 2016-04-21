using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XInputDotNetPure;
using AngryRain.Multiplayer;
using UnityEngine.Audio;

namespace AngryRain
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerVariables playerVariables = new PlayerVariables();
        public AnimationSettings animationSettings = new AnimationSettings();
        public ObjectHolder objectHolder = new ObjectHolder();
        public WeaponSettings weaponSettings = new WeaponSettings();
        public GUITextures guiTextures = new GUITextures();
        public BoostSettings boostSettings = new BoostSettings();

        public PlayerCamera playerCamera;
        public PlayerMovement playerMovement;

        Vector3[] weaponOffsetPosition = new Vector3[7]; //0-standard 1-firecontroller 2-AimController 3-Sway 4-Animation
        Vector3[] weaponOffsetRotation = new Vector3[4]; //0-animation 1-firecontroller 2-Sway 3-aim
        private float lastFireTime;
        private float recoilRecoverTime;

        public LocalPlayer localPlayer { get; set; }
        public PlayerManager playerManager { get; set; }
        public Rewired.Player input { get; set; }

        public AudioMixerGroup weaponMixerGroup;
        public AudioMixerGroup playerMixerGroup;

        private Transform characterHeadTransform;

        #region MonoBehaviours

        public void Initialize()
        {
            foreach (PlayerWeaponInfo p in weaponSettings.allWeapons)
                p.InitializeWeapon();

            characterHeadTransform = playerManager.playerCharacter.animationSettings.cameraFollow;
            input = Rewired.ReInput.players.GetPlayer(playerManager.clientPlayer.lPlayerIndex);

            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer ren in allRenderers)
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            playerCamera.camera.renderingPath = OptionManager.currentOptions.renderingPath;

            if (!playerManager)
                playerCamera.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!playerManager || !playerManager.clientPlayer.isAlive) return;
            StanceController();
            FireController();
            HandleSway();
            //HandleBoosters();
            UpdateRecoil();
            AnimationUpdate();
            AimController();
            UpdateWeaponOffset();
            CalcWeaponOffsetPosition(true);
        }

        void FixedUpdate()
        {
            if (input.GetButtonDown("Reload")) ReloadWeapon();

            if (input.GetButtonDown("Switch Weapon")) SwitchWeapon(weaponSettings.currentWeaponSpot == CustomizationManager.WeaponSpot.Primary ? CustomizationManager.WeaponSpot.Secondary : CustomizationManager.WeaponSpot.Primary);
            if (input.GetButtonDown("Switch to Primary")) SwitchWeapon(CustomizationManager.WeaponSpot.Primary);
            if (input.GetButtonDown("Switch to Secondary")) SwitchWeapon(CustomizationManager.WeaponSpot.Secondary);

            if (input.GetButtonDown("Grenade") && !playerVariables.isThrowingGrenade)
                StartCoroutine("ThrowGrenade");

            if (playerVariables.cameraFollowHeadRotation)
                playerCamera.transform.rotation = characterHeadTransform.rotation;
        }

        #endregion

        #region Offset

        Vector3 CalcWeaponOffsetPosition(bool setPos)
        {
            Vector3 totalPos = Vector3.zero;
            for (int i = 0; i < weaponOffsetPosition.Length; i++)
                totalPos += weaponOffsetPosition[i];
            if (setPos)
                objectHolder.weaponOffsetTransform.localPosition = totalPos;
            return totalPos;
        }

        void UpdateWeaponOffset()
        {
            Vector3 totalPos = Vector3.zero;

            for (int i = 0; i < weaponOffsetRotation.Length; i++)
                totalPos += Math.Vector3CorrectRotation(weaponOffsetRotation[i]);

            objectHolder.weaponOffsetTransform.localEulerAngles = totalPos;
            objectHolder.weaponRotOffsetTransform.localPosition = objectHolder.cameraOfffsetTransform.localPosition;
            objectHolder.weaponRotOffsetTransform.localEulerAngles = objectHolder.cameraOfffsetTransform.localEulerAngles;
        }

        #endregion

        #region Animation

        void AnimationUpdate()
        {
            WalkAnimUpdate();
        }

        private bool prevIsRunning;
        void WalkAnimUpdate()
        {
            Animator wepAnimator = animationSettings.weaponholderAnimation;
            bool clampRange = playerVariables.isReloading || playerMovement.isGrounded;

            float limit = playerMovement.velocityMagnitude;

            if (playerVariables.isAiming) limit = 0;

            wepAnimator.SetFloat("walkspeedstate", clampRange ? limit : 0, 0.075f, Time.smoothDeltaTime);
            //wepAnimator.SetBool("isRunning", playerMovement.isRunning);
            animationSettings.cameraholderAnimation.SetFloat("walkspeed", playerMovement.isGrounded ? playerMovement.velocityMagnitude : 0, 0.1f, Time.smoothDeltaTime);

            if (weaponSettings.currentWeapon != null)
                weaponOffsetPosition[0] = weaponSettings.currentWeapon.aimSettings.idlePosition;

            if (playerMovement.isRunning != prevIsRunning)
            {
                wepAnimator.CrossFade(playerMovement.isRunning ? weaponSettings.currentWeapon.animationVariables.runAnimation : "run stop", 0.25f);
            }
            prevIsRunning = playerMovement.isRunning;
        }

        public GameObject FindInChildren(GameObject go, string name)
        {
            if (go.name == name)
                return go;
            else
                return (from x in go.GetComponentsInChildren<Transform>()
                        where x.gameObject.name == name
                        select x.gameObject).First();
        }

        [System.Serializable]
        public class AnimationSettings
        {
            public Animator weaponholderAnimation;
            public Transform weaponholderTransform;

            public Animator cameraholderAnimation;
        }

        #endregion

        #region Handle Input

        #region Fire Controller

        private Vector3 recoilHardPositionDamp;
        private Vector3 recoilSoftPositionDamp;

        private Vector3 recoilHardRotationDamp;
        private Vector3 recoilSoftRotationDamp;

        private Vector3 cameraRecoilHardDamp;
        private Vector3 cameraRecoilSoftDamp;

        private Quaternion lastCameraRotation;

        public float softRecoilKickback { get { return recoilSoftPositionDamp.z; } }
        public float hardRecoilKickback { get { return recoilHardPositionDamp.z; } }

        public bool isFiring;
        public PooledAudioSource lastFireAudio;

        float timeLastShot;

        void FireController()
        {
            PlayerWeaponInfo cw = weaponSettings.currentWeapon;

            if (cw == null)
                return;

            float DT = Time.smoothDeltaTime;
            PlayerWeaponInfo.FireSettings fS = cw.fireSettings;
            PlayerWeaponInfo.FireSettings.RecoilSettings rS = playerVariables.isAiming ? fS.recoilAim : fS.recoilHip;
            bool iF = Time.time > (lastFireTime - DT);
            bool inputForShooting = (fS.allAvailableFiringModes.Length > 0 && fS.allAvailableFiringModes[0] == FiringMode.Automatic) ? input.GetButton("Fire") : input.GetButtonDown("Fire");

            if (playerVariables.canShoot && inputForShooting && iF && playerVariables.walkingState != Movement.WalkingState.Running && fS.currentAmmoMagazine > 0 && cw != null && CanFire() && fS.hasRoundInChamber)
            {
                if (!isFiring)
                {
                    isFiring = true;
                    if (playerManager)
                        playerManager.Local_UpdateFiring(isFiring, fS.allAvailableFiringModes[0]);

                    Animator wepAnimator = animationSettings.weaponholderAnimation;
                    wepAnimator.SetBool("IsFiring", true);

                    if (input.controllers.Joysticks.Count > 0 && input.controllers.Joysticks[0].supportsVibration)
                        input.controllers.Joysticks[0].SetVibration(0.4f, 0.4f);
                }

                fS.currentAmmoMagazine -= 1;
                if (fS.currentAmmoMagazine == 0 || fS.allAvailableFiringModes[0] == FiringMode.BoltAction)
                    fS.hasRoundInChamber = false;

                localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoCurrent(fS.currentAmmoMagazine);

                //Enabling MuzzleFlash
                cw.muzzleFlash.PlayParticleEffect();

                timeLastShot = Time.time;
                lastFireTime = Time.time + fS.fireRate;
                recoilRecoverTime = Time.time + 0.1f;

                //Recoil Addition - Weapon
                recoilHardPositionDamp.x += UnityEngine.Random.Range(-rS.positionRecoilValues.x, rS.positionRecoilValues.x) * (recoilHardPositionDamp.x > 0 ? -1 : 1);
                //recoilHardPositionDamp.x += rS.positionRecoilValues.x;
                recoilHardPositionDamp.y += rS.positionRecoilValues.y/* + Random.Range(-rS.positionRecoilValues.y, rS.positionRecoilValues.y)*/;
                recoilHardPositionDamp.z += rS.positionRecoilValues.z;
                recoilHardPositionDamp = Math.Vector3Clamp(recoilHardPositionDamp, rS.positionRecoilMaximum);

                //recoilHardRotationDamp.x += UnityEngine.Random.Range(-recoilHardRotationDamp.x, rS.rotationRecoilValues.x);
                recoilHardRotationDamp.x += rS.rotationRecoilValues.x;
                recoilHardRotationDamp.y += UnityEngine.Random.Range(-recoilHardRotationDamp.y, rS.rotationRecoilValues.y);
                recoilHardRotationDamp.z = recoilHardRotationDamp.z > 0 ? -rS.rotationRecoilValues.z : rS.rotationRecoilValues.z;

                //Recoil Addition - Camera
                cameraRecoilHardDamp += rS.cameraRecoilValues + Math.Vector3Random(-rS.cameraRecoilRandomValues, rS.cameraRecoilRandomValues);
                cameraRecoilHardDamp = Math.Vector3Clamp(cameraRecoilHardDamp, rS.cameraRecoilMaximum);

                //Randomize the direction for spray when not aimed
                float[] randomRange = { 0, 0 };

                if (!playerVariables.isAiming)
                {
                    randomRange[0] = UnityEngine.Random.Range(-fS.sprayRate, fS.sprayRate);
                    randomRange[1] = UnityEngine.Random.Range(-fS.sprayRate, fS.sprayRate);
                }

                Vector3 currentSpawnPos = cw.projectileSpawnpoint.position;
                cw.projectileSpawnpoint.rotation = lastCameraRotation;
                cw.projectileSpawnpoint.Rotate(randomRange[0], randomRange[1], 0, Space.Self);

                //Create Projectile
                if (fS.firingType == FiringType.Bullet)
                {
                    MultiplayerProjectile proj = PoolManager.CreateProjectile(objectHolder.projectileBullet, currentSpawnPos, cw.projectileSpawnpoint.rotation);

                    //Fire Projectile
                    if (playerManager != null)
                    {
                        proj.StartProjectile(new Multiplayer.DamageGiver(playerManager.clientPlayer, cw.serverWeaponID), true, false);
                    }
                    else
                        proj.StartProjectile(new Multiplayer.DamageGiver(null, -1));
                }
                else
                {
                    if (playerManager)
                        playerManager.Local_FireServerSideWeapon(fS.firingType, cw.serverWeaponID, currentSpawnPos, cw.projectileSpawnpoint.rotation);
                }

                if (cw.animationVariables.playFireWhenAiming && playerVariables.isAiming || !playerVariables.isAiming)
                {
                    if (cw.animationVariables.hasFire && cw.fireSettings.currentAmmoMagazine > 0)
                        cw.animation.CrossFade("Fire", 0.1f, 0, 0);
                    else if (cw.animationVariables.hasFireEmpty && cw.fireSettings.currentAmmoMagazine == 0)
                        cw.animation.CrossFade("Fire Last", 0.1f, 0, 0);
                }

                //Play fire animation when not aimed
                if (!playerVariables.isAiming)
                {
                    //OLD NEEDS REMAKE | Play global shooting animation on camera
                    animationSettings.cameraholderAnimation.CrossFade("camShoot", 0.1f, 0, 0);
                }


                //Play firing sound
                lastFireAudio = SoundManager.PlayAudioAtPoint(cw.soundSettings.audioShootClip, transform.position, transform, 0, 0, 1, 1, 5, 5, weaponMixerGroup);

                //Handle Bolt Action
                if (fS.allAvailableFiringModes[0] == FiringMode.BoltAction)
                {
                    StartCoroutine(BoltWeaponIE());
                }

                /*RaycastHit rayHit;
                if (Physics.Raycast(currentPos, targetPos - currentPos, out rayHit))
                {
                    if (rayHit.rigidbody)
                        rayHit.rigidbody.SendMessage("ReceiveDamage", cw.serverWeaponInfo, SendMessageOptions.DontRequireReceiver);
                }*/
            }
            else if (isFiring && iF)
            {
                isFiring = false;
                if (playerManager)
                    playerManager.Local_UpdateFiring(isFiring, fS.allAvailableFiringModes[0]);

                Animator wepAnimator = animationSettings.weaponholderAnimation;
                wepAnimator.SetBool("IsFiring", false);


                if (input.controllers.Joysticks.Count > 0 && input.controllers.Joysticks[0].supportsVibration)
                    input.controllers.Joysticks[0].StopVibration();
            }

        }

        void UpdateRecoil()
        {
            PlayerWeaponInfo cw = weaponSettings.currentWeapon ?? new PlayerWeaponInfo();

            //Recoil Handeling - Weapon

            float DT = Time.smoothDeltaTime;
            bool iF = Time.time > (recoilRecoverTime + DT);
            PlayerWeaponInfo.FireSettings fS = cw.fireSettings;
            PlayerWeaponInfo.FireSettings.RecoilSettings rS = playerVariables.isAiming ? fS.recoilAim : fS.recoilHip;

            Math.Vector3Lerp(ref recoilSoftRotationDamp, recoilHardRotationDamp, iF ? Math.Vector3Multiply(rS.rotationRecoilSoftSpeedIdle, DT) : Math.Vector3Multiply(rS.rotationRecoilSoftSpeed, DT));
            Math.Vector3Lerp(ref recoilHardRotationDamp, Vector3.zero, iF ? Math.Vector3Multiply(rS.rotationRecoilHardSpeedIdle, DT) : Math.Vector3Multiply(rS.rotationRecoilHardSpeed, DT));
            weaponOffsetRotation[1] = recoilSoftRotationDamp;

            Math.Vector3Lerp(ref recoilSoftPositionDamp, recoilHardPositionDamp, iF ? Math.Vector3Multiply(rS.positionRecoilSoftSpeedIdle, DT) : Math.Vector3Multiply(rS.positionRecoilSoftSpeed, DT));
            Math.Vector3Lerp(ref recoilHardPositionDamp, Vector3.zero, iF ? Math.Vector3Multiply(rS.positionRecoilHardSpeedIdle, DT) : Math.Vector3Multiply(rS.positionRecoilHardSpeed, DT));
            weaponOffsetPosition[1] = recoilSoftPositionDamp;

            //Recoil Handeling - Camera
            Math.Vector3Lerp(ref cameraRecoilSoftDamp, cameraRecoilHardDamp, Math.Vector3Multiply(iF ? rS.cameraRecoilSoftSpeedIdle : rS.cameraRecoilSoftSpeed, DT));
            Math.Vector3Lerp(ref cameraRecoilHardDamp, Vector3.zero, Math.Vector3Multiply(iF ? rS.cameraRecoilHardSpeedIdle : rS.cameraRecoilHardSpeed, DT));

            //Recoil Handeling - Addition to the camera
            objectHolder.cameraOfffsetTransform.Rotate(cameraRecoilSoftDamp, Space.Self);
            lastCameraRotation = objectHolder.cameraOfffsetTransform.rotation;
        }

        public void ReloadWeapon()
        {
            if (!playerVariables.isReloading && !playerVariables.isSwitchingWeapons && !playerVariables.isBoltingWeapon)
            {
                StopCoroutine("ReloadWeaponIE");
                StartCoroutine("ReloadWeaponIE");
            }
        }

        IEnumerator ReloadWeaponIE()
        {
            PlayerWeaponInfo cW = weaponSettings.currentWeapon;
            /*if (cW.animationVariables.reloadAnim != null && cW.animationVariables.reloadAnimEmpty != null)
            {
                AnimationClip anim = cW.fireSettings.currentAmmoMagazine == 0 ? cW.animationVariables.reloadAnimEmpty : cW.animationVariables.reloadAnim;

                cW.animation.CrossFade("Reload", 0.1f);

                playerVariables.isReloading = true;
                cW.fireSettings.currentAmmoMagazine = 0;
                localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoCurrent(cW.fireSettings.currentAmmoMagazine);
                localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoRemaining(cW.fireSettings.maxAmmoMagazine);

                StartCoroutine(PlaySoundArray(cW.soundSettings.reload));

                yield return new WaitForSeconds(anim.length);
            }*/

            bool playReloadEmpty = cW.fireSettings.currentAmmoMagazine == 0 && cW.animationVariables.hasReloadEmpty;
            bool playReload = cW.animationVariables.hasReload;

            playerVariables.isReloading = true;
            cW.fireSettings.currentAmmoMagazine = 0;
            localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoCurrent(cW.fireSettings.currentAmmoMagazine);
            localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoRemaining(cW.fireSettings.maxAmmoMagazine);
            cW.animation.CrossFade(playReloadEmpty ? "Reload Empty" : playReload ? "Reload" : "Idle", 0.1f);

            StartCoroutine(PlaySoundArray(playReloadEmpty ? cW.soundSettings.reloadEmpty : cW.soundSettings.reload));

            yield return new WaitForSeconds(playReloadEmpty ? cW.animationVariables.reloadEmptyLength : playReload ? cW.animationVariables.reloadLength : 1);

            cW.fireSettings.currentAmmoMagazine = cW.fireSettings.maxAmmoMagazine;
            cW.fireSettings.hasRoundInChamber = true;
            playerVariables.isReloading = false;
            playerVariables.isBoltingWeapon = false;

            localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoCurrent(cW.fireSettings.currentAmmoMagazine);
            localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoRemaining(cW.fireSettings.maxAmmoMagazine);
        }

        IEnumerator BoltWeaponIE()
        {
            while (playerVariables.isAiming || playerVariables.isReloading)
                yield return null;
            PlayerWeaponInfo cw = weaponSettings.currentWeapon;

            if (!cw.fireSettings.hasRoundInChamber)
            {
                playerVariables.isBoltingWeapon = true;
                yield return new WaitForSeconds(0.2f);
                if (cw.animationVariables.hasChamberBolt)
                {
                    cw.animation.CrossFade("Bolt Weapon", 0.2f);
                    yield return new WaitForSeconds(cw.animationVariables.chamberBoltLength);
                }
            }

            playerVariables.isBoltingWeapon = false;
            cw.fireSettings.hasRoundInChamber = true;
        }

        IEnumerator ThrowGrenade()
        {
            PlayerWeaponInfo cW = weaponSettings.currentWeapon;
            cW.animation.CrossFade("Grenade Throw", 0.1f, 0, 0);
            SoundManager.PlayAudioAtPoint(weaponSettings.clothMovementClip, transform.position, transform, 0, 0, 1, 1, 5, 5, playerMixerGroup);

            playerVariables.isThrowingGrenade = true;
            playerVariables.canShoot = false;
            playerVariables.canAim = false;

            //yield return new WaitForSeconds(cW.animationVariables.grenadeThrowLengthInstantiate - (TNManager.ping / 1000));

            playerManager.Local_FireServerSideWeapon(FiringType.Grenade, -1, playerCamera.position, playerCamera.rotation);

            yield return new WaitForSeconds(cW.animationVariables.grenadeThrowLengthEnd + (TNManager.ping / 1000));

            playerVariables.canShoot = true;
            playerVariables.canAim = true;
            playerVariables.isThrowingGrenade = false;
        }

        #endregion

        #region Aim Controller

        float aimTime;
        float aimCurFov;
        float aimCurWepFov;
        Vector3 aimCurPos;
        Vector3 aimCurRot;

        void AimController()
        {
            if (!playerVariables.canAim)
                return;

            bool nextVar = input.GetButton("Aim") && CanAim();

            if (nextVar != playerVariables.isAiming)
            {
                if (Time.time - aimTime > 2)
                    SoundManager.PlayAudioAtPoint(weaponSettings.clothMovementClip, transform.position, transform, 0, 0, 1, 0.5f, 5, 5, playerMixerGroup);

                aimTime = Time.time;
                aimCurFov = playerCamera.camera.fieldOfView;
                aimCurPos = weaponOffsetPosition[2];
                aimCurRot = weaponOffsetRotation[3];
                if (weaponSettings.currentWeapon != null)
                    aimCurWepFov = objectHolder.weaponRotOffsetTransform.localScale.z;

                playerVariables.isAiming = nextVar;
                animationSettings.weaponholderAnimation.SetBool("isAiming", nextVar);
            }

            float standardFOV = OptionManager.currentOptions.fieldOfView;

            float lerpTime = (Time.time - aimTime) * 5f;

            if (weaponSettings.currentWeapon != null)
            {
                playerCamera.camera.fieldOfView = Mathf.Lerp(aimCurFov, playerVariables.isAiming ? weaponSettings.currentWeapon.aimSettings.aimedFieldOfView : standardFOV, lerpTime);
                weaponOffsetPosition[2] = Math.Vector3Lerp(aimCurPos, playerVariables.isAiming ? weaponSettings.currentWeapon.aimSettings.aimPosition : Vector3.zero, lerpTime);
                weaponOffsetRotation[3] = Vector3.Lerp(aimCurRot, playerVariables.isAiming ? Vector3.zero : weaponSettings.currentWeapon.aimSettings.weaponRotation, lerpTime);
                UpdateWeaponFOV(lerpTime);
            }
            else
            {
                playerCamera.camera.fieldOfView = Mathf.Lerp(playerCamera.camera.fieldOfView, standardFOV, lerpTime);
                weaponOffsetPosition[2] = Math.Vector3Lerp(weaponOffsetPosition[2], Vector3.zero, lerpTime);
            }
        }

        void UpdateWeaponFOV(float lerpTime)
        {
            float targetScale = playerVariables.isAiming ? weaponSettings.currentWeapon.aimSettings.weaponFieldOfViewPercentageAim : weaponSettings.currentWeapon.aimSettings.weaponFieldOfViewPercentageIdle;
            Vector3 currentScale = objectHolder.weaponRotOffsetTransform.localScale;
            targetScale *= 60 / playerCamera.camera.fieldOfView;

            if (Mathf.Abs(targetScale - currentScale.z) > 0.1f)
            {
                currentScale.z = Mathf.Lerp(aimCurWepFov, targetScale, lerpTime);
                currentScale.x = 1;
                currentScale.y = 1;
                objectHolder.weaponRotOffsetTransform.localScale = currentScale;
                /*weaponSettings.currentWeapon.objectHolder.weaponGameObject.SetActive(false);
                weaponSettings.currentWeapon.objectHolder.weaponGameObject.SetActive(true);*/
            }
        }

        #endregion

        #region Weapon Switch Controller

        public void DisableAllWeapons()
        {
            foreach (PlayerWeaponInfo wep in weaponSettings.allWeapons)
                if (wep.gameObject) wep.gameObject.SetActive(false);
        }

        public void InitializeAttachments()
        {
            //Destroy Old Attachments
            Transform[] attachments = weaponSettings.currentWeapon.gameObject.transform.GetComponentsInChildren<Transform>(true);

            for (int a = 0; a < attachments.Length; a++)
            {
                if (attachments[a].transform.tag == "attachment")
                {
                    Destroy(attachments[a].gameObject);
                }
            }

            if (CustomizationManager.instance)
            {
                ServerWeaponInfo swi = CustomizationManager.GetServerWeapon(weaponSettings.currentWeapon.serverWeaponID);
                //Initialize new ones
                for (int i = 0; i < swi.weaponAttachments.Length; i++)
                {
                    ServerWeaponInfo.WeaponAttachment wa = swi.weaponAttachments[i];
                    if (wa.enabled)
                    {
                        for (int a = 0; a < wa.parts.Length; a++)
                        {
                            /*Transform attachment = Instantiate(weaponSettings.currentWeapon.serverWeaponInfo.weaponAttachments[i].prefab) as Transform;
                            attachment.parent = weaponSettings.currentWeapon.objectHolder.attachmentHolder.transform;
                            attachment.localScale = weaponSettings.currentWeapon.serverWeaponInfo.weaponAttachments[i].Positioning.size;
                            attachment.localPosition = weaponSettings.currentWeapon.serverWeaponInfo.weaponAttachments[i].Positioning.position;
                            attachment.localEulerAngles = weaponSettings.currentWeapon.serverWeaponInfo.weaponAttachments[i].Positioning.rotation;*/

                            GameObject ag = Instantiate(wa.parts[a]) as GameObject;
                            Transform at = ag.transform;

                            at.parent = weaponSettings.currentWeapon.attachementHolder.transform;
                            at.localScale = wa.partInfo[i].scale;
                            at.localPosition = wa.partInfo[i].position;
                            at.localEulerAngles = wa.partInfo[i].rotation;
                        }
                    }
                }
            }
        }

        public void SwitchWeapon(CustomizationManager.WeaponSpot weaponSpot, bool direct = false)
        {
            StopCoroutine("SwitchWeaponIE");
            StopCoroutine(ReloadWeaponIE());
            StopCoroutine(BoltWeaponIE());
            StartCoroutine(SwitchWeaponIE(weaponSpot, direct));
        }

        IEnumerator SwitchWeaponIE(CustomizationManager.WeaponSpot weaponSpot, bool direct)
        {
            try
            {
                weaponSettings.currentWeaponSpot = weaponSpot;
                string nextWep = weaponSpot == CustomizationManager.WeaponSpot.None ? "" : weaponSpot == CustomizationManager.WeaponSpot.Primary ? weaponSettings.primaryWeapon : weaponSettings.secondaryWeapon;
                playerVariables.isSwitchingWeapons = true;

                if (!direct && weaponSettings.currentWeapon != null)
                {
                    animationSettings.weaponholderAnimation.CrossFade("WeaponTakeaway", 0.2f, 0, 0);
                    StartCoroutine(PlaySoundArray(weaponSettings.currentWeapon.soundSettings.weaponDrop));
                    yield return new WaitForSeconds(1);
                }

                DisableAllWeapons();

                PlayerWeaponInfo wep = weaponSettings.GetWeapon(nextWep);
                weaponSettings.currentWeapon = wep;

                if (playerManager)
                    playerManager.Local_SwitchWeapon(nextWep);

                if (wep != null)
                {
                    objectHolder.weaponPivotTransform.localPosition = weaponSettings.currentWeapon.aimSettings.pivotPosition;
                    //InitializeAttachments();

                    if (weaponSettings.currentWeapon.gameObject != null)
                        weaponSettings.currentWeapon.gameObject.SetActive(true);

                    Animator animation = weaponSettings.currentWeapon.animation;

                    if (!direct)
                    {
                        if (weaponSettings.currentWeapon.animationVariables.hasTakeout)
                        {
                            animation.Play("Takeout");
                            yield return new WaitForEndOfFrame();
                            animationSettings.weaponholderAnimation.Play("Walking");
                            StartCoroutine(PlaySoundArray(weaponSettings.currentWeapon.soundSettings.weaponPickup));
                            yield return new WaitForSeconds(weaponSettings.currentWeapon.animationVariables.takeoutLength);
                        }
                        else
                        {
                            animationSettings.weaponholderAnimation.Play("WeaponPickup", 0, 0);
                            yield return new WaitForSeconds(1);
                        }
                    }

                    animation.CrossFade(playerMovement.isRunning ? weaponSettings.currentWeapon.animationVariables.runAnimation : "walking", 0.25f);

                    localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoCurrent(wep.fireSettings.currentAmmoMagazine);
                    localPlayer.playerGUI.playerInfo.UpdateWeaponAmmoRemaining(wep.fireSettings.maxAmmoMagazine);
                    localPlayer.playerGUI.playerInfo.UpdateWeaponName(wep.weaponName);
                }
            }
            finally
            {
                playerVariables.isSwitchingWeapons = false;
                playerVariables.isReloading = false;
                playerVariables.isBoltingWeapon = false;
            }
        }

        #endregion

        #region Stances

        void StanceController()
        {
            if (Input.GetKeyDown(KeyCode.X))//Set to crouch
                SetStance(playerVariables.playerStance == PlayerStance.Crouching ? PlayerStance.Standing : PlayerStance.Crouching);
        }

        public void SetStance(PlayerStance nextStance)
        {
            playerVariables.playerStance = nextStance;

            if (nextStance == PlayerStance.Standing)
            {
                GetComponent<CapsuleCollider>().center = new Vector3(0, 0.9f, 0);
                GetComponent<CapsuleCollider>().height = 1.8f;
                GetComponent<CapsuleCollider>().direction = 1;
            }
            else if (nextStance == PlayerStance.Crouching)
            {
                GetComponent<CapsuleCollider>().center = new Vector3(0, 0.75f, 0);
                GetComponent<CapsuleCollider>().height = 1.5f;
                GetComponent<CapsuleCollider>().direction = 1;
            }
            else
            {
                SwitchWeapon(CustomizationManager.WeaponSpot.None);
            }

            if (playerManager)
                playerManager.Local_SetStance(nextStance);
        }

        public void ResetAllWeaponMagazines()
        {
            foreach (PlayerWeaponInfo wep in weaponSettings.allWeapons)
                wep.fireSettings.currentAmmoMagazine = wep.fireSettings.maxAmmoMagazine;
        }

        #endregion

        #region Sway Controller

        void HandleSway()
        {
            PlayerWeaponInfo cw = weaponSettings.currentWeapon;

            if (cw == null)
                return;

            float DT = Time.smoothDeltaTime;

            PlayerWeaponInfo.SwaySettings.SettingsPerAim currentSettings = playerVariables.isAiming ? cw.swaySettings.swayWhileAiming : cw.swaySettings.swayWhileSpraying;

            Vector3 rot = new Vector3(playerCamera.angularVelocity.x, playerCamera.angularVelocity.y, playerCamera.angularVelocity.y);
            weaponOffsetRotation[2] = Math.Vector3Lerp(weaponOffsetRotation[2], Math.Vector3Multiply(currentSettings.rotationFactor, rot) / DT, currentSettings.rotationSpeed * DT);
            Vector3 pos = new Vector3(playerCamera.angularVelocity.y, playerCamera.angularVelocity.x, 0);
            weaponOffsetPosition[3] = Math.Vector3Lerp(weaponOffsetPosition[3], Math.Vector3Multiply(currentSettings.positionFactor, pos) / DT, currentSettings.postionSpeed * DT);

            if (playerVariables.isAiming)
            {
                weaponOffsetRotation[0] = Vector3.Lerp(weaponOffsetRotation[0], Vector3.zero, 10 * DT);
                weaponOffsetPosition[4] = Vector3.Lerp(weaponOffsetPosition[4], Vector3.zero, 10 * DT);
            }
            else
            {
                weaponOffsetRotation[0] = Vector3.Lerp(weaponOffsetRotation[0], new Vector3(0, 0, Mathf.Clamp(playerMovement.relativeVelocity.x, -3, 3) * -2), 10 * DT);
                weaponOffsetPosition[4] = Vector3.Lerp(weaponOffsetPosition[4], new Vector3(Mathf.Clamp(playerMovement.relativeVelocity.x, -3, 3) * 0.003f, 0, Mathf.Min(playerMovement.velocityMagnitude, 3) * -0.005f), 5 * DT);
            }
        }

        #endregion

        #region Timed Delay For Actions

        float fireDelay, aimDelay;

        public void SetFireDelay(float delay)
        {
            if (fireDelay < Time.time + delay)
                fireDelay = Time.time + delay;
        }

        public void SetAimDelay(float delay)
        {
            if (aimDelay < Time.time + delay)
                aimDelay = Time.time + delay;
        }

        public bool CanFire()
        {
            return !(playerVariables.isBoltingWeapon || playerVariables.isReloading || playerVariables.isSwitchingWeapons || playerVariables.walkingState == Movement.WalkingState.Running) && fireDelay < Time.time;
        }

        public bool CanAim()
        {
            return !(playerVariables.isBoltingWeapon || playerVariables.isReloading || playerVariables.isSwitchingWeapons || playerVariables.walkingState == Movement.WalkingState.Running) && aimDelay < Time.time;
        }

        #endregion

        #region Sound Controllers

        IEnumerator PlaySoundArray(SoundItem[] array)
        {
            bool hasFinished = false;

            float startTime = Time.time;
            while (hasFinished == false)
            {
                hasFinished = true;
                for (int i = 0; i < array.Length; i++)
                {
                    if (!array[i].hasPlayed && Time.time - startTime >= array[i].lastTimePlayed)
                    {
                        hasFinished = false;
                        array[i].hasPlayed = true;
                        array[i].Play(transform.position, transform);
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            for (int i = 0; i < array.Length; i++)
                array[i].hasPlayed = false;
        }

        #endregion

        #region Parkour Controller



        #endregion

        #endregion

        #region Global Methods



        #endregion

        #region GUI

        Color tempColor = Color.white;

        void DrawCrosshair()
        {
            if (weaponSettings.currentWeapon == null)
                return;

            Vector3 screenPos = new Vector3(Screen.width / 2, Screen.height / 2);
            int offset = (int)(recoilSoftPositionDamp.z * -750);
            offset += (int)(playerMovement.velocityMagnitude * 2);
            float spray = weaponSettings.currentWeapon.fireSettings.sprayRate;

            if (playerVariables.walkingState == Movement.WalkingState.Running || playerVariables.isAiming || playerVariables.isReloading)
                tempColor = Color.Lerp(tempColor, new Color(1, 1, 1, 0), 0.25f);
            else
                tempColor = Color.Lerp(tempColor, new Color(1, 1, 1, (6 - playerMovement.velocityMagnitude) * 0.75f), 0.1f);

            GUI.color = tempColor;

            GUI.DrawTexture(new Rect(screenPos.x + 5 + offset + spray, Screen.height - screenPos.y - 1, 5, 2), guiTextures.crosshair3);//right
            GUI.DrawTexture(new Rect(screenPos.x - 10 - offset - spray, Screen.height - screenPos.y - 1, 5, 2), guiTextures.crosshair3);//left
            GUI.DrawTexture(new Rect(screenPos.x - 1, Screen.height - screenPos.y - 10 - offset - spray, 2, 5), guiTextures.crosshair3);//up
            GUI.DrawTexture(new Rect(screenPos.x - 1, Screen.height - screenPos.y + 5 + offset + spray, 2, 5), guiTextures.crosshair3);//down

            GUI.color = Color.white;
        }

        void DrawHUD()
        {
            GUI.skin = guiTextures.hudskin;

            int health = (playerManager == null ? 100 : (int)playerManager.clientPlayer.health);

            GUI.Box(new Rect(25, Screen.height - 65, 310, 40), "");

            GUI.color = new Color(0.2f, 0.4f, 0.6f);
            GUI.Box(new Rect(90, Screen.height - 58, 235, 26), "");
            GUI.color = new Color(1, 0.4f, 0);
            GUI.Box(new Rect(90, Screen.height - 58, 235 * (health * 0.01f), 26), "", "Box2");
            GUI.color = Color.white;

            GUI.Label(new Rect(25, Screen.height - 65, 65, 40), health + "%");

            GUI.Box(new Rect(25, Screen.height - 110, 100, 40), "");

            if (weaponSettings.currentWeapon != null)
            {
                GUILayout.BeginArea(new Rect(25, Screen.height - 110, 100, 40), "");
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(weaponSettings.currentWeapon.fireSettings.currentRestAmmo.ToString(), "Label2", GUILayout.Height(40));
                GUILayout.Label(weaponSettings.currentWeapon.fireSettings.currentAmmoMagazine.ToString(), GUILayout.Height(40));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndArea();
            }
        }

        void DrawActions()
        {
            if (playerManager)
            {
                if (playerManager.clientPlayer.vehicle == null && playerManager.mVehicleTemp != null)
                {
                    GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 1.5f, 400, 30), "");
                    GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 1.5f, 400, 30), "Press [F] to enter " + playerManager.mVehicleTemp.vehicleName);
                }
                else if (playerManager.currentObjective == null && playerManager.tempObjective != null)
                {
                    GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 1.5f, 400, 30), "");
                    GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 1.5f, 400, 30), "Press [F] to plant " + playerManager.tempObjective.objectiveName);
                }
                else if (playerManager.currentObjective != null)
                {
                    GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 1.5f, 400, 30), "");
                    GUI.color = new Color(1, 0.4f, 0);
                    GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 1.5f + 2, 400 * playerManager.currentObjective.progress, 26), "");
                    GUI.color = Color.white;
                }
            }
        }

        #endregion

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

        [System.Serializable]
        public class WeaponSettings
        {
            public PlayerWeaponInfo currentWeapon;
            public List<PlayerWeaponInfo> allWeapons = new List<PlayerWeaponInfo>();
            public string primaryWeapon, secondaryWeapon;

            public AngryRain.CustomizationManager.WeaponSpot currentWeaponSpot;

            public PlayerWeaponInfo GetWeapon(string wep)
            {
                int c = allWeapons.Count;
                for (int i = 0; i < c; i++)
                {
                    PlayerWeaponInfo p = allWeapons[i];
                    if (wep.Equals(p.weaponName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return p;
                    }
                }
                return null;
            }

            public AudioClip clothMovementClip;
        }

        [System.Serializable]
        public class PlayerVariables
        {
            public bool isAiming;
            public bool isReloading;
            public bool isSwitchingWeapons;

            public bool canShoot = true;
            public bool canAim = true;

            public bool cameraFollowHeadRotation;

            public bool isBoltingWeapon;
            public bool isThrowingGrenade;

            public Movement.WalkingState walkingState = Movement.WalkingState.Idle;
            public PlayerStance playerStance = PlayerStance.Standing;
        }

        [System.Serializable]
        public class GUITextures
        {
            public Texture2D crosshair1;
            public Texture2D crosshair2;
            public Texture2D crosshair3;

            public GUISkin hudskin;
        }

        [System.Serializable]
        public class BoostSettings
        {
            public float movementBoostSpeed = 11;
            public float movementBoostTime = 0.3f;
            public float movementBoostWeaponDisableTime = 0.5f;

            public float jumpBoostSpeed = 3;
            public float jumpBoostTime = 0.25f;

            public AudioClip humanGruntSound;
            public AudioClip boosterSound;
        }
    }

    public enum BoolApply
    {
        DoNotChange,
        True,
        False
    }

    public enum PlayerStance
    {
        Standing,
        Crouching,
        Editor
    }
}