using UnityEngine;
using System.Collections;

public class FastGUI : MonoBehaviour 
{
    /*public GUISkin guiskin;

    void OnGUI()
    {
        GUI.skin = guiskin;

        Vector3 worldPosition = transform.position;
        Vector3 screenPosition = Camera.current.WorldToScreenPoint(worldPosition);
        float screenSpaceDistanceToMiddle = Vector2.Distance(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(screenPosition.x, screenPosition.y));

        if(screenPosition.z > 0)
        {
            if(screenSpaceDistanceToMiddle > Screen.height * 0.1f)
            {
                GUI.Box(new Rect(screenPosition.x - 20, Screen.height - screenPosition.y - 40, 40, 40), "");
            }
            else
            {
                GUI.Label(new Rect(screenPosition.x - 100, Screen.height - screenPosition.y - 50, 200, 50), "RainslayerX");
            }
        }
    }*/
}
