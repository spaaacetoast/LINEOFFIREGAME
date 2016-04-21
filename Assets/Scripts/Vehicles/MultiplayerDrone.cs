using UnityEngine;
using System.Collections;

public class MultiplayerDrone : MonoBehaviour 
{
    public AngryRain.Vehicle.Propellor[] allPropellors;

    void Update()
    {
        int c = allPropellors.Length;
        for(int i = 0; i < c; i++)
        {
            allPropellors[i].thisTransform.Rotate(allPropellors[i].turnSpeed);
        }
    }
}
