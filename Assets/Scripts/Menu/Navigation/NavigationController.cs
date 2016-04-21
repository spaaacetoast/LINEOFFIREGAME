using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class NavigationController : MonoBehaviour 
{
    public static NavigationController instance;
    public static NavigationOptions[] allNavs = new NavigationOptions[0];
    public string firstMenu;

    public NavigationOptions currentMenu { get; set; }
    public GameObject controlsDisplay { get; set; }
    public GameObject messagePanel { get; set; }
    public GameObject errorPanel { get; set; }

    public bool startSplashScreen;

    public static bool messageActive;
    public static bool errorActive;

    void Awake()
    {
        instance = this;
        allNavs = GetComponentsInChildren<NavigationOptions>(true);

        controlsDisplay = transform.Find("Controls Display").gameObject;
        messagePanel = transform.Find("Message Panel").gameObject;
        errorPanel = transform.Find("Error Message Panel").gameObject;
    }

    void Start()
    {
        SetMessage(false);

        if (!TNManager.isConnected)
        {
            if (startSplashScreen)
            {
                transform.Find("intro screen").gameObject.SetActive(true);
                NavigateTo("");
            }
            else
                NavigateTo(firstMenu);
        }
    }

    public static void NavigateTo(string name)
    {
        NavigationOptions nav = null;

        for (int i = 0; i < allNavs.Length; i++)
            allNavs[i].gameObject.SetActive(false);

        for (int i = 0; i < allNavs.Length; i++)
            if (allNavs[i].navigationName == name)
                nav = allNavs[i];

        if (nav != null)
        {
            nav.gameObject.SetActive(true);
            nav.GetComponent<CanvasGroup>().interactable = !messageActive && !errorActive;
            instance.currentMenu = nav;

            instance.controlsDisplay.SetActive(nav.enableControlDisplay);
            if(nav.enableControlDisplay)
            {
                bool useController = false;
                if (Rewired.ReInput.players.GetPlayer(0).controllers.GetLastActiveController() != null)
                    useController = (int)Rewired.ReInput.players.GetPlayer(0).controllers.GetLastActiveController().type > 1;

                instance.controlsDisplay.transform.Find("Keyboard").gameObject.SetActive(!useController);
                instance.controlsDisplay.transform.Find("Controller").gameObject.SetActive(useController);
            }
        }
        else
        {
            instance.controlsDisplay.SetActive(false);
        }
    }

    public static void SetMessage(bool enable)
    {
        instance.messagePanel.SetActive(enable);
        messageActive = enable;
        if (instance.currentMenu)
            instance.currentMenu.GetComponent<CanvasGroup>().interactable = !errorActive;
    }

    public static void SetMessage(string message)
    {
        instance.messagePanel.SetActive(true);
        instance.messagePanel.transform.Find("Text").GetComponent<Text>().text = message;
        instance.currentMenu.GetComponent<CanvasGroup>().interactable = false;
        messageActive = true;
    }

    public static void SetError(string message)
    {
        errorActive = true;
        instance.errorPanel.gameObject.SetActive(true);
        instance.errorPanel.transform.Find("Text").GetComponent<Text>().text = message;
        instance.currentMenu.GetComponent<CanvasGroup>().interactable = false;
    }

    public static void SetError()
    {
        errorActive = false;
        instance.errorPanel.gameObject.SetActive(false);
        instance.currentMenu.GetComponent<CanvasGroup>().interactable = !messageActive;
    }
}
