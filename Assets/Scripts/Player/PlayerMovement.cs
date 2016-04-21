using UnityEngine;
using System.Collections;

namespace AngryRain
{
    public class PlayerMovement : MonoBehaviour
    {
        public Settings settings = new Settings();

        public new Rigidbody rigidbody { private set; get; }
        private new GameObject gameObject;
        private new Transform transform;
        private PlayerController playerController;
        private CapsuleCollider capsule;      

        public Vector3 velocity;
        [HideInInspector]
        public float velocityMagnitude;
        [HideInInspector]
        public Vector3 relativeVelocity;

        private Vector3 position;
        private Quaternion rotation;

        public bool isGrounded = true;
        public bool isRunning;
        public bool isBoosting;

        public bool shouldUpdateMovement = true;
        public bool canMove;
        public bool canJump;

        [HideInInspector]
        public Vector3 inputDir = Vector3.zero;
        public Vector3 groundContactNormal = Vector3.zero;
        Vector3 velocityChange = Vector3.zero;

        bool            lastFrameGrounded;
        private Vector3 lastFrameVelocity;
        private bool shouldRun;

        public void Initialize()
        {
            transform = GetComponent<Transform>();
            gameObject = transform.gameObject;
            rigidbody = transform.GetComponent<Rigidbody>();
            playerController = GetComponent<PlayerController>();
            capsule = GetComponent<CapsuleCollider>();
        }

        void OnEnable()
        {
            CheckIfGrounded();
            playerController.playerManager.SetGrounded(isGrounded);
            playerController.animationSettings.weaponholderAnimation.SetBool("isGrounded", isGrounded);
        }

        #region Movement

        public void FixedUpdate()
        {
            velocity = rigidbody.velocity;
            position = transform.localPosition;
            rotation = transform.localRotation;
            velocityMagnitude = velocity.magnitude;
            relativeVelocity = transform.InverseTransformDirection(velocity);

            CheckIfGrounded();
            StickToGroundHelper();
            if (shouldUpdateMovement)
                UpdateMovementValues();
            if (playerController.playerVariables.playerStance == PlayerStance.Standing && isGrounded)
            {
                if (playerController.input.GetButtonDown("Jump"))
                {
                    playerController.animationSettings.weaponholderAnimation.Play("fpsjump", 1);
                    rigidbody.AddForce(Vector3.up * settings.jumpSpeed, ForceMode.VelocityChange);
                }
            }

            if (lastFrameGrounded != isGrounded)
            {
                if(isGrounded)
                playerController.animationSettings.weaponholderAnimation.Play("fpsland", 1);

                playerController.animationSettings.weaponholderAnimation.SetBool("isGrounded", isGrounded);
            }
            lastFrameGrounded = isGrounded;
            lastFrameVelocity = rigidbody.velocity;
        }

