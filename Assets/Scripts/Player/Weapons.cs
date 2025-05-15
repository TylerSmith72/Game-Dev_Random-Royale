using UnityEngine;
using FishNet.Object;

public class Weapons : NetworkBehaviour
{
    [SerializeField]
    private GameObject projectilePrefab;
    [SerializeField]
    private Transform firePoint;
    [SerializeField]
    private int projectileDamage = 10;
    [SerializeField]
    private float maxAimDistance = 100f;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private LayerMask raycastMask;

    private void Update()
    {
        if(GetComponent<PlayerMenu>().isMenuOpen == true)
        {
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }
        
        if (IsOwner && Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector3 aimPoint = GetAimPoint();

        ShootServerRpc(aimPoint);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 aimPoint)
    {
        Vector3 direction = (aimPoint - firePoint.position).normalized;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDamage(projectileDamage);
        }

        Spawn(projectile); // Spawn on all clients
    }

    private Vector3 GetAimPoint()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, raycastMask))
        {
            Debug.Log("Hit detected at: " + hit.point);
            return hit.point;
        }
        else
        {
            // If no hit, return the maximum distance in the direction of the ray
            Debug.Log("No hit detected, returning max aim distance point.");
            return ray.origin + ray.direction * maxAimDistance;
        }
    }
}