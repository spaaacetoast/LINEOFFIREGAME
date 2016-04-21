using UnityEngine;
using System.Collections;

namespace AngryRain.Vehicle
{
    public class HelicopterController : MultiplayerVehicle
    {
		public Vector2 speed;
		public Vector3 rotationSpeed;
		public Vector3 rotationBalanceSpeed;
		
		public Vector3 drag;
		
		public Vector3 currentSpeed;
		public Vector3 currentRotation;
		public Vector3 localVelocity;
		
		public Transform TopRotors;
		public Transform SideRotors;
		public bool broken;
		public float brokenTime;
		// Update is called once per frame
		void FixedUpdate () {
			/*if(Input.GetButtonDown ("Fire1")){
				brokenTime = Time.time;
				broken = true;
			}*/
			if (vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isMe)
			{
				if(Input.GetAxis("Vertical") != 0){
					currentSpeed.y = Input.GetAxis("Vertical") * speed.y + (-Physics.gravity.y * GetComponent<Rigidbody>().mass);
				}
				else{
					currentSpeed.y = (GetComponent<Rigidbody>().mass * -Physics.gravity.y) - 50;
				}
				if(Input.GetAxis("Horizontal") != 0){
					currentRotation.y = Input.GetAxis("Horizontal") * rotationSpeed.y;
				}
				if(Input.GetAxis("Mouse X") != 0){
					currentRotation.z = (-Input.GetAxis("Mouse X")) * rotationSpeed.z;
				}
				else{
					/*	if((transform.localEulerAngles.z < 355 && transform.localEulerAngles.z > 180) || (transform.localEulerAngles.z > 3 && transform.localEulerAngles.z < 180)){
					if(transform.localEulerAngles.z < 355 && transform.localEulerAngles.z > 180){
						currentRotation.z = rotationBalanceSpeed.z;
					}
					if(transform.localEulerAngles.z > 3 && transform.localEulerAngles.z < 180){
						currentRotation.z = -rotationBalanceSpeed.z;
					}
				}
				else{
					currentRotation.z = 0;
				}*/
				}
				if(Input.GetAxis("Mouse Y") != 0){
					currentRotation.x = Input.GetAxis("Mouse Y") * rotationSpeed.x;
				}
				if(broken){
					GetComponent<Rigidbody>().AddForce(new Vector3(0, GetComponent<Rigidbody>().mass * Physics.gravity.y * ((Time.time - brokenTime) * 0.5f), 0));
					GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(((GetComponent<Rigidbody>().mass * -Physics.gravity.y) / 2) * Time.deltaTime * ((Time.time - brokenTime) * 0.05f), ((GetComponent<Rigidbody>().mass * -Physics.gravity.y) / 2) * Time.deltaTime * ((Time.time - brokenTime) * 0.1f), 0) , ForceMode.Acceleration);
				}
				SideRotors.Rotate (currentRotation.y * Time.deltaTime, 0, 0);
				TopRotors.Rotate (0, currentSpeed.y * Time.deltaTime, 0);
				localVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
				Vector3 newDrag = new Vector3(localVelocity.x * drag.x, localVelocity.y * drag.y, localVelocity.z * drag.z);
				newDrag *= GetComponent<Rigidbody>().mass;
				GetComponent<Rigidbody>().AddRelativeTorque(currentRotation.x, currentRotation.y, currentRotation.z);
				GetComponent<Rigidbody>().AddForce(transform.TransformDirection(currentSpeed) - transform.TransformDirection(newDrag));
			}
		}

        void Accelerate()
        {
            /*speedSettings.direction.y += Mathf.Clamp(Input.GetAxis("Vertical"), -1, 1) * (speedSettings.Acceleration * Time.deltaTime);
            if (Input.GetAxisRaw("Vertical") == 0) speedSettings.direction.y += speedSettings.brakeSpeed * Time.deltaTime * Mathf.Clamp(-relativeVelocity.y, -1, 1);*/

            /*if (Input.GetAxis("Vertical") != 0)
            {
                speedSettings.direction.y += Input.GetAxis("Vertical") * (speedSettings.Acceleration * Time.deltaTime);
                //speedSettings.direction.y = Input.GetKey("w") ? speedSettings.direction.y + (speedSettings.Acceleration * Time.deltaTime) : speedSettings.direction.y - (speedSettings.brakeSpeed * Time.deltaTime);
            }
            else
            {
                //speed.direction.y = (-(Physics.gravity.y) * this.rigidbody.mass);
                speedSettings.direction.y = speedSettings.direction.y > 0 ? speedSettings.direction.y - (speedSettings.brakeSpeed * Time.deltaTime) : (speedSettings.direction.y < 0 ? speedSettings.direction.y + (speedSettings.brakeSpeed * Time.deltaTime) : 0.0f);
            }*/

            /*speedSettings.rotationVelocity.z = Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -1, 1) * speedSettings.rotationPower.z;
            speedSettings.rotationVelocity.x = Mathf.Clamp(Input.GetAxisRaw("Mouse Y"), -1, 1) * speedSettings.rotationPower.x;
            speedSettings.rotationVelocity.y = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1, 1) * speedSettings.rotationPower.y;*/
        }

        float Clamp(float vector, float min, float max)
        {
            vector = vector > max ? max : (vector < min ? min : vector);
            return vector;
        }

        [System.Serializable]
        public class SpeedSettings
        {
            public float acceleration;
            public float brakeSpeed;
            [Range(9.8f, 15)]
            public float maxSpeed;
            [Range(0, 9.8f)]
            public float neutralSpeed;//Keep this slighly below 9.8

            public float steeringMinSpeed;//At what speed should we start smoothing in the controll
            public float steeringMaxControlSpeed;//At wich speed should we give full controll
            public Vector3 angularForce;

            public float currentSpeed;

            public Vector3 relativeDrag;
            public Vector2 relativeDragSpeedRelation;//X is used for minimum speed, Y is for the damping
            public Vector3 relativeAngularDrag;

            public bool enginesEnabled;

            /*public float Acceleration;
            public float brakeSpeed;
            public Vector3 rotationPower;
            public Vector3 direction;
            public Vector3 rotationVelocity;

            public float maxSpeed;
            public float curMinSpeed;

            public float EngineOffMinSpeed;
            public float EngineStartedMinSpeed;

            public float propellorTurnSpeed;*/
        }
    }

    [System.Serializable]
    public class Propellor
    {
        public Transform thisTransform;
        public Vector3 turnSpeed;
    }
}