using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AngryRain.Multiplayer.LevelEditor
{
    public class Spawnpoint : MonoBehaviour
    {
        public static List<Spawnpoint> allSpawnpoints = new List<Spawnpoint>();

        public GameObject thisGameObject;
        public Transform thisTransform;
        public Vector3 thisPosition;
        public Vector3 thisRotation;
        public bool isPersistent;

        void Awake()
        {
            thisGameObject = gameObject;
            thisTransform = transform;
            thisPosition = thisTransform.position;
            thisRotation = thisTransform.eulerAngles;
        }

        IEnumerator Start()
        {
            yield return null;
            if (MultiplayerManager.instance)
            {
                allSpawnpoints.Add(this);
            }
        }
    }
}