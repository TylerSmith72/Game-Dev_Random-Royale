using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField]
    private float speed = 20f;
    [SerializeField]
    private float lifetime = 5f;
    [SerializeField]
    private LayerMask collisionMask;
    private Vector3 lastPosition;

    private int damage = 10;

    private void Start()
    {
        Destroy(gameObject, lifetime); // Destroy after time passed

        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 nextPosition = transform.position + transform.forward * speed * Time.deltaTime;

        // Damage player if raycast hits collider
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Vector3.Distance(transform.position, nextPosition), collisionMask))
        {
            OnHit(hit.collider);
        }

        // Move projectile forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        lastPosition = transform.position;
    }

    // Damage player if hit collider
    private void OnTriggerEnter(Collider other)
    {
        OnHit(other);
    }

    private void OnHit(Collider collider)
    {
        if (IsServerInitialized)
        {
            PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.UpdateHealth(-damage);
            }

            Despawn(gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerOfHit(NetworkConnection targetPlayer, int damage)
    {
        if (targetPlayer != null)
        {
            PlayerHealth playerHealth = targetPlayer.FirstObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.UpdateHealth(-damage);
            }
        }
    }

    public void SetDamage(int damageAmount)
    {
        damage = damageAmount;
    }
}