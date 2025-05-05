using UnityEngine;

public class Projectile : MonoBehaviour
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
        PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.UpdateHealth(-damage);
        }

        Destroy(gameObject);
    }

    public void SetDamage(int damageAmount)
    {
        damage = damageAmount;
    }
}