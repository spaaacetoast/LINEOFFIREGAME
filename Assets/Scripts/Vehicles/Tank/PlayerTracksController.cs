using UnityEngine;
using System.Collections;
using AngryRain;

public class PlayerTracksController : TankTracksController 
{
	public float steerG = 0.0f;
	public float accelG = 0.0f;

    private new void Update()
    {
        base.Update();
        if (vehicleSeats[0].clientPlayer != null && vehicleSeats[0].clientPlayer.isMe)
        {
            Vector2 input = InputManager.GetInputDirection(LocalPlayerManager.localPlayers[vehicleSeats[0].clientPlayer.lPlayerIndex]);
            accelG = input.y;
            steerG = input.x;
        }
    }

    private void FixedUpdate()
    {
		//float accelerate = 0;
		//float steer = 0;
		UpdateWheels(accelG,steerG);
	}
}
