using UnityEngine;
using System.Collections;

public class ErrorMessage : MonoBehaviour 
{
    void Update()
    {
        if(Rewired.ReInput.players.SystemPlayer.GetButton("Submit"))
            NavigationController.SetError();
    }
}
