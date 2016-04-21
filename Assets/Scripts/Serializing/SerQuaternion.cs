using UnityEngine;

[System.Serializable]
public class SerQuaternion
{
    public float x, y, z, w;

    public void Set(Quaternion vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
        w = vec.w;
    }

    public void Get(Quaternion vec)
    {
        vec.x = x;
        vec.y = y;
        vec.z = z;
        vec.w = w;
    }

    public Quaternion Get()
    {
        return new Quaternion(x, y, z, w);
    }
}