        private void CheckIfGrounded()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position + Vector3.up, capsule.radius-0.01f, Vector3.down, out hitInfo, settings.groundCheckDistance + 1, settings.groundLayer) && Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 60)
            {
                if (!isGrounded)
                {
                    playerController.playerManager.SetGrounded(true);
                    if (lastFrameVelocity.y < -3)
                    {
                        playerController.SetAimDelay(0.5f);
                        playerController.SetFireDelay(0.5f);
                    }
                }

                isGrounded = true;
                groundContactNormal = hitInfo.normal;
                rigidbody.useGravity = false;
            }
            else
            {
                if (isGrounded)
                    playerController.playerManager.SetGrounded(false);

                isGrounded = false;
                groundContactNormal = Vector3.up;
                rigidbody.useGravity = true;
            }

            Debug.DrawRay(transform.position + Vector3.up, Vector3.down * (settings.groundCheckDistance + 1));
        }

        private void StickToGroundHelper()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position + Vector3.up, capsule.radius-0.01f, Vector3.down, out hitInfo, settings.groundCheckDistance + 1, settings.groundLayer))
            {
                if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 60)
                {
                    rigidbody.velocity = Vector3.ProjectOnPlane(rigidbody.velocity, hitInfo.normal);
                }
            }
        }

        private void UpdateMovementValues()
        {
            PlayerStance stance = playerController.playerVariables.playerStance;
            Vector3 input = canMove ? new Vector3(playerController.input.GetAxis("Move Horizontal"), 0, playerController.input.GetAxis("Move Vertical")) : Vector3.zero;
            if (isGrounded)
                UpdateMovementValues(input, stance == PlayerStance.Standing ? (isRunning ? settings.runSpeed : settings.walkSpeed) : settings.crouchSpeed, settings.maxVelocityChange);
            else
                UpdateMovementValues(input, 0, 0);

            //Small animation update
            Movement.WalkingState nextVal = isRunning ? Movement.WalkingState.Running : (velocityMagnitude > 0.15f ? Movement.WalkingState.Walking : Movement.WalkingState.Idle);
            if (playerController.playerVariables.walkingState != nextVal)
            {
                playerController.playerVariables.walkingState = nextVal;
                if (playerController.animationSettings.weaponholderAnimation)
                    playerController.animationSettings.weaponholderAnimation.SetBool("isWalking", nextVal != Movement.WalkingState.Idle);
            }
        }

        public void UpdateMovementValues(Vector3 input, float maxSpeed, float maxVelocityChange)
        {
            inputDir = input;

            if (!isRunning && !shouldRun)
            {
                float sprintPressed = playerController.input.GetButtonTimePressed("Sprint");
                shouldRun = sprintPressed != 0f && sprintPressed < 1f;
            }
            else if (shouldRun)
            {
                isRunning = true;
                shouldRun = false;
            }
            else if (isRunning)
            {
                if (inputDir.z < 0.9f || playerController.input.GetButtonDown("Sprint") || playerController.input.GetButtonDown("Fire"))
                    isRunning = false;
            }

            Vector3 dir = Vector3.ClampMagnitude(inputDir, 1);
            dir = rotation * dir;

            if (playerController.playerVariables.isAiming) dir *= settings.aimSpeedReduction;

            PlayerStance stance = playerController.playerVariables.playerStance;
            dir *= maxSpeed;

            float targetMagnitude = dir.magnitude;//Take the current magnitude so that when ProjectOnPlane has a different magnitude, normalize it and multiply

            dir = Vector3.ProjectOnPlane(dir, groundContactNormal);//Make the input follow the angle of the plane we are standing on

            if (targetMagnitude > 2f)//Only normalize and multiply when bigger than X amount because of inprecision
                dir = dir.normalized * targetMagnitude;

            dir -= rigidbody.velocity;
            dir.y = 0;

            dir = Vector3.ClampMagnitude(dir, maxVelocityChange);
            rigidbody.AddForce(dir, ForceMode.VelocityChange);

            //Small animation update
            Movement.WalkingState nextVal = isRunning ? Movement.WalkingState.Running : (velocityMagnitude > 0.15f ? Movement.WalkingState.Walking : Movement.WalkingState.Idle);
            if (playerController.playerVariables.walkingState != nextVal)
            {
                playerController.playerVariables.walkingState = nextVal;
                if (playerController.animationSettings.weaponholderAnimation)
                    playerController.animationSettings.weaponholderAnimation.SetBool("isWalking", nextVal != Movement.WalkingState.Idle);
            }
        }

        public void ResetMovement()
        {
            inputDir = Vector3.zero;
            velocityChange = Vector3.zero;
            if (!rigidbody.isKinematic)
                rigidbody.velocity = Vector3.zero;
        }

        #endregion

        #region help/setting classes

        [System.Serializable]
        public class Settings
        {
            public float walkSpeed = 3;
            public float crouchSpeed = 2;
            public float runSpeed = 6f;

            public float aimSpeedReduction = 0.4f;
            public float maxVelocityChange = 60;

            public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            public float stickToGroundHelperDistance = 0.5f; // stops the character

            public LayerMask groundLayer; // The capsule collider for the first person character

            public float jumpSpeed = 1;
        }

        #endregion
    }
}