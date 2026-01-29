using UnityEngine;

namespace StylizedSkybox {

    public class FreeCameraMove : MonoBehaviour {
        public float cameraSensitivity = 150;
        public float climbSpeed = 20;
        public float normalMoveSpeed = 20;
        public float slowMoveFactor = 0.25f;
        public float fastMoveFactor = 3;

        float rotationX = 0.0f;
        float rotationY = 0.0f;

        void Start() {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update() {
            Vector3 mousePos = InputProxy.MousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
                return;

            rotationX += InputProxy.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
            rotationY += InputProxy.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, -90, 90);

            transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.Rotate(Vector3.left, rotationY, Space.Self);

            if (InputProxy.GetKey(KeyCode.LeftShift) || InputProxy.GetKey(KeyCode.RightShift)) {
                transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * InputProxy.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) * InputProxy.GetAxis("Horizontal") * Time.deltaTime;
            } else if (InputProxy.GetKey(KeyCode.LeftControl) || InputProxy.GetKey(KeyCode.RightControl)) {
                transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) * InputProxy.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) * InputProxy.GetAxis("Horizontal") * Time.deltaTime;
            } else {
                transform.position += transform.forward * normalMoveSpeed * InputProxy.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * normalMoveSpeed * InputProxy.GetAxis("Horizontal") * Time.deltaTime;
            }

            if (InputProxy.GetKey(KeyCode.Q)) {
                transform.position -= transform.up * climbSpeed * Time.deltaTime;
            }
            if (InputProxy.GetKey(KeyCode.E)) {
                transform.position += transform.up * climbSpeed * Time.deltaTime;
            }

        }

    }
}