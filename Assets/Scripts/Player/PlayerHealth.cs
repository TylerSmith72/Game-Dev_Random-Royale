using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Connection;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField]
    private int maxHealth = 100;
    [SerializeField]
    private int currentHealth;
    [SerializeField]
    private Slider healthSlider;
    [SerializeField]
    private TextMeshProUGUI healthText;
    [SerializeField]
    private Canvas canvas;

    private GameManager gameManager;
    public GameObject playerBody;
    public GameObject visor;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
        {
            GetComponent<PlayerHealth>().enabled = false;
            canvas.enabled = false;
            return;
        }

        // Find the GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("Player Health: GameManager not found!");
        }

        // --- Spectator cam logic for late joiners ---
        if (gameManager != null)
        {
            // Fetch Game State
            gameManager.RequestCurrentGameState(NetworkManager.ClientManager.Connection);

            // Either join as Player or Spectator
            Debug.Log("GameState Health: " + gameManager._currentState.ToString());
            if (gameManager._currentState.ToString() == "Playing" ||
                gameManager._currentState.ToString() == "GameOver")
            {
                EnableSpectatorCam();
            }
            else
            {
                EnablePlayerCam();
            }
        }
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}";
        }

        // Find the GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
        }
    }

    void Update()
    {
        // Find the GameManager if not already assigned
        if (gameManager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                gameManager = gmObj.GetComponent<GameManager>();
                Debug.Log("GameManager reference assigned in Update.");
            }
            else
            {
                // Still not found, skip logic
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            UpdateHealth(-10);
        }
    }

    public void UpdateHealth(int amount)
    {
        int newHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthServer(newHealth); // Call the server to update health
    }

    // Server-side function to update health
    [ServerRpc(RequireOwnership = false)]
    private void UpdateHealthServer(int newHealth)
    {
        currentHealth = newHealth;

        if (currentHealth <= 0)
        {
            HandleDeath();
        }

        UpdateHealthClient(newHealth); // Update health on all clients
    }

    // Client-side function to synchronize health
    [ObserversRpc]
    private void UpdateHealthClient(int newHealth)
    {
        currentHealth = newHealth;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}";
        }

        Debug.Log($"Health updated to: {currentHealth}");
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} has died!");

        // Notify the GameManager about the death
        if (gameManager != null && IsServerInitialized)
        {
            gameManager.PlayerDied(base.Owner);
        }

        // Call the client-side death logic
        HandleDeathClient();

        // Notify all clients about the death
        HandleDeathObserversRpc();
    }

    [ObserversRpc]
    private void HandleDeathObserversRpc()
    {
        HandleDeathClient();
    }

    private void HandleDeathClient()
    {
        if (gameManager != null)
        {
            gameManager.RequestDeadPlayersList();
        }

        // Disable movement and camera
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            Debug.Log("PlayerMovement disabled.");
        }

        PlayerCam playerCam = GetComponentInChildren<PlayerCam>();
        if (playerCam != null)
        {
            playerCam.enabled = false;
            Debug.Log("PlayerCam disabled.");
        }

        // Enable ragdoll effect
        EnableRagdoll();
    }

    private void EnableRagdoll()
    {
        // Disable the CharacterController
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("CharacterController disabled.");
        }

        // Enable Rigidbody and Collider for the player model
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;

        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        capsuleCollider.radius = 0.6f;
        capsuleCollider.height = 2.6f;
        capsuleCollider.center = new Vector3(0, 0, 0);

        capsuleCollider.isTrigger = false;
        capsuleCollider.enabled = true;

        Debug.Log("Ragdoll effect enabled.");
        StartCoroutine(DelayedSpectatorCam(5f));
    }

    private IEnumerator DelayedSpectatorCam(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        EnableSpectatorCam();
    }
    
    // Enable freecam and disable playercam controls
    public void EnableSpectatorCam()
    {
        Debug.Log("Enabling spectator camera.");
        var cam = GetComponentInChildren<Camera>();
        var playerCam = cam.GetComponent<PlayerCam>();
        var freeCam = cam.GetComponent<FreeCam>();

        if (playerCam != null) playerCam.enabled = false;
        if (playerBody != null)
        {
            playerBody.SetActive(false);
            visor.SetActive(false);

            if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Destroy(rb);
                // rb.isKinematic = true;
                // rb.useGravity = false;
                // rb.velocity = Vector3.zero;
                // rb.angularVelocity = Vector3.zero;
            }
            else
            {
                Debug.LogWarning("Rigidbody not found on playerBody.");

            }
        }
        if (freeCam != null) freeCam.enabled = true;
    }

    // Enable playercam and disable freecam controls
    public void EnablePlayerCam()
    {
        Debug.Log("Enabling PlayerCam and disabling FreeCam.");
        var cam = GetComponentInChildren<Camera>();
        var playerCam = cam.GetComponent<PlayerCam>();
        var freeCam = cam.GetComponent<FreeCam>();

        
        if (playerCam != null) playerCam.enabled = true;
        if (playerBody != null)
        {
            playerBody.SetActive(true);
            visor.SetActive(true);

            if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            else
            {
                Debug.LogWarning("Rigidbody not found on playerBody.");

            }
        }
        if (freeCam != null) freeCam.enabled = false;

    }
}