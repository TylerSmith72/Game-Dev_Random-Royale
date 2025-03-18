using System.Collections;
using System.Collections.Generic;
using FishNet.Example;
using UnityEngine;

public class ServerMenu : MonoBehaviour
{
    public GameObject networkHud;
    private NetworkHudCanvases networkHudCanvas;

    public KeyCode serverKey = KeyCode.O;
    public KeyCode clientKey = KeyCode.P;

    private void Start()
    {
        if (networkHud == null)
        {
            Debug.LogError("NetworkHud GameObject is not assigned!");
            return;
        }

        networkHudCanvas = networkHud.GetComponent<NetworkHudCanvases>();
        if (networkHudCanvas == null)
        {
            Debug.LogError("NetworkHudCanvases component not found on the assigned GameObject!");
        }
    }

    private void Update()
    {
        if (networkHudCanvas == null)
        {
            return;
        }

        if (Input.GetKeyDown(serverKey))
        {
            networkHudCanvas.OnClick_Server();
        }

        if (Input.GetKeyDown(clientKey))
        {
            networkHudCanvas.OnClick_Client();
        }
    }
}
