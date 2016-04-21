using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AngryRain
{
    public class CustomizationManager : MonoBehaviour
    {
        public static CustomizationManager instance;

        public List<ServerWeaponInfo> allWeapons = new List<ServerWeaponInfo>();

        public MultiplayerProjectile projectileRocket;
        public Grenade grenade;

		public PlayerClass PlayerClass;

        void Awake()
        {
            if (instance != null)
            {
                Destroy(transform.parent.gameObject);
                return;
            }
            instance = this;
        }

        public static float GetWeaponDamage(ServerWeaponInfo ServerWeaponInfo)
        {
            if (ServerWeaponInfo != null)
                return UnityEngine.Random.Range(ServerWeaponInfo.weaponDamage.minimumDamage, ServerWeaponInfo.weaponDamage.maximumDamage);
            else
                return 0;
        }

        public float GetWeaponDamage(int ServerWeaponInfo)
        {
            ServerWeaponInfo swi = GetServerWeapon(ServerWeaponInfo);
            if (swi != null)
                return UnityEngine.Random.Range(swi.weaponDamage.minimumDamage, swi.weaponDamage.maximumDamage);
            else
                return 0;
        }

        public static ServerWeaponInfo GetServerWeapon(string weapon)
        {
            if (!instance)
                return null;

            int c = instance.allWeapons.Count;
            for (int x = 0; x < c; x++)
            {
                if (instance.allWeapons[x].weaponName.Equals(weapon, System.StringComparison.OrdinalIgnoreCase))
                    return instance.allWeapons[x];
            }

            return null;
        }

        public static ServerWeaponInfo GetServerWeapon(int ID)
        {
            if (!instance)
                return null;

            int c = instance.allWeapons.Count;
            for (int x = 0; x < c; x++)
            {
                if (instance.allWeapons[x].weaponID == ID)
                    return instance.allWeapons[x];
            }

            return null;
        }

        public enum WeaponSpot
        {
            None,
            Primary,
            Secondary
        }
    }

    [System.Serializable]
    public class ServerWeaponInfo
    {
        public string weaponName;
        public int weaponID;

        public WeaponDamage weaponDamage = new WeaponDamage();
		public WeaponAttachment[] weaponAttachments;

        [System.Serializable]
        public class WeaponDamage
        {
            public float minimumDamage = 10;
            public float maximumDamage = 20;

            public float minimumRange = 25;
            public float maximumRange = 150;

            public float GetDamage(float distance)
            {
                return Math.GetValueOverDistance(UnityEngine.Random.Range(minimumDamage, maximumDamage), distance, minimumRange, maximumRange);
            }
        }
		[System.Serializable]
		public class WeaponAttachment
		{
            public string name;
            public bool enabled = false;
			public GameObject[] parts;
			public TransformInfo[] partInfo;

			[System.Serializable]
			public class TransformInfo 
            {
				public Vector3 position;
				public Vector3 rotation;
				public Vector3 scale;
			}

            public enum type
            {
                Scope,
                Bolt,
                Stock,
                Mag
            }
		}
    }

    [System.Serializable]
    public class PlayerWeaponInfo
    {
        public string weaponName = "";
        public int serverWeaponID = -1;

        public Transform transform;
        public Transform transformBaseGun;
        [HideInInspector] public GameObject gameObject;
        [HideInInspector] public Animator animation;
        [HideInInspector] public Transform attachementHolder;
        [HideInInspector] public Transform projectileSpawnpoint;
        [HideInInspector] public Transform shellSpawnpoint;

        [HideInInspector] public ParticleEffect muzzleFlash;
        [HideInInspector] public ParticleEffect muzzleFlashSilenced;
        [HideInInspector] public ParticleEffect shellSmoke;
        [HideInInspector] public ParticleEffect barrelSmoke;

        public FireSettings fireSettings = new FireSettings();
        public AimSettings aimSettings = new AimSettings();
        public SwaySettings swaySettings = new SwaySettings();
        public AnimationVariables animationVariables = new AnimationVariables();
        public SoundSettings soundSettings = new SoundSettings();

        public bool isWeaponFull()
        {
            return fireSettings.currentAmmoMagazine == fireSettings.maxAmmoMagazine;
        }
        public bool canReload()
        {
            return !isWeaponFull() && fireSettings.currentRestAmmo > 0;
        }

        public void InitializeWeapon()
        {
            //Get and set the server ID
            if (CustomizationManager.instance)
            {
                ServerWeaponInfo swi = CustomizationManager.GetServerWeapon(weaponName);
                if (swi != null)
                    serverWeaponID = swi.weaponID;
            }

            gameObject = transform.gameObject;
            animation = transform.GetComponent<Animator>();
            attachementHolder = transformBaseGun.Find("attachementHolder");
            projectileSpawnpoint = transformBaseGun.Find("projectileSpawnpoint");
            shellSpawnpoint = transformBaseGun.Find("shellSpawnpoint");

            muzzleFlash = GetParticleEffectInChild("muzzleFlash");
            muzzleFlashSilenced = GetParticleEffectInChild("muzzleFlashSilenced");
            shellSmoke = GetParticleEffectInChild("shellSmoke");
            barrelSmoke = GetParticleEffectInChild("barrelSmoke");

            fireSettings.currentAmmoMagazine = fireSettings.maxAmmoMagazine;
        }

        ParticleEffect GetParticleEffectInChild(string name)
        {
            Transform tar = transformBaseGun.Find(name);
            if (tar != null)
                return tar.GetComponent<ParticleEffect>();
            else
                return null;
        }

        [System.Serializable]
        public class FireSettings
        {
            public float fireRate = 0.1f;
            public float sprayRate = 1;

            public bool hasRoundInChamber = true;
            public int currentAmmoMagazine = 30;
            public int maxAmmoMagazine = 30;

            public int currentRestAmmo = 210;
            public int maxRestAmmo = 210;

            public FiringMode[] allAvailableFiringModes = new FiringMode[] { FiringMode.Automatic };
            public FiringType firingType = FiringType.Bullet;

            public RecoilSettings recoilAim = new RecoilSettings();
            public RecoilSettings recoilHip = new RecoilSettings();

            [System.Serializable]
            public class RecoilSettings
            {
                public Vector3 positionRecoilValues;
                public Vector3 positionRecoilMaximum;
                public Vector3 positionRecoilSoftSpeed;
                public Vector3 positionRecoilHardSpeed;
                public Vector3 positionRecoilSoftSpeedIdle;
                public Vector3 positionRecoilHardSpeedIdle;

                public Vector3 rotationRecoilValues;
                public Vector3 rotationRecoilSoftSpeed;
                public Vector3 rotationRecoilHardSpeed;
                public Vector3 rotationRecoilSoftSpeedIdle;
                public Vector3 rotationRecoilHardSpeedIdle;

                public Vector3 cameraRecoilMaximum;
                public Vector3 cameraRecoilValues;
                public Vector3 cameraRecoilRandomValues;
                public Vector3 cameraRecoilSoftSpeed;
                public Vector3 cameraRecoilHardSpeed;
                public Vector3 cameraRecoilSoftSpeedIdle;
                public Vector3 cameraRecoilHardSpeedIdle;
            }
        }

        [System.Serializable]
        public class AimSettings
        {
            public Vector3 pivotPosition;
            public Vector3 aimPosition;
            public Vector3 idlePosition;
            public float aimedFieldOfView = 50;

            public float weaponFieldOfViewPercentageIdle = 1;
            public float weaponFieldOfViewPercentageAim = 1;

            public Vector3 weaponRotation;
        }

        [System.Serializable]
        public class AnimationVariables
        {
            public bool hasReload;
            public float reloadLength;
            public bool hasReloadEmpty;
            public float reloadEmptyLength;

            public bool hasTakeout;
            public float takeoutLength;
            public bool hasDropdown;
            public float dropdownLength;

            public bool hasChamberBolt;
            public float chamberBoltLength;

            public bool hasFire;
            public bool hasFireEmpty;
            public bool playFireWhenAiming;

            public string runAnimation = "rifle run";

            public bool hasGrenadeThrowAnimation;
            public float grenadeThrowLengthInstantiate = 0.5f;
            public float grenadeThrowLengthEnd = 0.5f;
        }

        [System.Serializable]
        public class SoundSettings
        {
            public AudioClip audioShootClip;
            public SoundItem[] weaponPickup;
            public SoundItem[] weaponDrop;
            public SoundItem[] reload;
            public SoundItem[] reloadEmpty;
        }

        [System.Serializable]
        public class SwaySettings
        {
            public SettingsPerAim swayWhileAiming;
            public SettingsPerAim swayWhileSpraying;

            [System.Serializable]
            public class SettingsPerAim
            {
                public Vector3 positionFactor;
                public Vector3 postionSpeed;
                public Vector3 positionClamp;
                public Vector3 rotationFactor;
                public Vector3 rotationSpeed;
                public Vector3 rotationClamp;
            }
        }

        [System.Serializable]
        public class AttachementSettings
        {
            [System.Serializable]
            public class Attachement 
            {
                public GameObject gameObject;

                public Vector3 aimPosition;
            }

            public enum AttachementEnableState { Disabled, Enabled, EnabledTurnedOff }
            public enum AttachementAimType { Normal, Scoped, Additive }
        }
    }

    public enum FiringMode
    {
        Automatic,
        SemiAutomatic,
        BoltAction
    }

    public enum FiringType
    {
        Bullet,
        Rocket,
        Grenade
    }
}


[System.Serializable]
public enum PlayerClass {
	Assault,
	Engineer,
	Support,
	Marksman,
    Editor
}