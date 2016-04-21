using UnityEngine;
using System.Collections;

public class LinearTransformMovement : MonoBehaviour 
{
    public MovementState[] movementStates;
    public bool shouldPlay;
    bool _shouldPlay;

    void Update()
    {
        if(shouldPlay != _shouldPlay)
        {
            _shouldPlay = shouldPlay;
            if (shouldPlay)
                StartCoroutine("HandleAnim");
            else
                StopCoroutine("HandleAnim");
        }
    }

    IEnumerator HandleAnim()
    {
        int count = movementStates.Length;
        int curState = 0;
        float time = Time.time;
        while(curState < count)
        {
            yield return new WaitForEndOfFrame();
            MovementState ms = movementStates[curState];

            float t = (Time.time - time) / ms.length;
            transform.localPosition = Vector3.Lerp(ms.startPosition, ms.endPosition, t);
            transform.localRotation = Quaternion.Lerp(Quaternion.Euler(ms.startRotation), Quaternion.Euler(ms.endRotation), t);

            if (t >= 1)
            {
                curState++;
                time = Time.time;
            }
        }
    }

    [System.Serializable]
    public class MovementState
    {
        public Vector3 startPosition;
        public Vector3 endPosition;

        public Vector3 startRotation;
        public Vector3 endRotation;

        public float length = 1;
    }
}
