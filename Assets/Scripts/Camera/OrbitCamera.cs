using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float orbitSpeed = 0.2f;
    public float height = 30f;
    public float radius = 40f;

    private float angle = 0f;

    void Update()
    {
        if (target == null) return;

        // Calculate the new position
        angle += orbitSpeed * Time.deltaTime;
        float x = target.position.x + Mathf.Cos(angle) * radius;
        float z = target.position.z + Mathf.Sin(angle) * radius;
        float y = target.position.y + height;

        // Apply the position and look at the target
        transform.position = new Vector3(x, y, z);
        transform.LookAt(target);
    }
}
