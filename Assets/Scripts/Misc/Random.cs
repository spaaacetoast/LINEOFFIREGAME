using UnityEngine;
using System.Collections;

namespace AngryRain
{
    public static class Random
    {
        /// <summary>
        /// 50% chance of returning one of teh parameters
        /// </summary>
        /// <param name="num1"></param>
        /// <param name="num2"></param>
        /// <returns></returns>
        public static float GetNumberBasedOnRandom(float num1, float num2)
        {
            if (UnityEngine.Random.Range(0f, 2f) > 1f) return num1; else return num2;
        }

        /// <summary>
        /// Return -1 or 1 randomly
        /// </summary>
        /// <returns></returns>
        public static int GetNonZero1()
        {
            if (UnityEngine.Random.Range(-1, 1) >= 0) return 1; else return -1;
        }
    }
}