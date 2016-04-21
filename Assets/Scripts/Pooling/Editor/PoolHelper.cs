using UnityEngine;
using System.Collections;
using UnityEditor;

public class PoolHelper : EditorWindow
{
    ParticleEffect[] selectedParticles = new ParticleEffect[0];

    // Add menu item named "My Window" to the Window menu
    [MenuItem("GameObject/Custom System Helpers/Pool Helper")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(PoolHelper));
    }

    void OnGUI()
    {
        /*GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField("Text Field", myString);

        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        myBool = EditorGUILayout.Toggle("Toggle", myBool);
        myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();*/

        /*if (currentManager != null)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(25);
            EditorGUILayout.LabelField("Current selected object name: " + currentManager.objectName);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("Prepare Object"))
            {
                currentManager.objectSettings.objectColliders = currentManager.gameObject.GetComponentsInChildren<Collider>();
                currentManager.objectSettings.objectRigidbody = currentManager.gameObject.GetComponentInChildren<Rigidbody>();
                currentManager.networkView.observed = currentManager.rigidbody;
                currentManager.networkView.stateSynchronization = NetworkStateSynchronization.Unreliable;
                currentManager.rigidbody.useGravity = false;
            }
        }
        else
        {
            EditorGUILayout.LabelField("Please select an object with an EditObjectManager component attached to it.");
        }*/

        if (selectedParticles.Length != 0)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("You have selected " + selectedParticles.Length + " particle(s).");
            GUILayout.Space(5);
            if (GUILayout.Button("Update Selected Objects."))
            {
                for (int i = 0; i < selectedParticles.Length; i++)
                {
                    ParticleEffect par = selectedParticles[i];
                    par.transform = par.transform;
                    par.particleSystems = par.GetComponentsInChildren<ParticleSystem>(true);
                }
            }
            GUILayout.Space(5);
            for (int i = 0; i < selectedParticles.Length; i++)
            {
                EditorGUILayout.LabelField(selectedParticles[i].gameObject.name);
            }
        }
        else
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("No objects selected");
        }
    }

    void OnSelectionChange()
    {
        /*if (Selection.activeTransform.GetComponent<EditObjectManager>())
            currentManager = Selection.activeTransform.GetComponent<EditObjectManager>();
        else
            currentManager = null;*/
        selectedParticles = new ParticleEffect[Selection.gameObjects.Length];
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            ParticleEffect par = null;
            if (par = Selection.gameObjects[i].GetComponent<ParticleEffect>())
            {
                selectedParticles[i] = par;
            }
        }

        Repaint();
    }
}