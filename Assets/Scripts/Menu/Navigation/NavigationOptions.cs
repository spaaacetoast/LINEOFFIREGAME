using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using TNet;

public class NavigationOptions : MonoBehaviour 
{
    public string navigationName = "menu";
    public string backMenu = "";

    public GameObject firstSelection;

    public bool enableControlDisplay;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        Rewired.Controller con = Rewired.ReInput.players.GetSystemPlayer().controllers.GetLastActiveController();
        if (firstSelection && con != null && con.type == Rewired.ControllerType.Joystick)
            EventSystem.current.SetSelectedGameObject(firstSelection);
    }

    public void NavigateTo(string name)
    {
        NavigationController.NavigateTo(name);
    }

    void Update()
    {
        //if (EventSystem.current.currentSelectedGameObject == null && Rewired.ReInput.players.GetSystemPlayer().controllers.GetLastActiveController().type == Rewired.ControllerType.Joystick)
        //{
        //    if (EventSystem.current.lastSelectedGameObject.activeInHierarchy)
        //        EventSystem.current.SetSelectedGameObject(EventSystem.current.lastSelectedGameObject);
        //    else if (firstSelection)
        //        EventSystem.current.SetSelectedGameObject(firstSelection);
        //}

        if (backMenu != "" && Rewired.ReInput.players.GetPlayer(0).GetButtonDown("Cancel"))
        {
            NavigateTo(backMenu);
        }
    }
}
