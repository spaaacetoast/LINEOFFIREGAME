using UnityEngine;
using System.Collections;

public class CharacterDemo : MonoBehaviour 
{
    public string startAnimation;

    public bool enableLookDirection = true;
    public Vector2 lookDirection;

    public float lookatWheight;
    public float lookatBodyWheight;
    public float lookatHeadWheight;
    public Transform lookAtTarget;

    public Vector3 movementSpeed;
    public int stance;

    void Start()
    {
        GetComponent<Animator>().Play(startAnimation);
    }

    void Update()
    {
        GetComponent<Animator>().SetLayerWeight(2, enableLookDirection ? 1 : 0);
        GetComponent<Animator>().SetFloat("LookHorizontal", lookDirection.x);
        GetComponent<Animator>().SetFloat("LookVertical", lookDirection.y);
        GetComponent<Animator>().SetFloat("forwardspeed", movementSpeed.z);
        GetComponent<Animator>().SetFloat("sidespeed", movementSpeed.x);
        GetComponent<Animator>().SetFloat("Stance", stance);
    }

    void OnAnimatorIK()
    {
        GetComponent<Animator>().SetLookAtWeight(lookatWheight, lookatBodyWheight, lookatHeadWheight);
        GetComponent<Animator>().SetLookAtPosition(lookAtTarget.position);
    }
}
