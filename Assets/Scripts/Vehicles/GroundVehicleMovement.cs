using UnityEngine;
using System.Collections;
using AngryRain;
using TNet;
using UnityStandardAssets.Vehicles.Car;

public class GroundVehicleMovement : MultiplayerVehicle
{
    private CarController carController;

    private float motor;
    private float steering;
    private float lastSyncTime;

    void Awake()
    {
        carController = GetComponent<CarController>();
    }

    public void FixedUpdate()
    {
        if (vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isMe)
        {
        }
        carController.Move(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Vertical"), 0);
    }

    [RFC]
    void SyncWheelInfo(float motor, float steering)
    {
        this.motor = motor;
        this.steering = steering;
    }
}
