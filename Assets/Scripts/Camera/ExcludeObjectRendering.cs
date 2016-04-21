using UnityEngine;
using System.Collections;
using TNet;

public class ExcludeObjectRendering : MonoBehaviour 
{
    public List<Renderer> excludedObjects;
    public List<Renderer> includedObjects;

    //This will be called before the camere renders anything, OnPreCull is chosen because here is decided wich objects are visible
    void OnPreCull()
    {
        try
        {
            for (int i = 0; i < excludedObjects.size; i++)
                excludedObjects.buffer[i].enabled = false;

            for (int i = 0; i < excludedObjects.size; i++)
                includedObjects.buffer[i].enabled = true;
        }
        catch(System.Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    //Revert the process that was applied above
    void OnPostRender()
    {
        try
        {
            for (int i = 0; i < excludedObjects.size; i++)
                excludedObjects.buffer[i].enabled = true;

            for (int i = 0; i < excludedObjects.size; i++)
                includedObjects.buffer[i].enabled = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    void CheckRenderers()
    {
        int index = 0;
        while(index < excludedObjects.size)
        {
            if (excludedObjects[index] == null)
                excludedObjects.RemoveAt(index);
            else
                index++;
        }
    }
}
