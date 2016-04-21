using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace AngryRain.Menu
{
    public class DirectConnect : MonoBehaviour
    {
        public InputField inputField;

        public void ConnectTo()
        {
            NavigationController.NavigateTo("");
            NavigationController.SetMessage("Connecting to " + inputField.text);

            Multiplayer.MultiplayerManager.ConnectToIP(inputField.text);
        }
    }
}