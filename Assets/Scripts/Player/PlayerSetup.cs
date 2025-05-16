using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    public Camera playerCamera;
    public CharacterController characterController;
    public PlayerMovement playerMovement;
    public PlayerCam playerCam;
    public GameObject gameManagerObject;
    public GameObject terrainManagerObject;

    [SerializeField]
    private string localPlayerLayer = "LocalPlayer";
    [SerializeField]
    private string remotePlayerLayer = "RemotePlayer";

    public void Awake()
    {

    }

    public void Update()
    {
        if (!base.IsOwner)
        {
            Debug.Log("This object is not owned by the local client.");
            return;
        }

        if (gameManagerObject == null)
        {
            Debug.LogError("GameManagerObject is null!");
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed. Calling RespawnPlayer...");
            RespawnPlayer();
        }

        if (base.IsOwner && gameManagerObject != null && Input.GetKeyDown(KeyCode.R))
        {   
            RespawnPlayer();
        }
    }

    public void RespawnPlayer(){
        Debug.Log("Respawning player from setup...");
        if (IsServerInitialized)
        {
            // If this instance is the server, directly call the respawn logic
            gameManagerObject.GetComponent<GameManager>().ServerRequestRespawn(base.Owner);
        }
        else if (IsClientInitialized)
        {
            // If this instance is a client, send a request to the server
            RequestRespawnFromServer();
        }

        PlayerMenu playerMenu = GetComponent<PlayerMenu>();
        if (playerMenu != null)
        {
            playerMenu.ShowLoadingScreen();
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRespawnFromServer()
    {
        Debug.Log("Client requested respawn from server.");
        gameManagerObject.GetComponent<GameManager>().ServerRequestRespawn(base.Owner);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Debug.Log($"OnStartNetwork called. IsLocalClient: {base.Owner.IsLocalClient}");

        if (!base.Owner.IsLocalClient) // If not the local client, disable components
        {
            SetLayerRecursively(gameObject, LayerMask.NameToLayer(remotePlayerLayer));

            Debug.Log("Disabling components for non-local client.");
            if (playerCamera != null)
                playerCamera.enabled = false;

            if (characterController != null)
                characterController.enabled = false;

            if (playerMovement != null)
                playerMovement.enabled = false;

            if (playerCam != null)
                playerCam.enabled = false;

            enabled = false;
        } else {
            SetLayerRecursively(gameObject, LayerMask.NameToLayer(localPlayerLayer));
        }
        gameObject.GetComponent<CapsuleCollider>().isTrigger = true;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient called for PlayerSetup.");

        // Find the GameManager and register this player
        GameObject gameManager = FindObjectOfType<GameManager>()?.gameObject;
        if (gameManager != null)
        {
            gameManager.GetComponent<GameManager>().RegisterPlayer(this.gameObject);
        }
        else
        {
            Debug.LogError("GameManager not found on client!");
        }

        if(!base.Owner.IsLocalClient){
            return; // Don't set player camera for non-local clients
        }

        // Find the GameManager and register this player
        GameObject terrainManager = FindObjectOfType<SeedGenerator>()?.gameObject;
        if (terrainManager != null)
        {
            terrainManager.GetComponent<SeedGenerator>().SetPlayer(gameObject);
        }
        else
        {
            Debug.LogError("TerrainManager not found on client!");
        }
        
        GameObject meshGenerator = FindObjectOfType<MeshGenerator>()?.gameObject;
        if (meshGenerator != null)
        {
            meshGenerator.GetComponent<MeshGenerator>().SetPlayer(gameObject);
        }
        else
        {
            Debug.LogError("TerrainManager not found on client!");
        }
    }

    public void SetGameManager(GameObject gameManagerObject)
    {
        this.gameManagerObject = gameManagerObject;
        Debug.Log("GameManager set in PlayerSetup: " + gameManagerObject);
    }
}
