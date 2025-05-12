using FishNet.Example;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMenu : NetworkBehaviour
{
    [SerializeField]
    private GameObject playerMenu;
    [SerializeField]
    private GameObject playerCanvas;
    public GameObject loadingScreen;
    [SerializeField]
    private Text loadingText;

    private Button exitButton;
    private NetworkHudCanvases networkHudCanvas;
    private MeshGenerator meshGenerator;
    private GameObject canvas;
    private GameObject mainMenu;

    public bool isMenuOpen = false;

    private void Start()
    {
        networkHudCanvas = FindObjectOfType<NetworkHudCanvases>();
        meshGenerator = FindObjectOfType<MeshGenerator>();
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

        if (loadingScreen != null)
        {
            MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();
            if (meshGenerator != null)
            {
                if (meshGenerator.hasLoadedTerrain){
                    HideLoadingScreen();
                }
            }
            
            ShowLoadingScreen();
        }
    }

    private void Update()
    {
        if(loadingScreen.activeSelf == true)
        {
            // Check if the terrain has finished loading for this client
            if (meshGenerator != null && meshGenerator.hasLoadedTerrain)
            {
                HideLoadingScreen();
            }
            return; // Don't allow any input when loading screen is active
        }

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

    public void ShowLoadingScreen()
    {
        Debug.Log("Showing loading screen...");
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (loadingText != null)
            {
                loadingText.text = "Loading Terrain...";
            }
        }
    }

    public void HideLoadingScreen()
    {
        Debug.Log("Hiding loading screen...");
        isMenuOpen = false;
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Debug.Log($"OnStartNetwork called. IsLocalClient: {base.Owner.IsLocalClient}");

        if (!base.Owner.IsLocalClient)
        {
            Debug.Log("Disabling components for non-local client.");
            playerCanvas.SetActive(false);
            playerMenu.SetActive(false);
            loadingScreen.SetActive(false);
            enabled = false;
        }
    }
}