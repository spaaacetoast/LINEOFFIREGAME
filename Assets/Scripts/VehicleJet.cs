using UnityEngine;
using System.Collections;
using TNet;

namespace AngryRain
{
    public class VehicleJet : MultiplayerVehicle
    {
        private readonly VectorPid angularVelocityController = new VectorPid(33.7766f, 0, 0.2553191f);
        private readonly VectorPid headingController = new VectorPid(9.244681f, 0, 0.06382979f);

        public ObjectHolder objectHolder;

        public Vector3 thisPosition;

        public float forwardForce;
        public float maxForwardSpeed = 80;
        public Vector2 forwardForceClamp;

        public Vector2 gravityClamp;

        public float rotationForce;
        public float rotationMaxInput;
        public Vector3 rotationVelocityDamp;
        public Vector3 rotationMaxForce;

        public Vector2 velocityTransferClamp;

        public Vector2 rotationSpeedFactor;

        public float yawAdditionForRolling;

        public bool controlAI;

        public float inputVertical = 0;
        public Vector2 inputRot;

        public bool isFiring;
        public float lastFireTime;

        new void Start ()
        {
            base.Start ();
        }

        void FixedUpdate()
        {
            thisPosition = GetComponent<Rigidbody>().position;

            if (vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isMe)
            {
                Vector3 newRelVel = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
                relativeVelocity = newRelVel;
                relativeAngularVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().angularVelocity);
                velocityMagnitude = GetComponent<Rigidbody>().velocity.magnitude;

                if (!controlAI)
                {
                    inputVertical = Input.GetAxis("Vertical");
                    if (!Input.GetKey(KeyCode.LeftAlt))
                    {
                        inputRot = InputManager.GetAxis(vehicleSeats[0].clientPlayer.lPlayerIndex);
                        inputRot.x += Input.GetAxis("Horizontal");
                    }
                    else
                    {
                        inputRot.y = 0;
                        inputRot.x = 0;
                    }
                }
                else
                {
                    inputVertical = 0;
                    inputRot.y = 0;
                    inputRot.x = 0;
                }

                inputVertical = Mathf.Clamp(inputVertical, -1, 1);
                inputRot.y = Mathf.Clamp(inputRot.y, -1, 1);
                inputRot.x = Mathf.Clamp(inputRot.x, -1, 1);

                if (relativeVelocity.z < maxForwardSpeed)
                    GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * forwardForce * Mathf.Clamp(inputVertical, forwardForceClamp.x, forwardForceClamp.y), ForceMode.Acceleration);
                else
                    GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * forwardForce * Mathf.Clamp(inputVertical, forwardForceClamp.x, forwardForceClamp.y));

                Vector3 inputTorque = new Vector3(Mathf.Clamp(inputRot.y, -rotationMaxInput, rotationMaxInput), 0, Mathf.Clamp(-inputRot.x, -rotationMaxInput, rotationMaxInput)) * rotationForce;

                //Limit rotational speed when forwardspeed is lower than value
                float inputControlFactor = Mathf.Clamp((relativeVelocity.z - rotationSpeedFactor.x) / rotationSpeedFactor.y, 0, 1);
                inputTorque *= inputControlFactor;

                inputTorque = Math.Vector3Clamp(inputTorque, rotationMaxForce);

                inputTorque -= Math.Vector3Multiply(relativeAngularVelocity, rotationVelocityDamp);
                inputTorque = transform.TransformDirection(inputTorque);

                //Add rotation so it looks at the velocity for e.g stalling
                var desiredHeading = GetComponent<Rigidbody>().velocity;
                Debug.DrawRay(transform.position, desiredHeading, Color.magenta);

                var currentHeading = transform.forward;
                Debug.DrawRay(transform.position, currentHeading * 15, Color.blue);

                var headingError = Vector3.Cross(currentHeading, desiredHeading);
                var headingCorrection = headingController.Update(headingError, Time.deltaTime);
                headingCorrection = Math.Vector3Clamp(headingCorrection, rotationMaxForce);

                headingCorrection *= (1 - inputControlFactor)*0.25f;
                //Adding Yaw Rotation when airplane is giving more/less lift on one of the wings
                float dir = Math.CorrectRotation(relativeAngularVelocity.z);
                inputTorque.y += dir * yawAdditionForRolling;

                GetComponent<Rigidbody>().AddTorque(inputTorque + headingCorrection, ForceMode.Acceleration);

                float upGrav = Mathf.Clamp((relativeVelocity.z - gravityClamp.x) / gravityClamp.y, 0, 1);
                GetComponent<Rigidbody>().AddForce(Vector3.up * upGrav * (-Physics.gravity.y * 1.25f));

                float velTrans = Mathf.Clamp((relativeVelocity.z - velocityTransferClamp.x) / velocityTransferClamp.y, 0, 5);
                //print(velTrans);
                Vector3 noDragZ = relativeVelocity;
                noDragZ.z = 0;
                GetComponent<Rigidbody>().AddRelativeForce(-noDragZ * 2 * velTrans, ForceMode.Acceleration);
            }
        }

        void OnGUI()
        {
            if (Multiplayer.MultiplayerManager.showDebug == 3)
            {
                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("Relative Velocity", GUILayout.Width(125));
                GUILayout.Label(relativeVelocity.ToString());
                GUILayout.EndHorizontal();
            }
        }

        new void Update()
        {
            base.Update ();

            if (vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isMe)
            {
                bool newValue = InputManager.GetButton(InputName.Fire, LocalPlayerManager.localPlayers[0]);
                if (newValue != isFiring)
                    tno.Send("UpdateFiring", TNet.Target.Others, newValue);
                isFiring = newValue;
            }

            if (isFiring && Time.time > lastFireTime)
            {
                lastFireTime = Time.time + 0.05f;
                objectHolder.muzzleFlash.PlayParticleEffect();
                MultiplayerProjectile proj = PoolManager.CreateProjectile(objectHolder.projectile, objectHolder.bulletCreatePoint.position, objectHolder.bulletCreatePoint.rotation);
                proj.StartProjectile(null);
            }
        }

        void VehiclePlayerLeaving(VehicleSeat seat)
        {
            if (seat.seatIndex == 0)
                tno.Send("UpdateFiring", TNet.Target.All, false);
        }

        [RFC]
        protected void UpdateFiring(bool value)
        {
            isFiring = value;
        }

        [System.Serializable]
        public class ObjectHolder
        {
            public ParticleEffect muzzleFlash;
            public Transform bulletCreatePoint;
            public MultiplayerProjectile projectile;
        }
    }
}