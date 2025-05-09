using System.Collections;
using System.Collections.Generic;
using FishNet.Example;
using UnityEngine;

public class ServerMenu : MonoBehaviour
{
    public GameObject networkHud;
    private NetworkHudCanvases networkHudCanvas;
    public GameObject menuVideo;

    // public KeyCode serverKey = KeyCode.O;
    // public KeyCode clientKey = KeyCode.P;

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
        // if (networkHudCanvas == null)
        // {
        //     return;
        // }

        // if (Input.GetKeyDown(serverKey))
        // {
        //     StartServer();
        // }

        // if (Input.GetKeyDown(clientKey))
        // {
        //     JoinServer();
        // }
    }

    public void StartServer(){
        networkHudCanvas.OnClick_Server();

        JoinServer();
    }

    public void JoinServer(){
        networkHudCanvas.OnClick_Client();

        if(menuVideo.activeSelf == true)
        {
            menuVideo.SetActive(false);
        } else {
            menuVideo.SetActive(true);
        }
    }
}
