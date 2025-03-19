using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using System.Xml.Serialization;

public class PlayerMovement : NetworkBehaviour
{
    public Transform orientation;
    public GameObject TerrainManager;
    public MoveCam camController;
    public PlayerCam playerCam;

    [Header("References")]
    private CharacterController controller;

    [Header("Movement Settings")]
    [SerializeField] private float walkspeed = 5f;

    [Header("Input")]
    private float moveInput;
    private float turnInput;

    private void Start()
    {
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
    }

    private void GroundMovement()
    {
        Vector3 move = new Vector3(turnInput, 0, moveInput);

        move.y = 0;

        move *= walkspeed;

        controller.Move(move * Time.deltaTime);
    }

    private void InputManagement()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            Debug.Log("Player is owner");
            //camController.GetComponent<MoveCam>().SetPlayer(transform.Find("CameraPos"));
            //playerCam.GetComponent<PlayerCam>().SetOrientation(orientation);
            //TerrainManager.GetComponent<MeshGenerator>().SetPlayer(gameObject.transform);
            //TerrainManager.GetComponent<TreeGenerator>().SetPlayer(gameObject);
        }
        else
        {
            gameObject.GetComponent<PlayerMovement>().enabled = false;
        }
    }
}
