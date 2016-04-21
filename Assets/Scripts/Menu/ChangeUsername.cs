using UnityEngine;
using System.Collections;
using UnityEngine.UI;
namespace AngryRain.Menu
{
    public class ChangeUsername : MonoBehaviour
    {
        public InputField inputField;

        public void ChangeName()
        {
            LocalPlayerManager.localPlayers[0].playerName = inputField.text;
            PlayerPrefs.SetString("playername", inputField.text);
            PlayerPrefs.Save();

            NavigationController.NavigateTo("multiplayer menu");
            NavigationController.SetError("Username has been changed to " + inputField.text);
        }
    }
}