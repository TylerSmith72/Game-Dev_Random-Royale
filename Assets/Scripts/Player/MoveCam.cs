using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCam : MonoBehaviour
{
    private Transform cameraPosition;
    public void SetPlayer(Transform playerCameraPos)
    {
        cameraPosition = playerCameraPos;
    }

    private void Update()
    {
        if (cameraPosition != null)
        {
            transform.position = cameraPosition.position;
        }
    }
}
