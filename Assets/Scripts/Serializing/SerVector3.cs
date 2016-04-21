using UnityEngine;
using System.Collections;

namespace AngryRain
{
    [System.Serializable]
    public class SerVector3
    {
        public float x, y, z;

        public void Set(Vector3 vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        public void Get(Vector3 vec)
        {
            vec.x = x;
            vec.y = y;
            vec.z = z;
        }

        public Vector3 Get()
        {
            return new Vector3(x, y, z);
        }
    }
}