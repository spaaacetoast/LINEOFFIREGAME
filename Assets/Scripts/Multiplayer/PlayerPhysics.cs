using UnityEngine;
using System.Collections;
using AngryRain.Multiplayer;

public class PlayerPhysics : MonoBehaviour 
{
    //Public Variables
    public PlayerManager playerManager;
    public new Transform transform { get; private set; }
    public new GameObject gameObject { get; private set; }
    public new Rigidbody rigidbody { get; private set; }
    public new Collider collider { get; private set; }

    //Private Variables

    public void Initialize()
    {
        transform = GetComponent<Transform>();
        gameObject = transform.gameObject;
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
    }
}
