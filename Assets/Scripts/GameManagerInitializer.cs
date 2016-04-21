using UnityEngine;
using System.Collections;

public class GameManagerInitializer : MonoBehaviour 
{
    public static bool haveWeInitialized;
    public GameObject gameManager;

    void Awake()
    {
        if(!haveWeInitialized)
        {
            haveWeInitialized = true;
            gameManager.SetActive(true);
        }
    }
}
