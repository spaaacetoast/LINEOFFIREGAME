/*using UnityEngine;
using System.Collections;

namespace AngryRain.Menu
{
    public class Login : MonoBehaviour
    {
        public GUISkin skin;

        private string username = "Username";
        private string password = "Password";
        private string email = "Email";
        private string name = "Name";
        private bool waitingForServer = false;
        private bool loginMenu = true;
        private string message = "";

        public IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            GetComponent<MenuManager>().enabled = false;
        }

        private void Update()
        {
            if (loginMenu)
            {
                if (NetworkManager.LoggedIn())
                {
                    GetComponent<MenuManager>().enabled = true;
                    this.enabled = false;
                }
                else if (NetworkManager.serverResponce != null)// null always means its successfull
                {
                    waitingForServer = false;
                }
                message = NetworkManager.serverResponce;
            }
            else
            {
                if (NetworkManager.serverResponce == null)
                {
                    waitingForServer = false;
                    message = "User Created.";
                }
                else
                    message = NetworkManager.serverResponce;
            }
        }

        public void OnGUI()
        {
            GUI.skin = skin;

            if (!waitingForServer)
            {

                if (loginMenu)
                {
                    GUILayout.BeginArea(new Rect(25, Screen.height - 400, 350, 375));
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical("Box");

                    if (NetworkManager.serverResponce != null)
                        GUILayout.Box(NetworkManager.serverResponce);
                    username = GUILayout.TextField(username);
                    password = GUILayout.PasswordField(password, '*');
                    if (GUILayout.Button("Login", GUILayout.Height(30)))
                    {
                        NetworkManager.Login("213.107.103.140", username, password); // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                        waitingForServer = true;
                    }
                    if (GUILayout.Button("Register", GUILayout.Height(30)))
                    {
                        loginMenu = false;
                        message = null;
                    }
                    GUILayout.Button("Quit Game", GUILayout.Height(30));

                    GUILayout.EndVertical();

                    GUILayout.EndArea();
                }
                else
                {
                    GUILayout.BeginArea(new Rect(25, Screen.height - 400, 350, 375));
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical("Box");

                    if (NetworkManager.serverResponce != null)
                        GUILayout.Box(NetworkManager.serverResponce);
                    username = GUILayout.TextField(username);
                    password = GUILayout.PasswordField(password, '*');
                    email = GUILayout.TextField(email);
                    name = GUILayout.TextField(name);
                    if (GUILayout.Button("Register", GUILayout.Height(30)))
                    {
                        NetworkManager.Register("213.107.103.140", username, password, email, name); // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                        waitingForServer = true;
                    }
                    if (GUILayout.Button("Back", GUILayout.Height(30)))
                    {
                        loginMenu = true;
                        message = null;
                    }

                    GUILayout.EndVertical();

                    GUILayout.EndArea();
                }
            }
            else
            {
                GUILayout.BeginArea(new Rect(25, Screen.height - 400, 350, 375));
                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical("Box");

                GUILayout.TextField("Please wait...");

                GUILayout.EndVertical();

                GUILayout.EndArea();
            }
        }
    }
}*/