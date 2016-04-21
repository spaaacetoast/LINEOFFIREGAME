using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AngryRain.Multiplayer;

namespace AngryRain
{
    public class PlayerCamera : MonoBehaviour
    {
        #region Public Variables

        public static List<PlayerCamera> allPlayerCameras = new List<PlayerCamera>();

        public CameraSettings cameraSettings = new CameraSettings();
        public EffectsSettings effectsSettings = new EffectsSettings();

        [System.Serializable]
        public class CameraSettings
        {
            public Transform xRotationTransform;
            public Transform yRotationTransform;
            public float mouseSensitivity = 5;
            public float cameraFieldOfViewToSensitivityDamper = 11;

            public bool enableControls;

            public CameraType cameraType = CameraType.LevelEditor;
            public bool isCameraInVehicle; //This will make the camera effect the X and Y axis on the xRotationTransform
        }

        //Hidden Variables, Overriden Variables
        public new GameObject gameObject { private set; get; }
        public new Transform transform { private set; get; }
        public new Camera camera { private set; get; }
        public PlayerController playerController { set; get; }

        //Transform Variables
        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public Quaternion rotation;
        [HideInInspector]
        public Vector3 eulerAngles;
        [HideInInspector]
        public Vector3 localEulerAngles;
        [HideInInspector]
        public Vector3 forward;
        [HideInInspector]
        public Vector3 angularVelocity;
        [HideInInspector]
        private Vector3 lastRotation;

        [HideInInspector]
        public Vector2 newInput = new Vector2();
        public Vector3 currentRotation;

        private Transform startingTransform;

        public RaycastHit[] allRaycastHits;

        #endregion

        #region Initialization

        bool isInitialized;
        public void Initialize()
        {
            if (!isInitialized)
            {
                isInitialized = true;

                transform = GetComponent<Transform>();
                gameObject = transform.gameObject;
                camera = transform.GetComponent<Camera>();
                allPlayerCameras.Add(this);

                startingTransform = transform.parent;
            }
        }

        #endregion

        #region MonoBehaviours

        void Awake()
        {
            Initialize();
        }

        void Start()
        {
            SetUpAllCameras();

            OptionManager.ApplyOptions(OptionManager.currentOptions);
        }

        void OnEnable()
        {
            camera.fieldOfView = OptionManager.currentOptions.fieldOfView;
            OptionManager.ApplyOptions(OptionManager.currentOptions);
        }

        void Update()
        {
            UpdateRotation();

            position = transform.position;
            rotation = transform.rotation;
            eulerAngles = rotation.eulerAngles;
            localEulerAngles = transform.localEulerAngles;
            forward = transform.forward;
            angularVelocity = Math.Vector3CorrectRotation(eulerAngles - lastRotation);
            lastRotation = eulerAngles;
        }

        void LateUpdate()
        {
            rotation = transform.rotation;
        }

        void FixedUpdate()
        {
            allRaycastHits = Physics.RaycastAll(position, forward, 5);
        }

        void OnPreCull()
        {
            if (playerController == null) return;
            if (cameraSettings.cameraType != CameraType.Player) return;

            for (int i = 0; i < LocalPlayerManager.localPlayers.Count; i++)
            {
                ClientPlayer cPlayer = LocalPlayerManager.localPlayers[i].clientPlayer;
                PlayerManager pManager = cPlayer.playerManager;
                if (!cPlayer.isAlive) continue;
                bool isMe = pManager.clientPlayer.mPlayerID == cPlayer.mPlayerID;
                cPlayer.playerManager.SetCharacterRenderingForSplitscreen(isMe);
            }
        }

        void OnDestroy()
        {
            allPlayerCameras.Remove(this);
        }

        #endregion

        #region Camera Enabling/Resetting

        public void EnableCamera(CameraResetType resetTransform, bool enableControl)//Enabling default player camera
        {
            Initialize();
            EnableCamera(startingTransform, resetTransform, enableControl, true, CameraType.Player);
        }

        public void EnableCamera(Transform parent, CameraResetType resetTransform, bool enableControl, bool enableChilds, CameraType type)
        {
            Initialize();
            transform.parent = parent;
            EnableCamera(resetTransform, enableControl, enableChilds, type);
        }

