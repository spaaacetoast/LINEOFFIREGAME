using UnityEngine;
using System.Collections;

namespace AngryRain
{
    public class Math : MonoBehaviour
    {
        public static void Clamp01(ref float number)
        {
            if (number > 1)
                number= 1;
            else if (number < 0)
                number= 0;
        }

        public static void Clamp01(ref Vector3 n)
        {
            /*for (int i = 0; i < 3; i++)//More convient but slower
            {
                if (n[i] > 1)
                    n[i] = 1;
                else if (n[i] < 0)
                    n[i] = 0;
            }*/
            if (n.x > 1)
                n.x = 1;
            else if (n.x < 0)
                n.x = 0;

            if (n.y > 1)
                n.y = 1;
            else if (n.y < 0)
                n.y = 0;
            
            if (n.z > 1)
                n.z = 1;
            else if (n.z < 0)
                n.z = 0;
        }

        public static float CorrectRotation(float numb)
        {
            if (numb >= 180)
                numb -= 360;
            if (numb <= -180)
                numb += 360;
            return numb;
        }

        public static Vector3 Vector3Lerp(Vector3 one, Vector3 two, float t)
        {
            Clamp01(ref t);
            return new Vector3(
                    one.x + (two.x - one.x) * t,
                    one.y + (two.y - one.y) * t,
                    one.z + (two.z - one.z) * t
                );
        }

        public static void Vector3Lerp(ref Vector3 one, Vector3 two, float t)
        {
            Clamp01(ref t);
            one.x += (two.x - one.x) * t;
            one.y += (two.y - one.y) * t;
            one.z += (two.z - one.z) * t;
        }

        public static Vector3 Vector3Lerp(Vector3 one, Vector3 two, float t, bool fixRotation)
        {
            if (fixRotation)
            {
                /*one = Vector3CorrectRotation(one);
                two = Vector3CorrectRotation(two);*/

                /*if (one.x - two.x > 180)
                    one.x -= 360;
                else if (one.x - two.x < -180)
                    one.x += 360;
                else if (one.x + two.x > 180)
                    one.x -= 360;
                else if (one.x + two.x < -180)
                    one.x += 360;*/

                /*if (one.y - two.y > 180)
                    one.y -= 360;
                else if (one.y - two.y < -180)
                    one.y += 360;
                else if (one.y + two.y > 180)
                    one.y -= 360;
                else if (one.y + two.y < -180)
                    one.y += 360;*/

                /*if (one.z - two.z > 180)
                    one.z -= 360;
                else if (one.z - two.z < -180)
                    one.z += 360;
                else if (one.z + two.z > 180)
                    one.z -= 360;
                else if (one.z + two.z < -180)
                    one.z += 360;*/

                return Quaternion.Lerp(Quaternion.Euler(one), Quaternion.Euler(two), t).eulerAngles;
            }
            else
            {
                return Vector3Lerp(one, two, t);
            }
        }

        public static Vector3 Vector3Lerp(Vector3 one, Vector3 two, Vector3 t)
        {
            Clamp01(ref t);
            one.x += (two.x - one.x) * t.x;
            one.y += (two.y - one.y) * t.y;
            one.z += (two.z - one.z) * t.z;
            return one;
        }

        public static void Vector3Lerp(ref Vector3 one, Vector3 two, Vector3 t)
        {
            Clamp01(ref t);
            one.x += (two.x - one.x) * t.x;
            one.y += (two.y - one.y) * t.y;
            one.z += (two.z - one.z) * t.z;
        }

        public static Vector3 Vector3CorrectRotation(Vector3 vec3)
        {
            if (vec3.x > 180)
                vec3.x -= 360;
            if (vec3.y > 180)
                vec3.y -= 360;
            if (vec3.z > 180)
                vec3.z -= 360;

            if (vec3.x < -180)
                vec3.x += 360;
            if (vec3.y < -180)
                vec3.y += 360;
            if (vec3.z < -180)
                vec3.z += 360;

            return vec3;
        }

        public static Vector3 Vector3Multiply(Vector3 one, Vector3 two)
        {
            one.x *= two.x;
            one.y *= two.y;
            one.z *= two.z;
            return one;
        }

        public static Vector3 Vector3Multiply(Vector3 one, float two)
        {
            one.x *= two;
            one.y *= two;
            one.z *= two;
            return one;
        }

        public static Vector3 Vector3Divide(Vector3 one, Vector3 two)
        {
            return new Vector3(one.x / two.x, one.y / two.y, one.z / two.z);
        }

        public static Vector3 Vector3Clamp(Vector3 original, Vector3 clamp)
        {
            original.x = Mathf.Clamp(original.x, -clamp.x, clamp.x);
            original.y = Mathf.Clamp(original.y, -clamp.y, clamp.y);
            original.z = Mathf.Clamp(original.z, -clamp.z, clamp.z);
            return original;
        }

        public static Vector3 Vector3Random(Vector3 min, Vector3 max)
        {
            for (int i = 0; i < 3; i++)
                min[i] = UnityEngine.Random.Range(min[i], max[i]);
            return min;
        }

        public static float GetValueOverDistance(float value, float distance, float minimum, float maximum)
        {
            if (distance < minimum)
                return value;
            else if (distance > maximum)
                return 0;
            else
                return (maximum - (distance - minimum)) / (maximum + minimum) * value;
        }
    }
}
