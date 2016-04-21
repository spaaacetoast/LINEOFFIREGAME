using UnityEngine;
using System.Collections;

namespace AngryRain.Multiplayer.LevelEditor
{
    public class GizmoTransform : MonoBehaviour
    {
        public GizmoTransformDirection gizmoTransformDirection;

        private Vector3 mouseDifference = Vector3.zero;

        void Start()
        {
            Color targetColor = gizmoTransformDirection == GizmoTransformDirection.X ? Color.red :
                gizmoTransformDirection == GizmoTransformDirection.Y ? Color.blue : Color.green;
            GetComponent<Renderer>().material.color = targetColor;
            GetComponent<Renderer>().material.SetColor("_ReflectColor", targetColor);
        }

        void OnMouseDown()
        {
            LevelManager.instance.currentGizmo = this;
            Color targetColor = Color.yellow;
            GetComponent<Renderer>().material.color = targetColor;
            GetComponent<Renderer>().material.SetColor("_ReflectColor", targetColor);
            //mouseDifference = LocalPlayerManager.localPlayers[0].currentPlayerCamera.thisCamera.WorldToScreenPoint(transform.position) - Input.mousePosition;
        }

        void OnMouseUp()
        {
            if (mouseDifference != Vector3.zero)
            {
                LevelManager.instance.currentGizmo = null;
                mouseDifference = Vector3.zero;

                Color targetColor = gizmoTransformDirection == GizmoTransformDirection.X ? Color.red :
                gizmoTransformDirection == GizmoTransformDirection.Y ? Color.blue : Color.green;
                GetComponent<Renderer>().material.color = targetColor;
                GetComponent<Renderer>().material.SetColor("_ReflectColor", targetColor);
            }
        }

        public Vector3 GetNextPosition(Vector3 currentPosition, LocalPlayer lPlayer)
        {
            if (mouseDifference == Vector3.zero)
            {
                mouseDifference = Input.mousePosition - lPlayer.playerCamera.camera.WorldToScreenPoint(currentPosition);
                //mouseDifference.z = 0;
            }

            //float dis = Vector3.Distance(currentPosition, lPlayer.currentPlayerCamera.thisPosition);

            Vector3 newPosition = Input.mousePosition;
            //newPosition.z = dis;
            newPosition = lPlayer.playerCamera.camera.ScreenToWorldPoint(newPosition - mouseDifference);

            if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z))
                return Vector3.zero;

            if (gizmoTransformDirection == GizmoTransformDirection.X)
                currentPosition.x = newPosition.x;
            if (gizmoTransformDirection == GizmoTransformDirection.Y)
                currentPosition.y = newPosition.y;
            if (gizmoTransformDirection == GizmoTransformDirection.Z)
                currentPosition.z = newPosition.z;

            return currentPosition;
        }

        public enum GizmoTransformDirection
        {
            X, Y, Z
        }
    }
}