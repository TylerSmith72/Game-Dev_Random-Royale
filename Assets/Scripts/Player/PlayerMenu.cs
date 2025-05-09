using FishNet.Example;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMenu : NetworkBehaviour
{
    [SerializeField]
    private GameObject playerMenu;
    private Button exitButton;

    private NetworkHudCanvases networkHudCanvas;

    private GameObject canvas;
    private GameObject mainMenu;

    public bool isMenuOpen = false;

    private void Start()
    {
        networkHudCanvas = FindObjectOfType<NetworkHudCanvases>();
        canvas = GameObject.Find("Canvas");

        if(canvas != null)
        {
            mainMenu = canvas.transform.Find("Main Menu").gameObject;
        }
        else
        {
            Debug.LogError("Canvas not found!");
        }

        if (playerMenu != null)
        {
            playerMenu.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (playerMenu != null)
        {
            playerMenu.SetActive(isMenuOpen);
        }

        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        if(IsServerInitialized){
            networkHudCanvas.OnClick_Server(); // Leave Server - Also leaves Client
        } else {
            networkHudCanvas.OnClick_Client(); // Leave Client
        }
        mainMenu.SetActive(true);
    }
}