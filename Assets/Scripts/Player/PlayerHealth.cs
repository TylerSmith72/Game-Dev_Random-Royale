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

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
        {
            GetComponent<PlayerHealth>().enabled = false;
            canvas.enabled = false;
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
        if(Input.GetKeyDown(KeyCode.H))
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
    [ServerRpc]
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
    }

    // We've removed the RespawnAfterDelay coroutine because the GameManager now handles respawning
}