        public void EnableCamera(CameraResetType resetTransform, bool enableControl, bool enableChilds, CameraType type)
        {
            DisableAllCameras();

            gameObject.SetActive(true);

            if (resetTransform == CameraResetType.Position || resetTransform == CameraResetType.PositionAndRotation)
            {
                transform.localPosition = Vector3.zero;
            }

            if (resetTransform == CameraResetType.Rotation || resetTransform == CameraResetType.PositionAndRotation)
            {
                transform.localRotation = Quaternion.identity;
                currentRotation.x = 0;
            }
            else
                transform.eulerAngles = currentRotation;

            cameraSettings.enableControls = enableControl;
            playerController.playerMovement.rigidbody.useGravity = type != CameraType.LevelEditor;

            if (type == CameraType.LevelEditor)
            {
                playerController.SetStance(PlayerStance.Editor);
                playerController.playerManager.Local_SetStance(PlayerStance.Editor);
            }

            if (cameraSettings.cameraType != type && type == CameraType.Player)
            {
                playerController.SetStance(PlayerStance.Standing);
                playerController.playerManager.Local_SetStance(PlayerStance.Standing);
            }

            cameraSettings.cameraType = type;
        }

        public void ResetCameraEffects()//Reset all effects applied to the camera
        {
            currentRotation = Vector3.zero;
            StopCoroutine("HandleImageEffects");
        }

        #endregion

