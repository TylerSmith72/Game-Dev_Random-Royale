using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using System.Xml.Serialization;
using Unity.VisualScripting;

public class PlayerMovement : NetworkBehaviour
{
    public GameObject playerModel;
    public GameObject visor;

    public PlayerCam playerCam;
    public CharacterController controller;

    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    private void Start()
    {

    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if(GetComponent<PlayerMenu>().isMenuOpen == true || GetComponent<PlayerMenu>().loadingScreen.activeSelf == true)
        {
            return;
        }

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // if (base.IsOwner && gameManager != null && Input.GetKeyDown(KeyCode.R))
        // {   
        //     Debug.Log("Respawning player from setup...");
        //     gameManager.Respawn();
        // }
    }

    private void DisablePlayerModel()
    {
        playerModel.GetComponent<MeshRenderer>().enabled = false;
        visor.GetComponent<MeshRenderer>().enabled = false;
    }

    //public override void OnStartClient()
    //{
    //    base.OnStartClient();
    //    if (base.IsOwner)
    //    {
    //        Debug.Log("Player is owner");
    //        DisablePlayerModel();
    //    }
    //    else
    //    {
    //        gameObject.GetComponent<PlayerMovement>().enabled = false;
    //        gameObject.GetComponent<CharacterController>().enabled = false;
    //    }
    //}
}
