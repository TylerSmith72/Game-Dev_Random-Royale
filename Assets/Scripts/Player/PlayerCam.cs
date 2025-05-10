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

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if(player.GetComponent<PlayerMenu>().isMenuOpen == true || player.GetComponent<PlayerMenu>().loadingScreen.activeSelf == true)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -89f, 89f);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        player.Rotate(Vector3.up * mouseX);
    }
}