        private void UpdateRotation()
        {
            if (cameraSettings.enableControls && !Cursor.visible)
            {
                newInput.x = playerController.input.GetAxis("Look Vertical");
                newInput.y = playerController.input.GetAxis("Look Horizontal");
                newInput *= Time.timeScale;
            }
            else
            {
                newInput.x = 0;
                newInput.y = 0;
            }

            //tempRotation.x = Mathf.Clamp(tempRotation.x - newInput.x * cameraSettings.mouseSensitivity, -80, 80);
            //tempRotation.y = tempRotation.y + newInput.y * cameraSettings.mouseSensitivity;

            switch (cameraSettings.cameraType)
            {
                case CameraType.Player:
                    currentRotation.x = Mathf.Clamp(currentRotation.x - (newInput.x * cameraSettings.mouseSensitivity * (camera.fieldOfView / cameraSettings.cameraFieldOfViewToSensitivityDamper)), -80, 80);
                    currentRotation.y = currentRotation.y + (newInput.y * cameraSettings.mouseSensitivity * (camera.fieldOfView / cameraSettings.cameraFieldOfViewToSensitivityDamper));

                    if (cameraSettings.isCameraInVehicle)
                    {
                        Quaternion xRot = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
                        cameraSettings.xRotationTransform.localRotation = xRot;
                    }
                    else
                    {
                        Quaternion xRot = Quaternion.Euler(currentRotation.x, currentRotation.y - playerController.playerMovement.transform.eulerAngles.y, 0);
                        Quaternion yRot = Quaternion.Euler(0, currentRotation.y, 0);
                        cameraSettings.xRotationTransform.localRotation = xRot;
                        //cameraSettings.yRotationTransform.localRotation = yRot;
                        playerController.playerMovement.rigidbody.MoveRotation(yRot);
                    }

                    currentRotation += GetCurrentShakeRotation();
                    break;
                case CameraType.LevelEditor:
                    transform.localEulerAngles = Vector3.zero;
                    transform.localPosition = Vector3.zero;

                    if (Input.GetMouseButton(1))
                    {
                        currentRotation.x = Mathf.Clamp(currentRotation.x - (newInput.x * cameraSettings.mouseSensitivity * (camera.fieldOfView / cameraSettings.cameraFieldOfViewToSensitivityDamper)), -80, 80);
                        currentRotation.y = currentRotation.y + (newInput.y * cameraSettings.mouseSensitivity * (camera.fieldOfView / cameraSettings.cameraFieldOfViewToSensitivityDamper));
                        playerController.transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
                    }
                    else if (Input.GetMouseButton(2))
                        playerController.transform.Translate(InputManager.GetAxis(playerController.playerManager.clientPlayer.lPlayerIndex) * Time.deltaTime * 25);

                    playerController.transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetKey(KeyCode.Space) ? 1 : Input.GetKey(KeyCode.LeftControl) ? -1 : 0, Input.GetAxisRaw("Vertical")) * Time.deltaTime * (Input.GetKey(KeyCode.LeftShift) ? 20 : Input.GetKey(KeyCode.Z) ? 5 : 10));
                    break;
            }
        }

        public void SetRotation(Vector3 rot)
        {
            currentRotation = rot;
        }

        /// <summary>
        /// Focus the camera with its current rotation on a position on a certain distance
        /// </summary>
        /// <param name="position"></param>
        /// <param name="distance"></param>
        public void FocusCameraOnPoint(Vector3 position, float distance)
        {
            if (cameraSettings.cameraType != CameraType.LevelEditor)
                return;

            //Vector3 vec = Vector3.Project(Vector3.back * distance, thisTransform.forward);
            Vector3 vec = rotation * new Vector3(0, 0, -distance);
            vec += position;

            Debug.Log(position + ", " + distance + ", " + vec);

            playerController.transform.position = vec;
        }

        #region Shake Camera

        Vector3 GetCurrentShakeRotation()
        {
            Vector3 final = new Vector3();



            return final;
        }

        #endregion

        #region static functions

        public static void AddExplosionEffect(float explosionRange, Vector3 explosionPosition, float explosionStrength)
        {
            int c = allPlayerCameras.Count;
            for (int i = 0; i < c; i++)
            {
                /*float distance = Vector3.Distance(explosionPosition, allPlayerCameras[i].thisPosition);
                if (distance < explosionRange)
                {
                    float e = Mathf.Min((explosionRange - distance) / explosionRange * explosionStrength, explosionStrength);
                    allPlayerCameras[i].StartCameraShake(e, new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), 0));
                }*/
            }
        }

        public static void SetUpAllCameras()
        {
            if (!Multiplayer.MultiplayerManager.instance)
                return;

            //Disable all cameras when a new camera is created
            /*foreach (Camera camera in Camera.allCameras)
                camera.gameObject.SetActive(false);*/
            DisableAllCameras();

            //Enable all player cameras
            foreach (LocalPlayer player in LocalPlayerManager.localPlayers)
                player.playerCamera.gameObject.SetActive(true);

            int playerCount = LocalPlayerManager.localPlayers.size;
            if (playerCount == 1)
            {
                Camera cam = LocalPlayerManager.localPlayers[0].playerCamera.camera;
                cam.rect = new Rect(0,0,1,1);
            }
            else if (playerCount == 2)
            {
                Camera cam = LocalPlayerManager.localPlayers[0].playerCamera.camera;
                cam.rect = new Rect(0, 0.5f, 1, 0.5f);
                cam = LocalPlayerManager.localPlayers[1].playerCamera.camera;
                cam.rect = new Rect(0, 0, 1, 0.5f);
            }
            else if (playerCount == 3)
            {
                Camera cam = LocalPlayerManager.localPlayers[0].playerCamera.camera;
                cam.rect = new Rect(0, 0.5f, 1, 0.5f);
                cam = LocalPlayerManager.localPlayers[1].playerCamera.camera;
                cam.rect = new Rect(0, 0, 0.5f, 0.5f);
                cam = LocalPlayerManager.localPlayers[2].playerCamera.camera;
                cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            }
            else if (playerCount == 4)
            {
                Camera cam = LocalPlayerManager.localPlayers[0].playerCamera.camera;
                cam.rect = new Rect(0, 0, 0.5f, 0.5f);
                cam = LocalPlayerManager.localPlayers[1].playerCamera.camera;
                cam.rect = new Rect(0.5f, 0, 0.5f, 0.5f);
                cam = LocalPlayerManager.localPlayers[2].playerCamera.camera;
                cam.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                cam = LocalPlayerManager.localPlayers[3].playerCamera.camera;
                cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        public static void DisableAllCameras()
        {
            for (int i = 0; i < Camera.allCamerasCount; i++)
                if (!Camera.allCameras[i].CompareTag("GUI Camera") && !Camera.allCameras[i].GetComponent<PlayerCamera>())
                    Camera.allCameras[i].gameObject.SetActive(false);
        }

        #endregion

        #region Effects

        private Vector3 getHitHard;
        [HideInInspector]
        public Vector3 getHitSoft;

        private float startTime;

        public void ActivateGetHitEffect()
        {

        }

        [System.Serializable]
        public class EffectsSettings
        {
            public ShakeSettings microShake = new ShakeSettings(
                new Vector3(0, 1, 2),
                new Vector3(3, 4, 5),
                new Vector3(2, 2, 2),
                new Vector3(7.5f, 7.5f, 7.5f),
                new Vector3(0, 0, 0),
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.1f, 1), new Keyframe(3, 0)));

            public ShakeSettings smallShake = new ShakeSettings(
                new Vector3(0, 1, 2),
                new Vector3(3, 4, 5),
                new Vector3(10, 5, 5),
                new Vector3(2.5f, 3, 5),
                new Vector3(0, 0, 0),
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.1f, 1), new Keyframe(3, 0)));

            public ShakeSettings bigShake = new ShakeSettings(
                new Vector3(0, 1, 2),
                new Vector3(3, 4, 5),
                new Vector3(50,50,50),
                new Vector3(5,3,2),
                new Vector3(5,3,2),
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.1f, 1), new Keyframe(1, 0)));
        }

        #endregion
    }

    public enum CameraType
    {
        Player,
        LevelEditor,
        None
    }

    public enum CameraResetType
    {
        None,
        Position,
        Rotation,
        PositionAndRotation
    }
}