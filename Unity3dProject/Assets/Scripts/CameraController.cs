using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Vars
    [SerializeField] private float mouseSensitivity = 1000f;

    //Refs
    private Transform parent;

    private void Start() {
        parent = transform.parent;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {
        Rotate();
    }

    private void Rotate() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        parent.Rotate(Vector3.up, mouseX);
    }
}
