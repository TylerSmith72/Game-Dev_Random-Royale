using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using System.Xml.Serialization;

public class PlayerMovement : NetworkBehaviour
{
    public Transform orientation;
    public Transform camPos;
    public MeshGenerator TerrainManager;
    public MoveCam camController;
    public PlayerCam playerCam;

    [Header("References")]
    private CharacterController controller;

    [Header("Movement Settings")]
    [SerializeField] private float walkspeed = 5f;
    [SerializeField] private float turningSpeed = 1f;
    [SerializeField] private float gravity = 9.81f;

    private float verticalVelocity;

    [Header("Input")]
    private float moveInput;
    private float turnInput;

    private void Start()
    {
        TerrainManager = GetComponent<MeshGenerator>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        InputManagement();
        Movement();
    }

    private void Movement()
    {
        GroundMovement();
        Turn();
    }

    private void GroundMovement()
    {
        Vector3 move = new Vector3(turnInput, 0, moveInput);
        move = transform.TransformDirection(move);

        move *= walkspeed;

        move.y = VerticalForceCalculation();

        controller.Move(move * Time.deltaTime);
    }

    private void Turn()
    {
        if (playerCam == null)
        {
            Debug.Log("PlayerCam not loaded yet...");
            return;
        }

        Vector3 lookDirection = playerCam.transform.forward;

        lookDirection.y = 0;

        if (lookDirection.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private float VerticalForceCalculation()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        return verticalVelocity;
    }

    private void InputManagement()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    private void SetPlayerForAll()
    {
        // Find and assign PlayerCam
        if (playerCam == null)
        {
            playerCam = FindObjectOfType<PlayerCam>();
            if (playerCam == null)
            {
                Debug.LogError("PlayerCam not found in the scene!");
                return;
            }
        }
        playerCam.SetOrientation(orientation);

        // Find and assign CameraController
        if (camController == null)
        {
            camController = FindObjectOfType<MoveCam>();
            if (camController == null)
            {
                Debug.LogError("CameraController (MoveCam) not found in the scene!");
                return;
            }
        }
        camController.SetPlayer(camPos);

        // Find and assign TerrainManager
        if (TerrainManager == null)
        {
            TerrainManager = FindObjectOfType<MeshGenerator>();
            if (TerrainManager == null)
            {
                Debug.LogError("TerrainManager (MeshGenerator) not found in the scene!");
                return;
            }
        }
        TerrainManager.SetPlayer(gameObject);

        // Find and assign TreeGenerator
        TreeGenerator treeGen = TerrainManager.GetComponent<TreeGenerator>();
        if (treeGen != null)
        {
            treeGen.SetPlayer(gameObject);
        }
        else
        {
            Debug.LogError("TreeGenerator component not found on TerrainManager!");
        }
    }



    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            Debug.Log("Player is owner");
            SetPlayerForAll();
        }
        else
        {
            gameObject.GetComponent<PlayerMovement>().enabled = false;
        }
    }
}
