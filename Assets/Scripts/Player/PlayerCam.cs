using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform player;

    private float xRotation;
    private float yRotation;

    Camera cam;
    float baseFov = 90f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        float currentFov = cam.fieldOfView;
        float fovFactor = currentFov / baseFov;

        if (player.GetComponent<PlayerMenu>().isMenuOpen == true || player.GetComponent<PlayerMenu>().loadingScreen.activeSelf == true)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX * fovFactor;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY * fovFactor;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -89f, 89f);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        player.Rotate(Vector3.up * mouseX);

        // reduce fov when holding right mouse button
        if (Input.GetMouseButton(1))
        {
            GetComponent<Camera>().fieldOfView = Mathf.Lerp(GetComponent<Camera>().fieldOfView, baseFov / 3f, Time.deltaTime * 20);
        }
        else
        {
            GetComponent<Camera>().fieldOfView = Mathf.Lerp(GetComponent<Camera>().fieldOfView, baseFov, Time.deltaTime * 10);
        }
    }
}
