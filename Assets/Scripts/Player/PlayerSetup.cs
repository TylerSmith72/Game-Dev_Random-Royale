using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    public Camera playerCamera;
    public CharacterController characterController;
    public PlayerMovement playerMovement;
    public PlayerCam playerCam;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (!base.Owner.IsLocalClient)
        {
            if (playerCamera != null)
                playerCamera.enabled = false;

            if (characterController != null)
                characterController.enabled = false;

            if (playerMovement != null)
                playerMovement.enabled = false;

            if (playerCam != null)
                playerCam.enabled = false;
        }
    }
}
